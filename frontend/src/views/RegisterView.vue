<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const displayName = ref('')
const error = ref('')
const loading = ref(false)

async function handleSubmit() {
  error.value = ''

  if (password.value !== confirmPassword.value) {
    error.value = 'Passwords do not match'
    return
  }

  if (password.value.length < 8) {
    error.value = 'Password must be at least 8 characters'
    return
  }

  loading.value = true

  try {
    await authStore.register(email.value, password.value, displayName.value || undefined)
    router.push('/dashboard')
  } catch (e: unknown) {
    const err = e as { response?: { data?: { errors?: string[] } } }
    error.value = err.response?.data?.errors?.[0] || 'Registration failed. Please try again.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="max-w-md mx-auto">
    <div class="card-elevated p-6 sm:p-8 animate-slide-up">
      <!-- Header -->
      <div class="text-center mb-6 sm:mb-8">
        <div class="w-16 h-16 bg-[var(--color-blush)] rounded-2xl flex items-center justify-center text-3xl mx-auto mb-4">
          🐣
        </div>
        <h1 class="font-display text-2xl sm:text-3xl font-semibold text-[var(--color-warm-gray)]">
          Create Your Nest
        </h1>
        <p class="text-[var(--color-warm-gray-light)] mt-2">
          Start hatching the perfect baby name together
        </p>
      </div>

      <form @submit.prevent="handleSubmit" class="space-y-4 sm:space-y-5">
        <!-- Error Message -->
        <div v-if="error" class="error-message animate-bounce-in">
          {{ error }}
        </div>

        <!-- Display Name Field -->
        <div class="animate-slide-up stagger-1" style="animation-fill-mode: forwards; opacity: 0;">
          <label for="displayName" class="form-label">
            Display Name <span class="font-normal text-[var(--color-warm-gray-light)]">(optional)</span>
          </label>
          <input
            id="displayName"
            v-model="displayName"
            type="text"
            class="form-input"
            placeholder="Your name"
          />
        </div>

        <!-- Email Field -->
        <div class="animate-slide-up stagger-2" style="animation-fill-mode: forwards; opacity: 0;">
          <label for="email" class="form-label">
            Email
          </label>
          <input
            id="email"
            v-model="email"
            type="email"
            required
            class="form-input"
            placeholder="you@example.com"
          />
        </div>

        <!-- Password Field -->
        <div class="animate-slide-up stagger-3" style="animation-fill-mode: forwards; opacity: 0;">
          <label for="password" class="form-label">
            Password
          </label>
          <input
            id="password"
            v-model="password"
            type="password"
            required
            class="form-input"
            placeholder="At least 8 characters"
          />
        </div>

        <!-- Confirm Password Field -->
        <div class="animate-slide-up stagger-4" style="animation-fill-mode: forwards; opacity: 0;">
          <label for="confirmPassword" class="form-label">
            Confirm Password
          </label>
          <input
            id="confirmPassword"
            v-model="confirmPassword"
            type="password"
            required
            class="form-input"
            placeholder="Confirm your password"
          />
        </div>

        <!-- Submit Button -->
        <div class="pt-2">
          <button
            type="submit"
            :disabled="loading"
            class="btn-primary w-full text-base"
          >
            <span>{{ loading ? 'Creating account...' : 'Create Account' }}</span>
          </button>
        </div>
      </form>

      <!-- Footer Link -->
      <p class="mt-6 sm:mt-8 text-center text-[var(--color-warm-gray-light)]">
        Already have an account?
        <RouterLink to="/login" class="auth-link">
          Sign in
        </RouterLink>
      </p>
    </div>
  </div>
</template>
