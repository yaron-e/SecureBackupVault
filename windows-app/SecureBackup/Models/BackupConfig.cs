using System;
using System.Collections.Generic;

namespace SecureBackup.Models
{
    /// <summary>
    /// Represents the backup configuration stored in the database
    /// </summary>
    public class BackupConfig
    {
        /// <summary>
        /// Whether automatic backup is enabled for file changes
        /// </summary>
        public bool EnableAutoBackup { get; set; }
        
        /// <summary>
        /// List of directories to include in the backup
        /// </summary>
        public List<DirectoryConfig> Directories { get; set; } = new List<DirectoryConfig>();
        
        /// <summary>
        /// List of file extensions to include (e.g. ".txt", ".doc")
        /// If empty, all files will be included
        /// </summary>
        public List<string> IncludedExtensions { get; set; } = new List<string>();
        
        /// <summary>
        /// The timestamp of the last successful backup
        /// </summary>
        public DateTime LastBackupTime { get; set; }
    }
    
    /// <summary>
    /// Configuration for a directory to back up
    /// </summary>
    public class DirectoryConfig
    {
        /// <summary>
        /// Path to the directory
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Whether to include subdirectories in the backup
        /// </summary>
        public bool IncludeSubdirectories { get; set; }
    }
}
