<template>
    <div class="chart-section">
        <!-- ── Filter bar ─────────────────────────────────────────────────── -->
        <div class="chart-filterbar">
            <div class="cf-group">
                <span class="cf-label">
                    {{ $t('Meiko.Dashboard.Filters.DateFrom') }}
                </span>
                <input
                    v-model="dateFrom"
                    type="date"
                    class="cf-input"
                    :max="dateTo"
                />
            </div>
            <div class="cf-group">
                <span class="cf-label">
                    {{ $t('Meiko.Dashboard.Filters.DateTo') }}
                </span>
                <input
                    v-model="dateTo"
                    type="date"
                    class="cf-input"
                    :min="dateFrom"
                    :max="todayStr"
                />
            </div>
            <button
                class="cf-btn"
                :disabled="isLoading"
                @click="loadChartData"
            >
                <Icon icon="lucide:search" width="13" height="13" />
                {{ $t('Meiko.Dashboard.Filters.Apply') }}
            </button>
            <span v-if="rangeLabel" class="cf-info">
                {{ $t('Meiko.Dashboard.Filters.Viewing') }}:
                <strong>{{ rangeLabel }}</strong>
            </span>
            <span v-if="isLoading" class="cf-loading">
                <b-spinner small variant="primary" />
            </span>
        </div>

        <!-- ── Charts grid ───────────────────────────────────────────────── -->
        <div class="chart-grid">
            <!-- Chart 1: So sánh hiệu suất (bar) -->
            <div class="chart-card">
                <div class="chart-card-head">
                    <span class="chart-card-title">
                        <Icon icon="lucide:bar-chart-2" width="13" height="13" class="mr-1" />
                        {{ $t('Meiko.Dashboard.Chart.PerformanceCompareTitle') }}
                    </span>
                    <div class="chart-legend">
                        <span
                            v-for="s in perfSeries"
                            :key="s.name"
                            class="cleg-item"
                        >
                            <span
                                class="cleg-dot"
                                :style="{ background: s.color }"
                            ></span>
                            {{ s.name }}
                        </span>
                    </div>
                </div>
                <div class="chart-card-body">
                    <div v-if="isLoading || !chartResult" class="chart-skeleton">
                        <div class="chart-skeleton-bar" v-for="i in 7" :key="i" :style="{ height: (30 + i * 8) + 'px' }" />
                    </div>
                    <div v-else-if="!perfSeries.length" class="chart-empty">
                        <Icon icon="lucide:bar-chart" width="28" height="28" />
                        <span>{{ $t('Meiko.Dashboard.Chart.NoData') }}</span>
                    </div>
                    <vue-apex-charts
                        v-else
                        type="bar"
                        height="220"
                        :options="perfChartOptions"
                        :series="perfSeriesForApex"
                    />
                </div>
            </div>

            <!-- Chart 2: Tỷ lệ lỗi theo ngày (line) -->
            <div class="chart-card">
                <div class="chart-card-head">
                    <span class="chart-card-title">
                        <Icon icon="lucide:trending-down" width="13" height="13" class="mr-1" />
                        {{ $t('Meiko.Dashboard.Chart.ErrorRateTitle') }}
                    </span>
                    <div class="chart-legend">
                        <span
                            v-for="s in errSeries"
                            :key="s.name"
                            class="cleg-item"
                        >
                            <span
                                class="cleg-line"
                                :style="{ background: s.color }"
                            ></span>
                            {{ s.name }}
                        </span>
                    </div>
                </div>
                <div class="chart-card-body">
                    <div v-if="isLoading || !chartResult" class="chart-skeleton">
                        <div class="chart-skeleton-line" />
                    </div>
                    <div v-else-if="!errSeries.length" class="chart-empty">
                        <Icon icon="lucide:trending-down" width="28" height="28" />
                        <span>{{ $t('Meiko.Dashboard.Chart.NoData') }}</span>
                    </div>
                    <vue-apex-charts
                        v-else
                        type="line"
                        height="220"
                        :options="errChartOptions"
                        :series="errSeriesForApex"
                    />
                </div>
            </div>
        </div>
    </div>
</template>

<script>
import VueApexCharts from 'vue-apexcharts'
import dashboardMeikoService from '@/services/meiko.service'

// Palette matching the design tokens
const PALETTE = [
    '#1a6cf6', // blue
    '#0d9e5c', // green
    '#e06318', // orange
    '#d93a3a', // red
    '#0a8a8a', // teal
    '#8fa3bf', // gray
]

