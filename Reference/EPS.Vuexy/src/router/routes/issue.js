export default [
    // {
    //     path: '/issues',
    //     name: 'issue',
    //     component: () => import('@/views/categories/issues/List.vue'),
    //     meta: {
    //         pageTitle: 'route.breadcrumb.issue',
    //         breadcrumb: [
    //             {
    //                 text: 'route.common.issue',
    //                 active: true,
    //             },
    //         ],
    //     },

    // },
    {
        path: '/issues',
        name: 'issue',
        // meta: {
        //     text: 'Event',
        // },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        redirect: { name: 'issue-list' },
        children: [
            {
                path: 'list',
                name: 'issue-list',
                component: () => import('@/views/categories/issues/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.issue',
                        },
                    ],
                },
            },
            {
                path: 'detail/:issueId',
                name: 'issue-detail',
                component: () => import('@/views/categories/issues/Detail.vue'),
                meta: {
                    pageTitle: 'route.breadcrumb.issue',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            to: { name: 'issue-list' },
                            text: 'route.breadcrumb.issue',
                        },
                    ],
                },
            },
        ],
    },

    {
        path: '/issue/projects',
        name: 'issue-projects',
        redirect: { name: 'issue-projects-list' },
        meta: {
            text: 'Projects',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'issue-projects-list',
                component: () => import('@/views/categories/projects/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.projects',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'issue-projects-create',
                component: () =>
                    import('@/views/categories/projects/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.projects',
                        },
                    ],
                },
            },
            {
                path: 'detail/:projectId',
                name: 'issue-projects-detail',
                component: () =>
                    import('@/views/categories/projects/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.projects',
                        },
                    ],
                },
            },
        ],
    },

    // priority
    {
        path: '/issue/priority',
        name: 'issue-priority',
        redirect: { name: 'issue-priority-list' },
        meta: {
            text: 'Priority',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'issue-priority-list',
                component: () => import('@/views/issues/priority/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.priority',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'issue-priority-create',
                component: () =>
                    import('@/views/issues/priority/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.priority',
                        },
                    ],
                },
            },
            {
                path: 'detail/:priorityId',
                name: 'issue-priority-detail',
                component: () =>
                    import('@/views/issues/priority/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.priority',
                        },
                    ],
                },
            },
        ],
    },

    // issueTypes
    {
        path: '/issue/issueTypes',
        name: 'issue-issueTypes',
        redirect: { name: 'issue-issueTypes-list' },
        meta: {
            text: 'Priority',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'issue-issueTypes-list',
                component: () => import('@/views/issues/issueTypes/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.issueType',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'issue-issueTypes-create',
                component: () =>
                    import('@/views/issues/issueTypes/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.issueType',
                        },
                    ],
                },
            },
            {
                path: 'detail/:issueTypeId',
                name: 'issue-issueTypes-detail',
                component: () =>
                    import('@/views/issues/issueTypes/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.issueType',
                        },
                    ],
                },
            },
        ],
    },

    // statuses
    {
        path: '/issue/statuses',
        name: 'issue-statuses',
        redirect: { name: 'issue-status-list' },
        meta: {
            text: 'Status',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'issue-status-list',
                component: () => import('@/views/issues/statuses/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.statuses',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'issue-status-create',
                component: () =>
                    import('@/views/issues/statuses/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.statuses',
                        },
                    ],
                },
            },
            {
                path: 'detail/:statusId',
                name: 'issue-status-detail',
                component: () =>
                    import('@/views/issues/statuses/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.issue',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.statuses',
                        },
                    ],
                },
            },
        ],
    },
]
