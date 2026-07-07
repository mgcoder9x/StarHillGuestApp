<!-- eslint-disable -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row cols="2" align-h="center">
                        <!-- Start: input Data -->
                        <b-col md="7">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.groupAccesses.common.form.label.compName'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.groupAccesses.common.form.label.compName'
                                        )
                                    "
                                >
                                    <tree-select
                                        v-model="newGroupAccess.compId"
                                        :options="options.compTree"
                                        label="text"
                                        :reduce="(option) => option.id"
                                        :placeholder="
                                            $t(
                                                'categories.groupAccesses.common.form.placeholder.compName'
                                            )
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="7">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.groupAccesses.common.form.label.groupName'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.groupAccesses.common.form.label.groupName'
                                        )
                                    "
                                >
                                    <v-select
                                        v-model="newGroupAccess.groupId"
                                        :options="options.groups"
                                        :reduce="(item) => parseInt(item.id)"
                                        label="text"
                                        :placeholder="
                                            $t(
                                                'categories.groupAccesses.common.form.placeholder.groupName'
                                            )
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="7">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.groupAccesses.common.form.label.timeAccessName'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.groupAccesses.common.form.label.timeAccessName'
                                        )
                                    "
                                >
                                    <v-select
                                        v-model="newGroupAccess.timeAccessId"
                                        :options="options.timeAccesses"
                                        :reduce="(item) => parseInt(item.id)"
                                        label="text"
                                        :placeholder="
                                            $t(
                                                'categories.groupAccesses.common.form.placeholder.timeAccessName'
                                            )
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="7">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.groupAccesses.common.form.label.areaName'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.groupAccesses.common.form.label.areaName'
                                        )
                                    "
                                >
                                    <tree-select
                                        label="text"
                                        v-model="newGroupAccess.areaIds"
                                        :options="options.areaTree"
                                        :reduce="(item) => parseInt(item.id)"
                                        :value-consists-of="'ALL'"
                                        :multiple="true"
                                        :limit="3"
                                        track-by="id"
                                        :placeholder="
                                            $t(
                                                'categories.groupAccesses.common.form.placeholder.areaName'
                                            )
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="7">
                            <b-form-group
                                :label="
                                    this.$t(
                                        'categories.groupAccesses.common.form.label.effectiveFrom'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :rules="
                                        `required|fromDate:${newGroupAccess.effectiveTo}` 
                                    "
                                    :name="
                                        this.$t(
                                            'categories.groupAccesses.common.form.label.effectiveFrom'
                                        )
                                    "
                                >
                                    <b-form-datepicker
                                        id="createForm-effectiveFrom"
                                        v-model="newGroupAccess.effectiveFrom"
                                        :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }"
                                        reset-button
                                        type="datetime"
                                        :locale="currentLocale"
                                    >
                                    </b-form-datepicker>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="7">
                            <b-form-group
                                :label="
                                    this.$t(
                                        'categories.groupAccesses.common.form.label.effectiveTo'
                                    )
                                "
                                label-class="required"
                                label-cols-md="4"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        this.$t(
                                            'categories.groupAccesses.common.form.label.effectiveTo'
                                        )
                                    "
                                >
                                    <b-form-datepicker
                                        id="createForm-effectiveTo"
                                        v-model="newGroupAccess.effectiveTo"
                                        :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }"
                                        reset-button
                                        type="datetime"
                                        :locale="currentLocale"
                                    >
                                    </b-form-datepicker>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <!-- Button Action -->
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageGroupAccess'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120 btn-hover-linear-primary"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button
                            @click="navigateToList()"
                            type="reset"
                            variant="outline-secondary"
                            title="Cancel"
                            class="btn-120 mb-50 btn-hover-linear-secondary"
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
/*eslint-disable*/

import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import TreeHelper from '@/utils/treeHelper'

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            options: {
                compTree: [],
                groups: [],
                timeAccesses: [],
                areaTree: null,
            },
            newGroupAccess: {
                compId: null,
                groupId: null,
                timeAccessId: null,
                areaIds: [],
                effectiveFrom: null,
                effectiveTo: null,
            },
        }
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        currentLocale() {
            return this.$i18n.locale
        },
    },
    watch: {
        'newGroupAccess.compId'(val) {
            this.loadGroupsByComp(val)
            this.loadTimeAccesses(val)
            this.loadAreas(val)
        },
    },
    async created() {
        await this.loadOptions()
    },
    methods: {
        async loadOptions() {
            this.loadCompanyTree()
            this.loadGroups()
            this.loadTimeAccesses()
            this.loadAreas()
        },
        loadCompanyTree() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.options.compTree = TreeHelper.removeEmptyChildren(
                    response.data
                )
            })
        },
        loadGroups() {
            this.$services.get('/lookup/groups').then((response) => {
                this.options.groups = response.data.data
            })
        },
        loadGroupsByComp(compId) {
            const url = compId ? `/lookup/groups?compId=${compId}` : '/lookup/groups'
            this.$services.get(url).then((response) => {
                this.options.groups = response.data.data
            })
        },
        loadTimeAccesses(compId) {
            const url = compId ? `/lookup/timeAccesses?compId=${compId}` : '/lookup/timeAccesses'
            this.$services.get(url).then((response) => {
                this.options.timeAccesses = response.data.data
            })
        },
        async loadAreas(compId) {
            const url = compId ? `/lookup/areas-tree?compId=${compId}` : '/lookup/areas-tree'
            const response = await this.$services.get(url)
            this.options.areaTree = TreeHelper.removeEmptyChildren(
                response.data.data
            )
        },
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                if (success) {
                    try {
                        const res = await this.$services.post(
                            '/groupAccesses',
                            this.newGroupAccess
                        )
                        this.showSuccessToast(res.data)
                        this.navigateToList()
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
                        `categories.groupAccesses.error.${data.errorCode}`
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
                        `categories.groupAccesses.error.${error.errorCode}`
                    ),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: '',
                },
            })
        },
        navigateToList() {
            this.$router.push({ path: '/categories/groupAccesses/list' })
        },
    },
}
</script>

<style lang="scss"></style>
