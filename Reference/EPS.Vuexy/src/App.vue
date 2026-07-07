<template>
       
    <div id="app" class="h-100" :class="[skinClasses]">
               
        <component :is="layout">
                        <router-view />        
        </component>

                <scroll-to-top v-if="enableScrollToTop" />    
    </div>
</template>

<script>
import ScrollToTop from '@core/components/scroll-to-top/ScrollToTop.vue'

// This will be populated in `beforeCreate` hook
import { $themeColors, $themeBreakpoints, $themeConfig } from '@themeConfig'
import { provideToast } from 'vue-toastification/composition'
import { watch } from '@vue/composition-api' // Bỏ 'onMounted' đi
import useAppConfig from '@core/app-config/useAppConfig'

import { useWindowSize, useCssVar } from '@vueuse/core'

import store from '@/store'

const LayoutVertical = () => import('@/layouts/vertical/LayoutVertical.vue')
const LayoutHorizontal = () =>
    import('@/layouts/horizontal/LayoutHorizontal.vue')
const LayoutFull = () => import('@/layouts/full/LayoutFull.vue')

export default {
    name: 'App',
    components: {
        // Layouts
        LayoutHorizontal,
        LayoutVertical,
        LayoutFull,

        ScrollToTop,
    },
    // --- FIX ÂM THANH: BẮT ĐẦU ---
    mounted() {
        // Lắng nghe tương tác đầu tiên của người dùng để gọi hàm mở khóa âm thanh
        document.body.addEventListener('click', this.unlockAudio, {
            once: true,
        })
        document.body.addEventListener('keydown', this.unlockAudio, {
            once: true,
        })
    },
    methods: {
        unlockAudio() {
            console.log(
                'User interaction detected. Unlocking audio playback for the session.'
            )
            const silentAudio = new Audio(
                'data:audio/mpeg;base64,SUQzBAAAAAABEVRYWFgAAAAtAAADY29tbWVudABCaWdTb3VuZEJhbmsuY29tIC8gTGFTb25vdGhlcXVlLm9yZwBURU5DAAAAHQAAA1N3aXRjaCBQbHVzIMOpdG9Db2RlZVRYWFgAAAA/AAACFoVoUmlmZgB1UgAAACNSRUFNQwAAAAsAAAA/AAAACwAAAEFCTgBNVUhAAAAAEgBBAAAASAQBAAABQWlYJ4BAlQAAAAAAAAAAAAA='
            )
            silentAudio.play().catch((e) => {
                console.warn(
                    'Silent audio playback was intentionally triggered to unlock permissions.'
                )
            })
        },
    },
    // --- FIX ÂM THANH: KẾT THÚC ---
    computed: {
        layout() {
            if (this.$route.meta.layout === 'full') return 'layout-full'
            return `layout-${this.contentLayoutType}`
        },
        contentLayoutType() {
            return this.$store.state.appConfig.layout.type
        },
    },
    beforeCreate() {
        // Set colors in theme
        const colors = [
            'primary',
            'secondary',
            'success',
            'info',
            'warning',
            'danger',
            'light',
            'dark',
        ] // eslint-disable-next-line no-plusplus

        for (let i = 0, len = colors.length; i < len; i++) {
            $themeColors[colors[i]] = useCssVar(
                `--${colors[i]}`,
                document.documentElement
            ).value.trim()
        } // Set Theme Breakpoints

        const breakpoints = ['xs', 'sm', 'md', 'lg', 'xl'] // eslint-disable-next-line no-plusplus

        for (let i = 0, len = breakpoints.length; i < len; i++) {
            $themeBreakpoints[breakpoints[i]] = Number(
                useCssVar(
                    `--breakpoint-${breakpoints[i]}`,
                    document.documentElement
                ).value.slice(0, -2)
            )
        } // Set RTL

        const { isRTL } = $themeConfig.layout
        document.documentElement.setAttribute('dir', isRTL ? 'rtl' : 'ltr')
        document.title = $themeConfig.app.appName
    },
    setup() {
        const { skin, skinClasses } = useAppConfig()
        const { enableScrollToTop } = $themeConfig.layout // If skin is dark when initialized => Add class to body

        if (skin.value === 'dark') document.body.classList.add('dark-layout') // Provide toast for Composition API usage
        // This for those apps/components which uses composition API
        // Demos will still use Options API for ease

        provideToast({
            hideProgressBar: true,
            closeOnClick: false,
            closeButton: false,
            icon: false,
            timeout: 3000,
            transition: 'Vue-Toastification__fade',
        }) // Set Window Width in store

        store.commit('app/UPDATE_WINDOW_WIDTH', window.innerWidth)
        const { width: windowWidth } = useWindowSize()
        watch(windowWidth, (val) => {
            store.commit('app/UPDATE_WINDOW_WIDTH', val)
        })

        return {
            skinClasses,
            enableScrollToTop,
        }
    },
}
</script>

<style>
.blink {
    animation: blink 0.3s infinite;
}

@keyframes blink {
    0% {
        opacity: 1;
    }
    50% {
        opacity: 0.5;
    }
    100% {
        opacity: 1;
    }
}
</style>
