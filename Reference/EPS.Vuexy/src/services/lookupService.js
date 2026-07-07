import useJwt from '@/auth/jwt/useJwt'
import TreeHelper from '@/utils/treeHelper'

// eslint-disable-next-line import/prefer-default-export
export const lookupService = {
    url: '/lookup',
    async getManufacturer(params) {
        const { data } = await useJwt.get(`${this.url}/manufacturers`, {
            params,
        })
        return data?.data
    },
    async getAreas(params) {
        const { data } = await useJwt.get('/lookup/areas', { params })
        return data?.data
    },
    async getAreasTree(params = {}) {
        const { data } = await useJwt.get('/lookup/areas-tree', { params })
        return TreeHelper.removeEmptyChildren(data?.data)
    },
    async getDevices(params) {
        const { data } = await useJwt.get('/lookup/devices', { params })
        return data?.data
    },
    async getDepartments(params) {
        const { data } = await useJwt.get('/lookup/departments', { params })
        if (data?.status === 200) {
            return data.data
        }
        return []
    },
    async getContractor(params) {
        const { data } = await useJwt.get('/lookup/departments?type=2', {
            params,
        })
        if (data?.status === 200) {
            return data.data
        }
        return []
    },
    async getPropertiesClass(eventType) {
        try {
            const { data } = await useJwt.get(`/lookup/properties/${eventType}`)
            if (data?.status === 200) {
                return data.data
            }
        } catch (err) {
            console.log('Failed to fetch event files')
        }
        return []
    },
    async getEventSuggestion(eventType) {
        try {
            const { data } = await useJwt.get(
                `/lookup/event-suggestion/${eventType}`
            )
            if (data?.status === 200) {
                return data.data
            }
        } catch (err) {
            console.log('Failed to fetch event files')
        }
        return []
    },
    async getWarningLevels(params) {
        const { data } = await useJwt.get('/lookup/water-warning', { params })
        if (data?.status === 200) {
            return data.data
        }
        return {}
    },
    async getEventFiles(eventId) {
        try {
            const { data } = await useJwt.get(
                `/event/eventFilesById/${eventId}`
            )
            if (data?.status === 200) {
                return data.data
            }
        } catch (err) {
            console.log('Failed to fetch event files')
        }
        return []
    },
    async getEventTypes() {
        const { data } = await useJwt.get('/lookup/eventType')
        if (data?.status === 200) {
            return data.data
        }
        return []
    },
    async fetchDevices(params = {}) {
        const { data } = await useJwt.get('/lookup/devices', { params })
        return data?.data
    },
}
