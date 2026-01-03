import { test, expect } from '@playwright/test'

test.describe('Session Management', () => {
  test.describe('Create Session Page', () => {
    test('should redirect to login when not authenticated', async ({ page }) => {
      await page.goto('/session/create')

      await expect(page).toHaveURL('/login')
    })
  })

  test.describe('Join Session Page', () => {
    test('should redirect to login when not authenticated', async ({ page }) => {
      await page.goto('/session/join')

      await expect(page).toHaveURL('/login')
    })
  })

  test.describe('Join by Link', () => {
    test('should redirect to login when accessing join link without auth', async ({ page }) => {
      await page.goto('/join/some-partner-link')

      // Should redirect to login but store the pending link
      await expect(page).toHaveURL('/login')
    })
  })

  test.describe('Session Page', () => {
    test('should redirect to login when not authenticated', async ({ page }) => {
      await page.goto('/session')

      await expect(page).toHaveURL('/login')
    })
  })
})

// These tests require authentication setup - using storage state
test.describe('Authenticated Session Flow', () => {
  // Note: These tests are skipped by default as they require full backend integration
  // To run them, ensure both frontend and backend are running with a test database

  test.skip('should display create session form with gender options', async ({ page }) => {
    // Would need to set up authentication state first
    await page.goto('/session/create')

    await expect(page.getByRole('heading', { name: /start your journey/i })).toBeVisible()
    await expect(page.getByText(/boy names/i)).toBeVisible()
    await expect(page.getByText(/girl names/i)).toBeVisible()
    await expect(page.getByText(/all names/i)).toBeVisible()
    await expect(page.getByRole('button', { name: /create session/i })).toBeVisible()
  })

  test.skip('should show validation error when no gender selected', async ({ page }) => {
    await page.goto('/session/create')

    await page.getByRole('button', { name: /create session/i }).click()

    await expect(page.getByText(/please select a name category/i)).toBeVisible()
  })

  test.skip('should create session and display join code', async ({ page }) => {
    await page.goto('/session/create')

    // Select a gender option
    await page.getByText(/girl names/i).click()
    await page.getByRole('button', { name: /create session/i }).click()

    // Should redirect to session page with join code visible
    await expect(page).toHaveURL('/session', { timeout: 10000 })
    await expect(page.getByText(/waiting for partner/i)).toBeVisible()
  })

  test.skip('should display join session form', async ({ page }) => {
    await page.goto('/session/join')

    await expect(page.getByRole('heading', { name: /join.*session/i })).toBeVisible()
    await expect(page.getByPlaceholder(/enter.*code/i)).toBeVisible()
    await expect(page.getByRole('button', { name: /join session/i })).toBeVisible()
  })

  test.skip('should show error for invalid join code', async ({ page }) => {
    await page.goto('/session/join')

    await page.getByPlaceholder(/enter.*code/i).fill('INVALID')
    await page.getByRole('button', { name: /join session/i }).click()

    await expect(page.getByText(/not found/i)).toBeVisible({ timeout: 10000 })
  })
})

// Dashboard session state tests
test.describe('Dashboard Session State', () => {
  test.skip('should show create/join options when no active session', async ({ page }) => {
    await page.goto('/dashboard')

    await expect(page.getByText(/start a new session/i)).toBeVisible()
    await expect(page.getByText(/join a session/i)).toBeVisible()
  })

  test.skip('should show active session banner when session exists', async ({ page }) => {
    // This would require setting up a session first
    await page.goto('/dashboard')

    // When session is waiting for partner
    await expect(page.getByText(/waiting for partner/i)).toBeVisible()
    await expect(page.getByText(/share code/i)).toBeVisible()
  })

  test.skip('should disable create/join when session is active', async ({ page }) => {
    await page.goto('/dashboard')

    // The create/join cards should be visually disabled
    await expect(page.locator('[class*="opacity-60"]')).toHaveCount(2)
  })
})
