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
    {
      path: '/dashboard',
      name: 'dashboard',
      component: () => import('@/views/DashboardView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/session',
      name: 'session',
      component: () => import('@/views/SessionView.vue'),
      meta: { requiresAuth: true },
    },
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
    next({ name: 'dashboard' })
  } else {
    next()
  }
})

export default router
