<!-- eslint-disable -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <div class="d-flex flex-column align-items-end mb-2">
                        <!-- <CAlert
                            closeButton
                            :color="infoCardReder.status"
                            fade
                            style="margin-left: auto; font-size: 12px"
                        >
                            {{ infoCardReder.text }}
                        </CAlert> -->
                        <div
                            v-if="infoCardReder.text"
                            :class="[
                                'alert',
                                'alert-dismissible',
                                'fade',
                                'show',
                                alertClass(infoCardReder.status),
                            ]"
                            style="
                                margin-left: 10px;
                                font-size: 12px;
                                padding: 10px;
                            "
                            role="alert"
                            aria-live="polite"
                            aria-atomic="true"
                        >
                            <!-- <button type="button" class="close" aria-label="Close" @click="infoCardReder.text = null">
                              ×
                            </button> -->
                            {{ $t(infoCardReder.text) }}
                        </div>

                        <b-button
                            style="margin-left: 10px"
                            variant="success"
                            @click="readQrCode"
                        >
                            <i class="fa fa-qrcode"></i>
                            {{
                                $t(
                                    'categories.employees.common.form.label.readQRCode'
                                )
                            }}
                        </b-button>
                    </div>
                    <b-row>
                        <!-- Start: input Img -->
                        <b-col md="3">
                            <b-img
                                v-if="newEmployee.avatarBase64"
                                :src="newEmployee.avatarBase64"
                                class="mb-1 avatar-img"
                                alt="Avatar"
                            />
                            <b-form-file
                                accept="image/*"
                                :placeholder="
                                    $t(
                                        'categories.employees.common.form.label.avatarPath'
                                    )
                                "
                                drop-placeholder="Kéo file vào đây"
                                @change="handleFileUpload"
                            />
                            <small class="text-danger">{{ fileError }}</small>
                        </b-col>
                        <!-- End: input Img -->
                        <!-- Start: input Data -->
                        <b-col md="9">
                            <b-row>
                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.compName'
                                            )
                                        "
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="
                                                $t(
                                                    'categories.employees.common.form.label.compName'
                                                )
                                            "
                                        >
                                            <tree-select
                                                v-model="newEmployee.compId"
                                                :options="options.compTree"
                                                label="text"
                                                :reduce="(option) => option.id"
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.compName'
                                                    )
                                                "
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>

                                <!-- <b-col md="6">
                                    <b-form-group :label="
                                            $t(
                                                'categories.employees.common.form.label.depName'
                                            )
                                        " label-cols-md="4" label-class="required" :class="formGroupClass">
                                        <validation-provider #default="{ errors }" rules="required" :name="
                                                $t(
                                                    'categories.employees.common.form.label.depName'
                                                )
                                            ">
                                            <tree-select v-model="newEmployee.depId" :options="
                                                    options.departmentTree
                                                " label="text" :reduce="(option) => option.id" :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.depName'
                                                    )
                                                " />
                                            <small class="text-danger">{{
                                                errors[0]
                                                }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col> -->

                                <!-- <b-col md="6">
                                    <b-form-group :label="
                                            $t(
                                                'categories.employees.common.form.label.groupName'
                                            )
                                        " label-cols-md="4" label-class="required" :class="formGroupClass">
                                        <validation-provider #default="{ errors }" rules="required" :name="
                                                $t(
                                                    'categories.employees.common.form.label.groupName'
                                                )
                                            ">
                                            <v-select v-model="
                                                    newEmployee.listGroupId
                                                " :options="options.groups" :multiple="true" :reduce="
                                                    (item) => parseInt(item.id)
                                                " label="text" :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.groupName'
                                                    )
                                                " />
                                            <small class="text-danger">{{
                                                errors[0]
                                                }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col> -->

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.code'
                                            )
                                        "
                                        label-for="h-code"
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="
                                                $t(
                                                    'categories.employees.common.form.label.code'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-code"
                                                v-model.trim="newEmployee.code"
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.code'
                                                    )
                                                "
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.citizenId'
                                            )
                                        "
                                        label-for="h-citizenId"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            :rules="{
                                                regex: /^\d+$/,
                                            }"
                                            :name="
                                                $t(
                                                    'categories.employees.common.form.label.citizenId'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-citizenId"
                                                v-model.trim="newEmployee.citizenId"
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.citizenId'
                                                    )
                                                "
                                                :state="
                                                    errors.length > 0
                                                        ? false
                                                        : null
                                                "
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.blacklist'
                                            )
                                        "
                                        label-for="h-fullname"
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="
                                                $t(
                                                    'categories.employees.common.form.label.blacklist'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-fullname"
                                                v-model.trim="
                                                    newEmployee.fullname
                                                "
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.blacklist'
                                                    )
                                                "
                                                :state="
                                                    errors.length ? false : null
                                                "
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>

                                <!-- <b-col md="6">
                                    <b-form-group :label="
                                            $t(
                                                'categories.employees.common.form.label.jobDuties'
                                            )
                                        " label-for="h-jobDuties" label-cols-md="4" :class="formGroupClass">
                                        <b-form-input id="h-jobDuties" v-model.trim="newEmployee.jobDuties" :placeholder="
                                                $t(
                                                    'categories.employees.common.form.placeholder.jobDuties'
                                                )
                                            " />
                                    </b-form-group>
                                </b-col> -->

                                <!-- <b-col md="6">
                                    <b-form-group :label="
                                            $t(
                                                'categories.employees.common.form.label.position'
                                            )
                                        " label-for="h-position" label-cols-md="4" :class="formGroupClass">
                                        <b-form-input id="h-position" v-model.trim="newEmployee.position" :placeholder="
                                                $t(
                                                    'categories.employees.common.form.placeholder.position'
                                                )
                                            " />
                                    </b-form-group>
                                </b-col> -->

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.birthday'
                                            )
                                        "
                                        label-for="h-birthday"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <b-form-datepicker
                                            id="h-birthday"
                                            v-model="newEmployee.birthday"
                                            :placeholder="
                                                $t(
                                                    'categories.employees.common.form.placeholder.birthday'
                                                )
                                            "
                                            :date-format-options="{
                                                day: 'numeric',
                                                month: 'long',
                                                year: 'numeric',
                                            }"
                                            :locale="this.$i18n.locale"
                                        />
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.gender'
                                            )
                                        "
                                        label-for="h-gender"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <v-select
                                            id="h-gender"
                                            v-model="newEmployee.gender"
                                            :options="
                                                rechangeOptions(options.gender)
                                            "
                                            :reduce="(option) => option.value"
                                            :placeholder="
                                                $t(
                                                    'categories.employees.common.form.placeholder.gender'
                                                )
                                            "
                                        />
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.phoneNumber'
                                            )
                                        "
                                        label-for="h-phoneNumber"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            :rules="{
                                                regex: /^\d{10,11}$/,
                                            }"
                                            :name="
                                                $t(
                                                    'categories.employees.common.form.label.phoneNumber'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-phoneNumber"
                                                v-model.trim="
                                                    newEmployee.phoneNumber
                                                "
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.phoneNumber'
                                                    )
                                                "
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>
                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.address'
                                            )
                                        "
                                        label-for="h-address"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <b-form-input
                                            id="h-address"
                                            v-model.trim="newEmployee.address"
                                            :placeholder="
                                                $t(
                                                    'categories.employees.common.form.placeholder.address'
                                                )
                                            "
                                        />
                                    </b-form-group>
                                </b-col>
                                <!-- <b-col md="6">
                                    <b-form-group :label="$t(
                                        'categories.employees.common.form.label.email'
                                    )
                                        " label-for="h-email" label-cols-md="4" :class="formGroupClass">
                                        <validation-provider #default="{ errors }" :rules="{
                                            regex: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/,
                                        }" :name="$t(
                                            'categories.employees.common.form.label.email'
                                        )
                                            ">
                                            <b-form-input id="h-email" v-model.trim="newEmployee.email" :placeholder="$t(
                                                'categories.employees.common.form.placeholder.email'
                                            )
                                                " />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col> -->
                            </b-row>
                        </b-col>
                        <!-- End: input Data -->
                    </b-row>
                    <!-- Button Action -->
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageEmployee'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            :disabled="isSubmitting"
                            class="mx-50 mb-50 btn-120"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button
                            @click="navigateToList()"
                            type="reset"
                            variant="outline-secondary"
                            title="Cancel"
                            class="btn-120 mb-50"
                        >
                            {{ $t('common.button.cancel') }}
                        </b-button>
                    </div>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>
