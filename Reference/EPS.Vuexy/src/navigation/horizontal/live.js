/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

export const liveParent = {
    code: 'live',
    name: 'Nav.Live',
    header: 'route.common.live',
    icon: 'hugeicons:security-lock',
}

export const live = {
    code: 'live',
    name: 'Nav.Live',
    title: 'route.breadcrumb.live',
    route: 'live',
    icon: 'solar:screencast-broken',
    requiresPrivileges: ['ViewLive', 'IdentificationZone'],
}

export const liveTLMBF = {
    code: 'liveTLMBF',
    name: 'Nav.Live',
    title: 'route.breadcrumb.live',
    route: 'liveTLMBF',
    icon: 'solar:screencast-broken',
    requiresPrivileges: ['ViewLive', 'IdentificationZone'],
}

export const attendanceMonitor = {
    code: 'attendanceMonitor',
    name: 'Nav.attendanceMonitor',
    title: 'route.breadcrumb.attendanceMonitor',
    route: 'attendanceMonitor',
    icon: 'solar:screencast-broken',
    requiresPrivileges: ['ViewLive', 'IdentificationZone'],
}

export const liveView = {
    code: 'liveView',
    name: 'Nav.Live',
    title: 'route.breadcrumb.liveVehicle',
    route: 'liveView',
    icon: 'solar:screencast-broken',
    requiresPrivileges: ['ViewLiveVehicle'],
}

export const liveMap = {
    live,
    liveTLMBF,
    attendanceMonitor,
    liveView
}
export const liveHorizontalNav = createNavItemFactory(liveMap, liveParent)

export default [
    {
        ...createNavItemFactory(liveMap, liveParent),
    },
]
