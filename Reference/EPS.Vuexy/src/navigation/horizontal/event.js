/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

export const eventsParent = {
    code: 'event',
    name: 'Nav.EventManagement',
    header: 'route.common.events',
    icon: 'bi:calendar2-event',
}

export const allEvents = {
    code: 'manage_event',
    name: 'Nav.Event',
    title: 'route.breadcrumb.events',
    route: 'event-list',
    icon: 'line-md:alert-twotone-loop',
    requiresPrivileges: ['ViewEvent', 'PostEvent'],
}

export const vehicleEvent = {
    code: 'manage_vehicleEvent',
    name: 'Nav.VehicleEvent',
    title: 'route.breadcrumb.vehicleEvent',
    route: 'event-vehicleEvent-list',
    icon: 'tdesign:vehicle',
    requiresPrivileges: ['ViewVehicleEvent'],
}

export const fireworkEvent = {
    code: 'manage_fireworkEvent',
    name: 'Nav.FireWorkEvent',
    title: 'route.breadcrumb.fireWorkEvent',
    route: 'event-fireWorkEvent-list',
    icon: 'charm:rocket',
    requiresPrivileges: ['ViewFireWorkEvent'],
}

export const fireEvent = {
    code: 'manage_fireEvent',
    name: 'Nav.FireEvent',
    title: 'route.breadcrumb.fireEvent',
    route: 'event-fireEvent-list',
    icon: 'nimbus:fire',
    requiresPrivileges: ['ViewFireEvent'],
}

export const peopleCountEvent = {
    code: 'manage_peopleCountEvent',
    name: 'Nav.PeopleCountEvent',
    title: 'route.breadcrumb.peopleCountEvent',
    route: 'event-peopleCountEvent-list',
    icon: 'gridicons:multiple-users',
    requiresPrivileges: ['ViewPeopleCountEvent'],
}

export const waterEvent = {
    code: 'manage_waterEvent',
    name: 'Nav.WaterEvent',
    title: 'route.breadcrumb.waterEvent',
    route: 'event-waterEvent-list',
    icon: 'icon-park-twotone:water-level',
    requiresPrivileges: ['ViewWaterEvent'],
}

export const manufacturingEvent = {
    code: 'manage_eventManufacturing',
    name: 'Nav.Manufacturing',
    title: 'Sản lượng',
    route: 'event-manufacturing',
    icon: 'material-symbols-light:manufacturing-rounded',
    requiresPrivileges: ['ViewManufacturing'],
}

export const virtualFenceEvent = {
    code: 'manage_virtualFenceEvent',
    name: 'Nav.VirtualFenceEvent',
    title: 'route.breadcrumb.virtualFenceEvent',
    route: 'event-virtualFenceEvent',
    icon: 'iconoir:prohibition',
    requiresPrivileges: ['ViewVirtualFenceEvent'],
}

export const faceEvent = {
    code: 'manage_eventFace',
    name: 'Nav.FaceEvent',
    title: 'route.breadcrumb.faceEvent',
    route: 'event-faceEvent',
    icon: 'gridicons:multiple-users',
    requiresPrivileges: ['ViewFaceEvent'],
}

export const faceGateEvent = {
    code: 'manage_eventGateFace',
    name: 'Nav.FaceGateEvent',
    title: 'route.breadcrumb.faceGateEvent',
    route: 'event-faceGateEvent',
    icon: 'gridicons:multiple-users',
    requiresPrivileges: ['ViewFaceGateEvent'],
}

export const workAtHeightMonitoring = {
    code: 'manage_workAtHeightMonitoring',
    name: 'Nav.WorkAtHeightMonitoring',
    title: 'route.breadcrumb.workAtHeightMonitoring',
    route: 'event-workAtHeightMonitoring',
    icon: 'mdi:ladder',
    requiresPrivileges: ['ViewWorkAtHeightMonitoring'],
}

