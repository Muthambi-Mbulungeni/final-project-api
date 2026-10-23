package com.nexus.strategies.ui

import android.content.ActivityNotFoundException
import android.graphics.BitmapFactory
import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Email
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.FileProvider
import java.io.File
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.nexus.strategies.R
import com.nexus.strategies.data.DocStatus
import com.nexus.strategies.data.DocumentDto
import com.nexus.strategies.data.DocumentTypeDto
import com.nexus.strategies.data.sizeLabel
import com.nexus.strategies.data.formatDate

@Composable
fun DocumentsTab(
    ui: HomeUi,
    onRetry: () -> Unit,
    onPhoto: (Uri?, Boolean) -> Unit,
    onDiscardPhoto: () -> Unit,
    onSend: (DocumentTypeDto) -> Unit,
    onUploadProblem: (String) -> Unit,
) {
    val docs = ui.documents
    val toReplace = docs.filter { it.state == DocStatus.NeedsReplacing }

    LazyColumn(
        Modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        item {
            PageHeader(
                "Documents",
                "See what needs your attention, and which documents your consultant has approved.",
            )
        }
        ui.error?.let { item { ErrorBanner(it, onRetry) } }

        if (ui.case == null) {
            if (!ui.loading && ui.error == null) item {
                EmptyState("No documents yet", "Documents appear here once your case has been opened.")
            }
            return@LazyColumn
        }

        if (toReplace.isNotEmpty()) item { ActionNeededCard(toReplace) }
        item {
            SendDocumentCard(
                ui = ui,
                suggestedTypeId = toReplace.firstOrNull()?.documentTypeId,
                onRetry = onRetry,
                onPhoto = onPhoto,
                onDiscardPhoto = onDiscardPhoto,
                onSend = onSend,
                onUploadProblem = onUploadProblem,
            )
        }
        item { YourDocuments(docs) }
        item { HowReviewWorks() }
    }
}

@Composable
private fun ActionNeededCard(docs: List<DocumentDto>) {
    SectionCard(container = BrandLight, border = BrandBorder) {
        Eyebrow("Action needed")
        Spacer(Modifier.height(6.dp))
        Text(
            if (docs.size == 1) "Replace 1 document" else "Replace ${docs.size} documents",
            fontSize = 18.sp,
            fontWeight = FontWeight.Bold,
        )
        docs.forEach { d ->
            Spacer(Modifier.height(14.dp))
            Text(d.documentTypeName, fontWeight = FontWeight.SemiBold)
            formatDate(d.uploadedAt)?.let { Text("Uploaded $it", fontSize = 12.sp, color = Stone600) }
            d.reviewComment?.takeIf { it.isNotBlank() }?.let { note ->
                Spacer(Modifier.height(8.dp))
                Row(Modifier.height(IntrinsicSize.Min)) {
                    Box(Modifier.width(2.dp).fillMaxHeight().background(Brand))
                    Spacer(Modifier.width(10.dp))
                    Column {
                        Text(note)
                        Text(d.reviewerCredit, fontSize = 12.sp, color = Stone600)
                    }
                }
            }
        }
        Spacer(Modifier.height(14.dp))
        Text("Send a new copy with a photo, in the card below.", fontSize = 13.sp, color = Stone600)
    }
}

