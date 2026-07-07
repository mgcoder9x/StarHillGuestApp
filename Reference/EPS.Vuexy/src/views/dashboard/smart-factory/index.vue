<template>
    <div class="dashboard-container">
        <!-- Header -->
        <div class="dashboard-header">
            <b-row align-v="center" class="align-items-center">
                <b-col md="6" class="text-md-left mr-auto">
                    <b-button-group>
                        <b-button
                            :variant="
                                isRealTimeMode ? 'primary' : 'outline-secondary'
                            "
                            @click="handleViewModeChange('realtime')"
                        >
                            <Icon
                                icon="lucide:activity"
                                width="24"
                                height="24"
                                class="mr-1"
                            />
                            {{ $t('Meiko.Dashboard.Realtime') }}
                        </b-button>
                        <b-button
                            :variant="
                                isShiftMode ? 'primary' : 'outline-secondary'
                            "
                            @click="handleViewModeChange('shift')"
                        >
                            <Icon
                                icon="lucide:clock-4"
                                width="24"
                                height="24"
                                class="mr-1"
                            />
                            {{ $t('Meiko.Dashboard.ShiftTab') }}
                        </b-button>
                    </b-button-group>
                </b-col>
                <b-col md="6" class="text-md-right ml-auto" v-if="isShiftMode">
                    <ShiftFilters @filter-changed="handleShiftFilterChange" />
                </b-col>
            </b-row>
        </div>
        <!-- Loading State -->
        <div v-if="isLoading" class="text-center py-5">
            <b-spinner
                variant="primary"
                style="width: 3rem; height: 3rem"
            ></b-spinner>
            <p class="mt-3 text-muted">
                {{ $t('Meiko.Dashboard.LoadingDashboard') }}
            </p>
        </div>
        <!-- Error State -->
        <b-card v-else-if="error" class="text-center py-5">
            <i class="fas fa-exclamation-triangle fa-3x text-danger mb-3"></i>
            <h5>{{ $t('Meiko.Dashboard.ErrorTitle') }}</h5>
            <p class="text-muted">{{ error }}</p>
            <b-button variant="primary" @click="refresh">
                <i class="fas fa-sync mr-1"></i>
                {{ $t('Meiko.Dashboard.Retry') }}
            </b-button>
        </b-card>

        <template v-else>
            <template v-if="isRealTimeMode">
                <RealtimeView v-if="hasData" @error="handleChildError" />
                <!-- Empty State  -->
                <b-card v-else>
                    <div class="text-center py-5">
                        <i class="fas fa-industry fa-3x text-muted mb-3"></i>
                        <h5>{{ $t('Meiko.Dashboard.EmptyRealtimeTitle') }}</h5>
                        <p class="text-muted">
                            {{
                                $t(
                                    'Meiko.Dashboard.EmptyRealtimeDescription'
                                )
                            }}
                        </p>
                        <b-button variant="primary" @click="refresh">
                            <i class="fas fa-sync mr-1"></i>
                            {{ $t('Meiko.Dashboard.Refresh') }}
                        </b-button>
                    </div>
                </b-card>
            </template>
            <ShiftView v-else @filter-changed="" @error="handleChildError" />
        </template>
    </div>
</template>

<script>
import { mapActions, mapGetters, mapState } from 'vuex'
import signalRService from '@/utils/signalr-service'
import RealtimeView from './components/realtime/RealtimeView.vue'
import ShiftView from './components/shift/ShiftView.vue'
import ShiftFilters from './components/shift/ShiftFilters.vue'

