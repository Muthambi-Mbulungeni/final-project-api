package com.nexus.strategies.data

import kotlinx.serialization.Serializable
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter
import kotlin.math.max
import kotlin.math.roundToInt

// ---- Photo upload: the rules the API enforces (DocumentsController.UploadDocument) and our own limits ----

/** The API rejects anything over 5 MB. We aim lower because the multipart wrapper adds a few hundred bytes. */
const val TARGET_UPLOAD_BYTES = 4_500_000

/** Longest side of the photo we send. 2048 px keeps small print on a form readable at a fraction of the size. */
const val MAX_EDGE_PX = 2048

/**
 * Power-of-two `inSampleSize` for decoding: the biggest factor that still leaves the longest side at
 * [maxEdge] or more, so we never decode a 12 MP photo in full just to shrink it.
 */
fun sampleSizeFor(width: Int, height: Int, maxEdge: Int = MAX_EDGE_PX): Int {
    var sample = 1
    while (max(width, height) / (sample * 2) >= maxEdge) sample *= 2
    return sample
}

/** The size to scale to so the longest side is at most [maxEdge], keeping the aspect ratio. Never enlarges. */
fun scaledSize(width: Int, height: Int, maxEdge: Int = MAX_EDGE_PX): Pair<Int, Int> {
    val longest = max(width, height)
    if (longest <= maxEdge) return width to height
    val f = maxEdge.toDouble() / longest
    return max(1, (width * f).roundToInt()) to max(1, (height * f).roundToInt())
}

private val STAMP = DateTimeFormatter.ofPattern("yyyyMMdd-HHmm")

/** "Passport copy" at 14:05 on 24 Oct 2026 -> `passport-copy-20261024-1405.jpg`. Always ends in .jpg (the API checks the extension). */
fun photoFileName(documentTypeName: String, at: LocalDateTime): String {
    val slug = documentTypeName.lowercase()
        .replace(Regex("[^a-z0-9]+"), "-")
        .trim('-')
        .take(40)
        .ifEmpty { "document" }
    return "$slug-${at.format(STAMP)}.jpg"
}

/** "1.2 MB" / "480 KB", for the "Photo ready" line. */
fun sizeLabel(bytes: Int): String =
    if (bytes >= 1_000_000) "%.1f MB".format(java.util.Locale.ENGLISH, bytes / 1_000_000.0)
    else "${max(1, bytes / 1000)} KB"

// ---- Wire types ----

@Serializable
data class DocumentTypeDto(val id: String, val name: String, val description: String? = null)

/** Response of `POST /api/documents/upload`. [filePath] is the stored name to pass to `POST /api/documents`. */
@Serializable
data class UploadResponse(val filePath: String, val fileName: String = "")

@Serializable
data class CreateDocumentRequest(
    val caseId: String,
    val documentTypeId: String,
    val fileName: String,
    val filePath: String,
)
