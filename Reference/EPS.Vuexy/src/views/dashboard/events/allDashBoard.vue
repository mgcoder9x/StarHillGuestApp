<template>
    <div>
        <div class="traffic-dashboard-container">
            <!-- Auto Refresh Controls -->
            <div class="auto-refresh-section">
                <div class="refresh-info">
                    <span class="refresh-label"
                        >{{ $t('dashboard.lastUpdate') }}:
                    </span>
                    <span class="refresh-time">{{ lastUpdateTime }}</span>
                </div>
                <div class="refresh-control">
                    <v-select
                        v-model="refreshInterval"
                        :placeholder="$t('dashboard.selectRefreshInterval')"
                        label="text"
                        :reduce="(type) => type.value"
                        :options="refreshIntervalOptions"
                        :clearable="false"
                        class="refresh-dropdown"
                    />
                </div>
            </div>

            <div class="data-summary-section">
                <div class="summary-card">
                    <div class="icon-circle orange-bg">
                        <span class="value">{{ summaryData.inUnit }}</span>
                    </div>
                    <p class="label">{{ $t('dashboard.totalVehicle') }}</p>
                </div>
                <div class="summary-card">
                    <div class="icon-circle red-bg">
                        <span class="value">{{ summaryData.inToday }}</span>
                    </div>
                    <p class="label">{{ $t('dashboard.totalInVehicle') }}</p>
                </div>
                <div class="summary-card">
                    <div class="icon-circle green-bg">
                        <span class="value">{{ summaryData.outToday }}</span>
                    </div>
                    <p class="label">{{ $t('dashboard.totalOutVehicle') }}</p>
                </div>
                <div class="summary-card">
                    <div class="icon-circle brown-bg">
                        <span class="value">{{ summaryData.inWarehouse }}</span>
                    </div>
                    <p class="label">{{ $t('dashboard.totalInWarehouse') }}</p>
                </div>
                <div class="summary-card">
                    <div class="icon-circle red-flash-bg">
                        <span class="value">{{ summaryData.fireAlert }}</span>
                    </div>
                    <p class="label">{{ $t('dashboard.totalFireAlert') }}</p>
                </div>
            </div>
        </div>

        <b-row cols="2" align-h="start">
            <b-col md="6">
                <div class="traffic-dashboard-container">
                    <div class="chart-section">
                        <h3 class="chart-title">
                            {{ $t('dashboard.monitoringVehicleTraffic') }}
                        </h3>
                        <div class="chart-header-right">
                            <v-select
                                v-model="searchForm.topAreaId"
                                :placeholder="$t('dashboard.allAreas')"
                                label="text"
                                :reduce="(type) => type.value"
                                :options="areaDropdown"
                            />
                        </div>
                        <div class="chart-with-border">
                            <apex-chart
                                type="bar"
                                :options="vehicleTrafficOptions"
                                :series="reactiveVehicleTrafficSeries"
                            ></apex-chart>
                        </div>
                    </div>
                </div>
            </b-col>

            <b-col md="6">
                <div class="traffic-dashboard-container">
                    <div class="chart-section">
                        <h3 class="chart-title">
                            {{ $t('dashboard.statisticsOfIntrusions') }}
                        </h3>
                        <div class="chart-header-right">
                            <v-select
                                v-model="searchForm.botAreaId"
                                :placeholder="$t('dashboard.allAreas')"
                                label="text"
                                :reduce="(type) => type.value"
                                :options="warehouseDropdown"
                            />
                        </div>
                        <div class="chart-with-border">
                            <apex-chart
                                type="bar"
                                :options="intrusionStatsOptions"
                                :series="reactiveIntrusionStatsSeries"
                            ></apex-chart>
                        </div>
                    </div>
                </div>
            </b-col>
        </b-row>
        <Loader :is-show="isLoading" />
    </div>
</template>

<script>
import VueApexCharts from 'vue-apexcharts'
import Loader from '@/components/Loader.vue'

