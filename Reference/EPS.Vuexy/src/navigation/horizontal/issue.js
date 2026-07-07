/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

export const issueParent = {
    code: 'issue',
    name: 'Nav.Issue',
    header: 'route.common.issue',
    icon: 'hugeicons:laptop-issue',
}

export const issue = {
    code: 'issue',
    name: 'Nav.Issue',
    title: 'route.breadcrumb.issue',
    route: 'issue',
    icon: 'ant-design:issues-close-outlined',
    requiresPrivileges: ['ViewIssue', 'ManageIssue', 'RemoveIssue'],
}

export const projects = {
    code: 'manage_projects',
    name: 'route.breadcrumb.projects',
    title: 'route.breadcrumb.projects',
    route: 'issue-projects',
    icon: 'material-symbols:assignment',
    requiresPrivileges: ['ViewProjects', 'ManageProjects'],
}

export const priority = {
    code: 'manage_priority',
    name: 'route.breadcrumb.priority',
    title: 'route.breadcrumb.priority',
    route: 'issue-priority',
    icon: 'material-symbols:stacks',
    requiresPrivileges: ['ViewPriority', 'ManagePriority'],
}

export const statuses = {
    code: 'manage_statuses',
    name: 'route.breadcrumb.statuses',
    title: 'route.breadcrumb.statuses',
    route: 'issue-statuses',
    icon: 'material-symbols:person-alert',
    requiresPrivileges: ['ViewStatuses', 'ManageStatuses'],
}

export const issueType = {
    code: 'manage_issueType',
    name: 'route.breadcrumb.issueType',
    title: 'route.breadcrumb.issueType',
    route: 'issue-issueTypes',
    icon: 'material-symbols:problem',
    requiresPrivileges: ['ViewIssueType', 'ManageIssueType'],
}

export const issueMap = {
    issue,
    projects,
    priority,
    statuses,
    issueType
}
export const issueHorizontalNav = createNavItemFactory(issueMap, issueParent)

export default [
    {
        ...createNavItemFactory(issueMap, issueParent),
    },
]
