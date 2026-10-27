package com.nexus.strategies.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.withStyle
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.nexus.strategies.data.CaseDto
import com.nexus.strategies.data.DocStatus
import com.nexus.strategies.data.DocumentDto
import com.nexus.strategies.data.requestedBy
import com.nexus.strategies.data.Stage
import com.nexus.strategies.data.formatDate

@Composable
fun ProgressTab(ui: HomeUi, name: String, onOpenDocuments: () -> Unit, onRetry: () -> Unit) {
    val case = ui.case
    LazyColumn(
        Modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        item {
            val first = name.substringBefore(' ').ifBlank { "there" }
            val visa = case?.visaCategoryOrNull
            PageHeader(
                title = "Welcome back, $first",
                subtitle = when {
                    case == null -> "Your case will appear here once it has been opened."
                    visa != null -> "Here's the latest on your $visa application."
                    else -> "Here's the latest on your application."
                },
            )
        }

        ui.error?.let { item { ErrorBanner(it, onRetry) } }

        if (case == null) {
            if (!ui.loading && ui.error == null) item {
                EmptyState(
                    "No case yet",
                    "Once your enquiry has been assessed, your case and its progress will appear here.",
                )
            }
        } else {
            val toReplace = ui.documents.filter { it.state == DocStatus.NeedsReplacing }
            if (toReplace.isNotEmpty()) item { ActionNeeded(toReplace, onOpenDocuments) }
            item { SectionCard(title = "Application progress") { CaseStepper(case.stageEnum) } }
            item { WhatHappensNext(case) }
        }
    }
}

@Composable
private fun ActionNeeded(docs: List<DocumentDto>, onOpen: () -> Unit) {
    SectionCard(container = BrandLight, border = BrandBorder) {
        Eyebrow("Action needed")
        Spacer(Modifier.height(6.dp))
        Text(
            if (docs.size == 1) "Replace 1 document" else "Replace ${docs.size} documents",
            fontSize = 18.sp,
            fontWeight = FontWeight.Bold,
        )
        Spacer(Modifier.height(4.dp))
        Text(
            if (docs.size == 1) "${requestedBy(docs)} asked for a new copy of your ${docs[0].documentTypeName.lowercase()}."
            else "${requestedBy(docs)} asked for new copies of ${docs.size} documents.",
            color = Stone600,
        )
        Spacer(Modifier.height(14.dp))
        Button(
            onClick = onOpen,
            shape = RoundedCornerShape(8.dp),
            colors = ButtonDefaults.buttonColors(containerColor = Brand, contentColor = Color.White),
            modifier = Modifier.fillMaxWidth().height(48.dp),
        ) { Text("See what's needed  →", fontWeight = FontWeight.SemiBold) }
    }
}

private enum class StepState { Done, Current, Upcoming }

/** Vertical stepper, same as the web on phones: red tick = done, red ring = current, grey = upcoming. */
@Composable
fun CaseStepper(current: Stage) {
    val total = Stage.entries.size
    Column {
        Stage.entries.forEach { stage ->
            val state = when {
                stage.ordinal < current.ordinal -> StepState.Done
                stage == current -> StepState.Current
                else -> StepState.Upcoming
            }
            val isLast = stage.ordinal == total - 1
            val spoken = "Stage ${stage.ordinal + 1} of $total, ${stage.label}, " + when (state) {
                StepState.Done -> "completed"
                StepState.Current -> "current stage"
                StepState.Upcoming -> "upcoming"
            }
            Row(Modifier.height(IntrinsicSize.Min).clearAndSetSemantics { contentDescription = spoken }) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    StepCircle(stage.ordinal + 1, state)
                    if (!isLast) {
                        // The line is red if the stage above it is complete, grey otherwise.
                        Box(
                            Modifier
                                .width(2.dp)
                                .weight(1f)
                                .heightIn(min = 16.dp)
                                .background(if (state == StepState.Done) Brand else Stone300)
                        )
                    }
                }
                Spacer(Modifier.width(14.dp))
                Column(Modifier.padding(top = 6.dp, bottom = if (isLast) 0.dp else 20.dp)) {
                    Text(
                        stage.label,
                        fontWeight = if (state == StepState.Upcoming) FontWeight.Medium else FontWeight.SemiBold,
                        color = if (state == StepState.Upcoming) Stone500 else Stone800,
                    )
                    if (state == StepState.Current) {
                        Text("Current stage", color = BrandPressed, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
                    }
                    Text(
                        stage.hint,
                        fontSize = 13.sp,
                        color = if (state == StepState.Upcoming) Stone500 else Stone600,
                    )
                }
            }
        }
    }
}

