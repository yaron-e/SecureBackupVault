package com.securebackup.mobile.data.local

import androidx.lifecycle.LiveData
import androidx.room.*
import com.securebackup.mobile.data.model.LocalMediaFile
import kotlinx.coroutines.flow.Flow

@Dao
interface MediaFileDao {
    
    @Query("SELECT * FROM local_media_files ORDER BY dateAdded DESC")
    fun getAllMediaFiles(): Flow<List<LocalMediaFile>>
    
    @Query("SELECT * FROM local_media_files WHERE isBackedUp = 0 ORDER BY dateAdded DESC")
    fun getUnbackedUpFiles(): Flow<List<LocalMediaFile>>
    
    @Query("SELECT * FROM local_media_files WHERE isBackedUp = 1 ORDER BY dateAdded DESC")
    fun getBackedUpFiles(): Flow<List<LocalMediaFile>>
    
    @Query("SELECT * FROM local_media_files WHERE id = :id")
    suspend fun getMediaFileById(id: Long): LocalMediaFile?
    
    @Query("SELECT * FROM local_media_files WHERE path = :path")
    suspend fun getMediaFileByPath(path: String): LocalMediaFile?
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertMediaFile(mediaFile: LocalMediaFile)
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertMediaFiles(mediaFiles: List<LocalMediaFile>)
    
    @Update
    suspend fun updateMediaFile(mediaFile: LocalMediaFile)
    
    @Query("UPDATE local_media_files SET isBackedUp = 1, s3Key = :s3Key WHERE id = :id")
    suspend fun markAsBackedUp(id: Long, s3Key: String)
    
    @Query("UPDATE local_media_files SET backupAttempts = backupAttempts + 1, lastBackupAttempt = :timestamp WHERE id = :id")
    suspend fun incrementBackupAttempts(id: Long, timestamp: Long)
    
    @Delete
    suspend fun deleteMediaFile(mediaFile: LocalMediaFile)
    
    @Query("DELETE FROM local_media_files WHERE path = :path")
    suspend fun deleteByPath(path: String)
    
    @Query("SELECT COUNT(*) FROM local_media_files")
    suspend fun getTotalFileCount(): Int
    
    @Query("SELECT COUNT(*) FROM local_media_files WHERE isBackedUp = 1")
    suspend fun getBackedUpFileCount(): Int
    
    @Query("SELECT SUM(size) FROM local_media_files WHERE isBackedUp = 1")
    suspend fun getTotalBackedUpSize(): Long?
}