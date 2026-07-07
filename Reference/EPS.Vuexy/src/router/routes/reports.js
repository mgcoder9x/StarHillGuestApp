/* eslint-disable */
export default [
    {
        path: '/reports/test',
        name: 'reports-test',
        component: () => import('@/views/reports/reportTest.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.reportTest',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/reports-firework-inspection-statistics-25',
        name: 'reports-firework-inspection-statistics-25',
        component: () =>
            import('@/views/reports/reportFireworkInspectionStatistics25.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.reportFireworkInspectionStatistics25',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/violation',
        name: 'vehicle-violation-report',
        meta: {
            text: 'Reports',
        },
        component: () => import('@/views/reports/vehicleViolationReport.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.vehicleViolationReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/countSupervision',
        name: 'vehicle-supervision-report',
        meta: {
            text: 'ReportcountSupervision',
        },
        component: () => import('@/views/reports/countSupervisionReport.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.countSupervisionReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/reportConveyor',
        name: 'conveyor-report',
        meta: {
            text: 'ReportcountConveyor',
        },
        component: () => import('@/views/reports/reportConveyorEvent.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.countConveyorReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/vehicleDetailReport',
        name: 'reports-vehicle-detail',
        meta: {
            text: 'ReportcountConveyor',
        },
        component: () => import('@/views/reports/reportVehicleDetail.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.vehicleDetailReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/reportFireEvent',
        name: 'reportFireEvent',
        meta: {
            text: 'ReportFireEvent',
        },
        component: () => import('@/views/reports/reportFireEvent.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.reportFireEvent',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/reportVehicleWarning',
        name: 'reportVehicleWarning',
        meta: {
            text: 'ReportVehicleWarning',
        },
        component: () => import('@/views/reports/reportVehicleWarning.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.reportVehicleWarning',
                    active: true,
                },
            ],
        },
    },

    // ===========================
    // NEW: Vehicles in Plant/Warehouse
    // ===========================
    {
        path: '/reports/vehicleInPlant',
        name: 'reports-vehicle-in-plant',
        meta: {
            text: 'Reports',
        },
        component: () => import('@/views/reports/InPlantList.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.vehicleInPlantReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/vehicleCount',
        name: 'reports-count-vehicle',
        meta: {
            text: 'Reports',
        },
        component: () => import('@/views/reports/reportCountVehicle.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.countVehicleReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/violatePerson',
        name: 'reports-violate-person',
        meta: {
            text: 'Reports',
        },
        component: () => import('@/views/reports/RestrictedZonePersonReport.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.violatePersonReport',
                    active: true,
                },
            ],
        },
    },
    // Nghi Son Reports
    {
        path: '/reports/inOutStatics',
        name: 'reports-in-out-statics',
        component: () => import('@/views/reports/inOutStaticsReport.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.inOutStaticsReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/goodsStatistics',
        name: 'reports-goods-statistics',
        component: () => import('@/views/reports/reportGoodsStatisticsByPort.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.goodsStatisticsReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/portGateVehicleTraffic',
        name: 'reports-port-gate-vehicle-traffic',
        component: () => import('@/views/reports/portGateVehicleTrafficReport.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.portGateVehicleTrafficReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/violateRestrictedArea',
        name: 'reports-violate-restricted-area',
        component: () => import('@/views/reports/violateRestrictedArea.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.violateRestrictedAreaReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/wrongPlan',
        name: 'reports-wrong-plan',
        component: () => import('@/views/reports/wrongPlanReport.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.wrongPlanReport',
                    active: true,
                },
            ],
        },
    },
    // Novaland Reports
    {
        path: '/reports/safetyEquipmentViolation',
        name: 'reports-safety-equipment-violation',
        component: () => import('@/views/reports/safetyEquipmentViolationReport.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.safetyEquipmentViolationReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/safetyBarrierStatistics',
        name: 'reports-safety-barrier-statistics',
        component: () => import('@/views/reports/safetyBarrierStatisticsDetailReport.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.safetyBarrierStatisticsDetailReport',
                    active: true,
                },
            ],
        },
    },
    {
        path: '/reports/containerMonitoring',
        name: 'reports-container-monitoring',
        component: () => import('@/views/reports/containerMonitoringReport.vue'),
        meta: {
            pageTitle: 'route.report',
            breadcrumb: [
                {
                    text: 'route.common.reports',
                    active: true,
                },
                {
                    text: 'route.breadcrumb.containerMonitoringReport',
                    active: true,
                },
            ],
        },
    },
]
