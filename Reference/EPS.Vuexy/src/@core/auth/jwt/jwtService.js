// eslint-disable-next-line import/no-cycle
import router from '@/router'
import jwtDefaultConfig from './jwtDefaultConfig'

export default class JwtService {
    // Will be used by this service for making API calls
    axiosIns = null

    // jwtConfig <= Will be used by this service
    jwtConfig = { ...jwtDefaultConfig }

    // For Refreshing Token
    isAlreadyFetchingAccessToken = false

    // Promise that resolves when token refresh completes
    refreshTokenPromise = null

    // For Refreshing Token - queue of pending requests
    subscribers = []

    constructor(axiosIns, jwtOverrideConfig) {
        this.axiosIns = axiosIns
        this.jwtConfig = { ...this.jwtConfig, ...jwtOverrideConfig }

        // Cross-tab token sync
        this.setupCrossTabSync()

        // Request Interceptor with proactive token refresh
        this.axiosIns.interceptors.request.use(
            async (config) => {
                // Get token from localStorage
                const accessToken = this.getToken()

                // Skip proactive refresh for token endpoint (login, refresh, logout)
                const isTokenEndpoint =
                    config.url && config.url.includes('/token/')

                // If token is present and not a token endpoint request
                if (accessToken && !isTokenEndpoint) {
                    // Check if token needs refresh and use shared promise to avoid race condition
                    if (this.isTokenExpiringSoon()) {
                        try {
                            await this.ensureTokenRefreshed()
                        } catch (e) {
                            // Refresh failed, will be handled by response interceptor
                            console.warn('Proactive token refresh failed:', e)
                        }
                    }
                    // eslint-disable-next-line no-param-reassign
                    config.headers.Authorization = `${this.jwtConfig.tokenType} ${this.getToken()}`
                }
                return config
            },
            (error) => Promise.reject(error)
        )

        // Response interceptor with proper error handling
        this.axiosIns.interceptors.response.use(
            (response) => response,
            async (error) => {
                const { config, response } = error
                const originalRequest = config

                // Handle network errors gracefully (no response object)
                if (!response) {
                    return Promise.reject({
                        message: 'Network error - please check your connection',
                        originalError: error,
                        isNetworkError: true,
                    })
                }

                // Handle 401 Unauthorized
                if (response.status === 401 && !originalRequest._retry) {
                    originalRequest._retry = true // Prevent infinite retry loops

                    // If not already refreshing, start the refresh process
                    if (!this.isAlreadyFetchingAccessToken) {
                        this.isAlreadyFetchingAccessToken = true

                        this.refreshTokenPromise = this.refreshToken()
                            .then((r) => {
                                // Update tokens in localStorage
                                this.setToken(r.data.access_token)
                                this.setRefreshToken(r.data.refresh_token)
                                this.setUserData(r.data)

                                // Broadcast to other tabs
                                this.broadcastTokenUpdate(r.data)

                                // Notify all waiting subscribers
                                this.onAccessTokenFetched(r.data.access_token)

                                return r.data.access_token
                            })
                            .catch((err) => {
                                // Clear all tokens on refresh failure
                                this.clearTokens()

                                // Notify subscribers of failure
                                this.onAccessTokenFetchFailed(err)

                                // Redirect to login
                                router.push('/login')

                                throw err
                            })
                            .finally(() => {
                                this.isAlreadyFetchingAccessToken = false
                                // Clear promise after a short delay to handle late requests
                                setTimeout(() => {
                                    this.refreshTokenPromise = null
                                }, 1000)
                            })
                    }

                    // All requests wait for the same refresh promise
                    try {
                        const accessToken = await this.refreshTokenPromise
                        originalRequest.headers.Authorization = `${this.jwtConfig.tokenType} ${accessToken}`
                        return this.axiosIns(originalRequest)
                    } catch (err) {
                        return Promise.reject(err)
                    }
                }

                // Return error data or the response itself
                return Promise.reject(response.data || response)
            }
        )
    }

    /**
     * Check if the access token is expiring soon (within buffer time)
     * @returns {boolean} True if token will expire within 2 minutes
     */
    isTokenExpiringSoon() {
        const userData = this.getUserData()
        // If no expires field, we don't know when it expires, so don't proactively refresh
        // We'll wait for a 401 instead to avoid infinite refresh loops
        if (!userData || !userData.expires) return false

        const expiryTime = new Date(userData.expires).getTime()

        // If invalid date format, don't refresh proactively
        if (isNaN(expiryTime)) return false

        const bufferMs = 2 * 60 * 1000 // 2 minutes buffer before expiry
        return Date.now() >= expiryTime - bufferMs
    }

