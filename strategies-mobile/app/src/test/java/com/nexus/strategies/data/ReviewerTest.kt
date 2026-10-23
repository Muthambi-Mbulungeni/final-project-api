package com.nexus.strategies.data

import kotlinx.serialization.decodeFromString
import kotlinx.serialization.json.Json
import org.junit.Assert.assertEquals
import org.junit.Test

class ReviewerTest {
    private val json = Json { ignoreUnknownKeys = true; coerceInputValues = true; isLenient = true }
    private fun doc(by: String?) = DocumentDto(id = "d", reviewedByName = by)

    @Test fun credit_nameAndDate() =
        assertEquals("Marco Alexandre, 06 Sept 2026", reviewerCredit("Marco Alexandre", "2026-09-06T10:15:00.1234567Z"))

    @Test fun credit_nameWithoutReadableDate_isJustTheName() {
        assertEquals("Marco", reviewerCredit("Marco", null))
        assertEquals("Marco", reviewerCredit("  Marco ", "not a date"))
    }

    @Test fun credit_noName_fallsBackToYourConsultant() {
        assertEquals("Your consultant", reviewerCredit(null, "2026-09-06T10:15:00Z"))
        assertEquals("Your consultant", reviewerCredit("   ", "2026-09-06T10:15:00Z"))
    }

    @Test fun document_olderApiWithoutTheFields_stillParses() {
        val d = json.decodeFromString<DocumentDto>("""{"id":"1","status":"Rejected","reviewComment":"Blurry"}""")
        assertEquals("Your consultant", d.reviewerCredit)
    }

    @Test fun document_newApi_readsBothFields() {
        val d = json.decodeFromString<DocumentDto>(
            """{"id":"1","status":"Rejected","reviewedByName":"Herman","reviewedAt":"2026-10-02T08:00:00Z"}"""
        )
        assertEquals("Herman, 02 Oct 2026", d.reviewerCredit)
    }

    @Test fun requestedBy_oneReviewer_usesTheirName() {
        assertEquals("Herman", requestedBy(listOf(doc("Herman"))))
        assertEquals("Herman", requestedBy(listOf(doc("Herman"), doc("Herman"))))
    }

    @Test fun requestedBy_differentOrMissingReviewers_fallsBack() {
        assertEquals("Your consultant", requestedBy(listOf(doc("Herman"), doc("Marco"))))
        assertEquals("Your consultant", requestedBy(listOf(doc("Herman"), doc(null))))
        assertEquals("Your consultant", requestedBy(listOf(doc(null))))
    }
}
