<template>
    <div class="sv-wrap">
        <!-- ── Line Completion Cards ──────────────────────────────── -->
        <b-card no-body class="sv-section-card mb-1">
            <b-card-header class="sv-card-header">
                <Icon
                    icon="lucide:bar-chart-2"
                    width="16"
                    height="16"
                    class="mr-2 text-primary"
                />
                <strong class="sv-card-title">
                    {{ $t('Meiko.Dashboard.Shift.ShiftCompletionRate') }}
                </strong>
            </b-card-header>
            <b-card-body class="px-3 py-3">
                <div v-if="!lines.length" class="text-center py-5 text-muted">
                    <Icon
                        icon="lucide:inbox"
                        width="36"
                        height="36"
                        class="mb-2 text-muted"
                    />
                    <p class="mb-0 small">
                        {{ $t('Meiko.Dashboard.Shift.NoShiftData') }}
                    </p>
                </div>
                <div v-else class="sv-lines-grid">
                    <div
                        v-for="line in lines"
                        :key="line.lineName"
                        class="sv-line-cell"
                    >
                        <LineCompletionRow :line-data="mapLineData(line)" />
                    </div>
                </div>
            </b-card-body>
        </b-card>

        <!-- ── Charts Row ──────────────────────────────────────────── -->
        <b-row>
            <!-- Left: hourly violation trend -->
            <b-col lg="6" class="mb-1">
                <b-card no-body class="sv-chart-card h-100">
                    <b-card-header
                        class="sv-card-header"
                        style="margin-bottom: 10px !important"
                    >
                        <div
                            class="d-flex align-items-center justify-content-between w-100"
                        >
                            <div class="d-flex align-items-center">
                                <Icon
                                    icon="lucide:trending-up"
                                    width="16"
                                    height="16"
                                    class="mr-2 text-primary"
                                />
                                <strong class="sv-card-title">
                                    {{
                                        $t(
                                            'Meiko.Dashboard.Shift.HourlyViolationTrend'
                                        )
                                    }}
                                </strong>
                            </div>
                            <div class="sv-legend">
                                <span
                                    v-for="(color, name) in lineColorMap"
                                    :key="name"
                                    class="sv-legend-item"
                                >
                                    <span
                                        class="sv-legend-dot"
                                        :style="{ background: color }"
                                    ></span>
                                    <small>{{ name }}</small>
                                </span>
                            </div>
                        </div>
                    </b-card-header>
                    <b-card-body class="p-2">
                        <vue-apex-charts
                            v-if="hourlySeries.length"
                            type="line"
                            height="230"
                            :options="hourlyChartOptions"
                            :series="hourlySeries"
                        />
                        <div v-else class="text-center py-5 text-muted small">
                            {{ $t('Meiko.Dashboard.Shift.NoViolationsInShift') }}
                        </div>
                    </b-card-body>
                </b-card>
            </b-col>

            <!-- Right: top violated steps + ranking -->
            <b-col lg="6" class="mb-1">
                <b-row class="h-100">
                    <!-- Top steps bar chart -->
                    <b-col cols="12" class="mb-2">
                        <b-card no-body class="sv-chart-card">
                            <b-card-header class="sv-card-header">
                                <Icon
                                    icon="lucide:bar-chart"
                                    width="16"
                                    height="16"
                                    class="mr-2 text-warning"
                                />
                                <strong class="sv-card-title">
                                    {{ $t('Meiko.Dashboard.Shift.TopViolatedSteps') }}
                                    <span
                                        v-if="
                                            shiftStats &&
                                            shiftStats.focusedLineName
                                        "
                                        class="text-muted font-weight-normal"
                                    >
                                        — {{ shiftStats.focusedLineName }}
                                    </span>
                                </strong>
                            </b-card-header>
                            <b-card-body class="p-2">
                                <vue-apex-charts
                                    v-if="barSeries.length"
                                    type="bar"
                                    height="200"
                                    :options="barChartOptions"
                                    :series="barSeries"
                                />
                                <div
                                    v-else
                                    class="text-center py-4 text-muted small"
                                >
                                    {{ $t('Meiko.Dashboard.Shift.NoData') }}
                                </div>
                            </b-card-body>
                        </b-card>
                    </b-col>

                    <!-- Ranking list -->
                    <b-col cols="12">
                        <b-card no-body class="sv-chart-card">
                            <b-card-header
                                class="sv-card-header d-flex align-items-center justify-content-between"
                            >
                                <div class="d-flex align-items-center">
                                    <Icon
                                        icon="lucide:trophy"
                                        width="16"
                                        height="16"
                                        class="mr-2 text-warning"
                                    />
                                    <strong class="sv-card-title">
                                        {{
                                            $t(
                                                'Meiko.Dashboard.Shift.ViolationRanking'
                                            )
                                        }}
                                    </strong>
                                </div>
                                <b-badge variant="warning" pill class="px-2">
                                    {{ topViolatedSteps.length }}
                                </b-badge>
                            </b-card-header>
                            <b-card-body class="p-0 sv-rank-body">
                                <div
                                    v-if="!topViolatedSteps.length"
                                    class="text-center py-4 text-muted small"
                                >
                                    {{ $t('Meiko.Dashboard.Shift.NoViolationData') }}
                                </div>
                                <div
                                    v-for="item in topViolatedSteps"
                                    :key="item.rank"
                                    class="sv-rank-item px-3 py-2"
                                >
                                    <div class="d-flex align-items-center">
                                        <span
                                            class="sv-rank-badge mr-2"
                                            :class="rankClass(item.rank)"
                                        >
                                            {{ item.rank }}
                                        </span>
                                        <div class="flex-grow-1 min-w-0 mr-2">
                                            <div
                                                class="sv-rank-step font-weight-bold"
                                            >
                                                {{ item.stepName }}
                                            </div>
                                            <small class="text-muted">
                                                <Icon
                                                    icon="lucide:factory"
                                                    width="11"
                                                    height="11"
                                                    class="mr-1"
                                                />
                                                {{ item.lineName }}
                                            </small>
                                        </div>
                                        <div class="sv-rank-bar-wrap">
                                            <div
                                                class="sv-rank-bar"
                                                :style="{
                                                    width: rankBarWidth(
                                                        item.violationCount
                                                    ),
                                                }"
                                            ></div>
                                        </div>
                                        <span
                                            class="sv-rank-count ml-2 text-warning font-weight-bold"
                                        >
                                            {{ item.violationCount }}
                                        </span>
                                    </div>
                                </div>
                            </b-card-body>
                        </b-card>
                    </b-col>
                </b-row>
            </b-col>
        </b-row>
    </div>
