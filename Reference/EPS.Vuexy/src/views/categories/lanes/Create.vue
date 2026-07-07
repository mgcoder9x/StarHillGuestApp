<template>
    <validation-observer ref="obs">
        <b-card class>
            <b-card >
                <b-form @submit.prevent="onSubmit">
                    <!-- Lane info -->
                    <b-card class="form-flat shadow-sm">
                        <h6 class="text-muted text-uppercase font-weight-bold">{{ $t('Lanes.Form.Section.LaneInfo') }}</h6>
                        <b-row>
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.LaneCode')"
                                    label-class="required font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required"
                                        :name="$t('Lanes.Form.Fields.LaneCode')"
                                    >
                                        <b-form-input
                                            id="lane-code"
                                            v-model.lazy="form.LaneCode"
                                            autocomplete="off"
                                            spellcheck="false"
                                            placeholder="Nhập mã làn..."
                                            :state="errors.length ? false : null"
                                        />
                                        <small class="text-danger">{{ errors[0] }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>

                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.LaneName')"
                                    label-class="required font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required"
                                        :name="$t('Lanes.Form.Fields.LaneName')"
                                    >
                                        <b-form-input
                                            id="lane-name"
                                            v-model.lazy="form.LaneName"
                                            autocomplete="off"
                                            spellcheck="false"
                                            placeholder="Nhập tên làn..."
                                            :state="errors.length ? false : null"
                                        />
                                        <small class="text-danger">{{ errors[0] }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <b-row>
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.Direction')"
                                    label-class="required font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required"
                                        :name="$t('Lanes.Form.Fields.Direction')"
                                    >
                                        <b-form-select
                                            id="lane-direction"
                                            v-model="form.Direction"
                                            :options="withPlaceholder(directionOptions)"
                                            :state="errors.length ? false : null"
                                        />
                                        <small class="text-danger">{{ errors[0] }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>

                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.VehicleType')"
                                    label-class="required font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required"
                                        :name="$t('Lanes.Form.Fields.VehicleType')"
                                    >
                                        <b-form-select
                                            id="lane-vehicletype"
                                            v-model="form.VehicleType"
                                            :options="withPlaceholder(vehicleOptions)"
                                            :state="errors.length ? false : null"
                                        />
                                        <small class="text-danger">{{ errors[0] }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>
                        </b-row>
                        <b-row>
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.Area')"
                                    label-class="required font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required"
                                        :name="$t('Lanes.Form.Fields.Area')"
                                    >
                                        <b-form-select
                                            id="lane-area"
                                            v-model="form.AreaId"
                                            :options="withPlaceholder(areaOptions)"
                                            :state="errors.length ? false : null"
                                        />
                                        <small class="text-danger">{{ errors[0] }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.CardReader')"
                                    label-class="font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <b-form-select
                                        v-model="form.CardReaderId"
                                        :options="withPlaceholder(dropdowns.cardReaderOptions)"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <b-row>
                            
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.Controller')"
                                    label-class="font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <b-form-select
                                        v-model="form.ControllerId"
                                        :options="withPlaceholder(dropdowns.controllerOptions)"
                                    />
                                </b-form-group>
                            </b-col>
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.CameraConfig')"
                                    label-class="required font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required"
                                        :name="$t('Lanes.Form.Fields.CameraConfig')"
                                    >
                                        <b-form-select
                                            v-model="form.CameraConfig"
                                            :options="withPlaceholder(cameraConfigOptions)"
                                            :state="errors.length ? false : null"
                                        />
                                        <small class="text-danger">{{ errors[0] }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <b-row>
                            <b-col v-if="form.CameraConfig === 1 || form.CameraConfig === 3" md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.PlateCamera')"
                                    label-class="font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <b-form-select
                                        v-model="form.PlateCameraId"
                                        :options="withPlaceholder(plateCamOptions)"
                                    />
                                </b-form-group>
                            </b-col>

                            <b-col v-if="form.CameraConfig === 2 || form.CameraConfig === 3" md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.OverviewCamera')"
                                    label-class="font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <b-form-select
                                        v-model="form.OverviewCameraId"
                                        :options="withPlaceholder(overviewCamOptions)"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <b-row>
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.Status')"
                                    label-class="font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <b-form-select
                                        v-model="form.Status"
                                        :options="withPlaceholder(statusOptions)"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>
                    </b-card>
                    <!-- Lane settings -->
                    <b-card class="form-flat mb-0 shadow-sm">
                        <h6 class="text-muted text-uppercase font-weight-bold">{{ $t('Lanes.Form.Section.LaneConfig') }}</h6>

                        <b-row>
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.Function')"
                                    label-class="font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <b-form-select
                                        v-model="config.Function"
                                        :options="withPlaceholder(functionOptions)"
                                    />
                                </b-form-group>
                            </b-col>
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.AutoOpenBarrier')"
                                    label-class="font-weight-semibold"
                                    :class="formGroupClass"
                                    label-cols-md="4"
                                >
                                    <b-form-checkbox
                                        v-model="config.AutoOpen"
                                        switch
                                        class="custom-switch"
                                    >
                                        {{ config.AutoOpen ? $t('Common.On') : $t('Common.Off') }}
                                    </b-form-checkbox>
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <b-row>
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.DetectionDevices')"
                                    label-class="font-weight-semibold"
                                    :class="formGroupClass"
                                >
                                    <div class="d-flex flex-wrap">
                                        <b-form-checkbox v-model="config.HasCardReader" class="mr-3">
                                            {{ $t('Lanes.Enums.DetectDevice.CardReader') }}
                                        </b-form-checkbox>
                                        <b-form-checkbox v-model="config.HasLoopSensor" class="mr-3">
                                            {{ $t('Lanes.Enums.DetectDevice.LoopSensor') }}
                                        </b-form-checkbox>
                                        <b-form-checkbox v-model="config.HasAutoRecognition">
                                            {{ $t('Lanes.Enums.DetectDevice.AutoRecognition') }}
                                        </b-form-checkbox>
                                    </div>
                                </b-form-group>
                            </b-col>

                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Lanes.Form.Fields.RecognitionMethods')"
                                    label-class="font-weight-semibold"
                                    :class="formGroupClass"
                                >
                                    <div class="d-flex flex-wrap">
                                        <b-form-checkbox v-model="config.UseCard" class="mr-3">
                                            {{ $t('Lanes.Enums.Recognition.Card') }}
                                        </b-form-checkbox>
                                        <b-form-checkbox v-model="config.UseLicensePlate" class="mr-3">
                                            {{ $t('Lanes.Enums.Recognition.LicensePlate') }}
                                        </b-form-checkbox>
                                        <b-form-checkbox v-model="config.UseFace">
                                            {{ $t('Lanes.Enums.Recognition.Face') }}
                                        </b-form-checkbox>
                                    </div>
                                </b-form-group>
                            </b-col>
                        </b-row>

                        
                    </b-card>
                    <div class="mt-4 d-flex justify-content-center">
                            <b-button
                                class="mr-2 btn-120"
                                variant="primary"
                                type="submit"
                            >
                                {{ $t('Button.Save') }}
                            </b-button>
                            <b-button
                                class="btn-120"
                                variant="outline-secondary"
                                @click="$router.back()"
                            >
                                {{ $t('Button.Cancel') }}
                            </b-button>
                        </div>
                </b-form>
            </b-card>
        </b-card>
    </validation-observer>
</template>

<script>
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    name: 'LaneCreate',
    data() {
        return {
            formGroupClass: 'mb-2',
            form: {
                LaneCode: '',
                LaneName: '',
                Direction: null,
                VehicleType: null,
                AreaId: null,
                CardReaderId: null,
                ControllerId: null,
                CameraConfig: null,
                PlateCameraId: null,
                OverviewCameraId: null,
                Status: 1,
            },
            config: {
                Function: 1,
                AutoOpen: false,
                HasCardReader: false,
                HasLoopSensor: false,
                HasAutoRecognition: false,
                UseCard: false,
                UseLicensePlate: false,
                UseFace: false,
            },
            areaOptions: [],
            plateCamOptions: [],
            overviewCamOptions: [],
            dropdowns: {
                cardReaderOptions: [],
                controllerOptions: [],
            },
        }
    },
    computed: {
        directionOptions() {
            return [
                { value: 1, text: this.$t('Lanes.Enums.Direction.In') },
                { value: 2, text: this.$t('Lanes.Enums.Direction.Out') },
            ]
        },
        vehicleOptions() {
            return [
                { value: 1, text: this.$t('Lanes.Enums.VehicleType.Car') },
                { value: 2, text: this.$t('Lanes.Enums.VehicleType.Motorbike') },
            ]
        },
        cameraConfigOptions() {
            return [
                { value: 1, text: this.$t('Lanes.Enums.CameraConfig.Plate') },
                { value: 2, text: this.$t('Lanes.Enums.CameraConfig.Overview') },
                { value: 3, text: this.$t('Lanes.Enums.CameraConfig.Both') },
            ]
        },
        statusOptions() {
            return [
                { value: 1, text: this.$t('Common.Active') },
                { value: 0, text: this.$t('Common.Inactive') },
            ]
        },
        functionOptions() {
            return [
                { value: 1, text: this.$t('Lanes.Enums.Function.Monitor') },
                { value: 2, text: this.$t('Lanes.Enums.Function.Charge') },
                { value: 3, text: this.$t('Lanes.Enums.Function.Toll') },
            ]
        },
    },
    async created() {
        this.lookupFormData()
        await this.loadDropdown()
    },
    methods: {
        async loadDropdown() {
            try {
                const response = await this.$services.get(`/lanes/dropdowns`)
                this.dropdowns = {
                    cardReaderOptions: response.data.data.rfidDropdown,
                    controllerOptions: response.data.data.controllerDropdown,
                }
            } catch (error) {
                console.error('Error loading dropdowns:', error)
            }
        },
        isDuplicateLaneError(err) {
            const http = err?.response?.status
            const code = err?.response?.data?.errorCode
            const msg = err?.response?.data?.message
            return (
                http === 409 ||
                code === 'C_LANE_409' ||
                msg === 'AuthorizationService.Message.DuplicateLane'
            )
        },
        withPlaceholder(opts) {
            const first = {
                value: null,
                text: this.$t('Common.SelectValue'),
                disabled: true,
            }
            return [first, ...(opts || [])]
        },
        toInt(v) {
            const n = Number(v)
            return Number.isFinite(n) ? n : null
        },
        normalizeOptions(list) {
            if (!Array.isArray(list)) return []
            return list.map((x) => {
                const rawId = x.id ?? x.value ?? x.Id ?? null
                const value = this.toInt(rawId)
                const text =
                    x.text ??
                    x.label ??
                    x.name ??
                    x.Name ??
                    (value != null ? String(value) : '')
                return { value, text }
            })
        },
        lookupFormData() {
            const fill = (key, res) => {
                const raw =
                    (res && res.data && (res.data.data || res.data)) || []
                this[key] = this.normalizeOptions(raw)
            }
            this.$services
                .get('/lookup/areas')
                .then((r) => fill('areaOptions', r))
                .catch(() => {
                    this.areaOptions = []
                })
            this.$services
                .get('/lookup/devices?type=1')
                .then((r) => fill('plateCamOptions', r))
                .catch(() => {})
            this.$services
                .get('/lookup/devices?type=2')
                .then((r) => fill('overviewCamOptions', r))
                .catch(() => {})
        },

        async onSubmit() {
            const ok = await this.$refs.obs.validate()
            if (!ok) {
                this.scrollToFirstError()
                return
            }

            const lanePayload = {
                ...this.form,
                LaneCode: this.form.LaneCode?.trim() || null,
                LaneName: this.form.LaneName?.trim() || null,
                Direction: this.toInt(this.form.Direction),
                VehicleType: this.toInt(this.form.VehicleType),
                AreaId: this.toInt(this.form.AreaId),
                CardReaderId: this.toInt(this.form.CardReaderId),
                ControllerId: this.toInt(this.form.ControllerId),
                CameraConfig: this.toInt(this.form.CameraConfig),
                PlateCameraId: this.toInt(this.form.PlateCameraId),
                OverviewCameraId: this.toInt(this.form.OverviewCameraId),
                Status: this.toInt(this.form.Status),
            }

            try {
                const res = await this.$services.post('/lanes', lanePayload)
                const body = res && res.data
                const idCandidate =
                    body?.data?.Id ?? body?.data?.id ?? body?.data
                const newId = this.toInt(idCandidate)

                const cfg = {
                    ...this.config,
                    LaneId: newId,
                    Function: this.toInt(this.config.Function),
                }
                await this.$services.post(`/lanes/${newId}/settings`, cfg)

                this.showSuccessToast(res.data)
                this.navigateToList()
            } catch (err) {
                this.showErrorToast(err)
            }
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(`Lanes.Errors.${data.errorCode}`),
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
        showErrorToast(error) {
            console.log(error)
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: 'Error',
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(`${error.message}`),
                },
            })
        },
        navigateToList() {
            this.$router.push({ path: '/categories/lane/list' })
        },
        scrollToFirstError() {
            this.$nextTick(() => {
                const el = document.querySelector('.is-invalid, .text-danger')
                if (el && el.scrollIntoView)
                    el.scrollIntoView({ behavior: 'smooth', block: 'center' })
            })
        },
    },
}
</script>

<style lang="scss">
.form-flat {
    border: 1px solid #e5e7eb;
    background-color: #f8f9fa; // Thêm background-color nhẹ cho card
    border-radius: 12px;
    box-shadow: 0 8px 24px rgba(16, 24, 40, 0.1);
    overflow: hidden;
    padding: 0.5rem;
}

.shadow-sm {
    box-shadow: 0 2px 8px rgba(16, 24, 40, 0.05) !important;
    background-color: #ffffff; // Giữ background trắng cho phần nội dung
}

h5, h6 {
    background-color: #e9ecef; // Thêm background-color cho thẻ h để highlight tiêu đề
    padding: 0.5rem 1rem;
    border-radius: 4px;
    display: inline-block;
}

.btn-120 {
    min-width: 120px;
    transition: all 0.3s ease;
    &:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    }
}

.custom-switch .custom-control-label::before {
    background-color: #e5e7eb;
}

.custom-switch .custom-control-input:checked ~ .custom-control-label::before {
    background-color: #4caf50;
}

.text-primary {
    color: #007bff !important;
}

.text-muted {
    color: #6c757d !important;
}

.form-group label {
    font-size: 0.875rem;
    margin-bottom: 0.5rem;
}
</style>