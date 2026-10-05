import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { HashRouter } from 'react-router-dom'
import '@fontsource/ibm-plex-sans/400.css'
import '@fontsource/ibm-plex-sans/500.css'
import '@fontsource/ibm-plex-sans/600.css'
import '@fontsource/ibm-plex-sans/700.css'
import './index.css'
import App from './App.jsx'
import AppErrorFallback from './components/AppErrorFallback.jsx'
import ErrorBoundary from './components/ErrorBoundary.jsx'
import ToastProvider from './components/ui/ToastProvider.jsx'

// The outer boundary is the last resort: if anything above the page level fails, users get a
// branded screen with a way back instead of a blank page. Pages have their own boundary in AppShell.
createRoot(document.getElementById('root')).render(
  <StrictMode>
    <ErrorBoundary fallback={() => <AppErrorFallback />}>
      <HashRouter>
        <ToastProvider>
          <App />
        </ToastProvider>
      </HashRouter>
    </ErrorBoundary>
  </StrictMode>,
)
