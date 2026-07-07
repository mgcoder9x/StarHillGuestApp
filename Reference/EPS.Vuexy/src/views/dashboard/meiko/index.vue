<template>
    <div class="dashboard-container">
        <!-- ── Header: Tab navigation ─────────────────────────── -->
        <div class="dashboard-header">
            <div
                class="d-flex align-items-center justify-content-between flex-wrap"
                style="gap: 8px"
            >
                <!-- Tab buttons -->
                <div class="dash-tabs">
                    <button
                        class="dash-tab"
                        :class="{ 'dash-tab--active': isOverviewMode }"
                        @click="handleViewModeChange('overview')"
                    >
                        <Icon
                            icon="lucide:layout-dashboard"
                            width="14"
                            height="14"
                        />
                        {{ $t('Meiko.Dashboard.Overview.Tab') }}
                    </button>
                    <button
                        class="dash-tab"
                        :class="{ 'dash-tab--active': isShiftMode }"
                        @click="handleViewModeChange('shift')"
                    >
                        <Icon icon="lucide:clock-4" width="14" height="14" />
                        {{ $t('Meiko.Dashboard.ShiftStats') }}
                    </button>
                </div>

                <!-- Right: shift filter / last updated -->
                <div
                    class="d-flex align-items-center flex-wrap"
                    style="gap: 10px"
                >
                    <ShiftFilters
                        v-if="isShiftMode"
                        :is-loading="isLoading"
                        @filter-changed="handleShiftFilterChange"
                    />
                    <small
                        v-if="displayLastUpdatedAt && !isOverviewMode"
                        class="hdr-updated-at"
                    >
                        <Icon
                            icon="lucide:refresh-cw"
                            width="11"
                            height="11"
                            class="mr-1"
                        />
                        {{ $t('Meiko.Dashboard.UpdatedAt') }}:
                        <strong>{{ displayLastUpdatedAt }}</strong>
                    </small>
                </div>
            </div>
        </div>

        <!-- ── Loading ───────────────────────────────────────── -->
        <div v-if="isLoading" class="text-center py-5">
            <b-spinner variant="primary" style="width: 3rem; height: 3rem" />
            <p class="mt-3 text-muted">
                {{ $t('Meiko.Dashboard.LoadingDashboard') }}
            </p>
        </div>

        <!-- ── Error ─────────────────────────────────────────── -->
        <b-card v-else-if="error" class="text-center py-5">
            <Icon
                icon="lucide:alert-triangle"
                width="48"
                height="48"
                class="text-danger mb-3"
            />
            <h5>{{ $t('Meiko.Dashboard.ErrorTitle') }}</h5>
            <p class="text-muted">{{ error }}</p>
            <b-button variant="primary" @click="refresh">
                <Icon
                    icon="lucide:refresh-cw"
                    width="14"
                    height="14"
                    class="mr-1"
                />
                {{ $t('Meiko.Dashboard.Retry') }}
            </b-button>
        </b-card>

        <!-- ── Content ───────────────────────────────────────── -->
        <template v-else>
            <!-- OVERVIEW TAB -->
            <template v-if="isOverviewMode">
                <OverviewView v-if="hasOverviewData" />
                <b-card v-else>
                    <div class="text-center py-5">
                        <Icon
                            icon="lucide:bar-chart-2"
                            width="48"
                            height="48"
                            class="text-muted mb-3"
                        />
                        <h5>{{ $t('Meiko.Dashboard.Overview.EmptyTitle') }}</h5>
                        <p class="text-muted mb-3">
                            {{
                                $t('Meiko.Dashboard.Overview.EmptyDescription')
                            }}
                        </p>
                        <!-- Inline filter for first load -->
                        <div class="d-flex justify-content-center mb-3">
                            <OverviewFilters
                                :is-loading="overviewLoading"
                                @apply="handleInitialOverviewLoad"
                            />
                        </div>
                    </div>
                </b-card>
            </template>

            <!-- REALTIME TAB -->
            <!-- <template v-else-if="isRealTimeMode">
                <RealtimeView
                    v-if="hasRealtimeData"
                    @error="handleChildError"
                />
                <b-card v-else>
                    <div class="text-center py-5">
                        <Icon
                            icon="lucide:factory"
                            width="48"
                            height="48"
                            class="text-muted mb-3"
                        />
                        <h5>{{ $t('Meiko.Dashboard.EmptyRealtimeTitle') }}</h5>
                        <p class="text-muted">
                            {{ $t('Meiko.Dashboard.EmptyRealtimeDescription') }}
                        </p>
                        <b-button
                            variant="outline-primary"
                            size="sm"
                            @click="refresh"
                        >
                            <Icon
                                icon="lucide:refresh-cw"
                                width="13"
                                height="13"
                                class="mr-1"
                            />
                            {{ $t('Meiko.Dashboard.Refresh') }}
                        </b-button>
                    </div>
                </b-card>
            </template> -->

            <!-- SHIFT TAB -->
            <ShiftView v-else @error="handleChildError" />
        </template>
    </div>