export default {
    components: {
        'apex-chart': VueApexCharts,
        Loader,
    },
    data() {
        return {
            isLoading: false,
            searchForm: {
                topAreaId: null,
                botAreaId: null,
            },
            summaryData: {
                inUnit: 0,
                inToday: 0,
                outToday: 0,
                inWarehouse: 0,
                fireAlert: 0,
            },
            vehicleTrafficSeries: [
                { name: 'In', data: [] },
                { name: 'Out', data: [] },
            ],
            vehicleTrafficOptionsYMax: 60,
            intrusionStatsSeries: [
                { name: 'Nhân viên', data: [] },
                { name: 'Người lạ', data: [] },
            ],
            intrusionStatsOptionsYMax: 35,
            areaDropdown: [],
            warehouseDropdown: [],
            // Auto refresh data
            refreshInterval: 300000, // Default: 5 phút
            // refreshIntervalOptions: [
            //     { text: 'dashboard.polling.never', value: null },
            //     { text: 'dashboard.polling.every5seconds', value: 5000 },
            //     { text: 'dashboard.polling.every5minutes', value: 300000 },
            //     { text: 'dashboard.polling.every10minutes', value: 600000 },
            //     { text: 'dashboard.polling.every30minutes', value: 1800000 },
            //     { text: 'dashboard.polling.every1hour', value: 3600000 },
            //     { text: 'dashboard.polling.every2hours', value: 7200000 },
            // ],
            refreshTimer: null,
            lastUpdateTime: '',
        }
    },
    computed: {
        refreshIntervalOptions() {
            return [
                { text: this.$t('dashboard.polling.never'), value: null },
                {
                    text: this.$t('dashboard.polling.every5seconds'),
                    value: 5000,
                },
                {
                    text: this.$t('dashboard.polling.every5minutes'),
                    value: 300000,
                },
                {
                    text: this.$t('dashboard.polling.every10minutes'),
                    value: 600000,
                },
                {
                    text: this.$t('dashboard.polling.every30minutes'),
                    value: 1800000,
                },
                {
                    text: this.$t('dashboard.polling.every1hour'),
                    value: 3600000,
                },
                {
                    text: this.$t('dashboard.polling.every2hours'),
                    value: 7200000,
                },
            ]
        },
        reactiveVehicleTrafficSeries() {
            return [
                {
                    name: this.$t('dashboard.in'),
                    data: this.vehicleTrafficSeries[0].data,
                },
                {
                    name: this.$t('dashboard.out'),
                    data: this.vehicleTrafficSeries[1].data,
                },
            ]
        },
        reactiveIntrusionStatsSeries() {
            return [
                {
                    name: this.$t('dashboard.employee'),
                    data: this.intrusionStatsSeries[0].data,
                },
                {
                    name: this.$t('dashboard.stranger'),
                    data: this.intrusionStatsSeries[1].data,
                },
            ]
        },
        vehicleTrafficOptions() {
            const hours = Array.from({ length: 24 }, (_, i) => i)
            return {
                chart: {
                    type: 'bar',
                    height: '300',
                    stacked: false,
                    toolbar: { show: false },
                    fontFamily: 'Montserrat, Helvetica, Arial, serif',
                },
                plotOptions: {
                    bar: {
                        horizontal: false,
                        columnWidth: '70%',
                        dataLabels: {
                            position: 'top',
                        },
                    },
                },
                dataLabels: {
                    enabled: true,
                    formatter(val) {
                        return val > 0 ? val : ''
                    },
                    offsetY: -20,
                    style: {
                        fontSize: '12px',
                        colors: ['#304758'],
                        fontFamily: 'Montserrat, Helvetica, Arial, serif',
                    },
                },
                stroke: {
                    show: true,
                    width: 1,
                    colors: ['#fff'],
                },
                xaxis: {
                    categories: hours,
                    tickPlacement: 'on',
                    title: {
                        text: this.$t('dashboard.hoursOfDay'),
                        style: {
                            fontWeight: 'normal',
                            fontFamily: 'Montserrat, Helvetica, Arial, serif',
                        },
                    },
                    labels: {
                        style: {
                            fontFamily: 'Montserrat, Helvetica, Arial, serif',
                        },
                    },
                },
                yaxis: {
                    title: {
                        text: this.$t('dashboard.quantity'),
                        style: {
                            fontWeight: 'normal',
                            fontFamily: 'Montserrat, Helvetica, Arial, serif',
                        },
                    },
                    labels: {
                        style: {
                            fontFamily: 'Montserrat, Helvetica, Arial, serif',
                        },
                    },
                    min: 0,
                    max: this.vehicleTrafficOptionsYMax,
                },
                legend: {
                    position: 'bottom',
                    fontFamily: 'Montserrat, Helvetica, Arial, serif',
                    labels: {
                        colors: '#333',
                        useSeriesColors: false,
                        style: {
                            fontFamily: 'Montserrat, Helvetica, Arial, serif',
                        },
                    },
                },
                grid: {
                    show: true,
                    yaxis: {
                        lines: { show: true },
                    },
                },
            }
        },
        intrusionStatsOptions() {
            const hours = Array.from({ length: 24 }, (_, i) => i)
            return {
                chart: {
                    type: 'bar',
                    height: '300',
                    stacked: false,
                    toolbar: { show: false },
                    fontFamily: 'Montserrat, Helvetica, Arial, serif',
                },
                plotOptions: {
                    bar: {
                        horizontal: false,
                        columnWidth: '70%',
                        dataLabels: {
                            position: 'top',
                        },
                    },
                },
                dataLabels: {
                    enabled: true,
                    formatter(val) {
                        return val > 0 ? val : ''
                    },
                    offsetY: -20,
                    style: {
                        fontSize: '12px',
                        colors: ['#304758'],
                        fontFamily: 'Montserrat, Helvetica, Arial, serif',
                    },
                },
                stroke: {
                    show: true,
                    width: 1,
                    colors: ['#fff'],
                },
                xaxis: {
                    categories: hours,
                    tickPlacement: 'on',
                    title: {
                        text: this.$t('dashboard.hoursOfDay'),
                        style: {
                            fontWeight: 'normal',
                            fontFamily: 'Montserrat, Helvetica, Arial, serif',
                        },
                    },
                    labels: {
                        style: {
                            fontFamily: 'Montserrat, Helvetica, Arial, serif',
                        },
                    },
                },
                yaxis: {
                    title: {
                        text: this.$t('dashboard.quantity'),
                        style: {
                            fontWeight: 'normal',
                            fontFamily: 'Montserrat, Helvetica, Arial, serif',
                        },
                    },
                    labels: {
                        style: {
                            fontFamily: 'Montserrat, Helvetica, Arial, serif',
                        },
                    },
                    min: 0,
                    max: this.intrusionStatsOptionsYMax,
                },
                legend: {
                    position: 'bottom',
                    fontFamily: 'Montserrat, Helvetica, Arial, serif',
                    labels: {
                        colors: '#333',
                        useSeriesColors: false,
                        style: {
                            fontFamily: 'Montserrat, Helvetica, Arial, serif',
                        },
                    },
                },
                grid: {
                    show: true,
                    yaxis: {
                        lines: { show: true },
                    },
                },
            }
        },
    },
    watch: {
        'searchForm.topAreaId': {
            handler(newVal) {
                this.getDashboardData(newVal, true)
            },
        },
        'searchForm.botAreaId': {
            handler(newVal) {
                this.getDashboardData(newVal, false)
            },
        },
        vehicleTrafficSeries: {
            handler(newVal) {
                const allData = newVal.flatMap((series) => series.data)
                if (allData.length === 0) {
                    this.vehicleTrafficOptionsYMax = 60
                    return
                }

                const maxVal = Math.max(...allData)
                const newMax = Math.ceil((maxVal * 1.2) / 10) * 10
                this.vehicleTrafficOptionsYMax = newMax > 0 ? newMax : 60
            },
            deep: true,
        },
        intrusionStatsSeries: {
            handler(newVal) {
                const allData = newVal.flatMap((series) => series.data)
                if (allData.length === 0) {
                    this.intrusionStatsOptionsYMax = 35
                    return
                }

                const maxVal = Math.max(...allData)
                const newMax = Math.ceil((maxVal * 1.2) / 10) * 10
                this.intrusionStatsOptionsYMax = newMax > 0 ? newMax : 35
            },
            deep: true,
        },
        refreshInterval: {
            handler() {
                this.setupAutoRefresh()
            },
        },
    },
    async created() {
        await this.getDashboardData()
        await this.getAreaDropdown()
        this.updateLastUpdateTime()
        this.setupAutoRefresh()
    },
    beforeDestroy() {
        // Clear timer khi component bị destroy
        if (this.refreshTimer) {
            clearInterval(this.refreshTimer)
        }
    },
    methods: {
        async getDashboardData(areaId, fillTraffic, showLoader = true) {
            this.isLoading = showLoader
            this.$services
                .get(`/event/dashboard?areaId=${areaId || ''}`)
                .then((res) => {
                    if (res && res.data) {
                        const { statistic, chart } = res.data.data
                        if (fillTraffic === undefined) {
                            this.summaryData = {
                                inUnit: statistic.totalVehicle || 0,
                                inToday: statistic.totalInVehicle || 0,
                                outToday: statistic.totalOutVehicle || 0,
                                inWarehouse: statistic.totalInWarehouse || 0,
                                fireAlert: statistic.totalFireAlert || 0,
                            }
                            this.fillTrafficChartData(chart)
                            this.fillWarehouseChartData(chart)
                        } else if (!fillTraffic) {
                            this.fillWarehouseChartData(chart)
                        } else {
                            this.fillTrafficChartData(chart)
                        }
                        this.updateLastUpdateTime()
                    } else {
                        console.error('No data received from dashboard API')
                    }
                    this.isLoading = false
                })
                .catch((error) => {
                    console.error('Error fetching dashboard data:', error)
                    this.isLoading = false
                })
        },
        fillWarehouseChartData(chartData) {
            this.intrusionStatsSeries = [
                {
                    name: 'Nhân viên',
                    data: chartData.employeeInList,
                },
                {
                    name: 'Người lạ',
                    data: chartData.strangerInList,
                },
            ]
        },
        fillTrafficChartData(chartData) {
            this.vehicleTrafficSeries = [
                {
                    name: 'In',
                    data: chartData.inDataList,
                },
                {
                    name: 'Out',
                    data: chartData.outDataList,
                },
            ]
        },
        async getAreaDropdown() {
            this.$services.get(`/areas/dropdown`).then((res) => {
                if (res && res.data) {
                    this.areaDropdown = res.data.data
                } else {
                    console.error('No data received from dashboard API')
                }
            })
            this.$services
                .get(`/areas/dropdown?warehouseOnly=true`)
                .then((res) => {
                    if (res && res.data) {
                        this.warehouseDropdown = res.data.data
                    } else {
                        console.error('No data received from dashboard API')
                    }
                })
        },
        setupAutoRefresh() {
            // Clear timer cũ nếu có
            if (this.refreshTimer) {
                clearInterval(this.refreshTimer)
                this.refreshTimer = null
            }

            // Nếu refreshInterval là null hoặc 0, không setup timer
            if (!this.refreshInterval) {
                return
            }

            // Setup timer mới
            this.refreshTimer = setInterval(() => {
                this.refreshData()
            }, this.refreshInterval)
        },
        async refreshData() {
            // Refresh traffic chart nếu có area được chọn
            if (this.searchForm.topAreaId) {
                await this.getDashboardData(
                    this.searchForm.topAreaId,
                    true,
                    false
                )
            }

            // Refresh warehouse chart nếu có area được chọn
            if (this.searchForm.botAreaId) {
                await this.getDashboardData(
                    this.searchForm.botAreaId,
                    false,
                    false
                )
            }

            // Nếu không có area nào được chọn, refresh toàn bộ
            if (!this.searchForm.topAreaId && !this.searchForm.botAreaId) {
                await this.getDashboardData(null, null, false)
            }
        },
        updateLastUpdateTime() {
            const now = new Date()
            const hours = String(now.getHours()).padStart(2, '0')
            const minutes = String(now.getMinutes()).padStart(2, '0')
            const seconds = String(now.getSeconds()).padStart(2, '0')
            this.lastUpdateTime = `${hours}:${minutes}:${seconds}`
        },
    },
}
</script>

