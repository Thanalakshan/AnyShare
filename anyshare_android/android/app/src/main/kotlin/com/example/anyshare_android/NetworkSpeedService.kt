package com.example.anyshare_android

import android.app.*
import android.content.Context
import android.content.Intent
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.net.TrafficStats
import android.os.*
import androidx.core.app.NotificationCompat
import org.json.JSONArray
import org.json.JSONObject
import kotlin.math.max
import android.graphics.*
import androidx.core.graphics.drawable.IconCompat
import kotlin.math.roundToInt

class NetworkSpeedService : Service() {
    private val channelId = "anyshare_network_speed"
    private val notificationId = 1001

    private val handler = Handler(Looper.getMainLooper())

    private var lastRx = 0L
    private var lastTx = 0L
    private var lastTime = 0L

    private var todayWifiBytes = 0L
    private var todayCellularBytes = 0L
    private var currentDate = ""

    private val updater = object : Runnable {
        override fun run() {
            updateSpeed()
            handler.postDelayed(this, 1000)
        }
    }

    override fun onCreate() {
        super.onCreate()

        createChannel()
        loadUsage()

        lastRx = TrafficStats.getTotalRxBytes()
        lastTx = TrafficStats.getTotalTxBytes()
        lastTime = System.currentTimeMillis()

        val initialNotification = buildNotification(
            "↓ 0 B/s   ↑ 0 B/s",
            "WiFi: ${formatBytes(todayWifiBytes)}   Mobile: ${formatBytes(todayCellularBytes)}",
            "0 KB/s"
        )

        startForeground(notificationId, initialNotification)

        handler.post(updater)
    }

