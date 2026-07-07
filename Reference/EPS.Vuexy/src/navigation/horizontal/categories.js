/* eslint-disable */

import { createNavItemFactory } from '../navigation-factory'

export const areas = {
    code: 'manage_areas',
    name: 'Nav.Areas',
    title: 'route.breadcrumb.areas',
    route: 'categories-areas-list',
    icon: 'line-md:map-marker-alt',
    requiresPrivileges: ['ViewArea', 'ManageArea'],
}

export const devices = {
    code: 'manage_devices',
    name: 'Nav.Devices',
    title: 'route.breadcrumb.devices',
    route: 'categories-devices-list',
    icon: 'lucide:cctv',
    requiresPrivileges: ['ViewDevice', 'ManageDevice'],
}

export const machine = {
    code: 'manage_machine',
    name: 'Nav.Machines',
    title: 'route.breadcrumb.machines',
    route: 'categories-machines-list',
    icon: 'mingcute:device-line',
    requiresPrivileges: ['ViewMachine', 'ManageMachine'],
}

export const departments = {
    code: 'manage_departments',
    name: 'route.breadcrumb.departments',
    title: 'route.breadcrumb.departments',
    route: 'categories-departments-list',
    icon: 'mingcute:department-line',
    requiresPrivileges: ['ViewDepartment', 'ManageDepartment'],
}

export const groups = {
    code: 'manage_groups',
    name: 'route.breadcrumb.groups',
    title: 'route.breadcrumb.groups',
    route: 'categories-groups',
    icon: 'oui:app-users-roles',
    requiresPrivileges: ['ViewGroup', 'ManageGroup'],
}

export const contractors = {
    code: 'manage_contractors',
    name: 'route.breadcrumb.contractors',
    title: 'route.breadcrumb.contractors',
    route: 'categories-contractors',
    icon: 'mdi:account-hard-hat',
    requiresPrivileges: ['ViewContractor', 'ManageContractor'],
}

export const timeAccesses = {
    code: 'manage_timeAccesses',
    name: 'route.breadcrumb.timeAccesses',
    title: 'route.breadcrumb.timeAccesses',
    route: 'categories-timeAccesses',
    icon: 'material-symbols:avg-time-outline',
    requiresPrivileges: ['ViewTimeAccess', 'ManageTimeAccess'],
}

export const groupAccesses = {
    code: 'manage_groupAccess',
    name: 'route.breadcrumb.groupAccesses',
    title: 'route.breadcrumb.groupAccesses',
    route: 'categories-groupAccesses',
    icon: 'icon-park-outline:permissions',
    requiresPrivileges: ['ViewGroupAccess', 'ManageGroupAccess'],
}

export const waterWarning = {
    code: 'manage_waterWarning',
    title: 'route.breadcrumb.waterWarning',
    name: 'route.breadcrumb.waterWarning',
    route: 'water-warning-list',
    icon: 'ant-design:warning-outlined',
    requiresPrivileges: ['ViewWaterWarning', 'ManageWaterWarning'],
}

export const vehicles = {
    code: 'manage_vehicles',
    name: 'route.breadcrumb.vehicles',
    title: 'route.breadcrumb.vehicles',
    route: 'categories-vehicles-list',
    icon: 'tdesign:vehicle',
    requiresPrivileges: ['ViewVehicle', 'ManageVehicle'],
}

export const employees = {
    code: 'manage_employee',
    name: 'route.breadcrumb.employees',
    title: 'route.breadcrumb.employees',
    route: 'categories-employees',
    icon: 'clarity:employee-group-line',
    requiresPrivileges: ['ViewEmployee', 'ManageEmployee'],
}

export const blackLists = {
    code: 'manage_blackList',
    name: 'route.breadcrumb.blackLists',
    title: 'route.breadcrumb.blackLists',
    route: 'categories-blackLists',
    icon: 'clarity:employee-group-line',
    requiresPrivileges: ['ViewBlackList', 'ManageBlackList'],
}

