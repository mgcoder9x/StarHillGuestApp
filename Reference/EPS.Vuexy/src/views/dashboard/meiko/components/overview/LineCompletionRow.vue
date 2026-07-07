<template>
    <div class="lcc-card h-100">
        <!-- Line name header -->
        <div class="lcc-header">
            <Icon
                icon="fa7-solid:industry"
                width="14"
                height="14"
                class="text-primary flex-shrink-0"
            />
            <span
                v-b-tooltip.hover
                class="lcc-name ml-1"
                :title="lineData.productionLineName"
                >{{ lineData.productionLineName }}</span
            >
        </div>

        <!-- Stacked progress bar -->
        <div class="lcc-progress-wrap">
            <b-progress :max="totalSteps" class="lcc-progress-bar">
                <b-progress-bar
                    v-b-tooltip.hover
                    :value="lineData.completedCount"
                    variant="success"
                    :title="`${$t('Meiko.Dashboard.Completed')}: ${lineData.completedCount} (${completedPct}%)`"
                />
                <b-progress-bar
                    v-b-tooltip.hover
                    :value="lineData.violationCount"
                    variant="warning"
                    :title="`${$t('Meiko.Dashboard.Violation')}: ${lineData.violationCount} (${violationPct}%)`"
                />
                <!-- <b-progress-bar
                    v-b-tooltip.hover
                    :value="lineData.inProgressCount"
                    variant="primary"
                    :title="`${$t('Meiko.Dashboard.InProgress')}: ${lineData.inProgressCount} (${inProgressPct}%)`"
                /> -->
            </b-progress>
        </div>

        <!-- Donut + Stats row -->
        <div class="lcc-body">
            <div class="lcc-donut">
                <vue-apex-charts
                    type="donut"
                    width="96"
                    :options="donutOptions"
                    :series="donutSeries"
                />
            </div>
            <div class="lcc-stats">
                <div v-for="stat in statsRows" :key="stat.key" class="lcc-stat">
                    <span
                        class="lcc-stat-dot"
                        :style="{ background: stat.color }"
                    ></span>
                    <span class="lcc-stat-label text-muted">{{
                        stat.label
                    }}</span>
                    <span class="lcc-stat-count text-muted">{{
                        stat.value
                    }}</span>
                    <span
                        class="lcc-stat-pct font-weight-bold"
                        :class="`text-${stat.variant}`"
                    >
                        {{ stat.pct }}%
                    </span>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
import VueApexCharts from 'vue-apexcharts'

