package com.nexus.strategies.ui

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ExitToApp
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.automirrored.filled.List
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material3.*
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(userId: String, name: String, onSignOut: () -> Unit) {
    val vm: HomeViewModel = viewModel(key = "home-$userId")
    LaunchedEffect(userId) { vm.load(userId) }
    val ui by vm.ui.collectAsStateWithLifecycle()
    var tab by rememberSaveable { mutableIntStateOf(0) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("Nexus Strategies", fontWeight = FontWeight.Bold)
                        Text("Hello, $name", style = MaterialTheme.typography.bodySmall)
                    }
                },
                actions = {
                    IconButton(onClick = onSignOut) {
                        Icon(Icons.AutoMirrored.Filled.ExitToApp, contentDescription = "Sign out")
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = Brand,
                    titleContentColor = Color.White,
                    actionIconContentColor = Color.White,
                ),
            )
        },
        bottomBar = {
            NavigationBar(containerColor = Color.White) {
                NavigationBarItem(
                    selected = tab == 0, onClick = { tab = 0 },
                    icon = { Icon(Icons.Default.CheckCircle, contentDescription = null) },
                    label = { Text("Progress") },
                )
                NavigationBarItem(
                    selected = tab == 1, onClick = { tab = 1 },
                    icon = { Icon(Icons.AutoMirrored.Filled.List, contentDescription = null) },
                    label = { Text("Documents") },
                )
                NavigationBarItem(
                    selected = tab == 2, onClick = { tab = 2 },
                    icon = {
                        BadgedBox(badge = {
                            if (ui.unread > 0) Badge { Text(if (ui.unread > 99) "99+" else ui.unread.toString()) }
                        }) { Icon(Icons.Default.Notifications, contentDescription = null) }
                    },
                    label = { Text("Messages") },
                )
            }
        },
    ) { pad ->
        PullToRefreshBox(
            isRefreshing = ui.loading,
            onRefresh = { vm.refresh() },
            modifier = Modifier.padding(pad).fillMaxSize(),
        ) {
            when (tab) {
                0 -> ProgressTab(ui, onRetry = { vm.refresh() })
                1 -> DocumentsTab(ui, onRetry = { vm.refresh() })
                else -> NotificationsTab(ui, onOpen = vm::markRead, onRetry = { vm.refresh() })
            }
        }
    }
}

@Composable
fun ErrorBanner(message: String, onRetry: () -> Unit) {
    Card(colors = CardDefaults.cardColors(containerColor = BrandLight), modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp)) {
            Text(message, color = BrandPressed)
            TextButton(onClick = onRetry) { Text("Try again") }
        }
    }
}

@Composable
fun EmptyState(title: String, body: String) {
    Column(Modifier.fillMaxWidth().padding(vertical = 32.dp, horizontal = 8.dp)) {
        Text(title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(4.dp))
        Text(body, color = Stone600)
    }
}