@Composable
private fun YourDocuments(docs: List<DocumentDto>) {
    val approved = docs.count { it.state == DocStatus.Approved }
    val awaiting = docs.count { it.state == DocStatus.Awaiting }
    val replacing = docs.count { it.state == DocStatus.NeedsReplacing }

    SectionCard(title = "Your documents") {
        if (docs.isEmpty()) {
            Text("Nothing uploaded yet", fontWeight = FontWeight.SemiBold)
            Spacer(Modifier.height(4.dp))
            Text("Your document checklist will show here as files are added and reviewed.", color = Stone600)
            return@SectionCard
        }

        Row(verticalAlignment = Alignment.Bottom) {
            Text("$approved", fontSize = 30.sp, fontWeight = FontWeight.Bold)
            Text(" of ${docs.size} approved", color = Stone600, modifier = Modifier.padding(bottom = 5.dp))
        }
        Spacer(Modifier.height(10.dp))
        // Segmented bar: colour is never the only signal, the legend below spells the counts out.
        Row(Modifier.fillMaxWidth().height(6.dp).clip(RoundedCornerShape(3.dp)).background(Stone200)) {
            if (approved > 0) Box(Modifier.weight(approved.toFloat()).fillMaxHeight().background(Emerald500))
            if (awaiting > 0) Box(Modifier.weight(awaiting.toFloat()).fillMaxHeight().background(Amber500))
            if (replacing > 0) Box(Modifier.weight(replacing.toFloat()).fillMaxHeight().background(Brand))
        }
        Spacer(Modifier.height(10.dp))
        Row(horizontalArrangement = Arrangement.spacedBy(14.dp)) {
            if (approved > 0) Legend(Emerald500, "$approved approved")
            if (awaiting > 0) Legend(Amber500, "$awaiting awaiting review")
            if (replacing > 0) Legend(Brand, "$replacing needs replacing")
        }

        docs.forEach { d ->
            HorizontalDivider(color = Stone200, modifier = Modifier.padding(top = 14.dp))
            DocumentRow(d)
        }
    }
}

@Composable
private fun Legend(color: Color, text: String) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Box(Modifier.size(8.dp).background(color, CircleShape))
        Spacer(Modifier.width(6.dp))
        Text(text, fontSize = 12.sp, color = Stone600)
    }
}

@Composable
private fun DocumentRow(d: DocumentDto) {
    Row(Modifier.fillMaxWidth().padding(top = 14.dp), verticalAlignment = Alignment.Top) {
        StateIcon(d.state)
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.Top) {
                Text(d.documentTypeName, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                StatusBadge(d.state)
            }
            val meta = listOfNotNull(formatDate(d.uploadedAt)?.let { "Uploaded $it" }, d.fileName.takeIf { it.isNotBlank() })
                .joinToString(" · ")
            if (meta.isNotEmpty()) Text(meta, fontSize = 12.sp, color = Stone600, modifier = Modifier.padding(top = 4.dp))
        }
    }
}

@Composable
private fun StateIcon(state: DocStatus) {
    val (bg, fg) = when (state) {
        DocStatus.Approved -> Emerald50 to Emerald700
        DocStatus.Awaiting -> Amber50 to Amber700
        DocStatus.NeedsReplacing -> BrandLight to BrandPressed
    }
    Box(Modifier.size(40.dp).background(bg, CircleShape), contentAlignment = Alignment.Center) {
        when (state) {
            DocStatus.Approved -> Icon(Icons.Default.Check, contentDescription = null, tint = fg, modifier = Modifier.size(20.dp))
            DocStatus.Awaiting -> Icon(painterResource(R.drawable.ic_clock), contentDescription = null, tint = fg, modifier = Modifier.size(20.dp))
            DocStatus.NeedsReplacing -> Icon(painterResource(R.drawable.ic_alert), contentDescription = null, tint = fg, modifier = Modifier.size(20.dp))
        }
    }
}

@Composable
private fun StatusBadge(state: DocStatus) {
    // The status word is always shown, so colour is never the only signal.
    val (bg, border, fg, dot) = when (state) {
        DocStatus.Approved -> listOf(Emerald50, Emerald200, Emerald800, Emerald500)
        DocStatus.Awaiting -> listOf(Amber50, Amber300, Amber800, Amber500)
        DocStatus.NeedsReplacing -> listOf(BrandLight, BrandBorder, BrandDark, Brand)
    }
    Surface(shape = RoundedCornerShape(6.dp), color = bg, border = BorderStroke(1.dp, border)) {
        Row(Modifier.padding(horizontal = 8.dp, vertical = 3.dp), verticalAlignment = Alignment.CenterVertically) {
            Box(Modifier.size(6.dp).background(dot, CircleShape))
            Spacer(Modifier.width(5.dp))
            Text(state.label, fontSize = 12.sp, fontWeight = FontWeight.SemiBold, color = fg)
        }
    }
}

