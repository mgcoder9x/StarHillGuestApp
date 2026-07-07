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
                        v-for="option in refresh.options"
                        :key="option.value"
                        :class="{
                            'bg-light text-dark font-weight-bold':
                                option.value === refresh.interval,
                        }"
                        @click="refresh.interval = option.value"
                    >
                        {{ $t(option.text) }}
                    </b-dropdown-item>
                </b-dropdown>
            </b-card-text>
        </b-card-header>
        <b-row>
            <b-col md="12">
                <Statistics :data="statsData" />

            </b-col>
        </b-row>

        <!-- Vehicle bar chart -->
        <b-row>
            <b-col md="7">
                <VehicleChart
                    :data="vehicleChartData"
                    :categories="vehicleChartData.categories"
                />
            </b-col>

            <!-- Top access areas & PPE stats -->
            <b-col md="5">
                <VehicleRank :data="contInOutData" :header="headerInOut"/>

                <VehicleRank :data="contTransportData" :header="headerTransport"/>
            </b-col>
        </b-row>
    </b-container>
</template>

<script>
import Statistics from './Statistics.vue'
import VehicleChart from './VehicleChart.vue'
import VehicleRank from './VehicleRank.vue'

export default {
    components: {
        Statistics,
        VehicleChart,
        VehicleRank,
    },
    data() {
        return {
            refresh: {
                updateTime: new Date().toLocaleString(),
                interval: 0,
                options: [
                    { value: 0, text: 'Refresh.Never' },
                    { value: 5 * 1000, text: 'Refresh.5s' },
                    { value: 5 * 60 * 1000, text: 'Refresh.5m' },
                    { value: 15 * 60 * 1000, text: 'Refresh.15m' },
                    { value: 60 * 60 * 1000, text: 'Refresh.1h' },
                    { value: 2 * 60 * 60 * 1000, text: 'Refresh.2h' },
                ],
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
                    title: 'Events.OverView.VehicleIn',
                    dataStats: [
                        {
                            label: 'Events.OverView.GoodsIn',
                            value: 0,
                        },
                        {
                            label: 'Events.OverView.Empty',
                            value: 0,
                        },
                    ],
                },
                {
                    title: 'Events.OverView.VehicleOut',
                    dataStats: [
                        {
                            label: 'Events.OverView.GoodsOut',
                            value: 0,
                        },
                        {
                            label: 'Events.OverView.Empty',
                            value: 0,
                        },
                    ],
                },
                {
                    title: 'Events.OverView.Warning',
                    dataStats: [
                        {
                            label: 'Events.OverView.WrongPlan',
                            value: 0,
                        },
                        {
                            label: 'Events.OverView.SuspectInStock',
                            value: 0,
                        },
                        {
                            label: 'Events.OverView.ForbiddenArea',
                            value: 0,
                        },
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
            contInOutData: [],
            contTransportData: [],
            protectiveEquipmentRangeData: [],
            conveyorEventsData: [],
            headerInOut:{
                header: 'Events.OverView.InOut.Header',
                header1: 'Events.OverView.InOut.Area',
                header2: 'Events.OverView.InOut.Empty',
                header3: 'Events.OverView.InOut.Cargo',
                header4: 'Events.OverView.InOut.Still'
            },
            headerTransport:{
                header: 'Events.OverView.Transport.Header',
                header1: "",
                header2: 'Events.OverView.Transport.In',
                header3: 'Events.OverView.Transport.Out',
                header4: 'Events.OverView.Transport.Total'
            }
        }
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
                .get(`/dashboard/dashboard-events/coutEventByArea`)
                .then((data) => {
                    this.loadContInOutData(data.data.data)
                    this.loadContTransportData(data.data.data)
                    let totalIn = 0
                    let totalOut = 0
                    let loadedIn = 0
                    let loadedOut = 0
                    let emptyIn = 0
                    let emptyOut = 0
                    let suspectedLoad = 0
                    data.data.data.forEach((item) => {
                        switch (item.direction) {
                            case 1: 
                                //Tổng vào
                                totalIn += item.total
                                //Vào có hàng
                                loadedIn += item.woodChips + item.scrapIron + item.other
                                //Vào rỗng
                                emptyIn += item.totalEmpty
                                break;
                            case 2: 
                                //Tổng ra
                                totalOut += item.total
                                //Ra có hàng
                                loadedOut += item.woodChips + item.scrapIron + item.other
                                //Ra rỗng
                                emptyOut += item.totalEmpty
                                break;
                        }
                        //Nghi ngờ còn hàng
                        suspectedLoad += item.aLittle
                    })
                    this.statsData[0].dataStats[0].value = totalIn
                    this.statsData[0].dataStats[1].value = totalOut
                    this.statsData[1].dataStats[0].value = loadedIn
                    this.statsData[1].dataStats[1].value = emptyIn
                    this.statsData[2].dataStats[0].value = loadedOut
                    this.statsData[2].dataStats[1].value = emptyOut
                    
                    this.statsData[3].dataStats[1].value = suspectedLoad
                })
             this.$services
                .get(`/dashboard/dashboard-events/contChartData`)
                .then((data) => {
                    this.loadContChartData(data.data.data)
                })
                
             this.$services
                .get(`/dashboard/dashboard-events/countWrongPlan`)
                .then((data) => {
                    this.statsData[3].dataStats[0].value = data.data.data
                })
                
             this.$services
                .get(`/dashboard/countVirtualFence/0/0`)
                .then((data) => {
                    this.statsData[3].dataStats[2].value = data.data.data.countPerson + data.data.data.countVehicle
                })
        },

        loadContChartData(data) {
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

        loadContTransportData(data) {
            this.utilsClearArray(this.contTransportData)
            const newData = [
                //{ name: "Tôn cuộn" , data1: 0, data2:0, data3: 0},
                { name: "Events.OverView.Cont.ScrapIron" , data1: 0, data2:0, data3: 0},
                { name: "Events.OverView.Cont.WoodChips" , data1: 0, data2:0, data3: 0},
                { name: "Events.OverView.Cont.Other"  , data1: 0, data2:0, data3: 0}
            ]
            data.forEach(item => {
                switch (item.direction){
                    case 1:
                        newData[0].data1 += item.scrapIron || 0
                        newData[1].data1 += item.woodChips || 0
                        newData[2].data1 += item.other || 0
                        break;
                    case 2:
                        newData[0].data2 += item.scrapIron || 0
                        newData[1].data2 += item.woodChips || 0
                        newData[2].data2 += item.other || 0
                }
                newData[0].data3 += item.scrapIron
                newData[1].data3 += item.woodChips
                newData[2].data3 += item.other
            })

            this.contTransportData = newData
        },

        loadContInOutData(data) {
            this.utilsClearArray(this.contInOutData)

            const grouped = {}

            data.forEach(item => {
                const id = item.areaId
                if (!grouped[id]) {
                    grouped[id] = {
                        name: item.areaName,
                        data1: 0,
                        data2: 0,
                        data3: 0
                    }
                }

                grouped[id].data1 += item.totalEmpty || 0
                grouped[id].data2 += (item.woodChips || 0) + (item.scrapIron || 0) + (item.other || 0)
                grouped[id].data3 += item.aLittle || 0
            })

            // Đưa vào contInOutData
            for (const id in grouped) {
                this.contInOutData.push(grouped[id])
            }
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
