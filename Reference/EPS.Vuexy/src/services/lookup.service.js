import BaseService from './baseService'

class LookupService extends BaseService {
    constructor() {
        super('/lookup')
    }
    async loadStepOptions() {
        try {
            const response = await this.get('steps')
            return this.transformIdTextOptions(response.data.data)
        } catch (error) {
            console.error('error', error)
        }
    }
    async loadDeviceOptions() {
        try {
            const response = await this.get('devices')
            return this.transformIdTextOptions(response.data.data)
        } catch (error) {
            console.error('error', error)
        }
    }
    async loadEventTypeOptions() {
        try {
            const response = await this.get('eventType')
            return this.transformIdTextOptions(response.data.data)
        } catch (error) {
            console.error('error', error)
        }
    }
    async loadEventWarningLevels() {
        try {
            const res = await this.get('workflow-event-warning-levels')
            return this.transformIdTextOptions(res.data.data)
        } catch (error) {
            console.error('error', error)
        }
    }
    transformIdTextOptions(data) {
        return data.map((item) => ({
            ...item,
            id: parseInt(item.id),
        }))
    }
}

const lookupService = new LookupService()
export default lookupService
