package com.example.anyshare_android

import android.app.Service
import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import android.os.IBinder
import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.ServerSocket
import java.net.Socket
import java.net.URLDecoder
import java.net.URLEncoder
import kotlin.concurrent.thread

class ClipboardBridgeService : Service() {
    private var serverSocket: ServerSocket? = null
    private var running = false

    override fun onCreate() {
        super.onCreate()
        startServer()
    }

    override fun onDestroy() {
        running = false
        serverSocket?.close()
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
                val reader = BufferedReader(InputStreamReader(socket.getInputStream()))
                val requestLine = reader.readLine() ?: ""

                if (requestLine.startsWith("GET /clipboard/pull")) {
                    val text = getClipboardText()
                    val encoded = URLEncoder.encode(text, "UTF-8")

                    val body = """{"text":"$encoded"}"""

                    socket.getOutputStream().write(
                        buildResponse(body).toByteArray()
                    )
                } else if (requestLine.startsWith("POST /clipboard/push")) {
                    var contentLength = 0
                    var line: String?

                    while (true) {
                        line = reader.readLine()
                        if (line.isNullOrEmpty()) break

                        if (line.lowercase().startsWith("content-length:")) {
                            contentLength = line.substringAfter(":").trim().toIntOrNull() ?: 0
                        }
                    }

                    val bodyChars = CharArray(contentLength)
                    reader.read(bodyChars)
                    val body = String(bodyChars)

                    val encodedText = body
                        .substringAfter("\"text\":\"", "")
                        .substringBefore("\"", "")

                    val text = URLDecoder.decode(encodedText, "UTF-8")
                    setClipboardText(text)

                    socket.getOutputStream().write(
                        buildResponse("""{"ok":true}""").toByteArray()
                    )
                } else {
                    socket.getOutputStream().write(
                        buildResponse("""{"error":"unknown"}""").toByteArray()
                    )
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

    private fun getClipboardText(): String {
        val clipboard = getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
        val clip = clipboard.primaryClip ?: return ""

        if (clip.itemCount == 0) return ""

        return clip.getItemAt(0).coerceToText(this)?.toString() ?: ""
    }

    private fun setClipboardText(text: String) {
        val clipboard = getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
        val clip = ClipData.newPlainText("AnyShare Clipboard", text)
        clipboard.setPrimaryClip(clip)
    }

    private fun buildResponse(body: String): String {
        return """
HTTP/1.1 200 OK
Content-Type: application/json
Content-Length: ${body.toByteArray().size}
Connection: close

$body
""".trimIndent()
    }
}