</template>

<script>
import { mapState } from 'vuex'
import VueApexCharts from 'vue-apexcharts'
import LineCompletionRow from '../overview/LineCompletionRow.vue'

const LINE_COLORS = [
    '#4c9be8',
    '#28a745',
    '#fd7e14',
    '#6f42c1',
    '#17a2b8',
    '#e83e8c',
]

export default {
    name: 'ShiftView',
    components: { VueApexCharts, LineCompletionRow },
    computed: {
        ...mapState('dashboard', ['shiftStats', 'shiftStatsLoading']),

        lines() {
            return this.shiftStats?.lines || []
        },
        hours() {
            return this.shiftStats?.hours || []
        },
        hourlyViolations() {
            return this.shiftStats?.hourlyViolations || []
        },
        topViolatedSteps() {
            return this.shiftStats?.topViolatedSteps || []
        },
        focusedLineTopSteps() {
            return this.shiftStats?.focusedLineTopSteps || []
        },
        lineColorMap() {
            const map = {}
            this.lines.forEach((l, i) => {
                map[l.lineName] = LINE_COLORS[i % LINE_COLORS.length]
            })
            return map
        },
        maxViolationCount() {
            if (!this.topViolatedSteps.length) return 1
            return Math.max(
                ...this.topViolatedSteps.map((s) => s.violationCount),
                1
            )
        },
        // Line chart series (hourly violations per line)
        hourlySeries() {
            return this.hourlyViolations.map((series, i) => ({
                name: series.lineName,
                data: series.data,
                color: LINE_COLORS[i % LINE_COLORS.length],
            }))
        },
        hourlyChartOptions() {
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
                    categories: this.hours,
                    axisBorder: { show: false },
                    axisTicks: { show: false },
                    labels: { style: { fontSize: '12px', colors: '#8e8e8e' } },
                },
                yaxis: {
                    min: 0,
                    labels: {
                        style: { fontSize: '12px', colors: '#8e8e8e' },
                        formatter: (v) => Math.round(v),
                    },
                    title: {
                        text: this.$t('Meiko.Dashboard.Shift.ViolationCount'),
                        style: { fontSize: '12px', color: '#8e8e8e' },
                    },
                },
                legend: { show: false },
                grid: { strokeDashArray: 5, borderColor: '#f0f0f0' },
                tooltip: {
                    y: {
                        formatter: (val) =>
                            `${val} ${this.$t('Meiko.Dashboard.ViolationCountUnit')}`,
                    },
                },
            }
        },
        // Horizontal bar chart for focused line
        barSeries() {
            if (!this.focusedLineTopSteps.length) return []
            return [
                {
                    name: this.$t('Meiko.Dashboard.Violation'),
                    data: this.focusedLineTopSteps.map((s) => s.violationCount),
                    color: '#fd7e14',
                },
            ]
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
                        horizontal: true,
                        barHeight: '60%',
                        borderRadius: 3,
                        borderRadiusApplication: 'end',
                    },
                },
                dataLabels: { enabled: false },
                xaxis: {
                    categories: this.focusedLineTopSteps.map((s) => s.stepName),
                    axisBorder: { show: false },
                    axisTicks: { show: false },
                    labels: { style: { fontSize: '11px', colors: '#8e8e8e' } },
                },
                yaxis: {
                    labels: {
                        style: { fontSize: '11px', colors: '#333' },
                        maxWidth: 120,
                    },
                },
                legend: { show: false },
                grid: {
                    strokeDashArray: 5,
                    borderColor: '#f0f0f0',
                    xaxis: { lines: { show: true } },
                    yaxis: { lines: { show: false } },
                },
                tooltip: {
                    y: {
                        formatter: (val) =>
                            `${val} ${this.$t('Meiko.Dashboard.ViolationCountUnit')}`,
                    },
                },
            }
        },
    },
    methods: {
        mapLineData(line) {
            console.log(line)
            return {
                productionLineName: line.lineName,
                completedCount: line.completedCount,
                violationCount: line.violationCount,
                inProgressCount: line.inProgressCount,
                pendingCount: line.pendingCount,
                totalSteps: line.totalSteps,
            }
        },
        rankClass(rank) {
            if (rank === 1) return 'sv-rank-badge--gold'
            if (rank === 2) return 'sv-rank-badge--silver'
            if (rank === 3) return 'sv-rank-badge--bronze'
            return 'sv-rank-badge--default'
        },
        rankBarWidth(count) {
            const pct = Math.round((count / this.maxViolationCount) * 100)
            return `${Math.max(pct, 6)}%`
        },
    },
}
</script>