<style scoped>
/* Auto Refresh Section */
.auto-refresh-section {
    display: flex;
    justify-content: end;
    align-items: center;
    padding: 15px 20px;
    /* background-color: #f8f9fa; */
    /* border-radius: 8px; */
    /* margin-bottom: 20px; */
    /* border: 1px solid #e0e0e0; */
}

.refresh-info {
    display: flex;
    align-items: center;
    font-size: 14px;
}

.refresh-label {
    color: #666;
    font-weight: 500;
    margin-right: 8px;
}

.refresh-time {
    font-weight: bold;
    font-size: 15px;
}

.refresh-control {
    margin-left: 20px;
    min-width: 200px;
}

.refresh-dropdown {
    font-size: 14px;
}

/* Global Container & Responsive Setup */
.traffic-dashboard-container {
    max-width: auto;
    margin: 0 auto;
    padding: 20px;
    background-color: #fff;
    border-radius: 8px;
    margin: 25px 0px 0px 0px;
}

/* Data Summary Section (Top Cards) */
.data-summary-section {
    font-family: 'Montserrat', Helvetica, Arial, serif;
    display: flex;
    justify-content: space-around;
    flex-wrap: wrap;
    margin-bottom: 20px;
}

.summary-card {
    text-align: center;
    margin: 10px 15px;
    flex: 1;
    min-width: 120px;
}

