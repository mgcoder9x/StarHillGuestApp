import Vue from 'vue'
import VueI18n from 'vue-i18n'
import { merge } from 'lodash'

Vue.use(VueI18n)

// Get project from environment variable
const PROJECT = process.env.VUE_APP_PROJECT || 'default'
const loadedFiles = new Set()

/**
 * Deep merge objects, project-specific translations override default ones
 */
function mergeTranslations(defaultTranslations, projectTranslations) {
    return merge({}, defaultTranslations, projectTranslations)
}

/**
 * Load default locale messages from the main locales folder
 */
function loadDefaultLocaleMessages() {
    const locales = require.context(
        './locales',
        false,
        /[A-Za-z0-9-_,\s]+\.json$/i
    )
    const messages = {}
    locales.keys().forEach((key) => {
        const matched = key.match(/([A-Za-z0-9-_]+)\./i)
        if (matched && matched.length > 1) {
            const locale = matched[1]
            messages[locale] = locales(key)
        }
    })
    return messages
}

/**
 * Load module-based locale messages from locales/modules/
 * File naming convention: {module}.{locale}.json  (e.g. common.vi.json, categories.en.json)
 */
function loadModuleLocaleMessages() {
    try {
        const modules = require.context(
            './locales/modules',
            false,
            /[A-Za-z0-9-_,\s]+\.json$/i
        )
        const messages = {}
        modules.keys().forEach((key) => {
            // Match pattern: ./module-name.locale.json
            const matched = key.match(/\.\/[A-Za-z0-9-_]+\.([A-Za-z0-9-_]+)\.json$/i)
            if (matched && matched.length > 1) {
                const locale = matched[1]
                if (!messages[locale]) {
                    messages[locale] = {}
                }
                messages[locale] = merge(messages[locale], modules(key))
            }
        })
        return messages
    } catch (error) {
        console.warn('No module-based translations found in locales/modules/')
        return {}
    }
}

/**
 * Load project-specific locale messages
 */
function loadProjectLocaleMessages() {
    if (!PROJECT || PROJECT === 'default') {
        return {}
    }

    try {
        const locales = require.context(
            './locales/project',
            true,
            /[A-Za-z0-9-_,\s]+\.json$/i
        )
        const messages = {}
        locales.keys().forEach((key) => {
            const pathParts = key.split('/')
            if (pathParts.length >= 3 && pathParts[1] === PROJECT) {
                const matched = pathParts[2].match(/([A-Za-z0-9-_]+)\./i)
                if (matched && matched.length > 1) {
                    const locale = matched[1]
                    if (!messages[locale]) {
                        messages[locale] = {}
                    }
                    messages[locale] = merge(messages[locale], locales(key))
                }
            }
        })
        return messages
    } catch (error) {
        console.warn(
            `No project-specific translations found for project: ${PROJECT}`
        )
        return {}
    }
}

/**
 * Merge default, module, and project-specific messages
 */
function loadLocaleMessages() {
    const defaultMessages = loadDefaultLocaleMessages()
    const moduleMessages = loadModuleLocaleMessages()
    const projectMessages = loadProjectLocaleMessages()

    const messages = {}

    // Start with default messages
    Object.keys(defaultMessages).forEach((locale) => {
        messages[locale] = defaultMessages[locale]
    })

    // Merge module-based messages (override defaults with module specifics)
    Object.keys(moduleMessages).forEach((locale) => {
        if (messages[locale]) {
            messages[locale] = mergeTranslations(messages[locale], moduleMessages[locale])
        } else {
            messages[locale] = moduleMessages[locale]
        }
    })

    // Merge project-specific messages (highest priority)
    Object.keys(projectMessages).forEach((locale) => {
        if (messages[locale]) {
            messages[locale] = mergeTranslations(
                messages[locale],
                projectMessages[locale]
            )
        } else {
            messages[locale] = projectMessages[locale]
        }
    })

    return messages
}

const i18n = new VueI18n({
    locale: 'vi',
    fallbackLocale: 'vi',
    messages: loadLocaleMessages(),
})

export async function loadRouteLocaleMessage(locale, routePath) {
    const fileName =
        routePath.replace(/^\/+/, '').replace(/\//g, '-') || 'default'
    const fileKey = `${locale}/${fileName}`

    if (loadedFiles.has(fileKey)) return

    try {
        // Load default route-specific messages
        const messages = await import(
            `@/libs/i18n/locales/${locale}/${fileName}.json`
        )
        let mergedMessages = messages.default

        // Try to load project-specific route messages
        if (PROJECT && PROJECT !== 'default') {
            try {
                const projectMessages = await import(
                    `@/libs/i18n/locales/project/${PROJECT}/${locale}/${fileName}.json`
                )
                mergedMessages = mergeTranslations(
                    mergedMessages,
                    projectMessages.default
                )
            } catch (projectErr) {
                console.warn(
                    `No project-specific route translations found for ${PROJECT}/${locale}/${fileName}.json`
                )
            }
        }

        i18n.mergeLocaleMessage(locale, mergedMessages)
        loadedFiles.add(fileKey)
    } catch (err) {
        console.warn(
            `Không tìm thấy file ${fileName}.json trong ${locale}, sử dụng bản dịch mặc định`
        )
        try {
            // Fallback to default file
            const defaultMessages = await import(
                `@/libs/i18n/locales/${locale}.json`
            )
            let mergedMessages = defaultMessages.default

            // Try to merge with project-specific default messages
            if (PROJECT && PROJECT !== 'default') {
                try {
                    const projectDefaultMessages = await import(
                        `@/libs/i18n/locales/project/${PROJECT}/${locale}.json`
                    )
                    mergedMessages = mergeTranslations(
                        mergedMessages,
                        projectDefaultMessages.default
                    )
                } catch (projectErr) {
                    console.warn(
                        `No project-specific default translations found for ${PROJECT}/${locale}.json`
                    )
                }
            }

            i18n.mergeLocaleMessage(locale, mergedMessages)
            loadedFiles.add(`${locale}/${locale}`)
        } catch (fallbackErr) {
            console.error(
                `Không tìm thấy file mặc định ${locale}.json`,
                fallbackErr
            )
        }
    }
}

export function getCurrentLocale() {
    return i18n.locale
}

export function getCurrentProject() {
    return PROJECT
}

export default i18n
