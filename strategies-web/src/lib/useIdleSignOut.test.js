import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createIdleTimer } from './useIdleSignOut'

const MINUTE = 60 * 1000

describe('createIdleTimer', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('fires once after the timeout with no activity', () => {
    const onIdle = vi.fn()
    createIdleTimer(30 * MINUTE, onIdle)

    vi.advanceTimersByTime(30 * MINUTE - 1)
    expect(onIdle).not.toHaveBeenCalled()

    vi.advanceTimersByTime(1)
    vi.advanceTimersByTime(60 * MINUTE)
    expect(onIdle).toHaveBeenCalledTimes(1)
  })

  it('starts the countdown again on activity', () => {
    const onIdle = vi.fn()
    const idle = createIdleTimer(30 * MINUTE, onIdle)

    vi.advanceTimersByTime(29 * MINUTE)
    idle.touch()
    vi.advanceTimersByTime(29 * MINUTE)
    expect(onIdle).not.toHaveBeenCalled()

    vi.advanceTimersByTime(1 * MINUTE)
    expect(onIdle).toHaveBeenCalledTimes(1)
  })

  it('ignores activity less than a second after the last reset', () => {
    const onIdle = vi.fn()
    const idle = createIdleTimer(10 * MINUTE, onIdle)

    vi.advanceTimersByTime(500)
    idle.touch()
    vi.advanceTimersByTime(10 * MINUTE - 500)

    expect(onIdle).toHaveBeenCalledTimes(1)
  })

  it('does nothing after being stopped', () => {
    const onIdle = vi.fn()
    const idle = createIdleTimer(10 * MINUTE, onIdle)

    idle.stop()
    vi.advanceTimersByTime(60 * MINUTE)

    expect(onIdle).not.toHaveBeenCalled()
  })

  it('warns before signing out, passing the time the session ends', () => {
    const onIdle = vi.fn()
    const onWarn = vi.fn()
    createIdleTimer(30 * MINUTE, onIdle, { warningMs: MINUTE, onWarn })

    vi.advanceTimersByTime(29 * MINUTE)
    expect(onWarn).toHaveBeenCalledTimes(1)
    expect(onWarn).toHaveBeenCalledWith(Date.now() + MINUTE)
    expect(onIdle).not.toHaveBeenCalled()

    vi.advanceTimersByTime(MINUTE)
    expect(onIdle).toHaveBeenCalledTimes(1)
  })

  it('ignores ordinary activity once the warning is showing', () => {
    const onIdle = vi.fn()
    const idle = createIdleTimer(30 * MINUTE, onIdle, { warningMs: MINUTE, onWarn: vi.fn() })

    vi.advanceTimersByTime(29 * MINUTE + 5000)
    idle.touch()
    vi.advanceTimersByTime(MINUTE - 5000)

    expect(onIdle).toHaveBeenCalledTimes(1)
  })

  it('extend() restarts the full countdown, warning included', () => {
    const onIdle = vi.fn()
    const onWarn = vi.fn()
    const idle = createIdleTimer(30 * MINUTE, onIdle, { warningMs: MINUTE, onWarn })

    vi.advanceTimersByTime(29 * MINUTE)
    idle.extend()

    vi.advanceTimersByTime(29 * MINUTE - 1)
    expect(onWarn).toHaveBeenCalledTimes(1)
    expect(onIdle).not.toHaveBeenCalled()

    vi.advanceTimersByTime(1)
    expect(onWarn).toHaveBeenCalledTimes(2)

    vi.advanceTimersByTime(MINUTE)
    expect(onIdle).toHaveBeenCalledTimes(1)
  })
})
