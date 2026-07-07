/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'


export const issueParent = {
    title: 'route.common.issue',
    icon: 'hugeicons:laptop-issue',
    tagVariant: 'light-warning'
}


export const issue = {
    title: 'route.breadcrumb.issue',
    route: 'issue',
    icon: 'ant-design:issues-close-outlined',
    requiresPrivileges: ['ViewIssue', 'ManageIssue', 'RemoveIssue'],
}

export const projects = {
    title: 'route.breadcrumb.projects',
    route: 'issue-projects',
    icon: 'material-symbols:assignment',
    requiresPrivileges: ['ViewProjects', 'ManageProjects'],
}

export const priority = {
    title: 'route.breadcrumb.priority',
    route: 'issue-priority',
    icon: 'material-symbols:stacks',
    requiresPrivileges: ['ViewPriority', 'ManagePriority'],
}

export const statuses = {
    title: 'route.breadcrumb.statuses',
    route: 'issue-statuses',
    icon: 'material-symbols:person-alert',
    requiresPrivileges: ['ViewStatuses', 'ManageStatuses'],
}

export const issueType = {
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

export const issueVerticalNav = createNavItemFactory(issueMap, issueParent)

export default [
    {
        ...createNavItemFactory(issueMap, issueParent),
    },
]