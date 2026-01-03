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
  <header class="bg-white shadow-sm">
    <nav class="container mx-auto px-4 py-4 flex items-center justify-between">
      <RouterLink to="/" class="text-2xl font-bold text-rose-500">
        NameMatch
      </RouterLink>

      <div class="flex items-center gap-4">
        <template v-if="authStore.isAuthenticated">
          <RouterLink
            to="/dashboard"
            class="text-gray-600 hover:text-gray-900"
          >
            Dashboard
          </RouterLink>
          <button
            @click="handleLogout"
            class="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200"
          >
            Logout
          </button>
        </template>
        <template v-else>
          <RouterLink
            to="/login"
            class="text-gray-600 hover:text-gray-900"
          >
            Login
          </RouterLink>
          <RouterLink
            to="/register"
            class="px-4 py-2 text-sm font-medium text-white bg-rose-500 rounded-lg hover:bg-rose-600"
          >
            Sign Up
          </RouterLink>
        </template>
      </div>
    </nav>
  </header>
</template>
