package com.nexus.strategies

import android.app.Application
import com.nexus.strategies.data.ApiClient
import com.nexus.strategies.data.TokenStore

class NexusApp : Application() {
    override fun onCreate() {
        super.onCreate()
        tokens = TokenStore(this)
        api = ApiClient(tokens, BuildConfig.API_BASE_URL)
    }

    companion object {
        lateinit var tokens: TokenStore
            private set
        lateinit var api: ApiClient
            private set
    }
}
