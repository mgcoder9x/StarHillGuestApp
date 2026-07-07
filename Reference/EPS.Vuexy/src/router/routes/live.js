import { getCurrentLocale, loadRouteLocaleMessage } from '@/libs/i18n'

/* eslint-disable */
export default [
    {
        path: '/live',
        name: 'live',
        component: () => import('@/views/live/Live.vue'),
        meta: {
            pageTitle: 'route.live',
            breadcrumb: [
                {
                    text: 'route.common.live',
                    active: true,
                },
            ],
        },
        beforeEnter: (to, from, next) => {
            const currentLocale = getCurrentLocale()

            loadRouteLocaleMessage(currentLocale, to.path).then(() => next())
        },
    },
    {
        path: '/liveTLMBF',
        name: 'liveTLMBF',
        component: () => import('@/views/live/LiveTLMBF.vue'),
        meta: {
            pageTitle: 'route.live',
            breadcrumb: [
                {
                    text: 'route.common.live',
                    active: true,
                },
            ],
        },
        beforeEnter: (to, from, next) => {
            const currentLocale = getCurrentLocale()

            loadRouteLocaleMessage(currentLocale, to.path).then(() => next())
        },
    },
     {
        path: '/attendanceMonitor',
        name: 'attendanceMonitor',
        component: () => import('@/views/live/AttendanceMonitor.vue'),
        meta: {
            pageTitle: 'route.attendanceMonitor',
            breadcrumb: [
                {
                    text: 'route.common.attendanceMonitor',
                    active: true,
                },
            ],
        },
        beforeEnter: (to, from, next) => {
            const currentLocale = getCurrentLocale()

            loadRouteLocaleMessage(currentLocale, to.path).then(() => next())
        },
    },
    {
        path: '/liveView',
        name: 'liveView',
        component: () => import('@/views/live/LiveView.vue'),
        meta: {
            pageTitle: 'route.liveVehicle',
            breadcrumb: [
                {
                    text: 'route.common.vehicle',
                    active: true,
                },
            ],
        },
        beforeEnter: (to, from, next) => {
            const currentLocale = getCurrentLocale()

            loadRouteLocaleMessage(currentLocale, to.path).then(() => next())
        },
    },
]