export const peopleCountInOutEvent = {
    code: 'manage_peopleCountInOutEvent',
    name: 'Nav.PeopleCountInOutEvent',
    title: 'route.breadcrumb.peopleCountInOutEvent',
    route: 'event-peopleCountInOutEvent',
    icon: 'gridicons:multiple-users',
    requiresPrivileges: ['ViewPeopleCountInOutEvent'],
}

export const carEvent = {
    code: 'manage_carEvent',
    name: 'Nav.CarEvent',
    title: 'route.breadcrumb.CarEvent',
    route: 'event-carEvent',
    icon: 'gridicons:multiple-users',
    requiresPrivileges: ['ViewCarEvent', 'ManageCarEvent'],
}

// export const problemEvent = {
//     code: 'manage_problem',
//     name: 'Nav.Problem',
//     title: 'route.breadcrumb.Problem',
//     route: 'event-problem',
//     icon: 'gridicons:multiple-users',
//     requiresPrivileges: ['ViewProblem', 'ManageProblem'],
// }

export const protectiveEquipmentEvent = {
    code: 'manage_protectiveEquipmentEvent',
    name: 'Nav.ProtectiveEquipmentEvent',
    title: 'route.breadcrumb.protectiveEquipmentEvent',
    route: 'event-protectiveEquipmentEvent',
    icon: 'streamline:user-protection-2',
    requiresPrivileges: ['ViewProtectiveEquipmentEvent'],
}

export const conveyorEvent = {
    code: 'manage_conveyorEvent',
    name: 'Nav.ConveyorEvent',
    title: 'route.breadcrumb.conveyorEvent',
    route: 'event-conveyorEvent',
    icon: 'mynaui:briefcase-conveyor-belt',
    requiresPrivileges: ['ViewConveyorEvent'],
}

export const trafficViolationEvent = {
    code: 'manage_trafficViolationEvent',
    name: 'Nav.TrafficViolationEvent',
    title: 'route.breadcrumb.trafficViolationEvent',
    route: 'event-trafficViolationEvent',
    icon: 'material-symbols:traffic-outline',
    requiresPrivileges: ['ViewTrafficViolationEvent'],
}

export const restrictedZonesEvent = {
    code: 'manage_restrictedZonesEvent',
    name: 'Nav.restrictedZonesEvent',
    title: 'route.breadcrumb.restrictedZonesEvent',
    route: 'event-restrictedZonesEvent',
    icon: 'iconoir:prohibition',
    requiresPrivileges: ['ViewRestrictedZonesEvent'],
}

export const vehicleSessionsEvent = {
    code: 'manage_vehicleSessionsEvent',
    name: 'Nav.vehicleSessionsEvent',
    title: 'route.breadcrumb.vehicleSessionsEvent',
    route: 'event-vehicleSessionsEvent',
    icon: 'iconoir:prohibition',
    requiresPrivileges: ['ViewVehicleSessionsEvent'],
}
export const productionLineEvent = {
    code: 'manage_productionLineEvent',
    name: 'Nav.productionLineEvent',
    title: 'route.breadcrumb.productionLineEvent',
    route: 'event-productionLineEvent',
    icon: 'material-symbols:factory',
    requiresPrivileges: ['ViewProductionLineEvent'],
}
const eventsMap = {
    fireworkEvent,
    peopleCountEvent,
    waterEvent,
    manufacturingEvent,
    carEvent,
    //problemEvent,
    allEvents,
    vehicleEvent,
    fireEvent,
    virtualFenceEvent,
    faceEvent,
    faceGateEvent,
    protectiveEquipmentEvent,
    conveyorEvent,
    trafficViolationEvent,
    restrictedZonesEvent,
    vehicleSessionsEvent,
    workAtHeightMonitoring,
    peopleCountInOutEvent,
    productionLineEvent,
}

export const eventHorizontalNav = createNavItemFactory(eventsMap, eventsParent)

export default [
    {
        ...createNavItemFactory(eventsMap, eventsParent),
    },
]
