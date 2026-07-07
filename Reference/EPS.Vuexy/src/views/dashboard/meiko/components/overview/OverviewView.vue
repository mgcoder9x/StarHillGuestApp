<template>
    <div class="meiko-overview">
        <!-- ── KPI Summary Strip (realtime current shift) ──────── -->
        <div class="ov-kpi-strip mb-2">
            <div
                v-for="card in kpiCards"
                :key="card.key"
                class="ov-kpi-item"
                :class="`ov-kpi-item--${card.variant}`"
            >
                <div
                    class="ov-kpi-icon"
                    :class="`ov-kpi-icon--${card.variant}`"
                >
                    <Icon :icon="card.icon" width="22" height="22" />
                </div>
                <div class="ov-kpi-body">
                    <div class="ov-kpi-value" :class="`text-${card.variant}`">
                        {{ card.value.toLocaleString($i18n.locale || 'vi') }}
                    </div>
                    <div class="ov-kpi-label">{{ card.label }}</div>
                </div>
            </div>
        </div>

        <!-- ── Completion Rate + Violations (realtime) ────────── -->
        <b-row class="mb-2">
            <!-- Left: ALL production lines grid -->
            <b-col lg="12" class="mb-3 mb-lg-0">
                <b-card no-body class="ov-section-card h-100">
                    <b-card-header
                        class="ov-card-header d-flex align-items-center justify-content-between"
                    >
                        <div class="d-flex align-items-center">
                            <Icon
                                icon="lucide:bar-chart-2"
                                width="16"
                                height="16"
                                class="mr-2 text-primary"
                            />
                            <strong class="ov-card-title">
                                {{
                                    $t(
                                        'Meiko.Dashboard.Overview.CompletionRateTitle'
                                    )
                                }}
                            </strong>
                        </div>
                    </b-card-header>
                    <b-card-body class="p-0">
                        <div
                            v-if="!rtLines.length"
                            class="text-center py-5 text-muted"
                        >
                            <Icon
                                icon="lucide:inbox"
                                width="36"
                                height="36"
                                class="mb-2 text-muted"
                            />
                            <p class="mb-0">
                                {{ $t('Meiko.Dashboard.Overview.EmptyTitle') }}
                            </p>
                            <small>{{
                                $t('Meiko.Dashboard.Overview.EmptyDescription')
                            }}</small>
                        </div>
                        <!-- Single-row horizontal scroll: one card per production line -->
                        <div v-else ref="linesScroll" class="ov-lines-scroll">
                            <!-- @wheel.prevent="onLinesWheel" -->
                            <div
                                v-for="line in rtLines"
                                :key="line.productionLineName"
                                class="ov-line-cell"
                                :class="{
                                    'ov-line-cell--inactive': !line.isActive,
                                }"
                            >
                                <LineCompletionRow :line-data="line" />
                            </div>
                        </div>
                    </b-card-body>
                </b-card>
            </b-col>

            <!-- Right: violations panel (realtime) -->
            <!-- <b-col lg="4">
                <b-card no-body class="ov-violations-card h-100">
                    <b-card-header class="ov-violations-header d-flex align-items-center justify-content-between">
                        <div class="d-flex align-items-center">
                            <Icon icon="lucide:alert-triangle" width="16" height="16" class="mr-2 text-danger" />
                            <strong class="ov-card-title text-danger">
                                {{ $t('Meiko.Dashboard.Overview.ViolationsTitle') }}
                            </strong>
                        </div>
                        <b-badge variant="danger" pill class="px-2">
                            {{ rtViolations.length }}
                        </b-badge>
                    </b-card-header>

                    <b-card-body class="p-0 ov-violations-body">
                        <div v-if="!rtViolations.length" class="text-center py-5 text-muted">
                            <Icon icon="lucide:check-circle-2" width="32" height="32" class="mb-2 text-success" />
                            <p class="mb-0 small">{{ $t('Meiko.Dashboard.Overview.NoViolations') }}</p>
                        </div>
                        <div
                            v-for="(v, i) in rtViolations"
                            :key="i"
                            class="ov-violation-item px-3 py-2"
                        >
                            <div class="d-flex justify-content-between align-items-start">
                                <div class="flex-grow-1 mr-2 min-w-0">
                                    <div class="ov-violation-name">
                                        <span class="text-primary">{{ v.productionLineName }}</span>
                                        <span class="mx-1 text-muted">·</span>
                                        <span class="font-weight-bold">{{ v.stepName }}</span>
                                    </div>
                                    <div class="ov-violation-desc text-muted">
                                        {{ v.eventTypeName || $t('Meiko.Dashboard.Overview.ViolationDefault') }}
                                    </div>
                                </div>
                                <small class="ov-violation-time text-muted flex-shrink-0">
                                    {{ formatTime(v.time) }}
                                </small>
                            </div>
                        </div>
                    </b-card-body>
                </b-card>
            </b-col> -->
        </b-row>

        <!-- ── Date Filter Row ──────────────────────────────────── -->
        <div class="ov-filter-row mb-2">
            <OverviewFilters
                :is-loading="isLoadingFilter"
                @apply="handleApply"
            />
        </div>

        <!-- ── Charts Row ───────────────────────────────────────── -->
        <b-row>
            <!-- Bar chart: Performance comparison -->
            <b-col lg="6" class="mb-2">
                <b-card no-body class="ov-chart-card">
                    <b-card-header class="ov-card-header">
                        <div
                            class="d-flex align-items-center justify-content-between w-100"
                        >
                            <div class="d-flex align-items-center">
                                <Icon
                                    icon="lucide:bar-chart"
                                    width="16"
                                    height="16"
                                    class="mr-2 text-primary"
                                />
                                <strong class="ov-card-title">
                                    {{
                                        $t(
                                            'Meiko.Dashboard.Overview.PerformanceChartTitle'
                                        )
                                    }}
                                </strong>
                            </div>
                            <div class="ov-chart-legend">
                                <span
                                    v-for="(color, name) in lineColorMap"
                                    :key="name"
                                    class="ov-legend-item"
                                >
                                    <span
                                        class="ov-legend-dot"
                                        :style="{ background: color }"
                                    ></span>
                                    <small>{{ name }}</small>
                                </span>
                            </div>
                        </div>
                    </b-card-header>
                    <b-card-body class="p-2">
                        <vue-apex-charts
                            v-if="barSeries.length"
                            type="bar"
                            height="230"
                            :options="barChartOptions"
                            :series="barSeries"
                        />
                        <div v-else class="text-center py-5 text-muted small">
                            {{ $t('Meiko.Dashboard.Overview.NoChartData') }}
                        </div>
                    </b-card-body>
                </b-card>
            </b-col>

            <!-- Line chart: Error rate by day -->
            <b-col lg="6" class="mb-3">
                <b-card no-body class="ov-chart-card">
                    <b-card-header class="ov-card-header">
                        <div
                            class="d-flex align-items-center justify-content-between w-100"
                        >
                            <div class="d-flex align-items-center">
                                <Icon
                                    icon="lucide:trending-up"
                                    width="16"
                                    height="16"
                                    class="mr-2 text-danger"
                                />
                                <strong class="ov-card-title">
                                    {{
                                        $t(
                                            'Meiko.Dashboard.Overview.ErrorRateChartTitle'
                                        )
                                    }}
                                </strong>
                            </div>
                            <div class="ov-chart-legend">
                                <span
                                    v-for="(color, name) in lineColorMap"
                                    :key="name"
                                    class="ov-legend-item"
                                >
                                    <span
                                        class="ov-legend-dot"
                                        :style="{ background: color }"
                                    ></span>
                                    <small>{{ name }}</small>
                                </span>
                            </div>
                        </div>
                    </b-card-header>
                    <b-card-body class="p-2">
                        <vue-apex-charts
                            v-if="lineSeries.length"
                            type="line"
                            height="230"
                            :options="lineChartOptions"
                            :series="lineSeries"
                        />
                        <div v-else class="text-center py-5 text-muted small">
                            {{ $t('Meiko.Dashboard.Overview.NoChartData') }}
                        </div>
                    </b-card-body>
                </b-card>
            </b-col>
        </b-row>
    </div>
