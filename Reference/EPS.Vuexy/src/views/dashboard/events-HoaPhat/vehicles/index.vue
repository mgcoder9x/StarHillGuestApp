<template>
    <b-container fluid>
        <b-card-header>
            <b-card-title>
                {{ $t('Events.VehicleMonitor.OverviewAccessSystems') }}
            </b-card-title>
            <b-card-text>
                <strong class="mr-1">
                    {{ $t('Events.VehicleMonitor.LastUpdate') }}:
                    {{ refresh.updateTime }}</strong
                >
                <b-dropdown
                    variant="link"
                    no-caret
                    right
                    class="chart-dropdown"
                    toggle-class="p-0"
                >
                    <template #button-content>
                        <feather-icon
                            icon="RefreshCcwIcon"
                            size="18"
                            class="text-body cursor-pointer"
                        />
                    </template>
                    <b-dropdown-item
                        v-for="option in refreshOptions"
                        :key="option.value"
                        :class="{
                            'bg-light text-dark font-weight-bold':
                                option.value === refresh.interval,
                        }"
                        @click="refresh.interval = option.value"
                    >
                        {{ option.text }}
                    </b-dropdown-item>
                </b-dropdown>
            </b-card-text>
        </b-card-header>

        <VehicleCounts :data="vehicleCounts" />

        <!-- Vehicle bar chart -->
        <b-row style="margin">
            <b-col lg="6">
                <VehicleChartBySeat
                    :series="vehicleChartBySeat.series"
                    :labels="vehicleChartBySeat.labels"
                />
            </b-col>

            <b-col lg="6">
                <VehicleTableBySeat :data="vehicleBySeat" />
                <VehicleTableByArea :data="vehicleByArea" />
            </b-col>
        </b-row>
    </b-container>
</template>

<script>
import VehicleCounts from './VehicleCounts.vue'
import VehicleChartBySeat from './VehicleChartBySeat.vue'
import VehicleTableBySeat from './VehicleTableBySeat.vue'
import VehicleTableByArea from './VehicleTableByArea.vue'

export default {
    components: {
        VehicleCounts,
        VehicleChartBySeat,
        VehicleTableBySeat,
        VehicleTableByArea,
    },
    data() {
        return {
            refresh: {
                updateTime: new Date().toLocaleString(),
                interval: 0,
            },

            vehicleCounts: {
                inSide: 0,
                totalIn: 0,
                totalOut: 0,
            },
            vehicleBySeat: [],
            vehicleByArea: [],
            vehicleChartBySeat: {
                series: [],
                labels: [],
            },
        }
    },
    computed: {
        refreshOptions() {
            return [
                {
                    value: 0,
                    text: this.$t('Events.VehicleMonitor.Never'),
                },
                {
                    value: 5 * 1000,
                    text: this.$t('Events.VehicleMonitor.FiveSecond'),
                },
                {
                    value: 5 * 60 * 1000,
                    text: this.$t('Events.VehicleMonitor.FiveMinute'),
                },
                {
                    value: 15 * 60 * 1000,
                    text: this.$t('Events.VehicleMonitor.FifteenMinute'),
                },
                {
                    value: 60 * 60 * 1000,
                    text: this.$t('Events.VehicleMonitor.OneHour'),
                },
                {
                    value: 2 * 60 * 60 * 1000,
                    text: this.$t('Events.VehicleMonitor.TwoHour'),
                },
            ]
        },
    },
    watch: {
        // eslint-disable-next-line
        'refresh.interval'() {
            this.startInterval()
        },
    },

    created() {
        this.getData()
        this.startInterval()
    },
    beforeDestroy() {
        this.clearInterval()
    },

    methods: {
        // #region API: Load all dashboard vehicle data
        getData() {
            this.$services
                .get(`/dashboard/dashboard-events/vehicle-counts`)
                .then((data) => {
                    const res = data.data.data[0]
                    this.loadVehicleCounts(res)
                })

            this.$services
                .get(`/dashboard/dashboard-events/vehicle-stats-by-seat`)
                .then((data) => {
                    const res = data.data.data
                    this.loadVehicleChartBySeat(res)
                    this.loadVehicleTableBySeat(res)
                })

            this.$services
                .get(`/dashboard/dashboard-events/vehicle-stats-by-area`)
                .then((data) => {
                    const res = data.data.data
                    this.loadVehicleTableByArea(res)
                })
        },
        // #endregion

        // #region Data Mappers
        loadVehicleCounts(data) {
            this.vehicleCounts.inSide = data.inside
            this.vehicleCounts.totalIn = data.totalIn
            this.vehicleCounts.totalOut = data.totalOut
        },

        loadVehicleChartBySeat(data) {
            const { labels } = this.vehicleChartBySeat
            const { series } = this.vehicleChartBySeat
            this.utilsClearArray(labels)
            this.utilsClearArray(series)
            data.forEach((item) => {
                labels.push(item.totalNumberOfSeats)
                series.push(item.totalIn)
            })
        },

        loadVehicleTableBySeat(data) {
            const { vehicleBySeat } = this
            this.utilsClearArray(vehicleBySeat)
            data.forEach((item) => {
                const vehicle = {
                    type: item.totalNumberOfSeats,
                    in: item.totalIn,
                    out: item.totalOut,
                    inSide: item.inside,
                }
                vehicleBySeat.push(vehicle)
            })
        },

        loadVehicleTableByArea(data) {
            const { vehicleByArea } = this
            this.utilsClearArray(vehicleByArea)
            data.forEach((item) => {
                const vehicle = {
                    area: item.areaName,
                    in: item.totalIn,
                    out: item.totalOut,
                }
                vehicleByArea.push(vehicle)
            })
        },
        // #endregion

        // #region Utils
        utilsClearArray(array) {
            array.splice(0, array.length)
        },
        // #endregion

        // #region Interval
        startInterval() {
            this.clearInterval() // clear cái cũ nếu có
            if (!this.refresh.interval) return
            this.intervalId = setInterval(() => {
                this.getData()
                this.refresh.updateTime = new Date().toLocaleString()
            }, this.refresh.interval)
        },

        clearInterval() {
            if (this.intervalId) {
                clearInterval(this.intervalId)
                this.intervalId = null
            }
        },
        // #endregion
    },
}
</script>
