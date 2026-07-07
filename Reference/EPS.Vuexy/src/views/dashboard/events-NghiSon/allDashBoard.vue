<template>
    <section id="dashboard-ecommerce">
        <b-card class="shadow rounded border-0">
            <b-tabs v-model="activeTab" pills>
                <b-tab
                    :title="this.$t('Events.DashBoard.Overview')"
                    :active="activeTab === 0"
                >
                    <Overview v-if="activeTab === 0" />
                </b-tab>
            </b-tabs>
        </b-card>
    </section>
</template>

<script>
/* eslint-disable */
import Overview from './overview/'

export default {
    components: {
        Overview,
    },

    data() {
        return {
            activeTab: 0,
        }
    },
}
</script>

<style lang="scss">
@import '@core/scss/vue/pages/dashboard-ecommerce.scss';
@import '@core/scss/vue/libs/chart-apex.scss';
</style>
