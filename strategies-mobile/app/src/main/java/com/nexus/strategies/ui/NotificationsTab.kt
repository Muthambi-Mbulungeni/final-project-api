package com.nexus.strategies.ui

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.nexus.strategies.data.NotificationDto
import com.nexus.strategies.data.formatDate

@Composable
fun NotificationsTab(ui: HomeUi, onOpen: (NotificationDto) -> Unit, onRetry: () -> Unit) {
    LazyColumn(
        Modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        ui.notificationsError?.let { item { ErrorBanner(it, onRetry) } }

        if (ui.notifications.isEmpty() && !ui.loading && ui.notificationsError == null) item {
            EmptyState("No messages", "Updates from your consultant will appear here.")
        }
        items(ui.notifications, key = { it.id }) { n ->
            Card(
                onClick = { onOpen(n) },
                colors = CardDefaults.cardColors(containerColor = if (n.isRead) Color.White else BrandLight),
                border = BorderStroke(1.dp, Stone200),
            ) {
                Row(Modifier.padding(16.dp), verticalAlignment = Alignment.Top) {
                    // Unread dot; "Unread" is also spelled out in text for accessibility.
                    if (!n.isRead) Box(Modifier.padding(top = 6.dp, end = 12.dp).size(10.dp).background(Brand, CircleShape))
                    Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                        Text(n.text, fontWeight = if (n.isRead) FontWeight.Normal else FontWeight.SemiBold)
                        val meta = listOfNotNull(
                            if (n.isRead) null else "Unread",
                            n.caseReference?.let { "Case $it" },
                            formatDate(n.whenIso),
                        ).joinToString(" · ")
                        if (meta.isNotEmpty()) Text(meta, color = Stone600, style = MaterialTheme.typography.bodySmall)
                    }
                }
            }
        }
    }
}
