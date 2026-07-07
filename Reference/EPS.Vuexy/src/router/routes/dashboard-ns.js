export default [
    {
        path: '/dashboard/events-ns',
        name: 'dashboard-events-ns',
        component: () => import('@/views/dashboard/events-NghiSon/allDashBoard.vue'),
        meta: {
            pageTitle: 'route.breadcrumb.dashboardEvents',
            breadcrumb: [
                {
                    text: 'route.common.dashboard',
                    active: true,
                },
            ],
        },
    },
]
