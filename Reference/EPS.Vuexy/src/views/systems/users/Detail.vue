<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
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
                                        :disabled="!editing"
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
                        <b-col md="6">
                            <b-form-group
                                :label="$t('System.User.Detail.Label.Name')"
                                label-for="h-user-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="UserName"
                                >
                                    <b-form-input
                                        id="h-user-name"
                                        v-model="user.username"
                                        readonly
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
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                name="FullName"
                            >
                                <b-form-group
                                    :label="$t('System.User.Detail.Label.FullName')"
                                    label-for="h-full-name"
                                    label-cols-md="4"
                                    label-class="required"
                                >
                                    <b-form-input
                                        id="h-full-name"
                                        v-model="user.fullName"
                                        :disabled="!editing"
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
                        <b-col md="6">
                            <b-form-group
                                :label="$t('System.User.Detail.Label.PhoneNumber')"
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
                                            :disabled="!editing"
                                        />
                                    <small class="text-danger">{{
                                        $t(errors[0])
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
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
                                    :disabled="!editing"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                                
                            </b-form-group>
                        </b-col>
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
                                        :reduce="(option) => option.id"
                                        :disabled="!editing"
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
                        <b-col md="6">
                            <b-form-group
                                label="Zalo Id"
                                label-for="h-zaloId"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-zaloId"
                                    v-model="user.zaloId"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-if="editing && authorize(['ManageUser'])"
                                type="button"
                                variant="primary"
                                class="mx-50 mb-50 btn-120"
                                @click="validationForm"
                            >
                                {{ $t('Button.Save') }}
                            </b-button>
                            <b-button
                                v-if="!editing && authorize(['ManageUser'])"
                                type="button"
                                variant="primary"
                                class="mx-50 mb-50 btn-120"
                                @click="edit"
                                >{{ $t('Button.Edit') }}</b-button
                            >
                            <b-button
                                v-if="!editing"
                                :to="{ path: '/systems/users/list' }"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                            >
                                {{ $t('Button.Back') }}
                            </b-button>
                            <b-button
                                v-if="editing"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                                @click="cancel"
                                >{{ $t('Button.Cancel') }}</b-button
                            >
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
extend('phone', {
  validate: value => /^\d{10}$/.test(value),
  message: 'System.User.Valid.PhoneNumber'
})


export default {
    mixins: [authorizationMixin],
    components: {
        Treeselect,
        vSelect,
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
            editing: false,
        }
    },
    computed: {
        userId() {
            return this.$route.params.userId
        },
    },
    created() {
        this.loadCompanyTree()
        this.loadRoles()
        this.loadUserDetail()
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
        loadUserDetail() {
            this.$services.get(`/users/${this.userId}`).then((response) => {
                this.user = response.data
                this.user.roleIds = response.data.roleIds.map(String)
            })
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .put(`/users/${this.userId}`, this.user)
                        .then((response) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Update'),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                            this.cancel()
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${error.response.data.error}`,
                                },
                            })
                        })
                }
            })
        },
        edit() {
            this.editing = true
        },
        cancel() {
            this.editing = false
            this.loadUserDetail()
        },
    },
}
</script>

<style lang="scss"></style>
