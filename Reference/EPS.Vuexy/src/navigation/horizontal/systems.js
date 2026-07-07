/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

// Users component
const Users = {
    code: 'manage_users',
    name: 'Nav.Users',
    title: 'route.breadcrumb.users',
    route: 'systems-users-list',
    icon: 'el:user',
    requiresPrivileges: ['ViewUser', 'ManageUser'],
}

// Roles component
const Roles = {
    code: 'manage_roles',
    name: 'Nav.Roles',
    title: 'route.breadcrumb.roles',
    route: 'systems-roles-list',
    icon: 'el:group',
    requiresPrivileges: ['ViewRole', 'ManageRole'],
}

// Notification Templates component
const NotificationTemplates = {
    code: 'manage_notification_event_template',
    name: 'Nav.NotificationEventTemplate',
    title: 'route.breadcrumb.notificationTemplates',
    route: 'notification_event_template-list',
    icon: 'solar:notification-unread-lines-broken',
    requiresPrivileges: ['ViewEventTemplate', 'ManageEventTemplate'],
}

// Companies component
const Companies = {
    code: 'manage_companies',
    name: 'Nav.Company',
    title: 'route.breadcrumb.companies',
    route: 'systems-company',
    icon: 'solar:buildings-2-outline',
    requiresPrivileges: ['ViewCompany', 'ManageCompany'],
}

export const systemParent = {
    code: 'system',
    name: 'Nav.SystemManagement',
    header: 'route.common.systemManagement',
    icon: 'carbon:gui-management',
}

export const AuditLogs = {
    code: 'manage_auditlogs',
    name: 'Nav.AuditLogs',
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

export const systemHorizontalNav = createNavItemFactory(systemMap, systemParent)

export default [
    {
        ...createNavItemFactory(systemMap, systemParent),
    },
]
