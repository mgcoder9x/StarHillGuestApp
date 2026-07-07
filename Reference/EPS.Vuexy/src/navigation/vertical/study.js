/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

export const studyParent = {
    title: 'route.common.study',
    icon: 'quill:list',
    tagVariant: 'light-warning',
}

// Student component
export const Student = {
    title: 'route.breadcrumb.student',
    route: 'study-student',
    icon: 'lucide:user',
    requiresPrivileges: ['ViewSTDUsers', 'ManageSTDUsers'],
}

// Courses component
export const Courses = {
    title: 'route.breadcrumb.courses',
    route: 'study-course',
    icon: 'lucide:graduation-cap',
    requiresPrivileges: ['ViewSTDCourse', 'ManageSTDCourse'],
}

// Classes component
export const Classes = {
    title: 'route.breadcrumb.classes',
    route: 'study-classes',
    icon: 'lucide:door-open',
    requiresPrivileges: ['ViewSTDClasses', 'ManageSTDClasses'],
}

// Lesson component
export const Lesson = {
    title: 'route.breadcrumb.lesson',
    route: 'study-lesson',
    icon: 'lucide:book-open-check',
    requiresPrivileges: ['ViewSTDLesson', 'ManageSTDLesson'],
}

export const studyMap = {
    Student,
    Courses,
    Classes,
    Lesson,
}

export const studyVerticalNav = createNavItemFactory(studyParent, studyMap)

export default [
    {
        ...createNavItemFactory(studyMap, studyParent),
    },
]

