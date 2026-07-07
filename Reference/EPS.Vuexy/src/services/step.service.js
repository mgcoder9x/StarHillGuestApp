/* eslint-disable dot-notation */
import BaseService from './baseService'

export function getStatusCounts(operatorStatuses) {
    // Lọc bỏ placeholder
    const actualStatuses = Object.entries(operatorStatuses).filter(
        ([key]) => key !== '__no_person__'
    )

    if (actualStatuses.length === 0) {
        return null // Không có người tham gia
    }

    // Đếm theo từng trạng thái
    const counts = {
        completed: 0,
        violation: 0,
        inProgress: 0,
        pending: 0,
    }

    actualStatuses.forEach(([, status]) => {
        if (Object.prototype.hasOwnProperty.call(counts, status)) {
            counts[status]++
        }
    })

    return counts
}
export const getStepStatus = (operatorStatuses) => {
    if (operatorStatuses['__no_person__']) {
        return operatorStatuses['__no_person__']
    }

    if (operatorStatuses['__no_person__']) {
        return operatorStatuses['__no_person__'] // Trả về status: pending hoặc inProgress
    }

    const statuses = Object.values(operatorStatuses)
    if (statuses.length === 0) return 'pending'

    // Logic xác định trạng thái tổng hợp khi có nhiều người
    if (statuses.some((s) => s === 'violation')) return 'violation'
    if (statuses.every((s) => s === 'completed')) return 'completed'
    if (statuses.some((s) => s === 'inProgress')) return 'inProgress'
    return 'pending'
}
export function getStatusColor(status) {
    const colorMap = {
        completed: '#28a745', // green
        violation: '#ffc107', // yellow
        inProgress: '#007bff', // blue
        pending: '#6c757d', // gray
    }
    return colorMap[status] || '#6c757d'
}
export const getStepIcon = (status) => {
    const icons = {
        completed: 'lucide:circle-check-big',
        violation: 'lucide:circle-alert',
        inProgress: 'lucide:play',
        pending: 'lucide:circle',
        notApplicable: 'lucide:circle',
    }
    return icons[status] || 'circle'
}

export const getParticipatingOperators = (operatorStatuses, operators) => {
    const actualStatuses = Object.keys(operatorStatuses)
        .filter((key) => key !== '__no_person__')
        .map((x) => x.split('_')[0])

    if (actualStatuses.length === 0) {
        return [] // Không có người tham gia thực tế
    }

    return operators.filter((op) => actualStatuses.includes(op.personNo))
}

export const getLineStats = (line) => {
    const allStatuses = []
    Object.values(line.steps).forEach((stepStatuses) => {
        Object.values(stepStatuses).forEach((status) => {
            if (status !== 'notApplicable') {
                allStatuses.push(status)
            }
        })
    })
    return {
        completed: allStatuses.filter((s) => s === 'completed').length,
        violations: allStatuses.filter((s) => s === 'violation').length,
        inProgress: allStatuses.filter((s) => s === 'inProgress').length,
        pending: allStatuses.filter((s) => s === 'pending').length,
    }
}

class StepService extends BaseService {
    constructor() {
        super('/meiko/dashboard')
    }

    getStepDefinitions(workFlowId) {
        return this.get(`workflow/${workFlowId}/steps`)
    }
}

export const stepService = new StepService()
