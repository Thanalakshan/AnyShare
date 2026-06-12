package com.example.anyshare_android

import android.content.Context

object NetworkSharingStateStore {
    private const val preferencesName = "anyshare_network_sharing"
    private const val enabledKey = "enabled"

    fun isEnabled(context: Context): Boolean =
        context.getSharedPreferences(preferencesName, Context.MODE_PRIVATE)
            .getBoolean(enabledKey, false)

    fun setEnabled(context: Context, enabled: Boolean) {
        context.getSharedPreferences(preferencesName, Context.MODE_PRIVATE)
            .edit()
            .putBoolean(enabledKey, enabled)
            .commit()
    }
}