@Composable
private fun HowReviewWorks() {
    SectionCard(title = "How review works") {
        Explain(DocStatus.Awaiting, "Awaiting review", "Your consultant checks each document you upload.")
        Spacer(Modifier.height(14.dp))
        Explain(DocStatus.Approved, "Approved", "Accepted for your application. Nothing more to do.")
        Spacer(Modifier.height(14.dp))
        Explain(DocStatus.NeedsReplacing, "Needs replacing", "Your consultant explains what to fix, so you can send a new copy.")
        HorizontalDivider(color = Stone200, modifier = Modifier.padding(vertical = 14.dp))
        Row {
            Box(Modifier.size(40.dp).background(Stone100, CircleShape), contentAlignment = Alignment.Center) {
                Icon(Icons.Default.Email, contentDescription = null, tint = Stone600, modifier = Modifier.size(20.dp))
            }
            Spacer(Modifier.width(12.dp))
            Column(Modifier.weight(1f)) {
                Text("Sending documents", fontWeight = FontWeight.SemiBold)
                Text(
                    "Take a photo of a document, or choose one from your gallery, and send it from this screen. " +
                        "It shows here as awaiting review until your consultant has checked it.",
                    fontSize = 13.sp,
                    color = Stone600,
                )
            }
        }
    }
}

@Composable
private fun Explain(state: DocStatus, title: String, body: String) {
    Row {
        StateIcon(state)
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
            Text(title, fontWeight = FontWeight.SemiBold)
            Text(body, fontSize = 13.sp, color = Stone600)
        }
    }
}

/**
 * "Send a document": pick the document type, take a photo or choose one, check it, send it.
 * The camera app and the gallery photo picker need no permission from us; the photo is shrunk to fit
 * the API's 5 MB limit before it is shown here (see data/PhotoPrep.kt).
 */
