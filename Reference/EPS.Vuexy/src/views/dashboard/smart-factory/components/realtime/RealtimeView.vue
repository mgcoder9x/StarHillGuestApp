<template>
    <div>
        <LineCard
            v-for="({ lineKey, line }, index) in pagedLines"
            :key="lineKey"
            :line-key="lineKey"
            :line="line"
            :line-number="(currentPage - 1) * linesPerPage + index + 1"
        />
        <div
            v-if="totalRows > linesPerPage"
            class="d-flex justify-content-center mt-2"
        >
            <b-pagination
                v-model="currentPage"
                :total-rows="totalRows"
                :per-page="linesPerPage"
                pills
                align="center"
            />
        </div>
        <!-- Empty State -->
    </div>
</template>

<script>
import { mapState } from 'vuex'
import LineCard from './LineCard.vue'

export default {
    name: 'RealtimeView',

    components: {
        LineCard,
    },
    data() {
        return {
            currentPage: 1,
            linesPerPage: 10,
        }
    },

    computed: {
        ...mapState('dashboard', ['realtimeData']),
        sortedLines() {
            return Object.entries(this.realtimeData || {})
                .map(([lineKey, line]) => ({ lineKey, line }))
                .sort((a, b) => {
                    const activeRankA = a.line?.isActive ? 0 : 1
                    const activeRankB = b.line?.isActive ? 0 : 1

                    if (activeRankA !== activeRankB) {
                        return activeRankA - activeRankB
                    }

                    const nameA = a.line?.productionLineName || ''
                    const nameB = b.line?.productionLineName || ''
                    return nameA.localeCompare(nameB)
                })
        },
        totalRows() {
            return this.sortedLines.length
        },
        pagedLines() {
            const start = (this.currentPage - 1) * this.linesPerPage
            const end = start + this.linesPerPage
            return this.sortedLines.slice(start, end)
        },
    },
    watch: {
        totalRows() {
            const totalPages = Math.ceil(this.totalRows / this.linesPerPage) || 1
            if (this.currentPage > totalPages) {
                this.currentPage = 1
            }
        },
    },
}
</script>