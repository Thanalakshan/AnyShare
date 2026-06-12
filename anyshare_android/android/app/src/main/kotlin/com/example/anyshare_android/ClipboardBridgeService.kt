package com.example.anyshare_android

import android.app.Service
import android.content.Intent
import android.net.Uri
import android.os.IBinder
import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.ServerSocket
import java.net.Socket
import java.net.URLDecoder
import kotlin.concurrent.thread

class ClipboardBridgeService : Service() {
    private var serverSocket: ServerSocket? = null
    private var running = false

    companion object {
        @Volatile
        var isRunning = false
    }

    override fun onCreate() {
        super.onCreate()
        isRunning = true
        startServer()
    }

    override fun onDestroy() {
        running = false
        isRunning = false

        try {
            serverSocket?.close()
        } catch (_: Exception) {
        }

        super.onDestroy()
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun startServer() {
        running = true

        thread(start = true) {
            try {
                serverSocket = ServerSocket(8765)

                while (running) {
                    val client = serverSocket?.accept() ?: continue
                    handleClient(client)
                }
            } catch (_: Exception) {
            }
        }
    }

    private fun handleClient(socket: Socket) {
        thread(start = true) {
            try {
                val reader =
                    BufferedReader(InputStreamReader(socket.getInputStream()))

                val requestLine = reader.readLine() ?: ""

                when {
                    requestLine.startsWith("GET /clipboard/android/last") -> {
                        val encoded = Uri.encode(
                            ClipboardStateStore.getAndroidClipboard(this)
                        )

                        socket.getOutputStream().write(
                            buildResponse(
                                """{"text":"$encoded"}"""
                            ).toByteArray()
                        )
                    }

                    requestLine.startsWith("POST /clipboard/windows/send") -> {
                        val body = readBody(reader)

                        val encodedText = body
                            .substringAfter("\"text\":\"", "")
                            .substringBefore("\"", "")

                        ClipboardStateStore.setWindowsClipboard(
                            this,
                            URLDecoder.decode(encodedText, "UTF-8")
                        )

                        socket.getOutputStream().write(
                            buildResponse(
                                """{"ok":true}"""
                            ).toByteArray()
                        )
                    }

                    else -> {
                        socket.getOutputStream().write(
                            buildResponse(
                                """{"error":"unknown"}"""
                            ).toByteArray()
                        )
                    }
                }

                socket.close()
            } catch (_: Exception) {
                try {
                    socket.close()
                } catch (_: Exception) {
                }
            }
        }
    }

    private fun readBody(reader: BufferedReader): String {
        var contentLength = 0

        while (true) {
            val line = reader.readLine()

            if (line.isNullOrEmpty())
                break

            if (line.lowercase().startsWith("content-length:")) {
                contentLength =
                    line.substringAfter(":")
                        .trim()
                        .toIntOrNull() ?: 0
            }
        }

        val bodyChars = CharArray(contentLength)
        reader.read(bodyChars)

        return String(bodyChars)
    }

    private fun buildResponse(body: String): String {
        return "HTTP/1.1 200 OK\r\n" +
            "Content-Type: application/json; charset=utf-8\r\n" +
            "Content-Length: ${body.toByteArray().size}\r\n" +
            "Connection: close\r\n" +
            "\r\n" +
            body
    }
}
