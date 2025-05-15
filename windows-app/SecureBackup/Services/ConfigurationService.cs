using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using SecureBackup.Models;

namespace SecureBackup.Services
{
    public class ConfigurationService
    {
        private readonly string _dbPath;
        private readonly object _lockObject = new object();
        private BackupConfig _cachedConfig;

        public ConfigurationService()
        {
            // Set up database location
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SecureBackup");
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }
            
            _dbPath = Path.Combine(appDataFolder, "backup_config.db");
            
            // Initialize database if it doesn't exist
            InitializeDatabase();
        }

        /// <summary>
        /// Initializes the SQLite database if it doesn't exist
        /// </summary>
        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    
                    // Create configurations table
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS configurations (
                                id INTEGER PRIMARY KEY,
                                config_json TEXT NOT NULL
                            )";
                        command.ExecuteNonQuery();
                    }
                    
                    // Insert default configuration
                    var defaultConfig = new BackupConfig
                    {
                        EnableAutoBackup = false,
                        Directories = new List<DirectoryConfig>(),
                        IncludedExtensions = new List<string>(),
                        LastBackupTime = DateTime.MinValue
                    };
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            INSERT INTO configurations (id, config_json)
                            VALUES (@id, @config_json)";
                        
                        command.Parameters.AddWithValue("@id", 1);
                        command.Parameters.AddWithValue("@config_json", JsonConvert.SerializeObject(defaultConfig));
                        
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the backup configuration
        /// </summary>
        /// <returns>The current backup configuration</returns>
        public async Task<BackupConfig> GetConfigurationAsync()
        {
            // Return cached config if available
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }
            
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                    {
                        connection.Open();
                        
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT config_json FROM configurations WHERE id = 1";
                            
                            var configJson = command.ExecuteScalar() as string;
                            if (string.IsNullOrEmpty(configJson))
                            {
                                // This shouldn't happen normally, but just in case
                                _cachedConfig = new BackupConfig
                                {
                                    EnableAutoBackup = false,
                                    Directories = new List<DirectoryConfig>(),
                                    IncludedExtensions = new List<string>(),
                                    LastBackupTime = DateTime.MinValue
                                };
                            }
                            else
                            {
                                _cachedConfig = JsonConvert.DeserializeObject<BackupConfig>(configJson);
                            }
                            
                            return _cachedConfig;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Updates the backup configuration
        /// </summary>
        /// <param name="config">The new configuration to save</param>
        public async Task UpdateConfigurationAsync(BackupConfig config)
        {
            // Update cache
            _cachedConfig = config;
            
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                    {
                        connection.Open();
                        
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                                UPDATE configurations
                                SET config_json = @config_json
                                WHERE id = 1";
                            
                            command.Parameters.AddWithValue("@config_json", JsonConvert.SerializeObject(config));
                            
                            command.ExecuteNonQuery();
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Adds a directory to the backup configuration
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <param name="includeSubdirectories">Whether to include subdirectories</param>
        public async Task AddDirectoryAsync(string directoryPath, bool includeSubdirectories)
        {
            var config = await GetConfigurationAsync();
            
            // Check if directory already exists
            if (config.Directories.Exists(d => d.Path.Equals(directoryPath, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            
            // Add new directory
            config.Directories.Add(new DirectoryConfig
            {
                Path = directoryPath,
                IncludeSubdirectories = includeSubdirectories
            });
            
            // Update configuration
            await UpdateConfigurationAsync(config);
        }

        /// <summary>
        /// Removes a directory from the backup configuration
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        public async Task RemoveDirectoryAsync(string directoryPath)
        {
            var config = await GetConfigurationAsync();
            
            // Remove directory
            config.Directories.RemoveAll(d => d.Path.Equals(directoryPath, StringComparison.OrdinalIgnoreCase));
            
            // Update configuration
            await UpdateConfigurationAsync(config);
        }

        /// <summary>
        /// Adds a file extension to include in backups
        /// </summary>
        /// <param name="extension">File extension (with dot, e.g. ".txt")</param>
        public async Task AddExtensionAsync(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return;
            }
            
            // Ensure extension starts with dot
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }
            
            // Convert to lowercase
            extension = extension.ToLowerInvariant();
            
            var config = await GetConfigurationAsync();
            
            // Check if extension already exists
            if (config.IncludedExtensions.Contains(extension))
            {
                return;
            }
            
            // Add extension
            config.IncludedExtensions.Add(extension);
            
            // Update configuration
            await UpdateConfigurationAsync(config);
        }

        /// <summary>
        /// Removes a file extension from the backup configuration
        /// </summary>
        /// <param name="extension">File extension (with dot, e.g. ".txt")</param>
        public async Task RemoveExtensionAsync(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return;
            }
            
            // Ensure extension starts with dot
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }
            
            // Convert to lowercase
            extension = extension.ToLowerInvariant();
            
            var config = await GetConfigurationAsync();
            
            // Remove extension
            config.IncludedExtensions.Remove(extension);
            
            // Update configuration
            await UpdateConfigurationAsync(config);
        }

        /// <summary>
        /// Sets the auto-backup flag in the configuration
        /// </summary>
        /// <param name="enable">Whether to enable auto-backup</param>
        public async Task SetAutoBackupAsync(bool enable)
        {
            var config = await GetConfigurationAsync();
            config.EnableAutoBackup = enable;
            await UpdateConfigurationAsync(config);
        }
    }
}
