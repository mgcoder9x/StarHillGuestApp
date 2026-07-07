<template>
    <p class="clearfix mb-0">
        <!-- <span class="float-md-left d-block d-md-inline-block mt-25">
            COPYRIGHT © {{ new Date().getFullYear() }}
            <b-link class="ml-25" href="#" target=" _blank">{{
                companyName
            }}</b-link>
            <span class="d-none d-sm-inline-block">, All rights Reserved</span>
        </span> -->
    </p>
</template>

<script>
import { BLink } from 'bootstrap-vue'
import { $themeConfig } from '@themeConfig'

export default {
    components: {
        BLink,
    },
    setup() {
        // App Name
        const { companyName } = $themeConfig.app
        return {
            companyName,
        }
    },
}
</script>