    /**
     * Check if the access token is already expired
     * @returns {boolean} True if token is expired
     */
    isTokenExpired() {
        const userData = this.getUserData()
        if (!userData || !userData.expires) return false

        const expiryTime = new Date(userData.expires).getTime()
        if (isNaN(expiryTime)) return false

        return Date.now() >= expiryTime
    }


    /**
     * Refresh token and update localStorage
     * Uses shared promise pattern to prevent race conditions
     * @returns {Promise<string>} The new access token
     */
    async refreshTokenAndUpdate() {
        // If already refreshing, wait for that promise
        if (this.refreshTokenPromise) {
            return this.refreshTokenPromise
        }

        // Set flag BEFORE creating promise to prevent race condition
        this.isAlreadyFetchingAccessToken = true

        this.refreshTokenPromise = this.refreshToken()
            .then((r) => {
                this.setToken(r.data.access_token)
                this.setRefreshToken(r.data.refresh_token)
                this.setUserData(r.data)

                // Broadcast to other tabs
                this.broadcastTokenUpdate(r.data)

                return r.data.access_token
            })
            .finally(() => {
                this.isAlreadyFetchingAccessToken = false
                // Clear promise after a short delay to handle late requests
                setTimeout(() => {
                    this.refreshTokenPromise = null
                }, 1000)
            })

        return this.refreshTokenPromise
    }

    /**
     * Ensure token is refreshed, reusing existing refresh promise if available
     * This method prevents multiple parallel refresh attempts
     * @returns {Promise<string>} The access token
     */
    async ensureTokenRefreshed() {
        // If already refreshing, wait for that promise
        if (this.refreshTokenPromise) {
            return this.refreshTokenPromise
        }

        // If not refreshing and token is expiring, start refresh
        if (!this.isAlreadyFetchingAccessToken) {
            return this.refreshTokenAndUpdate()
        }

        // Flag is set but promise is not available yet - wait briefly for it
        return new Promise((resolve) => {
            const checkInterval = setInterval(() => {
                if (this.refreshTokenPromise) {
                    clearInterval(checkInterval)
                    resolve(this.refreshTokenPromise)
                }
            }, 10)

            // Timeout after 1 second and return current token
            setTimeout(() => {
                clearInterval(checkInterval)
                resolve(this.getToken())
            }, 1000)
        })
    }

    /**
     * Setup cross-tab synchronization for token refresh
     * Uses BroadcastChannel API to notify other tabs when token is refreshed
     */
    setupCrossTabSync() {
        // Only if BroadcastChannel is supported
        if (typeof BroadcastChannel !== 'undefined') {
            this.tokenChannel = new BroadcastChannel('jwt_token_channel')
            this.tokenChannel.onmessage = (event) => {
                if (event.data.type === 'TOKEN_REFRESH') {
                    // Another tab refreshed the token - update local storage
                    this.setToken(event.data.access_token)
                    this.setRefreshToken(event.data.refresh_token)
                    this.setUserData(event.data.userData)
                }
            }
        }
    }

    /**
     * Broadcast token update to other tabs
     * @param {Object} data Token response data from server
     */
    broadcastTokenUpdate(data) {
        if (this.tokenChannel) {
            this.tokenChannel.postMessage({
                type: 'TOKEN_REFRESH',
                access_token: data.access_token,
                refresh_token: data.refresh_token,
                userData: data,
            })
        }
    }

    /**
     * Notify all subscribers that a new access token is available
     * @param {string} accessToken The new access token
     */
    onAccessTokenFetched(accessToken) {
        // Execute all subscriber callbacks and clear the queue
        this.subscribers.forEach((callback) => callback(accessToken))
        this.subscribers = []
    }

    /**
     * Notify all subscribers that token refresh failed
     * @param {Error} error The error that occurred
     */
    onAccessTokenFetchFailed(error) {
        // Reject all pending subscribers
        this.subscribers.forEach((callback) => callback(null, error))
        this.subscribers = []
    }

    /**
     * Add a callback to be executed when token refresh completes
     * @param {Function} callback Function to call with new access token
     */
    addSubscriber(callback) {
        this.subscribers.push(callback)
    }

