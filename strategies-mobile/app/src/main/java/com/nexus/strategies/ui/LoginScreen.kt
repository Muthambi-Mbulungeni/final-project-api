package com.nexus.strategies.ui

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.nexus.strategies.R

@Composable
fun LoginScreen(notice: String?, error: String?, busy: Boolean, onLogin: (String, String) -> Unit) {
    var email by rememberSaveable { mutableStateOf("") }
    var password by rememberSaveable { mutableStateOf("") }
    var show by rememberSaveable { mutableStateOf(false) }

    Column(
        Modifier
            .fillMaxSize()
            .background(Color.White)
            .systemBarsPadding()
            .imePadding()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 24.dp, vertical = 20.dp),
    ) {
        Image(
            painter = painterResource(R.drawable.strategies_logo),
            contentDescription = "Strategies Migration Services",
            modifier = Modifier.height(36.dp),
        )
        Spacer(Modifier.height(24.dp))
        Text(
            "Your path,\none clear step\nat a time.",
            fontSize = 30.sp,
            lineHeight = 35.sp,
            fontWeight = FontWeight.Bold,
            color = Stone900,
        )
        Spacer(Modifier.height(20.dp))
        StageDots()
        Spacer(Modifier.height(10.dp))
        Text("Five clear stages, from enquiry to decision.", color = Stone600, fontSize = 14.sp)

        Spacer(Modifier.height(36.dp))
        Text(
            "Welcome back",
            fontSize = 22.sp,
            fontWeight = FontWeight.Bold,
            color = Stone900,
            textAlign = TextAlign.Center,
            modifier = Modifier.fillMaxWidth(),
        )
        Text(
            "Sign in to your client portal.",
            color = Stone600,
            textAlign = TextAlign.Center,
            modifier = Modifier.fillMaxWidth().padding(top = 6.dp),
        )
        Spacer(Modifier.height(24.dp))

        // Notices and errors are live regions so screen readers announce them when they appear.
        notice?.let {
            Notice(it, Modifier.semantics { liveRegion = LiveRegionMode.Polite })
            Spacer(Modifier.height(16.dp))
        }
        error?.let {
            Notice(it, Modifier.semantics { liveRegion = LiveRegionMode.Assertive })
            Spacer(Modifier.height(16.dp))
        }

        FieldLabel("Email address")
        OutlinedTextField(
            value = email,
            onValueChange = { email = it },
            singleLine = true,
            shape = RoundedCornerShape(10.dp),
            colors = fieldColors(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email, imeAction = ImeAction.Next),
            modifier = Modifier.fillMaxWidth().semantics { contentDescription = "Email address" },
        )

        Spacer(Modifier.height(18.dp))
        FieldLabel("Password")
        OutlinedTextField(
            value = password,
            onValueChange = { password = it },
            singleLine = true,
            shape = RoundedCornerShape(10.dp),
            colors = fieldColors(),
            visualTransformation = if (show) VisualTransformation.None else PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password, imeAction = ImeAction.Done),
            trailingIcon = {
                IconButton(onClick = { show = !show }) {
                    Icon(
                        painter = painterResource(if (show) R.drawable.ic_eye_off else R.drawable.ic_eye),
                        contentDescription = if (show) "Hide password" else "Show password",
                        tint = Stone600,
                    )
                }
            },
            modifier = Modifier.fillMaxWidth().semantics { contentDescription = "Password" },
        )

        Spacer(Modifier.height(22.dp))
        Button(
            onClick = { onLogin(email, password) },
            enabled = !busy,
            shape = RoundedCornerShape(8.dp),
            colors = ButtonDefaults.buttonColors(containerColor = Brand, contentColor = Color.White),
            modifier = Modifier.fillMaxWidth().height(50.dp),
        ) {
            Text(if (busy) "Signing in…" else "Sign in", fontWeight = FontWeight.SemiBold)
        }

        Spacer(Modifier.height(24.dp))
        Text(
            "Staff (admin and consultants): please use the web portal.",
            color = Stone600,
            fontSize = 13.sp,
            textAlign = TextAlign.Center,
            modifier = Modifier.fillMaxWidth(),
        )
        Spacer(Modifier.height(20.dp))
        Text(
            "Client portal. Personal information is processed in accordance with POPIA.",
            color = Stone600,
            fontSize = 12.sp,
            textAlign = TextAlign.Center,
            modifier = Modifier.fillMaxWidth(),
        )
        Spacer(Modifier.height(16.dp))
        Text(
            "© 2026 Strategies Migration Services",
            color = Stone600,
            fontSize = 12.sp,
            textAlign = TextAlign.Center,
            modifier = Modifier.fillMaxWidth(),
        )
    }
}

@Composable
private fun FieldLabel(text: String) {
    Text(text, fontSize = 14.sp, fontWeight = FontWeight.Medium, color = Stone900, modifier = Modifier.padding(bottom = 6.dp))
}

@Composable
private fun fieldColors() = OutlinedTextFieldDefaults.colors(
    focusedBorderColor = Brand,
    unfocusedBorderColor = Stone300,
    focusedContainerColor = Color.White,
    unfocusedContainerColor = Color.White,
    cursorColor = Brand,
)

@Composable
private fun Notice(text: String, modifier: Modifier = Modifier) {
    Surface(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(8.dp),
        color = BrandLight,
        border = BorderStroke(1.dp, BrandBorder),
    ) {
        Text(text, color = BrandDark, fontSize = 14.sp, modifier = Modifier.padding(horizontal = 12.dp, vertical = 10.dp))
    }
}

/** The five red dots joined by a line, echoing the five case stages (same motif as the web login). */
@Composable
private fun StageDots() {
    Row(Modifier.fillMaxWidth(0.8f), verticalAlignment = Alignment.CenterVertically) {
        repeat(5) { i ->
            Box(Modifier.size(20.dp).background(BrandHalo, CircleShape), contentAlignment = Alignment.Center) {
                Box(Modifier.size(10.dp).background(Brand, CircleShape))
            }
            if (i < 4) Box(Modifier.weight(1f).height(1.5.dp).background(Stone900))
        }
    }
}
