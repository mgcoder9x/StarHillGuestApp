<template>
    <div class="auth-wrapper auth-v2">
        <b-row class="auth-inner m-0">
            <!-- Brand logo-->
            <b-link class="brand-logo">
                <b-img :src="logo" :alt="appName" fluid />
                <h2 class="brand-text text-primary ml-1">{{ appName }}</h2>
            </b-link>
            <!-- /Brand logo-->

            <!-- Left Text-->
            <b-col lg="8" class="d-none d-lg-flex align-items-center p-5">
                <div
                    class="w-100 d-lg-flex align-items-center justify-content-center px-5"
                >
                    <b-img fluid :src="imgUrl" alt="Login V2" />
                </div>
            </b-col>
            <!-- /Left Text-->

            <!-- Login-->
            <b-col lg="4" class="d-flex align-items-center auth-bg px-2 p-lg-5">
                <b-col sm="8" md="6" lg="12" class="px-xl-2 mx-auto">
                    <b-card-title class="mb-1 font-weight-bold" title-tag="h2">
                        Welcome! 👋
                    </b-card-title>
                    <b-card-text class="mb-2">
                        Please sign-in to your account and start the adventure
                    </b-card-text>

                    <!-- form -->
                    <validation-observer ref="loginForm" #default="{ invalid }">
                        <b-form
                            class="auth-login-form mt-2"
                            @submit.prevent="login"
                        >
                            <!-- username -->
                            <b-form-group
                                label="UserName"
                                label-for="login-email"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    name="UserName"
                                    vid="email"
                                    rules="required"
                                >
                                    <b-form-input
                                        id="login-email"
                                        v-model.trim="userEmail"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        name="login-email"
                                        type="text"
                                        placeholder="UserName"
                                        autocomplete="username"
                                        spellcheck="false"
                                        inputmode="latin"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>

                            <!-- password -->
                            <b-form-group>
                                <div class="d-flex justify-content-between">
                                    <label for="login-password">Password</label>
                                </div>
                                <validation-provider
                                    #default="{ errors }"
                                    name="Password"
                                    vid="password"
                                    rules="required"
                                >
                                    <b-input-group
                                        class="input-group-merge"
                                        :class="
                                            errors.length > 0
                                                ? 'is-invalid'
                                                : null
                                        "
                                    >
                                        <b-form-input
                                            id="login-password"
                                            v-model="password"
                                            :state="
                                                errors.length > 0 ? false : null
                                            "
                                            class="form-control-merge"
                                            :type="passwordFieldType"
                                            name="login-password"
                                            placeholder="Password"
                                            autocomplete="current-password"
                                        />
                                        <b-input-group-append is-text>
                                            <feather-icon
                                                class="cursor-pointer"
                                                :icon="passwordToggleIcon"
                                                @click="
                                                    togglePasswordVisibility
                                                "
                                            />
                                        </b-input-group-append>
                                    </b-input-group>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>

                            <!-- submit buttons -->
                            <b-button
                                type="submit"
                                variant="primary"
                                block
                                :disabled="invalid"
                            >
                                Sign in
                            </b-button>
                        </b-form>
                    </validation-observer>
                </b-col>
            </b-col>
            <!-- /Login-->
        </b-row>
    </div>
</template>

<script>
/* eslint-disable global-require */
import { ValidationProvider, ValidationObserver } from 'vee-validate'
// import VuexyLogo from '@core/layouts/components/Logo.vue'
import { VBTooltip } from 'bootstrap-vue'
import { required, email } from '@validations'
import { togglePasswordVisibility } from '@core/mixins/ui/forms'
import store from '@/store/index'
import axios from 'axios'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { $themeConfig } from '@themeConfig'

const LS_ACCESS = 'access_token'
const LS_REFRESH = 'refresh_token'
const LS_USER = 'user_data'
const NAV_KEYS_TO_CLEAR = ['navbar', 'navbar:current'] // xoá nếu service dùng các key này
const AFTER_LOGIN_ROUTE = process.env.VUE_APP_AFTER_LOGIN_ROUTE // đích sau login

