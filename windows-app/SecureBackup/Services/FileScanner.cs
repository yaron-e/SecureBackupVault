using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SecureBackup.Models;

namespace SecureBackup.Services
{
    public class FileScanner
    {
        private readonly ConfigurationService _configService;

        public FileScanner(ConfigurationService configService)
        {
            _configService = configService;
        }

        /// <summary>
        /// Scans directories recursively to find files matching configured criteria
        /// </summary>
        /// <returns>A list of file models that match the backup criteria</returns>
        public async Task<List<FileModel>> ScanForFilesAsync()
        {
            var config = await _configService.GetConfigurationAsync();
            var result = new List<FileModel>();

            // Get configured directories to scan
            foreach (var directory in config.Directories)
            {
                if (!Directory.Exists(directory.Path))
                {
                    continue;
                }

                try
                {
                    // Scan the directory for files
                    var files = ScanDirectoryRecursive(directory.Path, directory.IncludeSubdirectories, config.IncludedExtensions);
                    result.AddRange(files);
                }
                catch (Exception ex)
                {
                    // Log error or handle exception
                    Console.WriteLine($"Error scanning directory {directory.Path}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Recursively scans a directory for files matching the specified extensions
        /// </summary>
        private List<FileModel> ScanDirectoryRecursive(string directoryPath, bool includeSubdirectories, List<string> includedExtensions)
        {
            var result = new List<FileModel>();

            try
            {
                // Get all files in the current directory
                var files = Directory.GetFiles(directoryPath);
                foreach (var file in files)
                {
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    
                    // Skip files that don't match any included extensions
                    if (includedExtensions.Count > 0 && !includedExtensions.Contains(extension))
                    {
                        continue;
                    }

                    var fileInfo = new FileInfo(file);
                    result.Add(new FileModel
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        Extension = extension,
                        Size = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime
                    });
                }

                // Recursively scan subdirectories if configured
                if (includeSubdirectories)
                {
                    var subdirectories = Directory.GetDirectories(directoryPath);
                    foreach (var subdirectory in subdirectories)
                    {
                        var subdirectoryFiles = ScanDirectoryRecursive(subdirectory, true, includedExtensions);
                        result.AddRange(subdirectoryFiles);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error or handle exception
                Console.WriteLine($"Error processing directory {directoryPath}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets the files that have been modified since the last backup
        /// </summary>
        public async Task<List<FileModel>> GetModifiedFilesSinceLastBackupAsync()
        {
            var config = await _configService.GetConfigurationAsync();
            var allFiles = await ScanForFilesAsync();
            
            // Filter files that have been modified since the last backup
            return allFiles.Where(f => f.LastModified > config.LastBackupTime).ToList();
        }

        /// <summary>
        /// Gets information about a specific file
        /// </summary>
        public FileModel GetFileInfo(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            return new FileModel
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Extension = Path.GetExtension(filePath).ToLowerInvariant(),
                Size = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime
            };
        }
    }
}
