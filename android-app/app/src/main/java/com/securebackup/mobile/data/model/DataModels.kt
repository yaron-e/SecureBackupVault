package com.securebackup.mobile.data.model

import androidx.room.Entity
import androidx.room.PrimaryKey
import com.google.gson.annotations.SerializedName

// API Request/Response Models
data class LoginRequest(
    val email: String,
    val password: String
)

data class RegisterRequest(
    val name: String,
    val email: String,
    val password: String
)

data class AuthResponse(
    val success: Boolean,
    val token: String?,
    val user: User?,
    val message: String?
)

data class User(
    val id: Int,
    val name: String?,
    val email: String,
    @SerializedName("google_id") val googleId: String?,
    @SerializedName("profile_image") val profileImage: String?,
    @SerializedName("created_at") val createdAt: String?,
    @SerializedName("updated_at") val updatedAt: String?
)

data class BackupFile(
    val id: Int,
    @SerializedName("user_id") val userId: Int,
    @SerializedName("file_name") val fileName: String,
    @SerializedName("s3_key") val s3Key: String,
    @SerializedName("key_id") val keyId: String,
    val size: Long,
    @SerializedName("last_modified") val lastModified: String,
    @SerializedName("created_at") val createdAt: String
)

data class FilesResponse(
    val success: Boolean,
    val files: List<BackupFile>?,
    val message: String?
)

data class UploadResponse(
    val success: Boolean,
    val file: BackupFile?,
    val message: String?
)

data class ApiResponse(
    val success: Boolean,
    val message: String?
)

data class UserStats(
    val totalFiles: Int,
    val totalSize: Long,
    val lastBackup: String?
)

// Local Database Models
@Entity(tableName = "local_media_files")
data class LocalMediaFile(
    @PrimaryKey val id: Long,
    val path: String,
    val name: String,
    val size: Long,
    val mimeType: String,
    val dateAdded: Long,
    val dateModified: Long,
    val isBackedUp: Boolean = false,
    val backupAttempts: Int = 0,
    val lastBackupAttempt: Long? = null,
    val s3Key: String? = null
)

@Entity(tableName = "backup_queue")
data class BackupQueueItem(
    @PrimaryKey(autoGenerate = true) val id: Int = 0,
    val mediaFileId: Long,
    val priority: Int = 0, // 0 = normal, 1 = high
    val createdAt: Long = System.currentTimeMillis(),
    val status: BackupStatus = BackupStatus.PENDING
)

enum class BackupStatus {
    PENDING,
    IN_PROGRESS,
    COMPLETED,
    FAILED,
    CANCELLED
}

// Configuration Models
data class ServerConfig(
    val baseUrl: String,
    val port: Int = 5000,
    val useHttps: Boolean = false
) {
    fun getFullUrl(): String {
        val protocol = if (useHttps) "https" else "http"
        return "$protocol://$baseUrl:$port/api/"
    }
}

data class BackupSettings(
    val autoBackupEnabled: Boolean = true,
    val wifiOnlyBackup: Boolean = true,
    val backupVideos: Boolean = true,
    val backupPhotos: Boolean = true,
    val maxFileSize: Long = 100 * 1024 * 1024, // 100MB
    val backupFrequency: BackupFrequency = BackupFrequency.IMMEDIATE
)

enum class BackupFrequency {
    IMMEDIATE,
    HOURLY,
    DAILY,
    WEEKLY
}