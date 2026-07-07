<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row cols="1" align-h="center">
                        <!-- Start: input Data -->
                        <b-col md="8">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.groups.common.form.label.compName'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.groups.common.form.label.compName'
                                        )
                                    "
                                    label-for="h-group-name"
                                    label-class="required"
                                    label-cols-md="4"
                                    class="mb-50 mb-md-1"
                                >
                                    <tree-select
                                        v-model="updatedGroup.compId"
                                        :disabled="!editing"
                                        :options="compTree"
                                        label="text"
                                        :reduce="(option) => option.id"
                                        :placeholder="
                                            $t(
                                                'categories.groups.common.form.placeholder.compName'
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
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.groups.common.form.label.groupCode'
                                    )
                                "
                                label-for="h-group-code"
                                label-cols-md="4"
                                label-class="required"
                                class="mb-50 mb-md-1"
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
                                        v-model="updatedGroup.groupCode"
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
                                >
                                    <b-form-input
                                        id="h-group-name"
                                        v-model="updatedGroup.groupName"
                                        :disabled="!editing"
                                        :placeholder="
                                            $t(
                                                'categories.groups.common.form.label.groupName'
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
                        <!-- End: input Data -->
                    </b-row>
                    <b-row>
                        <b-col class="text-center">
                            <Transition mode="out-in">
                                <b-button
                                    v-if="editing && authorize(['ManageGroup'])"
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="onSubmit"
                                >
                                    {{ $t('common.button.save') }}
                                </b-button>
                                <b-button
                                    v-if="
                                        !editing && authorize(['ManageGroup'])
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
                                :to="{ path: '/categories/groups/list' }"
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

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            compTree: [],
            updatedGroup: {
                groupCode: '',
                groupName: '',
                parentId: null,
                compId: '',
            },
            editing: false,
        }
    },
    computed: {
        groupId() {
            return this.$route.params.groupId
        },
    },
    async created() {
        this.loadCompanyTree()
        await this.getGroup()
    },
    methods: {
        loadCompanyTree() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.compTree = response.data
            })
        },
        async getGroup() {
            try {
                const res = await this.$services.get(`/groups/${this.groupId}`)
                this.updatedGroup = res.data.data
            } catch (error) {
                console.log('error')
            }
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        onSubmit() {
            this.$refs.rules.validate().then(async (isValid) => {
                this.updatedGroup.groupCode = this.trimField(
                    this.updatedGroup.groupCode
                )
                this.updatedGroup.groupName = this.trimField(
                    this.updatedGroup.groupName
                )
                if (isValid) {
                    try {
                        const res = await this.$services.put(
                            `/groups/${this.$route.params.groupId}`,
                            this.updatedGroup
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
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getGroup()
        },
    },
}
</script>

<style lang="scss"></style>
