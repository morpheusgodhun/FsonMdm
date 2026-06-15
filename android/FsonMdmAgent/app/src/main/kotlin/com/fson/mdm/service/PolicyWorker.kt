package com.fson.mdm.service

import android.content.Context
import androidx.work.CoroutineWorker
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.WorkerParameters
import com.fson.mdm.core.Constants
import com.fson.mdm.core.Prefs
import java.util.concurrent.TimeUnit

/**
 * WorkManager safety-net. The foreground [HeartbeatService] does the frequent
 * work; this periodic worker (min 15 min) guarantees recovery if the service
 * was killed — it re-arms the service and runs one [MdmSyncEngine] cycle.
 */
class PolicyWorker(
    appContext: Context,
    params: WorkerParameters
) : CoroutineWorker(appContext, params) {

    override suspend fun doWork(): Result {
        val prefs = Prefs.get(applicationContext)
        if (!prefs.isRegistered) return Result.success()

        // Make sure the long-running service is alive.
        HeartbeatService.start(applicationContext)

        // Run a one-shot cycle directly too, in case the service is delayed.
        runCatching { MdmSyncEngine(applicationContext).runCycle() }
        return Result.success()
    }

    companion object {
        fun schedule(context: Context) {
            val request = PeriodicWorkRequestBuilder<PolicyWorker>(
                Constants.WORKER_INTERVAL_MINUTES, TimeUnit.MINUTES
            ).build()

            WorkManager.getInstance(context).enqueueUniquePeriodicWork(
                Constants.WORK_POLICY,
                ExistingPeriodicWorkPolicy.UPDATE,
                request
            )
        }

        fun cancel(context: Context) {
            WorkManager.getInstance(context).cancelUniqueWork(Constants.WORK_POLICY)
        }
    }
}
