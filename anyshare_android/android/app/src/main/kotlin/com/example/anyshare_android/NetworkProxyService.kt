package com.example.anyshare_android

import android.app.Service
import android.content.Intent
import android.os.IBinder
import java.io.InputStream
import java.io.OutputStream
import java.net.ServerSocket
import java.net.Socket
import kotlin.concurrent.thread

class NetworkProxyService : Service() {
    private var serverSocket: ServerSocket? = null
    private var running = false

    companion object {
        var isRunning = false
    }

    override fun onCreate() {
        super.onCreate()
        startProxy()
    }

    override fun onDestroy() {
        running = false
        isRunning = false
        serverSocket?.close()
        super.onDestroy()
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun startProxy() {
        if (running) return

        running = true
        isRunning = true

        thread(start = true) {
            try {
                serverSocket = ServerSocket(8888)

                while (running) {
                    val client = serverSocket?.accept() ?: continue
                    handleClient(client)
                }
            } catch (_: Exception) {
                running = false
                isRunning = false
            }
        }
    }

    private fun handleClient(clientSocket: Socket) {
        thread(start = true) {
            try {
                val input = clientSocket.getInputStream()
                val output = clientSocket.getOutputStream()

                val requestBuffer = ByteArray(8192)
                val read = input.read(requestBuffer)

                if (read <= 0) {
                    clientSocket.close()
                    return@thread
                }

                val requestText = String(requestBuffer, 0, read)

                if (requestText.startsWith("CONNECT")) {
                    handleHttpsConnect(clientSocket, input, output, requestText)
                } else {
                    handleHttpRequest(clientSocket, input, output, requestBuffer, read, requestText)
                }
            } catch (_: Exception) {
                try {
                    clientSocket.close()
                } catch (_: Exception) {
                }
            }
        }
    }

    private fun handleHttpsConnect(
        clientSocket: Socket,
        clientInput: InputStream,
        clientOutput: OutputStream,
        requestText: String
    ) {
        try {
            val firstLine = requestText.lines().firstOrNull() ?: ""
            val hostPort = firstLine.split(" ").getOrNull(1) ?: ""

            val host = hostPort.substringBefore(":")
            val port = hostPort.substringAfter(":", "443").toIntOrNull() ?: 443

            val remoteSocket = Socket(host, port)

            clientOutput.write("HTTP/1.1 200 Connection Established\r\n\r\n".toByteArray())
            clientOutput.flush()

            pipeBothWays(clientSocket, remoteSocket)
        } catch (_: Exception) {
            clientSocket.close()
        }
    }

    private fun handleHttpRequest(
        clientSocket: Socket,
        clientInput: InputStream,
        clientOutput: OutputStream,
        firstBuffer: ByteArray,
        firstRead: Int,
        requestText: String
    ) {
        try {
            val hostLine = requestText
                .lines()
                .firstOrNull { it.lowercase().startsWith("host:") }
                ?: ""

            val hostValue = hostLine.substringAfter(":").trim()

            if (hostValue.isEmpty()) {
                clientSocket.close()
                return
            }

            val host = hostValue.substringBefore(":")
            val port = hostValue.substringAfter(":", "80").toIntOrNull() ?: 80

            val remoteSocket = Socket(host, port)
            val remoteOutput = remoteSocket.getOutputStream()

            remoteOutput.write(firstBuffer, 0, firstRead)
            remoteOutput.flush()

            pipeBothWays(clientSocket, remoteSocket, clientToRemoteAlreadyStarted = true)
        } catch (_: Exception) {
            clientSocket.close()
        }
    }

    private fun pipeBothWays(
        clientSocket: Socket,
        remoteSocket: Socket,
        clientToRemoteAlreadyStarted: Boolean = false
    ) {
        val clientInput = clientSocket.getInputStream()
        val clientOutput = clientSocket.getOutputStream()
        val remoteInput = remoteSocket.getInputStream()
        val remoteOutput = remoteSocket.getOutputStream()

        if (!clientToRemoteAlreadyStarted) {
            thread(start = true) {
                copyStream(clientInput, remoteOutput)
                closeSockets(clientSocket, remoteSocket)
            }
        } else {
            thread(start = true) {
                copyStream(clientInput, remoteOutput)
                closeSockets(clientSocket, remoteSocket)
            }
        }

        thread(start = true) {
            copyStream(remoteInput, clientOutput)
            closeSockets(clientSocket, remoteSocket)
        }
    }

    private fun copyStream(input: InputStream, output: OutputStream) {
        try {
            val buffer = ByteArray(16 * 1024)

            while (running) {
                val read = input.read(buffer)

                if (read == -1) break

                output.write(buffer, 0, read)
                output.flush()
            }
        } catch (_: Exception) {
        }
    }

    private fun closeSockets(clientSocket: Socket, remoteSocket: Socket) {
        try {
            clientSocket.close()
        } catch (_: Exception) {
        }

        try {
            remoteSocket.close()
        } catch (_: Exception) {
        }
    }
}