.icon-circle {
    width: 70px;
    height: 70px;
    border-radius: 50%;
    margin: 0 auto 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 18px;
    font-weight: bold;
    color: white;
    position: relative;
}

.icon-circle:before {
    content: '';
}

.icon-circle .value {
    z-index: 10;
}

.orange-bg {
    background-color: #f7a049;
}

.red-bg {
    background-color: #dc3545;
}

.green-bg {
    background-color: #28a745;
}

.brown-bg {
    background-color: #a0522d;
}

.red-flash-bg {
    background-color: #ff0000;
}

.label {
    font-size: 14px;
    font-weight: 500;
    text-transform: uppercase;
}

.arrow {
    position: absolute;
    width: 0;
    height: 0;
    border: solid transparent;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
}

.arrow.up {
    border-bottom-color: white;
    border-width: 15px;
    margin-top: -10px;
}

.arrow.down {
    border-top-color: white;
    border-width: 15px;
    margin-top: 10px;
}

/* Chart Sections */
.chart-section {
    position: relative;
    padding: 20px 0;
}

.chart-title {
    text-align: center;
    font-size: 16px;
    font-weight: 500;
    color: #333;
    margin-bottom: 40px;
    text-transform: uppercase;
}

.chart-header-right {
    position: absolute;
    top: 40px;
    right: 0;
    width: 30%;
}

.chart-with-border {
    border: 1px solid #e0e0e0;
    border-radius: 8px;
    padding: 15px;
    background-color: #fafafa;
    margin: 20px 0;
}

.separator {
    border: 0;
    height: 1px;
    background: #ccc;
    margin: 20px 0;
}

/* Media Query cho Responsive */
@media (max-width: 768px) {
    .auto-refresh-section {
        flex-direction: column;
        gap: 10px;
    }

    .refresh-control {
        width: 100%;
    }

    .data-summary-section {
        justify-content: center;
    }

    .summary-card {
        flex-basis: 30%;
        margin: 10px 5px;
    }
}

@media (max-width: 480px) {
    .summary-card {
        flex-basis: 45%;
    }

    .chart-header-right {
        width: 26%;
        position: static;
        text-align: right;
        margin-bottom: 10px;
    }
}
</style>
