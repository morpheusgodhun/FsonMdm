package com.fson.mdm.service

import android.app.Notification
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.graphics.Bitmap
import android.graphics.PixelFormat
import android.hardware.display.DisplayManager
import android.hardware.display.VirtualDisplay
import android.media.ImageReader
import android.media.projection.MediaProjection
import android.media.projection.MediaProjectionManager
import android.os.Build
import android.os.Handler
import android.os.HandlerThread
import android.os.IBinder
import android.util.DisplayMetrics
import android.util.Log
import android.view.WindowManager
import androidx.core.app.NotificationCompat
import com.fson.mdm.R
import com.fson.mdm.core.Constants
import com.fson.mdm.data.MdmRepository
import com.fson.mdm.data.MdmResult
import com.fson.mdm.device.MediaProjectionHolder
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.launch
import java.io.ByteArrayOutputStream

/**
 * Captures a single screen frame using MediaProjection and uploads it to the
 * backend, then stops itself. This is the "remote view" foundation: a true
 * live-control channel (continuous streaming + input injection) is intentionally
 * out of scope for this MVP.
 */
class ScreenCaptureService : Service() {

    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private var projection: MediaProjection? = null
    private var virtualDisplay: VirtualDisplay? = null
    private var imageReader: ImageReader? = null
    private var bgThread: HandlerThread? = null
    private var captured = false

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        startForeground(Constants.CAPTURE_NOTIFICATION_ID, buildNotification())

        if (!MediaProjectionHolder.hasConsent) {
            Log.w(TAG, "MediaProjection izni yok — yakalama atlandı.")
            stopSelf()
            return START_NOT_STICKY
        }

        try {
            val mpm = getSystemService(Context.MEDIA_PROJECTION_SERVICE) as MediaProjectionManager
            projection = mpm.getMediaProjection(
                MediaProjectionHolder.resultCode,
                MediaProjectionHolder.resultData!!
            )
            if (projection == null) {
                Log.w(TAG, "MediaProjection alınamadı.")
                stopSelf(); return START_NOT_STICKY
            }

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                projection!!.registerCallback(object : MediaProjection.Callback() {}, Handler(mainLooper))
            }
            startCapture()
        } catch (e: Exception) {
            Log.w(TAG, "Yakalama başlatılamadı: ${e.message}")
            stopSelf()
        }
        return START_NOT_STICKY
    }

    private fun startCapture() {
        val metrics = displayMetrics()
        val width = metrics.widthPixels
        val height = metrics.heightPixels
        val density = metrics.densityDpi

        bgThread = HandlerThread("fson-capture").also { it.start() }
        val handler = Handler(bgThread!!.looper)

        imageReader = ImageReader.newInstance(width, height, PixelFormat.RGBA_8888, 2)
        virtualDisplay = projection!!.createVirtualDisplay(
            "fson-capture",
            width, height, density,
            DisplayManager.VIRTUAL_DISPLAY_FLAG_AUTO_MIRROR,
            imageReader!!.surface,
            null,
            handler
        )

        imageReader!!.setOnImageAvailableListener({ reader ->
            if (captured) return@setOnImageAvailableListener
            val image = reader.acquireLatestImage() ?: return@setOnImageAvailableListener
            captured = true
            try {
                val bytes = toPng(image, width, height)
                uploadAndStop(bytes)
            } catch (e: Exception) {
                Log.w(TAG, "Görüntü işleme hatası: ${e.message}")
                stopSelf()
            } finally {
                runCatching { image.close() }
            }
        }, handler)
    }

    private fun toPng(image: android.media.Image, width: Int, height: Int): ByteArray {
        val plane = image.planes[0]
        val buffer = plane.buffer
        val pixelStride = plane.pixelStride
        val rowStride = plane.rowStride
        val rowPadding = rowStride - pixelStride * width

        val bitmap = Bitmap.createBitmap(
            width + rowPadding / pixelStride, height, Bitmap.Config.ARGB_8888
        )
        bitmap.copyPixelsFromBuffer(buffer)
        val cropped = Bitmap.createBitmap(bitmap, 0, 0, width, height)

        val out = ByteArrayOutputStream()
        cropped.compress(Bitmap.CompressFormat.PNG, 90, out)
        bitmap.recycle()
        cropped.recycle()
        return out.toByteArray()
    }

    private fun uploadAndStop(png: ByteArray) {
        scope.launch {
            when (val r = MdmRepository(applicationContext).uploadScreenshot(png)) {
                is MdmResult.Ok -> Log.d(TAG, "Ekran görüntüsü yüklendi")
                is MdmResult.Err -> Log.w(TAG, "Ekran görüntüsü yüklenemedi: ${r.message}")
            }
            stopSelf()
        }
    }

    @Suppress("DEPRECATION")
    private fun displayMetrics(): DisplayMetrics {
        val metrics = DisplayMetrics()
        val wm = getSystemService(Context.WINDOW_SERVICE) as WindowManager
        wm.defaultDisplay.getRealMetrics(metrics)
        return metrics
    }

    private fun buildNotification(): Notification {
        val nm = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = android.app.NotificationChannel(
                Constants.CAPTURE_CHANNEL_ID,
                "Ekran Yakalama",
                NotificationManager.IMPORTANCE_LOW
            )
            nm.createNotificationChannel(channel)
        }
        return NotificationCompat.Builder(this, Constants.CAPTURE_CHANNEL_ID)
            .setSmallIcon(R.mipmap.ic_launcher)
            .setContentTitle(getString(R.string.app_name))
            .setContentText("Ekran görüntüsü alınıyor…")
            .setOngoing(true)
            .build()
    }

    override fun onDestroy() {
        runCatching { virtualDisplay?.release() }
        runCatching { imageReader?.close() }
        runCatching { projection?.stop() }
        runCatching { bgThread?.quitSafely() }
        scope.cancel()
        super.onDestroy()
    }

    companion object {
        private const val TAG = "ScreenCaptureService"

        fun start(context: Context) {
            val intent = Intent(context, ScreenCaptureService::class.java)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                context.startForegroundService(intent)
            } else {
                context.startService(intent)
            }
        }
    }
}
