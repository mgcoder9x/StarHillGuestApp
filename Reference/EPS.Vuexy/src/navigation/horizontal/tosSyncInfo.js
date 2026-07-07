/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

export const tosSyncInfoParent = {
    code: 'tosSyncInfo',
    name: 'Nav.TosSyncInfo',
    header: 'route.common.tosSyncInfo',
    icon: 'line-md:clipboard-list',
}

export const tosSyncInfo = {
    code: 'tosSyncInfo',
    name: 'Nav.TosSyncInfo',
    title: 'route.breadcrumb.tosSyncInfo',
    route: 'tosSyncInfo',
    icon: 'line-md:clipboard-list',
    requiresPrivileges: ['ViewTosSyncInfo'],
}

export const tosSyncInfoMap = {
    tosSyncInfo,
}
export const tosSyncInfoHorizontalNav = createNavItemFactory(tosSyncInfoMap, tosSyncInfoParent)

export default [
    {
        ...createNavItemFactory(tosSyncInfoMap, tosSyncInfoParent),
    },
]