</template>

<script>
import { mapActions, mapGetters, mapState } from 'vuex'
import signalRService from '@/utils/signalr-service'
import ShiftView from './components/shift/ShiftView.vue'
import ShiftFilters from './components/shift/ShiftFilters.vue'
import OverviewView from './components/overview/OverviewView.vue'
import OverviewFilters from './components/overview/OverviewFilters.vue'

export default {
    name: 'Dashboard',
    components: {
        ShiftView,
        ShiftFilters,
        OverviewView,
        OverviewFilters,
    },
    data() {
        return {
            isLoading: false,
            error: null,
            isInitialized: false,
            isSignalRConnected: false,
            isConnecting: false,
            connectionError: null,
            pendingRealtimeReloadTimer: null,
        }
    },
    computed: {
        ...mapGetters('dashboard', [
            'isOverviewMode',
            'isShiftMode',
        ]),
        ...mapState('dashboard', [
            'realtimeData',
            'shiftData',
            'overviewData',
            'overviewLoading',
            'overviewDateFrom',
            'overviewDateTo',
            'selectedDate',
            'selectedShift',
            'shiftOptions',
            'lastUpdatedAtRealtime',
            'lastUpdatedAtShift',
            'lastUpdatedAtShiftStats',
        ]),
        hasRealtimeData() {
            return Object.keys(this.realtimeData || {}).length > 0
        },
        hasOverviewData() {
            if (!this.overviewData) return false
            const s = this.overviewData.summary || {}
            return (
                Object.keys(this.overviewData.lines || {}).length > 0 ||
                s.totalCompleted > 0 ||
                s.totalViolations > 0 ||
                s.totalInProgress > 0 ||
                s.totalPending > 0
            )
        },
        compId() {
            return this.$services.getUserData().companyId
        },
        displayLastUpdatedAt() {
            const ts = this.isRealTimeMode
                ? this.lastUpdatedAtRealtime
                : this.isShiftMode
                    ? this.lastUpdatedAtShiftStats
                    : this.lastUpdatedAtShift
            if (!ts) return null
            return new Intl.DateTimeFormat(this.$i18n.locale || 'vi', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
            }).format(new Date(ts))
        },
        selectedShiftText() {
            const found = (this.shiftOptions || []).find(
                (item) => item.value === this.selectedShift
            )
            return found
                ? found.text
                : this.$t('Meiko.Dashboard.NoShiftSelected')
        },
    },

    async mounted() {
        this.setViewMode('overview')
        await this.initializeDashboard()
        await this.connectSignalR()
    },

    beforeDestroy() {
        this.disconnectSignalR()
        if (this.pendingRealtimeReloadTimer) {
            clearTimeout(this.pendingRealtimeReloadTimer)
            this.pendingRealtimeReloadTimer = null
        }
    },

    methods: {
        ...mapActions('dashboard', [
            'loadRealtimeDashboard',
            'loadShiftDashboard',
            'loadShiftStats',
            'loadOverviewDashboard',
            'loadRealtimeOverview',
            'patchRealtimeOverviewLine',
            'scheduleRealtimeOverviewRefresh',
            'setViewMode',
            'updateStep',
            'updateChildrenStats',
            'loadShiftOptions',
        ]),

        async initializeDashboard() {
            this.isLoading = true
            this.error = null
            try {
                if (this.isOverviewMode) {
                    await this.loadOverviewDashboard()
                } else if (this.isRealTimeMode) {
                    await this.loadRealtimeDashboard()
                } else {
                    await this.loadShiftOptions()
                    if (!this.selectedDate || !this.selectedShift) return
                    await this.loadShiftStats()
                }
                this.isInitialized = true
            } catch (error) {
                console.error('Error initializing dashboard:', error)
                this.error = this.$t('Meiko.Dashboard.ErrorLoadDashboard')
            } finally {
                this.isLoading = false
            }
        },

        async handleInitialOverviewLoad() {
            this.isLoading = true
            this.error = null
            try {
                await this.loadOverviewDashboard()
            } catch (error) {
                this.error = this.$t('Meiko.Dashboard.ErrorLoadDashboard')
            } finally {
                this.isLoading = false
            }
        },

        async refresh() {
            await this.initializeDashboard()
        },

        handleChildError(errorMessage) {
            this.$bvToast.toast(errorMessage, {
                title: this.$t('Meiko.Dashboard.ErrorTitle'),
                variant: 'danger',
                solid: true,
            })
        },

        async handleShiftFilterChange() {
            this.isLoading = true
            this.error = null
            try {
                await this.loadShiftStats()
            } catch (error) {
                console.error('Error loading shift stats:', error)
                this.error = this.$t('Meiko.Dashboard.ErrorLoadShift')
            } finally {
                this.isLoading = false
            }
        },

        async handleViewModeChange(mode) {
            const isCurrent =
                (mode === 'overview' && this.isOverviewMode) ||
                (mode === 'realtime' && this.isRealTimeMode) ||
                (mode === 'shift' && this.isShiftMode)
            if (isCurrent) return

            this.isLoading = true
            this.error = null
            try {
                this.setViewMode(mode)
                if (mode === 'overview') {
                    await this.loadOverviewDashboard()
                } else if (mode === 'realtime') {
                    await this.loadRealtimeDashboard()
                } else {
                    await this.loadShiftOptions()
                    if (!this.selectedDate || !this.selectedShift) return
                    await this.loadShiftStats()
                }
            } catch (error) {
                console.error(`Error loading ${mode}:`, error)
                this.error = this.$t('Meiko.Dashboard.ErrorLoadDashboard')
            } finally {
                this.isLoading = false
            }
        },

        async connectSignalR() {
            if (this.isConnecting) return
            this.isConnecting = true
            this.connectionError = null
            try {
                await signalRService.connect('notificationHub')
                this.isSignalRConnected = true
                this.setupEventHandlers()
            } catch (error) {
                console.error('SignalR connection error:', error)
                this.isSignalRConnected = false
                this.connectionError = error.message
            } finally {
                this.isConnecting = false
            }
        },

        setupEventHandlers() {
            signalRService.on('StepCompleted', this.handleStepCompleted)
            signalRService.on('NewEvent', this.handleNewEvent)
        },

        disconnectSignalR() {
            signalRService.off('StepCompleted', this.handleStepCompleted)
            signalRService.off('NewEvent', this.handleNewEvent)
            signalRService.disconnect()
            this.isSignalRConnected = false
        },

        handleStepCompleted(data) {
            // ── Step-level update for the Realtime detail view ──────────
            const stepToUpdate = data.parentStepId || data.stepId
            this.updateStep({
                lineKey: data.lineKey,
                stepNumber: stepToUpdate,
                personNo: data.personNo,
                status: data.status,
            })
            if (data.workFlowId && data.parentStepId) {
                this.updateChildrenStats({
                    lineKey: data.lineKey,
                    parentStepId: data.parentStepId,
                    workFlowId: data.workFlowId,
                    status: data.status,
                })
            }

            // ── Aggregate update for Overview tab (realtime overview) ───
            // data.productionLineId is now explicit in the payload
            if (data.productionLineId) {
                this.patchRealtimeOverviewLine({
                    productionLineId: data.productionLineId,
                    status: data.status,
                })
                this.scheduleRealtimeOverviewRefresh()
            }

            // ── Toast notification ───────────────────────────────────────
            const stepLabel = data.stepName || data.workFlowName || `#${data.stepId}`
            // this.$bvToast.toast(
            //     this.$t('Meiko.Dashboard.StepStarted.Message', { stepName: stepLabel }),
            //     {
            //         title: this.$t('Meiko.Dashboard.StepStarted.Title'),
            //         variant: data.status === 'violation' ? 'warning' : 'info',
            //         autoHideDelay: 3000,
            //         solid: true,
            //     }
            // )
        },

        handleNewEvent(eventData) {
            const parsedData =
                typeof eventData === 'string' ? JSON.parse(eventData) : eventData
            if (!parsedData.ProductionLineEvent) return

            const productionLineId = parsedData.ProductionLineEvent.ProductionLineId
            const lineKey = `line${productionLineId}_compId${this.compId}`
            const isNewLine = !Object.prototype.hasOwnProperty.call(
                this.realtimeData,
                lineKey
            )
            if (isNewLine) this.handleNewLineDetected()

            // Trigger overview refresh on any new event
            this.scheduleRealtimeOverviewRefresh()
        },

        async handleNewLineDetected() {
            if (this.pendingRealtimeReloadTimer) return
            this.pendingRealtimeReloadTimer = setTimeout(async () => {
                try {
                    await this.loadRealtimeDashboard()
                    this.$bvToast.toast(
                        this.$t('Meiko.Dashboard.LineStarted.Message'),
                        {
                            title: this.$t('Meiko.Dashboard.LineStarted.Title'),
                            variant: 'success',
                            autoHideDelay: 5000,
                            solid: true,
                        }
                    )
                } catch (error) {
                    console.error('Error reloading realtime:', error)
                } finally {
                    clearTimeout(this.pendingRealtimeReloadTimer)
                    this.pendingRealtimeReloadTimer = null
                }
            }, 800)
        },
    },
}
</script>

<style lang="scss">
@import '~@core/scss/base/pages/dashboard-des.scss';
</style>
