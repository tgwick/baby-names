<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useSessionStore } from '@/stores/session'
import { useRouter } from 'vue-router'

const authStore = useAuthStore()
const sessionStore = useSessionStore()
const router = useRouter()

async function handleLogout() {
  authStore.logout()
  sessionStore.clearSession()
  router.push('/login')
}

// Fetch session data when authenticated
onMounted(async () => {
  if (authStore.isAuthenticated) {
    await sessionStore.fetchCurrentSession()
    if (sessionStore.isActive) {
      await sessionStore.fetchStats()
    }
  }
})

// Watch for auth changes
watch(() => authStore.isAuthenticated, async (isAuth) => {
  if (isAuth) {
    await sessionStore.fetchCurrentSession()
    if (sessionStore.isActive) {
      await sessionStore.fetchStats()
    }
  }
})
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
            class="px-4 py-2 text-[var(--color-warm-gray)] hover:text-[var(--color-coral)] font-medium transition-colors hidden sm:block"
          >
            Session
          </RouterLink>
          <!-- Matches link with badge -->
          <RouterLink
            v-if="sessionStore.isActive"
            to="/matches"
            class="relative px-4 py-2 text-[var(--color-warm-gray)] hover:text-[var(--color-coral)] font-medium transition-colors flex items-center gap-1"
          >
            <span>Matches</span>
            <span
              v-if="sessionStore.stats?.matchCount && sessionStore.stats.matchCount > 0"
              class="absolute -top-1 -right-1 min-w-5 h-5 flex items-center justify-center bg-gradient-to-r from-[var(--color-coral)] to-[var(--color-peach)] text-white text-xs font-bold rounded-full px-1.5 animate-pulse-soft"
            >
              {{ sessionStore.stats.matchCount > 99 ? '99+' : sessionStore.stats.matchCount }}
            </span>
          </RouterLink>
          <!-- Swipe button (quick access) -->
          <RouterLink
            v-if="sessionStore.isActive"
            to="/swipe"
            class="btn-primary text-sm py-2 px-4 hidden sm:flex"
          >
            <span>Swipe</span>
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
