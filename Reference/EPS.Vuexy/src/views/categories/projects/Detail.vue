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
                                        :disabled="!editing"
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
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col>
                            <div class="text-center">
                                <b-button
                                    v-if="
                                        editing && authorize(['ManageProjects'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="validationForm"
                                >
                                    {{ this.$t('Button.Save') }}
                                </b-button>
                                <b-button
                                    v-if="
                                        !editing && authorize(['ManageProjects'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="edit"
                                    >{{ this.$t('Button.Edit') }}</b-button
                                >
                                <b-button
                                    v-if="!editing"
                                    :to="{ path: '/issue/projects/list' }"
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary"
                                >
                                    {{ this.$t('Button.Back') }}
                                </b-button>
                                <b-button
                                    v-if="editing"
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary"
                                    @click="cancel"
                                    >{{ this.$t('Button.Cancel') }}</b-button
                                >
                            </div>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>

<script>
/* eslint-disable */
// import LatLngPicker from '@/components/LatLngPicker'
// import LatLngPickerImage from '@/components/LatLngPickerImage'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'

export default {
    mixins: [authorizationMixin],
    // components: { LatLngPicker, LatLngPickerImage },
    data() {
        return {
            project: {
                code: null,
                name: null,
                compId: null,
            },
            editing: false,
        }
    },
    computed: {
        projectId() {
            return this.$route.params.projectId
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.project.compId = accessToken.companyId
        this.loadProjectDetail()
    },
    methods: {
        loadProjectDetail() {
            this.$services.get(`/project/${this.projectId}`).then((response) => {
                this.project = {
                    ...response.data.data,
                }
            })
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    // Dọn dẹp dữ liệu
                    this.project.code = this.trimField(this.project.code)
                    this.project.name = this.trimField(this.project.name)

                    // Tạo một đối tượng FormData mới để gửi dữ liệu và file
                    const formData = new FormData()

                    // Thêm tất cả các trường dữ liệu của thiết bị vào FormData
                    for (const key in this.project) {
                        if (Object.prototype.hasOwnProperty.call(
                                this.project,key)) {
                                if (this.project[key] !== null) {
                                    formData.append(key, this.project[key])
                                }
                        }
                    }

                    this.$services
                        .put(`/project/${this.projectId}`, formData, {
                            headers: {
                                'Content-Type': 'multipart/form-data',
                            },
                        })
                        .then(() => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Update'),
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
        edit() {
            this.editing = true
        },
        cancel() {
            this.editing = false
            this.loadProjectDetail()
        },
    }
}
</script>

<style>

</style>