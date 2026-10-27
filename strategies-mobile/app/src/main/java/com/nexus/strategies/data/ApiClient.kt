package com.nexus.strategies.data

import com.nexus.strategies.data.ApiException.Kind
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.withContext
import kotlinx.serialization.decodeFromString
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import okhttp3.Authenticator
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.Response
import okhttp3.Route
import java.io.IOException
import java.util.concurrent.TimeUnit

class ApiClient(private val tokens: TokenStore, baseUrl: String) {

    private val base = if (baseUrl.endsWith("/")) baseUrl else "$baseUrl/"
    private val json = Json { ignoreUnknownKeys = true; coerceInputValues = true; isLenient = true }
    private val jsonType = "application/json; charset=utf-8".toMediaType()

    /** Emits when a refresh fails with 401: the session is over and the UI must go to login. */
    val sessionExpired = MutableSharedFlow<Unit>(extraBufferCapacity = 1)

    // Used for login / refresh / revoke (no Bearer header, no authenticator).
    private val plain = OkHttpClient.Builder()
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(20, TimeUnit.SECONDS)
        .build()

    // Only ONE refresh may run at a time: the API rotates refresh tokens, so a second refresh
    // using a token the first just revoked would log the user out. This lock makes later 401s wait.
    private val refreshLock = Any()

    private val authed = plain.newBuilder()
        .addInterceptor { chain ->
            val b = chain.request().newBuilder()
            tokens.accessToken?.let { b.header("Authorization", "Bearer $it") }
            chain.proceed(b.build())
        }
        .authenticator(object : Authenticator {
            override fun authenticate(route: Route?, response: Response): Request? = reauthenticate(response)
        })
        .build()

    // ---------- Auth ----------

    // On IO as a whole: acceptClientOnly() makes a blocking revoke call for staff accounts,
    // which fails on the main thread (NetworkOnMainThreadException, hidden by runCatching).
    suspend fun login(email: String, password: String): AuthResponse = withContext(Dispatchers.IO) {
        val req = Request.Builder().url(base + "api/auth/login")
            .post(json.encodeToString(LoginRequest(email.trim(), password)).toRequestBody(jsonType))
            .build()
        val auth = json.decodeFromString<AuthResponse>(exec(plain, req))
        acceptClientOnly(auth)
    }

    /** Silent sign-in on app start using the stored refresh token. Returns null if none is stored. */
    suspend fun restoreSession(): AuthResponse? = withContext(Dispatchers.IO) {
        val rt = tokens.refreshToken ?: return@withContext null
        val auth = synchronized(refreshLock) { refreshBlocking(rt) }
        if (auth == null) {
            tokens.clear()
            throw ApiException(Kind.SessionExpired)
        }
        acceptClientOnly(auth, blocking = true)
    }

    /** Always clears local tokens, even if the server call fails (offline sign-out must still work). */
    suspend fun signOut() {
        val rt = tokens.refreshToken
        tokens.clear()
        if (rt != null) runCatching { revoke(rt) }
    }

    private suspend fun revoke(refreshToken: String) {
        val req = Request.Builder().url(base + "api/auth/revoke")
            .post(json.encodeToString(RefreshRequest(refreshToken)).toRequestBody(jsonType))
            .build()
        exec(plain, req)
    }

    private fun acceptClientOnly(auth: AuthResponse, blocking: Boolean = false): AuthResponse {
        if (!auth.role.equals("Client", ignoreCase = true)) {
            // Staff accounts belong on the web portal: don't keep their session, and revoke it.
            runCatching {
                val req = Request.Builder().url(base + "api/auth/revoke")
                    .post(json.encodeToString(RefreshRequest(auth.refreshToken)).toRequestBody(jsonType))
                    .build()
                plain.newCall(req).execute().close()
            }
            tokens.clear()
            throw ApiException(Kind.NotClient)
        }
        tokens.save(auth)
        return auth
    }

    /** Blocking refresh. Returns null on 401 (session over). Throws ApiException on other failures. */
    private fun refreshBlocking(refreshToken: String): AuthResponse? {
        val req = Request.Builder().url(base + "api/auth/refresh")
            .post(json.encodeToString(RefreshRequest(refreshToken)).toRequestBody(jsonType))
            .build()
        try {
            plain.newCall(req).execute().use { resp ->
                return when {
                    resp.code == 401 -> null
                    resp.isSuccessful -> json.decodeFromString<AuthResponse>(resp.body?.string().orEmpty())
                    resp.code in 502..504 -> throw ApiException(Kind.Network)
                    else -> throw ApiException(Kind.Server)
                }
            }
        } catch (e: ApiException) {
            throw e
        } catch (e: IOException) {
            throw ApiException(Kind.Network)
        }
    }

