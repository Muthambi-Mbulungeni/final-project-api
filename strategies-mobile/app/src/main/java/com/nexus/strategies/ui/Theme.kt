package com.nexus.strategies.ui

import android.content.Context
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.Font
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight

// Brand colours shared with the web portal (brand-50 ... brand-800).
val Brand = Color(0xFFC4161C)
val BrandPressed = Color(0xFFA31217)
val BrandDark = Color(0xFF861217)
val BrandLight = Color(0xFFFDF3F3)
val BrandHalo = Color(0xFFFBE4E4)
val BrandBorder = Color(0xFFF6C9CA)

// Tailwind "stone" warm greys. Secondary text uses Stone600 (>= 4.5:1 on white).
val Stone50 = Color(0xFFFAFAF9)
val Stone100 = Color(0xFFF5F5F4)
val Stone200 = Color(0xFFE7E5E4)
val Stone300 = Color(0xFFD6D3D1)
val Stone500 = Color(0xFF78716C)
val Stone600 = Color(0xFF57534E)
val Stone800 = Color(0xFF292524)
val Stone900 = Color(0xFF1C1917)

// Document status colours (same families as the web badges).
val Amber50 = Color(0xFFFFFBEB)
val Amber300 = Color(0xFFFCD34D)
val Amber500 = Color(0xFFF59E0B)
val Amber700 = Color(0xFFB45309)
val Amber800 = Color(0xFF92400E)
val Emerald50 = Color(0xFFECFDF5)
val Emerald200 = Color(0xFFA7F3D0)
val Emerald500 = Color(0xFF10B981)
val Emerald700 = Color(0xFF047857)
val Emerald800 = Color(0xFF065F46)

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
    error = BrandPressed,
)

/**
 * The web portal uses IBM Plex Sans. To match it, add these files to app/src/main/res/font/ (see README):
 * ibm_plex_sans_regular.ttf, ibm_plex_sans_medium.ttf, ibm_plex_sans_semibold.ttf, ibm_plex_sans_bold.ttf.
 * They are looked up at run time, so the app builds and runs (with the system font) even before they are added.
 */
private fun plexFamily(context: Context): FontFamily {
    fun font(name: String, weight: FontWeight): Font? {
        val id = context.resources.getIdentifier(name, "font", context.packageName)
        return if (id != 0) Font(id, weight) else null
    }
    val fonts = listOfNotNull(
        font("ibm_plex_sans_regular", FontWeight.Normal),
        font("ibm_plex_sans_medium", FontWeight.Medium),
        font("ibm_plex_sans_semibold", FontWeight.SemiBold),
        font("ibm_plex_sans_bold", FontWeight.Bold),
    )
    return if (fonts.isEmpty()) FontFamily.Default else FontFamily(fonts)
}

private fun Typography.withFont(f: FontFamily) = copy(
    displayLarge = displayLarge.copy(fontFamily = f),
    displayMedium = displayMedium.copy(fontFamily = f),
    displaySmall = displaySmall.copy(fontFamily = f),
    headlineLarge = headlineLarge.copy(fontFamily = f),
    headlineMedium = headlineMedium.copy(fontFamily = f),
    headlineSmall = headlineSmall.copy(fontFamily = f),
    titleLarge = titleLarge.copy(fontFamily = f),
    titleMedium = titleMedium.copy(fontFamily = f),
    titleSmall = titleSmall.copy(fontFamily = f),
    bodyLarge = bodyLarge.copy(fontFamily = f),
    bodyMedium = bodyMedium.copy(fontFamily = f),
    bodySmall = bodySmall.copy(fontFamily = f),
    labelLarge = labelLarge.copy(fontFamily = f),
    labelMedium = labelMedium.copy(fontFamily = f),
    labelSmall = labelSmall.copy(fontFamily = f),
)

@Composable
fun NexusTheme(content: @Composable () -> Unit) {
    val context = LocalContext.current
    val family = remember { plexFamily(context) }
    MaterialTheme(colorScheme = Scheme, typography = Typography().withFont(family), content = content)
}
