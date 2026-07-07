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
        path: '/tosSyncInfo',
        name: 'tosSyncInfo',
        // meta: {
        //     text: 'Event',
        // },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        redirect: { name: 'tosSyncInfo-list' },
        children: [
            {
                path: 'list',
                name: 'tosSyncInfo-list',
                component: () => import('@/views/categories/tosSyncInfo/List.vue'),
                meta: {
                    pageTitle: 'route.common.tosSyncInfo',
                    breadcrumb: [
                        {
                            text: 'route.common.tosSyncInfo',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.tosSyncInfo',
                        },
                    ],
                },
            },
            // {
            //     path: 'detail/:issueId',
            //     name: 'issue-detail',
            //     component: () => import('@/views/categories/issues/Detail.vue'),
            //     meta: {
            //         pageTitle: 'route.breadcrumb.issue',
            //         breadcrumb: [
            //             {
            //                 text: 'route.common.issue',
            //                 active: true,
            //             },
            //             {
            //                 to: { name: 'issue-list' },
            //                 text: 'route.breadcrumb.issue',
            //             },
            //         ],
            //     },
            // },
        ],
    },
]