    /**
     * Clear all authentication tokens from localStorage
     */
    clearTokens() {
        localStorage.removeItem(this.jwtConfig.storageTokenKeyName)
        localStorage.removeItem(this.jwtConfig.storageRefreshTokenKeyName)
        localStorage.removeItem(this.jwtConfig.storageUserData)
    }

    getToken() {
        return localStorage.getItem(this.jwtConfig.storageTokenKeyName)
    }

    getUserData() {
        return JSON.parse(localStorage.getItem(this.jwtConfig.storageUserData))
    }

    getRefreshToken() {
        return localStorage.getItem(this.jwtConfig.storageRefreshTokenKeyName)
    }
    getNavbar() {
        return JSON.parse(localStorage.getItem(this.jwtConfig.navbarStorate))
    }
    setToken(value) {
        localStorage.setItem(this.jwtConfig.storageTokenKeyName, value)
    }

    setUserData(value) {
        if (!value) return

        // Get existing data to merge
        const existingData = this.getUserData() || {}

        // 1. Calculate expires field if server returns expires_in (seconds) but no fixed date
        if (value.expires_in && !value.expires) {
            const expiresDate = new Date()
            expiresDate.setSeconds(expiresDate.getSeconds() + value.expires_in)
            value.expires = expiresDate.toISOString()
        }

        // 2. Merge existing data with new value to prevent losing fields (like roles, id, etc.) during refresh
        const mergedData = { ...existingData, ...value }

        localStorage.setItem(
            this.jwtConfig.storageUserData,
            JSON.stringify(mergedData)
        )
    }

    setRefreshToken(value) {
        localStorage.setItem(this.jwtConfig.storageRefreshTokenKeyName, value)
    }
    setNavbar(value) {
        localStorage.setItem(
            this.jwtConfig.navbarStorate,
            JSON.stringify(value)
        )
    }
    login(...args) {
        /* eslint-disable */
        args[0].client_id = this.jwtConfig.clientId
        args[0].client_secret = this.jwtConfig.clientSecret
        args[0].grant_type = 'password'
        return this.axiosIns.post(this.jwtConfig.loginEndpoint, ...args)
    }

    get(url, params = {}) {
        url = url.replaceAll('=null', '=')
        url = url.replaceAll('=undefined', '=')
        const query = new URLSearchParams()
        if (Object.keys(params).length > 0) {
            Object.entries(params).forEach(([key, value]) => {
                query.set(key, value)
            })
        }
        return this.axiosIns.get(
            this.jwtConfig.serviceEndpoint +
                url +
                (query.toString() ? '?' + query.toString() : '')
        )
    }

    post(url, ...args) {
        return this.axiosIns.post(this.jwtConfig.serviceEndpoint + url, ...args)
    }

    put(url, ...args) {
        return this.axiosIns.put(this.jwtConfig.serviceEndpoint + url, ...args)
    }
    patch(url, ...args) {
        return this.axiosIns.patch(
            this.jwtConfig.serviceEndpoint + url,
            ...args
        )
    }

    delete(url) {
        return this.axiosIns.delete(this.jwtConfig.serviceEndpoint + url)
    }

    register(...args) {
        return this.axiosIns.post(this.jwtConfig.registerEndpoint, ...args)
    }

    refreshToken() {
        let data = {
            grant_type: 'refresh_token',
            refresh_token: this.getRefreshToken(),
            client_id: this.jwtConfig.clientId,
            client_secret: this.jwtConfig.clientSecret,
        }

        return this.axiosIns.post(this.jwtConfig.refreshEndpoint, data)
    }

    /**
     * Logout user - invalidate token on server and clear local storage
     * @returns {Promise} Promise that resolves when logout is complete
     */
    async logout() {
        const refreshToken = this.getRefreshToken()

        // Clear local tokens first (even if server call fails)
        this.clearTokens()

        // Invalidate token on server if we have one
        if (refreshToken) {
            try {
                await this.axiosIns.post(this.jwtConfig.refreshEndpoint, {
                    grant_type: 'invalidate_token',
                    refresh_token: refreshToken,
                    client_id: this.jwtConfig.clientId,
                    client_secret: this.jwtConfig.clientSecret,
                })
            } catch (e) {
                // Ignore errors - token is already cleared locally
                console.warn('Failed to invalidate token on server:', e)
            }
        }

        // Navigate to login
        router.push('/login')
    }
    upload(formdata) {
        return this.axiosIns.post(
            this.jwtConfig.serviceEndpoint + '/file',
            formdata,
            {
                headers: {
                    'Content-Type': 'multipart/form-data',
                },
            }
        )
    }
}