    override fun onDestroy() {
        handler.removeCallbacks(updater)
        saveUsage()
        super.onDestroy()
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun createSpeedIcon(speedText: String): IconCompat {
        val bitmap = Bitmap.createBitmap(192, 192, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)

        canvas.drawColor(Color.TRANSPARENT)

        val paint = Paint(Paint.ANTI_ALIAS_FLAG)
        paint.color = Color.WHITE
        paint.textAlign = Paint.Align.CENTER
        paint.typeface = Typeface.DEFAULT_BOLD

        var valueText = "0"
        var unitText = "KB/s"

        try {
            when {
                speedText.contains("MB/s") -> {
                    val value = speedText
                        .replace("MB/s", "")
                        .trim()
                        .toDouble()

                    valueText = String.format("%.1f", value)
                    unitText = "MB/s"
                }

                speedText.contains("KB/s") -> {
                    val value = speedText
                        .replace("KB/s", "")
                        .trim()
                        .toDouble()

                    valueText = value.roundToInt().toString()
                    unitText = "KB/s"
                }

                speedText.contains("B/s") -> {
                    val value = speedText
                        .replace("B/s", "")
                        .trim()
                        .toDouble()

                    valueText = value.roundToInt().toString()
                    unitText = "B/s"
                }
            }
        } catch (_: Exception) {
        }

        // Main speed value
        paint.textSize = 84f
        canvas.drawText(
            valueText,
            96f,
            85f,
            paint
        )

        // Unit
        paint.textSize = 36f
        canvas.drawText(
            unitText,
            96f,
            135f,
            paint
        )

        return IconCompat.createWithBitmap(bitmap)
    }

    private fun updateSpeed() {
        resetIfNewDay()

        val nowRx = TrafficStats.getTotalRxBytes()
        val nowTx = TrafficStats.getTotalTxBytes()
        val nowTime = System.currentTimeMillis()

        val diffTime = max(nowTime - lastTime, 1L) / 1000.0
        val diffRx = max(nowRx - lastRx, 0L)
        val diffTx = max(nowTx - lastTx, 0L)

        val networkType = getNetworkType()

        if (networkType == "Wi-Fi") {
            todayWifiBytes += diffRx + diffTx
        } else if (networkType == "Cellular") {
            todayCellularBytes += diffRx + diffTx
        }

        val downSpeed = diffRx / diffTime
        val upSpeed = diffTx / diffTime

        lastRx = nowRx
        lastTx = nowTx
        lastTime = nowTime

        saveUsage()
        saveSevenDayHistory(diffRx, diffTx, networkType)

        val title = "↓ ${formatSpeed(downSpeed)}   ↑ ${formatSpeed(upSpeed)}"
        val text = "WiFi: ${formatBytes(todayWifiBytes)}   Mobile: ${formatBytes(todayCellularBytes)}"

        val manager = getSystemService(NOTIFICATION_SERVICE) as NotificationManager
        val totalSpeedText = formatSpeed(downSpeed + upSpeed)

        val notification = buildNotification(
            title,
            text,
            totalSpeedText
        )

        manager.notify(notificationId, notification)
    }

    private fun buildNotification(
        title: String,
        text: String,
        totalSpeedText: String
    ): Notification {
        return NotificationCompat.Builder(this, channelId)
            .setSmallIcon(createSpeedIcon(totalSpeedText))
            .setContentTitle(title)
            .setContentText(text)
            .setStyle(NotificationCompat.BigTextStyle().bigText("$title\n$text"))
            .setOngoing(true)
            .setOnlyAlertOnce(true)
            .setShowWhen(false)
            .setPriority(NotificationCompat.PRIORITY_LOW)
            .setCategory(NotificationCompat.CATEGORY_STATUS)
            .build()
    }

    private fun getNetworkType(): String {
        val cm = getSystemService(CONNECTIVITY_SERVICE) as ConnectivityManager
        val network = cm.activeNetwork ?: return "Unknown"
        val caps = cm.getNetworkCapabilities(network) ?: return "Unknown"

        return when {
            caps.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) -> "Wi-Fi"
            caps.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR) -> "Cellular"
            else -> "Unknown"
        }
    }

    private fun formatSpeed(bytesPerSecond: Double): String {
        return when {
            bytesPerSecond >= 1024 * 1024 ->
                String.format("%.2f MB/s", bytesPerSecond / 1024 / 1024)
            bytesPerSecond >= 1024 ->
                String.format("%.1f KB/s", bytesPerSecond / 1024)
            else ->
                String.format("%.0f B/s", bytesPerSecond)
        }
    }

    private fun formatBytes(bytes: Long): String {
        return when {
            bytes >= 1024L * 1024L * 1024L ->
                String.format("%.2f GB", bytes / 1024.0 / 1024.0 / 1024.0)
            bytes >= 1024L * 1024L ->
                String.format("%.2f MB", bytes / 1024.0 / 1024.0)
            bytes >= 1024L ->
                String.format("%.1f KB", bytes / 1024.0)
            else -> "$bytes B"
        }
    }

    private fun today(): String {
        return java.text.SimpleDateFormat("yyyy-MM-dd", java.util.Locale.US)
            .format(java.util.Date())
    }

    private fun resetIfNewDay() {
        val nowDate = today()
        if (currentDate != nowDate) {
            currentDate = nowDate
            todayWifiBytes = 0L
            todayCellularBytes = 0L
            saveUsage()
        }
    }

    private fun loadUsage() {
        val prefs = getSharedPreferences("anyshare_speed", Context.MODE_PRIVATE)
        currentDate = prefs.getString("date", today()) ?: today()
        todayWifiBytes = prefs.getLong("wifi", 0L)
        todayCellularBytes = prefs.getLong("cellular", 0L)
        resetIfNewDay()
    }

    private fun saveUsage() {
        getSharedPreferences("anyshare_speed", Context.MODE_PRIVATE)
            .edit()
            .putString("date", currentDate)
            .putLong("wifi", todayWifiBytes)
            .putLong("cellular", todayCellularBytes)
            .apply()
    }

    private fun saveSevenDayHistory(downloadBytes: Long, uploadBytes: Long, networkType: String) {
        val prefs = getSharedPreferences("FlutterSharedPreferences", Context.MODE_PRIVATE)
        val key = "flutter.anyshare_usage_history"

        val todayDate = today()
        val raw = prefs.getString(key, "[]") ?: "[]"
        val array = JSONArray(raw)

        var foundIndex = -1

        for (i in 0 until array.length()) {
            val item = array.getJSONObject(i)
            if (item.getString("date") == todayDate) {
                foundIndex = i
                break
            }
        }

        val downloadMB = downloadBytes / 1024.0 / 1024.0
        val uploadMB = uploadBytes / 1024.0 / 1024.0
        val totalMB = downloadMB + uploadMB

        val wifiAdd = if (networkType == "Wi-Fi") totalMB else 0.0
        val cellularAdd = if (networkType == "Cellular") totalMB else 0.0

        if (foundIndex >= 0) {
            val item = array.getJSONObject(foundIndex)

            item.put("wifiMB", item.optDouble("wifiMB", 0.0) + wifiAdd)
            item.put("cellularMB", item.optDouble("cellularMB", 0.0) + cellularAdd)
            item.put("downloadMB", item.optDouble("downloadMB", 0.0) + downloadMB)
            item.put("uploadMB", item.optDouble("uploadMB", 0.0) + uploadMB)
            item.put("totalMB", item.optDouble("totalMB", 0.0) + totalMB)

            array.put(foundIndex, item)
        } else {
            val item = JSONObject()
            item.put("date", todayDate)
            item.put("wifiMB", wifiAdd)
            item.put("cellularMB", cellularAdd)
            item.put("downloadMB", downloadMB)
            item.put("uploadMB", uploadMB)
            item.put("totalMB", totalMB)
            array.put(item)
        }

        while (array.length() > 7) {
            array.remove(0)
        }

        prefs.edit().putString(key, array.toString()).apply()
    }

    private fun createChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                channelId,
                "AnyShare Network Speed",
                NotificationManager.IMPORTANCE_LOW
            )
            channel.setShowBadge(false)

            val manager = getSystemService(NotificationManager::class.java)
            manager.createNotificationChannel(channel)
        }
    }
}