<template>
    <b-card v-if="data" no-body class="mb-0">
        <!-- apex chart -->
        <vue-apex-charts
            v-if="data.performance[0]"
            type="radialBar"
            height="245"
            :options="goalOverviewRadialBar"
            :series="data.performance"
        />
        <b-row class="text-center mx-0">
            <b-col
                cols="6"
                class="border-top border-right d-flex align-items-between flex-column py-1"
            >
                <b-card-text class="text-muted mb-0 text-uppercase">
                    Thành phẩm
                </b-card-text>
                <h3 class="font-weight-bolder mb-0 text-success">
                    {{ data.totalSuccess }}
                </h3>
            </b-col>

            <b-col
                cols="6"
                class="border-top d-flex align-items-between flex-column py-1"
            >
                <b-card-text class="text-muted mb-0 text-uppercase">
                    SP lỗi
                </b-card-text>
                <h3 class="font-weight-bolder mb-0 text-danger">
                    {{ data.totalError }}
                </h3>
            </b-col>
        </b-row>
    </b-card>
</template>

<script>
import VueApexCharts from 'vue-apexcharts'
import { $themeColors, $z121Config } from '@themeConfig'

const $backgroundColor = '#ebe9f1'
const $goalStrokeColor = '#51e5a8'
export default {
    components: {
        VueApexCharts,
    },
    props: {
        data: {
            type: Object,
            default: () => {},
        },
    },
    data() {
        return {
            goalOverviewRadialBar: {
                chart: {
                    height: 245,
                    type: 'radialBar',
                    sparkline: {
                        enabled: true,
                    },
                    dropShadow: {
                        enabled: true,
                        blur: 3,
                        left: 1,
                        top: 1,
                        opacity: 0.1,
                    },
                },
                colors: [$goalStrokeColor],
                plotOptions: {
                    radialBar: {
                        offsetY: -10,
                        startAngle: -150,
                        endAngle: 150,
                        hollow: {
                            size: '77%',
                        },
                        track: {
                            background: $backgroundColor,
                            strokeWidth: '50%',
                        },
                        dataLabels: {
                            name: {
                                show: false,
                            },
                            value: {
                                fontSize: '2.86rem',
                                fontWeight: '600',
                            },
                        },
                    },
                },
                fill: {
                    type: 'gradient',
                    gradient: {
                        shade: 'dark',
                        type: 'horizontal',
                        shadeIntensity: 0.5,
                        gradientToColors: [$themeColors.success],
                        inverseColors: true,
                        opacityFrom: 1,
                        opacityTo: 1,
                        stops: [0, 100],
                    },
                },
                stroke: {
                    lineCap: 'round',
                },
                grid: {
                    padding: {
                        bottom: 30,
                    },
                },
            },
        }
    },
    created() {},
}
</script>
