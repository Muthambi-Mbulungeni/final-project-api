package com.nexus.strategies.ui

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.nexus.strategies.data.DocumentDto
import com.nexus.strategies.data.formatDate

@Composable
fun DocumentsTab(ui: HomeUi, onRetry: () -> Unit) {
    LazyColumn(
        Modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        ui.error?.let { item { ErrorBanner(it, onRetry) } }

        if (ui.case == null) {
            if (!ui.loading && ui.error == null) item {
                EmptyState("No documents yet", "Documents appear here once your case has been opened.")
            }
        } else {
            item {
                Text(
                    "Documents for case ${ui.case.reference}. Uploading from the app will arrive once the upload service is finished; for now your consultant adds files.",
                    color = Stone600,
                    style = MaterialTheme.typography.bodySmall,
                )
            }
            if (ui.documents.isEmpty() && !ui.loading) item {
                EmptyState("Nothing uploaded yet", "Your document checklist will show here as files are added and reviewed.")
            }
            items(ui.documents, key = { it.id }) { DocumentCard(it) }
        }
    }
}

@Composable
private fun DocumentCard(d: DocumentDto) {
    Card(colors = CardDefaults.cardColors(containerColor = Color.White), border = BorderStroke(1.dp, Stone200)) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text(d.documentTypeName, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                StatusChip(d.status)
            }
            if (d.fileName.isNotBlank()) Text(d.fileName, color = Stone600, style = MaterialTheme.typography.bodySmall)
            formatDate(d.uploadedAt)?.let { Text("Uploaded $it", color = Stone600, style = MaterialTheme.typography.bodySmall) }
            d.reviewComment?.takeIf { it.isNotBlank() }?.let {
                Text("Reviewer note: $it", style = MaterialTheme.typography.bodySmall)
            }
        }
    }
}

@Composable
private fun StatusChip(status: String) {
    // Colour is never the only signal: the status word is always shown.
    val (bg, fg) = when (status.lowercase()) {
        "approved" -> Color(0xFFDCFCE7) to Color(0xFF166534)
        "rejected" -> Color(0xFFFEE2E2) to Color(0xFF991B1B)
        else -> Color(0xFFFEF3C7) to Color(0xFF92400E)
    }
    Surface(color = bg, shape = MaterialTheme.shapes.small) {
        Text(status, color = fg, style = MaterialTheme.typography.labelMedium, modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp))
    }
}
