/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

export const dashboardParent = {
    code: 'dashboard',
    header: 'route.common.dashboard',
    name: 'Nav.Dashboard',
    icon: 'carbon:dashboard',
}

export const systemDashboard = {
    code: 'dashboard_system',
    name: 'Nav.SystemDashboard',
    title: 'route.breadcrumb.dashboardSystem',
    requiresPrivileges: ['ViewSystemDashboard'],
    icon: 'solar:cpu-bold',
    route: 'dashboard-system-info',
    url: 'https://kibana.atin.vn/app/dashboards#/view/b125f5ad-c16b-48e5-a9c9-9d62e7786e07?embed=true&_g=(refreshInterval%3A(pause%3A!t%2Cvalue%3A60000)%2Ctime%3A(from%3Anow-15m%2Cto%3Anow))&show-query-input=true&show-time-filter=true&height=3000px',
}

export const fireworkDashboard = {
    code: 'dashboard_firework',
    name: 'Nav.FireworkDashboard',
    title: 'route.breadcrumb.dashboardFirework',
    requiresPrivileges: ['ViewFireworkDashboard'],
    icon: 'hugeicons:product-loading',
    route: 'dashboard-firework-info',
}

export const mapDashboard = {
    code: 'dashboard_map',
    name: 'Nav.MapDashboard',
    title: 'route.breadcrumb.dashboardMap',
    requiresPrivileges: ['ViewMapDashboard'],
    icon: 'ic:round-map',
    route: 'dashboard-map-dashboard-events',
}

export const eventsDashboard = {
    code: 'dashboard_events',
    name: 'Nav.EventDashboard',
    title: 'route.breadcrumb.dashboardEvents',
    requiresPrivileges: ['ViewEventsDashboard'],
    icon: 'bi:calendar2-event',
    route: 'dashboard-events',
}

export const itineraryDashboard = {
    code: 'dashboard_itinerary',
    name: 'Nav.DashboardItinerary',
    title: 'route.breadcrumb.dashboardItinerary',
    requiresPrivileges: ['ViewDashboardItinerary'],
    icon: 'bi:calendar2-event',
    route: 'dashboard-itinerary',
}
export const meikoDashboard = {
    code: 'dashboard_smart_factory',
    name: 'Nav.DashboardMeiko',
    title: 'route.breadcrumb.dashboardMeiko',
    requiresPrivileges: ['ViewDashboardMeiko'],
    icon: 'bi:calendar2-event',
    route: 'dashboard-smart-factory',
}
export const monitorSopDes = {
    code: 'monitor-sop-des',
    name: 'Nav.MonitorSopDes',
    title: 'route.breadcrumb.monitorSopDes',
    requiresPrivileges: ['ViewMonitorSOP'],
    icon: 'bi:calendar2-event',
    route: 'monitor-sop-des',
}

export const hoaphatMapDashboard = {
    code: 'dashboard_hoaphat',
    name: 'Nav.DashboardMapHoaPhat',
    title: 'route.breadcrumb.dashboardMapHoaPhat',
    requiresPrivileges: ['ViewMapDashboardHoaPhat'],
    icon: 'ic:round-map',
    route: 'dashboard-map-dashboard-events-hp',
}

export const hoaphatEventDashboard = {
    code: 'dashboard_hoaphat',
    name: 'Nav.DashboardEventsHoaPhat',
    title: 'route.breadcrumb.dashboardEventsHoaPhat',
    requiresPrivileges: ['ViewEventsDashboardHoaPhat'],
    icon: 'bi:calendar2-event',
    route: 'dashboard-events-hp',
}

export const novalandMapDashboard = {
    code: 'dashboard_novaland',
    name: 'Nav.DashboardMapNovaland',
    title: 'route.breadcrumb.dashboardMapNovaland',
    requiresPrivileges: ['ViewMapDashboardNovaland'],
    icon: 'ic:round-map',
    route: 'dashboard-map-dashboard-events-nvl',
}

export const novalandEventDashboard = {
    code: 'dashboard_novaland',
    name: 'Nav.DashboardEventsNovaland',
    title: 'route.breadcrumb.dashboardEventsNovaland',
    requiresPrivileges: ['ViewEventsDashboardNovaland'],
    icon: 'bi:calendar2-event',
    route: 'dashboard-events-nvl',
}

export const nghisonEventDashboard = {
    code: 'dashboard_nghison',
    name: 'Nav.DashboardEventsNghiSon',
    title: 'route.breadcrumb.dashboardEventsNghiSon',
    requiresPrivileges: ['ViewEventsDashboardNghiSon'],
    icon: 'bi:calendar2-event',
    route: 'dashboard-events-ns',
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

export const dashboardHorizontalNav = createNavItemFactory(
    dashboardMap,
    dashboardParent
)

export default [
    {
        ...createNavItemFactory(dashboardMap, dashboardParent),
    },
]
