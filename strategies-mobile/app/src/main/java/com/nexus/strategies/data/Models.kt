package com.nexus.strategies.data

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.JsonPrimitive
import java.io.IOException
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale

@Serializable data class LoginRequest(val email: String, val password: String)
@Serializable data class RefreshRequest(val refreshToken: String)

@Serializable
data class AuthResponse(
    val accessToken: String,
    val refreshToken: String,
    val role: String,
    val displayName: String = "",
    val userId: String,
)

@Serializable
data class PagedResponse<T>(
    val data: List<T> = emptyList(),
    val totalRecords: Int = 0,
    val totalPages: Int = 0,
    val currentPage: Int = 1,
)

@Serializable
data class CaseDto(
    val id: String,
    // The API may send the stage name ("UnderReview") or its index (2), so keep it raw.
    val stage: JsonPrimitive? = null,
    val createdAt: String? = null,
    val lastUpdated: String? = null,
    val visaCategoryName: String? = null,
    val consultantName: String? = null,
    val isArchived: Boolean = false,
) {
    /** Reference shown to the client everywhere: first 8 characters of the id, upper case. */
    val reference: String get() = id.take(8).uppercase()
    val stageEnum: Stage get() = Stage.parse(stage?.content)
    /** "N/A" and "Unassigned" are placeholders from the API meaning "not set". */
    val visaCategoryOrNull: String? get() = visaCategoryName?.takeIf { it.isNotBlank() && it != "N/A" }
    val consultantOrNull: String? get() = consultantName?.takeIf { it.isNotBlank() && it != "Unassigned" }
}

@Serializable
data class DocumentDto(
    val id: String,
    val documentTypeName: String = "Document",
    val fileName: String = "",
    val status: String = "Pending",
    val reviewComment: String? = null,
    val uploadedAt: String? = null,
    val isArchived: Boolean = false,
) {
    val state: DocStatus get() = DocStatus.parse(status)
}

/** Client-facing wording for a document's review status (matches the web portal). */
enum class DocStatus(val label: String) {
    Awaiting("Awaiting review"),
    Approved("Approved"),
    NeedsReplacing("Needs replacing");

    companion object {
        fun parse(raw: String?): DocStatus = when (raw?.trim()?.lowercase()) {
            "approved" -> Approved
            "rejected" -> NeedsReplacing
            else -> Awaiting // "Pending" and anything unknown
        }
    }
}

@Serializable
data class NotificationDto(
    val id: String,
    val caseId: String? = null,
    val body: String? = null,        // real API
    val message: String? = null,     // old mock API
    val isRead: Boolean = false,
    val sentAt: String? = null,      // real API
    val createdAt: String? = null,   // old mock API
) {
    val text: String get() = body ?: message ?: ""
    val whenIso: String? get() = sentAt ?: createdAt
    val caseReference: String? get() = caseId?.take(8)?.uppercase()
}

@Serializable
data class NotificationsResponse(
    val unreadCount: Int = 0,
    val data: List<NotificationDto> = emptyList(),
)

enum class Stage(val label: String, val hint: String) {
    InitialIntake("Initial intake", "We assess your enquiry and eligibility."),
    DocumentGathering("Document gathering", "Upload the documents on your checklist."),
    UnderReview("Under review", "Your consultant checks your complete file."),
    SubmittedToAuthority("Submitted to authority", "Your application is lodged with the authority for a decision."),
    FinalDecision("Final decision", "The outcome is received and shared with you.");

    companion object {
        fun parse(raw: String?): Stage {
            val r = raw?.trim().orEmpty()
            r.toIntOrNull()?.let { return entries.getOrElse(it) { InitialIntake } }
            return entries.firstOrNull { it.name.equals(r, ignoreCase = true) } ?: InitialIntake
        }
    }
}

/** Formats an ISO timestamp from the API like the web does: "06 Sept 2026". Returns null if it can't be read. */
fun formatDate(iso: String?): String? = runCatching {
    val d = LocalDateTime.parse(iso!!.substringBefore('.').removeSuffix("Z"))
    d.format(DateTimeFormatter.ofPattern("dd MMM yyyy", Locale.ENGLISH)).replace("Sep ", "Sept ")
}.getOrNull()

/** Everything the UI needs to know about a failed call. Never carries raw server text. */
class ApiException(val kind: Kind, message: String = kind.name) : IOException(message) {
    enum class Kind { Unauthorized, SessionExpired, Forbidden, RateLimited, Server, Network, NotClient }

    fun userMessage(): String = when (kind) {
        Kind.Unauthorized -> "Incorrect email or password."
        Kind.SessionExpired -> "Your session has expired. Please sign in again."
        Kind.Forbidden -> "You don't have access to this."
        Kind.RateLimited -> "Too many attempts, wait a minute and try again."
        Kind.Server -> "Something went wrong on our side. Please try again."
        Kind.Network -> "We can't reach the service right now. Check your connection and try again."
        Kind.NotClient -> "This app is for clients. Staff please use the web portal."
    }
}
