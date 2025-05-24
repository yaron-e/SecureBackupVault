package com.securebackup.mobile.data.local

import androidx.room.TypeConverter
import com.securebackup.mobile.data.model.BackupStatus

class Converters {
    
    @TypeConverter
    fun fromBackupStatus(status: BackupStatus): String {
        return status.name
    }
    
    @TypeConverter
    fun toBackupStatus(status: String): BackupStatus {
        return BackupStatus.valueOf(status)
    }
}