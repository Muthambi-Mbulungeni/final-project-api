package com.nexus.strategies.ui

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.automirrored.filled.List
import androidx.compose.material3.*
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.nexus.strategies.R

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(userId: String, name: String, onSignOut: () -> Unit) {
    val vm: HomeViewModel = viewModel(key = "home-$userId")
    LaunchedEffect(userId) { vm.load(userId) }
    val ui by vm.ui.collectAsStateWithLifecycle()
    var tab by rememberSaveable { mutableIntStateOf(0) }

    Scaffold(
        containerColor = Stone50,
        topBar = { TopBar(name, ui.unread, onBell = { tab = 2 }, onSignOut = onSignOut) },
        bottomBar = { BottomBar(tab, ui.unread) { tab = it } },
    ) { pad ->
        PullToRefreshBox(
            isRefreshing = ui.loading,
            onRefresh = { vm.refresh() },
            modifier = Modifier.padding(pad).fillMaxSize(),
        ) {
            when (tab) {
                0 -> ProgressTab(ui, name, onOpenDocuments = { tab = 1 }, onRetry = { vm.refresh() })
                1 -> DocumentsTab(ui, onRetry = { vm.refresh() })
                else -> NotificationsTab(ui, onOpen = vm::markRead, onRetry = { vm.refresh() })
            }
        }
    }
}

/** White header like the web: logo on the left, bell with unread badge and the user's initials on the right. */
@Composable
private fun TopBar(name: String, unread: Int, onBell: () -> Unit, onSignOut: () -> Unit) {
    var menu by remember { mutableStateOf(false) }
    Surface(color = Color.White) {
        Column {
            Row(
                Modifier.statusBarsPadding().height(56.dp).padding(horizontal = 16.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Image(
                    painter = painterResource(R.drawable.strategies_logo),
                    contentDescription = "Strategies Migration Services",
                    modifier = Modifier.height(30.dp),
                )
                Spacer(Modifier.weight(1f))
                IconButton(onClick = onBell) {
                    BadgedBox(badge = {
                        if (unread > 0) Badge(containerColor = Brand, contentColor = Color.White) {
                            Text(if (unread > 99) "99+" else unread.toString())
                        }
                    }) {
                        Icon(
                            Icons.Default.Notifications,
                            contentDescription = if (unread > 0) "Messages, $unread unread" else "Messages",
                            tint = Stone600,
                        )
                    }
                }
                Spacer(Modifier.width(8.dp))
                Box {
                    Box(
                        Modifier
                            .size(36.dp)
                            .clip(CircleShape)
                            .background(Stone900)
                            .clickable(onClickLabel = "Open account menu") { menu = true },
                        contentAlignment = Alignment.Center,
                    ) {
                        Text(initials(name), color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.Bold)
                    }
                    DropdownMenu(expanded = menu, onDismissRequest = { menu = false }, containerColor = Color.White) {
                        DropdownMenuItem(text = { Text(name, fontWeight = FontWeight.SemiBold) }, onClick = {}, enabled = false)
                        DropdownMenuItem(text = { Text("Sign out") }, onClick = { menu = false; onSignOut() })
                    }
                }
            }
            HorizontalDivider(color = Stone200)
        }
    }
}

private fun initials(name: String): String {
    val parts = name.trim().split(Regex("\\s+")).filter { it.isNotEmpty() }
    return when {
        parts.isEmpty() -> "?"
        parts.size == 1 -> parts[0].take(1).uppercase()
        else -> (parts.first().take(1) + parts.last().take(1)).uppercase()
    }
}

@Composable
private fun BottomBar(tab: Int, unread: Int, onSelect: (Int) -> Unit) {
    val colors = NavigationBarItemDefaults.colors(
        selectedIconColor = Brand,
        selectedTextColor = Brand,
        indicatorColor = BrandHalo,
        unselectedIconColor = Stone600,
        unselectedTextColor = Stone600,
    )
    NavigationBar(containerColor = Color.White) {
        NavigationBarItem(
            selected = tab == 0, onClick = { onSelect(0) }, colors = colors,
            icon = { Icon(Icons.Default.Home, contentDescription = null) },
            label = { Text("My case") },
        )
        NavigationBarItem(
            selected = tab == 1, onClick = { onSelect(1) }, colors = colors,
            icon = { Icon(Icons.AutoMirrored.Filled.List, contentDescription = null) },
            label = { Text("Documents") },
        )
        NavigationBarItem(
            selected = tab == 2, onClick = { onSelect(2) }, colors = colors,
            icon = {
                BadgedBox(badge = {
                    if (unread > 0) Badge(containerColor = Brand, contentColor = Color.White) {
                        Text(if (unread > 99) "99+" else unread.toString())
                    }
                }) { Icon(Icons.Default.Email, contentDescription = null) }
            },
            label = { Text("Messages") },
        )
    }
}