@Composable
private fun StepCircle(number: Int, state: StepState) {
    // The outer 40dp box carries the soft halo for the current stage; every state uses the same size.
    Box(
        Modifier
            .size(40.dp)
            .background(if (state == StepState.Current) BrandHalo else Color.Transparent, CircleShape),
        contentAlignment = Alignment.Center,
    ) {
        val inner = Modifier.size(30.dp).clip(CircleShape)
        when (state) {
            StepState.Done -> Box(inner.background(Brand), contentAlignment = Alignment.Center) {
                Icon(Icons.Default.Check, contentDescription = null, tint = Color.White, modifier = Modifier.size(18.dp))
            }
            StepState.Current -> Box(
                inner.background(Color.White).border(2.dp, Brand, CircleShape),
                contentAlignment = Alignment.Center,
            ) { Text(number.toString(), color = Brand, fontWeight = FontWeight.Bold) }
            StepState.Upcoming -> Box(
                inner.background(Color.White).border(1.dp, Stone300, CircleShape),
                contentAlignment = Alignment.Center,
            ) { Text(number.toString(), color = Stone500) }
        }
    }
}

@Composable
private fun WhatHappensNext(case: CaseDto) {
    val current = case.stageEnum
    val next = Stage.entries.getOrNull(current.ordinal + 1)
    SectionCard(title = "What happens next") {
        Eyebrow("Stage ${current.ordinal + 1} of ${Stage.entries.size}")
        Spacer(Modifier.height(4.dp))
        Text(current.label, fontSize = 18.sp, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(4.dp))
        Text(current.hint, color = Stone600)
        Spacer(Modifier.height(12.dp))
        Text(
            if (next != null) buildAnnotatedString {
                withStyle(SpanStyle(fontWeight = FontWeight.Bold)) { append("Next:") }
                append(" ${next.label.lowercase()}. ${next.hint}")
            } else buildAnnotatedString { append("This is the final stage of your case.") },
        )

        // Case facts panel; "N/A" and "Unassigned" placeholders are left out rather than printed.
        val facts = listOfNotNull(
            "Case reference" to case.reference,
            formatDate(case.createdAt)?.let { "Opened" to it },
            case.visaCategoryOrNull?.let { "Visa category" to it },
            case.consultantOrNull?.let { "Your consultant" to it },
        )
        Spacer(Modifier.height(16.dp))
        Surface(shape = RoundedCornerShape(8.dp), color = Stone50, border = androidx.compose.foundation.BorderStroke(1.dp, Stone200)) {
            Column(Modifier.fillMaxWidth().padding(14.dp), verticalArrangement = Arrangement.spacedBy(14.dp)) {
                facts.chunked(2).forEach { row ->
                    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                        row.forEach { (label, value) ->
                            Column(Modifier.weight(1f)) {
                                Text(label, fontSize = 12.sp, color = Stone600)
                                Text(
                                    value,
                                    fontWeight = FontWeight.Bold,
                                    fontFamily = if (label == "Case reference") FontFamily.Monospace else null,
                                )
                            }
                        }
                        if (row.size == 1) Spacer(Modifier.weight(1f))
                    }
                }
            }
        }
    }
}
