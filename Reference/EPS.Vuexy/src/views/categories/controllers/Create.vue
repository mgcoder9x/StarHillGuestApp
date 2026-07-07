<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row cols="1" align-h="center">
                        <b-col md="8">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.controllers.common.form.label.code'
                                    )
                                "
                                label-for="h-controllers-code"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|codeFormat"
                                    :name="
                                        $t(
                                            'categories.controllers.common.form.label.code'
                                        )
                                    "
                                >
                                    <b-form-input
                                        id="h-controllers-code"
                                        v-model="newController.code"
                                        :placeholder="
                                            $t(
                                                'categories.controllers.common.form.placeholder.code'
                                            )
                                        "
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
                        <b-col md="8">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.controllers.common.form.label.name'
                                    )
                                "
                                label-for="h-controllers-name"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|max:250"
                                    :name="
                                        $t(
                                            'categories.controllers.common.form.label.name'
                                        )
                                    "
                                >
                                    <b-form-input
                                        id="h-controllers-name"
                                        v-model="newController.name"
                                        :placeholder="
                                            $t(
                                                'categories.controllers.common.form.placeholder.name'
                                            )
                                        "
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

                        <b-col md="8">
                            <validation-provider
                                v-slot="{ errors }"
                                rules="required|max:15|ipFormat"
                                :name="
                                    $t(
                                        'categories.controllers.common.form.label.ip'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.controllers.common.form.label.ip'
                                        )
                                    "
                                    label-for="h-group-name"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        id="h-controllers-ip"
                                        v-model="newController.ip"
                                        :placeholder="
                                            $t(
                                                'categories.controllers.common.form.placeholder.ip'
                                            )
                                        "
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">
                                        {{ getIpErrorMessage(errors) }}
                                    </small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>

                        <b-col md="8">
                            <validation-provider
                                #default="{ errors }"
                                :name="
                                    $t(
                                        'categories.controllers.common.form.label.port'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.controllers.common.form.label.port'
                                        )
                                    "
                                    label-for="h-group-name"
                                    label-cols-md="4"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        id="h-controllers-port"
                                        v-model="newController.port"
                                        type="number"
                                        :placeholder="
                                            $t(
                                                'categories.controllers.common.form.placeholder.port'
                                            )
                                        "
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
                        <!-- End: input Data -->
                    </b-row>
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageController'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button
                            :to="{ path: '/categories/controllers/list' }"
                            type="reset"
                            variant="outline-secondary"
                            title="Cancel"
                            class="btn-120 mb-50"
                        >
                            {{ $t('common.button.cancel') }}
                        </b-button>
                    </div>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>
<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import { extend } from 'vee-validate'

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            newController: {
                code: null,
                name: null,
                ip: null,
                port: null,
            },
        }
    },
    async created() {
        this.setupValidationRules()
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
    },
    watch: {
        '$i18n.locale': {
            handler() {
                this.setupValidationRules() // Re-setup validation rules with new locale
                // Re-validate to regenerate validation messages with new locale
                this.$nextTick(() => {
                    if (this.$refs.rules) {
                        this.$refs.rules.validate()
                    }
                })
            },
        },
    },
    methods: {
        setupValidationRules() {
            // Custom code validation rule with localized message
            extend('codeFormat', {
                validate: (value) => {
                    if (!value) return true // Let required rule handle empty values

                    // Không cho phép space
                    if (/\s/.test(value)) {
                        return false
                    }

                    // Chỉ cho phép chữ, số và dấu gạch dưới
                    if (!/^[a-zA-Z0-9_]+$/.test(value)) {
                        return false
                    }

                    return true
                },
                message:
                    this.$t('categories.controllers.validation.codeFormat') ||
                    'Code must not contain spaces or special characters (only a-z, A-Z, 0-9, _)',
            })

            // Custom IP validation rule with localized message
            extend('ipFormat', {
                validate: (value) => {
                    const ipRegex =
                        /^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/
                    return ipRegex.test(value)
                },
                message:
                    this.$t('categories.controllers.validation.ipFormat') ||
                    'Vui lòng nhập đúng định dạng IP',
            })
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        getIpErrorMessage(errors) {
            return errors.length > 0 ? errors[0] : ''
        },
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                this.newController.name = this.trimField(
                    this.newController.name
                )
                this.newController.code = this.trimField(
                    this.newController.code
                )
                this.newController.ip = this.trimField(this.newController.ip)
                this.newController.port = this.trimField(
                    this.newController.port
                )
                if (!success) {
                } else {
                    try {
                        const res = await this.$services.post(
                            '/controllers',
                            this.newController
                        )
                        this.showSuccessToast(res.data)
                        this.navigateToControllersList()
                    } catch (error) {
                        this.showErrorToast(error)
                    }
                }
            })
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(
                        `categories.controllers.error.${data.errorCode}`
                    ),
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
        showErrorToast(error) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t('Success.Warning'),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(`${error.message}`),
                },
            })
        },
        navigateToControllersList() {
            this.$router.push({ path: '/categories/controllers/list' })
        },
    },
}
</script>

<style lang="scss"></style>
