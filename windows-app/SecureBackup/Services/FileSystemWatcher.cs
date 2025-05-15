using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SecureBackup.Models;

namespace SecureBackup.Services
{
    public class FileSystemWatcherService
    {
        private readonly ConfigurationService _configService;
        private readonly EncryptionService _encryptionService;
        private readonly AwsService _awsService;
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private readonly ConcurrentDictionary<string, DateTime> _pendingChanges = new ConcurrentDictionary<string, DateTime>();
        private readonly SemaphoreSlim _processSemaphore = new SemaphoreSlim(1, 1);
        private Timer _processTimer;
        private bool _isEnabled;
        
        // Constants
        private const int PROCESS_DELAY_MS = 2000; // Delay to batch changes

        public event EventHandler<FileBackupEventArgs> FileBackupStarted;
        public event EventHandler<FileBackupEventArgs> FileBackupCompleted;
        public event EventHandler<FileBackupEventArgs> FileBackupFailed;

        public FileSystemWatcherService(
            ConfigurationService configService,
            EncryptionService encryptionService,
            AwsService awsService)
        {
            _configService = configService;
            _encryptionService = encryptionService;
            _awsService = awsService;
            
            // Initialize the timer but don't start it yet
            _processTimer = new Timer(ProcessPendingChanges, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Starts watching for file system changes in the configured directories
        /// </summary>
        public async Task StartWatchingAsync()
        {
            await _processSemaphore.WaitAsync();
            
            try
            {
                if (_isEnabled)
                {
                    return; // Already watching
                }
                
                // Clear any existing watchers
                StopWatchingInternal();
                
                var config = await _configService.GetConfigurationAsync();
                
                if (!config.EnableAutoBackup)
                {
                    return; // Auto backup is disabled
                }

                // Create a watcher for each configured directory
                foreach (var directory in config.Directories)
                {
                    if (!Directory.Exists(directory.Path))
                    {
                        continue;
                    }
                    
                    var watcher = new FileSystemWatcher(directory.Path)
                    {
                        IncludeSubdirectories = directory.IncludeSubdirectories,
                        EnableRaisingEvents = true,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
                    };
                    
                    // Add event handlers
                    watcher.Created += OnFileChanged;
                    watcher.Changed += OnFileChanged;
                    watcher.Renamed += OnFileRenamed;
                    
                    _watchers.Add(watcher);
                }
                
                _isEnabled = true;
            }
            finally
            {
                _processSemaphore.Release();
            }
        }

        /// <summary>
        /// Stops watching for file system changes
        /// </summary>
        public async Task StopWatchingAsync()
        {
            await _processSemaphore.WaitAsync();
            
            try
            {
                StopWatchingInternal();
                _isEnabled = false;
            }
            finally
            {
                _processSemaphore.Release();
            }
        }

        /// <summary>
        /// Event handler for file changed events
        /// </summary>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Ignore directories
            if (Directory.Exists(e.FullPath))
            {
                return;
            }
            
            // Ignore certain file types (like temporary files)
            if (e.Name.EndsWith(".tmp") || e.Name.StartsWith("~"))
            {
                return;
            }
            
            // Add to pending changes
            _pendingChanges[e.FullPath] = DateTime.Now;
            
            // Reset the timer to process changes after a delay
            _processTimer.Change(PROCESS_DELAY_MS, Timeout.Infinite);
        }

        /// <summary>
        /// Event handler for file renamed events
        /// </summary>
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            OnFileChanged(sender, e);
        }

        /// <summary>
        /// Processes pending file changes
        /// </summary>
        private async void ProcessPendingChanges(object state)
        {
            if (!await _processSemaphore.WaitAsync(0))
            {
                return; // Another process is already running
            }
            
            try
            {
                // Get configuration
                var config = await _configService.GetConfigurationAsync();
                
                // Get all pending changes
                var changes = _pendingChanges.ToArray();
                
                // Clear the pending changes
                _pendingChanges.Clear();
                
                foreach (var change in changes)
                {
                    string filePath = change.Key;
                    
                    try
                    {
                        // Skip files that don't match the extension filter
                        string extension = Path.GetExtension(filePath).ToLowerInvariant();
                        if (config.IncludedExtensions.Count > 0 && !config.IncludedExtensions.Contains(extension))
                        {
                            continue;
                        }
                        
                        // Skip files that are currently in use
                        if (IsFileLocked(filePath))
                        {
                            // Re-add to pending changes for later processing
                            _pendingChanges[filePath] = DateTime.Now;
                            continue;
                        }
                        
                        // Notify file backup started
                        OnFileBackupStarted(new FileBackupEventArgs
                        {
                            FilePath = filePath,
                            FileName = Path.GetFileName(filePath)
                        });
                        
                        // Create a temporary file for the encrypted content
                        string tempEncryptedPath = Path.Combine(
                            Path.GetTempPath(),
                            $"SecureBackup_{Guid.NewGuid()}.enc");
                        
                        // Encrypt the file
                        var encryptionResult = await _encryptionService.EncryptFileAsync(filePath, tempEncryptedPath);
                        
                        // Upload to S3
                        string s3Key = await _awsService.UploadFileToS3Async(tempEncryptedPath, encryptionResult.KeyId);
                        
                        // Delete the temporary encrypted file
                        File.Delete(tempEncryptedPath);
                        
                        // Update last backup time
                        config.LastBackupTime = DateTime.Now;
                        await _configService.UpdateConfigurationAsync(config);
                        
                        // Notify file backup completed
                        OnFileBackupCompleted(new FileBackupEventArgs
                        {
                            FilePath = filePath,
                            FileName = Path.GetFileName(filePath),
                            S3Key = s3Key
                        });
                    }
                    catch (Exception ex)
                    {
                        // Notify file backup failed
                        OnFileBackupFailed(new FileBackupEventArgs
                        {
                            FilePath = filePath,
                            FileName = Path.GetFileName(filePath),
                            ErrorMessage = ex.Message
                        });
                    }
                }
            }
            finally
            {
                _processSemaphore.Release();
                
                // If there are still pending changes, restart the timer
                if (_pendingChanges.Count > 0)
                {
                    _processTimer.Change(PROCESS_DELAY_MS, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// Checks if a file is currently locked by another process
        /// </summary>
        private bool IsFileLocked(string filePath)
        {
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return false; // File is not locked
                }
            }
            catch (IOException)
            {
                return true; // File is locked
            }
            catch (Exception)
            {
                return true; // Other error, assume locked
            }
        }

        /// <summary>
        /// Internal method to stop watching without acquiring the semaphore
        /// </summary>
        private void StopWatchingInternal()
        {
            // Stop the timer
            _processTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Dispose of all watchers
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Created -= OnFileChanged;
                watcher.Changed -= OnFileChanged;
                watcher.Renamed -= OnFileRenamed;
                watcher.Dispose();
            }
            
            _watchers.Clear();
        }

        protected virtual void OnFileBackupStarted(FileBackupEventArgs e)
        {
            FileBackupStarted?.Invoke(this, e);
        }

        protected virtual void OnFileBackupCompleted(FileBackupEventArgs e)
        {
            FileBackupCompleted?.Invoke(this, e);
        }

        protected virtual void OnFileBackupFailed(FileBackupEventArgs e)
        {
            FileBackupFailed?.Invoke(this, e);
        }
    }

    public class FileBackupEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string S3Key { get; set; }
        public string ErrorMessage { get; set; }
    }
}
