import eventWarningLevelService from '@/services/eventWarningLevel.service'

export default {
  namespaced: true,

  state: {
    items: [],
    total: 0,
    detail: null,
    loading: false,
    saving: false,
  },

  mutations: {
    SET_ITEMS(state, { data, total }) { state.items = data; state.total = total },
    SET_DETAIL(state, item) { state.detail = item },
    SET_LOADING(state, val) { state.loading = val },
    SET_SAVING(state, val) { state.saving = val },
  },

  actions: {
    async loadItems({ commit }, params = {}) {
      commit('SET_LOADING', true)
      try {
        const res = await eventWarningLevelService.getAll(params)
        const result = res.data?.data || {}
        commit('SET_ITEMS', { data: result.data || [], total: result.totalRows || 0 })
      } finally {
        commit('SET_LOADING', false)
      }
    },

    async loadDetail({ commit }, id) {
      commit('SET_LOADING', true)
      try {
        const res = await eventWarningLevelService.getById(id)
        commit('SET_DETAIL', res.data?.data || null)
      } finally {
        commit('SET_LOADING', false)
      }
    },

    async createItem({ commit }, payload) {
      commit('SET_SAVING', true)
      try { return await eventWarningLevelService.create(payload) }
      finally { commit('SET_SAVING', false) }
    },

    async updateItem({ commit }, { id, payload }) {
      commit('SET_SAVING', true)
      try { return await eventWarningLevelService.update(id, payload) }
      finally { commit('SET_SAVING', false) }
    },

    async deleteItem(_, id) {
      return eventWarningLevelService.remove(id)
    },
  },

  getters: {
    items: (s) => s.items,
    total: (s) => s.total,
    detail: (s) => s.detail,
    isLoading: (s) => s.loading,
    isSaving: (s) => s.saving,
  },
}
