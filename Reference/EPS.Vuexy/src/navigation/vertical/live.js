/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'


export const liveParent = {
      title: 'route.common.live',
        icon: 'hugeicons:security-lock',
        tagVariant: 'light-warning'}


export const live = {
    title: 'route.breadcrumb.live',
    route: 'live',
    icon: 'solar:screencast-broken',
    requiresPrivileges: ['ViewLive', 'IdentificationZone'],
}

export const liveTLMBF = {
    title: 'route.breadcrumb.live',
    route: 'liveTLMBF',
    icon: 'solar:screencast-broken',
    requiresPrivileges: ['ViewLive', 'IdentificationZone'],
}

export const attendanceMonitor = {
     title: 'route.breadcrumb.attendanceMonitor',
    route: 'attendanceMonitor',
    icon: 'solar:screencast-broken',
    requiresPrivileges: ['ViewLive', 'IdentificationZone'],
}

export const liveView = {
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

export const liveVerticalNav = createNavItemFactory(liveMap, liveParent)

export default [
    {
        ...createNavItemFactory(liveMap, liveParent),
    },
]