<template>
    <b-container fluid>
        <b-card-header style="margin-bottom: -1.5rem">
            <b-card-title>
                {{ $t('Events.OverView.DataOverview') }}
            </b-card-title>
            <b-card-text>
                <strong class="mr-1"
                    >{{ $t('Events.OverView.LastUpdate') }}
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

        <Statistics :data="statsData" />

        <!-- Vehicle bar chart -->
        <b-row>
            <b-col md="7" style="flex: 0 0 54.17%; max-width: 54.17%">
                <VehicleChart
                    :data="vehicleChartData"
                    :categories="vehicleChartData.categories"
                />
            </b-col>

            <!-- Top access areas & PPE stats -->
            <b-col md="5" style="flex: 0 0 45.83%; max-width: 45.83%">
                <!-- <VehicleRank :data="vehicleRankData" /> -->

                <b-row>
                    <b-col md="6">
                        <ProtectiveEquipmentRange
                            :data="protectiveEquipmentRangeData"
                        />
                    </b-col>

                    <b-col md="6">
                        <ConveyorEvents :data="conveyorEventsData" />
                    </b-col>
                </b-row>
            </b-col>
        </b-row>
    </b-container>
</template>

<script>
import Statistics from './Statistics.vue'
import VehicleChart from './VehicleChart.vue'
import VehicleRank from './VehicleRank.vue'
import ProtectiveEquipmentRange from './ProtectiveEquipmentRange.vue'
import ConveyorEvents from './ConveyorEvents.vue'

export default {
    components: {
        Statistics,
        VehicleChart,
        VehicleRank,
        ProtectiveEquipmentRange,
        ConveyorEvents,
    },
    data() {
        return {
            refresh: {
                updateTime: new Date().toLocaleString(),
                interval: 0,
            },
            statsData: [
                {
                    title: 'Events.OverView.DataOverview',
                    dataStats: [
                        {
                            label: 'Events.OverView.IN',
                            value: 0,
                        },
                        {
                            label: 'Events.OverView.OUT',
                            value: 0,
                        },
                    ],
                },
                {
                    title: 'Events.OverView.VirtualFence',
                    dataStats: [
                        {
                            label: 'Events.OverView.Visitor',
                            value: 0,
                        },
                        {
                            label: 'Events.OverView.Employee',
                            value: 0,
                        },
                    ],
                },
                {
                    title: 'Events.OverView.FireSmoke',
                    dataStats: [
                        {
                            label: 'Events.OverView.Fire',
                            value: 0,
                        },
                        {
                            label: 'Events.OverView.Smoke',
                            value: 0,
                        },
                    ],
                },
                {
                    title: 'Events.OverView.FireSmoke',
                    dataStats: [
                        // {
                        //     label: 'Thiếu đồ bảo hộ',
                        //     value: 0,
                        // },
                    ],
                },
                {
                    title: this.$t('Events.OverView.FireSmoke'),
                    dataStats: [
                        // {
                        //     label: 'Sự cố băng tải',
                        //     value: 0,
                        // },
                    ],
                },
            ],
            vehicleChartData: {
                series: [
                    {
                        name: 'Events.OverView.IN',
                        data: [],
                    },
                    {
                        name: 'Events.OverView.OUT',
                        data: [],
                    },
                ],
                categories: [],
            },
            vehicleRankData: [],
            protectiveEquipmentRangeData: [],
            conveyorEventsData: [],
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
        this.getStatsData()
        this.startInterval()
    },
    beforeDestroy() {
        this.clearInterval()
    },

    methods: {
        async getStatsData() {
            this.$services
                .get(`/dashboard/dashboard-events/vehicle-statics`)
                .then((res) => {
                    const { data } = res.data
                    this.loadVehicleChartData(data.hourlyData)
                    this.statsData[0].dataStats[0].value = data.totalIn
                    this.statsData[0].dataStats[1].value = data.totalOut
                })

            this.$services
                .get(`/dashboard/dashboard-events/forbidden-area`)
                .then((res) => {
                    const { data } = res.data
                    this.statsData[1].dataStats[0].value = data.stranger
                    this.statsData[1].dataStats[1].value = data.employee
                })

            this.$services
                .get(`/dashboard/dashboard-events/vehicle-rank`)
                .then((data) => {
                    this.loadVehicleRankData(data.data.data)
                })

            this.$services
                .get(`/dashboard/dashboard-events/protective-equipment-range`)
                .then((data) => {
                    this.loadProtectiveEquipmentRangeData(data.data.data)

                    let total = 0
                    data.data.data.forEach((item) => {
                        total += item.eventCount
                    })
                    if (this.statsData[3].dataStats.length > 0)
                        this.statsData[3].dataStats[0].value = total
                })

            this.$services
                .get(`/dashboard/dashboard-events/fire-events`)
                .then((data) => {
                    const fireEventsCount = data.data.data[0]
                    this.statsData[2].dataStats[0].value =
                        fireEventsCount.smokeEventCount
                    this.statsData[2].dataStats[1].value =
                        fireEventsCount.fireEventCount
                })

            this.$services
                .get(`/dashboard/dashboard-events/conveyor-events`)
                .then((data) => {
                    this.loadConveyorEventsData(data.data.data)

                    let total = 0
                    data.data.data.forEach((item) => {
                        total += item.eventCount
                    })
                    debugger
                    if (this.statsData[4].dataStats.length > 0)
                        this.statsData[4].dataStats[0].value = total
                })
        },

        loadVehicleChartData(data) {
            this.utilsClearArray(this.vehicleChartData.series[0].data)
            this.utilsClearArray(this.vehicleChartData.series[1].data)
            this.utilsClearArray(this.vehicleChartData.categories)
            data.forEach((item) => {
                this.vehicleChartData.series[0].data.push(item.in)
                this.vehicleChartData.series[1].data.push(item.out)
                this.vehicleChartData.categories.push(
                    `${item.hour.split('T')[1].split(':')[0]}h`
                )
            })
        },

        loadVehicleRankData(data) {
            this.utilsClearArray(this.vehicleRankData)
            data.forEach((item) => {
                this.vehicleRankData.push({
                    name: item.areaName,
                    in: item.in,
                    out: item.out,
                })
            })
        },

        loadProtectiveEquipmentRangeData(data) {
            this.utilsClearArray(this.protectiveEquipmentRangeData)
            data.forEach((item) => {
                this.protectiveEquipmentRangeData.push({
                    label: item.warningLevelId,
                    percent: item.percent,
                })
            })
        },

        loadConveyorEventsData(data) {
            this.utilsClearArray(this.conveyorEventsData)
            data.forEach((item) => {
                this.conveyorEventsData.push({
                    label: item.warningLevelId,
                    eventCount: item.eventCount,
                })
            })
        },

        utilsClearArray(array) {
            array.splice(0, array.length)
        },

        startInterval() {
            this.clearInterval() // clear cái cũ nếu có
            if (!this.refresh.interval) return
            this.intervalId = setInterval(() => {
                this.getStatsData()
                this.refresh.updateTime = new Date().toLocaleString()
            }, this.refresh.interval)
        },

        clearInterval() {
            if (this.intervalId) {
                clearInterval(this.intervalId)
                this.intervalId = null
            }
        },
    },
}
</script>
