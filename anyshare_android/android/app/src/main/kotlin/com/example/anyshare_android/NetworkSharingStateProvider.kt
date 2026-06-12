package com.example.anyshare_android

import android.content.ContentProvider
import android.content.ContentValues
import android.database.Cursor
import android.database.MatrixCursor
import android.net.Uri

class NetworkSharingStateProvider : ContentProvider() {
    override fun onCreate(): Boolean = true

    override fun query(
        uri: Uri,
        projection: Array<out String>?,
        selection: String?,
        selectionArgs: Array<out String>?,
        sortOrder: String?
    ): Cursor {
        val state = if (NetworkSharingStateStore.isEnabled(requireNotNull(context))) {
            "ON"
        } else {
            "OFF"
        }

        return MatrixCursor(arrayOf("state")).apply {
            addRow(arrayOf(state))
        }
    }

    override fun getType(uri: Uri): String =
        "vnd.android.cursor.item/vnd.anyshare.network-state"

    override fun insert(uri: Uri, values: ContentValues?): Uri? =
        throw UnsupportedOperationException("Read-only provider")

    override fun delete(uri: Uri, selection: String?, selectionArgs: Array<out String>?): Int =
        throw UnsupportedOperationException("Read-only provider")

    override fun update(
        uri: Uri,
        values: ContentValues?,
        selection: String?,
        selectionArgs: Array<out String>?
    ): Int = throw UnsupportedOperationException("Read-only provider")
}
