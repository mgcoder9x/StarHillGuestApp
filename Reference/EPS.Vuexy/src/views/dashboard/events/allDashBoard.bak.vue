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

                <b-tab
                    :title="this.$t('Events.DashBoard.VehicleMonitoring')"
                    :active="activeTab === 1"
                >
                    <Vehicles v-if="activeTab === 1" />
                </b-tab>

                <b-tab
                    :title="this.$t('Events.DashBoard.FaceMonitoring')"
                    :active="activeTab === 2"
                >
                    <b-row v-if="activeTab === 2" class="match-height">
                        <b-col lg="4" md="6">
                            <dashboard-face />
                        </b-col>
                        <b-col lg="4" md="6">
                            <dashboard-vehicle />
                        </b-col>
                    </b-row>
                </b-tab>
            </b-tabs>
        </b-card>
    </section>
</template>

<script>
/* eslint-disable */
import DashboardVehicle from './dashBoardVehicleEvent.vue'
import DashboardFace from './dashBoardFaceEvent.vue'
import Overview from './overview/'
import Vehicles from './vehicles/'

export default {
    components: {
        DashboardVehicle,
        DashboardFace,
        Overview,
        Vehicles,
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
