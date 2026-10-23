package com.nexus.strategies.data

import android.content.ContentResolver
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Matrix
import android.net.Uri
import androidx.exifinterface.media.ExifInterface
import java.io.ByteArrayOutputStream
import java.io.IOException

/**
 * Turns a photo from the camera or gallery into a JPEG the API will accept (PDF/JPG/PNG, 5 MB at most):
 * decodes at reduced size, applies the camera's rotation, flattens transparency onto white, and lowers
 * quality (then size) until it fits under [TARGET_UPLOAD_BYTES]. Blocking: call it off the main thread.
 */
object PhotoPrep {

    @Throws(IOException::class)
    fun prepare(resolver: ContentResolver, uri: Uri): ByteArray {
        val bounds = BitmapFactory.Options().apply { inJustDecodeBounds = true }
        open(resolver, uri).use { BitmapFactory.decodeStream(it, null, bounds) }
        if (bounds.outWidth <= 0 || bounds.outHeight <= 0) throw IOException("Not an image")

        val orientation = runCatching {
            open(resolver, uri).use {
                ExifInterface(it).getAttributeInt(ExifInterface.TAG_ORIENTATION, ExifInterface.ORIENTATION_NORMAL)
            }
        }.getOrDefault(ExifInterface.ORIENTATION_NORMAL)

        val opts = BitmapFactory.Options().apply { inSampleSize = sampleSizeFor(bounds.outWidth, bounds.outHeight) }
        var bitmap = open(resolver, uri).use { BitmapFactory.decodeStream(it, null, opts) }
            ?: throw IOException("Could not decode image")

        bitmap = replace(bitmap) { b ->
            val (w, h) = scaledSize(b.width, b.height)
            if (w == b.width && h == b.height) b else Bitmap.createScaledBitmap(b, w, h, true)
        }
        bitmap = replace(bitmap) { upright(it, orientation) }
        bitmap = replace(bitmap) { onWhite(it) }

        try {
            var quality = 85
            while (true) {
                val bytes = jpeg(bitmap, quality)
                if (bytes.size <= TARGET_UPLOAD_BYTES) return bytes
                if (quality > 45) {
                    quality -= 10
                } else {
                    // Still too big at the lowest sensible quality: shrink the picture itself and start again.
                    val smaller = Bitmap.createScaledBitmap(
                        bitmap, (bitmap.width * 0.75f).toInt().coerceAtLeast(1), (bitmap.height * 0.75f).toInt().coerceAtLeast(1), true,
                    )
                    if (smaller !== bitmap) bitmap.recycle()
                    bitmap = smaller
                    quality = 80
                    if (bitmap.width < 400) throw IOException("Cannot shrink image enough")
                }
            }
        } finally {
            bitmap.recycle()
        }
    }

    private fun open(resolver: ContentResolver, uri: Uri) =
        resolver.openInputStream(uri) ?: throw IOException("Cannot open image")

    /** Applies [transform] and recycles the old bitmap if a new one came back. */
    private inline fun replace(old: Bitmap, transform: (Bitmap) -> Bitmap): Bitmap {
        val new = transform(old)
        if (new !== old) old.recycle()
        return new
    }

    private fun upright(b: Bitmap, orientation: Int): Bitmap {
        val m = Matrix()
        when (orientation) {
            ExifInterface.ORIENTATION_ROTATE_90 -> m.postRotate(90f)
            ExifInterface.ORIENTATION_ROTATE_180 -> m.postRotate(180f)
            ExifInterface.ORIENTATION_ROTATE_270 -> m.postRotate(270f)
            ExifInterface.ORIENTATION_FLIP_HORIZONTAL -> m.postScale(-1f, 1f)
            ExifInterface.ORIENTATION_FLIP_VERTICAL -> m.postScale(1f, -1f)
            ExifInterface.ORIENTATION_TRANSPOSE -> { m.postRotate(90f); m.postScale(-1f, 1f) }
            ExifInterface.ORIENTATION_TRANSVERSE -> { m.postRotate(270f); m.postScale(-1f, 1f) }
            else -> return b
        }
        return Bitmap.createBitmap(b, 0, 0, b.width, b.height, m, true)
    }

    /** JPEG has no transparency: a see-through PNG would otherwise turn black. */
    private fun onWhite(b: Bitmap): Bitmap {
        if (!b.hasAlpha()) return b
        val out = Bitmap.createBitmap(b.width, b.height, Bitmap.Config.ARGB_8888)
        Canvas(out).apply { drawColor(Color.WHITE); drawBitmap(b, 0f, 0f, null) }
        return out
    }

    private fun jpeg(b: Bitmap, quality: Int): ByteArray =
        ByteArrayOutputStream().also { b.compress(Bitmap.CompressFormat.JPEG, quality, it) }.toByteArray()
}
