<template>
    <validation-observer ref="rules">
        <b-card>
            <!-- form -->
            <b-form>
                <b-row>
                    <!-- old password -->
                    <b-col md="6">
                        <b-form-group
                            :label="$t('ChangePassword.Label.OldPass')"
                            label-for="account-old-password"
                            label-cols-md="12"
                            label-class="required"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                name="AccountOldPassword"
                            >
                                <b-input-group class="input-group-merge">
                                    <b-form-input
                                        id="account-old-password"
                                        v-model="user.oldPassword"
                                        name="old-password"
                                        :type="passwordFieldTypeOld"
                                    />
                                    <b-input-group-append is-text>
                                        <feather-icon
                                            :icon="passwordToggleIconOld"
                                            class="cursor-pointer"
                                            @click="togglePasswordOld"
                                        />
                                    </b-input-group-append>
                                </b-input-group>
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                    <!--/ old password -->
                </b-row>
                <b-row>
                    <!-- new password -->
                    <b-col md="6">
                        <b-form-group
                            label-for="account-new-password"
                            :label="$t('ChangePassword.Label.NewPass')"
                            label-cols-md="12"
                            label-class="required"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required|min:8|password"
                                name="AccountNewPassword"
                            >
                                <b-input-group class="input-group-merge">
                                    <b-form-input
                                        id="account-new-password"
                                        v-model="user.newPassword"
                                        :type="passwordFieldTypeNew"
                                        name="new-password"
                                    />
                                    <b-input-group-append is-text>
                                        <feather-icon
                                            :icon="passwordToggleIconNew"
                                            class="cursor-pointer"
                                            @click="togglePasswordNew"
                                        />
                                    </b-input-group-append>
                                </b-input-group>
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                    <!--/ new password -->

                    <!-- retype password -->
                    <b-col md="6">
                        <b-form-group
                            label-for="account-retype-new-password"
                            :label="$t('ChangePassword.Label.RePass')"
                            label-cols-md="12"
                            label-class="required"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required|min:8|confirmed:AccountNewPassword"
                                name="AccountRetypeNewPassword"
                            >
                                <b-input-group class="input-group-merge">
                                    <b-form-input
                                        id="account-retype-new-password"
                                        v-model="user.newPasswordConfirm"
                                        :type="passwordFieldTypeRetype"
                                        name="retype-password"
                                    />
                                    <b-input-group-append is-text>
                                        <feather-icon
                                            :icon="passwordToggleIconRetype"
                                            class="cursor-pointer"
                                            @click="togglePasswordRetype"
                                        />
                                    </b-input-group-append>
                                </b-input-group>
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                    <!--/ retype password -->

                    <!-- buttons -->
                    <b-col cols="12">
                        <b-button
                            v-ripple.400="'rgba(255, 255, 255, 0.15)'"
                            variant="primary"
                            class="mt-1 mr-1"
                            @click.prevent="save"
                        >
                            {{ $t('Button.Save') }}
                        </b-button>
                    </b-col>
                    <!--/ buttons -->
                </b-row>
            </b-form>
        </b-card>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import Ripple from 'vue-ripple-directive'
import i18n from '@/libs/i18n'

export default {
    components: {},
    directives: {
        Ripple,
    },
    data() {
        return {
            user: {
                username: null,
                oldPassword: '',
                newPassword: '',
                newPasswordConfirm: '',
            },
            passwordFieldTypeOld: 'password',
            passwordFieldTypeNew: 'password',
            passwordFieldTypeRetype: 'password',
        }
    },
    computed: {
        passwordToggleIconOld() {
            return this.passwordFieldTypeOld === 'password'
                ? 'EyeIcon'
                : 'EyeOffIcon'
        },
        passwordToggleIconNew() {
            return this.passwordFieldTypeNew === 'password'
                ? 'EyeIcon'
                : 'EyeOffIcon'
        },
        passwordToggleIconRetype() {
            return this.passwordFieldTypeRetype === 'password'
                ? 'EyeIcon'
                : 'EyeOffIcon'
        },
    },
    methods: {
        togglePasswordOld() {
            this.passwordFieldTypeOld =
                this.passwordFieldTypeOld === 'password' ? 'text' : 'password'
        },
        togglePasswordNew() {
            this.passwordFieldTypeNew =
                this.passwordFieldTypeNew === 'password' ? 'text' : 'password'
        },
        togglePasswordRetype() {
            this.passwordFieldTypeRetype =
                this.passwordFieldTypeRetype === 'password'
                    ? 'text'
                    : 'password'
        },
        save() {
            let accessToken = this.$services.getUserData()
            let infoId = accessToken.userId
            this.user.username = accessToken.username
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .put(`/users/change-password/${infoId}`, this.user)
                        .then((response) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t(
                                        'ChangePassword.Message.ChangeSuccess'
                                    ),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })

                            // Remove userData from localStorage
                            // ? You just removed token from localStorage. If you like, you can also make API call to backend to blacklist used token
                            localStorage.removeItem(
                                this.$services.jwtConfig.storageTokenKeyName
                            )
                            localStorage.removeItem(
                                this.$services.jwtConfig
                                    .storageRefreshTokenKeyName
                            )

                            // Remove userData from localStorage
                            localStorage.removeItem(
                                this.$services.jwtConfig.storageUserData
                            )

                            // Redirect to login page
                            this.$router.push({ name: 'auth-login' })
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: i18n.t(`${error.error}`),
                                },
                            })
                        })
                }
            })
        },
    },
}
</script>
