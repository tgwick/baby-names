import { test, expect } from '@playwright/test'

// These tests run with pre-authenticated state from auth.setup.ts

// Run tests serially since they share auth state
test.describe.configure({ mode: 'serial' })

// Store the session ID for use across tests
let sessionId: string | null = null

test.describe('Preferences Flow', () => {
  // Helper to ensure we have a session and get its ID
  async function ensureSession(page: import('@playwright/test').Page): Promise<string> {
    // If we already have a session ID from a previous test, use it
    if (sessionId) {
      await page.goto(`/sessions/${sessionId}`)
      await page.waitForTimeout(1000)
      // Verify session still exists
      const url = page.url()
      if (url.includes(`/sessions/${sessionId}`)) {
        return sessionId
      }
    }

    // Go to sessions list
    await page.goto('/sessions')
    await page.waitForTimeout(2000)

    // Check if we have any sessions
    const currentUrl = page.url()
    if (currentUrl.endsWith('/sessions')) {
      // Check for existing sessions by looking for session cards
      const hasExistingSessions = await page.evaluate(() => {
        const text = document.body.innerText.toLowerCase()
        return text.includes('waiting') ||
               text.includes('active') ||
               (text.includes('session') && !text.includes('no sessions'))
      })

      if (hasExistingSessions) {
        // Click on first session to get its ID
        const sessionLink = page.locator('a[href^="/sessions/"]').first()
        if (await sessionLink.isVisible()) {
          const href = await sessionLink.getAttribute('href')
          if (href) {
            sessionId = href.replace('/sessions/', '').split('/')[0]
            await sessionLink.click()
            await page.waitForTimeout(1000)
            return sessionId!
          }
        }
      }
    }

    // No session - create one
    await page.goto('/session/create')
    await expect(page.getByRole('heading', { name: /build your nest/i })).toBeVisible()

    await page.getByRole('button', { name: /all names/i }).click()
    await page.getByRole('button', { name: /build nest/i }).click()

    // Wait for navigation to session detail
    await page.waitForTimeout(3000)

    // Extract session ID from URL
    const newUrl = page.url()
    const match = newUrl.match(/\/sessions\/([^\/]+)/)
    if (match) {
      sessionId = match[1]
      return sessionId
    }

    throw new Error('Failed to create or find a session')
  }

  test('should show preferences link on session page', async ({ page }) => {
    const id = await ensureSession(page)
    await page.goto(`/sessions/${id}`)
    await page.waitForTimeout(1000)

    // Check page content - preferences link or swipe button should be visible
    // If both partners have completed preferences, we'll see "Start Swiping" instead of preferences link
    const pageText = await page.evaluate(() => document.body.innerText.toLowerCase())
    const hasPrefsLink = pageText.includes('preferences') || pageText.includes('set your') || pageText.includes('set now')
    const hasSwipeButton = pageText.includes('start swiping') || pageText.includes('swipe')
    const hasWaitingContent = pageText.includes('waiting') || pageText.includes('invite')

    // One of these should be visible on session page
    expect(hasPrefsLink || hasSwipeButton || hasWaitingContent).toBeTruthy()
  })

  test('should navigate to preferences page and show questionnaire', async ({ page }) => {
    const id = await ensureSession(page)

    // Navigate directly to preferences page
    await page.goto(`/sessions/${id}/preferences`)
    await expect(page).toHaveURL(new RegExp(`/sessions/${id}/preferences`))

    // Wait for content to load
    await page.waitForLoadState('networkidle')

    // Should show either filter questionnaire or completed summary
    const pageText = await page.evaluate(() => document.body.innerText.toLowerCase())
    const hasQuestionnaire = pageText.includes('what kind of names')
    const hasCompleted = pageText.includes('setup complete')

    expect(hasQuestionnaire || hasCompleted).toBeTruthy()
  })

  test('should complete filter questionnaire and save', async ({ page }) => {
    const id = await ensureSession(page)

    // Navigate directly to preferences
    await page.goto(`/sessions/${id}/preferences`)
    await expect(page).toHaveURL(new RegExp(`/sessions/${id}/preferences`))

    // Wait for page to fully load
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Check if filters are already completed and we need to restart
    const pageText = await page.evaluate(() => document.body.innerText.toLowerCase())
    if (pageText.includes('setup complete')) {
      // Click "Update My Filters" button to restart
      await page.getByRole('button', { name: /update my filters/i }).click()
      await page.waitForTimeout(500)
    }

    // Question 1: What kind of names are you looking for? (name style)
    await expect(page.getByText(/what kind of names/i)).toBeVisible({ timeout: 5000 })
    await page.getByRole('button', { name: /classic/i }).click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 2: How long should the name sound? (syllables)
    await expect(page.getByText(/how long should the name/i)).toBeVisible({ timeout: 5000 })
    await page.getByRole('button', { name: /medium/i }).click()
    await page.getByRole('button', { name: /continue/i }).click()

    // Should show completion summary
    await expect(page.getByText(/setup complete/i)).toBeVisible({ timeout: 5000 })

    // Should show filter choices
    await expect(page.getByText(/classic names/i)).toBeVisible()
    await expect(page.getByText(/medium/i)).toBeVisible()
  })

  test('should allow skipping questions', async ({ page }) => {
    const id = await ensureSession(page)

    // Navigate directly to preferences
    await page.goto(`/sessions/${id}/preferences`)
    await expect(page).toHaveURL(new RegExp(`/sessions/${id}/preferences`))

    // Wait for page to fully load
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Check if filters are already completed and we need to restart
    const pageText = await page.evaluate(() => document.body.innerText.toLowerCase())
    if (pageText.includes('setup complete')) {
      // Click "Update My Filters" button to restart
      await page.getByRole('button', { name: /update my filters/i }).click()
      await page.waitForTimeout(500)
    }

    // Should now see the questionnaire - wait for question to appear
    await expect(page.getByText(/what kind of names/i)).toBeVisible({ timeout: 5000 })

    // Skip question 1
    await page.getByRole('button', { name: /skip/i }).click()

    // Skip question 2
    await expect(page.getByText(/how long should the name/i)).toBeVisible({ timeout: 5000 })
    await page.getByRole('button', { name: /skip/i }).click()

    // Should show completion summary
    await expect(page.getByText(/setup complete/i)).toBeVisible({ timeout: 5000 })
  })

  test('should allow updating filters after initial submission', async ({ page }) => {
    const id = await ensureSession(page)

    // Navigate directly to preferences
    await page.goto(`/sessions/${id}/preferences`)
    await expect(page).toHaveURL(new RegExp(`/sessions/${id}/preferences`))

    // Wait for page to load
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Check if we're on the completion screen (filters already submitted)
    const pageText = await page.evaluate(() => document.body.innerText.toLowerCase())
    const isCompleted = pageText.includes('setup complete')

    if (!isCompleted) {
      // Complete the questionnaire first
      await expect(page.getByText(/what kind of names/i)).toBeVisible({ timeout: 5000 })
      await page.getByRole('button', { name: /trendy/i }).click()
      await page.getByRole('button', { name: /next/i }).click()
      await expect(page.getByText(/how long should the name/i)).toBeVisible({ timeout: 5000 })
      await page.getByRole('button', { name: /short/i }).first().click()
      await page.getByRole('button', { name: /continue/i }).click()
      await expect(page.getByText(/setup complete/i)).toBeVisible({ timeout: 5000 })
    }

    // Now click update to restart
    await page.getByRole('button', { name: /update my filters/i }).click()
    await page.waitForTimeout(500)

    // Should be back at the questionnaire
    await expect(page.getByText(/what kind of names/i)).toBeVisible({ timeout: 5000 })

    // Select different options
    await page.getByRole('button', { name: /unique/i }).click()
    await page.getByRole('button', { name: /next/i }).click()

    await expect(page.getByText(/how long should the name/i)).toBeVisible({ timeout: 5000 })
    await page.getByRole('button', { name: /flowing.*elegant/i }).click()
    await page.getByRole('button', { name: /continue/i }).click()

    // Should show updated choices
    await expect(page.getByText(/setup complete/i)).toBeVisible({ timeout: 5000 })
    await expect(page.getByText(/unique names/i)).toBeVisible()
  })
})
