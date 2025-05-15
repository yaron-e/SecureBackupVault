using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Prism.Commands;
using Prism.Mvvm;
using SecureBackup.Models;
using SecureBackup.Services;

namespace SecureBackup.ViewModels
{
    public class FileSelectionViewModel : BindableBase
    {
        private readonly FileScanner _fileScanner;
        private readonly ConfigurationService _configService;

        private ObservableCollection<DirectoryConfig> _directories = new ObservableCollection<DirectoryConfig>();
        private ObservableCollection<string> _fileExtensions = new ObservableCollection<string>();
        private DirectoryConfig _selectedDirectory;
        private string _selectedExtension;
        private string _newExtension;
        private ObservableCollection<FileModel> _scannedFiles = new ObservableCollection<FileModel>();
        private bool _isScanning;
        private string _scanStatus;

        public FileSelectionViewModel(FileScanner fileScanner, ConfigurationService configService)
        {
            _fileScanner = fileScanner;
            _configService = configService;

            // Initialize commands
            AddDirectoryCommand = new DelegateCommand(AddDirectory);
            RemoveDirectoryCommand = new DelegateCommand(RemoveDirectory, CanRemoveDirectory);
            AddExtensionCommand = new DelegateCommand(AddExtension, CanAddExtension);
            RemoveExtensionCommand = new DelegateCommand(RemoveExtension, CanRemoveExtension);
            ScanFilesCommand = new DelegateCommand(ScanFiles, CanScanFiles);

            // Load data
            LoadConfigurationAsync();
        }

        public ObservableCollection<DirectoryConfig> Directories
        {
            get => _directories;
            set => SetProperty(ref _directories, value);
        }

        public ObservableCollection<string> FileExtensions
        {
            get => _fileExtensions;
            set => SetProperty(ref _fileExtensions, value);
        }

        public DirectoryConfig SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                SetProperty(ref _selectedDirectory, value);
                RemoveDirectoryCommand.RaiseCanExecuteChanged();
            }
        }

        public string SelectedExtension
        {
            get => _selectedExtension;
            set
            {
                SetProperty(ref _selectedExtension, value);
                RemoveExtensionCommand.RaiseCanExecuteChanged();
            }
        }

        public string NewExtension
        {
            get => _newExtension;
            set
            {
                SetProperty(ref _newExtension, value);
                AddExtensionCommand.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<FileModel> ScannedFiles
        {
            get => _scannedFiles;
            set => SetProperty(ref _scannedFiles, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                SetProperty(ref _isScanning, value);
                ScanFilesCommand.RaiseCanExecuteChanged();
            }
        }

        public string ScanStatus
        {
            get => _scanStatus;
            set => SetProperty(ref _scanStatus, value);
        }

        // Commands
        public DelegateCommand AddDirectoryCommand { get; }
        public DelegateCommand RemoveDirectoryCommand { get; }
        public DelegateCommand AddExtensionCommand { get; }
        public DelegateCommand RemoveExtensionCommand { get; }
        public DelegateCommand ScanFilesCommand { get; }

        private async void LoadConfigurationAsync()
        {
            try
            {
                var config = await _configService.GetConfigurationAsync();

                // Load directories
                Directories.Clear();
                foreach (var directory in config.Directories)
                {
                    Directories.Add(directory);
                }

                // Load extensions
                FileExtensions.Clear();
                foreach (var extension in config.IncludedExtensions)
                {
                    FileExtensions.Add(extension);
                }

                // Update scan status
                UpdateScanStatus();
            }
            catch (Exception ex)
            {
                ScanStatus = $"Error loading configuration: {ex.Message}";
            }
        }

        private void AddDirectory()
        {
            // Create folder browser dialog
            var dialog = new FolderBrowserDialog
            {
                Description = "Select a folder to back up",
                ShowNewFolderButton = true
            };

            // Show dialog
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = dialog.SelectedPath;

                // Add to configuration
                AddDirectoryAsync(selectedPath, true);
            }
        }

        private async void AddDirectoryAsync(string path, bool includeSubdirectories)
        {
            try
            {
                // Add to configuration
                await _configService.AddDirectoryAsync(path, includeSubdirectories);

                // Reload configuration
                LoadConfigurationAsync();
            }
            catch (Exception ex)
            {
                ScanStatus = $"Error adding directory: {ex.Message}";
            }
        }

        private async void RemoveDirectory()
        {
            if (SelectedDirectory == null)
            {
                return;
            }

            try
            {
                // Remove from configuration
                await _configService.RemoveDirectoryAsync(SelectedDirectory.Path);

                // Reload configuration
                LoadConfigurationAsync();
            }
            catch (Exception ex)
            {
                ScanStatus = $"Error removing directory: {ex.Message}";
            }
        }

        private bool CanRemoveDirectory()
        {
            return SelectedDirectory != null;
        }

        private async void AddExtension()
        {
            if (string.IsNullOrWhiteSpace(NewExtension))
            {
                return;
            }

            try
            {
                // Add to configuration
                await _configService.AddExtensionAsync(NewExtension);

                // Clear input
                NewExtension = string.Empty;

                // Reload configuration
                LoadConfigurationAsync();
            }
            catch (Exception ex)
            {
                ScanStatus = $"Error adding extension: {ex.Message}";
            }
        }

        private bool CanAddExtension()
        {
            return !string.IsNullOrWhiteSpace(NewExtension);
        }

        private async void RemoveExtension()
        {
            if (SelectedExtension == null)
            {
                return;
            }

            try
            {
                // Remove from configuration
                await _configService.RemoveExtensionAsync(SelectedExtension);

                // Reload configuration
                LoadConfigurationAsync();
            }
            catch (Exception ex)
            {
                ScanStatus = $"Error removing extension: {ex.Message}";
            }
        }

        private bool CanRemoveExtension()
        {
            return SelectedExtension != null;
        }

        private async void ScanFiles()
        {
            if (IsScanning)
            {
                return;
            }

            try
            {
                // Set scanning flag
                IsScanning = true;
                ScanStatus = "Scanning files...";
                ScannedFiles.Clear();

                // Scan files
                var files = await _fileScanner.ScanForFilesAsync();

                // Update UI
                ScannedFiles.Clear();
                foreach (var file in files)
                {
                    ScannedFiles.Add(file);
                }

                // Update scan status
                UpdateScanStatus();
            }
            catch (Exception ex)
            {
                ScanStatus = $"Error scanning files: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        private bool CanScanFiles()
        {
            return !IsScanning;
        }

        private void UpdateScanStatus()
        {
            if (ScannedFiles.Count > 0)
            {
                ScanStatus = $"Found {ScannedFiles.Count} files for backup.";
            }
            else if (Directories.Count == 0)
            {
                ScanStatus = "Add directories to scan for files.";
            }
            else
            {
                ScanStatus = "Click 'Scan Files' to see what will be backed up.";
            }
        }
    }
}
