/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

export const systemParent = {
    title: 'route.common.systemManagement',
    icon: 'carbon:gui-management',
    tagVariant: 'light-warning',
}

// Users component
export const Users = {
    title: 'route.breadcrumb.users',
    route: 'systems-users',
    icon: 'el:user',
    requiresPrivileges: ['ViewUser', 'ManageUser'],
}

// Roles component
export const Roles = {
    title: 'route.breadcrumb.roles',
    route: 'systems-roles',
    icon: 'line-md:account',
    requiresPrivileges: ['ViewRole', 'ManageRole'],
}

// Notification Templates component
export const NotificationTemplates = {
    title: 'route.breadcrumb.notificationTemplates',
    route: 'notification_event_template',
    icon: 'solar:notification-unread-lines-broken',
    requiresPrivileges: ['ViewEventTemplate', 'ManageEventTemplate'],
}

// Companies component
export const Companies = {
    title: 'route.breadcrumb.companies',
    route: 'systems-company',
    icon: 'solar:buildings-2-outline',
    requiresPrivileges: ['ViewCompany', 'ManageCompany'],
}

export const AuditLogs = {
    title: 'route.breadcrumb.auditlogs',
    route: 'systems-audit-logs',
    icon: 'material-symbols:history-rounded',
    requiresPrivileges: ['ViewAuditLogs', 'ManageAuditLogs'],
}
const systemMap = {
    NotificationTemplates,
    Users,
    Roles,
    Companies,
    AuditLogs,
}

export const systemVerticalNav = createNavItemFactory(systemMap, systemParent)

export default [
    {
        ...createNavItemFactory(systemMap, systemParent),
    },
]
