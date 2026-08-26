import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from './App'
import './styles.css'

const rootElement = document.getElementById('root')
const isRegionOverlay =
  new URLSearchParams(window.location.search).get('surface') === 'region-overlay'

if (isRegionOverlay) {
  document.documentElement.dataset.surface = 'region-overlay'
}

if (rootElement === null) {
  throw new Error('Renderer root element was not found.')
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
