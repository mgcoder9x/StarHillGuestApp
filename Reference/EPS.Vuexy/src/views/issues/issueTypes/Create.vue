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
                                            @keypress="onlyNumber"
                                            type="text"
                                            maxlength="20" 
                                            onpaste="return false"
                                            v-model="issueType.id"
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
                                            id="h-issueTypees-name"
                                            v-model="issueType.name"
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
                                        />

                                        <!-- input type="color" ẩn đi, để trigger khi click span -->
                                        <input
                                            type="color"
                                            v-model="issueType.color"
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
                            <b-col class="text-center">
                                <b-button
                                    v-if="authorize(['ManageIssueType'])"
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
                                    :to="{ path: '/issue/issueTypes/list' }"
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
            issueType: {
                id: null,
                copmId: null,
                name: null,
                color: '',
            },
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.issueType.compId = accessToken.companyId

    },
    methods: {
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.issueType.id = this.trimField(this.issueType.id)
                    this.issueType.name = this.trimField(this.issueType.name)
                    this.issueType.color = this.trimField(this.issueType.color)

                    // Tạo một đối tượng FormData mới để gửi dữ liệu và file
                    const formData = new FormData()

                    // Thêm tất cả các trường dữ liệu của thiết bị vào FormData
                    for (const key in this.issueType) {
                        if (
                            Object.prototype.hasOwnProperty.call(
                                this.issueType,
                                key
                            )
                        ) {
                            const value = this.issueType[key]
                            // Bỏ qua các field null hoặc undefined
                            if (value !== null && value !== undefined) {
                                formData.append(key, value)
                            }
                        }
                    }

                    this.$services
                        .post('/issueType', formData, {
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
                                path: '/issue/issueTypes/list',
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

<style>

</style>