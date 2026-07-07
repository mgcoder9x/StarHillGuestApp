<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
                        <!-- Company -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('System.User.Detail.Label.Company')"
                                label-for="h-compId"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="Company"
                                >
                                    <treeselect
                                        v-model="user.companyId"
                                        :multiple="false"
                                        :options="options"
                                        :normalizer="normalizer"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Username -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('System.User.Detail.Label.Name')"
                                label-for="h-user-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|username"
                                    name="UserName"
                                >
                                    <b-form-input
                                        id="h-user-name"
                                        v-model.trim="user.username"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        autocomplete="off"
                                        spellcheck="false"
                                        inputmode="latin"
                                        @keydown.space.prevent
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Full name -->
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                name="FullName"
                            >
                                <b-form-group
                                    :label="
                                        $t('System.User.Detail.Label.FullName')
                                    "
                                    label-for="h-full-name"
                                    label-cols-md="4"
                                    label-class="required"
                                >
                                    <b-form-input
                                        id="h-full-name"
                                        v-model="user.fullName"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>

                        <!-- Password -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('System.User.Detail.Label.Password')"
                                label-for="h-password"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    name="Password"
                                    vid="Password"
                                    rules="required|min:8|password"
                                >
                                    <b-form-input
                                        id="h-password"
                                        v-model="user.password"
                                        type="password"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Confirm password -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t('System.User.Detail.Label.RePassword')
                                "
                                label-for="h-password-confirmation"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    name="ConfirmPassword"
                                    rules="required|confirmed:Password"
                                >
                                    <b-form-input
                                        id="h-password-confirmation"
                                        v-model="user.passwordConfirmation"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        type="password"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Phone -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t('System.User.Detail.Label.PhoneNumber')
                                "
                                label-for="h-phone-number"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    name="phoneNumber"
                                    rules="phone"
                                >
                                    <b-form-input
                                        id="h-phone-number"
                                        v-model="user.phoneNumber"
                                    />
                                    <small class="text-danger">{{
                                        $t(errors[0])
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Email -->
                        <b-col md="6">
                            <b-form-group
                                label="Email"
                                label-for="h-email"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    name="Email"
                                    vid="Email"
                                    rules="email"
                                >
                                    <b-form-input
                                        id="h-email"
                                        v-model="user.email"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Roles -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('System.User.Detail.Label.Role')"
                                label-for="h-roles"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="Role"
                                >
                                    <v-select
                                        v-model="user.roleIds"
                                        :reduce="(item) => item.id"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        multiple
                                        label="text"
                                        :options="lstRoles"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Zalo Id -->
                        <b-col md="6">
                            <b-form-group
                                label="Zalo Id"
                                label-for="h-zaloId"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-zaloId"
                                    v-model="user.zaloId"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <!-- Buttons -->
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-if="authorize(['ManageUser'])"
                                type="submit"
                                variant="primary"
                                class="mr-1"
                                @click.prevent="validationForm"
                            >
                                {{ $t('Button.Save') }}
                            </b-button>
                            <b-button
                                :to="{ path: '/systems/users/list' }"
                                type="reset"
                                variant="outline-secondary"
                            >
                                {{ $t('Button.Cancel') }}
                            </b-button>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import Treeselect from '@riophae/vue-treeselect'
import '@riophae/vue-treeselect/dist/vue-treeselect.css'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import vSelect from 'vue-select'
import { authorizationMixin } from '@core/mixins/ui/forms'
import { extend } from 'vee-validate'
import i18n from '@/libs/i18n'

// phone rule
extend('phone', {
    validate: (value) => /^\d{10}$/.test(value),
    message: 'System.User.Valid.PhoneNumber',
})

// username rule (3 yêu cầu)
extend('username', {
    validate(value) {
        const v = (value || '').trim()

        // 1) Không được có khoảng trắng
        if (/\s/.test(v)) return { valid: false, data: { reason: 'space' } }

        // 2) Không dấu tiếng Việt (ngoài ASCII)
        if (/[^\x00-\x7F]/.test(v))
            return { valid: false, data: { reason: 'accent' } }

        // 3) Chỉ cho phép chữ cái, số và _
        if (!/^[A-Za-z0-9_]+$/.test(v))
            return { valid: false, data: { reason: 'special' } }

        return { valid: true }
    },
    message: (_, values) => {
        const map = {
            space: 'System.User.Valid.Username.Space',
            accent: 'System.User.Valid.Username.Accent',
            special: 'System.User.Valid.Username.Special',
            default: 'System.User.Valid.Username.Invalid',
        }
        const key = map[values?.data?.reason] || map.default
        return i18n.t(key) // trả về chuỗi đã dịch
    },
})

export default {
    name: 'UserCreate',
    mixins: [authorizationMixin],
    components: {
        Treeselect,
        vSelect,
        ToastificationContent,
    },
    data() {
        return {
            user: {
                status: 1,
                username: null,
                password: null,
                passwordConfirmation: null,
                companyId: null,
                fullName: null,
                email: null,
                phoneNumber: null,
                roleIds: [],
                roleId: null,
                zaloId: null,
            },
            lstRoles: [],
            options: [],
        }
    },
    created() {
        this.loadCompanyTree()
        this.loadRoles()
    },
    methods: {
        //Danh sách công ty - tree view
        loadCompanyTree() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.options = response.data
            })
        },
        loadRoles() {
            this.$services.get('/lookup/roles').then((response) => {
                this.lstRoles = response.data
            })
        },
        normalizer(node) {
            if (node.children == null || node.children == 'null') {
                delete node.children
            }
        },
        validationForm() {
            // Chuẩn hoá trước khi validate/submit
            this.user.username = (this.user.username || '').trim()

            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .post('/users/', this.user)
                        .then(() => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Create'),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                            this.$router.push({ path: '/systems/users/list' })
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: this.$t(`${error.message}`),
                                },
                            })
                        })
                }
            })
        },
    },
}
</script>

<style lang="scss"></style>
