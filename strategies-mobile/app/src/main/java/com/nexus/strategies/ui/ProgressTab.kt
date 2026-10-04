package com.nexus.strategies.ui

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.nexus.strategies.data.CaseDto
import com.nexus.strategies.data.Stage
import com.nexus.strategies.data.formatDate

@Composable
fun ProgressTab(ui: HomeUi, onRetry: () -> Unit) {
    LazyColumn(
        Modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        ui.error?.let { item { ErrorBanner(it, onRetry) } }

        val case = ui.case
        if (case == null) {
            if (!ui.loading && ui.error == null) item {
                EmptyState(
                    "No case yet",
                    "Once your enquiry has been assessed, your case and its progress will appear here.",
                )
            }
        } else {
            item { CaseSummary(case) }
            item {
                Card(colors = CardDefaults.cardColors(containerColor = Color.White), border = BorderStroke(1.dp, Stone200)) {
                    Column(Modifier.padding(16.dp)) {
                        Text("Your progress", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                        Spacer(Modifier.height(16.dp))
                        CaseStepper(case.stageEnum)
                    }
                }
            }
            item { WhatHappensNext(case.stageEnum) }
        }
    }
}

@Composable
private fun CaseSummary(case: CaseDto) {
    Card(colors = CardDefaults.cardColors(containerColor = Color.White), border = BorderStroke(1.dp, Stone200)) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
            Text("Case reference", color = Stone600, style = MaterialTheme.typography.labelMedium)
            Text(case.reference, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold, color = Brand)
            // Placeholder values ("N/A", "Unassigned") are hidden rather than printed.
            case.visaCategoryOrNull?.let { DetailRow("Visa category", it) }
            case.consultantOrNull?.let { DetailRow("Consultant", it) }
            formatDate(case.createdAt)?.let { DetailRow("Opened", it) }
            formatDate(case.lastUpdated)?.let { DetailRow("Last updated", it) }
        }
    }
}

@Composable
private fun DetailRow(label: String, value: String) {
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
        Text(label, color = Stone600)
        Spacer(Modifier.width(12.dp))
        Text(value, fontWeight = FontWeight.Medium)
    }
}

private enum class StepState { Done, Current, Upcoming }

/** Vertical stepper, matching the web portal: red tick = done, red ring = current, grey = upcoming. */
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
                        // Line is red if the stage above it is complete, grey otherwise.
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
                        fontWeight = if (state == StepState.Current) FontWeight.Bold else FontWeight.Medium,
                        color = if (state == StepState.Upcoming) Stone500 else MaterialTheme.colorScheme.onSurface,
                    )
                    Text(stage.hint, style = MaterialTheme.typography.bodySmall, color = Stone600)
                }
            }
        }
    }
}

@Composable
private fun StepCircle(number: Int, state: StepState) {
    // Outer 40dp box carries the soft halo for the current stage; every state uses the same size.
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
            StepState.Current -> Box(inner.background(Color.White).border(2.dp, Brand, CircleShape), contentAlignment = Alignment.Center) {
                Text(number.toString(), color = Brand, fontWeight = FontWeight.Bold)
            }
            StepState.Upcoming -> Box(inner.background(Color.White).border(1.dp, Stone300, CircleShape), contentAlignment = Alignment.Center) {
                Text(number.toString(), color = Stone500)
            }
        }
    }
}

@Composable
private fun WhatHappensNext(current: Stage) {
    val next = Stage.entries.getOrNull(current.ordinal + 1)
    Card(colors = CardDefaults.cardColors(containerColor = BrandLight)) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Text("What happens next", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
            Text("Stage ${current.ordinal + 1} of ${Stage.entries.size}", color = BrandPressed, style = MaterialTheme.typography.labelMedium)
            Text(current.label, fontWeight = FontWeight.Bold)
            Text(current.hint)
            Spacer(Modifier.height(4.dp))
            Text(
                if (next != null) "Next: ${next.label}. ${next.hint}" else "This is the final stage of your case.",
                color = Stone600,
            )
        }
    }
}
