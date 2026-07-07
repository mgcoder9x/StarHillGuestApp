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
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-if="authorize(['ManageRole'])"
                                type="submit"
                                variant="primary"
                                class="mr-1"
                                @click.prevent="validationForm"
                            >
                                {{ $t('Button.Save') }}
                            </b-button>
                            <b-button
                                :to="{ path: '/systems/roles/list' }"
                                type="reset"
                                variant="outline-secondary"
                            >
                                {{ $t('Button.Cancel') }}
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

export default {
    mixins: [authorizationMixin],
    components: {},
    data() {
        return {
            role: {
                status: 1,
                name: null,
                description: null,
            },
            selected: null,
            options: [
                { value: 1, text: 'StatusList.Active' },
                { value: 0, text: 'StatusList.Inactive' },
            ],
        }
    },
    methods: {
        rechangeOptions(dataList) {
            return dataList?.map((item) => ({
                ...item,
                text: this.$t(item.text),
            }))
        },
        validationForm() {
            this.role.name = this.role.name ? this.role.name.trim() : null
            this.role.description = this.role.description
                ? this.role.description.trim()
                : null
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .post('/roles/', this.role)
                        .then((response) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('System.Role.Message.CreateSuccess'),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                            this.$router.push({ path: '/systems/roles/list' })
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('System.Role.Message.CreateFailure'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${error.response.data.error}`,
                                },
                            })
                        })
                }
            })
        },
    },
}
</script>
