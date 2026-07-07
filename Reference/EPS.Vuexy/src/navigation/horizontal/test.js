/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

export default [
    {
        code: 'test',
        name: 'route.common.test',
        header: 'route.common.test',
        icon: 'quill:list',
        children: [
            {
                code: 'manage_config',
                name: 'Nav.Config',
                title: 'route.breadcrumb.student',
                route: 'test-config-list',
                icon: 'line-md:map-marker-alt',
                requiresPrivileges: ['ViewSTDConfig', 'ManageSTDConfig'],
            },
            {
                code: 'manage_test',
                name: 'Nav.Test',
                title: 'route.breadcrumb.test',
                route: 'test-test-list',
                icon: 'line-md:map-marker-alt',
                requiresPrivileges: ['ViewSTDTest', 'ManageSTDTest'],
            },
            {
                code: 'manage_question',
                name: 'Nav.Question',
                title: 'route.breadcrumb.question',
                route: 'test-question-list',
                icon: 'line-md:map-marker-alt',
                requiresPrivileges: ['ViewSTDQuestion', 'ManageSTDQuestion'],
            },
            {
                code: 'manage_result',
                name: 'Nav.Result',
                title: 'route.breadcrumb.result',
                route: 'test-result-list',
                icon: 'line-md:map-marker-alt',
                requiresPrivileges: ['ViewSTDResult'],
            },
        ],
    },
]
