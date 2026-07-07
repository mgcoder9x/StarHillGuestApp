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
                                        'categories.vehicleCards.common.form.label.cardId'
                                    )
                                "
                                label-for="h-groups-code"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.vehicleCards.common.form.label.cardId'
                                        )
                                    "
                                >
                                    <b-form-input
                                        id="h-group-code"
                                        v-model="newVehicleCard.cardId"
                                        label-class="required"
                                        :placeholder="
                                            $t(
                                                'categories.vehicleCards.common.form.placeholder.cardId'
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
                                        'categories.vehicleCards.common.form.label.cardName'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.vehicleCards.common.form.label.cardName'
                                        )
                                    "
                                    label-for="h-group-name"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        id="h-full-name"
                                        v-model="newVehicleCard.cardName"
                                        :placeholder="
                                            $t(
                                                'categories.vehicleCards.common.form.placeholder.cardName'
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
                        <b-col md="8">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.vehicleCards.common.form.label.status'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.vehicleCards.common.form.label.status'
                                        )
                                    "
                                    label-for="h-group-name"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <v-select
                                        v-model="newVehicleCard.status"
                                        :placeholder="
                                            $t(
                                                'categories.vehicleCards.common.form.placeholder.status'
                                            )
                                        "
                                        label="text"
                                        :reduce="(status) => status.value"
                                        :options="statusList"
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
                                rules="max:250"
                                :name="
                                    $t(
                                        'categories.vehicleCards.common.form.label.note'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.vehicleCards.common.form.label.note'
                                        )
                                    "
                                    label-for="h-group-name"
                                    label-cols-md="4"
                                    :class="formGroupClass"
                                >
                                    <b-form-textarea
                                        id="h-note"
                                        v-model="newVehicleCard.note"
                                        :placeholder="
                                            $t(
                                                'categories.vehicleCards.common.form.placeholder.note'
                                            )
                                        "
                                        rows="3"
                                        max-rows="6"
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
                            v-if="authorize(['ManageVehicleCard'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button
                            v-if="authorize(['ManageVehicleCard'])"
                            type="button"
                            variant="success"
                            title="Connect"
                            class="mx-50 mb-50 btn-120"
                            @click="openSerialPortModal"
                        >
                            {{
                                $t(
                                    'categories.vehicleCards.common.form.button.connect'
                                )
                            }}
                        </b-button>
                        <b-button
                            :to="{ path: '/categories/vehicle-cards/list' }"
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

        <!-- Modal Serial Port -->
        <b-modal
            id="serial-port-modal"
            v-model="showSerialPortModal"
            :title="$t('serialPort.title')"
            size="lg"
            hide-footer
            @hidden="onModalHidden"
        >
            <SerialPortPopup />
        </b-modal>
    </validation-observer>
</template>
<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import SerialPortPopup from '@/components/SerialPortPopup.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import serialPortMixin from '@/mixins/serialPortMixin'

export default {
    components: {
        SerialPortPopup,
    },
    mixins: [authorizationMixin, serialPortMixin],
    data() {
        return {
            compTree: [],
            showSerialPortModal: false,
            newVehicleCard: {
                cardId: null,
                cardName: null,
                status: null,
                note: null,
            },
        }
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        statusList() {
            return [
                {
                    text: this.$t(
                        'categories.vehicleCards.enums.status.active'
                    ),
                    value: 1,
                },
                {
                    text: this.$t(
                        'categories.vehicleCards.enums.status.broken'
                    ),
                    value: 2,
                },
                {
                    text: this.$t('categories.vehicleCards.enums.status.lost'),
                    value: 3,
                },
            ]
        },
    },
    methods: {
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        // Override method từ serialPortMixin để xử lý dữ liệu từ đầu đọc thẻ
        onSerialDataReceived(data) {
            // Tự động fill vào trường cardId
            this.fillCardIdFromSerial(data, 'cardId', 'newVehicleCard')

            // Focus vào trường tiếp theo (cardName)
            this.$nextTick(() => {
                const cardNameInput = document.getElementById('h-full-name')
                if (cardNameInput) {
                    cardNameInput.focus()
                }
            })
        },
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                this.newVehicleCard.cardId = this.trimField(
                    this.newVehicleCard.cardId
                )
                this.newVehicleCard.cardName = this.trimField(
                    this.newVehicleCard.cardName
                )
                this.newVehicleCard.note = this.trimField(
                    this.newVehicleCard.note
                )
                if (!success) {
                } else {
                    try {
                        const res = await this.$services.post(
                            '/vehicle-cards',
                            this.newVehicleCard
                        )
                        this.showSuccessToast(res.data)
                        this.navigateToVehicleCardsList()
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
                        `categories.vehicleCards.error.${data.errorCode}`
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
        navigateToVehicleCardsList() {
            this.$router.push({ path: '/categories/vehicle-cards/list' })
        },
        openSerialPortModal() {
            this.showSerialPortModal = true
        },
        onModalHidden() {
            this.showSerialPortModal = false
        },
    },
}
</script>

<style lang="scss"></style>
