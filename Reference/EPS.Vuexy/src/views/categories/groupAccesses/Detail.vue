<!-- eslint-disable -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
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
                                        v-model="updateGroupAccess.compId"
                                        :options="options.compTree"
                                        label="text"
                                        :disabled="!editing"
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
                                        v-model="updateGroupAccess.groupId"
                                        :options="options.groups"
                                        :disabled="!editing"
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
                                        v-model="updateGroupAccess.timeAccessId"
                                        :options="options.timeAccesses"
                                        :reduce="(item) => parseInt(item.id)"
                                        label="text"
                                        :disabled="!editing"
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
                                label-for="h-groupAccesses-code"
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
                                        v-model="updateGroupAccess.areaIds"
                                        :options="options.areaTree"
                                        :reduce="(item) => parseInt(item.id)"
                                        :multiple="true"
                                        track-by="id"
                                        label="text"
                                        :disabled="!editing"
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
                                        `required|fromDate:${updateGroupAccess.effectiveTo}` 
                                    "
                                    :name="
                                        this.$t(
                                            'categories.groupAccesses.common.form.label.effectiveFrom'
                                        )
                                    "
                                >
                                    <b-form-datepicker
                                        id="createForm-effectiveFrom"
                                        v-model="updateGroupAccess.effectiveFrom"
                                        :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }"
                                        :disabled="!editing"
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
                                        v-model="updateGroupAccess.effectiveTo"
                                        :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }"
                                        :disabled="!editing"
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
                    <b-row>
                        <b-col class="text-center">
                            <Transition mode="out-in">
                                <b-button
                                    v-if="
                                        editing &&
                                        authorize(['ManageGroupAccess'])
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
                                        authorize(['ManageGroupAccess'])
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
                                :to="{ path: '/categories/groupAccesses/list' }"
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
/* eslint-disable */
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
                areaTree: [],
            },
            updateGroupAccess: {
                compId: null,
                groupId: null,
                timeAccessId: null,
                areaIds: [],
                effectiveFrom: null,
                effectiveTo: null
            },
            editing: false,
        }
    },
    computed: {
        groupAccessId() {
            return this.$route.params.groupAccessId
        },
        currentLocale() {
            return this.$i18n.locale
        },
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
    },
    watch: {
        'updateGroupAccess.compId'(val) {
            this.loadGroupsByComp(val)
            this.loadTimeAccesses(val)
            this.loadAreaTree(val)
        },
    },
    async created() {
        this.loadCompanyTree()
        this.loadGroups()
        this.loadTimeAccesses()
        this.loadAreaTree()
        await this.getTimeAccess()
    },
    methods: {
        loadCompanyTree() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.options.compTree = TreeHelper.removeEmptyChildren(response.data)
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
        loadAreaTree(compId) {
            const url = compId ? `/lookup/areas-tree?compId=${compId}` : '/lookup/areas-tree'
            this.$services.get(url).then((response) => {
                this.options.areaTree = TreeHelper.removeEmptyChildren(response.data.data)
            })
        },
        async getTimeAccess() {
            try {
                const res = await this.$services.get(
                    `/groupAccesses/${this.groupAccessId}`
                )
                this.updateGroupAccess = res.data.data
            } catch (error) {
                console.log('error')
            }
        },
        onSubmit() {
            this.$refs.rules.validate().then(async (isValid) => {
                if (isValid) {
                    try {
                        const res = await this.$services.put(
                            '/groupAccesses/' + this.groupAccessId,
                            this.updateGroupAccess
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
                    title: 'Error',
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(
                        `categories.groupAccesses.error.${error.errorCode}`
                    ),
                },
            })
        },
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getTimeAccess()
        },
    },
}
</script>

<style lang="scss"></style>
