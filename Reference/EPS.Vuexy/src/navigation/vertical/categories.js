/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

// ✅ Export từng category riêng biệt
export const areas = {
    title: 'route.breadcrumb.areas',
    route: 'categories-areas',
    icon: 'line-md:map-marker-alt',
    requiresPrivileges: ['ViewArea', 'ManageArea'],
}

export const devices = {
    title: 'route.breadcrumb.devices',
    route: 'categories-devices',
    icon: 'lucide:cctv',
    requiresPrivileges: ['ViewDevice', 'ManageDevice'],
}

export const departments = {
    title: 'route.breadcrumb.departments',
    route: 'categories-departments',
    icon: 'mingcute:department-line',
    requiresPrivileges: ['ViewDepartment', 'ManageDepartment'],
}

export const contractors = {
    title: 'route.breadcrumb.contractors',
    route: 'categories-contractors',
    icon: 'mdi:account-hard-hat',
    requiresPrivileges: ['ViewContractor', 'ManageContractor'],
}

export const groups = {
    title: 'route.breadcrumb.groups',
    route: 'categories-groups',
    icon: 'oui:app-users-roles',
    requiresPrivileges: ['ViewGroup', 'ManageGroup'],
}

export const machines = {
    title: 'route.breadcrumb.machines',
    route: 'categories-machines',
    icon: 'mingcute:device-line',
    requiresPrivileges: ['ViewMachine', 'ManageMachine'],
}

export const timeAccesses = {
    title: 'route.breadcrumb.timeAccesses',
    route: 'categories-timeAccesses',
    icon: 'material-symbols:avg-time-outline',
    requiresPrivileges: ['ViewTimeAccess', 'ManageTimeAccess'],
}

export const groupAccesses = {
    title: 'route.breadcrumb.groupAccesses',
    route: 'categories-groupAccesses',
    icon: 'icon-park-outline:permissions',
    requiresPrivileges: ['ViewGroupAccess', 'ManageGroupAccess'],
}

export const waterWarning = {
    title: 'route.breadcrumb.waterWarning',
    route: 'water-warning',
    icon: 'ant-design:warning-outlined',
    requiresPrivileges: ['ViewWaterWarning', 'ManageWaterWarning'],
}

export const vehicles = {
    title: 'route.breadcrumb.vehicles',
    route: 'categories-vehicles',
    icon: 'tdesign:vehicle',
    requiresPrivileges: ['ViewVehicle', 'ManageVehicle'],
}

export const employees = {
    title: 'route.breadcrumb.employees',
    route: 'categories-employees',
    icon: 'clarity:employee-group-line',
    requiresPrivileges: ['ViewEmployee', 'ManageEmployee'],
}

export const blackLists = {
    title: 'route.breadcrumb.blackLists',
    route: 'categories-blackLists',
    icon: 'clarity:employee-group-line',
    requiresPrivileges: ['ViewBlackList', 'ManageBlackList'],
}

export const guests = {
    title: 'route.breadcrumb.guest',
    route: 'categories-guess',
    icon: 'clarity:employee-group-line',
    requiresPrivileges: ['ViewGuess', 'ManageGuess'],
}

export const vehicleCards = {
    title: 'route.breadcrumb.vehicleCards',
    route: 'categories-vehicleCard',
    icon: 'ant-design:credit-card-outlined',
    requiresPrivileges: ['ViewVehicleCard', 'ManageVehicleCard'],
}

export const rfidConfigs = {
    title: 'route.breadcrumb.rfidConfigs',
    route: 'categories-rfidConfigs',
    // icon: 'ant-design:wifi-outlined',
    icon: 'clarity:radar-line',
    requiresPrivileges: ['ManageRFIDConfig', 'ViewRFIDConfig'],
}
export const controllers = {
    title: 'route.breadcrumb.controllers',
    route: 'categories-controllers',
    icon: 'clarity:terminal-line',
    requiresPrivileges: ['ManageController', 'ViewController'],
}

export const lane = {
    title: 'route.breadcrumb.lane',
    icon: 'quill:list',
    route: 'categories-lane',
    requiresPrivileges: ['ViewLane', 'ManageLane'],
}

export const categoriesParent = {
    title: 'route.common.categories',
    icon: 'quill:list',
    tagVariant: 'light-warning',
}

export const workingShifts = {
    title: 'route.breadcrumb.workingShifts',
    route: 'categories-workingShifts',
    icon: 'clarity:alarm-clock-line',
    requiresPrivileges: ['ViewWorkingShift', 'ManageWorkingShift'],
}
export const productionLines = {
    title: 'route.breadcrumb.productionLines',
    route: 'categories-productionLines',
    icon: 'clarity:connect-line',
    requiresPrivileges: ['ViewProductionLine', 'ManageProductionLine'],
}
export const workFlows = {
    title: 'route.breadcrumb.workFlows',
    route: 'categories-workFlows',
    icon: 'clarity:flow-chart-line',
    requiresPrivileges: ['ViewWorkFlow', 'ManageWorkFlow'],
}
export const eventTypes = {
    title: 'route.breadcrumb.eventTypes',
    route: 'categories-event-type',
    icon: 'mdi:format-list-bulleted-type',
    requiresPrivileges: ['ViewEventType', 'ManageEventType'],
}
export const eventWarningLevel = {
    title: 'route.breadcrumb.eventWarningLevel',
    route: 'categories-event-warning-level',
    icon: 'mdi:shield-alert-outline',
    requiresPrivileges: ['ViewEventWarningLevel', 'ManageEventWarningLevel'],
}
const categoriesMap = {
    areas,
    devices,
    departments,
    contractors,
    groups,
    machines,
    timeAccesses,
    groupAccesses,
    waterWarning,
    vehicles,
    employees,
    guests,
    lane,
    vehicleCards,
    rfidConfigs,
    controllers,
    blackLists,
    workingShifts,
    // productionLines,
    workFlows,
    eventTypes,
    eventWarningLevel,
}

export const categoriesVerticalNav = createNavItemFactory(
    categoriesMap,
    categoriesParent
)

export default [
    {
        ...createNavItemFactory(categoriesMap, categoriesParent),
    },
]
