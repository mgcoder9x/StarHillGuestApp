import { $themeConfig } from '@themeConfig'

const isDevEnv = process.env.NODE_ENV === 'development'
const { apiURL, apiURLDev } = $themeConfig.app

function getBaseUrl() {
    const baseURL = isDevEnv ? apiURLDev : apiURL
    return baseURL
}

export default getBaseUrl
