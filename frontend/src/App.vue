<script setup lang="ts">
import { onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import AppHeader from '@/components/AppHeader.vue'

const authStore = useAuthStore()

onMounted(async () => {
  if (authStore.token) {
    await authStore.fetchUser()
  }
})
</script>

<template>
  <div class="min-h-screen relative overflow-hidden">
    <!-- Decorative background elements -->
    <div class="fixed inset-0 pointer-events-none overflow-hidden">
      <div class="decorative-circle w-96 h-96 -top-48 -right-48"></div>
      <div class="decorative-circle w-64 h-64 top-1/3 -left-32"></div>
      <div class="decorative-circle w-80 h-80 bottom-0 right-1/4"></div>
    </div>

    <AppHeader />
    <main class="container mx-auto px-4 py-8 relative z-10">
      <RouterView />
    </main>
  </div>
</template>
