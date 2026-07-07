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
                                        v-model="status.id"
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
                                :label="
                                    $t(
                                        'Statuses.Detail.Form.Type'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                                >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'Statuses.Detail.Form.Type'
                                        )
                                    "
                                    >
                                    <v-select
                                        id="h-status-type"
                                        v-model="status.type"
                                        :reduce="(issueType) => issueType.i18nKey"
                                        :options="ListStatus"
                                        label="text"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :disabled="!editing"
                                        >
                                            <template #option="{ i18nKey }">
                                                {{ $t(i18nKey) }}
                                            </template>

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
                                        :disabled="!editing"
                                    />

                                    <!-- input type="color" ẩn đi, để trigger khi click span -->
                                    <input
                                        type="color"
                                        v-model="status.color"
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
                        <b-col>
                            <div class="text-center">
                                <b-button
                                    v-if="
                                        editing && authorize(['ManageStatuses'])
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
                                        !editing && authorize(['ManageStatuses'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="edit"
                                    >{{ this.$t('Button.Edit') }}</b-button
                                >
                                <b-button
                                    v-if="!editing"
                                    :to="{ path: '/issue/statuses/list' }"
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
            status: {
                id: null,
                name: null,
                compId: null,
                type:null,
                color:'',
            },
            ListStatus: [
                { i18nKey: 'Statuses.List.ListStatus.ToDo' },
                { i18nKey: 'Statuses.List.ListStatus.Inprogress'},
                { i18nKey: 'Statuses.List.ListStatus.Done'},
            ],
            editing: false,
        }
    },
    computed: {
        statusId() {
            return this.$route.params.statusId
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.status.compId = accessToken.companyId
        this.loadStatusDetail()
    },
    methods: {
        loadStatusDetail() {
            this.$services.get(`/status/${this.statusId}`).then((response) => {
                this.status = {
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
                    this.status.id = this.status.id
                    this.status.name = this.trimField(this.status.name)
                    this.status.type = this.status.type
                    this.status.color = this.trimField(this.status.color)

                    // Tạo một đối tượng FormData mới để gửi dữ liệu và file
                    const formData = new FormData()

                    // Thêm tất cả các trường dữ liệu của thiết bị vào FormData
                    for (const key in this.status) {
                        if (Object.prototype.hasOwnProperty.call(
                                this.status,key)) {
                                if (this.status[key] !== null) {
                                    formData.append(key, this.status[key])
                                }
                        }
                    }

                    this.$services
                        .put(`/status/${this.statusId}`, formData, {
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
            this.loadStatusDetail()
        },
    }
}
</script>

<style>

</style>