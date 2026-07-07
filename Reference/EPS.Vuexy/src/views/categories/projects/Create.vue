<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                        <b-row>
                            <b-col md="4"></b-col>
                            <b-col md="4">
                                <b-form-group
                                    :label="this.$t('Project.Detail.Form.Code')"
                                    label-for="h-project-code"
                                    label-cols-md="4"
                                    label-class="required"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required|noSpecialCharsExceptUnderscore"
                                        :name="this.$t('Project.Detail.Form.Code')"
                                    >
                                        <b-form-input
                                            id="h-project-code"
                                            v-model="project.code"
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

                        <b-row>
                            <b-col md="4"></b-col>
                            <b-col md="4">
                                <b-form-group
                                    :label="this.$t('Project.Detail.Form.Name')"
                                    label-for="h-project-name"
                                    label-cols-md="4"
                                    label-class="required"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required"
                                        :name="this.$t('Project.Detail.Form.Name')"
                                    >
                                        <b-form-input
                                            id="h-project-name"
                                            v-model="project.name"
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
                        <b-row>
                            <b-col class="text-center">
                                <b-button
                                    v-if="authorize(['ManageProjects'])"
                                    type="submit"
                                    variant="primary"
                                    class="mr-1"
                                    @click.prevent="validationForm"
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
                                    :to="{ path: '/issue/projects/list' }"
                                    type="reset"
                                    variant="outline-secondary"
                                >
                                    <Icon icon="mdi:cancel" class="sm-icon" />
                                    <span class="ml-50">
                                        {{ $t('common.button.cancel') }}
                                    </span>
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
    data() {
        return {
            project: {
                code: null,
                name: null,
                compId: null,
            },
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.project.compId = accessToken.companyId
    },
    methods: {
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.project.code = this.trimField(this.project.code)
                    this.project.name = this.trimField(this.project.name)

                    // Tạo một đối tượng FormData mới để gửi dữ liệu và file
                    const formData = new FormData()

                    // Thêm tất cả các trường dữ liệu của thiết bị vào FormData
                    for (const key in this.project) {
                        if (
                            Object.prototype.hasOwnProperty.call(
                                this.project,
                                key
                            )
                        ) {
                            const value = this.project[key]
                            // Bỏ qua các field null hoặc undefined
                            if (value !== null && value !== undefined) {
                                formData.append(key, value)
                            }
                        }
                    }

                    this.$services
                        .post('/project', formData, {
                            headers: {
                                'Content-Type': 'multipart/form-data',
                            },
                        })
                        .then((response) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Create'),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                            this.$router.push({
                                path: '/issue/projects/list',
                            })
                        })
                        .catch((error) => {
                            debugger
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${this.$t(error.message)}`,
                                },
                            })
                        })
                }
            })
        },
    },
}
</script>

<style>

</style>