<style scoped>
/* ─── Section Cards ────────────────────────────────────────── */
.sv-section-card,
.sv-chart-card {
    border: 1px solid #eaeaea;
    border-radius: 10px;
    box-shadow: 0 1px 6px rgba(0, 0, 0, 0.05);
    margin-bottom: 10px !important;
}
.sv-card-header {
    background: #fafafa;
    border-bottom: 1px solid #eaeaea;
    padding: 11px 16px;
    display: flex;
    align-items: center;
    border-radius: 10px 10px 0 0 !important;
    display: flex;
    justify-content: start;
}
.sv-card-title {
    font-size: 0.92rem;
}

/* ─── Legend ─────────────────────────────────────────────── */
.sv-legend {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
}
.sv-legend-item {
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 0.78rem;
    color: #6c757d;
}
.sv-legend-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    flex-shrink: 0;
}

/* ─── Ranking List ─────────────────────────────────────── */
.sv-rank-body {
    max-height: 260px;
    overflow-y: auto;
}
.sv-rank-item {
    border-bottom: 1px solid #f5f5f5;
    transition: background 0.1s;
}
.sv-rank-item:hover {
    background: #fafafa;
}
.sv-rank-item:last-child {
    border-bottom: none;
}

.sv-rank-badge {
    width: 26px;
    height: 26px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.78rem;
    font-weight: 700;
    flex-shrink: 0;
}
.sv-rank-badge--gold {
    background: #ffd700;
    color: #7a5800;
}
.sv-rank-badge--silver {
    background: #c0c0c0;
    color: #555;
}
.sv-rank-badge--bronze {
    background: #cd7f32;
    color: #fff;
}
.sv-rank-badge--default {
    background: #e9ecef;
    color: #6c757d;
}

.sv-rank-step {
    font-size: 0.84rem;
    line-height: 1.3;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.sv-rank-bar-wrap {
    width: 80px;
    height: 6px;
    background: #f0f0f0;
    border-radius: 3px;
    overflow: hidden;
    flex-shrink: 0;
}
.sv-rank-bar {
    height: 100%;
    background: #ffc107;
    border-radius: 3px;
    transition: width 0.3s ease;
}
.sv-rank-count {
    font-size: 0.84rem;
    min-width: 24px;
    text-align: right;
}

/* ─── Grid layout – 5 items per row ────────────────────────── */
.sv-lines-grid {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 12px;
}
.sv-line-cell {
    width: 100%;
    min-width: 0;
}

/* Responsive Grid */
@media (max-width: 1400px) {
    .sv-lines-grid {
        grid-template-columns: repeat(4, 1fr);
    }
}
@media (max-width: 1199px) {
    .sv-lines-grid {
        grid-template-columns: repeat(3, 1fr);
    }
}
@media (max-width: 991px) {
    .sv-lines-grid {
        grid-template-columns: repeat(2, 1fr);
    }
}
@media (max-width: 767px) {
    .sv-lines-grid {
        grid-template-columns: 1fr;
    }
}
</style>