export default {
    name: 'LineCompletionRow',
    components: { VueApexCharts },
    props: {
        lineData: {
            type: Object,
            required: true,
        },
    },
    computed: {
        totalSteps() {
            return Math.max(
                this.lineData.totalSteps ||
                    (this.lineData.completedCount || 0) +
                        (this.lineData.violationCount || 0) +
                        (this.lineData.inProgressCount || 0) +
                        (this.lineData.pendingCount || 0),
                1
            )
        },
        completedPct() {
            return Math.round(
                ((this.lineData.completedCount || 0) / this.totalSteps) * 100
            )
        },
        violationPct() {
            return Math.round(
                ((this.lineData.violationCount || 0) / this.totalSteps) * 100
            )
        },
        inProgressPct() {
            return Math.round(
                ((this.lineData.inProgressCount || 0) / this.totalSteps) * 100
            )
        },
        pendingPct() {
            return Math.round(
                ((this.lineData.pendingCount || 0) / this.totalSteps) * 100
            )
        },
        donutSeries() {
            const c = this.lineData.completedCount || 0
            const v = this.lineData.violationCount || 0
            const ip =  0
            const p =  0
            return [c, v, ip, p].some((n) => n > 0)
                ? [c, v, ip, p]
                : [0, 0, 0, 1]
        },
        donutOptions() {
            return {
                chart: { type: 'donut', sparkline: { enabled: true } },
                colors: ['#28a745', '#e6a800', '#4c9be8', '#dee2e6'],
                labels: [
                    this.$t('Meiko.Dashboard.Completed'),
                    this.$t('Meiko.Dashboard.Violation'),
                    // this.$t('Meiko.Dashboard.InProgress'),
                    // this.$t('Meiko.Dashboard.Pending'),
                ],
                dataLabels: { enabled: false },
                legend: { show: false },
                stroke: { width: 1, colors: ['#fff'] },
                tooltip: {
                    y: {
                        formatter: (val) => {
                            const pct = Math.round(
                                (val / this.totalSteps) * 100
                            )
                            return `${val} ${this.$t('Meiko.Dashboard.StepUnit')} (${pct}%)`
                        },
                    },
                },
                plotOptions: {
                    pie: {
                        donut: {
                            size: '65%',
                            labels: {
                                show: true,
                                name: {
                                    show: true,
                                    fontSize: '9px',
                                    color: '#8e8e8e',
                                    offsetY: 14,
                                },
                                value: {
                                    show: true,
                                    fontSize: '13px',
                                    fontWeight: 700,
                                    color: '#333',
                                    offsetY: -10,
                                    formatter: (val) => {
                                        const pct = Math.round(
                                            (Number(val) / this.totalSteps) *
                                                100
                                        )
                                        return `${pct}%`
                                    },
                                },
                                total: {
                                    show: true,
                                    label: this.$t('Meiko.Dashboard.Completed'),
                                    fontSize: '9px',
                                    color: '#8e8e8e',
                                    fontWeight: 500,
                                    formatter: () => `${this.completedPct}%`,
                                },
                            },
                        },
                    },
                },
            }
        },
        statsRows() {
            return [
                {
                    key: 'completed',
                    label: this.$t('Meiko.Dashboard.Completed'),
                    value: this.lineData.completedCount || 0,
                    pct: this.completedPct,
                    variant: 'success',
                    color: '#28a745',
                },
                {
                    key: 'violation',
                    label: this.$t('Meiko.Dashboard.Violation'),
                    value: this.lineData.violationCount || 0,
                    pct: this.violationPct,
                    variant: 'warning',
                    color: '#e6a800',
                },
                // {
                //     key: 'inProgress',
                //     label: this.$t('Meiko.Dashboard.InProgress'),
                //     value: this.lineData.inProgressCount || 0,
                //     pct: this.inProgressPct,
                //     variant: 'primary',
                //     color: '#4c9be8',
                // },
                // {
                //     key: 'pending',
                //     label: this.$t('Meiko.Dashboard.Pending'),
                //     value: this.lineData.pendingCount || 0,
                //     pct: this.pendingPct,
                //     variant: 'secondary',
                //     color: '#adb5bd',
                // },
            ]
        },
    },
}
</script>

<style scoped>
/* ─── Card wrapper ─────────────────────────────────────────── */
.lcc-card {
    background: #fff;
    border: 1px solid #eaeaea;
    border-radius: 10px;
    padding: 12px 14px;
    display: flex;
    flex-direction: column;
    gap: 10px;
}

/* ─── Header ──────────────────────────────────────────────── */
.lcc-header {
    display: flex;
    align-items: center;
    border-bottom: 1px solid #f0f0f0;
    padding-bottom: 8px;
}
.lcc-name {
    font-size: 0.92rem;
    font-weight: 700;
    color: #333;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

/* ─── Progress ─────────────────────────────────────────────── */
.lcc-progress-wrap {
    display: flex;
    flex-direction: column;
    gap: 4px;
}
.lcc-progress-bar {
    height: 10px !important;
    border-radius: 6px;
    overflow: hidden;
}

/* ─── Body: donut + stats ──────────────────────────────────── */
.lcc-body {
    display: flex;
    align-items: center;
    gap: 10px;
}
.lcc-donut {
    flex-shrink: 0;
}
.lcc-stats {
    flex: 1;
    min-width: 0;
}
.lcc-stat {
    display: flex;
    align-items: center;
    gap: 5px;
    line-height: 1.9;
}
.lcc-stat-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    flex-shrink: 0;
}
.lcc-stat-label {
    flex: 1;
    font-size: 0.74rem;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    color: #6c757d;
}
.lcc-stat-count {
    font-size: 0.74rem;
    white-space: nowrap;
    min-width: 18px;
    text-align: right;
}
.lcc-stat-pct {
    font-size: 0.78rem;
    white-space: nowrap;
    min-width: 38px;
    text-align: right;
}
</style>
