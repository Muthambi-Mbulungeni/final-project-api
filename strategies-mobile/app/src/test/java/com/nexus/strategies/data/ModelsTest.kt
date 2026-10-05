package com.nexus.strategies.data

import kotlinx.serialization.decodeFromString
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonPrimitive
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class ModelsTest {
    private val json = Json { ignoreUnknownKeys = true; coerceInputValues = true; isLenient = true }

    // ---- Stage wording and parsing (API may send the name or the index 0-4) ----

    @Test fun stage_parsesByName_ignoringCase() {
        assertEquals(Stage.UnderReview, Stage.parse("UnderReview"))
        assertEquals(Stage.UnderReview, Stage.parse("underreview"))
    }

    @Test fun stage_parsesByIndex() {
        assertEquals(Stage.InitialIntake, Stage.parse("0"))
        assertEquals(Stage.SubmittedToAuthority, Stage.parse("3"))
        assertEquals(Stage.FinalDecision, Stage.parse("4"))
    }

    @Test fun stage_unknownOrMissing_fallsBackToInitialIntake() {
        assertEquals(Stage.InitialIntake, Stage.parse(null))
        assertEquals(Stage.InitialIntake, Stage.parse("nonsense"))
        assertEquals(Stage.InitialIntake, Stage.parse("99"))
    }

    @Test fun stage_labelsMatchTheAgreedWording() {
        assertEquals("Initial intake", Stage.InitialIntake.label)
        assertEquals("Document gathering", Stage.DocumentGathering.label)
        assertEquals("Under review", Stage.UnderReview.label)
        assertEquals("Submitted to authority", Stage.SubmittedToAuthority.label)
        assertEquals("Final decision", Stage.FinalDecision.label)
        assertEquals(5, Stage.entries.size)
    }

    // ---- Case display rules ----

    @Test fun case_referenceIsFirst8CharsUpperCase() {
        val c = CaseDto(id = "3fa85f64-5717-4562-b3fc-2c963f66afa6")
        assertEquals("3FA85F64", c.reference)
    }

    @Test fun case_placeholdersAreHidden() {
        val c = CaseDto(id = "x", visaCategoryName = "N/A", consultantName = "Unassigned")
        assertNull(c.visaCategoryOrNull)
        assertNull(c.consultantOrNull)
        val d = CaseDto(id = "x", visaCategoryName = "Work Visa", consultantName = "Thandi")
        assertEquals("Work Visa", d.visaCategoryOrNull)
        assertEquals("Thandi", d.consultantOrNull)
    }

    // ---- Documents shown to the client (same rule as the web portal) ----

    private fun doc(id: String, type: String?, status: String, at: String, archived: Boolean = false) =
        DocumentDto(id = id, documentTypeId = type, status = status, uploadedAt = at, isArchived = archived)

    @Test fun documents_rejectedCopyWithNewerCopyOfSameType_isHidden() {
        val shown = documentsForClient(listOf(
            doc("old", "medical", "Rejected", "2026-10-01T10:00:00.1234567Z"),
            doc("new", "medical", "Pending", "2026-10-02T09:00:00.5Z"),
            doc("passport", "passport", "Approved", "2026-09-30T10:00:00Z"),
        ))
        assertEquals(listOf("new", "passport"), shown.map { it.id })
    }

    @Test fun documents_comparesTimesNotText() {
        // As text "...00Z" sorts after "...00.5Z", which would wrongly keep the old copy.
        val shown = documentsForClient(listOf(
            doc("old", "medical", "Rejected", "2026-10-01T10:00:00Z"),
            doc("new", "medical", "Pending", "2026-10-01T10:00:00.5Z"),
        ))
        assertEquals(listOf("new"), shown.map { it.id })
    }

    @Test fun documents_rejectedCopyStays_whenNoNewerCopyOfSameType() {
        val shown = documentsForClient(listOf(
            doc("rejected", "medical", "Rejected", "2026-10-02T10:00:00Z"),
            doc("olderSameType", "medical", "Approved", "2026-10-01T10:00:00Z"),
            doc("newerOtherType", "passport", "Pending", "2026-10-03T10:00:00Z"),
            doc("noType", null, "Rejected", "2026-09-01T10:00:00Z"),
        ))
        assertEquals(setOf("rejected", "olderSameType", "newerOtherType", "noType"), shown.map { it.id }.toSet())
    }

    @Test fun documents_archivedLeftOut_andOrderedForTheClient() {
        val shown = documentsForClient(listOf(
            doc("approved", "a", "Approved", "2026-10-05T10:00:00Z"),
            doc("archived", "b", "Rejected", "2026-10-05T10:00:00Z", archived = true),
            doc("pendingOld", "c", "Pending", "2026-10-01T10:00:00Z"),
            doc("pendingNew", "d", "Pending", "2026-10-03T10:00:00Z"),
            doc("rejected", "e", "Rejected", "2026-09-01T10:00:00Z"),
        ))
        assertEquals(listOf("rejected", "pendingNew", "pendingOld", "approved"), shown.map { it.id })
    }

    @Test fun documents_typeIdReadFromJson() {
        val d = json.decodeFromString<DocumentDto>("""{"id":"a","documentTypeId":"7","status":"Rejected"}""")
        assertEquals("7", d.documentTypeId)
    }

    @Test fun case_stageAcceptsStringOrNumberFromJson() {
        val byName = json.decodeFromString<CaseDto>("""{"id":"a","stage":"UnderReview"}""")
        val byIndex = json.decodeFromString<CaseDto>("""{"id":"a","stage":2}""")
        assertEquals(Stage.UnderReview, byName.stageEnum)
        assertEquals(Stage.UnderReview, byIndex.stageEnum)
        assertEquals(Stage.SubmittedToAuthority, CaseDto(id = "a", stage = JsonPrimitive(3)).stageEnum)
    }

    // ---- Notifications: real API (body/sentAt) and old mock (message/createdAt) ----

    @Test fun notification_readsRealApiShape() {
        val n = json.decodeFromString<NotificationDto>(
            """{"id":"1","caseId":"abcdef12-0000","body":"Hello","isRead":false,"sentAt":"2026-09-30T10:00:00"}"""
        )
        assertEquals("Hello", n.text)
        assertEquals("2026-09-30T10:00:00", n.whenIso)
        assertEquals("ABCDEF12", n.caseReference)
    }

    @Test fun notification_fallsBackToMockShape() {
        val n = json.decodeFromString<NotificationDto>(
            """{"id":"1","message":"Old style","createdAt":"2026-09-30T10:00:00"}"""
        )
        assertEquals("Old style", n.text)
        assertEquals("2026-09-30T10:00:00", n.whenIso)
    }

    @Test fun notificationsResponse_readsUnreadCount() {
        val r = json.decodeFromString<NotificationsResponse>("""{"unreadCount":2,"data":[]}""")
        assertEquals(2, r.unreadCount)
    }

    // ---- Document status wording (same as the web badges) ----

    @Test fun docStatus_mapsApiValuesToClientWording() {
        assertEquals("Awaiting review", DocStatus.parse("Pending").label)
        assertEquals("Approved", DocStatus.parse("Approved").label)
        assertEquals("Needs replacing", DocStatus.parse("Rejected").label)
        assertEquals(DocStatus.Awaiting, DocStatus.parse("something new"))
        assertEquals(DocStatus.NeedsReplacing, DocumentDto(id = "1", status = "rejected").state)
    }

    // ---- Dates ----

    @Test fun formatDate_formatsIso() {
        assertEquals("30 Sept 2026", formatDate("2026-09-30T10:00:00"))
        assertEquals("30 Sept 2026", formatDate("2026-09-30T10:00:00.1234567Z"))
        assertEquals("06 Oct 2026", formatDate("2026-10-06T08:00:00"))
    }

    @Test fun formatDate_badInputIsNull() {
        assertNull(formatDate(null))
        assertNull(formatDate("not a date"))
    }

    // ---- Error wording shared with the web portal ----

    @Test fun errors_useTheAgreedWording() {
        assertEquals("You don't have access to this.", ApiException(ApiException.Kind.Forbidden).userMessage())
        assertEquals("Something went wrong on our side. Please try again.", ApiException(ApiException.Kind.Server).userMessage())
        assertEquals(
            "We can't reach the service right now. Check your connection and try again.",
            ApiException(ApiException.Kind.Network).userMessage(),
        )
        assertEquals("Too many attempts, wait a minute and try again.", ApiException(ApiException.Kind.RateLimited).userMessage())
        assertEquals("This app is for clients. Staff, please use the web portal.", ApiException(ApiException.Kind.NotClient).userMessage())
    }
}
