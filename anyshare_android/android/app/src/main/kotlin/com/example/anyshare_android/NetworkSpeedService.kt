package com.example.anyshare_android

import android.app.*
import android.content.Intent
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.net.TrafficStats
import android.os.*
import androidx.core.app.NotificationCompat
import kotlin.math.max

class NetworkSpeedService : Service() {
    private val channelId = "anyshare_network_speed"
    private val notificationId = 1001

    private val handler = Handler(Looper.getMainLooper())
    private var lastRx = 0L
    private var lastTx = 0L
    private var lastTime = 0L

    private val updater = object : Runnable {
        override fun run() {
            updateNotification()
            handler.postDelayed(this, 1000)
        }
    }

    override fun onCreate() {
        super.onCreate()
        createChannel()

        lastRx = TrafficStats.getTotalRxBytes()
        lastTx = TrafficStats.getTotalTxBytes()
        lastTime = System.currentTimeMillis()

        startForeground(notificationId, buildNotification("Starting...", "Detecting network"))
        handler.post(updater)
    }

    override fun onDestroy() {
        handler.removeCallbacks(updater)
        super.onDestroy()
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun updateNotification() {
        val nowRx = TrafficStats.getTotalRxBytes()
        val nowTx = TrafficStats.getTotalTxBytes()
        val nowTime = System.currentTimeMillis()

        val diffTime = max(nowTime - lastTime, 1L) / 1000.0

        val downloadSpeed = (nowRx - lastRx) / diffTime
        val uploadSpeed = (nowTx - lastTx) / diffTime

        lastRx = nowRx
        lastTx = nowTx
        lastTime = nowTime

        val networkType = getNetworkType()

        val content = "↓ ${formatSpeed(downloadSpeed)}   ↑ ${formatSpeed(uploadSpeed)}"

        val notification = buildNotification(content, networkType)

        val manager = getSystemService(NOTIFICATION_SERVICE) as NotificationManager
        manager.notify(notificationId, notification)
    }

    private fun buildNotification(content: String, networkType: String): Notification {
        return NotificationCompat.Builder(this, channelId)
            .setSmallIcon(android.R.drawable.stat_sys_download_done)
            .setContentTitle("AnyShare Network Speed")
            .setContentText("$content • $networkType")
            .setOngoing(true)
            .setOnlyAlertOnce(true)
            .setPriority(NotificationCompat.PRIORITY_LOW)
            .setCategory(NotificationCompat.CATEGORY_SERVICE)
            .build()
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

    private fun getNetworkType(): String {
        val cm = getSystemService(CONNECTIVITY_SERVICE) as ConnectivityManager
        val network = cm.activeNetwork ?: return "No network"
        val caps = cm.getNetworkCapabilities(network) ?: return "Unknown"

        return when {
            caps.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) -> "Wi-Fi"
            caps.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR) -> "Cellular"
            caps.hasTransport(NetworkCapabilities.TRANSPORT_VPN) -> "VPN"
            caps.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET) -> "Ethernet"
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
}