function today() {
    return new Date().toISOString().split('T')[0]
}
function daysAgo(n) {
    const d = new Date()
    d.setDate(d.getDate() - n)
    return d.toISOString().split('T')[0]
}
export default {
    name: 'ChartSection',
    components: { VueApexCharts },

    data() {
        return {
            dateFrom:    daysAgo(6),
            dateTo:      today(),
            todayStr:    today(),
            isLoading:   false,
            chartResult: null,   // { dates, performance, errorRate }
            error:       null,
        }
    },

    computed: {
        rangeLabel() {
            if (!this.dateFrom || !this.dateTo) return null
            return `${this.formatLabel(this.dateFrom)} — ${this.formatLabel(this.dateTo)}`
        },

        // Map API series to { name, color, data }
        perfSeries() {
            return (this.chartResult?.performance || []).map((s, i) => ({
                ...s,
                color: PALETTE[i % PALETTE.length],
            }))
        },
        errSeries() {
            return (this.chartResult?.errorRate || []).map((s, i) => ({
                ...s,
                color: PALETTE[i % PALETTE.length],
            }))
        },

        // ApexCharts expects: [{ name, data }]
        perfSeriesForApex() {
            return this.perfSeries.map((s) => ({ name: s.name, data: s.data }))
        },
        errSeriesForApex() {
            return this.errSeries.map((s) => ({ name: s.name, data: s.data }))
        },

        xLabels() {
            return this.chartResult?.dates || []
        },

        perfChartOptions() {
            return {
                chart: {
                    type: 'bar',
                    toolbar: { show: false },
                    fontFamily: "'Be Vietnam Pro', sans-serif",
                    animations: { enabled: true, speed: 400 },
                },
                colors: this.perfSeries.map((s) => s.color),
                plotOptions: {
                    bar: { borderRadius: 4, columnWidth: '60%' },
                },
                dataLabels: { enabled: false },
                stroke: { show: false },
                xaxis: {
                    categories: this.xLabels,
                    labels: {
                        style: { fontSize: '10px', fontFamily: "'Be Vietnam Pro'" },
                        rotate: -30,
                    },
                    axisBorder: { show: false },
                    axisTicks:  { show: false },
                },
                yaxis: {
                    title: {
                        text: this.$t('Meiko.Dashboard.Chart.CompletedSteps'),
                        style: { fontSize: '9px', color: '#7e92b0', fontFamily: "'Be Vietnam Pro'" },
                    },
                    labels: { style: { fontSize: '10px' } },
                },
                grid: { borderColor: '#f0f3f8', strokeDashArray: 3 },
                legend: { show: false },
                tooltip: {
                    style: { fontFamily: "'Be Vietnam Pro'" },
                    y: {
                        formatter: (val) =>
                            `${val} ${this.$t('Meiko.Dashboard.StepUnit')}`,
                    },
                },
            }
        },

        errChartOptions() {
            return {
                chart: {
                    type: 'line',
                    toolbar: { show: false },
                    fontFamily: "'Be Vietnam Pro', sans-serif",
                    animations: { enabled: true, speed: 400 },
                },
                colors: this.errSeries.map((s) => s.color),
                stroke: { curve: 'smooth', width: 2 },
                markers: { size: 4, strokeWidth: 0 },
                fill: {
                    type: 'gradient',
                    gradient: {
                        shadeIntensity: 1,
                        opacityFrom: 0.15,
                        opacityTo:   0.02,
                    },
                },
                dataLabels: { enabled: false },
                xaxis: {
                    categories: this.xLabels,
                    labels: {
                        style: { fontSize: '10px', fontFamily: "'Be Vietnam Pro'" },
                        rotate: -30,
                    },
                    axisBorder: { show: false },
                    axisTicks:  { show: false },
                },
                yaxis: {
                    min: 0,
                    max: 100,
                    title: {
                        text: this.$t('Meiko.Dashboard.Chart.ErrorRateAxisTitle'),
                        style: { fontSize: '9px', color: '#7e92b0', fontFamily: "'Be Vietnam Pro'" },
                    },
                    labels: {
                        style:     { fontSize: '10px' },
                        formatter: (val) => `${val}%`,
                    },
                },
                grid: { borderColor: '#f0f3f8', strokeDashArray: 3 },
                legend: { show: false },
                tooltip: {
                    style: { fontFamily: "'Be Vietnam Pro'" },
                    y: { formatter: (val) => `${val}%` },
                },
            }
        },
    },

    mounted() {
        this.loadChartData()
    },

    methods: {
        formatLabel(dateStr) {
            return new Intl.DateTimeFormat(this.$i18n.locale || 'vi', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
            }).format(new Date(dateStr))
        },
        async loadChartData() {
            this.isLoading = true
            this.error     = null
            try {
                this.chartResult = await dashboardMeikoService.getChartData(
                    this.dateFrom,
                    this.dateTo
                )
            } catch (err) {
                this.error = err.message
                this.$emit('error', err.message)
            } finally {
                this.isLoading = false
            }
        },
    },
}
</script>