</template>

<script>
import { mapState, mapActions } from 'vuex'
import VueApexCharts from 'vue-apexcharts'
import LineCompletionRow from './LineCompletionRow.vue'
import OverviewFilters from './OverviewFilters.vue'
import { LINE_COLORS } from '../common/color.js'

export default {
    name: 'OverviewView',
    components: { VueApexCharts, LineCompletionRow, OverviewFilters },
    data() {
        return {
            isLoadingFilter: false,
            _rtPollTimer: null,
        }
    },
    computed: {
        ...mapState('dashboard', [
            'overviewData',
            'realtimeOverviewData',
            'realtimeOverviewLoading',
        ]),

        // ── Realtime top section ───────────────────────────────────
        rtSummary() {
            return (
                this.realtimeOverviewData?.summary || {
                    totalCompleted: 0,
                    totalViolations: 0,
                    totalInProgress: 0,
                    totalPending: 0,
                }
            )
        },
        rtLines() {
            return this.realtimeOverviewData?.lines || []
        },
        rtViolations() {
            return this.realtimeOverviewData?.recentViolations || []
        },
        rtShiftName() {
            return this.realtimeOverviewData?.shiftName ?? ''
        },
        rtShiftTimeRange() {
            return this.realtimeOverviewData?.shiftTimeRange ?? ''
        },

        // ── Historical bottom section ──────────────────────────────
        overviewLines() {
            return this.overviewData?.lines || {}
        },
        dailyStats() {
            return this.overviewData?.dailyStats || []
        },
        dailyErrorRates() {
            return this.overviewData?.dailyErrorRates || []
        },

        // KPI cards use realtime summary
        kpiCards() {
            return [
                {
                    key: 'completed',
                    value: this.rtSummary.totalCompleted,
                    label: this.$t('Meiko.Dashboard.Completed'),
                    icon: 'lucide:circle-check-big',
                    variant: 'success',
                },
                {
                    key: 'violations',
                    value: this.rtSummary.totalViolations,
                    label: this.$t('Meiko.Dashboard.Violation'),
                    icon: 'lucide:circle-alert',
                    variant: 'warning',
                },
                {
                    key: 'inProgress',
                    value: this.rtSummary.totalInProgress,
                    label: this.$t('Meiko.Dashboard.InProgress'),
                    icon: 'lucide:play-circle',
                    variant: 'primary',
                },
                {
                    key: 'pending',
                    value: this.rtSummary.totalPending,
                    label: this.$t('Meiko.Dashboard.Pending'),
                    icon: 'lucide:clock',
                    variant: 'secondary',
                },
            ]
        },
        lineNames() {
            return Object.values(this.overviewLines).map(
                (l) => l.productionLineName
            )
        },
        lineColorMap() {
            const map = {}
            this.lineNames.forEach((name, i) => {
                map[name] = LINE_COLORS[i % LINE_COLORS.length]
            })
            return map
        },
        dateCategories() {
            return this.dailyStats.map((d) => d.date)
        },
        barSeries() {
            if (!this.dailyStats.length) return []
            return this.lineNames.map((name, i) => ({
                name,
                data: this.dailyStats.map((d) => d.lines[name] || 0),
                color: LINE_COLORS[i % LINE_COLORS.length],
            }))
        },
        lineSeries() {
            if (!this.dailyErrorRates.length) return []
            return this.lineNames.map((name, i) => ({
                name,
                data: this.dailyErrorRates.map((d) => d.lines[name] || 0),
                color: LINE_COLORS[i % LINE_COLORS.length],
            }))
        },
        barChartOptions() {
            return {
                chart: {
                    type: 'bar',
                    toolbar: { show: false },
                    parentHeightOffset: 0,
                    fontFamily: 'inherit',
                },
                plotOptions: {
                    bar: {
                        columnWidth: '55%',
                        borderRadius: 3,
                        borderRadiusApplication: 'end',
                    },
                },
                dataLabels: { enabled: false },
                xaxis: {
                    categories: this.dateCategories,
                    axisBorder: { show: false },
                    axisTicks: { show: false },
                    labels: { style: { fontSize: '12px', colors: '#8e8e8e' } },
                },
                yaxis: {
                    labels: { style: { fontSize: '12px', colors: '#8e8e8e' } },
                    title: {
                        text: this.$t('Meiko.Dashboard.Overview.AxisCompleted'),
                        style: { fontSize: '12px', color: '#8e8e8e' },
                    },
                },
                legend: { show: false },
                grid: { strokeDashArray: 5, borderColor: '#f0f0f0' },
                tooltip: {
                    y: {
                        formatter: (val) =>
                            `${val} ${this.$t('Meiko.Dashboard.Overview.StepUnit')}`,
                    },
                },
            }
        },
        lineChartOptions() {
            return {
                chart: {
                    type: 'line',
                    toolbar: { show: false },
                    parentHeightOffset: 0,
                    fontFamily: 'inherit',
                },
                stroke: { curve: 'smooth', width: 2.5 },
                markers: { size: 4, strokeWidth: 0, hover: { size: 6 } },
                dataLabels: { enabled: false },
                xaxis: {
                    categories: this.dateCategories,
                    axisBorder: { show: false },
                    axisTicks: { show: false },
                    labels: { style: { fontSize: '12px', colors: '#8e8e8e' } },
                },
                yaxis: {
                    min: 0,
                    max: 100,
                    labels: {
                        style: { fontSize: '12px', colors: '#8e8e8e' },
                        formatter: (v) => `${v}%`,
                    },
                    title: {
                        text: this.$t('Meiko.Dashboard.Overview.AxisErrorRate'),
                        style: { fontSize: '12px', color: '#8e8e8e' },
                    },
                },
                legend: { show: false },
                grid: { strokeDashArray: 5, borderColor: '#f0f0f0' },
                tooltip: { y: { formatter: (val) => `${val}%` } },
            }
        },
    },
    async created() {
        // Initial load of realtime top section
        await this.refreshRealtimeOverview()
        // Auto-refresh every 60 seconds
        this._rtPollTimer = setInterval(() => {
            this.refreshRealtimeOverview()
        }, 60_000)
    },
    beforeDestroy() {
        clearInterval(this._rtPollTimer)
    },
    methods: {
        ...mapActions('dashboard', [
            'loadOverviewDashboard',
            'loadRealtimeOverview',
        ]),
        async refreshRealtimeOverview() {
            try {
                await this.loadRealtimeOverview()
            } catch {
                // errors surfaced via toast in parent
            }
        },
        onLinesWheel(e) {
            const el = this.$refs.linesScroll
            if (!el) return
            el.scrollLeft += e.deltaY !== 0 ? e.deltaY : e.deltaX
        },
        async handleApply() {
            this.isLoadingFilter = true
            try {
                await this.loadOverviewDashboard()
            } catch {
                // errors surfaced via toast in parent
            } finally {
                this.isLoadingFilter = false
            }
        },
        formatTime(timeStr) {
            if (!timeStr) return '--'
            try {
                return new Intl.DateTimeFormat(this.$i18n.locale || 'vi', {
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit',
                }).format(new Date(timeStr))
            } catch {
                return '--'
            }
        },
    },
}
</script>

