package com.securebackup.mobile.data.repository

import android.content.Context
import com.securebackup.mobile.data.local.BackupQueueDao
import com.securebackup.mobile.data.local.MediaFileDao
import com.securebackup.mobile.data.model.*
import com.securebackup.mobile.data.network.ApiService
import com.securebackup.mobile.service.EncryptionService
import com.securebackup.mobile.util.PreferencesManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.asRequestBody
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.File
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class BackupRepository @Inject constructor(
    private val context: Context,
    private val apiService: ApiService,
    private val mediaFileDao: MediaFileDao,
    private val backupQueueDao: BackupQueueDao,
    private val encryptionService: EncryptionService,
    private val preferencesManager: PreferencesManager
) {
    
    fun getAllMediaFiles(): Flow<List<LocalMediaFile>> = mediaFileDao.getAllMediaFiles()
    
    fun getUnbackedUpFiles(): Flow<List<LocalMediaFile>> = mediaFileDao.getUnbackedUpFiles()
    
    fun getBackupQueue(): Flow<List<BackupQueueItem>> = backupQueueDao.getAllQueueItems()
    
    suspend fun addToBackupQueue(mediaFile: LocalMediaFile, priority: Int = 0) {
        val queueItem = BackupQueueItem(
            mediaFileId = mediaFile.id,
            priority = priority,
            status = BackupStatus.PENDING
        )
        backupQueueDao.insertQueueItem(queueItem)
    }
    
    suspend fun processBackupQueue(progressCallback: ((Int) -> Unit)? = null) = withContext(Dispatchers.IO) {
        val token = preferencesManager.getAuthToken() ?: throw Exception("Not authenticated")
        
        while (true) {
            val nextItem = backupQueueDao.getNextPendingItem() ?: break
            
            try {
                // Update status to in progress
                backupQueueDao.updateItemStatus(nextItem.id, BackupStatus.IN_PROGRESS)
                
                // Get the media file
                val mediaFile = mediaFileDao.getMediaFileById(nextItem.mediaFileId)
                    ?: throw Exception("Media file not found")
                
                // Backup the file
                backupFile(mediaFile, token)
                
                // Mark as completed
                backupQueueDao.updateItemStatus(nextItem.id, BackupStatus.COMPLETED)
                mediaFileDao.markAsBackedUp(mediaFile.id, "s3_key_${mediaFile.id}")
                
                // Calculate progress
                val totalItems = backupQueueDao.getPendingCount() + backupQueueDao.getInProgressCount()
                val completedItems = totalItems - backupQueueDao.getPendingCount()
                val progress = if (totalItems > 0) (completedItems * 100 / totalItems) else 100
                progressCallback?.invoke(progress)
                
            } catch (e: Exception) {
                // Mark as failed and increment attempts
                backupQueueDao.updateItemStatus(nextItem.id, BackupStatus.FAILED)
                mediaFileDao.incrementBackupAttempts(nextItem.mediaFileId, System.currentTimeMillis())
            }
        }
    }
    
    private suspend fun backupFile(mediaFile: LocalMediaFile, token: String): UploadResponse {
        val file = File(mediaFile.path)
        if (!file.exists()) {
            throw Exception("File not found: ${mediaFile.path}")
        }
        
        // Encrypt the file using triple-layer encryption
        val encryptedFile = encryptionService.encryptFile(file)
        
        try {
            // Prepare multipart request
            val requestFile = encryptedFile.asRequestBody("application/octet-stream".toMediaTypeOrNull())
            val filePart = MultipartBody.Part.createFormData("files", mediaFile.name, requestFile)
            
            val metadata = mapOf(
                "originalName" to mediaFile.name,
                "size" to mediaFile.size.toString(),
                "mimeType" to mediaFile.mimeType,
                "dateModified" to mediaFile.dateModified.toString()
            )
            val metadataBody = metadata.toString().toRequestBody("text/plain".toMediaTypeOrNull())
            
            // Upload to server
            val response = apiService.uploadFile("Bearer $token", filePart, metadataBody)
            
            if (response.isSuccessful) {
                return response.body() ?: throw Exception("Empty response")
            } else {
                throw Exception("Upload failed: ${response.code()}")
            }
        } finally {
            // Clean up encrypted file
            encryptedFile.delete()
        }
    }
    
    suspend fun downloadFile(s3Key: String, fileName: String): File = withContext(Dispatchers.IO) {
        val token = preferencesManager.getAuthToken() ?: throw Exception("Not authenticated")
        
        val response = apiService.downloadFile("Bearer $token", s3Key)
        if (!response.isSuccessful) {
            throw Exception("Download failed: ${response.code()}")
        }
        
        val encryptedData = response.body()?.bytes() ?: throw Exception("Empty response")
        
        // Create temporary file for encrypted data
        val encryptedFile = File.createTempFile("encrypted_", ".tmp", context.cacheDir)
        encryptedFile.writeBytes(encryptedData)
        
        try {
            // Decrypt the file
            return@withContext encryptionService.decryptFile(encryptedFile, fileName)
        } finally {
            encryptedFile.delete()
        }
    }
    
    suspend fun deleteFile(s3Key: String): Boolean = withContext(Dispatchers.IO) {
        val token = preferencesManager.getAuthToken() ?: throw Exception("Not authenticated")
        
        val response = apiService.deleteFile("Bearer $token", s3Key)
        response.isSuccessful
    }
    
    suspend fun getUserStats(): UserStats? = withContext(Dispatchers.IO) {
        val token = preferencesManager.getAuthToken() ?: return@withContext null
        
        val response = apiService.getUserStats("Bearer $token")
        if (response.isSuccessful) {
            response.body()
        } else {
            null
        }
    }
}