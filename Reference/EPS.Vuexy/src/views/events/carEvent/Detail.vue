<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <!-- Hàng dữ liệu -->
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Events.Table.Time')"
                                label-for="h-access-time"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-access-time"
                                    v-model="event.accessTime"
                                    :disabled="true"
                                />
                            </b-form-group>
                        </b-col>

                        <b-col md="6">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.LicensePlate')"
                                label-for="h-license"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :rules="licenseRules"
                                    :name="
                                        $t('VehicleEvent.Field.LicensePlate')
                                    "
                                >
                                    <b-form-input
                                        id="h-license"
                                        v-model="updatedEvent.licensePlate"
                                        v-model.trim="updatedEvent.licensePlate"
                                        :disabled="!editing"
                                        :state="errors.length ? false : null"
                                        :placeholder="
                                            $t(
                                                'CarEvent.Common.Placeholder.License'
                                            )
                                        "
                                        @input="sanitizePlate"
                                        @keydown.enter.prevent="onSubmit"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Events.Table.Area')"
                                label-for="h-area"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-area"
                                    v-model="event.areaName"
                                    :disabled="true"
                                />
                            </b-form-group>
                        </b-col>

                        <b-col md="6">
                            <b-form-group
                                :label="$t('Events.Table.Device')"
                                label-for="h-device"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-device"
                                    v-model="event.deviceName"
                                    :disabled="true"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.Merchandise')"
                                label-for="h-merch"
                                label-cols-md="4"
                            >
                                <v-select
                                    :disabled="!editing"
                                    id="h-merch"
                                    v-model="updatedEvent.warningLevelId"
                                    :options="listWarningLevels"
                                    :reduce="(option) => option.id"
                                    label="label"
                                    :placeholder="
                                        $t(
                                            'common.form.placeholder.selectValue'
                                        )
                                    "
                                >
                                    <template #option="{ label }">
                                        {{ $t(label) }}
                                    </template>
                                    <template #selected-option="{ label }">
                                        {{ $t(label) }}
                                    </template>
                                </v-select>
                                <!-- <b-form-input
                                    v-else
                                    id="h-merch"
                                    v-model="event.warningLevelTitle"
                                    :disabled="true"
                                /> -->
                            </b-form-group>
                        </b-col>

                        <b-col md="6">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.Status')"
                                label-for="h-status"
                                label-cols-md="4"
                            >
                                <v-select
                                    v-if="editing"
                                    id="h-status"
                                    v-model="updatedEvent.statusId"
                                    :options="listWarningStatus"
                                    :reduce="(option) => option.id"
                                    label="label"
                                    :placeholder="
                                        $t(
                                            'common.form.placeholder.selectValue'
                                        )
                                    "
                                    :disabled="true"
                                >
                                    <template #option="{ label }">
                                        {{ $t(label) }}
                                    </template>
                                    <template #selected-option="{ label }">
                                        {{ $t(label) }}
                                    </template>
                                </v-select>
                                <b-form-input
                                    v-else
                                    id="h-status"
                                    v-model="event.statusName"
                                    :disabled="true"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.Direction')"
                                label-for="h-direction"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-direction"
                                    v-model="event.directionName"
                                    :disabled="true"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <b-row>
                        <b-col>
                            <b-form-group
                                :label="$t('Events.Table.Image')"
                                label-cols-md="12"
                            >
                                <media-swiper :uuid="eventId" />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <!-- Hàng nút hành động -->
                    <b-row>
                        <b-col class="text-center">
                            <Transition mode="out-in">
                                <!-- Lưu -->
                                <b-button
                                    v-if="
                                        editing && authorize(['ManageCarEvent'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="onSubmit"
                                >
                                    {{ $t('common.button.save') }}
                                </b-button>

                                <!-- Sửa -->
                                <b-button
                                    v-if="
                                        !editing &&
                                        authorize(['ManageCarEvent'])
                                    "
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="primary"
                                    @click="startEdit"
                                >
                                    {{ $t('common.button.edit') }}
                                </b-button>
                            </Transition>

                            <!-- Quay lại -->
                            <b-button
                                v-if="!editing"
                                :to="{ path: '/event/carEvent/list' }"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                            >
                                {{ $t('common.button.back') }}
                            </b-button>

                            <!-- Hủy -->
                            <b-button
                                v-if="editing"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                                @click="stopEdit"
                            >
                                {{ $t('common.button.cancel') }}
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
import { $themeConfig } from '@themeConfig'
import moment from 'moment'
import TreeHelper from '@/utils/treeHelper'
import { CarWarningType, CarStatus } from './warningLevelData'

// 👉 FIX: import và extend rule
import { extend } from 'vee-validate'
import { required, alpha_num, min, max } from 'vee-validate/dist/rules'

extend('required', { ...required })
extend('alpha_num', { ...alpha_num })
extend('min', { ...min })
extend('max', { ...max })

const isDevEnv = process.env.NODE_ENV === 'development'

export default {
    mixins: [authorizationMixin],
    data() {
        return {
            event: {},
            updatedEvent: {
                licensePlate: '',
                warningLevelId: null,
                statusId: null,
            },
            editing: false,
            baseURL: isDevEnv
                ? 'http://localhost:1938'
                : $themeConfig.app.apiURL,
            licenseRules: 'required|alpha_num|min:4|max:12',
            listWarningLevels: TreeHelper.removeEmptyChildren(CarWarningType),
            listWarningStatus: TreeHelper.removeEmptyChildren(CarStatus),
        }
    },
    computed: {
        eventId() {
            return this.$route.params.eventId
        },
    },
    async created() {
        await this.loadEventDetail()
    },
    methods: {
        async loadEventDetail() {
            const res = await this.$services.get(`/carEvent/${this.eventId}`)
            const data = res.data.data
            this.event = {
                ...data,
                accessTime: moment
                    .utc(data.accessTime)
                    .format('DD/MM/YYYY HH:mm:ss'),
                statusName: this.$t(data.statusName),
                warningLevelTitle: this.$t(data.warningLevelTitle),
                directionName: this.$t(data.directionName),
            }
            this.updatedEvent.licensePlate = (
                data.licensePlate || ''
            ).toString()
            this.updatedEvent.warningLevelId = data.warningLevelId || null
            // this.updatedEvent.statusId = data.status || null
            const validStatus = this.listWarningStatus.find(
                s => s.id === data.status
            )
            this.updatedEvent.statusId = validStatus ? validStatus.id : null
        },
        getStatusIdFromWarningLevel(warningLevelId) {
            switch (warningLevelId) {
                case 301:
                    return 1 // None
                case 302:
                    return 2 // Suspect
                case 303:
                case 304:
                case 305:
                    return 3 // HasMerchandise
                default:
                    return null
            }
        },
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.loadEventDetail()
        },
        trimField(field) {
            return field && field.toString().trim()
                ? field.toString().trim()
                : ''
        },
        sanitizePlate() {
            const raw = this.updatedEvent.licensePlate || ''
            this.updatedEvent.licensePlate = raw
                .replace(/[^A-Za-z0-9]/g, '')
                .toUpperCase()
        },
        onSubmit() {
            this.$refs.rules.validate().then(async (isValid) => {
                this.updatedEvent.licensePlate = this.trimField(
                    this.updatedEvent.licensePlate
                )
                    .replace(/[^A-Za-z0-9]/g, '')
                    .toUpperCase()
                const onlyAlphaNum = /^[A-Za-z0-9]{4,12}$/.test(
                    this.updatedEvent.licensePlate
                )

                if (!this.updatedEvent.statusId && this.updatedEvent.warningLevelId) {
                    this.updatedEvent.statusId = this.getStatusIdFromWarningLevel(
                        this.updatedEvent.warningLevelId
                    )
                }
                if (!isValid || !onlyAlphaNum) {
                    if (!this.updatedEvent.licensePlate) {
                        this.$toast({
                            component: ToastificationContent,
                            position: 'top-right',
                            props: {
                                title: this.$t('Error.Error'),
                                icon: 'AlertTriangleIcon',
                                variant: 'danger',
                                text: this.$t('CarEvent.Common.Error.License'),
                            },
                        })
                    } else if (!onlyAlphaNum) {
                        this.$toast({
                            component: ToastificationContent,
                            position: 'top-right',
                            props: {
                                title: this.$t('Error.Error'),
                                icon: 'AlertTriangleIcon',
                                variant: 'danger',
                                text: this.$t(
                                    'CarEvent.Common.Error.LicenseValid'
                                ),
                            },
                        })
                    }
                    return
                }

                try {
                    await this.$services.put(`/carEvent/${this.eventId}`, {
                        licensePlate: this.updatedEvent.licensePlate,
                    })
                    const payload = {
                        licensePlate: this.updatedEvent.licensePlate,
                        statusId: this.updatedEvent.statusId,
                        warningLevelId:
                            this.updatedEvent.warningLevelId || null,
                    }

                    await this.$services.put(
                        `/carEvent/${this.eventId}`,
                        payload
                    )
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: this.$t('Success.Update'),
                            icon: 'CheckIcon',
                            variant: 'success',
                            text: this.$t('Success.Update'),
                        },
                    })
                    this.stopEdit()
                } catch (error) {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: this.$t('Error.Error'),
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                            text: this.$t(
                                `${error?.message || 'Cập nhật thất bại.'}`
                            ),
                        },
                    })
                }
            })
        },
    },
}
</script>

<style lang="scss"></style>
