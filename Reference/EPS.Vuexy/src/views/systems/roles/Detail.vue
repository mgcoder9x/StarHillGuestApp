<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row align-h="center">
                        <b-col md="6">
                            <b-form-group
                                :label="$t('System.Role.Field.Name')"
                                label-for="h-role-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="RoleName"
                                >
                                    <b-form-input
                                        id="h-role-name"
                                        v-model="role.name"
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
                    </b-row>
                    <b-row align-h="center">
                        <b-col md="6">
                            <b-form-group
                                :label="$t('System.Role.Field.Description')"
                                label-for="h-description"
                                label-cols-md="4"
                            >
                                <b-form-textarea
                                    id="h-description"
                                    v-model="role.description"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row align-h="center">
                        <b-col md="6">
                            <b-form-group
                                :label="$t('System.Role.Field.Status')"
                                label-for="h-status"
                                label-cols-md="4"
                            >
                                <b-form-select
                                    v-model="role.status"
                                    :options="rechangeOptions(options)"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-if="editing && authorize(['ManageRole'])"
                                type="button"
                                variant="primary"
                                class="mx-50 mb-50 btn-120"
                                @click="validationForm"
                            >
                                {{ $t('Button.Save') }}
                            </b-button>
                            <b-button
                                v-if="!editing && authorize(['ManageRole'])"
                                type="button"
                                variant="primary"
                                class="mx-50 mb-50 btn-120"
                                @click="edit"
                                >{{ $t('Button.Edit') }}
                            </b-button>
                            <b-button
                                v-if="!editing"
                                :to="{ path: '/systems/roles/list' }"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                            >
                                {{ $t('Button.Back') }}
                            </b-button>
                            <b-button
                                v-if="editing"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                                @click="cancel"
                                >{{ $t('Button.Cancel') }}</b-button
                            >
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    mixins: [authorizationMixin],
    components: {},
    data() {
        return {
            role: {
                id: null,
                name: null,
                description: null,
            },
            editing: false,
            options: [
                { value: 1, text: 'StatusList.Active' },
                { value: 0, text: 'StatusList.Inactive' },
            ],
        }
    },
    computed: {
        roleId() {
            return this.$route.params.roleId
        },
    },
    created() {
        this.loadRoleDetail()
    },
    methods: {
        rechangeOptions(dataList) {
            return dataList?.map((item) => ({
                ...item,
                text: this.$t(item.text),
            }))
        },
        loadRoleDetail() {
            this.$services.get(`/roles/${this.roleId}`).then((response) => {
                this.role = response.data
            })
        },
        validationForm() {
            this.role.name = this.role.name ? this.role.name.trim() : null
            this.role.description = this.role.description
                ? this.role.description.trim()
                : null
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .put(`/roles/${this.roleId}`, this.role)
                        .then((response) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('System.Role.Message.UpdateSuccess'),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                            this.cancel()
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('System.Role.Message.UpdateFailure'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${error.response.data.error}`,
                                },
                            })
                        })
                }
            })
        },
        edit() {
            this.editing = true
        },
        cancel() {
            this.editing = false
            this.loadRoleDetail()
        },
    },
}
</script>

<style lang="scss"></style>
