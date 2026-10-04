package com.nexus.strategies.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

// Brand colours shared with the web portal.
val Brand = Color(0xFFC4161C)
val BrandPressed = Color(0xFFA31217)
val BrandLight = Color(0xFFFDF3F3)
val BrandHalo = Color(0xFFFBE4E4)

// Tailwind "stone" warm greys. Secondary text uses Stone500 or darker (>= 4.5:1 on white).
val Stone50 = Color(0xFFFAFAF9)
val Stone100 = Color(0xFFF5F5F4)
val Stone200 = Color(0xFFE7E5E4)
val Stone300 = Color(0xFFD6D3D1)
val Stone500 = Color(0xFF78716C)
val Stone600 = Color(0xFF57534E)
val Stone900 = Color(0xFF1C1917)

private val Scheme = lightColorScheme(
    primary = Brand,
    onPrimary = Color.White,
    primaryContainer = BrandLight,
    onPrimaryContainer = BrandPressed,
    background = Stone50,
    onBackground = Stone900,
    surface = Color.White,
    onSurface = Stone900,
    onSurfaceVariant = Stone600,
    outline = Stone300,
    outlineVariant = Stone200,
    error = Color(0xFFB91C1C),
)

@Composable
fun NexusTheme(content: @Composable () -> Unit) {
    MaterialTheme(colorScheme = Scheme, content = content)
}
