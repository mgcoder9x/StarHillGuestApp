import useJwt from '@/auth/jwt/useJwt'

export default class BaseService {
    constructor(baseEndpoint) {
        this.baseEndpoint = baseEndpoint
        this.apiClient = window.$services || this.$services || useJwt
    }

    async get(endPoint, options = {}) {
        const url = this.buildUrl(endPoint)
        return new Promise((resolve, reject) => {
            this.apiClient.get(url, options).then(resolve).catch(reject)
        })
    }

    async post(endPoint, data = {}, options = {}) {
        const url = this.buildUrl(endPoint)
        return new Promise((resolve, reject) => {
            this.apiClient.post(url, data, options).then(resolve).catch(reject)
        })
    }

    async put(endpoint, data = {}, options = {}) {
        const url = this.buildUrl(endpoint)
        return new Promise((resolve, reject) => {
            this.apiClient.put(url, data, options).then(resolve).fail(reject)
        })
    }

    async delete(endpoint, options = {}) {
        const url = this.buildUrl(endpoint)
        return new Promise((resolve, reject) => {
            this.apiClient.delete(url, options).then(resolve).fail(reject)
        })
    }

    buildUrl(endpoint) {
        return endpoint ? `${this.baseEndpoint}/${endpoint}` : this.baseEndpoint
    }

    static async retryRequest(requestFn, retries = 3, delay = 1000) {
        try {
            return await requestFn()
        } catch (error) {
            if (retries > 1) {
                await new Promise((resolve) => setTimeout(resolve, delay))
                return this.retryRequest(requestFn, retries - 1, delay * 2)
            }
            throw error
        }
    }
}
