<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
                        <b-col md="4"></b-col>
                            <b-col md="4">
                            <b-form-group
                                :label="this.$t('Priority.Detail.Form.PriorityId')"
                                label-for="h-priority-priorityId"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    :name="this.$t('Priority.Detail.Form.PriorityId')"
                                >
                                    <b-form-input
                                        id="h-priority-priorityId"
                                        v-model="priority.id"
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
                                :label="this.$t('Priority.Detail.Form.Name')"
                                label-for="h-priority-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="this.$t('Priority.Detail.Form.Name')"
                                >
                                    <b-form-input
                                        id="h-priority-name"
                                        v-model="priority.name"
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
                            :label="this.$t('Priority.Detail.Form.StatusType')"
                            label-for="h-priority-statusType"
                            label-cols-md="4"
                            label-class="required"
                            >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="this.$t('Priority.Detail.Form.StatusType')"
                            >
                                <b-form-input
                                id="h-priority-statusType"
                                v-model="priority.statusType"
                                :state="errors.length > 0 ? false : null"
                                :disabled="!editing"
                                />
                                <small class="text-danger">{{ errors[0] }}</small>
                            </validation-provider>
                            </b-form-group> -->

                            <!-- Nhập mã màu -->
                            <b-form-group
                                :label="this.$t('Priority.Detail.Form.Color')"
                                label-for="h-priority-color"
                                label-cols-md="4"
                                label-class="required"
                                >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="this.$t('Priority.Detail.Form.Color')"
                                >
                                    <div class="d-flex align-items-center">
                                    <!-- input text để nhập mã màu -->
                                    <b-form-input
                                        id="h-priority-color"
                                        v-model="priority.color"
                                        placeholder="#"
                                        :state="errors.length > 0 ? false : null"
                                        :disabled="!editing"
                                    />

                                    <!-- input type="color" ẩn đi, để trigger khi click span -->
                                    <input
                                        type="color"
                                        v-model="priority.color"
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
                                        backgroundColor: priority.color || '#ccc',
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
                                        editing && authorize(['ManagePriority'])
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
                                        !editing && authorize(['ManagePriority'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="edit"
                                    >{{ this.$t('Button.Edit') }}</b-button
                                >
                                <b-button
                                    v-if="!editing"
                                    :to="{ path: '/issue/priority/list' }"
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
            priority: {
                id: null,
                name: null,
                compId: null,
                color: '',
            },
            editing: false,
        }
    },
    computed: {
        priorityId() {
            return this.$route.params.priorityId
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.priority.compId = accessToken.companyId
        this.loadPriorityDetail()
    },
    methods: {
        loadPriorityDetail() {
            this.$services.get(`/priority/${this.priorityId}`).then((response) => {
                this.priority = {
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
                    this.priority.id = this.priority.id
                    this.priority.name = this.trimField(this.priority.name)
                    this.priority.color = this.trimField(this.priority.color)

                    // Tạo một đối tượng FormData mới để gửi dữ liệu và file
                    const formData = new FormData()

                    // Thêm tất cả các trường dữ liệu của thiết bị vào FormData
                    for (const key in this.priority) {
                        if (Object.prototype.hasOwnProperty.call(
                                this.priority,key)) {
                                if (this.priority[key] !== null) {
                                    formData.append(key, this.priority[key])
                                }
                        }
                    }

                    this.$services
                        .put(`/priority/${this.priorityId}`, formData, {
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
            this.loadPriorityDetail()
        },
    }
}
</script>

<style>

</style>