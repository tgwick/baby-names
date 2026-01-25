import { test, expect } from '@playwright/test'

// These tests run with pre-authenticated state from auth.setup.ts

// Run tests serially since they share auth state
test.describe.configure({ mode: 'serial' })

test.describe('Preferences Flow', () => {
  // Helper to ensure we have a session (create if needed, or use existing)
  async function ensureSession(page: import('@playwright/test').Page) {
    // Go to /session - it will redirect to /dashboard if no session exists
    await page.goto('/session')
    await page.waitForTimeout(2000) // Wait for session fetch and potential redirect

    const currentUrl = page.url()

    // If we stayed on /session, we have an active session
    if (currentUrl.endsWith('/session') && !currentUrl.includes('/create') && !currentUrl.includes('/join')) {
      return
    }

    // No session - navigate to create page and create one
    await page.goto('/session/create')
    await expect(page.getByRole('heading', { name: /build your nest/i })).toBeVisible()

    await page.getByRole('button', { name: /all names/i }).click()
    await page.getByRole('button', { name: /build nest/i }).click()

    // Wait for navigation - might redirect to /session or show error
    await page.waitForTimeout(3000)

    // Check if we got an error (session already exists from another test run)
    const hasError = await page.evaluate(() => {
      return document.body.innerText.toLowerCase().includes('already have an active session')
    })

    if (hasError) {
      // Session exists, just navigate to it
      await page.goto('/session')
      await page.waitForTimeout(1000)
      return
    }

    // Should be on /session now
    await expect(page).toHaveURL('/session', { timeout: 5000 })
  }

  test('should show preferences link on session page', async ({ page }) => {
    await ensureSession(page)

    // Should show preferences link (either "Set Your Preferences" or "Update preferences")
    const prefsLink = page.getByRole('link', { name: /preferences/i })
    await expect(prefsLink).toBeVisible()
  })

  test('should navigate to preferences page and show questionnaire', async ({ page }) => {
    await ensureSession(page)

    // Click on preferences link
    await page.getByRole('link', { name: /preferences/i }).first().click()

    // Should be on preferences page
    await expect(page).toHaveURL('/preferences')

    // Should show either filter questionnaire or completed summary
    // Use page.evaluate for reliable text checking
    const pageText = await page.evaluate(() => document.body.innerText.toLowerCase())
    const hasQuestionnaire = pageText.includes('what kind of names')
    const hasCompleted = pageText.includes('setup complete')

    expect(hasQuestionnaire || hasCompleted).toBeTruthy()
  })

  test('should complete filter questionnaire and save', async ({ page }) => {
    await ensureSession(page)

    // Navigate to preferences
    await page.getByRole('link', { name: /preferences/i }).first().click()
    await expect(page).toHaveURL('/preferences')

    // If already completed, click update to restart
    const updateBtn = page.getByRole('button', { name: /update my filters/i })
    if (await updateBtn.isVisible().catch(() => false)) {
      await updateBtn.click()
    }

    // Question 1: What kind of names are you looking for? (name style)
    await expect(page.getByText(/what kind of names/i)).toBeVisible()
    await page.getByRole('button', { name: /classic/i }).click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 2: How long should the name sound? (syllables)
    await expect(page.getByText(/how long should the name/i)).toBeVisible()
    await page.getByRole('button', { name: /medium/i }).click()
    await page.getByRole('button', { name: /continue/i }).click()

    // Should show completion summary
    await expect(page.getByText(/setup complete/i)).toBeVisible({ timeout: 5000 })

    // Should show filter choices
    await expect(page.getByText(/classic names/i)).toBeVisible()
    await expect(page.getByText(/medium/i)).toBeVisible()
  })

  test('should allow skipping questions', async ({ page }) => {
    await ensureSession(page)

    // Navigate directly to preferences (link may not be visible if filters already completed)
    await page.goto('/preferences')
    await expect(page).toHaveURL('/preferences')

    // If already completed, click update to restart
    const updateBtn = page.getByRole('button', { name: /update my filters/i })
    if (await updateBtn.isVisible().catch(() => false)) {
      await updateBtn.click()
    }

    // Skip question 1
    await expect(page.getByText(/what kind of names/i)).toBeVisible()
    await page.getByRole('button', { name: /skip/i }).click()

    // Skip question 2
    await expect(page.getByText(/how long should the name/i)).toBeVisible()
    await page.getByRole('button', { name: /skip/i }).click()

    // Should show completion summary
    await expect(page.getByText(/setup complete/i)).toBeVisible({ timeout: 5000 })
  })

  test('should allow updating filters after initial submission', async ({ page }) => {
    await ensureSession(page)

    // Navigate directly to preferences (link may not be visible if filters already completed)
    await page.goto('/preferences')
    await expect(page).toHaveURL('/preferences')

    // Wait for page to load - check for either the completion screen or questionnaire
    await page.waitForLoadState('networkidle')

    // Check if we're on the completion screen (filters already submitted)
    const pageText = await page.evaluate(() => document.body.innerText.toLowerCase())
    const isCompleted = pageText.includes('setup complete')

    if (!isCompleted) {
      // Complete the questionnaire
      await page.getByRole('button', { name: /trendy/i }).click()
      await page.getByRole('button', { name: /next/i }).click()
      await page.getByRole('button', { name: /short.*punchy/i }).click()
      await page.getByRole('button', { name: /continue/i }).click()
      await expect(page.getByText(/setup complete/i)).toBeVisible({ timeout: 5000 })
    }

    // Now click update to restart
    await page.getByRole('button', { name: /update my filters/i }).click()

    // Should be back at the questionnaire
    await expect(page.getByText(/what kind of names/i)).toBeVisible()

    // Select different options
    await page.getByRole('button', { name: /unique/i }).click()
    await page.getByRole('button', { name: /next/i }).click()

    await page.getByRole('button', { name: /flowing.*elegant/i }).click()
    await page.getByRole('button', { name: /continue/i }).click()

    // Should show updated choices
    await expect(page.getByText(/setup complete/i)).toBeVisible({ timeout: 5000 })
    await expect(page.getByText(/unique names/i)).toBeVisible()
  })
})
