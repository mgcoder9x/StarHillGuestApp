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
                                        'categories.contractors.common.form.label.contractorCode'
                                    )
                                "
                                label-for="h-contractors-code"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    name="ContractorCode"
                                >
                                    <b-form-input
                                        id="h-contractor-code"
                                        v-model="newContractor.code"
                                        :placeholder="
                                            $t(
                                                'categories.contractors.common.form.placeholder.contractorCode'
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
                                name="ContractorName"
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.contractors.common.form.label.contractorName'
                                        )
                                    "
                                    label-for="h-contractor-name"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        id="h-full-name"
                                        v-model="newContractor.name"
                                        :placeholder="
                                            $t(
                                                'categories.contractors.common.form.placeholder.contractorName'
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
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.contractors.common.form.label.parentContractor'
                                    )
                                "
                                label-for="h-contractor-name"
                                label-cols-md="4"
                                :class="formGroupClass"
                            >
                                <tree-select
                                    v-model="newContractor.parentId"
                                    label="text"
                                    track-by="id"
                                    :reduce="(item) => parseInt(item.id)"
                                    :options="treeContractors"
                                    :multiple="false"
                                    :disabled="disabledParent"
                                    :placeholder="
                                        $t(
                                            'categories.contractors.common.form.placeholder.parentContractor'
                                        )
                                    "
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageContractor'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                            ><Icon
                                icon="material-symbols:save-outline"
                                class="sm-icon"
                            />
                            <span class="ml-25">
                                {{ $t('common.button.save') }}
                            </span>
                        </b-button>
                        <b-button
                            :to="{ path: '/categories/contractors/list' }"
                            type="reset"
                            variant="outline-secondary btn-hover-linear-secondary border-0"
                            title="Cancel"
                            class="btn-120 mb-50"
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
import TreeHelper from '@/utils/treeHelper'

export default {
    mixins: [authorizationMixin],
    components: {},
    data() {
        return {
            CONTRACTOR_TYPE: 2,
            treeContractors: [],
            newContractor: {
                code: null,
                name: null,
                parentId: null,
                note: null,
                compId: null,
                isDelete: false,
                type: 2,
            },
            // disabledParent: true,
        }
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        disabledParent() {
            return this.$route.query?.parentId !== undefined
        },
    },
    async created() {
        await this.getTreeContractors()
        this.newContractor.parentId =
            this.$route.query?.parentId !== undefined
                ? this.$route.query?.parentId
                : null
    },
    methods: {
        async getTreeContractors() {
            try {
                const res = await this.$services.get(
                    `/lookup/departments-tree?Type=${this.CONTRACTOR_TYPE}`
                )
                this.treeContractors = TreeHelper.removeEmptyChildren(
                    res.data.data
                )
            } catch (error) {
                console.log('error-getTreeContractors', error)
            }
        },
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then((success) => {
                if (!success) {
                    return
                    // handle validation errors...
                } else {
                    this.submitForm()
                }
            })
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        async submitForm() {
            try {
                this.newContractor.code = this.trimField(
                    this.newContractor.code
                )
                this.newContractor.name = this.trimField(
                    this.newContractor.name
                )

                const res = await this.$services.post(
                    '/departments',
                    this.newContractor
                )
                this.showSuccessToast(res.data)
                this.navigateToContractorsList()
            } catch (error) {
                this.showErrorToast(error)
            }
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(
                        `categories.contractors.error.${data.errorCode}`
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
        navigateToContractorsList() {
            this.$router.push({ path: '/categories/contractors/list' })
        },
    },
}
</script>