<script>
/*eslint-disable*/

import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import TreeHelper from '@/utils/treeHelper'
import CardReaderSocket from '@/utils/cardReaderSocket'
import moment from 'moment'

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            isSubmitting: false,
            infoCardReder: {
                text: 'categories.employees.readIdentification.noDevices',
                status: 'danger',
            },
            options: {
                compTree: [],
                groups: [],
                departmentTree: [],
                areaTree: null,
                gender: [
                    { value: 0, label: 'categories.employees.gender.female' },
                    { value: 1, label: 'categories.employees.gender.male' },
                ],
            },
            newEmployee: {
                compId: null,
                depId: null,
                groupId: null,
                code: null,
                fullname: null,
                jobDuties: null,
                position: null,
                birthday: null,
                gender: null,
                phoneNumber: null,
                email: null,
                avatarBase64: null,
                citizenId: null,
                listGroupId: null,
                personType: 4, //Gán mặc định giá trị black list
            },

            fileError: null,
        }
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
    },
    watch: {
        '$i18n.locale'(newLocale) {
            if (this.fileError) {
                this.fileError = this.$t('Blacklist.Error.InvalidFormat')
            }
        },
    },
    async mounted() {
        await this.connectToCardReader()
    },
    async created() {
        await this.loadOptions()
    },
    methods: {
        rechangeOptions(dataList) {
            return dataList?.map((item) => ({
                ...item,
                label: this.$t(item.label),
            }))
        },
        async connectToCardReader() {
            await CardReaderSocket.connectSocket((data) => {
                this.getStatus()
                if (data.type == 'card' || data.type == 'qrCode') {
                    this.fillPersonInfo(data)
                }
            })
            this.getStatus()
        },
        fillPersonInfo(data) {
            this.newEmployee.citizenId = data.id
            if (data.birth != null) {
                const parts = data.birth.split('/')
                if (parts.length === 3) {
                    const day = parts[0]
                    const month = parts[1]
                    const year = parts[2]
                    var date = new Date(`${year}-${month}-${day}`)
                    this.newEmployee.birthday =
                        moment(date).format('YYYY-MM-DD')
                }
            }
            this.newEmployee.fullname = data.name
            this.newEmployee.gender = data.gender == 'Nam' ? 1 : 0
            //this.person.Address = data.address;
            // this.newEmployee.avatarBase64 = data.base64;
            this.newEmployee.avatarBase64 = data.base64
                ? `data:image/png;base64,${data.base64}`
                : null
        },
        getStatus() {
            this.infoCardReder.text =
                CardReaderSocket.isConnected && CardReaderSocket.deviceName
                    ? this.$t(
                          'categories.employees.readIdentification.deviceConnect'
                      ) + `${CardReaderSocket.deviceName}`
                    : 'categories.employees.readIdentification.noDevices'
            this.infoCardReder.status =
                CardReaderSocket.isConnected && CardReaderSocket.deviceName
                    ? `success`
                    : 'danger'
        },
        readQrCode() {
            CardReaderSocket.ingestEvent('get_qr')
        },
        alertClass(status) {
            // Map từ status sang class bootstrap tương ứng
            switch (status) {
                case 'success':
                case 'green':
                    return 'alert-success'
                case 'danger':
                case 'red':
                    return 'alert-danger'
                case 'warning':
                case 'yellow':
                    return 'alert-warning'
                case 'info':
                case 'blue':
                default:
                    return 'alert-info'
            }
        },
        handleFileUpload(event) {
            const file = event.target.files[0]
            if (file) {
                if (!file.type.startsWith('image/')) {
                    this.fileError = this.$t('Blacklist.Error.InvalidFormat')
                    this.newEmployee.avatarBase64 = null
                    return
                }
                this.fileError = null
                const reader = new FileReader()
                reader.onload = (e) => {
                    const base64String = e.target.result
                    if (!base64String.startsWith('data:')) {
                        const mimeType = file.type || 'image/png'
                        this.newEmployee.avatarBase64 = `data:${mimeType};base64,${base64String}`
                    } else {
                        this.newEmployee.avatarBase64 = base64String
                    }
                }
                reader.onerror = () => {
                    this.fileError = 'Không thể đọc file, vui lòng thử lại.'
                }
                reader.readAsDataURL(file)
            } else {
                this.newEmployee.avatarBase64 = ''
                this.fileError = null
            }
        },
        async loadOptions() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.options.compTree = TreeHelper.removeEmptyChildren(
                    response.data
                )
            })
            this.$services.get('/lookup/groups').then((response) => {
                this.options.groups = response.data.data
            })
            this.$services
                .get('/lookup/departments-tree?type=1')
                .then((response) => {
                    this.options.departmentTree = response.data.data
                })
            const response = await this.$services.get('/lookup/areas-tree')
            this.options.areaTree = TreeHelper.removeEmptyChildren(
                response.data.data
            )
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        async onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                if (this.fileError) {
                    return
                }
                if (success) {
                    this.isSubmitting = true
                    try {
                        this.newEmployee.code = this.trimField(
                            this.newEmployee.code
                        )
                        this.newEmployee.fullname = this.trimField(
                            this.newEmployee.fullname
                        )
                        this.newEmployee.citizenId = this.trimField(
                            this.newEmployee.citizenId
                        )
                        this.newEmployee.phoneNumber = this.trimField(
                            this.newEmployee.phoneNumber
                        )
                        this.newEmployee.email = this.trimField(
                            this.newEmployee.email
                        )
                        this.newEmployee.address = this.trimField(
                            this.newEmployee.address
                        )
                        const res = await this.$services.post(
                            '/employees',
                            this.newEmployee
                        )
                        this.showSuccessToast(res.data)
                        this.navigateToList()
                        this.isSubmitting = false
                    } catch (error) {
                        this.showErrorToast(error)
                        this.isSubmitting = false
                    }
                }
            })
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(
                        `categories.blacklist.error.${data.errorCode}`
                    ),
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
        showErrorToast(error) {
            console.log(error.message)
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t('Error.Error'),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(`${error.message}`),
                },
            })
        },
        navigateToList() {
            this.$router.push({ path: '/categories/blackLists/list' })
        },
    },
}
</script>

<style lang="scss" scoped>
.avatar-img {
    max-width: 100%;
    object-fit: contain;
}
</style>
