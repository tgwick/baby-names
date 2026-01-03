<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <div class="max-w-4xl mx-auto text-center">
    <h1 class="text-5xl font-bold text-gray-900 mb-6">
      Find the Perfect Baby Name
      <span class="text-rose-500">Together</span>
    </h1>

    <p class="text-xl text-gray-600 mb-8">
      A collaborative way for couples to discover and agree on baby names.
      Swipe through names, match on your favorites, and find the one you both love.
    </p>

    <div class="flex justify-center gap-4">
      <template v-if="authStore.isAuthenticated">
        <RouterLink
          to="/dashboard"
          class="px-8 py-3 text-lg font-medium text-white bg-rose-500 rounded-lg hover:bg-rose-600 transition-colors"
        >
          Go to Dashboard
        </RouterLink>
      </template>
      <template v-else>
        <RouterLink
          to="/register"
          class="px-8 py-3 text-lg font-medium text-white bg-rose-500 rounded-lg hover:bg-rose-600 transition-colors"
        >
          Get Started
        </RouterLink>
        <RouterLink
          to="/login"
          class="px-8 py-3 text-lg font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
        >
          Sign In
        </RouterLink>
      </template>
    </div>

    <div class="mt-16 grid grid-cols-1 md:grid-cols-3 gap-8">
      <div class="p-6 bg-white rounded-xl shadow-sm">
        <div class="w-12 h-12 bg-rose-100 rounded-lg flex items-center justify-center mb-4 mx-auto">
          <span class="text-2xl">1</span>
        </div>
        <h3 class="text-lg font-semibold mb-2">Create a Session</h3>
        <p class="text-gray-600">Start a new session and choose the name gender you're looking for.</p>
      </div>

      <div class="p-6 bg-white rounded-xl shadow-sm">
        <div class="w-12 h-12 bg-rose-100 rounded-lg flex items-center justify-center mb-4 mx-auto">
          <span class="text-2xl">2</span>
        </div>
        <h3 class="text-lg font-semibold mb-2">Invite Your Partner</h3>
        <p class="text-gray-600">Share your unique code or link with your partner to join.</p>
      </div>

      <div class="p-6 bg-white rounded-xl shadow-sm">
        <div class="w-12 h-12 bg-rose-100 rounded-lg flex items-center justify-center mb-4 mx-auto">
          <span class="text-2xl">3</span>
        </div>
        <h3 class="text-lg font-semibold mb-2">Find Your Match</h3>
        <p class="text-gray-600">Swipe through names and see which ones you both love!</p>
      </div>
    </div>
  </div>
</template>
