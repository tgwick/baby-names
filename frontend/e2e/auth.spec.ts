import { test, expect } from '@playwright/test'

// Helper to generate unique email for each test run
const uniqueEmail = () => `test${Date.now()}@example.com`

test.describe('Authentication Flow', () => {
  test.describe('Registration', () => {
    test('should display registration form', async ({ page }) => {
      await page.goto('/register')

      await expect(page.getByRole('heading', { name: /create account/i })).toBeVisible()
      await expect(page.locator('#displayName')).toBeVisible()
      await expect(page.locator('#email')).toBeVisible()
      await expect(page.locator('#password')).toBeVisible()
      await expect(page.locator('#confirmPassword')).toBeVisible()
      await expect(page.getByRole('button', { name: /create account/i })).toBeVisible()
    })

    test('should show validation error for password mismatch', async ({ page }) => {
      await page.goto('/register')

      await page.locator('#email').fill('test@example.com')
      await page.locator('#password').fill('password123')
      await page.locator('#confirmPassword').fill('differentpassword')
      await page.getByRole('button', { name: /create account/i }).click()

      await expect(page.getByText(/passwords do not match/i)).toBeVisible()
    })

    test('should show validation error for short password', async ({ page }) => {
      await page.goto('/register')

      await page.locator('#email').fill('test@example.com')
      await page.locator('#password').fill('short')
      await page.locator('#confirmPassword').fill('short')
      await page.getByRole('button', { name: /create account/i }).click()

      await expect(page.getByText(/at least 8 characters/i)).toBeVisible()
    })

    test('should navigate to login page via link', async ({ page }) => {
      await page.goto('/register')

      await page.getByRole('link', { name: /sign in/i }).click()

      await expect(page).toHaveURL('/login')
      await expect(page.getByRole('heading', { name: /welcome back/i })).toBeVisible()
    })
  })

  test.describe('Login', () => {
    test('should display login form', async ({ page }) => {
      await page.goto('/login')

      await expect(page.getByRole('heading', { name: /welcome back/i })).toBeVisible()
      await expect(page.locator('#email')).toBeVisible()
      await expect(page.locator('#password')).toBeVisible()
      await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible()
    })

    test('should show error for invalid credentials', async ({ page }) => {
      await page.goto('/login')

      await page.locator('#email').fill('nonexistent@example.com')
      await page.locator('#password').fill('wrongpassword')
      await page.getByRole('button', { name: /sign in/i }).click()

      // Wait for error message (API might return various error formats)
      await expect(page.locator('.bg-red-50')).toBeVisible({ timeout: 10000 })
    })

    test('should navigate to register page via link', async ({ page }) => {
      await page.goto('/login')

      // Click the "Sign up" link (the second one, below the form)
      await page.locator('p').getByRole('link', { name: /sign up/i }).click()

      await expect(page).toHaveURL('/register')
      await expect(page.getByRole('heading', { name: /create account/i })).toBeVisible()
    })
  })

  test.describe('Protected Routes', () => {
    test('should redirect to login when accessing dashboard without auth', async ({ page }) => {
      await page.goto('/dashboard')

      // Should redirect to login
      await expect(page).toHaveURL('/login')
    })

    test('should redirect to login when accessing session create without auth', async ({ page }) => {
      await page.goto('/session/create')

      await expect(page).toHaveURL('/login')
    })
  })

  test.describe('Home Page', () => {
    test('should display welcome message and CTA buttons', async ({ page }) => {
      await page.goto('/')

      await expect(page.getByRole('heading', { name: /find the perfect baby name/i })).toBeVisible()
      await expect(page.getByRole('link', { name: /get started/i })).toBeVisible()
      await expect(page.getByRole('link', { name: /sign in/i })).toBeVisible()
    })

    test('should navigate to register page from Get Started button', async ({ page }) => {
      await page.goto('/')

      await page.getByRole('link', { name: /get started/i }).click()

      await expect(page).toHaveURL('/register')
    })
  })
})

// Full registration/login flow tests are now in auth.setup.ts and session.authenticated.ts
