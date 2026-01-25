import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@/views/HomeView.vue'),
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { guest: true },
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/views/RegisterView.vue'),
      meta: { guest: true },
    },
    // New multi-session routes
    {
      path: '/sessions',
      name: 'sessions',
      component: () => import('@/views/SessionListView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/sessions/:sessionId',
      name: 'session-detail',
      component: () => import('@/views/SessionDetailView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/sessions/:sessionId/swipe',
      name: 'session-swipe',
      component: () => import('@/views/SwipeView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/sessions/:sessionId/matches',
      name: 'session-matches',
      component: () => import('@/views/MatchesView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/sessions/:sessionId/conflicts',
      name: 'session-conflicts',
      component: () => import('@/views/ConflictsView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/sessions/:sessionId/preferences',
      name: 'session-preferences',
      component: () => import('@/views/PreferencesView.vue'),
      meta: { requiresAuth: true },
    },
    // Legacy routes - redirect to new structure
    {
      path: '/dashboard',
      redirect: '/sessions',
    },
    {
      path: '/session',
      redirect: '/sessions',
    },
    {
      path: '/swipe',
      redirect: '/sessions',
    },
    {
      path: '/matches',
      redirect: '/sessions',
    },
    {
      path: '/conflicts',
      redirect: '/sessions',
    },
    {
      path: '/preferences',
      redirect: '/sessions',
    },
    // Session creation/joining (still at old paths)
    {
      path: '/session/create',
      name: 'create-session',
      component: () => import('@/views/CreateSessionView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/session/join',
      name: 'join-session',
      component: () => import('@/views/JoinSessionView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/join/:partnerLink',
      name: 'join-link',
      component: () => import('@/views/JoinLinkView.vue'),
    },
  ],
})

router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next({ name: 'login' })
  } else if (to.meta.guest && authStore.isAuthenticated) {
    next({ name: 'sessions' })
  } else {
    next()
  }
})

export default router
