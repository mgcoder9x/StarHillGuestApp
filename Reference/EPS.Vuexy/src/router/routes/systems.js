/* eslint-disable */
export default [
    {
        path: '/notification/template',
        name: 'notification_event_template',
        redirect: { name: 'notification_event_template-list' },
        meta: {
            text: 'Notification',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'notification_event_template-list',
                component: () =>
                    import('@/views/notifications/template/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.notificationTemplates',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'notification_event_template-create',
                component: () =>
                    import('@/views/notifications/template/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.notificationTemplates',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'notification_event_template-detail',
                component: () =>
                    import('@/views/notifications/template/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.notificationTemplates',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/systems/users',
        name: 'systems-users',
        redirect: { name: 'systems-users-list' },
        meta: {
            text: 'Users',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'systems-users-list',
                component: () => import('@/views/systems/users/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.users',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'systems-users-create',
                component: () => import('@/views/systems/users/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.users',
                        },
                    ],
                },
            },
            {
                path: 'detail/:userId',
                name: 'systems-users-detail',
                component: () => import('@/views/systems/users/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.users',
                        },
                    ],
                },
            },
            {
                path: 'privileges/:userId',
                name: 'systems-users-privileges',
                component: () => import('@/views/systems/users/Privileges.vue'),
                meta: {
                    pageTitle: 'route.privileges',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.users',
                        },
                    ],
                },
            },
            {
                path: 'notification/:userId',
                name: 'systems-users-notification',
                component: () =>
                    import('@/views/notifications/setting/Detail.vue'),
                meta: {
                    pageTitle: 'route.notification',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.users',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/systems/roles',
        name: 'systems-roles',
        redirect: { name: 'systems-roles-list' },
        meta: {
            text: 'Roles',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'systems-roles-list',
                component: () => import('@/views/systems/roles/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.roles',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'systems-roles-create',
                component: () => import('@/views/systems/roles/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.roles',
                        },
                    ],
                },
            },
            {
                path: 'detail/:roleId',
                name: 'systems-roles-detail',
                component: () => import('@/views/systems/roles/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.roles',
                        },
                    ],
                },
            },
            {
                path: 'privileges/:roleId',
                name: 'systems-roles-privileges',
                component: () => import('@/views/systems/roles/Privileges.vue'),
                meta: {
                    pageTitle: 'route.privileges',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.roles',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/systems/company',
        name: 'systems-company',
        redirect: { name: 'systems-company-list' },
        meta: {
            text: 'Company',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'systems-company-list',
                component: () => import('@/views/systems/company/list.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.companies',
                        },
                    ],
                },
            },
            {
                path: 'detail/:companyId',
                name: 'systems-company-detail',
                component: () => import('@/views/systems/company/detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.companies',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'systems-company-create',
                component: () => import('@/views/systems/company/create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.companies',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/systems/audit-logs',
        name: 'systems-audit-logs',
        redirect: { name: 'systems-audit-logs-list' },
        meta: {
            text: 'AuditLogs',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'systems-audit-logs-list',
                component: () => import('@/views/systems/audit_logs/list.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.systemManagement',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.auditLogs',
                        },
                    ],
                },
            },
        ],
    },
]
