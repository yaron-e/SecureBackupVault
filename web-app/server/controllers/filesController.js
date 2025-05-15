const { db } = require('../db');
const { eq } = require('drizzle-orm');
const { fileBackups, users } = require('../../shared/schema');
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
    const files = await db
      .select()
      .from(fileBackups)
      .where(eq(fileBackups.userId, userId))
      .orderBy(fileBackups.lastModified);
    
    // Format response
    const formattedFiles = files.map(file => ({
      id: file.id,
      fileName: file.fileName,
      s3Key: file.s3Key,
      keyId: file.keyId,
      size: file.size,
      lastModified: file.lastModified,
      createdAt: file.createdAt
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
    const s3Key = req.query.s3Key;
    const keyId = req.query.keyId;
    
    if (!s3Key || !keyId) {
      return res.status(400).json({ message: 'S3 key and key ID are required' });
    }
    
    // Verify file belongs to user
    const [file] = await db
      .select()
      .from(fileBackups)
      .where(eq(fileBackups.s3Key, s3Key));
    
    if (!file) {
      return res.status(404).json({ message: 'File not found' });
    }
    
    if (file.userId !== userId) {
      return res.status(403).json({ message: 'You do not have permission to access this file' });
    }
    
    // Create temp directories
    const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'secure-backup-'));
    const encryptedFilePath = path.join(tempDir, 'encrypted-file');
    const decryptedFilePath = path.join(tempDir, file.fileName);
    
    // Download encrypted file from S3
    await awsService.downloadFile(s3Key, encryptedFilePath);
    
    // Decrypt the file
    await decryptionService.decryptFile(encryptedFilePath, decryptedFilePath, keyId);
    
    // Set response headers
    res.setHeader('Content-Disposition', `attachment; filename="${file.fileName}"`);
    res.setHeader('Content-Type', 'application/octet-stream');
    
    // Send the file
    const fileStream = fs.createReadStream(decryptedFilePath);
    fileStream.pipe(res);
    
    // Clean up temp files after sending
    fileStream.on('close', () => {
      try {
        if (fs.existsSync(encryptedFilePath)) fs.unlinkSync(encryptedFilePath);
        if (fs.existsSync(decryptedFilePath)) fs.unlinkSync(decryptedFilePath);
        if (fs.existsSync(tempDir)) fs.rmdirSync(tempDir);
      } catch (cleanupErr) {
        console.error('Error during cleanup:', cleanupErr);
      }
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
    const [file] = await db
      .select()
      .from(fileBackups)
      .where(eq(fileBackups.s3Key, s3Key));
    
    if (!file) {
      return res.status(404).json({ message: 'File not found' });
    }
    
    if (file.userId !== userId) {
      return res.status(403).json({ message: 'You do not have permission to delete this file' });
    }
    
    // Delete from S3
    await awsService.deleteFile(s3Key);
    
    // Delete from database
    await db
      .delete(fileBackups)
      .where(eq(fileBackups.s3Key, s3Key));
    
    // Delete encryption keys
    await awsService.deleteEncryptionKeys(file.keyId);
    
    res.status(200).json({ message: 'File deleted successfully' });
  } catch (error) {
    next(error);
  }
};

// Get user statistics
exports.getUserStats = async (req, res, next) => {
  try {
    const userId = req.user.id;
    
    // Get files for the user to calculate stats
    const files = await db
      .select()
      .from(fileBackups)
      .where(eq(fileBackups.userId, userId));
    
    // Calculate total size
    let totalSize = 0;
    let lastBackup = null;
    
    if (files.length > 0) {
      // Sum up the sizes
      totalSize = files.reduce((sum, file) => sum + Number(file.size || 0), 0);
      
      // Find the latest backup date
      const dates = files.map(file => new Date(file.lastModified));
      const validDates = dates.filter(date => !isNaN(date.getTime()));
      
      if (validDates.length > 0) {
        lastBackup = new Date(Math.max(...validDates));
      }
    }
    
    // Format size
    let formattedSize = '0 B';
    
    if (totalSize > 0) {
      const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
      const i = Math.floor(Math.log(totalSize) / Math.log(1024));
      formattedSize = parseFloat((totalSize / Math.pow(1024, i)).toFixed(2)) + ' ' + sizes[i];
    }
    
    res.status(200).json({
      totalFiles: files.length,
      totalSize: formattedSize,
      lastBackup: lastBackup
    });
  } catch (error) {
    next(error);
  }
};
