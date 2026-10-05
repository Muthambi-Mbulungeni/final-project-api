package com.nexus.strategies.ui

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

/** Small red uppercase label above headings, like "YOUR APPLICATION" on the web. */
@Composable
fun Eyebrow(text: String, modifier: Modifier = Modifier, color: Color = BrandPressed) {
    Text(
        text.uppercase(),
        modifier = modifier,
        color = color,
        fontSize = 12.sp,
        fontWeight = FontWeight.SemiBold,
        letterSpacing = 1.2.sp,
    )
}

@Composable
fun PageHeader(title: String, subtitle: String) {
    Column(Modifier.fillMaxWidth()) {
        Eyebrow("Your application")
        Spacer(Modifier.height(6.dp))
        Text(title, fontSize = 26.sp, lineHeight = 32.sp, fontWeight = FontWeight.Bold, color = Stone900)
        Spacer(Modifier.height(6.dp))
        Text(subtitle, color = Stone600)
        Spacer(Modifier.height(16.dp))
        HorizontalDivider(color = Stone200)
    }
}

/** White bordered card with an optional titled header, matching the web panels. */
@Composable
fun SectionCard(
    modifier: Modifier = Modifier,
    title: String? = null,
    container: Color = Color.White,
    border: Color = Stone200,
    content: @Composable ColumnScope.() -> Unit,
) {
    Surface(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(12.dp),
        color = container,
        border = BorderStroke(1.dp, border),
    ) {
        Column {
            if (title != null) {
                Text(title, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(horizontal = 16.dp, vertical = 14.dp))
                HorizontalDivider(color = border)
            }
            Column(Modifier.padding(16.dp), content = content)
        }
    }
}

@Composable
fun ErrorBanner(message: String, onRetry: () -> Unit) {
    SectionCard(container = BrandLight, border = BrandBorder) {
        Text(message, color = BrandDark)
        TextButton(onClick = onRetry) { Text("Try again", color = BrandPressed, fontWeight = FontWeight.SemiBold) }
    }
}

@Composable
fun EmptyState(title: String, body: String) {
    SectionCard {
        Text(title, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(4.dp))
        Text(body, color = Stone600)
    }
}
