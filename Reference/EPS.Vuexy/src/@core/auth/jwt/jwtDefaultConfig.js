// eslint-disable-next-line
const { VUE_APP_BASE_URL: apiURL } = process.env
const serviceEndpoint = `${apiURL}/api`
export default {
    // Endpoints
    /* eslint-disable */
    loginEndpoint: serviceEndpoint + '/token/auth',
    registerEndpoint: '/jwt/register',
    refreshEndpoint: serviceEndpoint + '/token/auth',
    logoutEndpoint: '/jwt/logout',
    serviceEndpoint: serviceEndpoint,

    // Client info
    clientId: 'IAC_Cloud',
    clientSecret: '1a82f1d60ba6353bb64a8fb4b05e4bc4',

    // This will be prefixed in authorization header with token
    // e.g. Authorization: Bearer <token>
    tokenType: 'Bearer',

    // Value of this property will be used as key to store JWT token in storage
    storageTokenKeyName: 'accessToken',
    storageRefreshTokenKeyName: 'refreshToken',
    storageUserData: 'userData',
    navbarStorate: 'navbar',
}