<style scoped>
/* ─── KPI Strip ────────────────────────────────────────────── */
.ov-kpi-strip {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 14px;
}
@media (max-width: 767px) {
    .ov-kpi-strip {
        grid-template-columns: repeat(2, 1fr);
    }
}

.ov-kpi-item {
    background: #fff;
    border-radius: 10px;
    box-shadow: 0 1px 6px rgba(0, 0, 0, 0.07);
    padding: 16px 18px;
    display: flex;
    align-items: center;
    gap: 14px;
    border-top: 3px solid transparent;
}
.ov-kpi-item--success {
    border-top-color: #28a745;
}
.ov-kpi-item--warning {
    border-top-color: #ffc107;
}
.ov-kpi-item--primary {
    border-top-color: #4c9be8;
}
.ov-kpi-item--secondary {
    border-top-color: #adb5bd;
}

.ov-kpi-icon {
    width: 46px;
    height: 46px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}
.ov-kpi-icon--success {
    background: rgba(40, 167, 69, 0.12);
    color: #28a745;
}
.ov-kpi-icon--warning {
    background: rgba(255, 193, 7, 0.15);
    color: #e6a800;
}
.ov-kpi-icon--primary {
    background: rgba(76, 155, 232, 0.12);
    color: #4c9be8;
}
.ov-kpi-icon--secondary {
    background: rgba(108, 117, 125, 0.1);
    color: #6c757d;
}

