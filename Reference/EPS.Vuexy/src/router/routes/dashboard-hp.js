export default [
    {
        path: '/dashboard/map-dashboard-hp',
        name: 'dashboard-map-dashboard-events-hp',
        component: () => import('@/views/dashboard/map-dashboard-HoaPhat/events.vue'),
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
        path: '/dashboard/events-hp',
        name: 'dashboard-events-hp',
        component: () => import('@/views/dashboard/events-HoaPhat/allDashBoard.vue'),
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
