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
                                            @keypress="onlyNumber"
                                            type="text"
                                            maxlength="20" 
                                            onpaste="return false"
                                            v-model="priority.id"
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
                                        />

                                        <!-- input type="color" ẩn đi, để trigger khi click span -->
                                        <input
                                            type="color"
                                            v-model="priority.color"
                                            ref="colorPicker"
                                            style="opacity:0; width:1px; height:1px; position:absolute; right:-1px;"
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
                            <b-col class="text-center">
                                <b-button
                                    v-if="authorize(['ManagePriority'])"
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
                                    :to="{ path: '/issue/priority/list' }"
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
            priority: {
                id: null,
                name: '',
                compId : null,
                color: '',
            },
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.priority.compId = accessToken.companyId

    },
    methods: {
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.priority.id = this.priority.id
                    this.priority.name = this.trimField(this.priority.name)
                    this.priority.color = this.trimField(this.priority.color)
                    

                    // Tạo một đối tượng FormData mới để gửi dữ liệu và file
                    const formData = new FormData()

                    // Thêm tất cả các trường dữ liệu của thiết bị vào FormData
                    for (const key in this.priority) {
                        if (
                            Object.prototype.hasOwnProperty.call(
                                this.priority,
                                key
                            )
                        ) {
                            const value = this.priority[key]
                            // Bỏ qua các field null hoặc undefined
                            if (value !== null && value !== undefined) {
                                formData.append(key, value)
                            }
                        }
                    }

                    this.$services
                        .post('/priority', formData, {
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
                                path: '/issue/priority/list',
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
        onlyNumber (event) {
            const charCode = event.which ? event.which : event.keyCode
            if (charCode < 48 || charCode > 57) {
                event.preventDefault()
            }
        },
    },
}
</script>

<style scoped>

</style>