/* eslint-disable */
export default [
  {
    path: '/categories/event-type',
    name: 'categories-event-type',
    redirect: { name: 'event-type' },
    component: { render(c) { return c('router-view') } },
    children: [
      {
        path: 'list',
        name: 'event-type',
        component: () => import('@/views/categories/event-type/index.vue'),
        meta: {
          pageTitle: 'route.list',
          breadcrumb: [
            { text: 'route.common.categories', active: true },
            { text: 'route.breadcrumb.eventTypes' },
          ],
        },
      },
      {
        path: 'create',
        name: 'event-type-create',
        component: () => import('@/views/categories/event-type/create.vue'),
        meta: {
          pageTitle: 'route.create',
          breadcrumb: [
            { text: 'route.common.categories', active: true },
            { text: 'route.breadcrumb.eventTypes', to: { name: 'event-type' } },
            { text: 'route.create' },
          ],
        },
      },
      {
        path: 'detail/:id',
        name: 'event-type-detail',
        component: () => import('@/views/categories/event-type/detail.vue'),
        meta: {
          pageTitle: 'route.detail',
          breadcrumb: [
            { text: 'route.common.categories', active: true },
            { text: 'route.breadcrumb.eventTypes', to: { name: 'event-type' } },
            { text: 'route.detail' },
          ],
        },
      },
    ],
  },
]
