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
                                        'categories.rfidConfigs.common.form.label.code'
                                    )
                                "
                                label-for="h-rfidConfigs-code"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|codeFormat"
                                    :name="
                                        $t(
                                            'categories.rfidConfigs.common.form.label.code'
                                        )
                                    "
                                >
                                    <b-form-input
                                        id="h-rfidConfigs-code"
                                        v-model="newConfig.code"
                                        :placeholder="
                                            $t(
                                                'categories.rfidConfigs.common.form.placeholder.code'
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
                                        'categories.rfidConfigs.common.form.label.name'
                                    )
                                "
                                label-for="h-rfidConfigs-name"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.rfidConfigs.common.form.label.name'
                                        )
                                    "
                                >
                                    <b-form-input
                                        id="h-rfidConfigs-name"
                                        v-model="newConfig.name"
                                        :placeholder="
                                            $t(
                                                'categories.rfidConfigs.common.form.placeholder.name'
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
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.rfidConfigs.common.form.label.type'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.rfidConfigs.common.form.label.type'
                                        )
                                    "
                                    label-for="h-group-name"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <v-select
                                        v-model="newConfig.typeId"
                                        :placeholder="
                                            $t(
                                                'categories.rfidConfigs.common.form.placeholder.type'
                                            )
                                        "
                                        label="text"
                                        :reduce="(type) => type.value"
                                        :options="dropdowns.typeDropdown"
                                    />

                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>

                        <b-col md="8">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.rfidConfigs.common.form.label.protocol'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.rfidConfigs.common.form.label.protocol'
                                        )
                                    "
                                    label-for="h-group-name"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <v-select
                                        v-model="newConfig.protocolId"
                                        :placeholder="
                                            $t(
                                                'categories.rfidConfigs.common.form.placeholder.protocol'
                                            )
                                        "
                                        label="text"
                                        :reduce="(protocol) => protocol.value"
                                        :options="dropdowns.protocolDropdown"
                                    />

                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <b-col md="8">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.rfidConfigs.common.form.label.comPort'
                                    )
                                "
                                label-for="h-rfidConfigs-code"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.rfidConfigs.common.form.label.comPort'
                                        )
                                    "
                                >
                                    <v-select
                                        v-model="newConfig.comPort"
                                        :placeholder="
                                            $t(
                                                'categories.rfidConfigs.common.form.placeholder.comPort'
                                            )
                                        "
                                        label="text"
                                        :reduce="(comPort) => comPort.value"
                                        :options="dropdowns.comPortDropdown"
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
                                        'categories.rfidConfigs.common.form.label.baudRate'
                                    )
                                "
                                label-for="h-rfidConfigs-code"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.rfidConfigs.common.form.label.baudRate'
                                        )
                                    "
                                >
                                    <v-select
                                        v-model="newConfig.baudRate"
                                        :placeholder="
                                            $t(
                                                'categories.rfidConfigs.common.form.placeholder.baudRate'
                                            )
                                        "
                                        label="text"
                                        :reduce="(baudRate) => baudRate.value"
                                        :options="dropdowns.baudRateDropdown"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <!-- End: input Data -->
                    </b-row>
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageRFIDConfig'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button
                            :to="{ path: '/categories/rfid-configs/list' }"
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
            newConfig: {
                code: null,
                name: null,
                typeId: null,
                protocolId: null,
                comPort: null,
                baudRate: null,
            },
            dropdowns: {
                typeDropdown: [],
                protocolDropdown: [],
                baudRateDropdown: [],
                comPortDropdown: [],
            },
        }
    },
    async created() {
        this.setupValidationRules()
        await this.loadDropdown()
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
                this.loadDropdown()
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
            // Custom validation rule cho code format with localized message
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
                    this.$t('categories.rfidConfigs.validation.codeFormat') ||
                    'Code must not contain spaces or special characters (only a-z, A-Z, 0-9, _)',
            })
        },
        async loadDropdown() {
            const lang = this.$i18n.locale
            try {
                const response = await this.$services.get(
                    `/rfid-configs/dropdowns/?lang=${lang}`
                )
                this.dropdowns = {
                    typeDropdown: response.data.data.typeDropdown,
                    protocolDropdown: response.data.data.protocolDropdown,
                    baudRateDropdown: response.data.data.baudRateDropdown,
                    comPortDropdown: response.data.data.comPortDropdown,
                }
            } catch (error) {
                console.error('Error loading dropdowns:', error)
            }
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                this.newConfig.name = this.trimField(this.newConfig.name)
                this.newConfig.code = this.trimField(this.newConfig.code)
                if (!success) {
                } else {
                    try {
                        const res = await this.$services.post(
                            '/rfid-configs',
                            this.newConfig
                        )
                        this.showSuccessToast(res.data)
                        this.navigateToRfidConfigsList()
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
                        `categories.rfidConfigs.error.${data.errorCode}`
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
        navigateToRfidConfigsList() {
            this.$router.push({ path: '/categories/rfid-configs/list' })
        },
    },
}
</script>

<style lang="scss"></style>
