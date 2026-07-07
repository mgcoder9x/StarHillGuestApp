export default [
    {
        path: '/dashboard/fireworkDashboard',
        name: 'dashboard-firework-info',
        component: () =>
            import('@/views/dashboard/z121/FireworkCountByDay.vue'),
        meta: {
            pageTitle: 'route.breadcrumb.dashboardFirework',
            breadcrumb: [
                {
                    text: 'route.common.dashboard',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/dashboard/map-dashboard',
        name: 'dashboard-map-dashboard-events',
        component: () => import('@/views/dashboard/map-dashboard/events.vue'),
        meta: {
            pageTitle: 'route.breadcrumb.dashboardMap',
            breadcrumb: [
                {
                    text: 'route.common.dashboard',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.dashboardMap',
                },
            ],
        },
    },
    {
        path: '/dashboard/events',
        name: 'dashboard-events',
        component: () => import('@/views/dashboard/events/allDashBoard.vue'),
        meta: {
            pageTitle: 'route.breadcrumb.dashboardEvents',
            breadcrumb: [
                {
                    text: 'route.common.dashboard',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.dashboardEvents',
                },
            ],
        },
    },
]
