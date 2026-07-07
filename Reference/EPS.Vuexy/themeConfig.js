// Theme Colors
// Initially this will be blank. Later on when app is initialized we will assign bootstrap colors to this from CSS variables.
export const $themeColors = {}

// App Breakpoints
// Initially this will be blank. Later on when app is initialized we will assign bootstrap breakpoints to this object from CSS variables.
export const $themeBreakpoints = {}

// APP CONFIG
export const $themeConfig = {
    app: {
        appName: 'SMART APP', // Will update name in navigation menu (Branding)
        companyName: 'ATIN',
        // eslint-disable-next-line global-require
        appLogoImage: require('@/assets/images/logo/logo.png'), // Will update logo in navigation menu (Branding)
        apiURL: `${window.location.origin}/Service`,
        apiURLDev: 'http://localhost:1938',
        nodeMediaServer: 'https://cam-live.atin.vn/POC_',
        timeReload: 10000,
        soundurl: '/sound/alarm.mp3'
    },
    layout: {
        isRTL: false,
        skin: 'light', // light, dark, bordered, semi-dark
        routerTransition: 'zoom-fade', // zoom-fade, slide-fade, fade-bottom, fade, zoom-out, none
        type: 'vertical', // vertical, horizontal
        contentWidth: 'full', // full, boxed
        menu: {
            hidden: false,
            isCollapsed: false,
        },
        navbar: {
            // ? For horizontal menu, navbar type will work for navMenu type
            type: 'floating', // static , sticky , floating, hidden
            backgroundColor: '', // BS color options [primary, success, etc]
        },
        footer: {
            type: 'static', // static, sticky, hidden
        },
        breadcrumb: {
            type: 'static', // static, sticky, hidden
        },
        customizer: true,
        enableScrollToTop: true,
    },
}

export const $z121Config = {
    PERFORMANCE: {
        GOOD: 80,
        BAD: 70,
    },
}
