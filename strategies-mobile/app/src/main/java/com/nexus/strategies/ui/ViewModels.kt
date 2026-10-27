package com.nexus.strategies.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.nexus.strategies.NexusApp
import com.nexus.strategies.data.ApiException
import com.nexus.strategies.data.AuthResponse
import com.nexus.strategies.data.CaseDto
import com.nexus.strategies.data.DocumentDto
import com.nexus.strategies.data.DocumentTypeDto
import com.nexus.strategies.data.PhotoPrep
import com.nexus.strategies.data.photoFileName
import com.nexus.strategies.data.NotificationDto
import android.net.Uri
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.time.LocalDateTime

sealed interface AuthState {
    data object Restoring : AuthState
    data class SignedOut(val notice: String? = null) : AuthState
    data class SignedIn(val userId: String, val name: String) : AuthState
}

class AuthViewModel : ViewModel() {
    private val api = NexusApp.api
    private val tokens = NexusApp.tokens

    private val _state = MutableStateFlow<AuthState>(AuthState.Restoring)
    val state: StateFlow<AuthState> = _state.asStateFlow()

    private val _busy = MutableStateFlow(false)
    val busy: StateFlow<Boolean> = _busy.asStateFlow()

    private val _error = MutableStateFlow<String?>(null)
    val error: StateFlow<String?> = _error.asStateFlow()

    init {
        // A refresh that returns 401 anywhere in the app ends the session.
        viewModelScope.launch {
            api.sessionExpired.collect {
                if (_state.value is AuthState.SignedIn) {
                    _state.value = AuthState.SignedOut(ApiException(ApiException.Kind.SessionExpired).userMessage())
                }
            }
        }
        restore()
    }

    private fun restore() {
        viewModelScope.launch {
            if (tokens.refreshToken == null) {
                _state.value = AuthState.SignedOut()
                return@launch
            }
            try {
                val a = api.restoreSession()
                _state.value = if (a != null) signedIn(a) else AuthState.SignedOut()
            } catch (e: ApiException) {
                _state.value = AuthState.SignedOut(e.userMessage())
            }
        }
    }

    fun login(email: String, password: String) {
        // A new attempt replaces any earlier notice ("You have been signed out."), so only one message shows.
        if (_state.value is AuthState.SignedOut) _state.value = AuthState.SignedOut()
        if (email.isBlank() || password.isBlank()) {
            _error.value = "Enter your email and password."
            return
        }
        viewModelScope.launch {
            _busy.value = true
            _error.value = null
            try {
                _state.value = signedIn(api.login(email, password))
            } catch (e: ApiException) {
                _error.value = e.userMessage()
            } finally {
                _busy.value = false
            }
        }
    }

    fun signOut() {
        viewModelScope.launch {
            api.signOut()
            _error.value = null
            _state.value = AuthState.SignedOut("You have been signed out.")
        }
    }

    private fun signedIn(a: AuthResponse) = AuthState.SignedIn(a.userId, a.displayName.ifBlank { "there" })
}

/** A photo already shrunk to a JPEG the API accepts, waiting for the client to press Send. */
class PreparedPhoto(val bytes: ByteArray)

data class HomeUi(
    val loading: Boolean = true,
    val error: String? = null,
    val case: CaseDto? = null,
    val documents: List<DocumentDto> = emptyList(),
    val notifications: List<NotificationDto> = emptyList(),
    val unread: Int = 0,
    val notificationsError: String? = null,
    val docTypes: List<DocumentTypeDto> = emptyList(),
    val docTypesError: String? = null,
    val photo: PreparedPhoto? = null,
    val uploadBusy: Boolean = false,
    val uploadMessage: String? = null,
    val uploadError: String? = null,
)

class HomeViewModel : ViewModel() {
    private val api = NexusApp.api
    private var userId: String = ""

    private val _ui = MutableStateFlow(HomeUi())
    val ui: StateFlow<HomeUi> = _ui.asStateFlow()

    fun load(userId: String) {
        this.userId = userId
        refresh()
    }

