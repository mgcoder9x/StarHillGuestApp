<template>
    <b-container fluid class="p-0">
        <b-card>
            <validation-observer ref="rules">
                <b-form>
                    <b-row class="justify-content-center">
                        <b-col md="8" class="mx-auto">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.productionLines.common.form.label.name'
                                    )
                                "
                                label-for="detail-production-line-name"
                                label-cols-md="3"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    :name="
                                        $t(
                                            'categories.productionLines.common.form.label.name'
                                        )
                                    "
                                >
                                    <b-form-input
                                        id="detail-production-line-name"
                                        v-model="updatedProductionLine.name"
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
                        <b-col md="8" class="mx-auto">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.productionLines.common.form.label.code'
                                    )
                                "
                                label-for="detail-production-line-code"
                                label-cols-md="3"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    :name="
                                        $t(
                                            'categories.productionLines.common.form.label.code'
                                        )
                                    "
                                >
                                    <b-form-input
                                        id="detail-production-line-code"
                                        v-model="updatedProductionLine.code"
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
                        <b-col md="8" class="mx-auto">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.productionLines.common.form.label.status'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.productionLines.common.form.label.status'
                                        )
                                    "
                                    label-for="detail-production-line-status"
                                    label-cols-md="3"
                                    label-class="required"
                                >
                                    <v-select
                                        v-model="updatedProductionLine.status"
                                        :options="statusOptions"
                                        label="text"
                                        :reduce="(opt) => opt.value"
                                        :clearable="false"
                                        :disabled="!editing"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col>
                            <div class="text-center">
                                <Transition mode="out-in">
                                    <b-button
                                        v-if="
                                            editing &&
                                            authorize(['ManageWorkingShift'])
                                        "
                                        v-waves
                                        type="button"
                                        variant="primary"
                                        class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                                        @click="validateAndSubmitForm"
                                    >
                                        <Icon
                                            icon="material-symbols:save-outline"
                                            class="sm-icon"
                                        />
                                        <span class="ml-50">
                                            {{ $t('common.button.save') }}
                                        </span>
                                    </b-button>
                                    <b-button
                                        v-if="
                                            !editing &&
                                            authorize(['ManageWorkingShift'])
                                        "
                                        v-waves
                                        type="button"
                                        variant="primary"
                                        class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                                        @click="startEdit"
                                    >
                                        <Icon
                                            icon="line-md:edit-twotone"
                                            class="sm-icon"
                                        />
                                        <span class="ml-25">
                                            {{ $t('common.button.edit') }}
                                        </span>
                                    </b-button>
                                </Transition>
                                <b-button
                                    v-if="!editing"
                                    v-waves
                                    :to="{
                                        path: '/categories/productionLines/list',
                                    }"
                                    type="button"
                                    variant="outline-secondary"
                                    class="mx-50 mb-50 btn-120 btn-hover-linear-secondary border-0"
                                >
                                    <Icon
                                        icon="line-md:arrow-small-left"
                                        class="sm-icon"
                                    />
                                    <span class="ml-25">
                                        {{ $t('common.button.back') }}
                                    </span>
                                </b-button>
                                <b-button
                                    v-if="editing"
                                    v-waves
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary btn-hover-linear-secondary border-0"
                                    @click="stopEdit"
                                >
                                    <Icon icon="mdi:cancel" class="sm-icon" />
                                    <span class="ml-50">
                                        {{ $t('common.button.cancel') }}
                                    </span>
                                </b-button>
                            </div>
                        </b-col>
                    </b-row>
                </b-form>
            </validation-observer>
        </b-card>
    </b-container>
</template>

<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'

export default {
    mixins: [authorizationMixin],
    data() {
        return {
            updatedProductionLine: {
                name: null,
                code: null,
                status: 1,
                compId: null,
            },
            editing: false,
        }
    },
    computed: {
        productionLineId() {
            return this.$route.params.id
        },
        currentLocale() {
            return this.$i18n.locale
        },
        statusOptions() {
            return [
                {
                    value: 1,
                    text: this.$t(
                        'categories.productionLines.list.searchForm.options.active'
                    ),
                },
                {
                    value: 0,
                    text: this.$t(
                        'categories.productionLines.list.searchForm.options.inactive'
                    ),
                },
            ]
        },
    },
    async created() {
        const accessToken = this.$services.getUserData()
        this.updatedProductionLine.compId = accessToken.companyId
        await this.getProductionLine()
    },
    methods: {
        async getProductionLine() {
            try {
                const res = await this.$services.get(
                    `/production-lines/${this.productionLineId}`
                )
                this.updatedProductionLine = res.data.data
            } catch (error) {
                console.log(error)
                this.showErrorToast(error)
            }
        },
        validateAndSubmitForm() {
            this.$refs.rules.validate().then((isValid) => {
                if (isValid) {
                    this.submitForm()
                }
            })
        },
        async submitForm() {
            try {
                this.updatedProductionLine.name =
                    this.updatedProductionLine.name.trim()
                this.updatedProductionLine.code =
                    this.updatedProductionLine.code.trim()
                const res = await this.$services.put(
                    `/production-lines/${this.productionLineId}`,
                    this.updatedProductionLine
                )
                this.showSuccessToast(res.data)
                this.stopEdit()
            } catch (error) {
                console.log(error)
                this.showErrorToast(error)
            }
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(
                        `categories.productionLines.error.${data.errorCode}`
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
                    title: this.$t(
                        'categories.productionLines.error.UpdateFail'
                    ),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(
                        `categories.productionLines.error.${error.message}`
                    ),
                },
            })
        },
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getProductionLine()
        },
    },
}
</script>

<style lang="scss"></style>
