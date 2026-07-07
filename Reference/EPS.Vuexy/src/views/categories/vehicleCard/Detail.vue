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
                                        'categories.vehicleCards.common.form.label.cardId'
                                    )
                                "
                                label-for="h-card-id"
                                label-cols-md="4"
                                class="mb-50 mb-md-1"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :name="
                                        $t(
                                            'categories.vehicleCards.common.form.label.cardId'
                                        )
                                    "
                                >
                                    <b-form-input
                                        id="h-card-id"
                                        v-model="updatedVehicleCard.cardId"
                                        :disabled="true"
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
                                    label-for="h-card-name"
                                    label-cols-md="4"
                                    label-class="required"
                                    class="mb-50 mb-md-1"
                                >
                                    <b-form-input
                                        id="h-card-name"
                                        v-model="updatedVehicleCard.cardName"
                                        :disabled="!editing"
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
                                    label-for="h-status"
                                    label-cols-md="4"
                                    label-class="required"
                                    class="mb-50 mb-md-1"
                                >
                                    <v-select
                                        v-model="updatedVehicleCard.status"
                                        :disabled="!editing"
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
                                    label-for="h-note"
                                    label-cols-md="4"
                                    class="mb-50 mb-md-1"
                                >
                                    <b-form-textarea
                                        id="h-note"
                                        v-model="updatedVehicleCard.note"
                                        :disabled="!editing"
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
                        <Transition mode="out-in">
                            <b-button
                                v-if="
                                    editing && authorize(['ManageVehicleCard'])
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
                                    !editing && authorize(['ManageVehicleCard'])
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
                            :to="{ path: '/categories/vehicle-cards/list' }"
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
                    </div>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>

<script>
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            compTree: [],
            updatedVehicleCard: {
                cardId: '',
                cardName: '',
                status: '',
                note: '',
            },
            editing: false,
        }
    },
    computed: {
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
        vehicleCardId() {
            return this.$route.params.vehicleCardId
        },
    },
    async created() {
        await this.getVehicleCard()
    },
    methods: {
        async getVehicleCard() {
            try {
                const res = await this.$services.get(
                    `/vehicle-cards/${this.vehicleCardId}`
                )
                this.updatedVehicleCard = res.data.data
            } catch (error) {
                console.log('error')
            }
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        onSubmit() {
            this.$refs.rules.validate().then(async (isValid) => {
                this.updatedVehicleCard.cardId = this.trimField(
                    this.updatedVehicleCard.cardId
                )
                this.updatedVehicleCard.cardName = this.trimField(
                    this.updatedVehicleCard.cardName
                )
                this.updatedVehicleCard.note = this.trimField(
                    this.updatedVehicleCard.note
                )
                if (isValid) {
                    try {
                        const res = await this.$services.put(
                            `/vehicle-cards/${this.$route.params.vehicleCardId}`,
                            this.updatedVehicleCard
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
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getVehicleCard()
        },
    },
}
</script>

<style lang="scss"></style>
