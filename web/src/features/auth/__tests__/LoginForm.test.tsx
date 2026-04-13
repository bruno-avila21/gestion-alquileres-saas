import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { LoginForm } from '../components/LoginForm'

describe('LoginForm', () => {
  it('shows validation errors when submitting empty form', async () => {
    const onSubmit = vi.fn()
    render(<LoginForm onSubmit={onSubmit} isPending={false} submitLabel="Ingresar" />)
    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: /ingresar/i }))
    await waitFor(() => {
      expect(screen.getAllByRole('alert').length).toBeGreaterThanOrEqual(1)
    })
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('calls onSubmit with typed values when form is valid', async () => {
    const onSubmit = vi.fn()
    render(<LoginForm onSubmit={onSubmit} isPending={false} submitLabel="Ingresar" />)
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/organización/i), 'acme')
    await user.type(screen.getByLabelText(/email/i), 'admin@acme.com')
    await user.type(screen.getByLabelText(/contraseña/i), 'SuperSecret123')
    await user.click(screen.getByRole('button', { name: /ingresar/i }))
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith({
        organizationSlug: 'acme',
        email: 'admin@acme.com',
        password: 'SuperSecret123',
      })
    })
  })

  it('rejects invalid email format via zod', async () => {
    const onSubmit = vi.fn()
    render(<LoginForm onSubmit={onSubmit} isPending={false} submitLabel="Ingresar" />)
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/organización/i), 'acme')
    // Use fireEvent.change to bypass jsdom native type="email" constraint validation
    // so zod resolver can run and produce its own error message
    const emailInput = screen.getByLabelText(/email/i)
    fireEvent.change(emailInput, { target: { value: 'not-an-email' } })
    await user.type(screen.getByLabelText(/contraseña/i), 'pass')
    // Submit via fireEvent to bypass native form validation entirely
    const form = screen.getByRole('form', { name: /login-form/i })
    fireEvent.submit(form)
    await waitFor(() => {
      const alerts = screen.getAllByRole('alert')
      const hasEmailError = alerts.some((el) =>
        el.textContent?.toLowerCase().includes('email'),
      )
      expect(hasEmailError).toBe(true)
    })
    expect(onSubmit).not.toHaveBeenCalled()
  })
})
