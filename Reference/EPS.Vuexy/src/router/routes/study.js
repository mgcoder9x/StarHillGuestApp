/* eslint-disable */
export default [
    {
        path: '/study/student',
        name: 'study-student',
        redirect: { name: 'study-student-list' },
        meta: {
            text: 'Student',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'study-student-list',
                component: () => import('@/views/study/student/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.student',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'study-student-create',
                component: () => import('@/views/study/student/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.student',
                        },
                    ],
                },
            },
            {
                path: 'detail/:studentId',
                name: 'study-student-detail',
                component: () => import('@/views/study/student/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.student',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/study/course',
        name: 'study-course',
        redirect: { name: 'study-course-list' },
        meta: {
            text: 'Course',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'study-course-list',
                component: () => import('@/views/study/course/list.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.course',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'study-course-create',
                component: () =>
                    import('@/views/study/course/create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.course',
                        },
                    ],
                },
            },
            {
                path: 'detail/:courseId',
                name: 'study-course-detail',
                component: () =>
                    import('@/views/study/course/detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.course',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/study/classes',
        name: 'study-classes',
        redirect: { name: 'study-classes-list' },
        meta: {
            text: 'Classes',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'study-classes-list',
                component: () => import('@/views/study/classes/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.classes',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'study-classes-create',
                component: () =>
                    import('@/views/study/classes/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.classes',
                        },
                    ],
                },
            },
            {
                path: 'detail/:classId',
                name: 'study-classes-detail',
                component: () =>
                    import('@/views/study/classes/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.classes',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/study/lesson',
        name: 'study-lesson',
        redirect: { name: 'study-lesson-list' },
        meta: {
            text: 'Lesson',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'study-lesson-list',
                component: () => import('@/views/study/lesson/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.lesson',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'study-lesson-create',
                component: () =>
                    import('@/views/study/lesson/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.lesson',
                        },
                    ],
                },
            },
            {
                path: 'detail/:lessonId',
                name: 'study-lesson-detail',
                component: () =>
                    import('@/views/study/lesson/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.study',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.lesson',
                        },
                    ],
                },
            },
        ],
    },
]
