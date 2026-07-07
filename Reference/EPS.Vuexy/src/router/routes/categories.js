import eventType from './eventType'
import eventWarningLevel from './eventWarningLevel'

/* eslint-disable */
export default [
    {
        path: '/categories/areas',
        name: 'categories-areas',
        redirect: { name: 'categories-areas-list' },
        meta: {
            text: 'Areas',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-areas-list',
                component: () => import('@/views/categories/areas/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.areas',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-areas-create',
                component: () => import('@/views/categories/areas/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.areas',
                        },
                    ],
                },
            },
            {
                path: 'detail/:areaId',
                name: 'categories-areas-detail',
                component: () => import('@/views/categories/areas/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.areas',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/devices',
        name: 'categories-devices',
        redirect: { name: 'categories-devices-list' },
        meta: {
            text: 'Camera',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-devices-list',
                component: () => import('@/views/categories/devices/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.devices',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-devices-create',
                component: () =>
                    import('@/views/categories/devices/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.devices',
                        },
                    ],
                },
            },
            {
                path: 'detail/:deviceId',
                name: 'categories-devices-detail',
                component: () =>
                    import('@/views/categories/devices/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.devices',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/departments',
        name: 'categories-departments',
        redirect: { name: 'categories-departments-list' },
        meta: {
            text: 'Departments',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-departments-list',
                component: () =>
                    import('@/views/categories/departments/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.departments',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-departments-create',
                component: () =>
                    import('@/views/categories/departments/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.departments',
                        },
                    ],
                },
            },
            {
                path: 'detail/:departmentId',
                name: 'categories-departments-detail',
                component: () =>
                    import('@/views/categories/departments/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.departments',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/contractors',
        name: 'categories-contractors',
        redirect: { name: 'categories-contractors-list' },
        meta: {
            text: 'Contractors',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-contractors-list',
                component: () =>
                    import('@/views/categories/contractors/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.contractors',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-contractors-create',
                component: () =>
                    import('@/views/categories/contractors/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.contractors',
                        },
                    ],
                },
            },
            {
                path: 'detail/:contractorId',
                name: 'categories-contractors-detail',
                component: () =>
                    import('@/views/categories/contractors/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.contractors',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/groups',
        name: 'categories-groups',
        redirect: { name: 'categories-groups-list' },
        meta: {
            text: 'Groups',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-groups-list',
                component: () => import('@/views/categories/groups/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.groups',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-groups-create',
                component: () => import('@/views/categories/groups/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.groups',
                        },
                    ],
                },
            },
            {
                path: 'detail/:groupId',
                name: 'categories-groups-detail',
                component: () => import('@/views/categories/groups/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.groups',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/machines',
        name: 'categories-machines',
        redirect: { name: 'categories-machines-list' },
        meta: {
            text: 'Machines',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-machines-list',
                component: () => import('@/views/categories/machines/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.machines',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-machines-create',
                component: () =>
                    import('@/views/categories/machines/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.machines',
                        },
                    ],
                },
            },
            {
                path: 'detail/:machineId',
                name: 'categories-machines-detail',
                component: () =>
                    import('@/views/categories/machines/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.machines',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/timeAccesses',
        name: 'categories-timeAccesses',
        redirect: { name: 'categories-timeAccesses-list' },
        meta: {
            text: 'TimeAccesses',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-timeAccesses-list',
                component: () =>
                    import('@/views/categories/timeAccesses/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.timeAccesses',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-timeAccesses-create',
                component: () =>
                    import('@/views/categories/timeAccesses/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.timeAccesses',
                        },
                    ],
                },
            },
            {
                path: 'detail/:timeAccessId',
                name: 'categories-timeAccess-detail',
                component: () =>
                    import('@/views/categories/timeAccesses/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.timeAccesses',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/groupAccesses',
        name: 'categories-groupAccesses',
        redirect: { name: 'categories-groupAccesses-list' },
        meta: {
            text: 'GroupAccesses',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-groupAccesses-list',
                component: () =>
                    import('@/views/categories/groupAccesses/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.groupAccesses',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-groupAccesses-create',
                component: () =>
                    import('@/views/categories/groupAccesses/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.groupAccesses',
                        },
                    ],
                },
            },
            {
                path: 'detail/:groupAccessId',
                name: 'categories-groupAccesses-detail',
                component: () =>
                    import('@/views/categories/groupAccesses/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.groupAccesses',
                        },
                    ],
                },
            },
        ],
    },

    {
        path: '/categories/water-warning',
        name: 'water-warning',
        redirect: { name: 'water-warning-list' },
        meta: {
            text: 'WaterWarning',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'create',
                name: 'water-warning-create',
                component: () =>
                    import('@/views/categories/waterWarning/create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.waterWarning',
                        },
                    ],
                },
            },
            {
                path: 'list',
                name: 'water-warning-list',
                component: () =>
                    import('@/views/categories/waterWarning/list.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.waterWarning',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'water-warning-detail',
                component: () =>
                    import('@/views/categories/waterWarning/detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.waterWarning',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/vehicles',
        name: 'categories-vehicles',
        redirect: { name: 'categories-vehicles-list' },
        meta: {
            text: 'Vehicles',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-vehicles-list',
                component: () => import('@/views/categories/vehicles/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.vehicles',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-vehicles-create',
                component: () =>
                    import('@/views/categories/vehicles/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.vehicles',
                        },
                    ],
                },
            },
            {
                path: 'detail/:vehicleId',
                name: 'categories-vehicles-detail',
                component: () =>
                    import('@/views/categories/vehicles/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.vehicles',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/employees',
        name: 'categories-employees',
        redirect: { name: 'categories-employees-list' },
        meta: {
            text: 'employees',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-employees-list',
                component: () =>
                    import('@/views/categories/employees/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.employees',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-employees-create',
                component: () =>
                    import('@/views/categories/employees/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.employees',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'categories-employees-detail',
                component: () =>
                    import('@/views/categories/employees/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.employees',
                        },
                    ],
                },
            },
            {
                path: 'import',
                name: 'import',
                component: () =>
                    import('@/views/categories/employees/Import.vue'),
                meta: {
                    pageTitle: 'route.common.information',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                        },
                        {
                            text: 'categories.employees.import.header',
                            active: true,
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/guess',
        name: 'categories-guess',
        redirect: { name: 'categories-guess-list' },
        meta: {
            text: 'guess',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-guess-list',
                component: () => import('@/views/categories/guess/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.guest',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-guess-create',
                component: () => import('@/views/categories/guess/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.guest',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'categories-guess-detail',
                component: () => import('@/views/categories/guess/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.guest',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/tosSyncInfo',
        name: 'categories-tosSyncInfo',
        component: () => import('@/views/categories/tosSyncInfo/List.vue'),
        meta: {
            pageTitle: 'route.list',
            breadcrumb: [
                {
                    text: 'route.common.categories',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.tosSyncInfo',
                },
            ],
        },
    },
    {
        path: '/categories/lane',
        name: 'categories-lane',
        redirect: { name: 'categories-lane-list' },
        meta: {
            text: 'lane',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-lane-list',
                component: () => import('@/views/categories/lanes/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.lane',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-lane-create',
                component: () => import('@/views/categories/lanes/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.lane',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'categories-lane-detail',
                component: () => import('@/views/categories/lanes/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.lane',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/vehicle-cards',
        name: 'categories-vehicleCard',
        redirect: { name: 'categories-vehicleCards-list' },
        meta: {
            text: 'Vehicle Cards',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-vehicleCards-list',
                component: () =>
                    import('@/views/categories/vehicleCard/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.vehicleCards',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-vehicleCards-create',
                component: () =>
                    import('@/views/categories/vehicleCard/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.vehicleCards',
                        },
                    ],
                },
            },
            {
                path: 'detail/:vehicleCardId',
                name: 'categories-vehicleCards-detail',
                component: () =>
                    import('@/views/categories/vehicleCard/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.vehicleCards',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/rfid-configs',
        name: 'categories-rfidConfigs',
        redirect: { name: 'categories-rfidConfigs-list' },
        meta: {
            text: 'RFID Configs',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-rfidConfigs-list',
                component: () =>
                    import('@/views/categories/rfidConfig/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.rfidConfigs',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-rfidConfigs-create',
                component: () =>
                    import('@/views/categories/rfidConfig/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.rfidConfigs',
                        },
                    ],
                },
            },
            {
                path: 'detail/:rfidConfigId',
                name: 'categories-rfidConfigs-detail',
                component: () =>
                    import('@/views/categories/rfidConfig/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.rfidConfigs',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/controllers',
        name: 'categories-controllers',
        redirect: { name: 'categories-controllers-list' },
        meta: {
            text: 'Controllers',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-controllers-list',
                component: () =>
                    import('@/views/categories/controllers/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.controllers',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-controllers-create',
                component: () =>
                    import('@/views/categories/controllers/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.controllers',
                        },
                    ],
                },
            },
            {
                path: 'detail/:controllerId',
                name: 'categories-controllers-detail',
                component: () =>
                    import('@/views/categories/controllers/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.controllers',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/workingShifts',
        name: 'categories-workingShifts',
        redirect: { name: 'categories-workingShifts-list' },
        meta: {
            text: 'workingShifts',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-workingShifts-list',
                component: () =>
                    import('@/views/categories/workingShifts/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.workingShifts',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-workingShifts-create',
                component: () =>
                    import('@/views/categories/workingShifts/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.workingShifts',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'categories-workingShifts-detail',
                component: () =>
                    import('@/views/categories/workingShifts/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.workingShifts',
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/blackLists',
        name: 'categories-blackLists',
        redirect: { name: 'categories-blackLists-list' },
        meta: {
            text: 'blackLists',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-blackLists-list',
                component: () =>
                    import('@/views/categories/blackList/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.blackLists',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-blackLists-create',
                component: () =>
                    import('@/views/categories/blackList/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.blackLists',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'categories-blackLists-detail',
                component: () =>
                    import('@/views/categories/blackList/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.employees',
                        },
                    ],
                },
            },
            {
                path: 'import',
                name: 'import',
                component: () =>
                    import('@/views/categories/blackList/Import.vue'),
                meta: {
                    pageTitle: 'route.common.information',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                        },
                        {
                            text: 'categories.blacklist.import.header',
                            active: true,
                        },
                    ],
                },
            },
        ],
    },
    {
        path: '/categories/workFlows',
        name: 'categories-workFlows',
        redirect: { name: 'categories-workFlows-list' },
        meta: {
            text: 'workFlows',
        },
        component: {
            render(c) {
                return c('router-view')
            },
        },
        children: [
            {
                path: 'list',
                name: 'categories-workFlows-list',
                component: () =>
                    import('@/views/categories/workFlows/List.vue'),
                meta: {
                    pageTitle: 'route.list',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.workFlows',
                        },
                    ],
                },
            },
            {
                path: 'create',
                name: 'categories-workFlows-create',
                component: () =>
                    import('@/views/categories/workFlows/Create.vue'),
                meta: {
                    pageTitle: 'route.create',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.workFlows',
                        },
                    ],
                },
            },
            {
                path: 'detail/:id',
                name: 'categories-workFlows-detail',
                component: () =>
                    import('@/views/categories/workFlows/Detail.vue'),
                meta: {
                    pageTitle: 'route.detail',
                    breadcrumb: [
                        {
                            text: 'route.common.categories',
                            active: true,
                        },
                        {
                            text: 'route.breadcrumb.workFlows',
                        },
                    ],
                },
            },
        ],
    },
    ...eventType,
    ...eventWarningLevel,
]
