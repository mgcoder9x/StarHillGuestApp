<template>
    <b-nav-item @click="skin = isDark ? 'light' : 'dark'">
        <Icon
            class="md-icon"
            :icon="`${isDark ? 'line-md:sunny-filled-loop-to-moon-alt-filled-loop-transition' : 'line-md:moon-filled-to-sunny-filled-loop-transition'}`"
        />
    </b-nav-item>
</template>

<script>
import useAppConfig from '@core/app-config/useAppConfig'
import { computed } from '@vue/composition-api'
import { BNavItem } from 'bootstrap-vue'

export default {
    components: {
        BNavItem,
    },
    setup() {
        const { skin } = useAppConfig()

        const isDark = computed(() => skin.value === 'dark')

        return { skin, isDark }
    },
}
</script>
