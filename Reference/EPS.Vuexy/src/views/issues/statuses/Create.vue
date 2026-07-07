<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                        <b-row>
                            <b-col md="4"></b-col>
                            <b-col md="4">
                                <b-form-group
                                    :label="this.$t('Statuses.Detail.Form.StatusId')"
                                    label-for="h-status-statusId"
                                    label-cols-md="4"
                                    label-class="required"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required|noSpecialCharsExceptUnderscore"
                                        :name="this.$t('Statuses.Detail.Form.StatusId')"
                                    >
                                        <b-form-input
                                            id="h-status-statusId"
                                            @keypress="onlyNumber"
                                            type="text"
                                            maxlength="20" 
                                            onpaste="return false"
                                            v-model="status.id"
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
                                    :label="this.$t('Statuses.Detail.Form.Name')"
                                    label-for="h-status-name"
                                    label-cols-md="4"
                                    label-class="required"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required"
                                        :name="this.$t('Statuses.Detail.Form.Name')"
                                    >
                                        <b-form-input
                                            id="h-status-name"
                                            v-model="status.name"
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
                                    :label="this.$t('Statuses.Detail.Form.Type')"
                                    label-for="h-status-type"
                                    label-cols-md="4"
                                    label-class="required"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required"
                                        :name="this.$t('Statuses.Detail.Form.Type')"
                                    >
                                        <v-select
                                            v-model="status.type"
                                            label="text"
                                            :options="ListStatus"
                                            :reduce="(issueType) => issueType.i18nKey"
                                            >
                                                <!-- option hiển thị trong dropdown -->
                                                <template #option="{ i18nKey }">
                                                    {{ $t(i18nKey) }}
                                                </template>

                                                <!-- option đã chọn hiển thị trên input -->
                                                <template #selected-option="{ i18nKey }">
                                                    {{ $t(i18nKey) }}
                                                </template>
                                        </v-select>
                                        <small class="text-danger">{{
                                            errors[0]
                                        }}</small>
                                    </validation-provider>
                                </b-form-group>

                                <!-- Nhập mã màu -->
                                <b-form-group
                                    :label="this.$t('Statuses.Detail.Form.Color')"
                                    label-for="h-status-color"
                                    label-cols-md="4"
                                    label-class="required"
                                    >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required"
                                        :name="this.$t('Statuses.Detail.Form.Color')"
                                    >
                                        <div class="d-flex align-items-center">
                                            <!-- input text để nhập mã màu -->
                                            <b-form-input
                                                id="h-status-color"
                                                v-model="status.color"
                                                placeholder="#"
                                                :state="errors.length > 0 ? false : null"
                                            />

                                            <!-- input type="color" ẩn đi, để trigger khi click span -->
                                            <input
                                                type="color"
                                                v-model="status.color"
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
                                                backgroundColor: status.color || '#ccc',
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
                                    v-if="authorize(['ManageStatuses'])"
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
                                    :to="{ path: '/issue/statuses/list' }"
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
            status: {
                id: null,
                copmId: null,
                name: null,
                type: null,
                color: '',
            },
            ListStatus: [
                { i18nKey: 'Statuses.List.ListStatus.ToDo' },
                { i18nKey: 'Statuses.List.ListStatus.Inprogress' },
                { i18nKey: 'Statuses.List.ListStatus.Done' },
            ],
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.status.compId = accessToken.companyId
    },
    methods: {
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.status.id = this.status.id
                    this.status.name = this.trimField(this.status.name)
                    this.status.type = this.status.type
                    this.status.color = this.trimField(this.status.color)

                    // Tạo một đối tượng FormData mới để gửi dữ liệu và file
                    const formData = new FormData()

                    // Thêm tất cả các trường dữ liệu của thiết bị vào FormData
                    for (const key in this.status) {
                        if (
                            Object.prototype.hasOwnProperty.call(
                                this.status,
                                key
                            )
                        ) {
                            const value = this.status[key]
                            // Bỏ qua các field null hoặc undefined
                            if (value !== null && value !== undefined) {
                                formData.append(key, value)
                            }
                        }
                    }

                    this.$services
                        .post('/status', formData, {
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
                                path: '/issue/statuses/list',
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