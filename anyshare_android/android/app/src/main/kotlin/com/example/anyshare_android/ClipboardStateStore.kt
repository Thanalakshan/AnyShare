package com.example.anyshare_android

import android.content.Context

object ClipboardStateStore {
    private const val preferencesName = "anyshare_clipboard"
    private const val androidClipboardKey = "android_clipboard"
    private const val windowsClipboardKey = "windows_clipboard"

    fun setAndroidClipboard(context: Context, text: String) {
        preferences(context).edit().putString(androidClipboardKey, text).commit()
    }

    fun getAndroidClipboard(context: Context): String =
        preferences(context).getString(androidClipboardKey, "") ?: ""

    fun setWindowsClipboard(context: Context, text: String) {
        preferences(context).edit().putString(windowsClipboardKey, text).commit()
    }

    fun getWindowsClipboard(context: Context): String =
        preferences(context).getString(windowsClipboardKey, "") ?: ""

    private fun preferences(context: Context) =
        context.getSharedPreferences(preferencesName, Context.MODE_PRIVATE)
}
