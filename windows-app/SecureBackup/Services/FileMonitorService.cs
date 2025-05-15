using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SecureBackup.Services
{
    /// <summary>
    /// Provides file monitoring services for detecting file changes and triggering backups
    /// </summary>
    public class FileMonitorService
    {
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private readonly HashSet<string> _pathsToWatch = new HashSet<string>();
        private readonly HashSet<string> _fileExtensionsToWatch = new HashSet<string>();
        private readonly Dictionary<string, DateTime> _lastModifiedTimes = new Dictionary<string, DateTime>();
        private readonly SemaphoreSlim _backupSemaphore = new SemaphoreSlim(1, 1);
        
        // Event to notify when a file needs to be backed up
        public event EventHandler<FileBackupEventArgs> FileBackupNeeded;
        
        /// <summary>
        /// Adds a directory to be monitored for file changes
        /// </summary>
        /// <param name="path">Path to the directory to monitor</param>
        /// <param name="includeSubdirectories">Whether to include subdirectories</param>
        public void AddPathToWatch(string path, bool includeSubdirectories = true)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }
            
            _pathsToWatch.Add(path);
            
            // Create and configure a FileSystemWatcher
            var watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = includeSubdirectories
            };
            
            // Add event handlers
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            
            // Store the watcher
            _watchers.Add(watcher);
        }
        
        /// <summary>
        /// Adds file extensions to be monitored (e.g., ".docx", ".pdf")
        /// </summary>
        /// <param name="extensions">Array of file extensions to monitor</param>
        public void AddFileExtensionsToWatch(params string[] extensions)
        {
            foreach (var extension in extensions)
            {
                var ext = extension.StartsWith(".") ? extension : $".{extension}";
                _fileExtensionsToWatch.Add(ext.ToLowerInvariant());
            }
        }
        
        /// <summary>
        /// Starts monitoring files for changes
        /// </summary>
        public void StartMonitoring()
        {
            // If no file extensions specified, watch all files
            if (_fileExtensionsToWatch.Count == 0)
            {
                foreach (var watcher in _watchers)
                {
                    watcher.Filter = "*.*";
                    watcher.EnableRaisingEvents = true;
                }
            }
            else
            {
                // For each watcher, set up filters for each extension
                foreach (var watcher in _watchers)
                {
                    // FileSystemWatcher doesn't support multiple filters, so we need to create multiple watchers
                    var basePath = watcher.Path;
                    var includeSubdirectories = watcher.IncludeSubdirectories;
                    
                    // Disable the original watcher
                    watcher.EnableRaisingEvents = false;
                    
                    // Create new watchers for each extension
                    foreach (var extension in _fileExtensionsToWatch)
                    {
                        var extensionWatcher = new FileSystemWatcher
                        {
                            Path = basePath,
                            Filter = $"*{extension}",
                            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                            IncludeSubdirectories = includeSubdirectories
                        };
                        
                        extensionWatcher.Changed += OnFileChanged;
                        extensionWatcher.Created += OnFileChanged;
                        extensionWatcher.Renamed += OnFileRenamed;
                        extensionWatcher.EnableRaisingEvents = true;
                        
                        _watchers.Add(extensionWatcher);
                    }
                }
            }
            
            // Perform initial scan to get current state
            ScanExistingFiles();
        }
        
        /// <summary>
        /// Stops monitoring files for changes
        /// </summary>
        public void StopMonitoring()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= OnFileChanged;
                watcher.Created -= OnFileChanged;
                watcher.Renamed -= OnFileRenamed;
            }
            
            _watchers.Clear();
        }
        
        /// <summary>
        /// Scans for existing files to establish baseline
        /// </summary>
        private void ScanExistingFiles()
        {
            foreach (var path in _pathsToWatch)
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    if (ShouldWatchFile(file))
                    {
                        _lastModifiedTimes[file] = File.GetLastWriteTime(file);
                    }
                }
            }
        }
        
        /// <summary>
        /// Event handler for file changed events
        /// </summary>
        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed && 
                e.ChangeType != WatcherChangeTypes.Created)
            {
                return;
            }
            
            await ProcessFileChangeAsync(e.FullPath);
        }
        
        /// <summary>
        /// Event handler for file renamed events
        /// </summary>
        private async void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            // Remove old path from tracking
            _lastModifiedTimes.Remove(e.OldFullPath);
            
            // Process new file name
            await ProcessFileChangeAsync(e.FullPath);
        }
        
        /// <summary>
        /// Processes a file change event
        /// </summary>
        private async Task ProcessFileChangeAsync(string filePath)
        {
            // Ignore if it's not a file we should watch
            if (!ShouldWatchFile(filePath))
            {
                return;
            }
            
            // Wait a moment for the file to stabilize
            await Task.Delay(500);
            
            try
            {
                await _backupSemaphore.WaitAsync();
                
                // Check if the file is still there (it might have been deleted)
                if (!File.Exists(filePath))
                {
                    return;
                }
                
                var lastWriteTime = File.GetLastWriteTime(filePath);
                
                // If we've seen this file before, check if it's actually changed
                if (_lastModifiedTimes.TryGetValue(filePath, out var previousWriteTime))
                {
                    if (lastWriteTime <= previousWriteTime)
                    {
                        return;
                    }
                }
                
                // Update our record of the last modified time
                _lastModifiedTimes[filePath] = lastWriteTime;
                
                // Trigger backup
                OnFileBackupNeeded(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file change: {ex.Message}");
            }
            finally
            {
                _backupSemaphore.Release();
            }
        }
        
        /// <summary>
        /// Determines if a file should be watched based on its extension
        /// </summary>
        private bool ShouldWatchFile(string filePath)
        {
            try
            {
                // Skip temporary files and directories
                if (!File.Exists(filePath) || Path.GetFileName(filePath).StartsWith(".") || Path.GetFileName(filePath).StartsWith("~$"))
                {
                    return false;
                }
                
                // If no extensions specified, watch all files
                if (_fileExtensionsToWatch.Count == 0)
                {
                    return true;
                }
                
                // Check if the file extension matches one we're watching
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                return _fileExtensionsToWatch.Contains(extension);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Raises the FileBackupNeeded event
        /// </summary>
        protected virtual void OnFileBackupNeeded(string filePath)
        {
            FileBackupNeeded?.Invoke(this, new FileBackupEventArgs(filePath));
        }
    }
    
    /// <summary>
    /// Event arguments for the FileBackupNeeded event
    /// </summary>
    public class FileBackupEventArgs : EventArgs
    {
        public string FilePath { get; }
        
        public FileBackupEventArgs(string filePath)
        {
            FilePath = filePath;
        }
    }
}