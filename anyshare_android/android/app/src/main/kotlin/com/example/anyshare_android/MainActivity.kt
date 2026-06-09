package com.example.anyshare_android

import android.content.Intent
import android.net.Uri
import android.os.Build
import android.provider.Settings
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    private val speedChannel = "anyshare/network_speed"
    private val deviceChannel = "anyshare/device"
    private val clipboardChannel = "anyshare/clipboard"

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)

        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, speedChannel)
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    "startSpeedNotification" -> {
                        val intent = Intent(this, NetworkSpeedService::class.java)

                        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                            startForegroundService(intent)
                        } else {
                            startService(intent)
                        }

                        result.success(true)
                    }

                    "stopSpeedNotification" -> {
                        stopService(Intent(this, NetworkSpeedService::class.java))
                        result.success(true)
                    }

                    "isSpeedServiceRunning" -> {
                        result.success(NetworkSpeedService.isRunning)
                    }

                    else -> result.notImplemented()
                }
            }

        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, clipboardChannel)
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    "startClipboardBridge" -> {
                        val intent = Intent(this, ClipboardBridgeService::class.java)
                        startService(intent)
                        result.success(true)
                    }

                    "stopClipboardBridge" -> {
                        stopService(Intent(this, ClipboardBridgeService::class.java))
                        result.success(true)
                    }

                    else -> result.notImplemented()
                }
            }

        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, deviceChannel)
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    "isUsbDebuggingEnabled" -> {
                        val enabled = Settings.Global.getInt(
                            contentResolver,
                            Settings.Global.ADB_ENABLED,
                            0
                        ) == 1

                        result.success(enabled)
                    }

                    "isOverlayPermissionGranted" -> {
                        result.success(Settings.canDrawOverlays(this))
                    }

                    "openOverlayPermissionSettings" -> {
                        val intent = Intent(
                            Settings.ACTION_MANAGE_OVERLAY_PERMISSION,
                            Uri.parse("package:$packageName")
                        )
                        startActivity(intent)
                        result.success(true)
                    }

                    "isAccessibilityEnabled" -> {
                        val enabledServices = Settings.Secure.getString(
                            contentResolver,
                            Settings.Secure.ENABLED_ACCESSIBILITY_SERVICES
                        ) ?: ""

                        result.success(enabledServices.contains(packageName))
                    }

                    "openAccessibilitySettings" -> {
                        startActivity(Intent(Settings.ACTION_ACCESSIBILITY_SETTINGS))
                        result.success(true)
                    }

                    else -> result.notImplemented()
                }
            }
    }
}