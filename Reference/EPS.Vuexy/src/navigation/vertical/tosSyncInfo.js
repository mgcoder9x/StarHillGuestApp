/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'


export const tosSyncInfoParent = {
    title: 'route.common.tosSyncInfo',
    icon: 'line-md:clipboard-list',
    tagVariant: 'light-warning'}


export const tosSyncInfo = {
    title: 'route.breadcrumb.tosSyncInfo',
    route: 'tosSyncInfo',
    icon: 'line-md:clipboard-list',
    requiresPrivileges: ['ViewTosSyncInfo'],
}

export const tosSyncInfoMap = {
    tosSyncInfo
}

export const tosSyncInfoVerticalNav = createNavItemFactory(tosSyncInfoMap, tosSyncInfoParent)

export default [
    {
        ...createNavItemFactory(tosSyncInfoMap, tosSyncInfoParent),
    },
]