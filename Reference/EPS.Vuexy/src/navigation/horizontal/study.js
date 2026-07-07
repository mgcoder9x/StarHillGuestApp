/* eslint-disable */
import { createNavItemFactory } from '../navigation-factory'

export const studyParent = {
    code: 'study',
    name: 'route.common.study',
    header: 'route.common.study',
    icon: 'quill:list',
}

// Student component
export const Student = {
    code: 'manage_student',
    name: 'Nav.Student',
    title: 'route.breadcrumb.student',
    route: 'study-student-list',
    icon: 'line-md:map-marker-alt',
    requiresPrivileges: ['ViewSTDUsers', 'ManageSTDUsers'],
}

// Courses component
export const Courses = {
    code: 'manage_courses',
    name: 'Nav.Courses',
    title: 'route.breadcrumb.courses',
    route: 'study-courses-list',
    icon: 'lucide:graduation-cap',
    requiresPrivileges: ['ViewSTDCourse', 'ManageSTDCourse'],
}

// Classes component
export const Classes = {
    code: 'manage_classes',
    name: 'Nav.Classes',
    title: 'route.breadcrumb.classes',
    route: 'study-classes-list',
    icon: 'lucide:door-open',
    requiresPrivileges: ['ViewSTDClasses', 'ManageSTDClasses'],
}

// Lesson component
export const Lesson = {
    code: 'manage_lesson',
    name: 'Nav.Lesson',
    title: 'route.breadcrumb.lesson',
    route: 'study-lesson-list',
    icon: 'lucide:book-open-check',
    requiresPrivileges: ['ViewSTDLesson', 'ManageSTDLesson'],
}

export const studyMap = {
    Student,
    Courses,
    Classes,
    Lesson,
}

export const studyHorizontalNav = createNavItemFactory(
    studyMap,
    studyParent
)

export default [
    {
        ...createNavItemFactory(studyMap, studyParent),
    },
]
