<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'

const authStore = useAuthStore()
const router = useRouter()

async function handleLogout() {
  authStore.logout()
  router.push('/login')
}
</script>

<template>
  <header class="bg-white/70 backdrop-blur-md border-b border-[var(--color-peach-light)]/30 sticky top-0 z-50">
    <nav class="container mx-auto px-4 py-4 flex items-center justify-between">
      <RouterLink to="/" class="flex items-center gap-2 group">
        <div class="w-10 h-10 rounded-xl bg-gradient-to-br from-[var(--color-coral)] to-[var(--color-peach)] flex items-center justify-center shadow-md group-hover:shadow-lg transition-shadow">
          <span class="text-lg">💕</span>
        </div>
        <span class="font-display text-2xl font-semibold text-[var(--color-warm-gray)] hidden sm:block">
          NameMatch
        </span>
      </RouterLink>

      <div class="flex items-center gap-3">
        <template v-if="authStore.isAuthenticated">
          <RouterLink
            to="/dashboard"
            class="px-4 py-2 text-[var(--color-warm-gray)] hover:text-[var(--color-coral)] font-medium transition-colors hidden sm:block"
          >
            Dashboard
          </RouterLink>
          <RouterLink
            to="/session"
            class="px-4 py-2 text-[var(--color-warm-gray)] hover:text-[var(--color-coral)] font-medium transition-colors"
          >
            Session
          </RouterLink>
          <button
            @click="handleLogout"
            class="px-4 py-2 text-sm font-medium text-[var(--color-warm-gray)] bg-[var(--color-cream)] rounded-xl border border-[var(--color-peach-light)] hover:bg-[var(--color-blush)] hover:border-[var(--color-peach)] transition-all"
          >
            Logout
          </button>
        </template>
        <template v-else>
          <RouterLink
            to="/login"
            class="px-4 py-2 text-[var(--color-warm-gray)] hover:text-[var(--color-coral)] font-medium transition-colors"
          >
            Login
          </RouterLink>
          <RouterLink
            to="/register"
            class="btn-primary text-sm py-2.5 px-5"
          >
            <span>Sign Up</span>
          </RouterLink>
        </template>
      </div>
    </nav>
  </header>
</template>
