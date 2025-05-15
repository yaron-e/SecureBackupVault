const { FileBackup } = require('../models/schema');
const awsService = require('../services/awsService');
const decryptionService = require('../services/decryptionService');
const fs = require('fs');
const path = require('path');
const os = require('os');

// Get all files for the user
exports.getFiles = async (req, res, next) => {
  try {
    const userId = req.user.id;
    
    // Get files from database
    const files = await FileBackup.findByUserId(userId);
    
    // Format response
    const formattedFiles = files.map(file => ({
      id: file.id,
      fileName: file.file_name,
      s3Key: file.s3_key,
      keyId: file.key_id,
      size: file.size,
      lastModified: file.last_modified,
      createdAt: file.created_at
    }));
    
    res.status(200).json(formattedFiles);
  } catch (error) {
    next(error);
  }
};

// Download and decrypt a file
exports.downloadFile = async (req, res, next) => {
  try {
    const userId = req.user.id;
    const s3Key = req.params.s3Key;
    const keyId = req.query.keyId;
    
    // Verify file belongs to user
    const file = await FileBackup.findByS3Key(s3Key);
    
    if (!file) {
      return res.status(404).json({ message: 'File not found' });
    }
    
    if (file.user_id !== userId) {
      return res.status(403).json({ message: 'You do not have permission to access this file' });
    }
    
    // Create temp directories
    const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'secure-backup-'));
    const encryptedFilePath = path.join(tempDir, 'encrypted-file');
    const decryptedFilePath = path.join(tempDir, file.file_name);
    
    // Download encrypted file from S3
    await awsService.downloadFile(s3Key, encryptedFilePath);
    
    // Decrypt the file
    await decryptionService.decryptFile(encryptedFilePath, decryptedFilePath, keyId);
    
    // Set response headers
    res.setHeader('Content-Disposition', `attachment; filename="${file.file_name}"`);
    res.setHeader('Content-Type', 'application/octet-stream');
    
    // Send the file
    const fileStream = fs.createReadStream(decryptedFilePath);
    fileStream.pipe(res);
    
    // Clean up temp files after sending
    fileStream.on('close', () => {
      fs.unlinkSync(encryptedFilePath);
      fs.unlinkSync(decryptedFilePath);
      fs.rmdirSync(tempDir);
    });
    
    fileStream.on('error', (err) => {
      console.error('Error sending file:', err);
      // Attempt cleanup even if there's an error
      try {
        if (fs.existsSync(encryptedFilePath)) fs.unlinkSync(encryptedFilePath);
        if (fs.existsSync(decryptedFilePath)) fs.unlinkSync(decryptedFilePath);
        if (fs.existsSync(tempDir)) fs.rmdirSync(tempDir);
      } catch (cleanupErr) {
        console.error('Error during cleanup:', cleanupErr);
      }
    });
  } catch (error) {
    next(error);
  }
};

// Delete a file
exports.deleteFile = async (req, res, next) => {
  try {
    const userId = req.user.id;
    const s3Key = req.params.s3Key;
    
    // Verify file belongs to user
    const file = await FileBackup.findByS3Key(s3Key);
    
    if (!file) {
      return res.status(404).json({ message: 'File not found' });
    }
    
    if (file.user_id !== userId) {
      return res.status(403).json({ message: 'You do not have permission to delete this file' });
    }
    
    // Delete from S3
    await awsService.deleteFile(s3Key);
    
    // Delete from database
    await FileBackup.delete(s3Key, userId);
    
    // Delete encryption keys
    await awsService.deleteEncryptionKeys(file.key_id);
    
    res.status(200).json({ message: 'File deleted successfully' });
  } catch (error) {
    next(error);
  }
};

// Get user statistics
exports.getUserStats = async (req, res, next) => {
  try {
    const userId = req.user.id;
    
    // Get stats from database
    const stats = await FileBackup.getUserStats(userId);
    
    // Format size
    let formattedSize = '0 B';
    
    if (stats.total_size > 0) {
      const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
      const i = Math.floor(Math.log(stats.total_size) / Math.log(1024));
      formattedSize = parseFloat((stats.total_size / Math.pow(1024, i)).toFixed(2)) + ' ' + sizes[i];
    }
    
    res.status(200).json({
      totalFiles: parseInt(stats.total_files),
      totalSize: formattedSize,
      lastBackup: stats.last_backup
    });
  } catch (error) {
    next(error);
  }
};
