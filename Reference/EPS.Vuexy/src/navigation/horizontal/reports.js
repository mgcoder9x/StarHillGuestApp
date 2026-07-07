/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

// Conveyor Report component
export const ReportParent = {
    code: 'reports',
    name: 'Nav.Reports',
    header: 'route.common.reports',
    icon: 'line-md:document-report',
}

// Conveyor Report component
export const Conveyor = {
    code: 'conveyor-report',
    name: 'Nav.ConveyorReport',
    title: 'route.breadcrumb.countConveyorReport',
    route: 'conveyor-report',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewConveyorReport'],
}

// Supervision Report component
export const Supervision = {
    code: 'supervision-report',
    name: 'Nav.SupervisorReport',
    title: 'route.breadcrumb.countSupervisionReport',
    route: 'vehicle-supervision-report',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewSupervisorReport'],
}

// Vehicle Detail Report component
export const VehicleDetail = {
    code: 'reports-vehicle-detail',
    name: 'Nav.VehicleDetailReport',
    title: 'route.breadcrumb.vehicleDetailReport',
    route: 'reports-vehicle-detail',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewVehicleDetailReport'],
}

// Vehicle Violation Report component
export const VehicleViolation = {
    code: 'vehicle-violation-report',
    name: 'Nav.VehicleViolationReports',
    title: 'route.breadcrumb.vehicleViolationReport',
    route: 'vehicle-violation-report',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewVehicleViolationReports'],
}

// Fire Event Report component
export const FireEvent = {
    code: 'reportFireEvent',
    name: 'Nav.reportFireEvent',
    title: 'route.breadcrumb.fireSmokeStatistics',
    route: 'reportFireEvent',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewReportFireEvent'],
}

export const ReportVehicleWarning = {
    code: 'reportVehicleWarning',
    name: 'Nav.ReportVehicleWarning',
    title: 'route.breadcrumb.reportVehicleWarning',
    route: 'reportVehicleWarning',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewReportVehicleWarning'],
}
export const VehicleInPlantReport = {
    code: 'vehicleInPlantReport',
    name: 'route.breadcrumb.vehicleInPlantReport',
    title: 'route.breadcrumb.vehicleInPlantReport',
    route: 'reports-vehicle-in-plant',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewVehicleInPlantReport'],
}
export const CountVehicleReport = {
    code: 'countVehicleReport',
    name: 'route.breadcrumb.countVehicleReport',
    title: 'route.breadcrumb.countVehicleReport',
    route: 'reports-count-vehicle',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewCountVehicleReport'],
}
export const ViolatePersonReport = {
    code: 'violatePersonReport',
    name: 'route.breadcrumb.violatePersonReport',
    title: 'route.breadcrumb.violatePersonReport',
    route: 'reports-violate-person',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewViolatePersonReport'],
}

// Nghi Son Reports
export const InOutStaticsReport = {
    code: 'inOutStaticsReport',
    name: 'route.breadcrumb.inOutStaticsReport',
    title: 'route.breadcrumb.inOutStaticsReport',
    route: 'reports-in-out-statics',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewInOutStaticsReport'],
}
export const GoodsStatisticsReport = {
    code: 'goodsStatisticsReport',
    name: 'route.breadcrumb.goodsStatisticsReport',
    title: 'route.breadcrumb.goodsStatisticsReport',
    route: 'reports-goods-statistics',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewGoodsStatisticsReport'],
}
export const PortGateVehicleTrafficReport = {
    code: 'portGateVehicleTrafficReport',
    name: 'route.breadcrumb.portGateVehicleTrafficReport',
    title: 'route.breadcrumb.portGateVehicleTrafficReport',
    route: 'reports-port-gate-vehicle-traffic',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewPortGateVehicleTrafficReport'],
}
export const ViolateRestrictedAreaReport = {
    code: 'violateRestrictedAreaReport',
    name: 'route.breadcrumb.violateRestrictedAreaReport',
    title: 'route.breadcrumb.violateRestrictedAreaReport',
    route: 'reports-violate-restricted-area',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewViolateRestrictedAreaReport'],
}
export const WrongPlanReport = {
    code: 'wrongPlanReport',
    name: 'route.breadcrumb.wrongPlanReport',
    title: 'route.breadcrumb.wrongPlanReport',
    route: 'reports-wrong-plan',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewWrongPlanReport'],
}

// Novaland Reports
export const SafetyEquipmentViolationReport = {
    code: 'safetyEquipmentViolationReport',
    name: 'route.breadcrumb.safetyEquipmentViolationReport',
    title: 'route.breadcrumb.safetyEquipmentViolationReport',
    route: 'reports-safety-equipment-violation',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewSafetyEquipmentViolationReport'],
}
export const SafetyBarrierStatisticsDetailReport = {
    code: 'safetyBarrierStatisticsDetailReport',
    name: 'route.breadcrumb.safetyBarrierStatisticsDetailReport',
    title: 'route.breadcrumb.safetyBarrierStatisticsDetailReport',
    route: 'reports-safety-barrier-statistics',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewSafetyBarrierStatisticsDetailReport'],
}
export const ContainerMonitoringReport = {
    code: 'containerMonitoringReport',
    name: 'route.breadcrumb.containerMonitoringReport',
    title: 'route.breadcrumb.containerMonitoringReport',
    route: 'reports-container-monitoring',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewContainerMonitoringReport'],
}

export const ReportMap = {
    Conveyor,
    Supervision,
    VehicleDetail,
    VehicleViolation,
    FireEvent,
    ReportVehicleWarning,
    VehicleInPlantReport,
    CountVehicleReport,
    ViolatePersonReport,
    InOutStaticsReport,
    GoodsStatisticsReport,
    PortGateVehicleTrafficReport,
    ViolateRestrictedAreaReport,
    WrongPlanReport,
    SafetyEquipmentViolationReport,
    SafetyBarrierStatisticsDetailReport,
    ContainerMonitoringReport,
}

export const reportHorizontalNav = createNavItemFactory(ReportMap, ReportParent)

export default [
    {
        ...createNavItemFactory(ReportMap, ReportParent),
    },
]
