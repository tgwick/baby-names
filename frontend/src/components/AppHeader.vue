<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useSessionStore } from '@/stores/session'
import { useRouter } from 'vue-router'

const authStore = useAuthStore()
const sessionStore = useSessionStore()
const router = useRouter()

const mobileMenuOpen = ref(false)

async function handleLogout() {
  mobileMenuOpen.value = false
  authStore.logout()
  sessionStore.clearSession()
  router.push('/login')
}

function closeMobileMenu() {
  mobileMenuOpen.value = false
}

// Fetch session data when authenticated
onMounted(async () => {
  if (authStore.isAuthenticated) {
    await sessionStore.fetchCurrentSession()
    if (sessionStore.isActive) {
      await Promise.all([
        sessionStore.fetchStats(),
        sessionStore.fetchConflicts(),
      ])
    }
  }
})

// Watch for auth changes
watch(() => authStore.isAuthenticated, async (isAuth) => {
  if (isAuth) {
    await sessionStore.fetchCurrentSession()
    if (sessionStore.isActive) {
      await Promise.all([
        sessionStore.fetchStats(),
        sessionStore.fetchConflicts(),
      ])
    }
  }
})
</script>

<template>
  <header class="bg-white/70 backdrop-blur-md border-b border-[var(--color-peach-light)]/30 sticky top-0 z-50">
    <nav class="container mx-auto px-4 py-3 sm:py-4 flex items-center justify-between">
      <RouterLink to="/" class="flex items-center gap-2 group">
        <div class="w-9 h-9 sm:w-10 sm:h-10 rounded-xl bg-gradient-to-br from-[var(--color-coral)] to-[var(--color-peach)] flex items-center justify-center shadow-md group-hover:shadow-lg transition-shadow">
          <span class="text-base sm:text-lg">💕</span>
        </div>
        <span class="font-display text-xl sm:text-2xl font-semibold text-[var(--color-warm-gray)] hidden sm:block">
          NameMatch
        </span>
      </RouterLink>

      <div class="flex items-center gap-2 sm:gap-3">
        <template v-if="authStore.isAuthenticated">
          <!-- Desktop Navigation -->
          <RouterLink
            to="/dashboard"
            class="px-3 py-2 text-[var(--color-warm-gray)] hover:text-[var(--color-coral)] font-medium transition-colors hidden md:block"
          >
            Dashboard
          </RouterLink>
          <RouterLink
            to="/session"
            class="px-3 py-2 text-[var(--color-warm-gray)] hover:text-[var(--color-coral)] font-medium transition-colors hidden md:block"
          >
            Session
          </RouterLink>
          <!-- Matches link with badge (desktop) -->
          <RouterLink
            v-if="sessionStore.isActive"
            to="/matches"
            class="relative px-3 py-2 text-[var(--color-warm-gray)] hover:text-[var(--color-coral)] font-medium transition-colors items-center gap-1 hidden md:flex"
          >
            <span>Matches</span>
            <span
              v-if="sessionStore.stats?.matchCount && sessionStore.stats.matchCount > 0"
              class="absolute -top-1 -right-1 min-w-5 h-5 flex items-center justify-center bg-gradient-to-r from-[var(--color-coral)] to-[var(--color-peach)] text-white text-xs font-bold rounded-full px-1.5 animate-pulse-soft"
            >
              {{ sessionStore.stats.matchCount > 99 ? '99+' : sessionStore.stats.matchCount }}
            </span>
          </RouterLink>
          <!-- Conflicts link with badge (desktop) -->
          <RouterLink
            v-if="sessionStore.isActive"
            to="/conflicts"
            class="relative px-3 py-2 text-[var(--color-warm-gray)] hover:text-[var(--color-coral)] font-medium transition-colors items-center gap-1 hidden md:flex"
          >
            <span>Conflicts</span>
            <span
              v-if="sessionStore.conflicts.length > 0"
              class="absolute -top-1 -right-1 min-w-5 h-5 flex items-center justify-center bg-[var(--color-lavender)] text-[var(--color-warm-gray)] text-xs font-bold rounded-full px-1.5"
            >
              {{ sessionStore.conflicts.length > 99 ? '99+' : sessionStore.conflicts.length }}
            </span>
          </RouterLink>
          <!-- Swipe button (desktop) -->
          <RouterLink
            v-if="sessionStore.isActive"
            to="/swipe"
            class="btn-primary text-sm py-2 px-4 hidden md:flex"
          >
            <span>Swipe</span>
          </RouterLink>
          <!-- Logout (desktop) -->
          <button
            @click="handleLogout"
            class="px-3 py-2 text-sm font-medium text-[var(--color-warm-gray)] bg-[var(--color-cream)] rounded-xl border border-[var(--color-peach-light)] hover:bg-[var(--color-blush)] hover:border-[var(--color-peach)] transition-all hidden sm:block"
          >
            Logout
          </button>
          <!-- Mobile menu button -->
          <button
            @click="mobileMenuOpen = !mobileMenuOpen"
            class="p-2 text-[var(--color-warm-gray)] hover:text-[var(--color-coral)] transition-colors md:hidden min-w-[44px] min-h-[44px] flex items-center justify-center"
            aria-label="Toggle menu"
          >
            <svg v-if="!mobileMenuOpen" xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
            <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </template>
        <template v-else>
          <RouterLink
            to="/login"
            class="px-3 py-2 text-[var(--color-warm-gray)] hover:text-[var(--color-coral)] font-medium transition-colors text-sm sm:text-base"
          >
            Login
          </RouterLink>
          <RouterLink
            to="/register"
            class="btn-primary text-sm py-2 px-4 sm:py-2.5 sm:px-5"
          >
            <span>Sign Up</span>
          </RouterLink>
        </template>
      </div>
    </nav>

    <!-- Mobile dropdown menu -->
    <div
      v-if="authStore.isAuthenticated && mobileMenuOpen"
      class="md:hidden bg-white border-t border-[var(--color-cream-dark)] shadow-lg"
    >
      <div class="container mx-auto px-4 py-3 space-y-1">
        <RouterLink
          to="/dashboard"
          @click="closeMobileMenu"
          class="flex items-center gap-3 px-4 py-3 rounded-xl text-[var(--color-warm-gray)] hover:bg-[var(--color-cream)] transition-colors"
        >
          <span class="text-xl">🏠</span>
          <span class="font-medium">Dashboard</span>
        </RouterLink>
        <RouterLink
          to="/session"
          @click="closeMobileMenu"
          class="flex items-center gap-3 px-4 py-3 rounded-xl text-[var(--color-warm-gray)] hover:bg-[var(--color-cream)] transition-colors"
        >
          <span class="text-xl">👥</span>
          <span class="font-medium">Session</span>
        </RouterLink>
        <template v-if="sessionStore.isActive">
          <RouterLink
            to="/swipe"
            @click="closeMobileMenu"
            class="flex items-center gap-3 px-4 py-3 rounded-xl bg-gradient-to-r from-[var(--color-coral)] to-[var(--color-peach)] text-white"
          >
            <span class="text-xl">👆</span>
            <span class="font-semibold">Swipe Names</span>
          </RouterLink>
          <RouterLink
            to="/matches"
            @click="closeMobileMenu"
            class="flex items-center justify-between px-4 py-3 rounded-xl text-[var(--color-warm-gray)] hover:bg-[var(--color-cream)] transition-colors"
          >
            <div class="flex items-center gap-3">
              <span class="text-xl">💕</span>
              <span class="font-medium">Matches</span>
            </div>
            <span
              v-if="sessionStore.stats?.matchCount && sessionStore.stats.matchCount > 0"
              class="min-w-6 h-6 flex items-center justify-center bg-gradient-to-r from-[var(--color-coral)] to-[var(--color-peach)] text-white text-xs font-bold rounded-full px-2"
            >
              {{ sessionStore.stats.matchCount }}
            </span>
          </RouterLink>
          <RouterLink
            to="/conflicts"
            @click="closeMobileMenu"
            class="flex items-center justify-between px-4 py-3 rounded-xl text-[var(--color-warm-gray)] hover:bg-[var(--color-cream)] transition-colors"
          >
            <div class="flex items-center gap-3">
              <span class="text-xl">💔</span>
              <span class="font-medium">Conflicts</span>
            </div>
            <span
              v-if="sessionStore.conflicts.length > 0"
              class="min-w-6 h-6 flex items-center justify-center bg-[var(--color-lavender)] text-[var(--color-warm-gray)] text-xs font-bold rounded-full px-2"
            >
              {{ sessionStore.conflicts.length }}
            </span>
          </RouterLink>
        </template>
        <div class="border-t border-[var(--color-cream-dark)] pt-2 mt-2">
          <button
            @click="handleLogout"
            class="flex items-center gap-3 w-full px-4 py-3 rounded-xl text-[var(--color-warm-gray)] hover:bg-[var(--color-cream)] transition-colors"
          >
            <span class="text-xl">🚪</span>
            <span class="font-medium">Logout</span>
          </button>
        </div>
      </div>
    </div>
  </header>
</template>
