import { Component } from 'react'

// Catches errors thrown while rendering its children and shows `fallback` instead of a blank screen.
// React 19 still needs a class for this. React itself logs the error to the console, so nothing is
// logged here, and no error details are ever shown to users.
// When `resetKey` changes (for example, the user opens another page) the boundary clears itself.
export default class ErrorBoundary extends Component {
  state = { error: null }

  static getDerivedStateFromError(error) {
    return { error }
  }

  componentDidUpdate(previousProps) {
    if (this.state.error && previousProps.resetKey !== this.props.resetKey) {
      this.setState({ error: null })
    }
  }

  reset = () => {
    this.setState({ error: null })
  }

  render() {
    if (this.state.error) return this.props.fallback({ reset: this.reset })
    return this.props.children
  }
}
