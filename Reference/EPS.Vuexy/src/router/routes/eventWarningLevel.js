/* eslint-disable */
export default [
  {
    path: '/categories/event-warning-level',
    name: 'categories-event-warning-level',
    redirect: { name: 'event-warning-level' },
    component: { render(c) { return c('router-view') } },
    children: [
        {
          path: 'list',
          name: 'event-warning-level',
          component: () => import('@/views/categories/event-warning-level/index.vue'),
          meta: {
            pageTitle: 'route.list',
            breadcrumb: [
              { text: 'route.common.categories', active: true },
              { text: 'route.breadcrumb.eventWarningLevels' },
            ],
          },
        },
        {
          path: 'create',
          name: 'event-warning-level-create',
          component: () => import('@/views/categories/event-warning-level/create.vue'),
          meta: {
            pageTitle: 'route.create',
            breadcrumb: [
              { text: 'route.common.categories', active: true },
              { text: 'route.breadcrumb.eventWarningLevels', to: { name: 'event-warning-level' } },
              { text: 'route.create' },
            ],
          },
        },
        {
          path: 'detail/:id',
          name: 'event-warning-level-detail',
          component: () => import('@/views/categories/event-warning-level/detail.vue'),
          meta: {
            pageTitle: 'route.detail',
            breadcrumb: [
              { text: 'route.common.categories', active: true },
              { text: 'route.breadcrumb.eventWarningLevels', to: { name: 'event-warning-level' } },
              { text: 'route.detail' },
            ],
          },
        },
    ],
  },
]
