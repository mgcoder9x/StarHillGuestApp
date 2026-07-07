<template>
    <div>
        <!-- <ShiftFilters @filter-changed="handleFilterChange" /> -->
        <template v-if="hasData">
            <ShiftLineCard
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
        </template>
        <!-- Empty state khi không có data -->
        <b-card v-else class="text-center py-5">
            <i class="fas fa-clipboard-list fa-3x text-muted mb-3"></i>
            <h5>{{ $t('Meiko.Dashboard.EmptyShiftTitle') }}</h5>
            <p class="text-muted">
                {{ $t('Meiko.Dashboard.EmptyShiftDescription') }}
            </p>
        </b-card>
    </div>
</template>

<script>
import { mapState } from 'vuex'
import ShiftLineCard from './ShiftLineCard.vue'

export default {
    name: 'ShiftView',
    components: {
        // ShiftFilters,
        ShiftLineCard,
    },
    data() {
        return {
            currentPage: 1,
            linesPerPage: 10,
        }
    },
    computed: {
        ...mapState('dashboard', [
            'shiftData',
            'selectedDate',
            'selectedShift',
        ]),
        sortedLines() {
            return Object.entries(this.shiftData || {})
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
        hasData() {
            return this.totalRows > 0
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
    methods: {
        /**
         * Emit filter change lên parent để parent xử lý fetch
         */
    },
}
</script>
