/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

export const dashboardParent = {
    title: 'route.common.dashboard',
    icon: 'carbon:dashboard',
    tagVariant: 'light-warning',
}

export const systemDashboard = {
    icon: 'solar:cpu-bold',
    title: 'route.breadcrumb.dashboardSystem',
    route: 'dashboard-system-info',
    requiresPrivileges: ['ViewSystemDashboard'],
}

export const fireworkDashboard = {
    icon: 'hugeicons:product-loading',
    title: 'route.breadcrumb.dashboardFirework',
    route: 'dashboard-firework-info',
    requiresPrivileges: ['ViewFireworkDashboard'],
}

export const mapDashboard = {
    icon: 'ic:round-map',
    title: 'route.breadcrumb.dashboardMap',
    route: 'dashboard-map-dashboard-events',
    requiresPrivileges: ['ViewMapDashboard'],
}

export const eventsDashboard = {
    icon: 'bi:calendar2-event',
    title: 'route.breadcrumb.dashboardEvents',
    route: 'dashboard-events',
    requiresPrivileges: ['ViewEventsDashboard'],
}

export const itineraryDashboard = {
    icon: 'ic:round-map',
    title: 'route.breadcrumb.dashboardItinerary',
    route: 'dashboard-itinerary',
    requiresPrivileges: ['ViewDashboardItinerary'],
}
export const meikoDashboard = {
    icon: 'bi:calendar2-event',
    title: 'route.breadcrumb.dashboardMeiko',
    route: 'dashboard-smart-factory',
    requiresPrivileges: ['ViewDashboardMeiko'],
}
export const monitorSopDes = {
    icon: 'bi:calendar2-event',
    title: 'route.breadcrumb.monitorSopDes',
    route: 'monitor-sop-des',
    requiresPrivileges: ['ViewMonitorSOP'],
}

export const hoaphatMapDashboard = {
    icon: 'ic:round-map',
    title: 'route.breadcrumb.dashboardMapHoaPhat',
    route: 'dashboard-map-dashboard-events-hp',
    requiresPrivileges: ['ViewMapDashboardHoaPhat'],
}

export const hoaphatEventDashboard = {
    icon: 'bi:calendar2-event',
    title: 'route.breadcrumb.dashboardEventsHoaPhat',
    route: 'dashboard-events-hp',
    requiresPrivileges: ['ViewEventsDashboardHoaPhat'],
}

export const novalandMapDashboard = {
    icon: 'ic:round-map',
    title: 'route.breadcrumb.dashboardMapNovaland',
    route: 'dashboard-map-dashboard-events-nvl',
    requiresPrivileges: ['ViewMapDashboardNovaland'],
}

export const novalandEventDashboard = {
    icon: 'bi:calendar2-event',
    title: 'route.breadcrumb.dashboardEventsNovaland',
    route: 'dashboard-events-nvl',
    requiresPrivileges: ['ViewEventsDashboardNovaland'],
}

export const nghisonEventDashboard = {
    icon: 'bi:calendar2-event',
    title: 'route.breadcrumb.dashboardEventsNghiSon',
    route: 'dashboard-events-ns',
    requiresPrivileges: ['ViewEventsDashboardNghiSon'],
}
const dashboardMap = {
    systemDashboard,
    fireworkDashboard,
    itineraryDashboard,
    mapDashboard,
    eventsDashboard,
    meikoDashboard,
    monitorSopDes,
    hoaphatMapDashboard,
    hoaphatEventDashboard,
    novalandMapDashboard,
    novalandEventDashboard,
    nghisonEventDashboard,
}

export const dashboardVerticalNav = createNavItemFactory(
    dashboardMap,
    dashboardParent
)

export default [
    {
        ...createNavItemFactory(dashboardMap, dashboardParent),
    },
]
