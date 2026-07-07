import useJwt from '@/auth/jwt/useJwt'

/**
 * Return if user is logged in
 * Checks for valid tokens in localStorage
 * - If refresh token exists, user can potentially be authenticated (access token can be refreshed)
 * - If no refresh token, user must login again
 */
export const isUserLoggedIn = () => {
    const userData = localStorage.getItem(useJwt.jwtConfig.storageUserData)
    const accessToken = localStorage.getItem(useJwt.jwtConfig.storageTokenKeyName)
    const refreshToken = localStorage.getItem(useJwt.jwtConfig.storageRefreshTokenKeyName)

    // Must have userData and at least one token
    if (!userData || !accessToken) {
        return false
    }

    try {
        const parsed = JSON.parse(userData)

        // If refresh token exists, user can stay logged in
        // (access token can be refreshed even if expired)
        if (refreshToken) {
            return true
        }

        // No refresh token - check if access token is still valid
        if (parsed.expires) {
            const expiryTime = new Date(parsed.expires).getTime()
            return Date.now() < expiryTime
        }

        // No expiry info but has userData and token
        return true
    } catch {
        return false
    }
}

/**
 * Check if the current access token is expired
 * @returns {boolean} True if expired or no token exists
 */
export const isAccessTokenExpired = () => {
    const userData = localStorage.getItem(useJwt.jwtConfig.storageUserData)
    if (!userData) return true

    try {
        const parsed = JSON.parse(userData)
        if (!parsed.expires) return true

        const expiryTime = new Date(parsed.expires).getTime()
        return Date.now() >= expiryTime
    } catch {
        return true
    }
}

/**
 * Check if refresh token exists
 * @returns {boolean} True if refresh token is available
 */
export const hasRefreshToken = () => {
    return !!localStorage.getItem(useJwt.jwtConfig.storageRefreshTokenKeyName)
}

export const getUserData = () =>
    JSON.parse(localStorage.getItem(useJwt.jwtConfig.storageUserData))

/**
 * This function is used for demo purpose route navigation
 * In real app you won't need this function because your app will navigate to same route for each users regardless of ability
 * Please note role field is just for showing purpose it's not used by anything in frontend
 * We are checking role just for ease
 * NOTE: If you have different pages to navigate based on user ability then this function can be useful. However, you need to update it.
 * @param {String} userRole Role of user
 */
export const getHomeRouteForLoggedInUser = (userRole) => {
    if (userRole === 'admin') return '/'
    if (userRole === 'client') return { name: 'access-control' }
    return { name: 'auth-login' }
}
