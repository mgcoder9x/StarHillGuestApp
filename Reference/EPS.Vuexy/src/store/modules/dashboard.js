/* eslint-disable dot-notation */
import {
    stepService,
    dashboardMeikoService,
    workingShiftService,
} from '@/services'
import Vue from 'vue'
import moment from 'moment'

// Module-level debounce timer for realtimeOverview refresh (not stored in Vuex state)
let _realtimeOverviewRefreshTimer = null

const today = () => new Date().toISOString().split('T')[0]
const daysAgo = (n) => {
    const d = new Date()
    d.setDate(d.getDate() - n)
    return d.toISOString().split('T')[0]
}

const ACTIVE_STATUS = 'active'
const INACTIVE_STATUS = 'inactive'

const hasAnyEventInLine = (lineData = {}) => {
    const steps = Object.values(lineData.steps || {})

    return steps.some((stepStatuses) =>
        Object.keys(stepStatuses || {}).some((key) => key !== '__no_person__')
    )
}

const normalizeLineActivity = (lineData = {}, fallbackIsActive = false) => {
    const hasEvents = hasAnyEventInLine(lineData)
    const isActive = hasEvents || fallbackIsActive

    return {
        ...lineData,
        isActive,
        status: isActive ? ACTIVE_STATUS : INACTIVE_STATUS,
    }
}

const createEmptyLine = (line) => ({
    sessionId: null,
    productionLineId: line.productionLineId,
    workFlowId: line.productionLineId,
    productionLineName: line.productionLineName,
    operators: [],
    currentStep: 0,
    steps: {},
    childrenStats: {},
    isActive: line.isActive || line.status === ACTIVE_STATUS,
    status:
        line.status ||
        (line.isActive ? ACTIVE_STATUS : INACTIVE_STATUS),
})

const mergeWithCatalogLines = (
    linesData = {},
    catalogLines = [],
    options = {}
) => {
    const { useCatalogStatus = true } = options

    if (!Array.isArray(catalogLines) || catalogLines.length === 0) {
        const normalized = {}
        Object.entries(linesData).forEach(([lineKey, lineValue]) => {
            normalized[lineKey] = normalizeLineActivity(lineValue)
        })
        return normalized
    }

    const merged = { ...linesData }
    const lineIdToKey = {}

    Object.entries(linesData).forEach(([lineKey, lineValue]) => {
        const lineId = lineValue?.productionLineId || lineValue?.workFlowId
        if (lineId) {
            lineIdToKey[lineId] = lineKey
        }
    })

    catalogLines.forEach((line) => {
        if (!line?.productionLineId) return

        const existingLineKey = lineIdToKey[line.productionLineId]
        if (existingLineKey) {
            const existingLine = merged[existingLineKey]
            merged[existingLineKey] = normalizeLineActivity({
                ...existingLine,
                productionLineName:
                    existingLine.productionLineName || line.productionLineName,
            })
            return
        }

        const syntheticKey = `line-${line.productionLineId}`
        if (!merged[syntheticKey]) {
            const fallbackActive =
                useCatalogStatus &&
                (line.status === ACTIVE_STATUS || line.isActive === true)

            merged[syntheticKey] = normalizeLineActivity(
                createEmptyLine(line),
                fallbackActive
            )
        }
    })

    Object.entries(merged).forEach(([lineKey, lineValue]) => {
        merged[lineKey] = normalizeLineActivity(lineValue)
    })

    return merged
}

const initialState = {
    viewMode: 'overview',
    selectedDate: today(),
    selectedShift: null,
    expandedLines: {},
    shiftData: {},
    realtimeData: {},
    overviewData: null,
    overviewDateFrom: daysAgo(7),
    overviewDateTo: today(),
    overviewLoading: false,
    lastUpdatedAtRealtime: null,
    lastUpdatedAtShift: null,
    lastUpdatedAtOverview: null,
    stepDefinitions: {},
    stepDefinitionsError: null,
    stepDefinitionsByWorkFlow: {},
    stepDefinitionsLoading: {},
    shiftOptions: [],
    shiftOptionsLoaded: false,
    // Shift statistics (aggregate view for "Thống kê theo Ca" tab)
    shiftStats: null,
    shiftStatsLoading: false,
    lastUpdatedAtShiftStats: null,
    // Realtime overview (top section of "Tổng quan" — all lines, current shift)
    realtimeOverviewData: null,
    realtimeOverviewLoading: false,
    lastUpdatedAtRealtimeOverview: null,
}

