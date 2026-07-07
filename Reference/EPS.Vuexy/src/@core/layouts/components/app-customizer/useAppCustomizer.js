/* eslint-disable no-undef */
import { ref, watch } from '@vue/composition-api'
import useAppConfig from '@core/app-config/useAppConfig'

export default function useAppCustomizer() {
    // Customizer
    const isCustomizerOpen = ref(false)

    // Skin
    const skinOptions = [
        { text: 'Light', value: 'light' },
        { text: 'Bordered', value: 'bordered' },
        { text: 'Dark', value: 'dark' },
        { text: 'Semi Dark', value: 'semi-dark' },
    ]

    // Content Width Options
    const contentWidthOptions = [
        { text: 'Full  Width', value: 'full' },
        { text: 'Boxed', value: 'boxed' },
    ]

    // Router Transition
    const routerTransitionOptions = [
        { title: 'Zoom Fade', value: 'zoom-fade' },
        { title: 'Fade', value: 'fade' },
        { title: 'Fade Bottom', value: 'fade-bottom' },
        { title: 'Slide Fade', value: 'slide-fade' },
        { title: 'Zoom Out', value: 'zoom-out' },
        { title: 'None', value: 'none' },
    ]

    // Router Transition
    const layoutTypeOptions = [
        { text: 'Vertical', value: 'vertical' },
        { text: 'Horizontal', value: 'horizontal' },
    ]

    // Navbar
    const navbarColors = [
        '',
        'primary',
        'secondary',
        'success',
        'danger',
        'warning',
        'info',
        'dark',
    ]

    // Navbar Types
    const navbarTypes = [
        { text: 'Floating', value: 'floating' },
        { text: 'Sticky', value: 'sticky' },
        { text: 'Static', value: 'static' },
        { text: 'Hidden', value: 'hidden' },
    ]

    // Footer Types
    const footerTypes = [
        { text: 'Sticky', value: 'sticky' },
        { text: 'Static', value: 'static' },
        { text: 'Hidden', value: 'hidden' },
    ]

     // Breadcrumb Types
    const breadcrumbTypes = [
        { text: 'Static', value: 'static' },
        { text: 'Hidden', value: 'hidden' },
    ]

    // eslint-disable-next-line object-curly-newline
    const {
        isRTL,
        skin,
        contentWidth,
        routerTransition,
        layoutType,
        isNavMenuHidden,
        isVerticalMenuCollapsed,
        navbarBackgroundColor,
        navbarType,
        footerType,
        breadcrumbType,
    } = useAppConfig()

    const persistVariables = [
        { variable: skin, key: 'skin' },
        { variable: contentWidth, key: 'contentWidth' },
        { variable: isRTL, key: 'isRTL' },
        { variable: routerTransition, key: 'routerTransition' },
        { variable: layoutType, key: 'layoutType' },
        { variable: isNavMenuHidden, key: 'isNavMenuHidden' },
        { variable: isVerticalMenuCollapsed, key: 'isVerticalMenuCollapsed' },
        { variable: navbarBackgroundColor, key: 'navbarBackgroundColor' },
        { variable: navbarType, key: 'navbarType' },
        { variable: footerType, key: 'footerType' },
        { variable: breadcrumbType, key: 'breadcrumbType' },
    ]

    // Thêm .value khi gán giá trị từ localStorage
    persistVariables.forEach(({ variable, key }) => {
        const savedValue = localStorage.getItem(key)
        // console.log(`Key ${key} - Saved Value: ${savedValue}`)
        if (savedValue !== null) {
            // eslint-disable-next-line no-param-reassign
            variable.value =
                typeof variable.value === 'boolean'
                    ? savedValue === 'true'
                    : savedValue
        }
    })
    // Lắng nghe thay đổi và lưu vào localStorage
    persistVariables.forEach(({ variable, key }) => {
        watch(variable, (newValue) => {
            localStorage.setItem(key, newValue)
        })
    })
    return {
        // Customizer
        isCustomizerOpen,

        // Vertical Menu
        isVerticalMenuCollapsed,

        // Skin
        skin,
        skinOptions,

        // Content Width
        contentWidth,
        contentWidthOptions,

        // RTL
        isRTL,

        // routerTransition
        routerTransition,
        routerTransitionOptions,

        // Layout Type
        layoutType,
        layoutTypeOptions,

        // NavMenu Hidden
        isNavMenuHidden,

        // Navbar
        navbarColors,
        navbarTypes,
        navbarBackgroundColor,
        navbarType,

        // Footer
        footerTypes,
        footerType,

        // Footer
        breadcrumbTypes,
        breadcrumbType,
    }
}
