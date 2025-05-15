using System;

namespace SecureBackup.Models
{
    /// <summary>
    /// Represents a file in the backup system
    /// </summary>
    public class FileModel
    {
        /// <summary>
        /// Full path to the file
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// File name with extension
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// File extension (with dot, e.g. ".txt")
        /// </summary>
        public string Extension { get; set; }
        
        /// <summary>
        /// File size in bytes
        /// </summary>
        public long Size { get; set; }
        
        /// <summary>
        /// Last modified timestamp
        /// </summary>
        public DateTime LastModified { get; set; }
        
        /// <summary>
        /// S3 key where the file is stored (if backed up)
        /// </summary>
        public string S3Key { get; set; }
        
        /// <summary>
        /// KMS key ID for decryption (if backed up)
        /// </summary>
        public string KeyId { get; set; }
        
        /// <summary>
        /// Formatted file size as a human-readable string
        /// </summary>
        public string FormattedSize
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = Size;
                int order = 0;
                
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                
                return $"{len:0.##} {sizes[order]}";
            }
        }
        
        /// <summary>
        /// Formatted last modified date
        /// </summary>
        public string FormattedLastModified => LastModified.ToString("g");
        
        /// <summary>
        /// Returns true if the file has been backed up
        /// </summary>
        public bool IsBackedUp => !string.IsNullOrEmpty(S3Key);
    }
}
