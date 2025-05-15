using System;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using SecureBackup.Services;

namespace SecureBackup.ViewModels
{
    public class ConfigurationViewModel : BindableBase
    {
        private readonly ConfigurationService _configService;
        private readonly FileSystemWatcherService _fileSystemWatcher;

        private bool _enableAutoBackup;
        private string _awsRegion;
        private string _s3Bucket;
        private string _kmsKeyId;
        private string _statusMessage;
        private bool _isSaving;

        public ConfigurationViewModel(
            ConfigurationService configService,
            FileSystemWatcherService fileSystemWatcher)
        {
            _configService = configService;
            _fileSystemWatcher = fileSystemWatcher;

            // Initialize commands
            SaveConfigurationCommand = new DelegateCommand(SaveConfiguration, CanSaveConfiguration);
            TestConnectionCommand = new DelegateCommand(TestConnection);

            // Load data
            LoadConfigurationAsync();
        }

        public bool EnableAutoBackup
        {
            get => _enableAutoBackup;
            set => SetProperty(ref _enableAutoBackup, value);
        }

        public string AwsRegion
        {
            get => _awsRegion;
            set => SetProperty(ref _awsRegion, value);
        }

        public string S3Bucket
        {
            get => _s3Bucket;
            set => SetProperty(ref _s3Bucket, value);
        }

        public string KmsKeyId
        {
            get => _kmsKeyId;
            set => SetProperty(ref _kmsKeyId, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                SetProperty(ref _isSaving, value);
                SaveConfigurationCommand.RaiseCanExecuteChanged();
            }
        }

        // Commands
        public DelegateCommand SaveConfigurationCommand { get; }
        public DelegateCommand TestConnectionCommand { get; }

        private async void LoadConfigurationAsync()
        {
            try
            {
                var config = await _configService.GetConfigurationAsync();
                EnableAutoBackup = config.EnableAutoBackup;

                // Load AWS configuration from app settings
                // In a real app, these would be loaded from a secure store
                // or the Windows registry, rather than from a file
                // This is just a placeholder
                AwsRegion = "us-east-1";
                S3Bucket = "your-backup-bucket";
                KmsKeyId = "your-kms-key-id";

                StatusMessage = "Configuration loaded.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading configuration: {ex.Message}";
            }
        }

        private async void SaveConfiguration()
        {
            if (IsSaving)
            {
                return;
            }

            try
            {
                IsSaving = true;
                StatusMessage = "Saving configuration...";

                // Update auto-backup setting
                await _configService.SetAutoBackupAsync(EnableAutoBackup);

                // Update AWS settings (in a real app, these would be stored securely)
                // This is just a placeholder

                // Restart file system watcher with new settings
                if (EnableAutoBackup)
                {
                    await _fileSystemWatcher.StartWatchingAsync();
                }
                else
                {
                    await _fileSystemWatcher.StopWatchingAsync();
                }

                StatusMessage = "Configuration saved successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving configuration: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        private bool CanSaveConfiguration()
        {
            return !IsSaving;
        }

        private void TestConnection()
        {
            StatusMessage = "Testing connection...";
            // In a real app, we would test the AWS connection here
            // This is just a placeholder
            StatusMessage = "Connection test not implemented in this version.";
        }
    }
}
