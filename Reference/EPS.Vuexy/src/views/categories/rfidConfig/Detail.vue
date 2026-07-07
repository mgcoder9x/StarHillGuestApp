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
                                        v-model="updatedConfig.code"
                                        :disabled="!editing"
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
                                        v-model="updatedConfig.name"
                                        :disabled="!editing"
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
                                    label-for="h-group-type"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <v-select
                                        :key="`type-${updatedConfig.typeId}-${typeDropdown.length}`"
                                        v-model="updatedConfig.typeId"
                                        :disabled="!editing"
                                        :placeholder="
                                            $t(
                                                'categories.rfidConfigs.common.form.placeholder.type'
                                            )
                                        "
                                        label="text"
                                        :reduce="(type) => Number(type.value)"
                                        :options="typeDropdown"
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
                                    label-for="h-group-protocol"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <v-select
                                        :key="`protocol-${updatedConfig.protocolId}-${protocolDropdown.length}`"
                                        v-model="updatedConfig.protocolId"
                                        :disabled="!editing"
                                        :placeholder="
                                            $t(
                                                'categories.rfidConfigs.common.form.placeholder.protocol'
                                            )
                                        "
                                        label="text"
                                        :reduce="
                                            (protocol) => Number(protocol.value)
                                        "
                                        :options="protocolDropdown"
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
                                        'categories.rfidConfigs.common.form.label.comPort'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.rfidConfigs.common.form.label.comPort'
                                        )
                                    "
                                    label-for="h-group-comPort"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <v-select
                                        v-model="updatedConfig.comPort"
                                        :disabled="!editing"
                                        :placeholder="
                                            $t(
                                                'categories.rfidConfigs.common.form.placeholder.comPort'
                                            )
                                        "
                                        label="text"
                                        :reduce="(comPort) => comPort.value"
                                        :options="comPortDropdown"
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
                                        'categories.rfidConfigs.common.form.label.baudRate'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.rfidConfigs.common.form.label.baudRate'
                                        )
                                    "
                                    label-for="h-group-baudRate"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <v-select
                                        v-model="updatedConfig.baudRate"
                                        :disabled="!editing"
                                        :placeholder="
                                            $t(
                                                'categories.rfidConfigs.common.form.placeholder.baudRate'
                                            )
                                        "
                                        label="text"
                                        :reduce="(baudRate) => baudRate.value"
                                        :options="baudRateDropdown"
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
                                        authorize(['ManageRFIDConfig'])
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
                                        authorize(['ManageRFIDConfig'])
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
                                :to="{ path: '/categories/rfid-configs/list' }"
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
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import { extend } from 'vee-validate'

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            updatedConfig: {
                code: null,
                name: null,
                typeId: null,
                protocolId: null,
                comPort: null,
                baudRate: null,
            },
            typeDropdown: [],
            protocolDropdown: [],
            baudRateDropdown: [],
            comPortDropdown: [],
            editing: false,
        }
    },
    computed: {
        rfidConfigId() {
            return this.$route.params.rfidConfigId
        },
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
    async created() {
        this.setupValidationRules()
        await this.loadDropdown()
        await this.getRfidConfig()
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
            console.log('current lang', lang)
            try {
                const response = await this.$services.get(
                    `/rfid-configs/dropdowns/?lang=${lang}`
                )
                this.typeDropdown = response.data.data.typeDropdown
                this.protocolDropdown = response.data.data.protocolDropdown
                this.baudRateDropdown = response.data.data.baudRateDropdown
                this.comPortDropdown = response.data.data.comPortDropdown
            } catch (error) {
                console.error('Error loading dropdowns:', error)
            }
        },
        async getRfidConfig() {
            try {
                const res = await this.$services.get(
                    `/rfid-configs/${this.rfidConfigId}`
                )
                this.updatedConfig = res.data.data
            } catch (error) {
                console.log('error')
            }
        },
        trimField(field) {
            return field && field.trim ? field.trim() : null
        },
        onSubmit() {
            this.$refs.rules.validate().then(async (isValid) => {
                this.updatedConfig.code = this.trimField(
                    this.updatedConfig.code
                )
                this.updatedConfig.name = this.trimField(
                    this.updatedConfig.name
                )
                if (isValid) {
                    try {
                        const res = await this.$services.put(
                            `/rfid-configs/${this.rfidConfigId}`,
                            this.updatedConfig
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
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getRfidConfig()
        },
    },
}
</script>

<style lang="scss"></style>
