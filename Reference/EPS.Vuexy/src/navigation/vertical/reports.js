/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

// Conveyor Report component
export const ReportParent = {
    title: 'route.common.reports',
    icon: 'line-md:document-report',
    tagVariant: 'light-warning',
}

// Vehicle Detail Report component
export const VehicleDetail = {
    title: 'route.breadcrumb.vehicleDetailReport',
    route: 'reports-vehicle-detail',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewVehicleDetailReport'],
}

// Supervision Report component
export const CountSupervision = {
    title: 'route.breadcrumb.countSupervisionReport',
    route: 'vehicle-supervision-report',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewSupervisorReport'],
}

// Conveyor Report component
export const CountConveyor = {
    title: 'route.breadcrumb.countConveyorReport',
    route: 'conveyor-report',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewConveyorReport'],
}

// Vehicle Violation Report component
export const VehicleViolation = {
    title: 'route.breadcrumb.vehicleViolationReport',
    route: 'vehicle-violation-report',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewVehicleViolationReports'],
}

// Fire Event Report component
export const FireEvent = {
    title: 'route.breadcrumb.reportFireEvent',
    route: 'reportFireEvent',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewReportFireEvent'],
}

// Firework Inspection Statistics component
export const FireworkInspectionStatistics25 = {
    title: 'route.breadcrumb.reportFireworkInspectionStatistics25',
    route: 'reports-firework-inspection-statistics-25',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewReportFireworkInspectionStatistics25'],
}

// Vehicle Warning Report
export const ReportVehicleWarning = {
    title: 'route.breadcrumb.reportVehicleWarning',
    route: 'reportVehicleWarning',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewReportVehicleWarning'],
}

// ⬇️ NEW: Vehicles In Plant/Warehouse
export const VehicleInPlantReport = {
    title: 'route.breadcrumb.vehicleInPlantReport',
    route: 'reports-vehicle-in-plant',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewVehicleInPlantReport'],
}
export const CountVehicleReport = {
    title: 'route.breadcrumb.countVehicleReport',
    route: 'reports-count-vehicle',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewCountVehicleReport'],
}
export const ViolatePersonReport = {
    title: 'route.breadcrumb.violatePersonReport',
    route: 'reports-violate-person',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewViolatePersonReport'],
}

// Nghi Son Reports
export const InOutStaticsReport = {
    title: 'route.breadcrumb.inOutStaticsReport',
    route: 'reports-in-out-statics',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewInOutStaticsReport'],
}
export const GoodsStatisticsReport = {
    title: 'route.breadcrumb.goodsStatisticsReport',
    route: 'reports-goods-statistics',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewGoodsStatisticsReport'],
}
export const PortGateVehicleTrafficReport = {
    title: 'route.breadcrumb.portGateVehicleTrafficReport',
    route: 'reports-port-gate-vehicle-traffic',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewPortGateVehicleTrafficReport'],
}
export const ViolateRestrictedAreaReport = {
    title: 'route.breadcrumb.violateRestrictedAreaReport',
    route: 'reports-violate-restricted-area',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewViolateRestrictedAreaReport'],
}
export const WrongPlanReport = {
    title: 'route.breadcrumb.wrongPlanReport',
    route: 'reports-wrong-plan',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewWrongPlanReport'],
}

// Novaland Reports
export const SafetyEquipmentViolationReport = {
    title: 'route.breadcrumb.safetyEquipmentViolationReport',
    route: 'reports-safety-equipment-violation',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewSafetyEquipmentViolationReport'],
}
export const SafetyBarrierStatisticsDetailReport = {
    title: 'route.breadcrumb.safetyBarrierStatisticsDetailReport',
    route: 'reports-safety-barrier-statistics',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewSafetyBarrierStatisticsDetailReport'],
}
export const ContainerMonitoringReport = {
    title: 'route.breadcrumb.containerMonitoringReport',
    route: 'reports-container-monitoring',
    icon: 'line-md:document-report',
    requiresPrivileges: ['ViewContainerMonitoringReport'],
}

const ReportMap = {
    VehicleDetail,
    CountSupervision,
    CountConveyor,
    VehicleViolation,
    FireEvent,
    FireworkInspectionStatistics25,
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

export const reportVerticalNav = createNavItemFactory(ReportMap, ReportParent)

export default [
    {
        ...createNavItemFactory(ReportMap, ReportParent),
    },
]
