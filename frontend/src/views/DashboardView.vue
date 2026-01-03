<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <div class="max-w-4xl mx-auto">
    <h1 class="text-3xl font-bold mb-8">
      Welcome{{ authStore.user?.displayName ? `, ${authStore.user.displayName}` : '' }}!
    </h1>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <div class="bg-white p-6 rounded-xl shadow-sm">
        <h2 class="text-xl font-semibold mb-4">Start a New Session</h2>
        <p class="text-gray-600 mb-4">
          Create a new session to start swiping through baby names.
          Your partner can join using a unique code.
        </p>
        <button
          class="w-full py-3 text-white bg-rose-500 rounded-lg hover:bg-rose-600 transition-colors"
          disabled
        >
          Create Session (Coming Soon)
        </button>
      </div>

      <div class="bg-white p-6 rounded-xl shadow-sm">
        <h2 class="text-xl font-semibold mb-4">Join a Session</h2>
        <p class="text-gray-600 mb-4">
          Have a code from your partner? Enter it here to join their session
          and start finding names together.
        </p>
        <button
          class="w-full py-3 text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
          disabled
        >
          Join Session (Coming Soon)
        </button>
      </div>
    </div>

    <div class="mt-8 bg-white p-6 rounded-xl shadow-sm">
      <h2 class="text-xl font-semibold mb-4">Your Matches</h2>
      <p class="text-gray-600">
        No matches yet. Start a session to begin discovering names!
      </p>
    </div>
  </div>
</template>
