import { test as setup, expect } from '@playwright/test'

const authFile = 'e2e/.auth/user.json'

setup('authenticate', async ({ page }) => {
  // Generate unique email for this test run
  const testEmail = `e2e-test-${Date.now()}@example.com`
  const testPassword = 'TestPassword123!'

  // Register a new user
  await page.goto('/register')

  await page.locator('#displayName').fill('E2E Test User')
  await page.locator('#email').fill(testEmail)
  await page.locator('#password').fill(testPassword)
  await page.locator('#confirmPassword').fill(testPassword)
  await page.getByRole('button', { name: /create account/i }).click()

  // Wait for redirect to dashboard (successful registration)
  await expect(page).toHaveURL('/dashboard', { timeout: 15000 })

  // Save authentication state
  await page.context().storageState({ path: authFile })
})
