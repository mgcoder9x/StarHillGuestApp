/* eslint-disable */
export default [
    {
        title: 'route.common.test',
        icon: 'quill:list',
        tagVariant: 'light-warning',
        children: [
            {
                title: 'route.breadcrumb.config',
                route: 'test-config',
                icon: 'line-md:cog',
                requiresPrivileges: ['ViewSTDConfig', 'ManageSTDConfig'],
            },
            {
                title: 'route.breadcrumb.test',
                route: 'test-test',
                icon: 'lucide:clipboard-list',
                requiresPrivileges: ['ViewSTDTest', 'ManageSTDTest'],
            },
            {
                title: 'route.breadcrumb.question',
                route: 'test-question',
                icon: 'lucide:help-circle',
                requiresPrivileges: ['ViewSTDQuestion', 'ManageSTDQuestion'],
            },
            {
                title: 'route.breadcrumb.result',
                route: 'test-result',
                icon: 'lucide:flag',
                requiresPrivileges: ['ViewSTDResult'],
            },
        ],
    },
]