export default {
    name: 'Dashboard',
    components: {
        RealtimeView,
        ShiftView,
        ShiftFilters,
    },
    data() {
        return {
            isLoading: false,
            error: null,
            isInitialized: false,
            isSignalRConnected: false,
            isConnecting: false,
            connectionError: null,
        }
    },
    computed: {
        ...mapGetters('dashboard', ['isRealTimeMode', 'isShiftMode']),
        ...mapState('dashboard', [
            'realtimeData',
            'shiftData',
            'stepDefinitionsLoading',
            'selectedDate',
            'selectedShift',
        ]),
        hasData() {
            if (this.isRealTimeMode) {
                return Object.keys(this.realtimeData || {}).length > 0
            }
            return Object.keys(this.shiftData || {}).length > 0
        },
        connectionStatusVariant() {
            if (this.isConnecting) return 'warning'
            if (this.connectionError) return 'danger'
            return this.isSignalRConnected ? 'success' : 'danger'
        },

        connectionIcon() {
            if (this.isConnecting) return 'fa6-solid:spinner'
            if (this.isSignalRConnected) return 'fa6-solid:wifi'
            return 'fa6-solid:exclamation-triangle'
        },

        connectionStatusText() {
            if (this.isConnecting) {
                return this.$t('Meiko.Dashboard.Connection.Connecting')
            }
            if (this.connectionError) {
                return this.$t('Meiko.Dashboard.Connection.Error')
            }
            return this.isSignalRConnected
                ? this.$t('Meiko.Dashboard.Connection.Connected')
                : this.$t('Meiko.Dashboard.Connection.Disconnected')
        },
        compId() {
            return this.$services.getUserData().companyId
        },
    },

    async mounted() {
        // this.refreshInterval = setInterval(() => {
        //     if (this.isRealTimeMode) {
        //         this.refreshDashboard()
        //     }
        // }, 30000)
        this.setViewMode('realtime') // Set default mode to realtime
        this.initializeDashboard()
        await this.connectSignalR()
    },

    beforeDestroy() {
        this.disconnectSignalR()
    },
    methods: {
        ...mapActions('dashboard', [
            'loadRealtimeDashboard',
            'loadShiftDashboard',
            'setViewMode',
            'updateStep',
            'updateChildrenStats',
            'loadShiftOptions',
        ]),
        async initializeDashboard() {
            this.isLoading = true
            this.error = null
            try {
                if (this.isRealTimeMode) {
                    await this.loadRealtimeDashboard()
                } else {
                    if (!this.selectedDate || !this.selectedShift) {
                        return
                    }
                    await this.loadShiftDashboard({
                        selectedShift: this.selectedShift,
                        selectedDate: this.selectedDate,
                    })
                }
                this.isInitialized = true
            } catch (error) {
                console.error('Error initializing dashboard:', error)
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
        /**
         * Xử lý thay đổi filter từ ShiftView
         */
        async handleShiftFilterChange() {
            this.isLoading = true
            this.error = null

            try {
                await this.loadShiftDashboard({
                    selectedShift: this.selectedShift,
                    selectedDate: this.selectedDate,
                })
            } catch (error) {
                console.error('Error loading shift data:', error)
                this.error = this.$t('Meiko.Dashboard.ErrorLoadShift')
            } finally {
                this.isLoading = false
            }
        },
        /**
         * Xử lý thay đổi view mode
         * Fetch data TRƯỚC khi chuyển mode
         */
        async handleViewModeChange(mode) {
            // Không làm gì nếu đã ở mode đó
            if (
                (mode === 'realtime' && this.isRealTimeMode) ||
                (mode === 'shift' && this.isShiftMode)
            ) {
                return
            }

            this.isLoading = true
            this.error = null

            try {
                // Chuyển mode
                this.setViewMode(mode)
                // Fetch data của mode mới TRƯỚC
                if (mode === 'realtime') {
                    await this.loadRealtimeDashboard()
                } else {
                    // Fetch shift options TRƯỚC (nếu chưa có)
                    await this.loadShiftOptions()
                    if (!this.selectedDate || !this.selectedShift) {
                        return
                    }
                    await this.loadShiftDashboard({
                        selectedShift: this.selectedShift,
                        selectedDate: this.selectedDate,
                    })
                }
            } catch (error) {
                console.error(`Error loading ${mode} data:`, error)
                this.error =
                    mode === 'realtime'
                        ? this.$t('Meiko.Dashboard.ErrorLoadRealtime')
                        : this.$t('Meiko.Dashboard.ErrorLoadShift')
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

                // Setup event handlers
                this.setupEventHandlers()
            } catch (error) {
                console.error('SignalR connection error:', error)
                this.isSignalRConnected = false
                this.connectionError = error.message
                this.$emit('error', this.$t('Meiko.Dashboard.ErrorSignalR'))
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
            // Use parentStepId for dashboard update (aggregated parent status)
            // Fall back to stepId for compatibility with items without parent
            const stepToUpdate = data.parentStepId || data.stepId

            // Update store with parent step status
            this.updateStep({
                lineKey: data.lineKey,
                stepNumber: stepToUpdate,
                personNo: data.personNo,
                status: data.status,
            })

            // Update childrenStats if this is a child step (has workFlowId and parentStepId)
            if (data.workFlowId && data.parentStepId) {
                this.updateChildrenStats({
                    lineKey: data.lineKey,
                    parentStepId: data.parentStepId,
                    workFlowId: data.workFlowId,
                    status: data.status,
                })
            }

            // Show notification with original step info
            this.$bvToast.toast(
                this.$t('Meiko.Dashboard.StepStarted.Message', {
                    stepName: data.workFlowName,
                }),
                {
                    title: this.$t('Meiko.Dashboard.StepStarted.Title'),
                    variant: 'info',
                    autoHideDelay: 3000,
                    solid: true,
                }
            )
        },
        /**
         * Xử lý sự kiện mới từ production line
         */
        handleNewEvent(eventData) {
            const parsedData =
                typeof eventData === 'string'
                    ? JSON.parse(eventData)
                    : eventData

            if (!parsedData.ProductionLineEvent) return

            const productionLineId =
                parsedData.ProductionLineEvent.ProductionLineId
            const lineKey = `line${productionLineId}_compId${this.compId}`
            const isNewLine = !Object.prototype.hasOwnProperty.call(
                this.realtimeData,
                lineKey
            )

            if (isNewLine) {
                this.handleNewLineDetected()
            }
        },

        /**
         * Xử lý khi phát hiện line mới
         */
        async handleNewLineDetected() {
            try {
                // Reload data để có line mới
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
                console.error('Error loading new line:', error)
                this.$emit('error', this.$t('Meiko.Dashboard.ErrorLoadNewLine'))
            }
        },
        /**
         * Xử lý click vào badge connection
         */
        handleConnectionClick() {
            if (!this.isSignalRConnected && !this.isConnecting) {
                this.connectSignalR()
            }
        },
    },
}
</script>
<style lang="scss">
@import '~@core/scss/base/pages/dashboard-des.scss';
</style>