const mutations = {
    SET_VIEW_MODE(state, mode) {
        state.viewMode = mode
    },
    SET_SELECTED_DATE(state, date) {
        state.selectedDate = date
    },
    SET_SELECTED_SHIFT(state, shift) {
        state.selectedShift = shift
    },
    TOGGLE_LINE_EXPAND(state, lineKey) {
        state.expandedLines = {
            ...state.expandedLines,
            [lineKey]: !state.expandedLines[lineKey],
        }
    },
    SET_REALTIME_DATA(state, data) {
        state.realtimeData = data
        state.lastUpdatedAtRealtime = new Date().toISOString()
    },
    SET_SHIFT_DATA(state, data) {
        state.shiftData = data
        state.lastUpdatedAtShift = new Date().toISOString()
    },
    SET_OVERVIEW_DATA(state, data) {
        state.overviewData = data
        state.lastUpdatedAtOverview = new Date().toISOString()
    },
    SET_OVERVIEW_DATE_FROM(state, date) {
        state.overviewDateFrom = date
    },
    SET_OVERVIEW_DATE_TO(state, date) {
        state.overviewDateTo = date
    },
    SET_OVERVIEW_LOADING(state, loading) {
        state.overviewLoading = loading
    },
    UPDATE_LINE_STEP(state, { lineKey, stepNumber, operatorStatuses }) {
        if (state.realtimeData[lineKey]) {
            state.realtimeData[lineKey].steps[stepNumber] = operatorStatuses
            const isActive = hasAnyEventInLine(state.realtimeData[lineKey])
            state.realtimeData[lineKey].isActive = isActive
            state.realtimeData[lineKey].status = isActive
                ? ACTIVE_STATUS
                : INACTIVE_STATUS
        }
    },
    UPDATE_STEP(state, { lineKey, stepNumber, personNo, status }) {
        if (!state.realtimeData[lineKey]) return
        const line = state.realtimeData[lineKey]

        if (line.steps[stepNumber]) {
            if (personNo && personNo !== '__no_person__') {
                if (line.steps[stepNumber]['__no_person__']) {
                    Vue.delete(line.steps[stepNumber], '__no_person__')
                }
            }
            const operatorExists = line.operators.some(
                (op) => op.personNo === personNo
            )
            if (!operatorExists) {
                line.operators.push({ personNo, name: null })
            }
            Vue.set(line.steps[stepNumber], personNo, status)
        }

        const currentStepData = line.steps[stepNumber]
        if (currentStepData) {
            const realStatuses = Object.entries(currentStepData)
                .filter(([key]) => key !== '__no_person__')
                .map(([, s]) => s)

            const allCompleted =
                realStatuses.length > 0 &&
                realStatuses.every((s) => s === 'completed')

            if (allCompleted) {
                const nextStepNumber = stepNumber + 1
                if (line.steps[nextStepNumber]) {
                    const nextStepData = line.steps[nextStepNumber]
                    const hasRealPeople = Object.keys(nextStepData).some(
                        (k) => k !== '__no_person__'
                    )
                    if (!hasRealPeople) {
                        Vue.set(
                            line.steps[nextStepNumber],
                            '__no_person__',
                            'inProgress'
                        )
                        if (nextStepNumber > line.currentStep) {
                            line.currentStep = nextStepNumber
                        }
                    }
                }
            }
        }
        if (stepNumber > line.currentStep) {
            line.currentStep = stepNumber
        }

        const isActive = hasAnyEventInLine(line)
        line.isActive = isActive
        line.status = isActive ? ACTIVE_STATUS : INACTIVE_STATUS
    },
    SET_STEP_DEFINITIONS(state, { workFlowId, steps }) {
        state.stepDefinitionsByWorkFlow = {
            ...state.stepDefinitionsByWorkFlow,
            [workFlowId]: steps,
        }
    },
    SET_STEP_DEFINITIONS_LOADING(state, { workFlowId, loading }) {
        state.stepDefinitionsLoading = {
            ...state.stepDefinitionsLoading,
            [workFlowId]: loading,
        }
    },
    SET_SHIFT_OPTIONS(state, options) {
        state.shiftOptions = options
        state.shiftOptionsLoaded = true
    },
    SET_SHIFT_STATS(state, data) {
        state.shiftStats = data
        state.lastUpdatedAtShiftStats = new Date().toISOString()
    },
    SET_SHIFT_STATS_LOADING(state, loading) {
        state.shiftStatsLoading = loading
    },
    SET_REALTIME_OVERVIEW(state, data) {
        state.realtimeOverviewData = data
        state.lastUpdatedAtRealtimeOverview = new Date().toISOString()
    },
    SET_REALTIME_OVERVIEW_LOADING(state, loading) {
        state.realtimeOverviewLoading = loading
    },
    /**
     * Optimistically update the aggregate counts in realtimeOverviewData when a
     * StepCompleted SignalR message arrives, so the Overview tab responds instantly.
     * A server refresh is scheduled separately to keep data accurate.
     */
    PATCH_REALTIME_OVERVIEW_LINE(state, { productionLineId, status }) {
        if (!state.realtimeOverviewData) return
        const lines = state.realtimeOverviewData.lines
        if (!lines) return

        const idx = lines.findIndex(
            (l) => l.productionLineId === productionLineId
        )
        if (idx === -1) return

        const line = { ...lines[idx], isActive: true }
        const summary = { ...state.realtimeOverviewData.summary }

        if (status === 'completed') {
            line.completedCount++
            summary.totalCompleted++
        } else if (status === 'violation') {
            line.violationCount++
            summary.totalViolations++
        } else if (status === 'inProgress') {
            line.inProgressCount++
            summary.totalInProgress++
        }
        line.totalSteps++
        line.completionRate =
            line.totalSteps > 0
                ? Math.round((line.completedCount / line.totalSteps) * 100)
                : 0

        const updatedLines = [...lines]
        updatedLines[idx] = line
        state.realtimeOverviewData = {
            ...state.realtimeOverviewData,
            lines: updatedLines,
            summary,
        }
        console.log(`Patched realtime overview line ${productionLineId}:`, line)
    },

    UPDATE_CHILDREN_STATS(
        state,
        { lineKey, parentStepId, workFlowId, status }
    ) {
        const line = state.realtimeData[lineKey]
        if (!line || !line.childrenStats) return
        if (!line.childrenStats[parentStepId]) {
            Vue.set(line.childrenStats, parentStepId, {})
        }
        if (!line.childrenStats[parentStepId][workFlowId]) {
            Vue.set(line.childrenStats[parentStepId], workFlowId, {
                violationCount: 0,
                completedCount: 0,
            })
        }
        const childStats = line.childrenStats[parentStepId][workFlowId]
        if (status === 'violation') childStats.violationCount++
        else if (status === 'completed') childStats.completedCount++
    },
}