export const guests = {
    code: 'manage_guess',
    name: 'route.breadcrumb.guest',
    title: 'route.breadcrumb.guest',
    route: 'categories-guess',
    icon: 'clarity:employee-group-line',
    requiresPrivileges: ['ViewGuess', 'ManageGuess'],
}

export const vehicleCards = {
    code: 'manage_vehicleCard',
    name: 'route.breadcrumb.vehicleCards',
    title: 'route.breadcrumb.vehicleCards',
    route: 'categories-vehicleCard',
    icon: 'ant-design:credit-card-outlined',
    requiresPrivileges: ['ViewVehicleCard', 'ManageVehicleCard'],
}
export const rfidConfigs = {
    code: 'manage_rfidConfig',
    name: 'route.breadcrumb.rfidConfigs',
    title: 'route.breadcrumb.rfidConfigs',
    route: 'categories-rfidConfigs',
    // icon: 'ant-design:wifi-outlined',
    icon: 'clarity:radar-line',
    requiresPrivileges: ['ManageRFIDConfig', 'ViewRFIDConfig'],
}
export const controllers = {
    code: 'manage_controllers',
    name: 'route.breadcrumb.controllers',
    title: 'route.breadcrumb.controllers',
    route: 'categories-controllers',
    icon: 'clarity:terminal-line',
    requiresPrivileges: ['ManageController', 'ViewController'],
}

export const lane = {
    code: 'manage_lane',
    name: 'route.breadcrumb.lane',
    title: 'route.breadcrumb.lane',
    route: 'categories-lane',
    icon: 'clarity:employee-group-line',
    requiresPrivileges: ['ViewLane', 'ManageLane'],
}

export const categoriesParent = {
    code: 'category',
    name: 'route.common.categories',
    header: 'route.common.categories',
    icon: 'quill:list',
}
export const workingShifts = {
    code: 'manage_workingShift',
    name: 'route.breadcrumb.workingShifts',
    title: 'route.breadcrumb.workingShifts',
    route: 'categories-workingShifts',
    icon: 'clarity:alarm-clock-line',
    requiresPrivileges: ['ViewWorkingShift', 'ManageWorkingShift'],
}
export const productionLines = {
    code: 'manage_productionLine',
    name: 'route.breadcrumb.productionLines',
    title: 'route.breadcrumb.productionLines',
    route: 'categories-productionLines',
    icon: 'clarity:connect-line',
    requiresPrivileges: ['ViewProductionLine', 'ManageProductionLine'],
}
export const workFlows = {
    code: 'manage_workFlow',
    name: 'route.breadcrumb.workFlows',
    title: 'route.breadcrumb.workFlows',
    route: 'categories-workFlows',
    icon: 'clarity:flow-chart-line',
    requiresPrivileges: ['ViewWorkFlow', 'ManageWorkFlow'],
}
export const eventTypes = {
    code: 'manage_eventType',
    name: 'route.breadcrumb.eventTypes',
    title: 'route.breadcrumb.eventTypes',
    route: 'categories-event-type',
    icon: 'mdi:format-list-bulleted-type',
    requiresPrivileges: ['ViewEventType', 'ManageEventType'],
}
export const eventWarningLevel = {
    code: 'manage_eventWarningLevel',
    name: 'route.breadcrumb.eventWarningLevel',
    title: 'route.breadcrumb.eventWarningLevel',
    route: 'categories-event-warning-level',
    icon: 'mdi:shield-alert-outline',
    requiresPrivileges: ['ViewEventWarningLevel', 'ManageEventWarningLevel'],
}
const categoriesMap = {
    areas,
    devices,
    departments,
    groups,
    contractors,
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
    machine,
    blackLists,
    workingShifts,
    // productionLines,
    workFlows,
    eventTypes,
    eventWarningLevel,
}

export const categoriesHorizontalNav = createNavItemFactory(
    categoriesMap,
    categoriesParent
)

export default [
    {
        ...createNavItemFactory(categoriesMap, categoriesParent),
    },
]
