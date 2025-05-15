using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Prism.Commands;
using Prism.Mvvm;
using SecureBackup.Services;

namespace SecureBackup.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private readonly FileScanner _fileScanner;
        private readonly EncryptionService _encryptionService;
        private readonly AwsService _awsService;
        private readonly ConfigurationService _configService;
        private readonly FileSystemWatcherService _fileSystemWatcher;
        
        private object _currentView;
        private string _statusMessage;
        private Brush _statusMessageBrush;
        private string _backedUpFilesCount = "0";
        private string _backedUpTotalSize = "0 B";
        private string _lastBackupTime = "Never";
        private string _connectionStatus = "Disconnected";
        private Brush _connectionStatusColor = Brushes.Red;

        private FileSelectionViewModel _fileSelectionViewModel;
        private ConfigurationViewModel _configurationViewModel;

        public MainViewModel(
            FileScanner fileScanner,
            EncryptionService encryptionService,
            AwsService awsService,
            ConfigurationService configService,
            FileSystemWatcherService fileSystemWatcher,
            FileSelectionViewModel fileSelectionViewModel,
            ConfigurationViewModel configurationViewModel)
        {
            _fileScanner = fileScanner;
            _encryptionService = encryptionService;
            _awsService = awsService;
            _configService = configService;
            _fileSystemWatcher = fileSystemWatcher;
            
            _fileSelectionViewModel = fileSelectionViewModel;
            _configurationViewModel = configurationViewModel;
            
            // Set initial view
            CurrentView = _fileSelectionViewModel;
            
            // Initialize commands
            NavigateToFileSelectionCommand = new DelegateCommand(NavigateToFileSelection);
            NavigateToConfigurationCommand = new DelegateCommand(NavigateToConfiguration);
            ManualBackupCommand = new DelegateCommand(ManualBackup);
            
            // Subscribe to events
            _fileSystemWatcher.FileBackupStarted += FileSystemWatcher_FileBackupStarted;
            _fileSystemWatcher.FileBackupCompleted += FileSystemWatcher_FileBackupCompleted;
            _fileSystemWatcher.FileBackupFailed += FileSystemWatcher_FileBackupFailed;
            
            // Initialize watchers
            InitializeAsync();
        }

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public Brush StatusMessageBrush
        {
            get => _statusMessageBrush;
            set => SetProperty(ref _statusMessageBrush, value);
        }

        public string BackedUpFilesCount
        {
            get => _backedUpFilesCount;
            set => SetProperty(ref _backedUpFilesCount, value);
        }

        public string BackedUpTotalSize
        {
            get => _backedUpTotalSize;
            set => SetProperty(ref _backedUpTotalSize, value);
        }

        public string LastBackupTime
        {
            get => _lastBackupTime;
            set => SetProperty(ref _lastBackupTime, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public Brush ConnectionStatusColor
        {
            get => _connectionStatusColor;
            set => SetProperty(ref _connectionStatusColor, value);
        }

        // Commands
        public DelegateCommand NavigateToFileSelectionCommand { get; }
        public DelegateCommand NavigateToConfigurationCommand { get; }
        public DelegateCommand ManualBackupCommand { get; }

        private void NavigateToFileSelection()
        {
            CurrentView = _fileSelectionViewModel;
        }

        private void NavigateToConfiguration()
        {
            CurrentView = _configurationViewModel;
        }

        private async void ManualBackup()
        {
            try
            {
                SetStatus("Starting manual backup...", Brushes.Blue);
                
                // Get files to back up
                var files = await _fileScanner.ScanForFilesAsync();
                
                if (files.Count == 0)
                {
                    SetStatus("No files found to back up.", Brushes.Orange);
                    return;
                }
                
                SetStatus($"Backing up {files.Count} files...", Brushes.Blue);
                
                // Perform the backup in a background task
                await Task.Run(async () =>
                {
                    int successCount = 0;
                    
                    foreach (var file in files)
                    {
                        try
                        {
                            // Create a temporary file for the encrypted content
                            string tempEncryptedPath = System.IO.Path.Combine(
                                System.IO.Path.GetTempPath(),
                                $"SecureBackup_{Guid.NewGuid()}.enc");
                            
                            // Update status on UI thread
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                SetStatus($"Encrypting: {file.FileName}", Brushes.Blue);
                            });
                            
                            // Encrypt the file
                            var encryptionResult = await _encryptionService.EncryptFileAsync(file.FilePath, tempEncryptedPath);
                            
                            // Update status on UI thread
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                SetStatus($"Uploading: {file.FileName}", Brushes.Blue);
                            });
                            
                            // Upload to S3
                            string s3Key = await _awsService.UploadFileToS3Async(tempEncryptedPath, encryptionResult.KeyId);
                            
                            // Delete the temporary encrypted file
                            System.IO.File.Delete(tempEncryptedPath);
                            
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            // Update status on UI thread
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                SetStatus($"Error backing up {file.FileName}: {ex.Message}", Brushes.Red);
                            });
                            
                            // Short pause to allow user to see the error message
                            await Task.Delay(2000);
                        }
                    }
                    
                    // Update last backup time
                    var config = await _configService.GetConfigurationAsync();
                    config.LastBackupTime = DateTime.Now;
                    await _configService.UpdateConfigurationAsync(config);
                    
                    // Update status on UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SetStatus($"Backup completed: {successCount} of {files.Count} files backed up.", 
                            successCount == files.Count ? Brushes.Green : Brushes.Orange);
                        
                        // Update stats
                        UpdateBackupStats();
                    });
                });
            }
            catch (Exception ex)
            {
                SetStatus($"Backup failed: {ex.Message}", Brushes.Red);
            }
        }

        private async void InitializeAsync()
        {
            try
            {
                // Update connection status
                await CheckAwsConnectionAsync();
                
                // Update backup statistics
                await UpdateBackupStats();
                
                // Start file system watchers
                await _fileSystemWatcher.StartWatchingAsync();
                
                SetStatus("Ready", Brushes.Green);
            }
            catch (Exception ex)
            {
                SetStatus($"Initialization error: {ex.Message}", Brushes.Red);
            }
        }

        private async Task CheckAwsConnectionAsync()
        {
            try
            {
                // Try to list backup files to check the connection
                await _awsService.ListBackupFilesAsync();
                
                ConnectionStatus = "Connected";
                ConnectionStatusColor = Brushes.Green;
            }
            catch (Exception)
            {
                ConnectionStatus = "Disconnected";
                ConnectionStatusColor = Brushes.Red;
            }
        }

        private async Task UpdateBackupStats()
        {
            try
            {
                // Get all backed up files
                var files = await _awsService.ListBackupFilesAsync();
                
                // Update files count
                BackedUpFilesCount = files.Count.ToString();
                
                // Calculate total size
                long totalSize = 0;
                foreach (var file in files)
                {
                    totalSize += file.Size;
                }
                
                // Format size
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = totalSize;
                int order = 0;
                
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                
                BackedUpTotalSize = $"{len:0.##} {sizes[order]}";
                
                // Update last backup time
                var config = await _configService.GetConfigurationAsync();
                if (config.LastBackupTime > DateTime.MinValue)
                {
                    LastBackupTime = config.LastBackupTime.ToString("g");
                }
                else
                {
                    LastBackupTime = "Never";
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error updating stats: {ex.Message}", Brushes.Red);
            }
        }

        private void SetStatus(string message, Brush color)
        {
            StatusMessage = message;
            StatusMessageBrush = color;
        }

        private void FileSystemWatcher_FileBackupStarted(object sender, FileBackupEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SetStatus($"Backing up: {e.FileName}", Brushes.Blue);
            });
        }

        private void FileSystemWatcher_FileBackupCompleted(object sender, FileBackupEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SetStatus($"Backup completed: {e.FileName}", Brushes.Green);
                UpdateBackupStats();
            });
        }

        private void FileSystemWatcher_FileBackupFailed(object sender, FileBackupEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SetStatus($"Backup failed: {e.FileName} - {e.ErrorMessage}", Brushes.Red);
            });
        }
    }
}
