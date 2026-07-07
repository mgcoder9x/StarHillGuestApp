// eslint-disable-next-line import/no-extraneous-dependencies
import moment from 'moment'

import BaseService from './baseService'

export class WorkingShiftService extends BaseService {
    constructor() {
        super('/working-shifts')
    }

    async getAllWorkingShifts() {
        const accessToken = this.apiClient.getUserData()
        const response = await this.get('', {
            compId: accessToken.companyId,
            statuses: 1,
        })
        return WorkingShiftService.toSelectOptions(response.data.data.data)
    }

    static toSelectOptions(workingShifts) {
        if (!workingShifts) return []
        return workingShifts.map((workingShift) => ({
            value: workingShift.id,
            startTime: workingShift.startTime,
            endTime: workingShift.endTime,
            id: workingShift.id,
            text: `${workingShift.name} ${moment(workingShift.startTime, 'HH:mm:ss').format('HH:mm')} - ${moment(
                workingShift.endTime,
                'HH:mm:ss'
            ).format('HH:mm')}`,
        }))
    }
}

export const workingShiftService = new WorkingShiftService()
