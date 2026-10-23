package com.nexus.strategies.data

import kotlinx.serialization.decodeFromString
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import java.time.LocalDateTime

class PhotosTest {
    private val json = Json { ignoreUnknownKeys = true; coerceInputValues = true; isLenient = true }

    // ---- Decoding and scaling ----

    @Test fun sampleSize_smallPhoto_isNotReduced() {
        assertEquals(1, sampleSizeFor(1600, 1200))
        assertEquals(1, sampleSizeFor(4000, 3000)) // 12 MP: halving would drop below 2048 px
    }

    @Test fun sampleSize_hugePhoto_keepsLongestSideAtLeastMaxEdge() {
        assertEquals(2, sampleSizeFor(8000, 6000))  // 4000 px after decode
        assertEquals(4, sampleSizeFor(12000, 9000)) // 3000 px after decode
        for (side in listOf(2049, 4095, 4096, 8191, 16000)) {
            val s = sampleSizeFor(side, side / 2)
            assertTrue("side=$side sample=$s", side / s >= MAX_EDGE_PX)
        }
    }

    @Test fun scaledSize_neverEnlarges_andKeepsAspectRatio() {
        assertEquals(800 to 600, scaledSize(800, 600))
        assertEquals(2048 to 1536, scaledSize(4000, 3000))
        assertEquals(1536 to 2048, scaledSize(3000, 4000)) // portrait
        assertEquals(2048 to 1, scaledSize(4000, 1))       // never collapses to zero
    }

    // ---- File name: the API checks the extension, so it must stay .jpg ----

    @Test fun fileName_isSlugPlusTimestamp_andAlwaysJpg() {
        val at = LocalDateTime.of(2026, 10, 24, 14, 5)
        assertEquals("passport-copy-20261024-1405.jpg", photoFileName("Passport copy", at))
        assertEquals("proof-of-address-20261024-1405.jpg", photoFileName("  Proof of address! ", at))
    }

    @Test fun fileName_unusableTypeName_fallsBackToDocument() {
        val at = LocalDateTime.of(2026, 1, 2, 3, 4)
        assertEquals("document-20260102-0304.jpg", photoFileName("???", at))
        assertEquals("document-20260102-0304.jpg", photoFileName("", at))
    }

    @Test fun fileName_isShortEnoughForTheApi() {
        // API limit is 500 characters; the slug is capped at 40.
        assertTrue(photoFileName("x".repeat(1000), LocalDateTime.of(2026, 1, 2, 3, 4)).length < 100)
    }

    @Test fun sizeLabel_showsKbOrMb() {
        assertEquals("480 KB", sizeLabel(480_000))
        assertEquals("1.2 MB", sizeLabel(1_200_000))
        assertEquals("1 KB", sizeLabel(10))
    }

    @Test fun targetIsBelowTheApiLimit() {
        assertTrue(TARGET_UPLOAD_BYTES < 5 * 1024 * 1024)
    }

    // ---- Wire format (matches DocumentsController / DocumentTypesController) ----

    @Test fun documentTypes_parseFromApi_ignoringExtraFields() {
        val body = """[{"id":"11111111-1111-1111-1111-111111111111","name":"Passport","description":"Photo page"},
                        {"id":"22222222-2222-2222-2222-222222222222","name":"Police clearance","description":null}]"""
        val types = json.decodeFromString<List<DocumentTypeDto>>(body)
        assertEquals(listOf("Passport", "Police clearance"), types.map { it.name })
        assertEquals(null, types[1].description)
    }

    @Test fun uploadResponse_readsStoredNameFromFilePath() {
        val r = json.decodeFromString<UploadResponse>(
            """{"message":"File uploaded successfully.","fileName":"scan.jpg","filePath":"3f2b8c1e-0000-4000-8000-123456789abc.jpg"}"""
        )
        assertEquals("3f2b8c1e-0000-4000-8000-123456789abc.jpg", r.filePath)
    }

    @Test fun createDocumentRequest_usesTheNamesTheApiExpects() {
        val s = json.encodeToString(CreateDocumentRequest("c1", "t1", "passport-20261024-1405.jpg", "abc.jpg"))
        assertEquals(
            """{"caseId":"c1","documentTypeId":"t1","fileName":"passport-20261024-1405.jpg","filePath":"abc.jpg"}""",
            s,
        )
    }

    @Test fun rejectedUploads_haveSafeWording() {
        assertEquals(
            "We couldn't accept that. Please check it and try again.",
            ApiException(ApiException.Kind.Rejected).userMessage(),
        )
    }
}
