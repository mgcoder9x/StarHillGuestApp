import Vue from 'vue'
import Vuex from 'vuex'

// Modules
import ecommerceStoreModule from '@/views/apps/e-commerce/eCommerceStoreModule'
import verticalMenu from './vertical-menu'
import appConfig from './app-config'
import app from './app'
import dashboard from './modules/dashboard'
import eventType from './modules/eventType'
import eventWarningLevel from './modules/eventWarningLevel'

Vue.use(Vuex)

export default new Vuex.Store({
    modules: {
        app,
        appConfig,
        verticalMenu,
        dashboard,
        eventType,
        eventWarningLevel,
        'app-ecommerce': ecommerceStoreModule,
    },
    strict: process.env.DEV,
})

