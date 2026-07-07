import Vue from 'vue'
import VueRouter from 'vue-router'

import routeConfig from './projects'

const PROJECT_CONFIG = routeConfig

// Routes
import { isUserLoggedIn } from '@/auth/utils'
import pages from './routes/pages'

Vue.use(VueRouter)

const router = new VueRouter({
    mode: 'history',
    base: process.env.BASE_URL,
    scrollBehavior() {
        return { x: 0, y: 0 }
    },
    routes: [
        { path: '/', redirect: { name: PROJECT_CONFIG.name } },
        ...PROJECT_CONFIG.enabledRoutes,
        ...pages,
        {
            path: '*',
            redirect: 'error-404',
        },
    ],
})

router.beforeEach((to, _, next) => {
    /* eslint-disable */
    const isLoggedIn = isUserLoggedIn()

    // // Redirect if logged in
    // if (to.meta.redirectIfLoggedIn) {
    //   next('/login')
    // }

    if (!isLoggedIn && to.name != 'auth-login')
        return next({ name: 'auth-login' })

    // // if (!canNavigate(to)) {
    // //   // Redirect to login if not logged in
    // //   if (!isLoggedIn) return next({ name: 'auth-login' })

    // //   // If logged in => not authorized
    // //   return next({ name: 'misc-not-authorized' })
    // // }

    return next()
})

// Xử lý lỗi chunk loading
router.onError((error) => {
    const pattern = /Loading chunk \d+ failed|Unexpected token '<'/i
    const isChunkLoadFailed = error.message && pattern.test(error.message)
    
    if (isChunkLoadFailed) {
        // Kiểm tra xem đã reload chưa để tránh loop
        const hasReloaded = sessionStorage.getItem('chunk-load-failed')
        if (!hasReloaded) {
            sessionStorage.setItem('chunk-load-failed', 'true')
            window.location.reload()
        } else {
            sessionStorage.removeItem('chunk-load-failed')
            console.error('Chunk load failed after reload:', error)
        }
    }
})

// ? For splash screen
// Remove afterEach hook if you are not using splash screen
router.afterEach(() => {
    // Remove initial loading
    const appLoading = document.getElementById('loading-bg')
    if (appLoading) {
        appLoading.style.display = 'none'
    }
})

export default router
