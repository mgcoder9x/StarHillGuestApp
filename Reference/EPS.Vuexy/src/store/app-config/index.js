import { $themeConfig } from '@themeConfig'

export default {
    namespaced: true,
    state: {
        // layout: {
        //     isRTL: $themeConfig.layout.isRTL,
        //     skin:
        //         localStorage.getItem('vuexy-skin') || $themeConfig.layout.skin,
        //     routerTransition: $themeConfig.layout.routerTransition,
        //     type: $themeConfig.layout.type,
        //     contentWidth: $themeConfig.layout.contentWidth,
        //     menu: {
        //         hidden: $themeConfig.layout.menu.hidden,
        //     },
        //     navbar: {
        //         type: $themeConfig.layout.navbar.type,
        //         backgroundColor: $themeConfig.layout.navbar.backgroundColor,
        //     },
        //     footer: {
        //         type: $themeConfig.layout.footer.type,
        //     },
        // },
        layout: {
            isRTL:
                localStorage.getItem('isRTL') === 'true' ||
                $themeConfig.layout.isRTL,
            skin:
                localStorage.getItem('vuexy-skin') || $themeConfig.layout.skin,
            routerTransition:
                localStorage.getItem('routerTransition') ||
                $themeConfig.layout.routerTransition,
            type:
                localStorage.getItem('layoutType') || $themeConfig.layout.type,
            contentWidth:
                localStorage.getItem('contentWidth') ||
                $themeConfig.layout.contentWidth,
            menu: {
                hidden:
                    localStorage.getItem('isNavMenuHidden') === 'true' ||
                    $themeConfig.layout.menu.hidden,
            },
            navbar: {
                type:
                    localStorage.getItem('navbarType') ||
                    $themeConfig.layout.navbar.type,
                backgroundColor:
                    localStorage.getItem('navbarBackgroundColor') ||
                    $themeConfig.layout.navbar.backgroundColor,
            },
            footer: {
                type:
                    localStorage.getItem('footerType') ||
                    $themeConfig.layout.footer.type,
            },
            breadcrumb: {
                type:
                    localStorage.getItem('breadcrumbType') ||
                    $themeConfig.layout.breadcrumb.type,
            },
        },
    },
    getters: {},
    mutations: {
        // TOGGLE_RTL(state, val) {
        //     state.layout.isRTL = val
        //     document.documentElement.setAttribute('dir', val ? 'rtl' : 'ltr')
        // },
        // UPDATE_SKIN(state, skin) {
        //     state.layout.skin = skin

        //     // Update value in localStorage
        //     localStorage.setItem('vuexy-skin', skin)

        //     // Update DOM for dark-layout
        //     if (skin === 'dark') document.body.classList.add('dark-layout')
        //     else if (document.body.className.match('dark-layout'))
        //         document.body.classList.remove('dark-layout')
        // },
        // UPDATE_ROUTER_TRANSITION(state, val) {
        //     state.layout.routerTransition = val
        // },
        // UPDATE_LAYOUT_TYPE(state, val) {
        //     state.layout.type = val
        // },
        // UPDATE_CONTENT_WIDTH(state, val) {
        //     state.layout.contentWidth = val
        // },
        // UPDATE_NAV_MENU_HIDDEN(state, val) {
        //     state.layout.menu.hidden = val
        // },
        // UPDATE_NAVBAR_CONFIG(state, obj) {
        //     Object.assign(state.layout.navbar, obj)
        // },
        // UPDATE_FOOTER_CONFIG(state, obj) {
        //     Object.assign(state.layout.footer, obj)
        // },
        TOGGLE_RTL(state, val) {
            state.layout.isRTL = val
            localStorage.setItem('isRTL', val) // Lưu vào localStorage
            document.documentElement.setAttribute('dir', val ? 'rtl' : 'ltr')
        },
        UPDATE_SKIN(state, skin) {
            state.layout.skin = skin

            // Update value in localStorage
            localStorage.setItem('vuexy-skin', skin)

            // Update DOM for dark-layout
            if (skin === 'dark') document.body.classList.add('dark-layout')
            else if (document.body.className.match('dark-layout'))
                document.body.classList.remove('dark-layout')
        },
        UPDATE_LAYOUT_TYPE(state, val) {
            state.layout.type = val
            localStorage.setItem('layoutType', val) // Lưu vào localStorage
        },
        // Thêm logic lưu vào localStorage cho các mutations khác
        UPDATE_CONTENT_WIDTH(state, val) {
            state.layout.contentWidth = val
            localStorage.setItem('contentWidth', val)
        },
        UPDATE_NAV_MENU_HIDDEN(state, val) {
            state.layout.menu.hidden = val
            localStorage.setItem('isNavMenuHidden', val)
        },
        UPDATE_ROUTER_TRANSITION(state, val) {
            state.layout.routerTransition = val
            localStorage.setItem('routerTransition', val)
        },
        UPDATE_NAVBAR_CONFIG(state, obj) {
            Object.assign(state.layout.navbar, obj)
            if (obj.type) localStorage.setItem('navbarType', obj.type)
            if (obj.backgroundColor)
                localStorage.setItem(
                    'navbarBackgroundColor',
                    obj.backgroundColor
                )
        },
        UPDATE_FOOTER_CONFIG(state, obj) {
            Object.assign(state.layout.footer, obj)
            localStorage.setItem('footerType', obj.type)
        },
         UPDATE_BREADCRUMB_CONFIG(state, obj) {
            Object.assign(state.layout.breadcrumb, obj)
            localStorage.setItem('breadcrumbType', obj.type)
        },
    },
    actions: {},
}