.ov-kpi-body {
    min-width: 0;
}
.ov-kpi-value {
    font-size: 1.8rem;
    font-weight: 700;
    line-height: 1;
    letter-spacing: -0.02em;
}
.ov-kpi-label {
    font-size: 0.75rem;
    color: #8e8e8e;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    margin-top: 4px;
}

/* ─── Section Cards ────────────────────────────────────────── */
.ov-section-card {
    border: 1px solid #eaeaea;
    border-radius: 10px;
    box-shadow: 0 1px 6px rgba(0, 0, 0, 0.05);
}
.ov-card-header {
    background: #fafafa;
    border-bottom: 1px solid #eaeaea;
    padding: 11px 16px;
    display: flex;
    align-items: center;
    border-radius: 10px 10px 0 0 !important;
}
.ov-card-title {
    font-size: 0.92rem;
}
.ov-card-footer {
    background: #fafafa;
    border-top: 1px solid #eaeaea;
    border-radius: 0 0 10px 10px;
    font-size: 0.82rem;
}

/* ─── Violations Panel ─────────────────────────────────────── */
.ov-violations-card {
    border: 1px solid #f5c6cb;
    border-radius: 10px;
    box-shadow: 0 1px 6px rgba(220, 53, 69, 0.08);
}
.ov-violations-header {
    background: #fff5f5;
    border-bottom: 1px solid #f5c6cb;
    padding: 11px 14px;
    border-radius: 10px 10px 0 0 !important;
}
.ov-violations-body {
    max-height: 310px;
    overflow-y: auto;
}
.ov-violation-item {
    border-bottom: 1px solid #fef2f2;
    transition: background 0.12s;
}
.ov-violation-item:hover {
    background: #fff8f8;
}
.ov-violation-item:last-child {
    border-bottom: none;
}
.ov-violation-name {
    font-size: 0.84rem;
    line-height: 1.3;
}
.ov-violation-desc {
    font-size: 0.76rem;
    line-height: 1.3;
    margin-top: 1px;
}
.ov-violation-time {
    font-size: 0.74rem;
    white-space: nowrap;
}
.ov-violations-footer {
    background: #fff5f5;
    border-top: 1px solid #f5c6cb;
    border-radius: 0 0 10px 10px;
    font-size: 0.8rem;
}

