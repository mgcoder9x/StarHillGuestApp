<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row class="justify-content-center">
                        <b-col md="8" class="mx-auto">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.productionLines.common.form.label.name'
                                    )
                                "
                                label-for="create-production-line-name"
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
                                        id="create-working-shift-name"
                                        v-model="newProductionLine.name"
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
                                label-for="create-working-shift-name"
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
                                        id="create-production-line-code"
                                        v-model="newProductionLine.code"
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
                                    label-for="create-production-line-status"
                                    label-cols-md="3"
                                    label-class="required"
                                >
                                    <v-select
                                        v-model="newProductionLine.status"
                                        :options="statusOptions"
                                        label="text"
                                        :reduce="(opt) => opt.value"
                                        :clearable="false"
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
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageWorkingShift'])"
                            v-waves
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                        >
                            <Icon
                                icon="material-symbols:save-outline"
                                class="sm-icon"
                            />
                            <span class="ml-25">
                                {{ $t('common.button.save') }}
                            </span>
                        </b-button>
                        <b-button
                            v-waves
                            :to="{ path: '/categories/productionLines/list' }"
                            type="reset"
                            variant="secondary"
                            title="Cancel"
                            class="btn-120 mb-50 btn-hover-linear-secondary border-0"
                        >
                            <Icon icon="line-md:cancel" class="sm-icon" />
                            <span class="ml-25">
                                {{ $t('common.button.cancel') }}
                            </span>
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

export default {
    mixins: [authorizationMixin],
    data() {
        return {
            newProductionLine: {
                name: null,
                code: null,
                status: 1,
                compId: null,
            },
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.newProductionLine.compId = accessToken.companyId
    },
    computed: {
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
    methods: {
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then((success) => {
                if (!success) {
                    return
                } else {
                    this.submitForm()
                }
            })
        },
        async submitForm() {
            try {
                this.newProductionLine.name = this.newProductionLine.name.trim()
                this.newProductionLine.code = this.newProductionLine.code.trim()
                const res = await this.$services.post(
                    '/production-lines',
                    this.newProductionLine
                )
                this.showSuccessToast(res.data)
                this.navigateToProductionLinesList()
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
                        'categories.productionLines.error.CreateFail'
                    ),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(
                        `categories.productionLines.error.${error.message}`
                    ),
                },
            })
        },
        navigateToProductionLinesList() {
            this.$router.push({ path: '/categories/productionLines/list' })
        },
    },
}
</script>

<style lang="scss"></style>
