package com.securebackup.mobile.data.local

import androidx.room.*
import com.securebackup.mobile.data.model.BackupQueueItem
import com.securebackup.mobile.data.model.BackupStatus
import kotlinx.coroutines.flow.Flow

@Dao
interface BackupQueueDao {
    
    @Query("SELECT * FROM backup_queue ORDER BY priority DESC, createdAt ASC")
    fun getAllQueueItems(): Flow<List<BackupQueueItem>>
    
    @Query("SELECT * FROM backup_queue WHERE status = :status ORDER BY priority DESC, createdAt ASC")
    fun getQueueItemsByStatus(status: BackupStatus): Flow<List<BackupQueueItem>>
    
    @Query("SELECT * FROM backup_queue WHERE status = 'PENDING' ORDER BY priority DESC, createdAt ASC LIMIT 1")
    suspend fun getNextPendingItem(): BackupQueueItem?
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertQueueItem(queueItem: BackupQueueItem)
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertQueueItems(queueItems: List<BackupQueueItem>)
    
    @Update
    suspend fun updateQueueItem(queueItem: BackupQueueItem)
    
    @Query("UPDATE backup_queue SET status = :status WHERE id = :id")
    suspend fun updateItemStatus(id: Int, status: BackupStatus)
    
    @Query("UPDATE backup_queue SET status = :newStatus WHERE status = :oldStatus")
    suspend fun updateAllItemsStatus(oldStatus: BackupStatus, newStatus: BackupStatus)
    
    @Delete
    suspend fun deleteQueueItem(queueItem: BackupQueueItem)
    
    @Query("DELETE FROM backup_queue WHERE mediaFileId = :mediaFileId")
    suspend fun deleteByMediaFileId(mediaFileId: Long)
    
    @Query("DELETE FROM backup_queue WHERE status = :status")
    suspend fun deleteByStatus(status: BackupStatus)
    
    @Query("SELECT COUNT(*) FROM backup_queue WHERE status = 'PENDING'")
    suspend fun getPendingCount(): Int
    
    @Query("SELECT COUNT(*) FROM backup_queue WHERE status = 'IN_PROGRESS'")
    suspend fun getInProgressCount(): Int
}