<style scoped>
/* ── Filter bar ──────────────────────────────────────────────────────── */
.chart-section { display: flex; flex-direction: column; gap: 12px; }

.chart-filterbar {
    background: #fff;
    border: 1px solid #e2e8f2;
    border-radius: 8px;
    padding: 10px 14px;
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
    box-shadow: 0 1px 4px rgba(13,20,33,.05);
}

.cf-group {
    display: flex;
    align-items: center;
    gap: 6px;
}

.cf-label {
    font-size: 11px;
    font-weight: 700;
    color: #3d4f6b;
    white-space: nowrap;
}

.cf-input {
    font-size: 11px;
    font-weight: 600;
    color: #0d1421;
    border: 1px solid #c9d4e8;
    border-radius: 6px;
    padding: 5px 9px;
    background: #fff;
    outline: none;
    cursor: pointer;
    transition: border-color .15s;
    font-family: inherit;
}
.cf-input:focus { border-color: #1a6cf6; }

.cf-btn {
    font-size: 11px;
    font-weight: 700;
    color: #fff;
    background: #1a6cf6;
    border: none;
    border-radius: 6px;
    padding: 5px 14px;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 5px;
    transition: background .15s;
}
.cf-btn:hover:not(:disabled) { background: #1456d0; }
.cf-btn:disabled { opacity: .6; cursor: not-allowed; }

.cf-info {
    margin-left: auto;
    font-size: 10px;
    color: #7e92b0;
}
.cf-info strong { color: #3d4f6b; }
.cf-loading { margin-left: 4px; }

/* ── Chart grid ──────────────────────────────────────────────────────── */
.chart-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
}
@media (max-width: 1024px) { .chart-grid { grid-template-columns: 1fr; } }

.chart-card {
    background: #fff;
    border: 1px solid #e2e8f2;
    border-radius: 10px;
    box-shadow: 0 1px 4px rgba(13,20,33,.05);
    overflow: hidden;
}

.chart-card-head {
    padding: 11px 14px 9px;
    border-bottom: 1px solid #e2e8f2;
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 6px;
}

.chart-card-title {
    font-size: 12px;
    font-weight: 700;
    color: #0d1421;
    display: flex;
    align-items: center;
}

.chart-card-body {
    padding: 10px 8px 6px;
}

/* Legend */
.chart-legend {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
}
.cleg-item {
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 10px;
    color: #3d4f6b;
    font-weight: 600;
}
.cleg-dot {
    width: 10px;
    height: 10px;
    border-radius: 2px;
    flex-shrink: 0;
}
.cleg-line {
    width: 18px;
    height: 3px;
    border-radius: 2px;
    flex-shrink: 0;
}

/* Skeleton */
.chart-skeleton {
    display: flex;
    align-items: flex-end;
    justify-content: space-around;
    gap: 6px;
    height: 220px;
    padding: 16px;
}
.chart-skeleton-bar {
    flex: 1;
    background: linear-gradient(90deg, #edf1f7 25%, #e2e8f2 50%, #edf1f7 75%);
    background-size: 200% 100%;
    border-radius: 4px;
    animation: shimmer 1.4s infinite;
}
.chart-skeleton-line {
    width: 100%;
    height: 220px;
    background: linear-gradient(90deg, #edf1f7 25%, #e2e8f2 50%, #edf1f7 75%);
    background-size: 200% 100%;
    border-radius: 6px;
    animation: shimmer 1.4s infinite;
}
@keyframes shimmer {
    0%   { background-position: 200% 0; }
    100% { background-position: -200% 0; }
}

/* Empty state */
.chart-empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 8px;
    height: 220px;
    font-size: 12px;
    color: #aebdd4;
}
</style>