@Composable
private fun SendDocumentCard(
    ui: HomeUi,
    suggestedTypeId: String?,
    onRetry: () -> Unit,
    onPhoto: (Uri?, Boolean) -> Unit,
    onDiscardPhoto: () -> Unit,
    onSend: (DocumentTypeDto) -> Unit,
    onUploadProblem: (String) -> Unit,
) {
    val context = LocalContext.current
    var chosenTypeId by rememberSaveable { mutableStateOf<String?>(null) }
    var menuOpen by remember { mutableStateOf(false) }
    // Kept across rotation: the camera app can restart this screen while it is open.
    var cameraUri by rememberSaveable { mutableStateOf<Uri?>(null) }

    val camera = rememberLauncherForActivityResult(ActivityResultContracts.TakePicture()) { saved ->
        val uri = cameraUri
        cameraUri = null
        if (saved && uri != null) onPhoto(uri, true)
        else if (uri != null) runCatching { context.contentResolver.delete(uri, null, null) }
    }
    val gallery = rememberLauncherForActivityResult(ActivityResultContracts.PickVisualMedia()) { uri -> onPhoto(uri, false) }

    fun takePhoto() {
        try {
            val dir = File(context.cacheDir, "camera").apply { mkdirs() }
            val file = File.createTempFile("photo-", ".jpg", dir)
            val uri = FileProvider.getUriForFile(context, "${context.packageName}.fileprovider", file)
            cameraUri = uri
            camera.launch(uri)
        } catch (_: ActivityNotFoundException) {
            onUploadProblem("No camera app was found. Choose a photo from your gallery instead.")
        } catch (_: java.io.IOException) {
            onUploadProblem("We couldn't start the camera. Try again, or choose a photo from your gallery.")
        }
    }

    val types = ui.docTypes
    val selected = types.firstOrNull { it.id == (chosenTypeId ?: suggestedTypeId) }
    val busy = ui.uploadBusy

    SectionCard(title = "Send a document") {
        Text(
            "Take a clear photo in good light, with the whole page in view. We shrink it for you before it is sent.",
            fontSize = 13.sp,
            color = Stone600,
        )
        Spacer(Modifier.height(14.dp))

        Text("Document type", fontSize = 12.sp, fontWeight = FontWeight.SemiBold, color = Stone600)
        Spacer(Modifier.height(6.dp))
        when {
            ui.docTypesError != null && types.isEmpty() -> ErrorBanner(ui.docTypesError, onRetry)
            else -> Box {
                OutlinedButton(
                    onClick = { menuOpen = true },
                    enabled = types.isNotEmpty() && !busy,
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(8.dp),
                ) {
                    Text(
                        selected?.name ?: "Choose a document type",
                        modifier = Modifier.weight(1f),
                        color = if (selected == null) Stone600 else Stone900,
                    )
                    Text("▾", color = Stone600)
                }
                DropdownMenu(expanded = menuOpen, onDismissRequest = { menuOpen = false }, containerColor = Color.White) {
                    types.forEach { t ->
                        DropdownMenuItem(text = { Text(t.name) }, onClick = { chosenTypeId = t.id; menuOpen = false })
                    }
                }
            }
        }
        Spacer(Modifier.height(14.dp))

        val photo = ui.photo
        if (photo == null) {
            Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                Button(onClick = ::takePhoto, enabled = !busy, modifier = Modifier.weight(1f), shape = RoundedCornerShape(8.dp)) {
                    Text("Take photo")
                }
                OutlinedButton(
                    onClick = { gallery.launch(PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly)) },
                    enabled = !busy,
                    modifier = Modifier.weight(1f),
                    shape = RoundedCornerShape(8.dp),
                ) { Text("Choose photo") }
            }
        } else {
            // A small preview so the client can check it is readable before sending.
            val preview = remember(photo) {
                BitmapFactory.decodeByteArray(photo.bytes, 0, photo.bytes.size, BitmapFactory.Options().apply { inSampleSize = 4 })
                    ?.asImageBitmap()
            }
            if (preview != null) {
                Image(
                    bitmap = preview,
                    contentDescription = "Preview of the photo you are about to send",
                    contentScale = ContentScale.Fit,
                    modifier = Modifier.fillMaxWidth().height(200.dp).clip(RoundedCornerShape(8.dp)).background(Stone100),
                )
                Spacer(Modifier.height(8.dp))
            }
            Text("Photo ready · ${sizeLabel(photo.bytes.size)}", fontSize = 12.sp, color = Stone600)
            Spacer(Modifier.height(10.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                Button(
                    onClick = { selected?.let(onSend) },
                    enabled = selected != null && !busy,
                    modifier = Modifier.weight(1f),
                    shape = RoundedCornerShape(8.dp),
                ) { Text(if (busy) "Sending…" else "Send") }
                OutlinedButton(onClick = onDiscardPhoto, enabled = !busy, modifier = Modifier.weight(1f), shape = RoundedCornerShape(8.dp)) {
                    Text("Discard")
                }
            }
            if (selected == null) {
                Spacer(Modifier.height(6.dp))
                Text("Choose the document type above to send this photo.", fontSize = 12.sp, color = Stone600)
            }
        }

        if (busy) {
            Spacer(Modifier.height(12.dp))
            LinearProgressIndicator(Modifier.fillMaxWidth(), color = Brand)
        }
        ui.uploadError?.let {
            Spacer(Modifier.height(10.dp))
            Text(it, color = BrandDark, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
        }
        ui.uploadMessage?.let {
            Spacer(Modifier.height(10.dp))
            Text(it, color = Emerald700, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
        }
    }
}
