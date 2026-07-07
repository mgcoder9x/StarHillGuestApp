import { getCurrentLocale, loadRouteLocaleMessage } from '@/libs/i18n'

export default [
    {
        path: '/dashboard/smart-factory',
        name: 'dashboard-smart-factory',
        component: () => import('@/views/dashboard/meiko/index.vue'),
        beforeEnter: (to, from, next) => {
            const currentLocale = getCurrentLocale()
            const parentPath = to.matched[0].path
            loadRouteLocaleMessage(currentLocale, parentPath).then(() => next())
        },
        meta: {
            pageTitle: 'route.breadcrumb.dashboardMeiko',
            breadcrumb: [
                {
                    text: 'route.common.dashboard',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/monitor/sop-des',
        name: 'monitor-sop-des',
        component: () => import('@/views/dashboard/smart-factory/index.vue'),
        beforeEnter: (to, from, next) => {
            const currentLocale = getCurrentLocale()
            const parentPath = to.matched[0].path
            loadRouteLocaleMessage(currentLocale, parentPath).then(() => next())
        },
        meta: {
            pageTitle: 'route.breadcrumb.monitorSopDes',
            breadcrumb: [
                {
                    text: 'route.common.dashboard',
                    active: true,
                },
            ],
        },
    }
]
