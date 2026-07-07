<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row cols="1" align-h="center">
                        <!-- Start: input Data -->
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
                                        v-model="updatedController.code"
                                        :disabled="!editing"
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
                                        v-model="updatedController.name"
                                        :disabled="!editing"
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
                                        v-model="updatedController.ip"
                                        :disabled="!editing"
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
                                        v-model="updatedController.port"
                                        type="number"
                                        :disabled="!editing"
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
                    <b-row>
                        <b-col class="text-center">
                            <Transition mode="out-in">
                                <b-button
                                    v-if="
                                        editing &&
                                        authorize(['ManageController'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="onSubmit"
                                >
                                    {{ $t('common.button.save') }}
                                </b-button>
                                <b-button
                                    v-if="
                                        !editing &&
                                        authorize(['ManageController'])
                                    "
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="primary"
                                    @click="startEdit"
                                >
                                    {{ $t('common.button.edit') }}
                                </b-button>
                            </Transition>
                            <b-button
                                v-if="!editing"
                                :to="{ path: '/categories/controllers/list' }"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                            >
                                {{ $t('common.button.back') }}
                            </b-button>
                            <b-button
                                v-if="editing"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                                @click="stopEdit"
                            >
                                {{ $t('common.button.cancel') }}
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
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import { extend } from 'vee-validate'

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            updatedController: {
                code: null,
                name: null,
                ip: null,
                port: null,
            },
            editing: false,
        }
    },
    computed: {
        controllerId() {
            return this.$route.params.controllerId
        },
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
    async created() {
        this.setupValidationRules()
        await this.getController()
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
        async getController() {
            try {
                const res = await this.$services.get(
                    `/controllers/${this.controllerId}`
                )
                this.updatedController = res.data.data
            } catch (error) {
                console.log('error')
            }
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        getIpErrorMessage(errors) {
            return errors.length > 0 ? errors[0] : ''
        },
        onSubmit() {
            console.log('ố dề', this.updatedController)
            this.$refs.rules.validate().then(async (isValid) => {
                this.updatedController.code = this.trimField(
                    this.updatedController.code
                )
                this.updatedController.name = this.trimField(
                    this.updatedController.name
                )
                this.updatedController.ip = this.trimField(
                    this.updatedController.ip
                )
                // this.updatedController.port = this.trimField(
                //     this.updatedController.port
                // )
                if (isValid) {
                    try {
                        const res = await this.$services.put(
                            `/controllers/${this.controllerId}`,
                            this.updatedController
                        )
                        this.showSuccessToast(res.data)
                        this.stopEdit()
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
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getController()
        },
    },
}
</script>

<style lang="scss"></style>
