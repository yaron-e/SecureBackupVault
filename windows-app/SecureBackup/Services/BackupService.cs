using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace SecureBackup.Services
{
    /// <summary>
    /// Coordinates the backup process using encryption and AWS services
    /// </summary>
    public class BackupService
    {
        private readonly EncryptionService _encryptionService;
        private readonly AwsService _awsService;
        private readonly FileMonitorService _fileMonitorService;
        private readonly string _userId;
        private readonly string _tempDirectory;
        private readonly SemaphoreSlim _backupSemaphore = new SemaphoreSlim(3, 3); // Allow 3 concurrent backups
        
        // Event to notify when a backup is completed
        public event EventHandler<BackupCompletedEventArgs> BackupCompleted;
        
        // Event to notify when a backup fails
        public event EventHandler<BackupFailedEventArgs> BackupFailed;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public BackupService(
            EncryptionService encryptionService,
            AwsService awsService,
            FileMonitorService fileMonitorService,
            string userId,
            string tempDirectory = null)
        {
            _encryptionService = encryptionService;
            _awsService = awsService;
            _fileMonitorService = fileMonitorService;
            _userId = userId;
            _tempDirectory = tempDirectory ?? Path.Combine(Path.GetTempPath(), "SecureBackup");
            
            // Create temp directory if it doesn't exist
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
            
            // Subscribe to file monitor events
            _fileMonitorService.FileBackupNeeded += OnFileBackupNeeded;
        }
        
        /// <summary>
        /// Handles the FileBackupNeeded event
        /// </summary>
        private async void OnFileBackupNeeded(object sender, FileBackupEventArgs e)
        {
            await BackupFileAsync(e.FilePath);
        }
        
        /// <summary>
        /// Performs a backup of a file
        /// </summary>
        /// <param name="filePath">Path to the file to backup</param>
        /// <returns>Information about the backup</returns>
        public async Task<BackupResult> BackupFileAsync(string filePath)
        {
            try
            {
                await _backupSemaphore.WaitAsync();
                
                // Create a unique temp file path
                var tempEncryptedPath = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}.enc");
                
                try
                {
                    // Step 1: Encrypt the file
                    var keys = await _encryptionService.EncryptFileAsync(filePath, tempEncryptedPath);
                    
                    // Step 2: Upload the encrypted file to S3
                    var s3Key = await _awsService.UploadFileAsync(tempEncryptedPath, _userId);
                    
                    // Step 3: Store the encryption keys in KMS
                    var keyId = await _awsService.StoreEncryptionKeysAsync(keys);
                    
                    // Create backup result
                    var result = new BackupResult
                    {
                        OriginalFilePath = filePath,
                        S3Key = s3Key,
                        KeyId = keyId,
                        BackupDate = DateTime.UtcNow,
                        FileName = Path.GetFileName(filePath),
                        FileSize = new FileInfo(filePath).Length
                    };
                    
                    // Notify listeners that the backup was completed
                    OnBackupCompleted(result);
                    
                    return result;
                }
                finally
                {
                    // Clean up temp file
                    if (File.Exists(tempEncryptedPath))
                    {
                        File.Delete(tempEncryptedPath);
                    }
                }
            }
            catch (Exception ex)
            {
                var failedArgs = new BackupFailedEventArgs(filePath, ex);
                OnBackupFailed(failedArgs);
                throw;
            }
            finally
            {
                _backupSemaphore.Release();
            }
        }
        
        /// <summary>
        /// Restores a backed up file
        /// </summary>
        /// <param name="s3Key">S3 object key of the file</param>
        /// <param name="keyId">ID of the encryption keys</param>
        /// <param name="outputPath">Path where the file should be restored</param>
        /// <returns>True if successful</returns>
        public async Task<bool> RestoreFileAsync(string s3Key, string keyId, string outputPath)
        {
            var tempEncryptedPath = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}.enc");
            
            try
            {
                // Step 1: Download the encrypted file from S3
                await _awsService.DownloadFileAsync(s3Key, tempEncryptedPath);
                
                // Step 2: Retrieve the encryption keys from KMS
                var keys = await _awsService.RetrieveEncryptionKeysAsync(keyId);
                
                // Step 3: Decrypt the file
                await _encryptionService.DecryptFileAsync(tempEncryptedPath, outputPath, keys);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring file: {ex.Message}");
                return false;
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempEncryptedPath))
                {
                    File.Delete(tempEncryptedPath);
                }
            }
        }
        
        /// <summary>
        /// Deletes a backed up file
        /// </summary>
        /// <param name="s3Key">S3 object key of the file</param>
        /// <param name="keyId">ID of the encryption keys</param>
        /// <returns>True if successful</returns>
        public async Task<bool> DeleteBackupAsync(string s3Key, string keyId)
        {
            try
            {
                await _awsService.DeleteFileAsync(s3Key, keyId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting backup: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts monitoring files for changes
        /// </summary>
        public void StartMonitoring()
        {
            _fileMonitorService.StartMonitoring();
        }
        
        /// <summary>
        /// Stops monitoring files for changes
        /// </summary>
        public void StopMonitoring()
        {
            _fileMonitorService.StopMonitoring();
        }
        
        /// <summary>
        /// Raises the BackupCompleted event
        /// </summary>
        protected virtual void OnBackupCompleted(BackupResult result)
        {
            BackupCompleted?.Invoke(this, new BackupCompletedEventArgs(result));
        }
        
        /// <summary>
        /// Raises the BackupFailed event
        /// </summary>
        protected virtual void OnBackupFailed(BackupFailedEventArgs args)
        {
            BackupFailed?.Invoke(this, args);
        }
        
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _fileMonitorService.FileBackupNeeded -= OnFileBackupNeeded;
            StopMonitoring();
        }
    }
    
    /// <summary>
    /// Result of a backup operation
    /// </summary>
    public class BackupResult
    {
        public string OriginalFilePath { get; set; }
        public string S3Key { get; set; }
        public string KeyId { get; set; }
        public DateTime BackupDate { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
    }
    
    /// <summary>
    /// Event arguments for the BackupCompleted event
    /// </summary>
    public class BackupCompletedEventArgs : EventArgs
    {
        public BackupResult Result { get; }
        
        public BackupCompletedEventArgs(BackupResult result)
        {
            Result = result;
        }
    }
    
    /// <summary>
    /// Event arguments for the BackupFailed event
    /// </summary>
    public class BackupFailedEventArgs : EventArgs
    {
        public string FilePath { get; }
        public Exception Exception { get; }
        
        public BackupFailedEventArgs(string filePath, Exception exception)
        {
            FilePath = filePath;
            Exception = exception;
        }
    }
}