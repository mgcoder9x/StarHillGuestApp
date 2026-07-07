<template>
    <div class="rc-wrap">
        <div class="rc-name">
            <Icon :icon="lineIcon" width="13" height="13" class="rc-ico" />
            {{ lineName }}
        </div>

        <!-- Stack bar -->
        <div class="rc-stack-bar">
            <div
                v-for="seg in segments"
                v-if="seg.pct > 0"
                :key="seg.key"
                :class="['rc-seg', `rc-seg--${seg.key}`]"
                :style="{ width: seg.pct + '%' }"
            >
                <span v-if="seg.pct > 12">{{ seg.pct }}%</span>
            </div>
        </div>

        <!-- Donut + stat list -->
        <div class="rc-donut-row">
            <!-- Donut SVG -->
            <div class="rc-donut-wrap">
                <svg viewBox="0 0 36 36" width="68" height="68" style="transform:rotate(-90deg)">
                    <circle cx="18" cy="18" r="14" fill="none" stroke="#edf1f7" stroke-width="4" />
                    <circle
                        v-for="arc in arcs"
                        :key="arc.key"
                        cx="18" cy="18" r="14" fill="none"
                        :stroke="arc.color"
                        stroke-width="4"
                        :stroke-dasharray="`${arc.dash} ${circumference - arc.dash}`"
                        :stroke-dashoffset="`${-arc.offset}`"
                        stroke-linecap="round"
                    />
                </svg>
                <div class="rc-donut-ctr">
                    <span class="rc-donut-pct">{{ donePct }}%</span>
                    <span class="rc-donut-lbl">
                        {{ $t('Meiko.Dashboard.CompletedShort') }}
                    </span>
                </div>
            </div>

            <!-- Stat list -->
            <div class="rc-stat-list">
                <div v-for="seg in segments" :key="seg.key" class="rc-stat-row">
                    <span :class="['rc-stat-name', `rc-stat-name--${seg.key}`]">{{ seg.label }}</span>
                    <span :class="['rc-stat-val', `rc-stat-val--${seg.key}`]">{{ seg.count }}/{{ total }}</span>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
const COLOR_MAP = {
    done:  '#0d9e5c',
    vio:   '#e06318',
    doing: '#1a6cf6',
    pend:  '#c8d4e4',
}
const CIRCUMFERENCE = 2 * Math.PI * 14  // ≈ 87.96

export default {
    name: 'RatioCard',
    props: {
        lineName: { type: String, required: true },
        lineIcon:  { type: String, default: 'lucide:factory' },
        stats: {
            type: Object,
            default: () => ({ completed: 0, violations: 0, inProgress: 0, pending: 0 }),
        },
    },
    computed: {
        circumference() { return CIRCUMFERENCE },

        total() {
            const s = this.stats || {}
            return (s.completed || 0) + (s.violations || 0) + (s.inProgress || 0) + (s.pending || 0)
        },

        donePct() {
            if (!this.total) return 0
            return Math.round(((this.stats.completed || 0) / this.total) * 100)
        },

        segments() {
            const s = this.stats || {}
            const t = this.total || 1
            const raw = [
                { key: 'done',  count: s.completed  || 0 },
                { key: 'vio',   count: s.violations  || 0 },
                { key: 'doing', count: s.inProgress  || 0 },
                { key: 'pend',  count: s.pending     || 0 },
            ]
            const labelMap = {
                done: this.$t('Meiko.Dashboard.Completed'),
                vio: this.$t('Meiko.Dashboard.Violation'),
                doing: this.$t('Meiko.Dashboard.InProgressShort'),
                pend: this.$t('Meiko.Dashboard.PendingShort'),
            }
            return raw.map((item) => ({
                ...item,
                label: labelMap[item.key],
                color: COLOR_MAP[item.key],
                pct:   this.total ? Math.round((item.count / t) * 100) : 0,
            }))
        },

        arcs() {
            let offset = 0
            return this.segments.map((seg) => {
                const dash = this.total ? (seg.count / this.total) * CIRCUMFERENCE : 0
                const arc  = { key: seg.key, color: seg.color, dash, offset }
                offset += dash
                return arc
            })
        },
    },
}
</script>
