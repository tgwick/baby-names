import { test, expect } from '@playwright/test'

// These tests run with pre-authenticated state from auth.setup.ts

// Run tests serially since they share auth state
test.describe.configure({ mode: 'serial' })

test.describe('Preferences Flow', () => {
  // Helper to ensure we have a session (create if needed, or use existing)
  async function ensureSession(page: import('@playwright/test').Page) {
    // First try to go directly to session page
    await page.goto('/session')
    await page.waitForTimeout(1000) // Wait for any redirects

    const currentUrl = page.url()

    // If we're already on /session (not /create), we have an active session
    if (currentUrl === 'http://localhost:5173/session' || currentUrl.endsWith('/session')) {
      return
    }

    // If redirected to create page, check if there's an "active session" error
    if (currentUrl.includes('/session/create')) {
      const hasActiveSessionError = await page.getByText(/already have an active session/i).isVisible().catch(() => false)

      if (hasActiveSessionError) {
        // Already have a session, navigate to it
        await page.goto('/session')
        await page.waitForTimeout(500)
        return
      }

      // No active session, create one
      await page.getByRole('button', { name: /all names/i }).click()
      await page.getByRole('button', { name: /build nest/i }).click()
      await page.waitForURL('/session', { timeout: 10000 })
      return
    }

    // If on dashboard, navigate to create
    if (currentUrl.includes('/dashboard')) {
      await page.goto('/session/create')
      await page.getByRole('button', { name: /all names/i }).click()
      await page.getByRole('button', { name: /build nest/i }).click()
      await page.waitForURL('/session', { timeout: 10000 })
    }
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

    // Should show either questionnaire or saved preferences
    const hasQuestionnaire = await page.getByText(/what name styles/i).isVisible().catch(() => false)
    const hasSaved = await page.getByRole('heading', { name: /preferences saved/i }).isVisible().catch(() => false)

    expect(hasQuestionnaire || hasSaved).toBeTruthy()
  })

  test('should complete preferences questionnaire and save', async ({ page }) => {
    await ensureSession(page)

    // Navigate to preferences (might already be completed, so reset if needed)
    await page.getByRole('link', { name: /preferences/i }).first().click()
    await expect(page).toHaveURL('/preferences')

    // If already completed, click update to restart
    const updateBtn = page.getByRole('button', { name: /update my preferences/i })
    if (await updateBtn.isVisible().catch(() => false)) {
      await updateBtn.click()
    }

    // Question 1: What name styles appeal to you most?
    await expect(page.getByText(/what name styles appeal to you/i)).toBeVisible()
    await page.getByRole('button', { name: /classic.*traditional/i }).click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 2: Cultural or origin preferences
    await expect(page.getByText(/cultural or origin preferences/i)).toBeVisible()
    await page.getByRole('button', { name: /no preference/i }).first().click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 3: Shorter or longer names
    await expect(page.getByText(/shorter or longer/i)).toBeVisible()
    await page.getByRole('button', { name: /no preference/i }).first().click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 4: Sound preference
    await expect(page.getByText(/what kind of sound/i)).toBeVisible()
    await page.getByRole('button', { name: /no preference/i }).first().click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 5: Biblical names
    await expect(page.getByText(/biblical.*religious/i)).toBeVisible()
    await page.getByRole('button', { name: /they're fine/i }).click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 6: Nature-inspired names
    await expect(page.getByText(/nature-inspired/i)).toBeVisible()
    await page.getByRole('button', { name: /they're fine/i }).click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 7: Trendy or timeless
    await expect(page.getByText(/trendy or timeless/i)).toBeVisible()
    await page.getByRole('button', { name: /no preference/i }).first().click()

    // Submit preferences (button says "Finish" on last question)
    await page.getByRole('button', { name: /finish/i }).click()

    // Should show success state
    await expect(page.getByRole('heading', { name: /preferences saved/i })).toBeVisible({ timeout: 10000 })
    await expect(page.getByRole('button', { name: /back to session/i })).toBeVisible()
  })

  test('should exclude names when "Do not include" is selected', async ({ page }) => {
    await ensureSession(page)

    // Navigate directly to preferences (link may not be visible if already completed)
    await page.goto('/preferences')
    await expect(page).toHaveURL('/preferences')

    // Wait for page to load
    await page.waitForTimeout(1000)

    // If already completed (showing "Preferences Saved!"), click update to restart
    const savedHeading = page.getByRole('heading', { name: /preferences saved/i })
    if (await savedHeading.isVisible().catch(() => false)) {
      await page.getByRole('button', { name: /update my preferences/i }).click()
      await page.waitForTimeout(500)
    }

    // Answer questions, selecting "Do not include" for Biblical and Nature names

    // Question 1: Styles
    await expect(page.getByText(/what name styles/i)).toBeVisible({ timeout: 5000 })
    await page.getByRole('button', { name: /classic.*traditional/i }).click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 2: Origins
    await page.getByRole('button', { name: /no preference/i }).first().click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 3: Length
    await page.getByRole('button', { name: /no preference/i }).first().click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 4: Sound
    await page.getByRole('button', { name: /no preference/i }).first().click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 5: Biblical - SELECT "Do not include"
    await expect(page.getByText(/biblical.*religious/i)).toBeVisible()
    await page.getByRole('button', { name: /do not include/i }).click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 6: Nature - SELECT "Do not include"
    await expect(page.getByText(/nature-inspired/i)).toBeVisible()
    await page.getByRole('button', { name: /do not include/i }).click()
    await page.getByRole('button', { name: /next/i }).click()

    // Question 7: Trendy
    await page.getByRole('button', { name: /no preference/i }).first().click()

    // Submit preferences (button says "Finish" on last question)
    await page.getByRole('button', { name: /finish/i }).click()

    // Should show success with the exclusions noted
    await expect(page.getByRole('heading', { name: /preferences saved/i })).toBeVisible({ timeout: 10000 })

    // Verify excluded categories are shown (with thumbs down emoji)
    await expect(page.getByText('👎 Biblical')).toBeVisible()
    await expect(page.getByText('👎 Nature')).toBeVisible()

    // Go back to session
    await page.getByRole('button', { name: /back to session/i }).click()
    await expect(page).toHaveURL('/session')
  })

  test('should allow updating preferences after initial submission', async ({ page }) => {
    await ensureSession(page)

    // Navigate directly to preferences
    await page.goto('/preferences')
    await expect(page).toHaveURL('/preferences')

    // Wait for page to fully load
    await page.waitForTimeout(1000)

    // Preferences should already be saved from previous tests
    await expect(page.getByRole('heading', { name: /preferences saved/i })).toBeVisible({ timeout: 10000 })

    // Click "Update My Preferences" to redo
    await page.getByRole('button', { name: /update my preferences/i }).click()

    // Should be back at questionnaire
    await expect(page.getByText(/what name styles appeal to you/i)).toBeVisible({ timeout: 5000 })
  })
})
