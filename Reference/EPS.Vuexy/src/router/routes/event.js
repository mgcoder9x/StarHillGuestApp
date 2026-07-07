import { getCurrentLocale, loadRouteLocaleMessage } from '@/libs/i18n'

/* eslint-disable */
export default [
    {
        path: '/event/allEvents',
        name: 'event-allEvents',
        meta: {
            text: 'Event',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        redirect: { name: 'event-list' },
        children: [
            {
                path: 'list',
                name: 'event-list',
                component: () => import('@/views/events/allEvents/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.events',
                        },
                    ],
                },
            },
            {
                path: 'detail/:eventId',
                name: 'event-detail',
                component: () => import('@/views/events/allEvents/Detail.vue'),
                meta: {
                    pageTitle: 'route.breadcrumb.events',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-list' },
                            text: 'route.breadcrumb.events',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/vehicleEvent',
        name: 'event-vehicleEvent',
        meta: {
            text: 'VehicleEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        redirect: { name: 'event-vehicleEvent-list' },
        children: [
            {
                path: 'list',
                name: 'event-vehicleEvent-list',
                component: () => import('@/views/events/vehicleEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.vehicleEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:eventId',
                name: 'event-vehicleEvent-detail',
                component: () =>
                    import('@/views/events/vehicleEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-vehicleEvent-list' },
                            text: 'route.breadcrumb.vehicleEvent',
                        },
                    ],
                },
            },
            {
                path: 'itinerary/:type/:eventId/:dateFrom/:dateTo',
                name: 'itinerary',
                component: () =>
                    import('@/views/dashboard/itinerary/itinerary.vue'),
                meta: {
                    pageTitle: 'route.breadcrumb.dashboardItinerary',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-vehicleEvent-list' },
                            text: 'route.breadcrumb.vehicleEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/fireWorkEvent',
        name: 'event-fireWorkEvent',
        redirect: { name: 'event-fireWorkEvent-list' },
        meta: {
            text: 'FireWorkEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'event-fireWorkEvent-list',
                component: () =>
                    import('@/views/events/fireWorkEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.fireWorkEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:eventId',
                name: 'event-fireWorkEvent-detail',
                component: () =>
                    import('@/views/events/fireWorkEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-fireWorkEvent-list' },
                            text: 'route.breadcrumb.fireWorkEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/fireEvent',
        name: 'event-fireEvent',
        meta: {
            text: 'FireEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        redirect: { name: 'event-fireEvent-list' },
        children: [
            {
                path: 'list',
                name: 'event-fireEvent-list',
                component: () => import('@/views/events/fireEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.fireEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:eventId',
                name: 'event-fireEvent-detail',
                component: () => import('@/views/events/fireEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-fireEvent-list' },
                            text: 'route.breadcrumb.fireEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/peopleCountEvent',
        name: 'event-peopleCountEvent',
        redirect: { name: 'event-peopleCountEvent-list' },
        meta: {
            text: 'PeopleCountEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'event-peopleCountEvent-list',
                component: () =>
                    import('@/views/events/peopleCountEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.peopleCountEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/waterEvent',
        name: 'event-waterEvent',
        redirect: { name: 'event-waterEvent-list' },
        meta: {
            text: 'WaterEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'event-waterEvent-list',
                component: () => import('@/views/events/waterEvent/list.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.waterEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'event-waterEvent-detail',
                component: () => import('@/views/events/waterEvent/detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-waterEvent-list' },
                            text: 'route.breadcrumb.waterEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/manufacturing',
        name: 'event-manufacturing',
        redirect: { name: 'event-manufacturing-list' },
        meta: {
            text: 'Manufacturing',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'event-manufacturing-list',
                component: () =>
                    import('@/views/events/manufacturing/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.manufacturing',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'event-manufacturing-detail',
                component: () =>
                    import('@/views/events/manufacturing/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-manufacturing-list' },
                            text: 'route.breadcrumb.manufacturing',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/virtualFenceEvent',
        name: 'event-virtualFenceEvent',
        redirect: { name: 'event-virtualFenceEvent-list' },
        meta: {
            text: 'VirtualFenceEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'event-virtualFenceEvent-list',
                component: () =>
                    import('@/views/events/virtualFenceEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.virtualFenceEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:eventId',
                name: 'event-virtualFenceEvent-detail',
                component: () =>
                    import('@/views/events/virtualFenceEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-virtualFenceEvent-list' },
                            text: 'route.breadcrumb.virtualFenceEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/faceEvent',
        name: 'event-faceEvent',
        redirect: { name: 'event-faceEvent-list' },
        meta: {
            text: 'FaceEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'event-faceEvent-list',
                component: () => import('@/views/events/faceEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.faceEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'event-faceEvent-detail',
                component: () => import('@/views/events/faceEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-faceEvent-list' },
                            text: 'route.breadcrumb.faceEvent',
                        },
                    ],
                },
            },
            {
                path: 'itinerary/:type/:eventId/:dateFrom/:dateTo',
                name: 'itinerary',
                component: () =>
                    import('@/views/dashboard/itinerary/itinerary.vue'),
                meta: {
                    pageTitle: 'route.breadcrumb.dashboardItinerary',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-faceEvent-list' },
                            text: 'route.breadcrumb.faceEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/faceGateEvent',
        name: 'event-faceGateEvent',
        redirect: { name: 'event-faceGateEvent-list' },
        meta: {
            text: 'FaceGateEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'event-faceGateEvent-list',
                component: () =>
                    import('@/views/events/faceGateEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.faceGateEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'event-faceGateEvent-detail',
                component: () =>
                    import('@/views/events/faceGateEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-faceGateEvent-list' },
                            text: 'route.breadcrumb.faceGateEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/protectiveEquipmentEvent',
        name: 'event-protectiveEquipmentEvent',
        beforeEnter: (to, from, next) => {
            const currentLocale = getCurrentLocale()
            const parentPath = to.matched[0].path
            loadRouteLocaleMessage(currentLocale, parentPath).then(() => next())
        },
        redirect: { name: 'event-protectiveEquipmentEvent-list' },
        meta: {
            text: 'ProtectiveEquipmentEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },

        children: [
            {
                path: 'list',
                name: 'event-protectiveEquipmentEvent-list',
                component: () =>
                    import('@/views/events/protectiveEquipmentEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.protectiveEquipmentEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'event-protectiveEquipmentEvent-detail',
                component: () =>
                    import(
                        '@/views/events/protectiveEquipmentEvent/Detail.vue'
                    ),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: {
                                name: 'event-protectiveEquipmentEvent-detail',
                            },
                            text: 'route.breadcrumb.protectiveEquipmentEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/carEvent',
        name: 'event-carEvent',
        beforeEnter: (to, from, next) => {
            const currentLocale = getCurrentLocale()
            const parentPath = to.matched[0].path
            loadRouteLocaleMessage(currentLocale, parentPath).then(() => next())
        },
        redirect: { name: 'event-carEvent-list' },
        meta: {
            text: 'carEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },

        children: [
            {
                path: 'list',
                name: 'event-carEvent-list',
                component: () => import('@/views/events/carEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.CarEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:eventId',
                name: 'event-carEvent-detail',
                component: () => import('@/views/events/carEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: {
                                name: 'event-carEvent-detail',
                            },
                            text: 'route.breadcrumb.CarEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/problem',
        name: 'event-problem',
        beforeEnter: (to, from, next) => {
            const currentLocale = getCurrentLocale()
            const parentPath = to.matched[0].path
            loadRouteLocaleMessage(currentLocale, parentPath).then(() => next())
        },
        redirect: { name: 'event-problem-list' },
        meta: {
            text: 'problem',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },

        children: [
            {
                path: 'list',
                name: 'event-problem-list',
                component: () => import('@/views/events/problem/list.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.Problem',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'event-problem-detail',
                component: () =>
                    import(
                        '@/views/events/protectiveEquipmentEvent/Detail.vue'
                    ),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: {
                                name: 'event-protectiveEquipmentEvent-detail',
                            },
                            text: 'route.breadcrumb.protectiveEquipmentEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/conveyorEvent',
        name: 'event-conveyorEvent',
        meta: {
            text: 'ConveyorEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        redirect: { name: 'event-conveyorEvent-list' },
        children: [
            {
                path: 'list',
                name: 'event-conveyorEvent-list',
                component: () =>
                    import('@/views/events/conveyorEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.conveyorEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:eventId',
                name: 'event-detail',
                component: () =>
                    import('@/views/events/conveyorEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.breadcrumb.conveyorEvent',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-conveyorEvent-list' },
                            text: 'route.breadcrumb.conveyorEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/trafficViolationEvent',
        name: 'event-trafficViolationEvent',
        redirect: { name: 'event-trafficViolationEvent-list' },
        meta: {
            text: 'TrafficViolationEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'event-trafficViolationEvent-list',
                component: () =>
                    import('@/views/events/trafficViolationEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.trafficViolationEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'event-trafficViolationEvent-detail',
                component: () =>
                    import('@/views/events/trafficViolationEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-trafficViolationEvent-list' },
                            text: 'route.breadcrumb.trafficViolationEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/restrictedZonesEvent',
        name: 'event-restrictedZonesEvent',
        redirect: { name: 'event-restrictedZonesEvent-list' },
        meta: {
            text: 'restrictedZonesEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'event-restrictedZonesEvent-list',
                component: () =>
                    import('@/views/events/restrictedZonesEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.restrictedZonesEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:eventId',
                name: 'event-restrictedZonesEvent-detail',
                component: () =>
                    import('@/views/events/restrictedZonesEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-restrictedZonesEvent-list' },
                            text: 'route.breadcrumb.restrictedZonesEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/vehicleSessionsEvent',
        name: 'event-vehicleSessionsEvent',
        redirect: { name: 'event-vehicleSessionsEvent-list' },
        meta: {
            text: 'vehicleSessionsEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'event-vehicleSessionsEvent-list',
                component: () =>
                    import('@/views/events/vehicleSessions/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.vehicleSessionsEvent',
                        },
                    ],
                },
            },

            {
                path: 'detail/:sessionId',
                name: 'event-vehicleSessionsEvent-detail',
                component: () =>
                    import('@/views/events/vehicleSessions/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-vehicleSessionsEvent-list' },
                            text: 'route.breadcrumb.vehicleSessionsEvent',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/workAtHeightMonitoring',
        name: 'event-workAtHeightMonitoring',
        beforeEnter: (to, from, next) => {
            const currentLocale = getCurrentLocale()
            const parentPath = to.matched[0].path
            loadRouteLocaleMessage(currentLocale, parentPath).then(() => next())
        },
        redirect: { name: 'event-workAtHeightMonitoring-list' },
        meta: {
            text: 'WorkAtHeightMonitoring',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },

        children: [
            {
                path: 'list',
                name: 'event-workAtHeightMonitoring-list',
                component: () =>
                    import('@/views/events/workAtHeightMonitoring/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.workAtHeightMonitoring',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'event-workAtHeightMonitoring-detail',
                component: () =>
                    import(
                        '@/views/events/workAtHeightMonitoring/Detail.vue'
                    ),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: {
                                name: 'event-workAtHeightMonitoring-detail',
                            },
                            text: 'route.breadcrumb.workAtHeightMonitoring',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/event/peopleCountInOutEvent',
        name: 'event-peopleCountInOutEvent',
        redirect: { name: 'event-peopleCountInOutEvent-list' },
        meta: {
            text: 'PeopleCountInOutEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'event-peopleCountInOutEvent-list',
                component: () =>
                    import('@/views/events/peopleCountInOutEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.peopleCountInOutEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'event-peopleCountInOutEvent-detail',
                component: () =>
                    import('@/views/events/peopleCountInOutEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-peopleCountInOutEvent-list' },
                            text: 'route.breadcrumb.peopleCountInOutEvent',
                        },
                    ],
                },
            },
        ],
    },
    // Production Line Event routes
    {
        path: '/event/productionLineEvent',
        name: 'event-productionLineEvent',
        meta: {
            text: 'ProductionLineEvent',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        redirect: { name: 'event-productionLineEvent-list' },
        children: [
            {
                path: 'list',
                name: 'event-productionLineEvent-list',
                component: () =>
                    import('@/views/events/productionLineEvent/List.vue'),
                meta: {
                    pageTitle: 'route.lookup',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.productionLineEvent',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'event-productionLineEvent-detail',
                component: () =>
                    import('@/views/events/productionLineEvent/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.events',
                            active: true,
                        },
                        {
                            to: { name: 'event-productionLineEvent-list' },
                            text: 'route.breadcrumb.productionLineEvent',
                        },
                    ],
                },
            },
        ],
    },
]
