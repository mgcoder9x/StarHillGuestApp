import BaseService from './baseService'

const mapLines = (linesObj) => {
    const mappedData = {}
    const workFlowIds = new Set()
    Object.entries(linesObj || {}).forEach(([lineKey, lineData]) => {
        workFlowIds.add(lineData.workFlowId)
        mappedData[lineKey] = {
            sessionId: lineData.sessionId,
            productionLineId: lineData.workFlowId, // Keep variable name if other components rely on it, but use workFlowId value
            workFlowId: lineData.workFlowId,
            productionLineName: lineData.productionLineName,
            operators: lineData.persons || [],
            currentStep: lineData.currentStepId,
            steps: {},
            childrenStats: lineData.childrenStats || {},
        }
        Object.entries(lineData.steps || {}).forEach(
            ([stepId, personStatuses]) => {
                const stepNumber = parseInt(stepId, 10)
                mappedData[lineKey].steps[stepNumber] = {}
                Object.entries(personStatuses || {}).forEach(
                    ([personNo, statusInfo]) => {
                        mappedData[lineKey].steps[stepNumber][personNo] =
                            statusInfo?.status ?? statusInfo
                    }
                )
            }
        )
    })
    return { data: mappedData, workFlowIds: Array.from(workFlowIds) }
}

class DashboardMeikoService extends BaseService {
    constructor() {
        super('/meiko/dashboard')
    }

    async getRealtimeDashboard() {
        try {
            const { data } = await this.get('realtime')
            return mapLines(data?.data?.lines)
        } catch (error) {
            throw new Error(`getRealtimeDashboard: ${error.message}`)
        }
    }

    async getShiftDashboard(shiftId, date) {
        try {
            const { data } = await this.get(`shift/${shiftId}`, { date })
            return mapLines(data?.data?.lines)
        } catch (error) {
            throw new Error(`getShiftDashboard: ${error.message}`)
        }
    }

    /**
     * GET /api/meiko/dashboard/realtime-overview
     * Returns realtime data for the current shift — ALL production lines.
     */
    async getRealtimeOverview() {
        try {
            const { data } = await this.get('realtime-overview')
            const raw = data?.data || {}
            return {
                hasActiveShift: raw.hasActiveShift ?? false,
                shiftName: raw.shiftName ?? '',
                shiftTimeRange: raw.shiftTimeRange ?? '',
                summary: {
                    totalCompleted: raw.summary?.totalCompleted ?? 0,
                    totalViolations: raw.summary?.totalViolations ?? 0,
                    totalInProgress: raw.summary?.totalInProgress ?? 0,
                    totalPending: raw.summary?.totalPending ?? 0,
                },
                lines: (raw.lines || []).map((l) => ({
                    productionLineId:
                        l.productionLineId ?? l.workFlowId ?? l.workflowId ?? 0,
                    status:
                        l.status ?? ((l.isActive ?? false) ? 'active' : 'inactive'),
                    productionLineName: l.productionLineName ?? '',
                    isActive: l.isActive ?? false,
                    completedCount: l.completedCount ?? 0,
                    violationCount: l.violationCount ?? 0,
                    inProgressCount: l.inProgressCount ?? 0,
                    pendingCount: l.pendingCount ?? 0,
                    totalSteps: l.totalSteps ?? 0,
                    completionRate: l.completionRate ?? 0,
                })),
                recentViolations: (raw.recentViolations || []).map((v) => ({
                    productionLineName: v.productionLineName ?? '-',
                    stepName: v.stepName ?? '-',
                    eventTypeName: v.eventTypeName ?? 'Vi phạm',
                    time: v.time,
                    personNo: v.personNo ?? '-',
                })),
            }
        } catch (error) {
            throw new Error(`getRealtimeOverview: ${error.message}`)
        }
    }

    async getOverviewDashboard(dateFrom, dateTo) {
        try {
            const { data } = await this.get('overview', { dateFrom, dateTo })
            const raw = data?.data || {}
            return {
                summary: {
                    totalCompleted: raw.summary?.totalCompleted ?? 0,
                    totalViolations: raw.summary?.totalViolations ?? 0,
                    totalInProgress: raw.summary?.totalInProgress ?? 0,
                    totalPending: raw.summary?.totalPending ?? 0,
                },
                lines: raw.lines || {},
                recentViolations: (raw.recentViolations || []).map((v) => ({
                    productionLineName: v.productionLineName ?? '-',
                    stepName: v.stepName ?? '-',
                    eventTypeName: v.eventTypeName ?? 'Vi phạm',
                    time: v.time,
                    personNo: v.personNo ?? '-',
                })),
                dailyStats: raw.dailyStats || [],
                dailyErrorRates: raw.dailyErrorRates || [],
            }
        } catch (error) {
            throw new Error(`getOverviewDashboard: ${error.message}`)
        }
    }

    /**
     * GET /api/meiko/dashboard/chart
     * Returns daily performance (completed count) and error-rate (%) per workflow.
     *
     * @param {string} dateFrom  "YYYY-MM-DD"
     * @param {string} dateTo    "YYYY-MM-DD"
     * @returns {{ dates: string[], performance: Series[], errorRate: Series[] }}
     *   Series = { workflowId, name, data: number[] }
     */
    async getChartData(dateFrom, dateTo) {
        try {
            const { data } = await this.get('chart', { dateFrom, dateTo })
            const raw = data?.data || {}
            return {
                dates: raw.dates || [],
                performance: raw.performance || [],
                errorRate: raw.errorRate || [],
            }
        } catch (error) {
            throw new Error(`getChartData: ${error.message}`)
        }
    }

    /**
     * GET /api/meiko/dashboard/shift-stats/{shiftId}?date=YYYY-MM-DD
     * Returns aggregate shift statistics for the "Thống kê theo Ca" tab.
     */
    async getShiftStats(shiftId, date) {
        try {
            const { data } = await this.get(`shift-stats/${shiftId}`, { date })
            const raw = data?.data || {}
            return {
                shiftName: raw.shiftName ?? '',
                shiftTimeRange: raw.shiftTimeRange ?? '',
                date: raw.date ?? date,
                lines: raw.lines || [],
                hours: raw.hours || [],
                hourlyViolations: raw.hourlyViolations || [],
                topViolatedSteps: raw.topViolatedSteps || [],
                focusedLineName: raw.focusedLineName ?? '',
                focusedLineTopSteps: raw.focusedLineTopSteps || [],
            }
        } catch (error) {
            throw new Error(`getShiftStats: ${error.message}`)
        }
    }

    getPersonProgress(personNo, shiftId) {
        return this.get(`/sop/progress/${personNo}`, { params: { shiftId } })
    }

    getStepDefinition(stepNumber) {
        return this.get(`/sop/step-definition/${stepNumber}`)
    }
}

export default new DashboardMeikoService()