const actions = {
    setViewMode({ commit }, mode) {
        commit('SET_VIEW_MODE', mode)
    },
    setSelectedDate({ commit }, date) {
        commit('SET_SELECTED_DATE', date)
    },
    setSelectedShift({ commit }, shift) {
        commit('SET_SELECTED_SHIFT', shift)
    },
    setOverviewDateFrom({ commit }, date) {
        commit('SET_OVERVIEW_DATE_FROM', date)
    },
    setOverviewDateTo({ commit }, date) {
        commit('SET_OVERVIEW_DATE_TO', date)
    },
    toggleLineExpand({ commit }, lineKey) {
        commit('TOGGLE_LINE_EXPAND', lineKey)
    },
    updateRealtimeData({ commit }, data) {
        commit('SET_REALTIME_DATA', data)
    },
    updateShiftData({ commit }, data) {
        commit('SET_SHIFT_DATA', data)
    },
    updateLineStep({ commit }, { lineKey, stepNumber, operatorStatuses }) {
        commit('UPDATE_LINE_STEP', { lineKey, stepNumber, operatorStatuses })
    },
    updateStep({ commit }, { lineKey, stepNumber, personNo, status }) {
        commit('UPDATE_STEP', { lineKey, stepNumber, personNo, status })
    },
    updateChildrenStats(
        { commit },
        { lineKey, parentStepId, workFlowId, status }
    ) {
        commit('UPDATE_CHILDREN_STATS', {
            lineKey,
            parentStepId,
            workFlowId,
            status,
        })
    },

    async loadRealtimeDashboard({ commit, dispatch }) {
        try {
            const response = await dashboardMeikoService.getRealtimeDashboard()

            let overview = null
            try {
                overview = await dashboardMeikoService.getRealtimeOverview()
            } catch (overviewError) {
                console.warn(
                    'Failed to load realtime overview catalog, fallback to realtime data only:',
                    overviewError
                )
            }

            if (!response?.data) return

            const mergedData = mergeWithCatalogLines(
                response.data,
                overview?.lines || [],
                { useCatalogStatus: true }
            )

            commit('SET_REALTIME_DATA', mergedData)

            const catalogWorkFlowIds = (overview?.lines || [])
                .map((line) => line.productionLineId)
                .filter(Boolean)

            const allWorkFlowIds = Array.from(
                new Set([...(response.workFlowIds || []), ...catalogWorkFlowIds])
            )

            await Promise.all(
                allWorkFlowIds.map((id) => dispatch('loadStepDefinitions', id))
            )
        } catch (error) {
            console.error('Error loading realtime dashboard:', error)
            throw error
        }
    },

    async loadShiftDashboard({ dispatch }, { selectedShift, selectedDate }) {
        try {
            const response = await dashboardMeikoService.getShiftDashboard(
                selectedShift,
                selectedDate
            )

            let overview = null
            try {
                overview = await dashboardMeikoService.getRealtimeOverview()
            } catch (overviewError) {
                console.warn(
                    'Failed to load realtime overview catalog, fallback to shift data only:',
                    overviewError
                )
            }

            const mergedData = mergeWithCatalogLines(
                response?.data,
                overview?.lines || [],
                { useCatalogStatus: false }
            )

            dispatch('updateShiftData', mergedData)

            const catalogWorkFlowIds = (overview?.lines || [])
                .map((line) => line.productionLineId)
                .filter(Boolean)

            const allWorkFlowIds = Array.from(
                new Set([
                    ...(response?.workFlowIds || []),
                    ...catalogWorkFlowIds,
                ])
            )

            await Promise.all(
                allWorkFlowIds.map((id) => dispatch('loadStepDefinitions', id))
            )
        } catch (error) {
            console.error('Error loading shift dashboard:', error)
            throw error
        }
    },

    async loadOverviewDashboard({ commit, state }) {
        commit('SET_OVERVIEW_LOADING', true)
        try {
            const data = await dashboardMeikoService.getOverviewDashboard(
                state.overviewDateFrom,
                state.overviewDateTo
            )
            commit('SET_OVERVIEW_DATA', data)
        } catch (error) {
            console.error('Error loading overview dashboard:', error)
            throw error
        } finally {
            commit('SET_OVERVIEW_LOADING', false)
        }
    },

    async loadStepDefinitions({ commit, state }, workFlowId) {
        if (state.stepDefinitionsByWorkFlow[workFlowId]) {
            return state.stepDefinitionsByWorkFlow[workFlowId]
        }
        commit('SET_STEP_DEFINITIONS_LOADING', { workFlowId, loading: true })
        try {
            const response = await stepService.getStepDefinitions(workFlowId)
            if (response.data.status === 200) {
                const steps = response.data.data
                commit('SET_STEP_DEFINITIONS', { workFlowId, steps })
                return steps
            }
        } catch (error) {
            console.error(
                `Error loading step definitions for workflow ${workFlowId}:`,
                error
            )
        } finally {
            commit('SET_STEP_DEFINITIONS_LOADING', {
                workFlowId,
                loading: false,
            })
        }
        return null
    },

    /**
     * Optimistically patch a single line in realtimeOverviewData.
     * Call immediately when StepCompleted fires for instant UI feedback.
     */
    patchRealtimeOverviewLine({ commit }, { productionLineId, status }) {
        if (!productionLineId || !status) return
        commit('PATCH_REALTIME_OVERVIEW_LINE', { productionLineId, status })
    },

    /**
     * Debounced server refresh of realtimeOverviewData.
     * Call after patchRealtimeOverviewLine to eventually sync with truth.
     * Multiple rapid events collapse into a single refresh after 5 s.
     */
    scheduleRealtimeOverviewRefresh({ dispatch }) {
        if (_realtimeOverviewRefreshTimer)
            clearTimeout(_realtimeOverviewRefreshTimer)
        _realtimeOverviewRefreshTimer = setTimeout(async () => {
            _realtimeOverviewRefreshTimer = null
            try {
                await dispatch('loadRealtimeOverview')
            } catch {
                // fail silently — the next auto-refresh will correct it
            }
        }, 5000)
    },

    async loadRealtimeOverview({ commit }) {
        commit('SET_REALTIME_OVERVIEW_LOADING', true)
        try {
            const data = await dashboardMeikoService.getRealtimeOverview()
            commit('SET_REALTIME_OVERVIEW', data)
        } catch (error) {
            console.error('Error loading realtime overview:', error)
            throw error
        } finally {
            commit('SET_REALTIME_OVERVIEW_LOADING', false)
        }
    },

    async loadShiftStats({ commit, state }) {
        if (!state.selectedShift) return
        commit('SET_SHIFT_STATS_LOADING', true)
        try {
            const data = await dashboardMeikoService.getShiftStats(
                state.selectedShift,
                state.selectedDate
            )
            commit('SET_SHIFT_STATS', data)
        } catch (error) {
            console.error('Error loading shift stats:', error)
            throw error
        } finally {
            commit('SET_SHIFT_STATS_LOADING', false)
        }
    },

    async loadShiftOptions({ commit, state }) {
        if (state.shiftOptionsLoaded) return state.shiftOptions
        const data = await workingShiftService.getAllWorkingShifts()
        commit('SET_SHIFT_OPTIONS', data)
        if (!state.selectedShift) {
            const now = moment().format('HH:mm')
            const currentShift = data.find(
                (shift) => shift.startTime <= now && shift.endTime >= now
            )
            if (currentShift) {
                commit('SET_SELECTED_SHIFT', currentShift.id)
            }
        }
        return data
    },
}

const getters = {
    isOverviewMode: (state) => state.viewMode === 'overview',
    isRealTimeMode: (state) => state.viewMode === 'realtime',
    isShiftMode: (state) => state.viewMode === 'shift',

    getStepDefinitionsForLine: (state) => (lineKey) => {
        const line =
            state.viewMode === 'realtime'
                ? state.realtimeData[lineKey]
                : state.shiftData[lineKey]
        if (!line) return []
        return state.stepDefinitionsByWorkFlow[line.productionLineId] || []
    },

    isStepDefinitionsLoadedForLine: (state) => (lineKey) => {
        const line =
            state.viewMode === 'realtime'
                ? state.realtimeData[lineKey]
                : state.shiftData[lineKey]
        if (!line) return false
        return !!state.stepDefinitionsByWorkFlow[line.productionLineId]
    },

    getLineData: (state) => (lineKey) => {
        return state.viewMode === 'realtime'
            ? state.realtimeData[lineKey]
            : state.shiftData[lineKey]
    },

    isLineExpanded: (state) => (lineKey) =>
        state.expandedLines[lineKey] || false,
}

export default {
    namespaced: true,
    state: initialState,
    getters,
    mutations,
    actions,
}
