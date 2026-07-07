<template>
    <b-card no-body class="card-browser-states mb-1">
        <b-card-header class="px-0">
            <div>
                <b-card-title class="text-uppercase font-weight-bolder">
                    {{ data.name }}
                </b-card-title>
            </div>
            <b-dropdown
                variant="link"
                no-caret
                class="chart-dropdown"
                toggle-class="p-0"
            >
                <template #button-content>
                    <feather-icon
                        icon="MoreVerticalIcon"
                        size="18"
                        class="text-body cursor-pointer"
                    />
                </template>
                <b-dropdown-item @click="data.viewMode = VIEW_MODE.NUMBER">
                    Số lượng
                </b-dropdown-item>
                <b-dropdown-item @click="data.viewMode = VIEW_MODE.PERCENT">
                    Tỉ lệ
                </b-dropdown-item>
            </b-dropdown>
        </b-card-header>
        <b-card-body class="p-0 mb-0">
            <div
                v-for="classFw in data._children"
                :key="classFw.label"
                class="browser-states"
            >
                <b-media no-body>
                    <b-media-body>
                        <h6
                            class="align-self-center my-auto font-weight-bolder"
                        >
                            {{ classFw.name }}
                        </h6>
                    </b-media-body>
                </b-media>
                <div
                    v-if="
                        data.viewMode == VIEW_MODE.NUMBER ||
                        data.viewMode == VIEW_MODE.ALL
                    "
                    class="d-flex justify-content-end mr-2"
                    style="width: 50%"
                >
                    <div class="font-weight-bold text-success mr-3">
                        {{
                            classFw.successProduct ? classFw.successProduct : 0
                        }}
                    </div>
                    <div class="font-weight-bold text-danger">
                        {{ classFw.errorProduct ? classFw.errorProduct : 0 }}
                    </div>
                </div>
                <div
                    v-if="
                        data.viewMode == VIEW_MODE.PERCENT ||
                        data.viewMode == VIEW_MODE.ALL
                    "
                    class="d-flex justify-content-end mr-2"
                    style="width: 50%"
                >
                    <span
                        class="font-weight-bold text-body-heading mr-1"
                        :class="{
                            'text-success': classFw.performance >= 80,
                            'text-danger': classFw.performance < 80,
                        }"
                    >
                        {{ classFw.performance ? classFw.performance : 0 }} %
                    </span>
                    <vue-apex-charts
                        type="radialBar"
                        height="30"
                        width="30"
                        :options="chart.options"
                        :series="[
                            classFw.performance ? classFw.performance : 0,
                        ]"
                    />
                </div>
            </div>
        </b-card-body>
        <!--/ body -->
    </b-card>
</template>

<script>
/* eslint-disable */
import VueApexCharts from 'vue-apexcharts'
import { $themeColors } from '@themeConfig'
/* eslint-disable global-require */
const $trackBgColor = '#e9ecef'
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
        const VIEW_MODE = {
            ALL: 0,
            NUMBER: 1,
            PERCENT: 2,
        }
        return {
            VIEW_MODE,
            chartData: [],
            chartClone: {},
            chartColor: [
                $themeColors.primary,
                $themeColors.warning,
                $themeColors.secondary,
                $themeColors.info,
                $themeColors.danger,
            ],
            chartSeries: [54.4, 6.1, 14.6, 4.2, 8],
            browserData: [
                {
                    browserImg: require('@/assets/images/icons/google-chrome.png'),
                    name: 'Google Chrome',
                    usage: '54.4%',
                },
                {
                    browserImg: require('@/assets/images/icons/mozila-firefox.png'),
                    name: 'Mozila Firefox',
                    usage: '6.1%',
                },
                {
                    browserImg: require('@/assets/images/icons/apple-safari.png'),
                    name: 'Apple Safari',
                    usage: '14.6%',
                },
                {
                    browserImg: require('@/assets/images/icons/internet-explorer.png'),
                    name: 'Internet Explorer',
                    usage: '4.2%',
                },
                {
                    browserImg: require('@/assets/images/icons/opera.png'),
                    name: 'Opera Mini',
                    usage: '8.%',
                },
            ],
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
                    colors: ['#28c76f'],
                    plotOptions: {
                        radialBar: {
                            hollow: {
                                size: '22%',
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
        }
    },
    created() {
        // for (let i = 0; i < this.browserData.length; i += 1) {
        //   const chartClone = JSON.parse(JSON.stringify(this.chart));
        //   chartClone.options.colors[0] = this.chartColor[i];
        //   chartClone.series[0] = this.chartSeries[i];
        //   this.chartData.push(chartClone);
        // }
    },
}
</script>
<style lang="scss" scoped>
.card-browser-states .browser-states:not(:first-child) {
    margin-top: 1rem;
}
</style>
