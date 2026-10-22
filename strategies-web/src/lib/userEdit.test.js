import { describe, expect, it } from 'vitest'
import { ApiError } from './api'
import { changedUserFields, roleName, userEditErrorOutcome, userEditPath, userEditValues, validateUserEdit } from './userEdit'

const USER = { id: 'u1', fullName: 'Sipho Ndlovu', email: 'sipho@nexus.com', role: 'Consultant', isActive: true }

describe('userEditPath', () => {
  it('encodes the id', () => {
    expect(userEditPath('a/b')).toBe('/api/admin/users/a%2Fb')
  })
})

describe('roleName', () => {
  it('accepts the API name or the enum index', () => {
    expect(roleName('Admin')).toBe('Admin')
    expect(roleName(2)).toBe('Client')
    expect(roleName('Owner')).toBe('')
    expect(roleName(undefined)).toBe('')
  })
})

describe('userEditValues', () => {
  it('starts from the saved values', () => {
    expect(userEditValues(USER)).toEqual({ fullName: 'Sipho Ndlovu', email: 'sipho@nexus.com', role: 'Consultant' })
  })

  it('fills gaps with empty strings', () => {
    expect(userEditValues({ id: 'x' })).toEqual({ fullName: '', email: '', role: '' })
  })
})

describe('changedUserFields', () => {
  it('is empty when nothing changed', () => {
    expect(changedUserFields(USER, userEditValues(USER))).toEqual({})
  })

  it('ignores spaces added around a value', () => {
    expect(changedUserFields(USER, { ...userEditValues(USER), fullName: '  Sipho Ndlovu ' })).toEqual({})
  })

  it('returns only the changed fields, trimmed', () => {
    expect(changedUserFields(USER, { ...userEditValues(USER), email: ' sipho.n@nexus.com ' })).toEqual({
      email: 'sipho.n@nexus.com',
    })
    expect(changedUserFields(USER, { fullName: 'S. Ndlovu', email: USER.email, role: 'Admin' })).toEqual({
      fullName: 'S. Ndlovu',
      role: 'Admin',
    })
  })

  it('compares a role sent as an index by its name', () => {
    expect(changedUserFields({ ...USER, role: 1 }, { ...userEditValues(USER), role: 'Consultant' })).toEqual({})
  })
})

describe('validateUserEdit', () => {
  it('accepts valid values', () => {
    expect(validateUserEdit(userEditValues(USER))).toEqual({})
  })

  it('requires a name, a valid email and a role', () => {
    expect(validateUserEdit({ fullName: '  ', email: '', role: '' })).toEqual({
      fullName: 'Enter the user’s full name.',
      email: 'Enter an email address.',
      role: 'Choose a role.',
    })
    expect(validateUserEdit({ ...userEditValues(USER), email: 'sipho@nexus' }).email).toBe(
      'Enter a valid email address, like name@example.com.',
    )
  })
})

describe('userEditErrorOutcome', () => {
  it('puts a 409 under the email field', () => {
    const error = new ApiError('That email address is already in use.', { status: 409 })
    expect(userEditErrorOutcome(error)).toEqual({ email: 'That email address is already in use.' })
  })

  it('lists 400 field errors together', () => {
    const error = new ApiError('Validation failed.', { status: 400, errors: ['Full name is too long.', 'Role is invalid.'] })
    expect(userEditErrorOutcome(error)).toEqual({ form: ['Full name is too long.', 'Role is invalid.'] })
  })

  it('falls back to the API message, then to a plain sentence', () => {
    expect(userEditErrorOutcome(new ApiError('User not found.', { status: 404 }))).toEqual({ form: ['User not found.'] })
    expect(userEditErrorOutcome(new ApiError('', { status: 0 }))).toEqual({
      form: ['The changes were not saved. Please try again.'],
    })
  })
})
