<template>
    <b-card no-body>
        <b-card-header>
            <b-card-title>
                {{ $t('Events.VehicleMonitor.RateAccessComplex') }}
            </b-card-title>
        </b-card-header>
        <b-card-body>
            <VueApexCharts
                type="pie"
                :options="chartOptions"
                :series="series"
            />
        </b-card-body>
    </b-card>
</template>

<script>
import VueApexCharts from 'vue-apexcharts'

export default {
    components: {
        VueApexCharts,
    },
    props: {
        series: {
            type: Array,
            default: () => [],
        },
        labels: {
            type: Array,
            default: () => [],
        },
    },

    data() {
        return {
            chartOptions: {
                chart: {
                    type: 'pie',
                },
                labels: this.labels,
                legend: {
                    position: 'bottom',
                },
                responsive: [
                    {
                        breakpoint: 480,
                        options: {
                            chart: {
                                width: 300,
                            },
                            legend: {
                                position: 'bottom',
                            },
                        },
                    },
                ],
            },
        }
    },
    watch: {
        labels: {
            immediate: true,
            handler(newLabels) {
                this.chartOptions = {
                    ...this.chartOptions,
                    labels: newLabels.map(seat =>
                    this.formatVehicChart(seat, this.$i18n.locale)
                    ),
                }
            },
        },
        '$i18n.locale'(newLocale) {
            this.updateChartLabels(this.labels, newLocale)
        },
    },
    methods:{
        formatVehicChart(totalSeats, locale) {
            if (locale === 'vi') {
                return `Xe ${totalSeats} chỗ`
            } else if (locale === 'en') {
                return `${totalSeats} seat vehicle`
            }
        },
        updateChartLabels(labels, locale) {
            this.chartOptions = {
                ...this.chartOptions,
                labels: labels.map(seat => this.formatVehicChart(seat, locale)),
            }
        },
    },
}
</script>
