import { $themeBreakpoints } from '@themeConfig'

export default {
    namespaced: true,
    state: {
        windowWidth: 0,
        shallShowOverlay: false,
        currentUser: null,
    },
    getters: {
        currentBreakPoint: (state) => {
            const { windowWidth } = state
            if (windowWidth >= $themeBreakpoints.xl) return 'xl'
            if (windowWidth >= $themeBreakpoints.lg) return 'lg'
            if (windowWidth >= $themeBreakpoints.md) return 'md'
            if (windowWidth >= $themeBreakpoints.sm) return 'sm'
            return 'xs'
        },
        getCurrentUser: (state) => {
            return state.currentUser
        }, // Retrieves the current user
        isAuthenticated: (state) => !!state.currentUser,
    },
    mutations: {
        UPDATE_WINDOW_WIDTH(state, val) {
            state.windowWidth = val
        },
        TOGGLE_OVERLAY(state, val) {
            state.shallShowOverlay =
                val !== undefined ? val : !state.shallShowOverlay
        },
        setCurrentUser(state, userData) {
            state.currentUser = userData
        },
        clearCurrentUser(state) {
            state.currentUser = null
        },
    },
    actions: {
        saveCurrentUser({ commit }, userData) {
            commit('setCurrentUser', userData)
        },
        logout({ commit }) {
            commit('clearCurrentUser') // Clears user data on logout
        },
    },
}
