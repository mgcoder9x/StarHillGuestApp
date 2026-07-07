/* eslint-disable */
export default [
    
    {
        path: '/notification/allNotification',
        name: 'notification-all',
        meta: {
            text: 'All Notification',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'notification_all-list',
                component: () =>
                    import('@/views/notifications/allNotifications/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.notifications',
                        },
                    ],
                },
            },
        ],
    },
]
