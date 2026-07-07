<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
                        <b-col md="4"></b-col>
                            <b-col md="4">
                            <b-form-group
                                :label="this.$t('IssueTypes.Detail.Form.IssueTypeId')"
                                label-for="h-issueType-issueTypeId"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    :name="this.$t('IssueTypes.Detail.Form.IssueTypeId')"
                                >
                                    <b-form-input
                                        id="h-issueType-issueTypeId"
                                        v-model="issueType.id"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :disabled="!editing & editing"
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
                                :label="this.$t('IssueTypes.Detail.Form.Name')"
                                label-for="h-issueType-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="this.$t('IssueTypes.Detail.Form.Name')"
                                >
                                    <b-form-input
                                        id="h-issueType-name"
                                        v-model="issueType.name"
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
                            <!-- Nhập tên
                            <b-form-group
                            :label="this.$t('IssueTypes.Detail.Form.StatusType')"
                            label-for="h-issueType-statusType"
                            label-cols-md="4"
                            label-class="required"
                            >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="this.$t('IssueTypes.Detail.Form.StatusType')"
                            >
                                <b-form-input
                                id="h-issueType-statusType"
                                v-model="issueType.statusType"
                                :state="errors.length > 0 ? false : null"
                                :disabled="!editing"
                                />
                                <small class="text-danger">{{ errors[0] }}</small>
                            </validation-provider>
                            </b-form-group> -->

                            <!-- Nhập mã màu -->
                            <b-form-group
                                :label="this.$t('IssueTypes.Detail.Form.Color')"
                                label-for="h-issueType-color"
                                label-cols-md="4"
                                label-class="required"
                                >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="this.$t('IssueTypes.Detail.Form.Color')"
                                >
                                    <div class="d-flex align-items-center">
                                    <!-- input text để nhập mã màu -->
                                    <b-form-input
                                        id="h-issueType-color"
                                        v-model="issueType.color"
                                        placeholder="#"
                                        :state="errors.length > 0 ? false : null"
                                        :disabled="!editing"
                                    />

                                    <!-- input type="color" ẩn đi, để trigger khi click span -->
                                    <input
                                        type="color"
                                        v-model="issueType.color"
                                        ref="colorPicker"
                                        style="opacity:0; width:1px; height:1px; position:absolute; right:-1px;"
                                        :disabled="!editing"
                                    />

                                    <!-- ô review màu -->
                                    <span
                                        class="ml-2"
                                        :style="{
                                        display: 'inline-block',
                                        width: '40px',
                                        height: '20px',
                                        backgroundColor: issueType.color || '#ccc',
                                        borderRadius: '5px',
                                        cursor: 'pointer'
                                        }"
                                        @click="$refs.colorPicker.click()"
                                    ></span>
                                    </div>

                                    <small class="text-danger">{{ errors[0] }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>


                    <b-row>
                        <b-col>
                            <div class="text-center">
                                <b-button
                                    v-if="
                                        editing && authorize(['ManageIssueType'])
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
                                        !editing && authorize(['ManageIssueType'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="edit"
                                    >{{ this.$t('Button.Edit') }}</b-button
                                >
                                <b-button
                                    v-if="!editing"
                                    :to="{ path: '/issue/issueTypes/list' }"
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
            issueType: {
                id: null,
                name: null,
                compId: null,
            },
            editing: false,
        }
    },
    computed: {
        issueTypeId() {
            return this.$route.params.issueTypeId
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.issueType.compId = accessToken.companyId
        this.loadIssueTypeDetail()
    },
    methods: {
        loadIssueTypeDetail() {
            this.$services.get(`/issueType/${this.issueTypeId}`).then((response) => {
                this.issueType = {
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
                    this.issueType.id = this.issueType.id
                    this.issueType.name = this.trimField(this.issueType.name)
                    this.issueType.color = this.trimField(this.issueType.color)

                    // Tạo một đối tượng FormData mới để gửi dữ liệu và file
                    const formData = new FormData()

                    // Thêm tất cả các trường dữ liệu của thiết bị vào FormData
                    for (const key in this.issueType) {
                        if (Object.prototype.hasOwnProperty.call(
                                this.issueType,key)) {
                                if (this.issueType[key] !== null) {
                                    formData.append(key, this.issueType[key])
                                }
                        }
                    }

                    this.$services
                        .put(`/issueType/${this.issueTypeId}`, formData, {
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
            this.loadIssueTypeDetail()
        },
    }
}
</script>

<style>

</style>