/* ─── Filter Row ───────────────────────────────────────────── */
.ov-filter-row {
    background: #fff;
    border: 1px solid #eaeaea;
    border-radius: 8px;
    padding: 10px 16px;
    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.04);
}

/* ─── Chart Cards ──────────────────────────────────────────── */
.ov-chart-card {
    border: 1px solid #eaeaea;
    border-radius: 10px;
    box-shadow: 0 1px 6px rgba(0, 0, 0, 0.05);
}
.ov-chart-legend {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
}
.ov-legend-item {
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 0.78rem;
    color: #6c757d;
}
.ov-legend-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    flex-shrink: 0;
}

/* ─── Realtime Lines Scroll ─────────────────────────────────── */
/* ─── Grid layout – max 3 rows visible ──────────────────────── */
.ov-lines-scroll {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 10px;
    padding: 12px 14px;
    /* (3 cards × ~178px) + (2 gaps × 10px) = ~574px */
    max-height: 574px;
    overflow-y: auto;
    overflow-x: hidden;
    /* thin scrollbar */
    scrollbar-width: thin;
    scrollbar-color: #c8cdd4 transparent;
}
.ov-lines-scroll::-webkit-scrollbar {
    width: 5px;
}
.ov-lines-scroll::-webkit-scrollbar-track {
    background: transparent;
}
.ov-lines-scroll::-webkit-scrollbar-thumb {
    background-color: #c8cdd4;
    border-radius: 4px;
}

.ov-line-cell {
    width: 100%;
    min-width: 0;
}

/* ─── Responsive Grid ───────────────────────────────────────── */
@media (max-width: 1400px) {
    .ov-lines-scroll {
        grid-template-columns: repeat(3, 1fr);
    }
}
@media (max-width: 1199px) {
    .ov-lines-scroll {
        grid-template-columns: repeat(2, 1fr);
    }
}
@media (max-width: 767px) {
    .ov-lines-scroll {
        grid-template-columns: 1fr;
        max-height: 640px; /* slightly taller due to text wrap */
    }
}
/* Dim inactive lines (no events yet in this shift) */
.ov-line-cell--inactive {
    opacity: 0.9;
    filter: grayscale(0.35);
}

/* ─── Shift Badge ────────────────────────────────────────────── */
.ov-shift-badge {
    display: inline-flex;
    align-items: center;
    background: rgba(76, 155, 232, 0.1);
    color: #4c9be8;
    border-radius: 20px;
    padding: 2px 10px;
    font-size: 0.78rem;
    font-weight: 600;
    white-space: nowrap;
}

/* ─── Refresh button ─────────────────────────────────────────── */
.ov-refresh-btn {
    padding: 2px 6px;
    line-height: 1;
    border-radius: 6px;
}
</style>
