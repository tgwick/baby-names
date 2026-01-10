import { test, expect } from '@playwright/test'

// These tests run with pre-authenticated state from auth.setup.ts

test.describe('Authenticated Session Flow', () => {
  test('should display create session form with gender options', async ({ page }) => {
    await page.goto('/session/create')

    await expect(page.getByRole('heading', { name: /build your nest/i })).toBeVisible()
    await expect(page.getByRole('button', { name: /boy names/i })).toBeVisible()
    await expect(page.getByRole('button', { name: /girl names/i })).toBeVisible()
    await expect(page.getByRole('button', { name: /all names/i })).toBeVisible()
    await expect(page.getByRole('button', { name: /build nest/i })).toBeVisible()
  })

  test('should have create button disabled when no gender selected', async ({ page }) => {
    await page.goto('/session/create')

    // Create button should be disabled when no gender is selected
    await expect(page.getByRole('button', { name: /build nest/i })).toBeDisabled()

    // After selecting a gender, the button should be enabled
    await page.getByRole('button', { name: /boy names/i }).click()
    await expect(page.getByRole('button', { name: /build nest/i })).toBeEnabled()
  })

  test('should create session and display join code', async ({ page }) => {
    await page.goto('/session/create')

    // Select a gender option
    await page.getByRole('button', { name: /girl names/i }).click()
    await page.getByRole('button', { name: /build nest/i }).click()

    // Should redirect to session page with waiting status visible
    await expect(page).toHaveURL('/session', { timeout: 10000 })
    await expect(page.getByRole('heading', { name: /waiting for your partner/i })).toBeVisible()
  })

  test('should display join session form', async ({ page }) => {
    await page.goto('/session/join')

    await expect(page.getByRole('heading', { name: /join your partner/i })).toBeVisible()
    await expect(page.getByPlaceholder(/xxxxxx/i)).toBeVisible()
    await expect(page.getByRole('button', { name: /join session/i })).toBeVisible()
  })

  test('should show error for invalid join code', async ({ page }) => {
    await page.goto('/session/join')

    await page.getByPlaceholder(/xxxxxx/i).fill('ZZZZZZ')
    await page.getByRole('button', { name: /join session/i }).click()

    await expect(page.getByText(/not found|invalid|check/i)).toBeVisible({ timeout: 10000 })
  })
})

test.describe('Dashboard Session State', () => {
  test('should show create/join options when no active session', async ({ page }) => {
    await page.goto('/dashboard')

    // Look for the specific section headings on the dashboard
    await expect(page.getByRole('heading', { name: /start a new session/i })).toBeVisible()
    await expect(page.getByRole('heading', { name: /join a session/i })).toBeVisible()
  })
})