    fun refresh() {
        if (userId.isEmpty()) return
        viewModelScope.launch {
            _ui.update { it.copy(loading = true, error = null, notificationsError = null) }
            try {
                val cases = api.cases(userId).sortedByDescending { it.createdAt.orEmpty() }
                // The web shows the most recently opened case; prefer one that isn't archived.
                val current = cases.firstOrNull { !it.isArchived } ?: cases.firstOrNull()
                val docs = current?.let { api.documents(it.id) } ?: emptyList()
                _ui.update { it.copy(case = current, documents = docs) }
            } catch (e: ApiException) {
                _ui.update { it.copy(error = e.userMessage()) }
            }
            try {
                val types = api.documentTypes()
                _ui.update { it.copy(docTypes = types, docTypesError = null) }
            } catch (e: ApiException) {
                _ui.update { it.copy(docTypesError = e.userMessage()) }
            }
            try {
                val n = api.notifications()
                _ui.update { it.copy(notifications = n.data, unread = n.unreadCount) }
            } catch (e: ApiException) {
                _ui.update { it.copy(notificationsError = e.userMessage()) }
            }
            _ui.update { it.copy(loading = false) }
        }
    }

    // ---------- Photo upload ----------

    /** A photo was taken or chosen: shrink it off the main thread, then show it for confirmation. */
    fun onPhoto(uri: Uri?, deleteAfter: Boolean = false) {
        if (uri == null || _ui.value.uploadBusy) return
        viewModelScope.launch {
            _ui.update { it.copy(uploadBusy = true, uploadError = null, uploadMessage = null) }
            val resolver = NexusApp.app.contentResolver
            val result = withContext(Dispatchers.IO) {
                // Catches OutOfMemoryError too: a huge photo must show an error, not crash the app.
                val r = runCatching { PhotoPrep.prepare(resolver, uri) }
                // The camera's temporary file is no longer needed once it has been read.
                if (deleteAfter) runCatching { resolver.delete(uri, null, null) }
                r
            }
            _ui.update { s ->
                result.fold(
                    onSuccess = { s.copy(photo = PreparedPhoto(it), uploadBusy = false) },
                    onFailure = { s.copy(uploadBusy = false, uploadError = "We couldn't read that photo. Try another one.") },
                )
            }
        }
    }

    fun discardPhoto() = _ui.update { it.copy(photo = null, uploadError = null) }

    fun showUploadError(message: String) = _ui.update { it.copy(uploadError = message, uploadMessage = null) }

    fun sendPhoto(documentType: DocumentTypeDto) {
        val s = _ui.value
        val photo = s.photo ?: return
        val case = s.case ?: return
        if (s.uploadBusy) return
        viewModelScope.launch {
            _ui.update { it.copy(uploadBusy = true, uploadError = null, uploadMessage = null) }
            try {
                api.sendDocument(case.id, documentType.id, photoFileName(documentType.name, LocalDateTime.now()), photo.bytes)
                _ui.update { it.copy(photo = null, uploadMessage = "Sent. Your consultant will review it.") }
                // Show the new copy straight away; a failure here must not look like a failed upload.
                runCatching { api.documents(case.id) }.onSuccess { docs -> _ui.update { it.copy(documents = docs) } }
            } catch (e: ApiException) {
                // The photo is kept so the client can simply press Send again.
                _ui.update { it.copy(uploadError = e.userMessage()) }
            } finally {
                _ui.update { it.copy(uploadBusy = false) }
            }
        }
    }

    fun markRead(n: NotificationDto) {
        if (n.isRead) return
        _ui.update { s ->
            s.copy(
                notifications = s.notifications.map { if (it.id == n.id) it.copy(isRead = true) else it },
                unread = (s.unread - 1).coerceAtLeast(0),
            )
        }
        viewModelScope.launch {
            // Safe to call twice; a 403 (our own message) or network blip doesn't matter here.
            try { api.markRead(n.id) } catch (_: ApiException) { }
        }
    }
}
