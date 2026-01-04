<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <div class="max-w-4xl mx-auto">
    <!-- Hero Section -->
    <div class="text-center mb-12 sm:mb-16 animate-slide-up">
      <div class="inline-flex items-center gap-2 px-4 py-2 bg-[var(--color-blush)] rounded-full mb-6">
        <span class="text-lg">💕</span>
        <span class="text-sm font-medium text-[var(--color-coral)]">Find your perfect name together</span>
      </div>

      <h1 class="font-display text-4xl sm:text-5xl md:text-6xl font-bold text-[var(--color-warm-gray)] mb-6 leading-tight">
        Find the Perfect Baby Name
        <span class="block text-[var(--color-coral)]">Together</span>
      </h1>

      <p class="text-lg sm:text-xl text-[var(--color-warm-gray-light)] mb-8 max-w-2xl mx-auto leading-relaxed">
        A collaborative way for couples to discover and agree on baby names.
        Swipe through names, match on your favorites, and find the one you both love.
      </p>

      <div class="flex flex-col sm:flex-row justify-center gap-4">
        <template v-if="authStore.isAuthenticated">
          <RouterLink
            to="/dashboard"
            class="btn-primary text-lg px-8 py-4"
          >
            <span>Go to Dashboard</span>
          </RouterLink>
        </template>
        <template v-else>
          <RouterLink
            to="/register"
            class="btn-primary text-lg px-8 py-4"
          >
            <span>Get Started</span>
          </RouterLink>
          <RouterLink
            to="/login"
            class="btn-secondary text-lg px-8 py-4"
          >
            Sign In
          </RouterLink>
        </template>
      </div>
    </div>

    <!-- How It Works Section -->
    <div class="animate-slide-up stagger-2" style="animation-fill-mode: forwards; opacity: 0;">
      <h2 class="font-display text-2xl sm:text-3xl font-semibold text-[var(--color-warm-gray)] text-center mb-8">
        How It Works
      </h2>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <!-- Step 1 -->
        <div class="feature-card text-center animate-slide-up stagger-2" style="animation-fill-mode: forwards; opacity: 0;">
          <div class="feature-icon bg-[var(--color-blush)] text-[var(--color-coral)] mx-auto">
            1
          </div>
          <h3 class="font-display text-xl font-semibold text-[var(--color-warm-gray)] mb-2">
            Create a Session
          </h3>
          <p class="text-[var(--color-warm-gray-light)] text-sm leading-relaxed">
            Start a new session and choose the name gender you're looking for.
          </p>
        </div>

        <!-- Step 2 -->
        <div class="feature-card text-center animate-slide-up stagger-3" style="animation-fill-mode: forwards; opacity: 0;">
          <div class="feature-icon bg-[var(--color-lavender)] text-purple-600 mx-auto">
            2
          </div>
          <h3 class="font-display text-xl font-semibold text-[var(--color-warm-gray)] mb-2">
            Invite Your Partner
          </h3>
          <p class="text-[var(--color-warm-gray-light)] text-sm leading-relaxed">
            Share your unique code or link with your partner to join.
          </p>
        </div>

        <!-- Step 3 -->
        <div class="feature-card text-center animate-slide-up stagger-4" style="animation-fill-mode: forwards; opacity: 0;">
          <div class="feature-icon bg-[var(--color-mint)] text-emerald-600 mx-auto">
            3
          </div>
          <h3 class="font-display text-xl font-semibold text-[var(--color-warm-gray)] mb-2">
            Find Your Match
          </h3>
          <p class="text-[var(--color-warm-gray-light)] text-sm leading-relaxed">
            Swipe through names and see which ones you both love!
          </p>
        </div>
      </div>
    </div>

    <!-- Bottom CTA -->
    <div class="mt-12 sm:mt-16 text-center animate-slide-up stagger-4" style="animation-fill-mode: forwards; opacity: 0;">
      <div class="card-elevated p-8 sm:p-10 bg-gradient-to-r from-[var(--color-blush)] to-[var(--color-peach-light)]">
        <div class="text-4xl mb-4 animate-float">💑</div>
        <h3 class="font-display text-2xl font-semibold text-[var(--color-warm-gray)] mb-3">
          Ready to find the perfect name?
        </h3>
        <p class="text-[var(--color-warm-gray-light)] mb-6 max-w-md mx-auto">
          Join thousands of couples who've discovered their baby's name together.
        </p>
        <RouterLink
          v-if="!authStore.isAuthenticated"
          to="/register"
          class="btn-primary inline-flex"
        >
          <span>Start Free Today</span>
        </RouterLink>
        <RouterLink
          v-else
          to="/dashboard"
          class="btn-primary inline-flex"
        >
          <span>Continue to Dashboard</span>
        </RouterLink>
      </div>
    </div>
  </div>
</template>