    /** OkHttp calls this on a 401: refresh once, retry once. */
    private fun reauthenticate(response: Response): Request? {
        val path = response.request.url.encodedPath
        if (path.endsWith("/auth/login") || path.endsWith("/auth/refresh")) return null
        if (priorResponses(response) >= 2) return null

        synchronized(refreshLock) {
            val sent = response.request.header("Authorization")?.removePrefix("Bearer ")
            val current = tokens.accessToken
            // Another request already refreshed while we waited for the lock: just retry with the new token.
            if (current != null && current != sent) {
                return response.request.newBuilder().header("Authorization", "Bearer $current").build()
            }
            val rt = tokens.refreshToken ?: return expire()
            val fresh = refreshBlocking(rt) ?: return expire()
            if (!fresh.role.equals("Client", ignoreCase = true)) return expire()
            tokens.save(fresh)
            return response.request.newBuilder().header("Authorization", "Bearer ${fresh.accessToken}").build()
        }
    }

    private fun expire(): Request? {
        tokens.clear()
        sessionExpired.tryEmit(Unit)
        return null
    }

    private fun priorResponses(r: Response): Int {
        var n = 1
        var p = r.priorResponse
        while (p != null) { n++; p = p.priorResponse }
        return n
    }

    // ---------- Data ----------

    suspend fun cases(clientId: String): List<CaseDto> {
        val body = get("api/cases/client/$clientId?page=1&pageSize=10")
        return json.decodeFromString<PagedResponse<CaseDto>>(body).data
    }

    suspend fun documents(caseId: String): List<DocumentDto> {
        val body = get("api/documents?caseId=$caseId&page=1&pageSize=50")
        return documentsForClient(json.decodeFromString<PagedResponse<DocumentDto>>(body).data)
    }

    suspend fun notifications(): NotificationsResponse =
        json.decodeFromString(get("api/notifications?page=1&pageSize=20"))

    suspend fun documentTypes(): List<DocumentTypeDto> =
        json.decodeFromString(get("api/DocumentTypes"))

    // A photo is a few MB over a mobile connection: allow longer than the 20 s used for small JSON calls.
    private val uploader by lazy {
        authed.newBuilder().writeTimeout(60, TimeUnit.SECONDS).readTimeout(60, TimeUnit.SECONDS).build()
    }

    /**
     * Sends a photo to the case in two steps, like the web portal: upload the file (the API returns its stored
     * name), then register it as a document of [documentTypeId]. It appears as "Awaiting review".
     */
    suspend fun sendDocument(caseId: String, documentTypeId: String, fileName: String, jpeg: ByteArray) {
        val form = MultipartBody.Builder().setType(MultipartBody.FORM)
            .addFormDataPart("file", fileName, jpeg.toRequestBody("image/jpeg".toMediaType()))
            .build()
        val uploaded = json.decodeFromString<UploadResponse>(
            exec(uploader, Request.Builder().url(base + "api/Documents/upload").post(form).build())
        )
        val record = CreateDocumentRequest(caseId, documentTypeId, fileName, uploaded.filePath)
        exec(authed, Request.Builder().url(base + "api/Documents")
            .post(json.encodeToString(record).toRequestBody(jsonType)).build())
    }

    suspend fun markRead(id: String) {
        val req = Request.Builder().url(base + "api/notifications/$id/read")
            .patch("".toRequestBody(jsonType)).build()
        exec(authed, req)
    }

    private suspend fun get(path: String): String =
        exec(authed, Request.Builder().url(base + path).get().build())

    // ---------- Plumbing ----------

    /** Runs a call off the main thread and turns every failure into an ApiException with safe wording. */
    private suspend fun exec(client: OkHttpClient, req: Request): String = withContext(Dispatchers.IO) {
        try {
            client.newCall(req).execute().use { resp ->
                val body = resp.body?.string().orEmpty()
                when {
                    resp.isSuccessful -> body
                    resp.code == 401 -> throw ApiException(
                        if (req.url.encodedPath.endsWith("/auth/login")) Kind.Unauthorized else Kind.SessionExpired
                    )
                    resp.code == 403 -> throw ApiException(Kind.Forbidden)
                    resp.code == 429 -> throw ApiException(Kind.RateLimited)
                    resp.code == 400 || resp.code == 413 -> throw ApiException(Kind.Rejected)
                    resp.code in 502..504 -> throw ApiException(Kind.Network)
                    else -> throw ApiException(Kind.Server)
                }
            }
        } catch (e: ApiException) {
            throw e
        } catch (e: IOException) {
            throw ApiException(Kind.Network)
        } catch (e: kotlinx.serialization.SerializationException) {
            throw ApiException(Kind.Server)
        }
    }
}