export default {
    directives: { 'b-tooltip': VBTooltip },
    components: {
        // VuexyLogo,
        ValidationProvider,
        ValidationObserver,
    },
    setup() {
        const { appName } = $themeConfig.app
        return { appName }
    },
    mixins: [togglePasswordVisibility],
    data() {
        return {
            status: '',
            password: '',
            userEmail: '',
            sideImg: require('@/assets/images/pages/login-v2.svg'),

            // validation rules
            required,
            email,
        }
    },
    computed: {
        passwordToggleIcon() {
            return this.passwordFieldType === 'password'
                ? 'EyeIcon'
                : 'EyeOffIcon'
        },
        imgUrl() {
            if (store.state.appConfig.layout.skin === 'dark') {
                // eslint-disable-next-line vue/no-side-effects-in-computed-properties
                this.sideImg = require('@/assets/images/pages/login-v2-dark.svg')
                return this.sideImg
            }
            return this.sideImg
        },
        logo() {
            return require('@/assets/images/logo/logo.png')
        },
    },

    methods: {
        // Dọn phiên cũ (chỉ client; không đụng service)
        clearAuthCacheLocal() {
            try {
                localStorage.removeItem(LS_ACCESS)
                localStorage.removeItem(LS_REFRESH)
                localStorage.removeItem(LS_USER)
                NAV_KEYS_TO_CLEAR.forEach((k) => localStorage.removeItem(k))
                delete axios.defaults.headers.common.Authorization

                // reset một số vuex nếu có (bọc try để không crash)
                try {
                    store.commit('app/RESET_STATE', null, { root: true })
                } catch {}
                try {
                    store.commit('acl/RESET', null, { root: true })
                } catch {}
                try {
                    store.commit('user/RESET', null, { root: true })
                } catch {}
            } catch {}
        },

        // Nuốt lỗi redirect/duplicate nếu bạn vẫn muốn dùng router.replace ở nơi khác
        swallowNavError(err) {
            const msg = err && err.message ? err.message : ''
            if (
                err?.name === 'NavigationDuplicated' ||
                msg.includes('Avoided redundant navigation') ||
                msg.includes('Redirected when going')
            ) {
                return
            }
            throw err
        },

        async login() {
            const ok = await this.$refs.loginForm.validate()
            if (!ok) return

            // 1) Dọn cache phiên cũ (đảm bảo không dính quyền user trước)
            this.clearAuthCacheLocal()

            // 2) Gọi API login (giữ nguyên service)
            this.$services
                .login({
                    username: (this.userEmail || '').trim(),
                    password: this.password,
                })
                .then(async (response) => {
                    // 3) Lưu session mới (giữ nguyên cách của bạn)
                    this.$services.setToken(response.data.access_token)
                    this.$services.setRefreshToken(response.data.refresh_token)
                    this.$services.setUserData(response.data)

                    // 4) Tải menu/quyền TRƯỚC khi điều hướng & chống cache file config
                    const nav = await this.fetchDynamicNav()
                    this.$services.setNavbar(nav)

                    // 5) HARD RELOAD sang route đích để reset toàn bộ SPA state/guards
                    //    => không cần F5 tay, luôn nhận đúng quyền user mới
                    console.log(process.env)
                    window.location.replace(AFTER_LOGIN_ROUTE)

                    // (Nếu muốn giữ toast, cần cơ chế hiển thị sau reload ở layout; ở đây bỏ qua để đơn giản)
                })
                .catch((error) => {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: 'Error',
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                            text: `${error}`,
                        },
                    })
                })
        },

        async fetchDynamicNav() {
            try {
                // no-store để trình duyệt không cache config.json cũ
                const response = await fetch('/config.json', {
                    cache: 'no-store',
                })
                if (!response.ok) throw new Error('Failed to fetch config')

                const config = await response.json()
                return this.getDynamicRoutes(config.routes)
            } catch (error) {
                console.error('Error fetching dynamic navigation:', error)
                return []
            }
        },
        getDynamicRoutes(routes) {
            return routes.map((route) => ({
                title: route.title,
                icon: route.icon,
                requiresPrivileges: route.requiresPrivileges,
                route: route.name,
            }))
        },
    },
}
</script>

<style lang="scss">
@import '@core/scss/vue/pages/page-auth.scss';
</style>
