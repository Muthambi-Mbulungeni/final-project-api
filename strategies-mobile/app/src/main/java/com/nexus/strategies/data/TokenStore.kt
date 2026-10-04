package com.nexus.strategies.data

import android.content.Context
import android.content.SharedPreferences
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey

/**
 * Access token: memory only (lost when the process dies, restored via /refresh).
 * Refresh token: EncryptedSharedPreferences backed by the Android Keystore. Never logged.
 */
class TokenStore(private val context: Context) {
    private val prefs: SharedPreferences = create()

    @Volatile var accessToken: String? = null

    var refreshToken: String?
        get() = prefs.getString(KEY_REFRESH, null)
        private set(v) = prefs.edit().apply { if (v == null) remove(KEY_REFRESH) else putString(KEY_REFRESH, v) }.apply()

    fun save(r: AuthResponse) {
        accessToken = r.accessToken
        refreshToken = r.refreshToken // the pair rotates: always store the newest refresh token
    }

    fun clear() {
        accessToken = null
        refreshToken = null
    }

    private fun create(): SharedPreferences {
        fun build(): SharedPreferences = EncryptedSharedPreferences.create(
            context,
            FILE,
            MasterKey.Builder(context).setKeyScheme(MasterKey.KeyScheme.AES256_GCM).build(),
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM,
        )
        return try {
            build()
        } catch (e: Exception) {
            // Keystore key lost/corrupted (e.g. after a restore): start clean, user just signs in again.
            context.deleteSharedPreferences(FILE)
            build()
        }
    }

    private companion object {
        const val FILE = "nexus_secure_session"
        const val KEY_REFRESH = "refresh_token"
    }
}
