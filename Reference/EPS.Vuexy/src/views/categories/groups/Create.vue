<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row cols="1" align-h="center">
                        <!-- Start: input Data -->
                        <b-col md="8">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.groups.common.form.label.compName'
                                    )
                                "
                                label-for="h-group-name"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.groups.common.form.label.compName'
                                        )
                                    "
                                >
                                    <tree-select
                                        v-model="newGroup.compId"
                                        :options="compTree"
                                        label="text"
                                        :reduce="(option) => option.id"
                                        :placeholder="
                                            $t(
                                                'categories.groups.common.form.placeholder.compName'
                                            )
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
                                        'categories.groups.common.form.label.groupCode'
                                    )
                                "
                                label-for="h-groups-code"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|regex:^[a-zA-Z0-9]+$"
                                    :name="
                                        $t(
                                            'categories.groups.common.form.label.groupCode'
                                        )
                                    "
                                >
                                    <b-form-input
                                        id="h-group-code"
                                        v-model="newGroup.groupCode"
                                        :placeholder="
                                            $t(
                                                'categories.groups.common.form.placeholder.groupCode'
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
                                        'categories.groups.common.form.label.groupName'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.groups.common.form.label.groupName'
                                        )
                                    "
                                    label-for="h-group-name"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        id="h-full-name"
                                        v-model="newGroup.groupName"
                                        :placeholder="
                                            $t(
                                                'categories.groups.common.form.placeholder.groupName'
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
                            v-if="authorize(['ManageGroup'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button
                            :to="{ path: '/categories/groups/list' }"
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

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            compTree: [],
            newGroup: {
                groupCode: null,
                groupName: null,
                compId: null,
                isDelete: false,
            },
        }
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
    },
    async created() {
        this.loadCompanyTree()
    },
    methods: {
        loadCompanyTree() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.compTree = response.data
            })
        },
        normalizer(node) {
            if (node.children == null || node.children == 'null') {
                delete node.children
            }
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                this.newGroup.groupCode = this.trimField(
                    this.newGroup.groupCode
                )
                this.newGroup.groupName = this.trimField(
                    this.newGroup.groupName
                )
                if (!success) {
                    // handle validation errors...
                } else {
                    try {
                        const res = await this.$services.post(
                            '/groups',
                            this.newGroup
                        )
                        this.showSuccessToast(res.data)
                        this.navigateTogroupsList()
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
                    title: this.$t(`categories.groups.error.${data.errorCode}`),
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
        navigateTogroupsList() {
            this.$router.push({ path: '/categories/groups/list' })
        },
    },
}
</script>

<style lang="scss"></style>
