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

// Authenticated session tests are now in session.authenticated.ts
