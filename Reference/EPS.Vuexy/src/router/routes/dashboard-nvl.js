export default [
    {
        path: '/dashboard/map-dashboard-nvl',
        name: 'dashboard-map-dashboard-events-nvl',
        component: () => import('@/views/dashboard/map-dashboard-Novaland/events.vue'),
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
        path: '/dashboard/events-nvl',
        name: 'dashboard-events-nvl',
        component: () => import('@/views/dashboard/events-Novaland/allDashBoard.vue'),
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
