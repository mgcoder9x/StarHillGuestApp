<template>
    <b-card v-if="data" class="card-transaction" no-body>
        <b-card-header
            class="d-flex justify-content-between align-items-center"
        >
            <b-card-title class="mb-0">{{
                $t('Events.DashBoard.VehicleMonitoring')
            }}</b-card-title>

            <feather-icon
                v-if="!isSetting"
                icon="MoreVerticalIcon"
                size="18"
                class="text-body cursor-pointer"
                @click="isSetting = true"
            />
        </b-card-header>

        <b-card-body>
            <!-- Giao diện cấu hình -->
            <div v-if="isSetting">
                <b-form-group
                    :label="this.$t('Events.DashBoard.Period')"
                    label-for="period-select"
                    class="mb-2"
                >
                    <b-form-select
                        id="period-select"
                        v-model="setting.period"
                        :options="periodOptions"
                    />
                </b-form-group>

                <b-form-group
                    :label="this.$t('Events.DashBoard.AutoRefresh')"
                    label-for="refresh-select"
                    class="mb-2"
                >
                    <b-form-select
                        id="refresh-select"
                        v-model="setting.refreshInterval"
                        :options="refreshOptions"
                    />
                    <!-- <small class="text-muted">How often you would like this gadget to update.</small> -->
                </b-form-group>

                <div class="text-right">
                    <b-button
                        size="sm"
                        variant="primary"
                        class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                        @click="saveSettings"
                    >
                        {{ $t('common.button.save') }}
                    </b-button>
                    <b-button
                        size="sm"
                        variant="secondary"
                        class="btn-120 mb-50 btn-hover-linear-secondary border-0"
                        @click="resetSettings"
                    >
                        {{ $t('common.button.cancel') }}
                    </b-button>
                </div>
            </div>

            <!-- Giao diện hiển thị dữ liệu -->
            <div v-else>
                <div
                    v-for="(transaction, index) in data"
                    :key="transaction.mode"
                    class="transaction-item mb-1"
                >
                    <b-media no-body>
                        <b-media-body>
                            <h6 class="transaction-title mb-25">
                                {{ transaction.name }}
                            </h6>
                        </b-media-body>
                    </b-media>

                    <div
                        class="d-flex align-items-center justify-content-between"
                    >
                        <div
                            class="d-flex align-items-center mr-1"
                            style="min-width: 50px"
                        >
                            <span class="font-weight-bold text-primary mr-2">
                                {{ transaction.countEventId }}
                            </span>
                            <span class="font-weight-bold text-success">
                                {{ transaction.percentOfTotal + '%' }}
                            </span>
                        </div>

                        <vue-apex-charts
                            type="radialBar"
                            height="30"
                            width="30"
                            :tooltip="transaction.percentOfTotal + `%`"
                            :options="chartData[index].options"
                            :series="chartData[index].series"
                        />
                    </div>
                </div>
            </div>
        </b-card-body>
    </b-card>
</template>

<script>
import {
    BCard,
    BCardHeader,
    BCardTitle,
    BCardBody,
    BMediaBody,
    BMedia,
    BMediaAside,
    BAvatar,
    BDropdown,
    BDropdownItem,
} from 'bootstrap-vue'
import VueApexCharts from 'vue-apexcharts'
const $trackBgColor = '#e9ecef'

export default {
    components: {
        BCard,
        BCardHeader,
        BCardTitle,
        BCardBody,
        BMediaBody,
        BMedia,
        BMediaAside,
        BAvatar,
        BDropdown,
        BDropdownItem,
        VueApexCharts,
    },
    data() {
        return {
            chartData: [],
            chart: {
                series: [65],
                options: {
                    grid: {
                        show: false,
                        padding: {
                            left: -15,
                            right: -15,
                            top: -12,
                            bottom: -15,
                        },
                    },
                    plotOptions: {
                        radialBar: {
                            hollow: {
                                size: '20%',
                            },
                            track: {
                                background: $trackBgColor,
                            },
                            dataLabels: {
                                showOn: 'always',
                                name: {
                                    show: false,
                                },
                                value: {
                                    show: false,
                                },
                            },
                        },
                    },
                    stroke: {
                        lineCap: 'round',
                    },
                },
            },
            isSetting: false,
            setting: {
                dashboardType: 1,
                period: null,
                refreshInterval: null,
            },
            refreshTimer: null,
            refreshIntervals: {
                1: 0,
                2: 5 * 60 * 1000,
                3: 15 * 60 * 1000,
                4: 60 * 60 * 1000,
                5: 2 * 60 * 60 * 1000,
            },
        }
    },
    computed: {
        periodOptions() {
            return [
                { value: 1, text: this.$t('Events.VehicleMonitor.EveryDay') },
                { value: 2, text: this.$t('Events.VehicleMonitor.EveryWeek') },
                { value: 3, text: this.$t('Events.VehicleMonitor.EveryMonth') },
            ]
        },
        refreshOptions() {
            return [
                { value: 1, text: this.$t('Events.VehicleMonitor.Never') },
                {
                    value: 2,
                    text: this.$t('Events.VehicleMonitor.FiveMinute'),
                },
                {
                    value: 3,
                    text: this.$t('Events.VehicleMonitor.FifteenMinute'),
                },
                {
                    value: 4,
                    text: this.$t('Events.VehicleMonitor.OneHour'),
                },
                {
                    value: 5,
                    text: this.$t('Events.VehicleMonitor.TwoHour'),
                },
            ]
        },
    },
    props: {
        data: {
            type: Array,
            default: () => [],
        },
    },
    watch: {
        data: {
            immediate: true, // để chạy luôn khi component mount
            handler(newVal) {
                this.chartData = []
                for (let i = 0; i < newVal.length; i += 1) {
                    const chartClone = JSON.parse(JSON.stringify(this.chart))
                    chartClone.series[0] = newVal[i].percentOfTotal
                    this.chartData.push(chartClone)
                }
            },
        },
        'setting.refreshInterval'(newVal) {
            this.setupAutoRefresh()
        },
    },
    async created() {
        this.getSetting().then(() => {
            this.getData()
            this.setupAutoRefresh()
        })
    },
    beforeDestroy() {
        if (this.refreshTimer) {
            clearInterval(this.refreshTimer)
        }
    },
    methods: {
        async getData() {
            var vm = this
            await this.$services
                .get(`/dashboard/dashBoardVehicle/${vm.setting.period}`)
                .then((response) => {
                    vm.data = response.data.data
                })
        },
        async getSetting() {
            var vm = this
            await this.$services.get('/dashboard/setting/1').then((rs) => {
                if (rs.data.data) {
                    vm.setting = rs.data.data
                } else {
                    vm.setting.period = 1
                    vm.setting.refreshInterval = 1
                    vm.setting.dashboardType = 1
                }
            })
        },
        saveSettings() {
            this.setting.dashboardType = 1
            this.$services
                .post('/dashboard/saveSetting', this.setting)
                .then((rs) => {
                    this.isSetting = false
                    this.getData()
                    this.setupAutoRefresh()
                })
        },
        resetSettings() {
            this.isSetting = false
        },
        setupAutoRefresh() {
            if (this.refreshTimer) {
                clearInterval(this.refreshTimer)
                this.refreshTimer = null
            }
            const interval = this.refreshIntervals[this.setting.refreshInterval]
            if (interval > 0) {
                this.refreshTimer = setInterval(() => {
                    this.getData()
                }, interval)
            }
        },
    },
}
</script>
