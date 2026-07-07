<!-- eslint-disable vue/html-self-closing -->
<template>
    <b-container fluid class="p-0">
        <b-card>
            <validation-observer ref="rules">
                <b-form>
                    <b-row cols="1" align-h="center">
                        <b-col md="8">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.contractors.common.form.label.contractorCode'
                                    )
                                "
                                label-for="h-contractor-code"
                                label-cols-md="4"
                                label-class="required"
                                class="mb-50 mb-md-1"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    name="ContractorCode"
                                >
                                    <b-form-input
                                        id="h-contractor-code"
                                        v-model="updatedContractor.code"
                                        :disabled="!editing"
                                        placeholder="Mã"
                                        :state="errors.length ? false : null"
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
                                >
                                    <b-form-input
                                        id="h-contractor-name"
                                        v-model="updatedContractor.name"
                                        :disabled="!editing"
                                        :placeholder="
                                            $t(
                                                'categories.contractors.common.form.label.contractorName'
                                            )
                                        "
                                        :state="errors.length ? false : null"
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
                                :rules="{ is_not: parseInt(contractorId) }"
                                name="ParentContractorName"
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.contractors.common.form.label.parentContractor'
                                        )
                                    "
                                    label-for="h-contractor-name"
                                    label-cols-md="4"
                                    class="mb-50 mb-md-1"
                                >
                                    <tree-select
                                        v-model="updatedContractor.parentId"
                                        label="text"
                                        track-by="id"
                                        :reduce="(item) => parseInt(item.id)"
                                        :options="treeContractors"
                                        :multiple="false"
                                        :disabled="!editing"
                                        :placeholder="
                                            $t(
                                                'categories.contractors.common.form.placeholder.parentContractor'
                                            )
                                        "
                                        :state="errors.length ? false : null"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                            ? 'Vui lòng chọn lại nhà thầu'
                                            : null
                                    }}</small>
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
                                            authorize(['ManageContractor'])
                                        "
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
                                            authorize(['ManageContractor'])
                                        "
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
                                    :to="{
                                        path: '/categories/contractors/list',
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
                                    type="button"
                                    variant="outline-secondary"
                                    class="mx-50 mb-50 btn-120 btn-hover-linear-secondary border-0"
                                    @click="stopEdit"
                                >
                                    <Icon
                                        icon="line-md:cancel"
                                        class="sm-icon"
                                    />
                                    <span class="ml-25">
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
import TreeHelper from '@/utils/treeHelper'

export default {
    mixins: [authorizationMixin],
    components: {},
    data() {
        return {
            CONTRACTOR_TYPE: 2,
            treeContractors: [],
            updatedContractor: {
                code: '',
                name: '',
                parentId: null,
            },
            editing: false,
        }
    },
    computed: {
        contractorId() {
            return this.$route.params.contractorId
        },
    },
    async created() {
        await this.getTreeContractors()
        await this.getContractor()
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
        async getContractor() {
            try {
                const res = await this.$services.get(
                    `/departments/${this.contractorId}`
                )
                this.updatedContractor = res.data.data
            } catch (error) {
                console.log('error')
            }
        },
        validateAndSubmitForm() {
            this.$refs.rules.validate().then((isValid) => {
                if (isValid) {
                    this.submitForm()
                }
            })
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        async submitForm() {
            try {
                this.updatedContractor.code = this.trimField(
                    this.updatedContractor.code
                )
                this.updatedContractor.name = this.trimField(
                    this.updatedContractor.name
                )
                const res = await this.$services.put(
                    `/departments/${this.$route.params.contractorId}`,
                    this.updatedContractor
                )
                console.log(res)
                this.showSuccessToast(res.data)
                this.stopEdit()
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
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getContractor()
        },
    },
}
</script>
