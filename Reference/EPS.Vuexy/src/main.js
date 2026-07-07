/* eslint-disable */
import Vue from 'vue'
import VueCompositionAPI from '@vue/composition-api'
// Must be called before any Vue.component() or Vue.use() calls to avoid $attrs readonly warning
Vue.use(VueCompositionAPI)
import { ToastPlugin, ModalPlugin } from 'bootstrap-vue'
import useJwt from '@/auth/jwt/useJwt'
import BasicTable from '@/components/BasicTable.vue'
import BasicTable2 from '@/components/BasicTable2.vue'
import EventCarousel from '@/components/EventCarousel.vue'
import JsonExcel from '@/components/JsonExcelCustom.vue'
import ImageUpload from '@/components/ImageUpload.vue'
import FormSearch from '@/components/FormSearch.vue'
import flvPlayer from '@/components/FlvPlayer'
import BootstrapVue from 'bootstrap-vue'
import Swiper from '@/components/MediaSwiper.vue'
import TelerikReportViewer from '@/components/TelerikReportViewer.vue'
// Import Bootstrap and BootstrapVue CSS files (order is important)
import 'bootstrap/dist/css/bootstrap.css'
import 'bootstrap-vue/dist/bootstrap-vue.css'
// Import Telerik
import '@/assets/telerikReport/js/telerikReportViewer.min.js'
import '@/assets/telerikReport/css/default-main.min.css'

import {
    ValidationProvider,
    ValidationObserver,
    localize,
    extend,
} from 'vee-validate'
import VueKonva from 'vue-konva'

import {
    required,
    email,
    confirmed,
    url,
    between,
    alpha,
    integer,
    password,
    min,
    digits,
    alphaDash,
    length,
} from '@validations'
// import waves
import Waves from 'vue-waves-effect'
import 'vue-waves-effect/dist/vueWavesEffect.css'

import i18n from '@/libs/i18n'
import router from './router'
import store from './store'
import App from './App.vue'

// Import Iconify via wrapper that prevents $attrs/$listeners readonly warnings
// caused by @vue/composition-api reactive proxies being mutated inside Icon's render()
import IconWrapper from '@/components/IconWrapper.vue'
import loadIconSets from '@/libs/iconify-offline'

// Configure Iconify API endpoint
loadIconSets()

// Global Components
import './global-components'

// 3rd party plugins
import '@axios'
import '@/libs/acl'
import '@/libs/portal-vue'
import '@/libs/clipboard'
import '@/libs/toastification'
import '@/libs/sweet-alerts'
import treeSelect from '@riophae/vue-treeselect'
import vSelect from 'vue-select'
import '@core/scss/vue/libs/vue-select.scss'
import 'vue-ads-table-tree/dist/vue-ads-table-tree.css'
import 'vue-ads-pagination/dist/vue-ads-pagination.css'
import '@/libs/tour'
import '@riophae/vue-treeselect/dist/vue-treeselect.css'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import { faBell, faUser, faCheck, faTimes, faExclamationTriangle, faInfo } from '@fortawesome/free-solid-svg-icons'
import { library } from '@fortawesome/fontawesome-svg-core'
import { Sketch } from 'vue-color'
import DatePicker from '@/components/DatePicker.vue'
import CreateIssue from '@/components/CreateIssue.vue'

// Only load fake mock data in development
if (process.env.NODE_ENV !== 'production') require('@/@fake-db/db')

library.add(faBell, faUser, faCheck, faTimes, faExclamationTriangle, faInfo)
// BSV Plugin Registration
Vue.component('font-awesome-icon', FontAwesomeIcon)
Vue.use(ToastPlugin)
Vue.use(ModalPlugin)

Vue.use(BootstrapVue)
Vue.component('Icon', IconWrapper)
Vue.component('SketchPicker', Sketch)
Vue.component('BasicTable', BasicTable)
Vue.component('BasicTable2', BasicTable2)
Vue.component('EventCarousel', EventCarousel)
Vue.component('FormSearch', FormSearch)
Vue.component('ImageUpload', ImageUpload)
Vue.component('FlvPlayer', flvPlayer)
Vue.component('media-swiper', Swiper)
Vue.component('datePicker', DatePicker)
Vue.component('telerik-report-viewer', TelerikReportViewer)
Vue.component('create-issue', CreateIssue)
// Register konva
Vue.use(VueKonva)

// Register vue-select
Vue.component('v-select', vSelect)
Vue.component('tree-select', treeSelect)
treeSelect.mixins[0].props.noOptionsText.default = 'Sorry, no matching options.'
// Register waves
Vue.use(Waves)
// Register vee-validate
Vue.component('ValidationProvider', ValidationProvider)
Vue.component('ValidationObserver', ValidationObserver)
Vue.component('extend', extend)
Vue.component('downloadExcel', JsonExcel)
// Services Regis
Vue.prototype.$services = useJwt
// Composition API already initialised at top of file
Vue.use(require('vue-moment'))
// Feather font icon - For form-wizard
require('@core/assets/fonts/feather/iconfont.css') // For form-wizard

// import core styles
require('@core/scss/core.scss')
// import assets styles
require('@/assets/scss/main.scss')

Vue.config.productionTip = false

localize(i18n.locale)
;(async () => {
    try {
        const response = await fetch(
            `${process.env.BASE_URL || '/'}config.json?_id=${new Date().getTime()}`
        )
        if (response.ok) {
            const contentType = response.headers.get('content-type') || ''
            const rawText = await response.text()

            if (
                contentType.includes('application/json') ||
                rawText.trim().startsWith('{')
            ) {
                const config = JSON.parse(rawText)

                const dynamicRoutes = (config.routes || []).map((route) => ({
                    path: route.path,
                    name: route.name,
                    component: () =>
                        import(
                            '@/views/dashboard/system-info/IFrameSystemInfo.vue'
                        ),
                    props: { iframeUrl: route.url },
                    meta: {
                        pageTitle: 'route.breadcrumb.dashboardSystem',
                        breadcrumb: [
                            {
                                text: 'route.common.dashboard',
                                active: true,
                            },
                        ],
                    },
                }))
                router.addRoutes(dynamicRoutes)
            } else {
                console.warn('config.json is not valid JSON content, skipping dynamic routes')
            }
        }
    } catch (err) {
        console.log(err)
    }
    new Vue({
        router,
        store,
        i18n,
        render: (h) => h(App),
    }).$mount('#app')
})()