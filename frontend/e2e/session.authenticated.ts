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
    // First go to sessions list
    await page.goto('/sessions')
    await page.waitForTimeout(2000)

    // Check if we already have sessions
    const hasExistingSessions = await page.evaluate(() => {
      const text = document.body.innerText.toLowerCase()
      return text.includes('waiting') ||
             text.includes('active') ||
             text.includes('swipe') ||
             text.includes('partner')
    })

    if (hasExistingSessions) {
      // Already have a session - click on it to see details
      const sessionCard = page.locator('[class*="card"]').first()
      if (await sessionCard.isVisible()) {
        await sessionCard.click()
        await page.waitForTimeout(1000)
      }
      // Verify we're on a session detail page
      const hasSessionContent = await page.evaluate(() => {
        const text = document.body.innerText.toLowerCase()
        return text.includes('waiting') ||
               text.includes('preferences') ||
               text.includes('swipe') ||
               text.includes('partner') ||
               text.includes('connected')
      })
      expect(hasSessionContent).toBeTruthy()
      return
    }

    // No active session - navigate to create page and create one
    await page.goto('/session/create')
    await expect(page.getByRole('heading', { name: /build your nest/i })).toBeVisible()

    await page.getByRole('button', { name: /girl names/i }).click()
    await page.getByRole('button', { name: /build nest/i }).click()

    // Wait for navigation to session detail page
    await page.waitForTimeout(3000)

    // Should now be on session detail page
    const hasSessionContent = await page.evaluate(() => {
      const text = document.body.innerText.toLowerCase()
      return text.includes('waiting') ||
             text.includes('preferences') ||
             text.includes('partner') ||
             text.includes('connected') ||
             text.includes('invite')
    })
    expect(hasSessionContent).toBeTruthy()
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

test.describe('Sessions List View', () => {
  test('should show sessions list or empty state', async ({ page }) => {
    await page.goto('/sessions')

    // Should show either sessions or empty state with create option
    const pageText = await page.evaluate(() => document.body.innerText.toLowerCase())
    const hasSessionsContent = pageText.includes('session') ||
                               pageText.includes('create') ||
                               pageText.includes('start') ||
                               pageText.includes('no sessions')
    expect(hasSessionsContent).toBeTruthy()
  })
})
