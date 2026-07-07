/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

export const eventsParent = {
    title: 'route.common.events',
    icon: 'bi:calendar2-event',
    tagVariant: 'light-warning',
}

export const allEvents = {
    title: 'route.breadcrumb.commonEvents',
    route: 'event-allEvents',
    icon: 'line-md:alert-twotone-loop',
    requiresPrivileges: ['ViewEvent', 'PostEvent'],
}

export const vehicleEvent = {
    title: 'route.breadcrumb.vehicleEvent',
    route: 'event-vehicleEvent',
    icon: 'tdesign:vehicle',
    requiresPrivileges: ['ViewVehicleEvent'],
}

export const fireWorkEvent = {
    title: 'route.breadcrumb.fireWorkEvent',
    route: 'event-fireWorkEvent-list',
    icon: 'charm:rocket',
    requiresPrivileges: ['ViewFireWorkEvent'],
}

export const fireEvent = {
    title: 'route.breadcrumb.fireEvent',
    route: 'event-fireEvent',
    icon: 'nimbus:fire',
    requiresPrivileges: ['ViewFireEvent'],
}

export const peopleCountEvent = {
    title: 'route.breadcrumb.peopleCountEvent',
    route: 'event-peopleCountEvent-list',
    icon: 'gridicons:multiple-users',
    requiresPrivileges: ['ViewPeopleCountEvent'],
}

export const waterEvent = {
    title: 'route.breadcrumb.waterEvent',
    route: 'event-waterEvent',
    icon: 'icon-park-twotone:water-level',
    requiresPrivileges: ['ViewWaterEvent'],
}

export const manufacturingEvent = {
    title: 'Sản lượng',
    route: 'event-manufacturing',
    icon: 'material-symbols-light:manufacturing-rounded',
    requiresPrivileges: ['ViewManufacturing'],
}

export const virtualFenceEvent = {
    title: 'route.breadcrumb.virtualFenceEvent',
    route: 'event-virtualFenceEvent',
    icon: 'iconoir:prohibition',
    requiresPrivileges: ['ViewVirtualFenceEvent'],
}

export const faceEvent = {
    title: 'route.breadcrumb.faceEvent',
    route: 'event-faceEvent',
    icon: 'gridicons:multiple-users',
    requiresPrivileges: ['ViewFaceEvent'],
}

export const faceGateEvent = {
    title: 'route.breadcrumb.faceGateEvent',
    route: 'event-faceGateEvent',
    icon: 'gridicons:multiple-users',
    requiresPrivileges: ['ViewFaceGateEvent'],
}

export const workAtHeightMonitoring = {
    code: 'manage_workAtHeightMonitoring',
    title: 'route.breadcrumb.workAtHeightMonitoring',
    route: 'event-workAtHeightMonitoring',
    icon: 'mdi:ladder',
    requiresPrivileges: ['ViewWorkAtHeightMonitoring'],
}

export const peopleCountInOutEvent = {
    code: 'manage_peopleCountInOutEvent',
    title: 'route.breadcrumb.peopleCountInOutEvent',
    route: 'event-peopleCountInOutEvent',
    icon: 'gridicons:multiple-users',
    requiresPrivileges: ['ViewPeopleCountInOutEvent'],
}

export const protectiveEquipmentEvent = {
    code: 'manage_protectiveEquipmentEvent',
    title: 'route.breadcrumb.protectiveEquipmentEvent',
    route: 'event-protectiveEquipmentEvent',
    icon: 'streamline:user-protection-2',
    requiresPrivileges: ['ViewProtectiveEquipmentEvent'],
}

export const carEvent = {
    code: 'manage_CarEvent',
    title: 'route.breadcrumb.CarEvent',
    route: 'event-carEvent',
    icon: 'streamline:shipping-truck',
    requiresPrivileges: ['ViewCarEvent'],
}

// export const problemEvent = {
//     code: 'manage_problem',
//     title: 'route.breadcrumb.Problem',
//     route: 'event-problem',
//     icon: 'streamline:shipping-truck',
//     requiresPrivileges: ['ViewProblem'],
// }

export const conveyorEvent = {
    code: 'manage_conveyorEvent',
    title: 'route.breadcrumb.conveyorEvent',
    route: 'event-conveyorEvent',
    icon: 'mynaui:briefcase-conveyor-belt',
    requiresPrivileges: ['ViewConveyorEvent'],
}

export const trafficViolationEvent = {
    title: 'route.breadcrumb.trafficViolationEvent',
    route: 'event-trafficViolationEvent',
    icon: 'material-symbols:traffic-outline',
    requiresPrivileges: ['ViewTrafficViolationEvent'],
}

export const restrictedZonesEvent = {
    title: 'route.breadcrumb.restrictedZonesEvent',
    route: 'event-restrictedZonesEvent',
    icon: 'iconoir:prohibition',
    requiresPrivileges: ['ViewRestrictedZonesEvent'],
}

export const vehicleSessionsEvent = {
    title: 'route.breadcrumb.vehicleSessionsEvent',
    route: 'event-vehicleSessionsEvent',
    icon: 'iconoir:prohibition',
    requiresPrivileges: ['ViewVehicleSessionsEvent'],
}

export const productionLineEvent = {
    title: 'route.breadcrumb.productionLineEvent',
    route: 'event-productionLineEvent',
    icon: 'material-symbols:factory',
    requiresPrivileges: ['ViewProductionLineEvent'],
}
const eventsMap = {
    allEvents,
    vehicleEvent,
    fireWorkEvent,
    fireEvent,
    peopleCountEvent,
    waterEvent,
    virtualFenceEvent,
    faceEvent,
    faceGateEvent,
    protectiveEquipmentEvent,
    carEvent,
    //problemEvent,
    conveyorEvent,
    trafficViolationEvent,
    restrictedZonesEvent,
    vehicleSessionsEvent,
    workAtHeightMonitoring,
    peopleCountInOutEvent,
    productionLineEvent,
}

export const eventVerticalNav = createNavItemFactory(eventsMap, eventsParent)

export default [
    {
        ...createNavItemFactory(eventsMap, eventsParent),
    },
]
