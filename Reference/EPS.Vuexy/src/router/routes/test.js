/* eslint-disable */
export default [
    {
        path: '/test/config',
        name: 'test-config',
        redirect: { name: 'test-config-list' },
        meta: {
            text: 'Config',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'test-config-list',
                component: () => import('@/views/test/config/list.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.test',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.config',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'test-config-create',
                component: () => import('@/views/test/config/create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.test',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.config',
                        },
                    ],
                },
            },
            {
                path: 'detail/:configId',
                name: 'test-config-detail',
                component: () => import('@/views/test/config/detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.test',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.config',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/test/test',
        name: 'test-test',
        redirect: { name: 'test-test-list' },
        meta: {
            text: 'Test',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'test-test-list',
                component: () => import('@/views/test/test/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.test',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.test',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'test-test-create',
                component: () => import('@/views/test/test/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.test',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.test',
                        },
                    ],
                },
            },
            {
                path: 'detail/:testId',
                name: 'test-test-detail',
                component: () => import('@/views/test/test/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.test',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.test',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/test/question',
        name: 'test-question',
        redirect: { name: 'test-question-list' },
        meta: {
            text: 'Question',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'test-question-list',
                component: () => import('@/views/test/question/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.test',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.question',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'test-question-create',
                component: () => import('@/views/test/question/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.test',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.question',
                        },
                    ],
                },
            },
            {
                path: 'detail/:questionId',
                name: 'test-question-detail',
                component: () => import('@/views/test/question/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.test',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.question',
                        },
                    ],
                },
            },
        ],
    },
    
    {
        path: '/test/result',
        name: 'test-result',
        redirect: { name: 'test-result-list' },
        meta: {
            text: 'Result',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'test-result-list',
                component: () => import('@/views/test/result/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.test',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.result',
                        },
                    ],
                },
            },
            {
                path: 'detail/:resultId',
                name: 'test-result-detail',
                component: () => import('@/views/test/result/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.test',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.result',
                        },
                    ],
                },
            },
        ],
    },
]
