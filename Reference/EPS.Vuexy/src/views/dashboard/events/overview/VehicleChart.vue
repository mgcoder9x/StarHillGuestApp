<template>
    <b-card v-if="data" body-class="pb-50" class="h-100 w-100">
        <h4>{{ $t('Events.OverView.PeopleAccessControl') }}</h4>
        <vue-apex-charts
            ref="myChart"
            height="100%"
            :options="statisticsOrder.chartOptions"
            :series="processedSeries"
        />
    </b-card>
</template>

<script>
import { BCard } from 'bootstrap-vue'
import VueApexCharts from 'vue-apexcharts'
import { $themeColors } from '@themeConfig'

export default {
    components: {
        BCard,
        VueApexCharts,
    },
    props: {
        data: {
            type: Object,
            default: () => ({
                series: [
                    {
                        name: 'Vào',
                        data: [
                            70, 52, 45, 60, 65, 58, 72, 66, 55, 59, 61, 50, 48,
                            62, 71, 63, 69, 57, 64, 53, 49, 68, 54, 60,
                        ],
                    },
                    {
                        name: 'Ra',
                        data: [
                            30, 20, 40, 35, 25, 28, 22, 26, 35, 29, 31, 20, 18,
                            32, 41, 33, 39, 27, 34, 23, 19, 38, 24, 30,
                        ],
                    },
                ],
            }),
        },
        categories: {
            type: Array,
            default: () => Array.from({ length: 24 }, (_, i) => i),
        },
    },
    data() {
        return {
            statisticsOrder: {
                chartOptions: {
                    chart: {
                        type: 'bar',
                        stacked: true,
                        toolbar: { show: false },
                    },
                    grid: {
                        show: true,
                        borderColor: '#f1f1f1',
                        padding: { left: 10, right: 10, top: 0, bottom: 0 },
                    },
                    plotOptions: {
                        bar: {
                            horizontal: false,
                            columnWidth: '35%',
                            startingShape: 'rounded',
                            endingShape: 'rounded',
                        },
                    },
                    legend: {
                        show: true,
                        position: 'top',
                        horizontalAlign: 'right',
                        fontSize: '14px',
                    },
                    dataLabels: {
                        enabled: false,
                    },
                    colors: [$themeColors.primary, $themeColors.warning], // "Vào" là xanh, "Ra" là cam
                    xaxis: {
                        categories: this.categories,
                        labels: {
                            show: true,
                            style: {
                                colors: '#6e6b7b',
                                fontSize: '12px',
                            },
                        },
                        axisBorder: { show: true },
                        axisTicks: { show: true },
                    },
                    yaxis: {
                        show: true,
                        labels: {
                            formatter: (val) => Math.abs(val),
                            style: {
                                colors: '#6e6b7b',
                                fontSize: '12px',
                            },
                        },
                        title: {
                            text: this.$t('Events.OverView.NumberOfTurns'),
                            style: {
                                fontSize: '13px',
                                color: '#6e6b7b',
                            },
                        },
                    },
                    tooltip: {
                        enabled: true,
                        y: {
                            formatter: (val) => Math.abs(val),
                        },
                    },
                },
            },
        }
    },
    computed: {
        processedSeries() {
            // Chuyển dữ liệu Ra thành số âm để biểu đồ hiển thị xuống dưới
            return this.data.series.map((s) => ({
                ...s,
                data:
                    s.name === 'Events.OverView.OUT'
                        ? s.data.map((val) => -val)
                        : s.data,
                name: this.$t(s.name),
            }))
        },
    },
    watch: {
        // eslint-disable-next-line
        '$i18n.locale'() {
            this.$nextTick(() => {
                if (this.$refs.myChart) {
                    this.statisticsOrder.chartOptions.yaxis.title.text =
                        this.$t('Events.OverView.NumberOfTurns')
                    this.$refs.myChart.updateOptions({
                        ...this.statisticsOrder.chartOptions,
                    })
                }
            })
        },
    },
}
</script>
