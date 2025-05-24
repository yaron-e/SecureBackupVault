package com.securebackup.mobile.service

import android.app.Notification
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Intent
import android.os.IBinder
import androidx.core.app.NotificationCompat
import androidx.hilt.work.HiltWorker
import androidx.work.*
import com.securebackup.mobile.R
import com.securebackup.mobile.SecureBackupApplication
import com.securebackup.mobile.data.local.BackupQueueDao
import com.securebackup.mobile.data.local.MediaFileDao
import com.securebackup.mobile.data.model.BackupStatus
import com.securebackup.mobile.data.network.ApiService
import com.securebackup.mobile.data.repository.BackupRepository
import com.securebackup.mobile.ui.MainActivity
import com.securebackup.mobile.util.PreferencesManager
import dagger.assisted.Assisted
import dagger.assisted.AssistedInject
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.*
import java.util.concurrent.TimeUnit
import javax.inject.Inject

@AndroidEntryPoint
class BackupService : Service() {

    @Inject
    lateinit var backupRepository: BackupRepository
    
    @Inject
    lateinit var preferencesManager: PreferencesManager

    private val serviceScope = CoroutineScope(Dispatchers.IO + SupervisorJob())
    private var isBackupRunning = false

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onCreate() {
        super.onCreate()
        startForeground(SecureBackupApplication.BACKUP_NOTIFICATION_ID, createNotification())
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_START_BACKUP -> startBackupProcess()
            ACTION_STOP_BACKUP -> stopBackupProcess()
            ACTION_PAUSE_BACKUP -> pauseBackupProcess()
        }
        return START_STICKY
    }

    private fun startBackupProcess() {
        if (isBackupRunning) return
        
        isBackupRunning = true
        serviceScope.launch {
            try {
                backupRepository.processBackupQueue { progress ->
                    updateNotification("Backing up files...", progress)
                }
                updateNotification("Backup completed", 100)
            } catch (e: Exception) {
                updateNotification("Backup failed: ${e.message}", 0)
            } finally {
                isBackupRunning = false
                stopSelf()
            }
        }
    }

    private fun stopBackupProcess() {
        isBackupRunning = false
        serviceScope.coroutineContext.cancelChildren()
        stopSelf()
    }

    private fun pauseBackupProcess() {
        isBackupRunning = false
        serviceScope.coroutineContext.cancelChildren()
        updateNotification("Backup paused", 0)
    }

    private fun createNotification(
        title: String = "Secure Backup",
        text: String = "Ready to backup your files",
        progress: Int = 0
    ): Notification {
        val intent = Intent(this, MainActivity::class.java)
        val pendingIntent = PendingIntent.getActivity(
            this, 0, intent, PendingIntent.FLAG_IMMUTABLE
        )

        return NotificationCompat.Builder(this, SecureBackupApplication.BACKUP_CHANNEL_ID)
            .setContentTitle(title)
            .setContentText(text)
            .setSmallIcon(R.drawable.ic_backup)
            .setContentIntent(pendingIntent)
            .setOngoing(true)
            .apply {
                if (progress > 0) {
                    setProgress(100, progress, false)
                }
            }
            .build()
    }

    private fun updateNotification(text: String, progress: Int) {
        val notification = createNotification("Secure Backup", text, progress)
        val notificationManager = getSystemService(NotificationManager::class.java)
        notificationManager.notify(SecureBackupApplication.BACKUP_NOTIFICATION_ID, notification)
    }

    override fun onDestroy() {
        super.onDestroy()
        serviceScope.cancel()
    }

    companion object {
        const val ACTION_START_BACKUP = "com.securebackup.mobile.START_BACKUP"
        const val ACTION_STOP_BACKUP = "com.securebackup.mobile.STOP_BACKUP"
        const val ACTION_PAUSE_BACKUP = "com.securebackup.mobile.PAUSE_BACKUP"
    }
}

@HiltWorker
class BackupWorker @AssistedInject constructor(
    @Assisted context: android.content.Context,
    @Assisted workerParams: WorkerParameters,
    private val backupRepository: BackupRepository,
    private val mediaScannerService: MediaScannerService
) : CoroutineWorker(context, workerParams) {

    override suspend fun doWork(): Result {
        return try {
            // Scan for new media files
            mediaScannerService.scanForNewMedia()
            
            // Process backup queue
            backupRepository.processBackupQueue()
            
            Result.success()
        } catch (e: Exception) {
            if (runAttemptCount < 3) {
                Result.retry()
            } else {
                Result.failure()
            }
        }
    }

    companion object {
        const val WORK_NAME = "backup_worker"
        
        fun schedulePeriodicBackup(context: android.content.Context) {
            val constraints = Constraints.Builder()
                .setRequiredNetworkType(NetworkType.CONNECTED)
                .setRequiresBatteryNotLow(true)
                .build()

            val backupRequest = PeriodicWorkRequestBuilder<BackupWorker>(
                1, TimeUnit.HOURS
            )
                .setConstraints(constraints)
                .setBackoffCriteria(
                    BackoffPolicy.EXPONENTIAL,
                    WorkRequest.MIN_BACKOFF_MILLIS,
                    TimeUnit.MILLISECONDS
                )
                .build()

            WorkManager.getInstance(context)
                .enqueueUniquePeriodicWork(
                    WORK_NAME,
                    ExistingPeriodicWorkPolicy.KEEP,
                    backupRequest
                )
        }
    }
}