<template>
    <validation-observer ref="obs">
        <b-container fluid>
            <!-- Header -->
            <!-- <b-card class="mb-2">
            <h5 class="mb-0">
                {{ isEdit ? $t('Lanes.Form.Title.Update') : $t('Common.View') }}
            </h5>
        </b-card> -->

            <!-- Lane info -->
            <b-card class="mb-3">
                <h6 class="mb-2">{{ $t('Lanes.Form.Section.LaneInfo') }}</h6>
                <b-row>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.LaneCode')"
                            label-class="required"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="$t('Lanes.Form.Fields.LaneCode')"
                            >
                                <b-form-input
                                    v-model.trim="form.LaneCode"
                                    :disabled="!isEdit"
                                    :state="errors.length ? false : null"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.LaneName')"
                            label-class="required"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="$t('Lanes.Form.Fields.LaneName')"
                            >
                                <b-form-input
                                    v-model.trim="form.LaneName"
                                    :disabled="!isEdit"
                                    :state="errors.length ? false : null"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-row>
                    <b-col md="3">
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.Direction')"
                            label-class="required"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="$t('Lanes.Form.Fields.Direction')"
                            >
                                <b-form-select
                                    v-model="form.Direction"
                                    :options="directionOptions"
                                    :disabled="!isEdit"
                                    :state="errors.length ? false : null"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                    <b-col md="3">
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.VehicleType')"
                            label-class="required"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="$t('Lanes.Form.Fields.VehicleType')"
                            >
                                <b-form-select
                                    v-model="form.VehicleType"
                                    :options="vehicleOptions"
                                    :disabled="!isEdit"
                                    :state="errors.length ? false : null"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.Area')"
                            label-class="required"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="$t('Lanes.Form.Fields.Area')"
                            >
                                <b-form-select
                                    v-model="form.AreaId"
                                    :options="areaOptions"
                                    :disabled="!isEdit"
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
                            :label="$t('Lanes.Form.Fields.CardReader')"
                        >
                            <b-form-select
                                v-model="form.CardReaderId"
                                label="text"
                                :options="dropdowns.cardReaderOptions"
                                :disabled="!isEdit"
                            />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.Controller')"
                        >
                            <b-form-select
                                v-model="form.ControllerId"
                                label="text"
                                :options="dropdowns.controllerOptions"
                                :disabled="!isEdit"
                            />
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-row>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.CameraConfig')"
                            label-class="required"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="$t('Lanes.Form.Fields.CameraConfig')"
                            >
                                <b-form-select
                                    v-model="form.CameraConfig"
                                    :options="cameraConfigOptions"
                                    :disabled="!isEdit"
                                    :state="errors.length ? false : null"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>

                    <b-col
                        v-if="
                            form.CameraConfig === 1 || form.CameraConfig === 3
                        "
                        md="6"
                    >
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.PlateCamera')"
                        >
                            <b-form-select
                                v-model="form.PlateCameraId"
                                :options="plateCamOptions"
                                :disabled="!isEdit"
                            />
                        </b-form-group>
                    </b-col>

                    <b-col
                        v-if="
                            form.CameraConfig === 2 || form.CameraConfig === 3
                        "
                        md="6"
                    >
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.OverviewCamera')"
                        >
                            <b-form-select
                                v-model="form.OverviewCameraId"
                                :options="overviewCamOptions"
                                :disabled="!isEdit"
                            />
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-row>
                    <b-col md="3">
                        <b-form-group :label="$t('Lanes.Form.Fields.Status')">
                            <b-form-select
                                v-model="form.Status"
                                :options="statusOptions"
                                :disabled="!isEdit"
                            />
                        </b-form-group>
                    </b-col>
                </b-row>
            </b-card>

            <!-- Lane settings -->
            <b-card>
                <h6 class="mb-2">{{ $t('Lanes.Form.Section.LaneConfig') }}</h6>

                <b-row>
                    <b-col md="6">
                        <b-form-group :label="$t('Lanes.Form.Fields.Function')">
                            <b-form-select
                                v-model="config.Function"
                                :options="functionOptions"
                                :disabled="!isEdit"
                                @focus.native="markConfigTouched"
                                @mousedown.native="markConfigTouched"
                                @change="markConfigDirty"
                            />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.AutoOpenBarrier')"
                        >
                            <b-form-checkbox
                                v-model="config.AutoOpen"
                                switch
                                :disabled="!isEdit"
                                @focus.native="markConfigTouched"
                                @mousedown.native="markConfigTouched"
                                @change="markConfigDirty"
                            >
                                {{
                                    config.AutoOpen
                                        ? $t('Common.On')
                                        : $t('Common.Off')
                                }}
                            </b-form-checkbox>
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-row>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.DetectionDevices')"
                        >
                            <div class="d-flex flex-wrap">
                                <b-form-checkbox
                                    v-model="config.HasCardReader"
                                    :disabled="!isEdit"
                                    class="mr-3"
                                    @focus.native="markConfigTouched"
                                    @mousedown.native="markConfigTouched"
                                    @change="markConfigDirty"
                                >
                                    {{
                                        $t(
                                            'Lanes.Enums.DetectDevice.CardReader'
                                        )
                                    }}
                                </b-form-checkbox>
                                <b-form-checkbox
                                    v-model="config.HasLoopSensor"
                                    :disabled="!isEdit"
                                    class="mr-3"
                                    @focus.native="markConfigTouched"
                                    @mousedown.native="markConfigTouched"
                                    @change="markConfigDirty"
                                >
                                    {{
                                        $t(
                                            'Lanes.Enums.DetectDevice.LoopSensor'
                                        )
                                    }}
                                </b-form-checkbox>
                                <b-form-checkbox
                                    v-model="config.HasAutoRecognition"
                                    :disabled="!isEdit"
                                    @focus.native="markConfigTouched"
                                    @mousedown.native="markConfigTouched"
                                    @change="markConfigDirty"
                                >
                                    {{
                                        $t(
                                            'Lanes.Enums.DetectDevice.AutoRecognition'
                                        )
                                    }}
                                </b-form-checkbox>
                            </div>
                        </b-form-group>
                    </b-col>

                    <b-col md="6">
                        <b-form-group
                            :label="$t('Lanes.Form.Fields.RecognitionMethods')"
                        >
                            <div class="d-flex flex-wrap">
                                <b-form-checkbox
                                    v-model="config.UseCard"
                                    :disabled="!isEdit"
                                    class="mr-3"
                                    @focus.native="markConfigTouched"
                                    @mousedown.native="markConfigTouched"
                                    @change="markConfigDirty"
                                >
                                    {{ $t('Lanes.Enums.Recognition.Card') }}
                                </b-form-checkbox>
                                <b-form-checkbox
                                    v-model="config.UseLicensePlate"
                                    :disabled="!isEdit"
                                    class="mr-3"
                                    @focus.native="markConfigTouched"
                                    @mousedown.native="markConfigTouched"
                                    @change="markConfigDirty"
                                >
                                    {{
                                        $t(
                                            'Lanes.Enums.Recognition.LicensePlate'
                                        )
                                    }}
                                </b-form-checkbox>
                                <b-form-checkbox
                                    v-model="config.UseFace"
                                    :disabled="!isEdit"
                                    @focus.native="markConfigTouched"
                                    @mousedown.native="markConfigTouched"
                                    @change="markConfigDirty"
                                >
                                    {{ $t('Lanes.Enums.Recognition.Face') }}
                                </b-form-checkbox>
                            </div>
                        </b-form-group>
                    </b-col>
                </b-row>

                <!-- Actions -->
                <b-row>
                    <b-col>
                        <div class="text-center mt-2">
                            <transition mode="out-in">
                                <b-button
                                    v-if="isEdit && authorize(['ManageLane'])"
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    :disabled="saving || !loaded"
                                    @click="save"
                                >
                                    {{ $t('common.button.save') }}
                                </b-button>
                                <b-button
                                    v-else-if="
                                        !isEdit && authorize(['ManageLane'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    :disabled="!loaded"
                                    @click="startEdit"
                                >
                                    {{ $t('common.button.edit') }}
                                </b-button>
                            </transition>

                            <b-button
                                v-if="!isEdit"
                                type="button"
                                variant="outline-secondary"
                                class="mx-50 mb-50 btn-120"
                                @click="$router.back()"
                            >
                                {{ $t('common.button.back') }}
                            </b-button>

                            <b-button
                                v-if="isEdit"
                                type="button"
                                variant="outline-secondary"
                                class="mx-50 mb-50 btn-120"
                                :disabled="saving"
                                @click="cancelEdit"
                            >
                                {{ $t('common.button.cancel') }}
                            </b-button>
                        </div>
                    </b-col>
                </b-row>
            </b-card>
        </b-container>
    </validation-observer>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    name: 'LaneUpsert',
    mixins: [authorizationMixin],
    data() {
        return {
            id: Number(this.$route.params.id),
            isEdit: false,

            // ready/guard flags
            loaded: false,
            saving: false,
            loadingConfig: false,
            configTouched: false,
            configDirty: false,
            // Lane VM
            form: {
                Id: null,
                LaneCode: '',
                LaneName: '',
                Direction: 1,
                VehicleType: 1,
                AreaId: null,
                CardReaderId: null,
                ControllerId: null,
                CameraConfig: 1,
                PlateCameraId: null,
                OverviewCameraId: null,
                Status: 1,
            },

            // Config VM
            config: {
                Id: null,
                LaneId: null,
                Function: 1,
                AutoOpen: false,
                HasCardReader: false,
                HasLoopSensor: false,
                HasAutoRecognition: false,
                UseCard: false,
                UseLicensePlate: false,
                UseFace: false,
            },
            originalConfig: null, // snapshot

            // options
            directionOptions: [
                { value: 1, text: this.$t('Lanes.Enums.Direction.In') },
                { value: 2, text: this.$t('Lanes.Enums.Direction.Out') },
            ],
            vehicleOptions: [
                { value: 1, text: this.$t('Lanes.Enums.VehicleType.Car') },
                {
                    value: 2,
                    text: this.$t('Lanes.Enums.VehicleType.Motorbike'),
                },
            ],
            cameraConfigOptions: [
                { value: 1, text: this.$t('Lanes.Enums.CameraConfig.Plate') },
                {
                    value: 2,
                    text: this.$t('Lanes.Enums.CameraConfig.Overview'),
                },
                { value: 3, text: this.$t('Lanes.Enums.CameraConfig.Both') },
            ],
            statusOptions: [
                { value: 1, text: this.$t('Common.Active') },
                { value: 0, text: this.$t('Common.Inactive') },
            ],
            functionOptions: [
                { value: 1, text: this.$t('Lanes.Enums.Function.Monitor') },
                { value: 2, text: this.$t('Lanes.Enums.Function.Charge') },
                { value: 3, text: this.$t('Lanes.Enums.Function.Toll') },
            ],
            areaOptions: [],
            plateCamOptions: [
                { value: null, text: this.$t('Common.SelectNone') },
            ],
            overviewCamOptions: [
                { value: null, text: this.$t('Common.SelectNone') },
            ],
            // cardReaderOptions: [
            //     { value: null, text: this.$t('Common.SelectNone') },
            // ],
            // controllerOptions: [
            //     { value: null, text: this.$t('Common.SelectNone') },
            // ],
            dropdowns: {
                cardReaderOptions: [],
                controllerOptions: [],
            },
        }
    },
    async created() {
        await this.loadDropdown()
        this.lookupFormData()
        this.load()
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
        // utils
        // ===== Helpers =====
        toInt(v) {
            const n = Number(v)
            return Number.isFinite(n) ? n : null
        },
        clone(o) {
            return JSON.parse(JSON.stringify(o || {}))
        },

        // Hợp nhất config hiện tại với bản gốc để không bị mất field nào
        mergeConfig(orig, curr) {
            const keys = [
                'Id',
                'LaneId',
                'Function',
                'AutoOpen',
                'HasCardReader',
                'HasLoopSensor',
                'HasAutoRecognition',
                'UseCard',
                'UseLicensePlate',
                'UseFace',
            ]
            const out = {}
            for (const k of keys) {
                const vCurr = curr != null ? curr[k] : undefined
                const vOrig = orig != null ? orig[k] : undefined
                out[k] = vCurr !== undefined ? vCurr : vOrig
            }
            return out
        },

        normalizeOptions(list) {
            if (!Array.isArray(list)) return []
            return list.map((x) => ({
                value: this.toInt(x.id ?? x.value ?? x.Id),
                text:
                    x.text ??
                    x.label ??
                    x.name ??
                    x.Name ??
                    String(x.id ?? x.value ?? ''),
            }))
        },

        toFormVM(api) {
            return {
                Id: this.toInt(api.id),
                LaneCode: api.laneCode || '',
                LaneName: api.laneName || '',
                Direction: this.toInt(api.direction) ?? 1,
                VehicleType: this.toInt(api.vehicleType) ?? 1,
                AreaId: this.toInt(api.areaId),
                CardReaderId: this.toInt(api.cardReaderId),
                ControllerId: this.toInt(api.controllerId),
                CameraConfig: this.toInt(api.cameraConfig) ?? 1,
                PlateCameraId: this.toInt(api.plateCameraId),
                OverviewCameraId: this.toInt(api.overviewCameraId),
                Status: this.toInt(api.status) ?? 1,
            }
        },
        toConfigVM(api) {
            return {
                Id: this.toInt(api.id),
                LaneId: this.toInt(api.laneId),
                Function: this.toInt(api.function) ?? 1,
                AutoOpen: !!api.autoOpen,
                HasCardReader: !!api.hasCardReader,
                HasLoopSensor: !!api.hasLoopSensor,
                HasAutoRecognition: !!api.hasAutoRecognition,
                UseCard: !!api.useCard,
                UseLicensePlate: !!api.useLicensePlate,
                UseFace: !!api.useFace,
            }
        },
        toFormDTO(vm) {
            return {
                id: this.toInt(vm.Id),
                laneCode: vm.LaneCode,
                laneName: vm.LaneName,
                direction: this.toInt(vm.Direction),
                vehicleType: this.toInt(vm.VehicleType),
                areaId: this.toInt(vm.AreaId),
                cardReaderId: this.toInt(vm.CardReaderId),
                controllerId: this.toInt(vm.ControllerId),
                cameraConfig: this.toInt(vm.CameraConfig),
                plateCameraId: this.toInt(vm.PlateCameraId),
                overviewCameraId: this.toInt(vm.OverviewCameraId),
                status: this.toInt(vm.Status),
            }
        },
        toConfigDTO(cfg) {
            return {
                id: this.toInt(cfg.Id),
                laneId: this.toInt(cfg.LaneId),
                function: this.toInt(cfg.Function),
                autoOpen: !!cfg.AutoOpen,
                hasCardReader: !!cfg.HasCardReader,
                hasLoopSensor: !!cfg.HasLoopSensor,
                hasAutoRecognition: !!cfg.HasAutoRecognition,
                useCard: !!cfg.UseCard,
                useLicensePlate: !!cfg.UseLicensePlate,
                useFace: !!cfg.UseFace,
            }
        },

        // compare settings
        normForCompare(cfg) {
            if (!cfg) return null
            const n = (v) => (v === undefined ? null : v)
            return {
                LaneId: this.toInt(n(cfg.LaneId)),
                Function: this.toInt(n(cfg.Function)) ?? 1,
                AutoOpen: !!n(cfg.AutoOpen),
                HasCardReader: !!n(cfg.HasCardReader),
                HasLoopSensor: !!n(cfg.HasLoopSensor),
                HasAutoRecognition: !!n(cfg.HasAutoRecognition),
                UseCard: !!n(cfg.UseCard),
                UseLicensePlate: !!n(cfg.UseLicensePlate),
                UseFace: !!n(cfg.UseFace),
            }
        },
        hasConfigChanged() {
            const a = JSON.stringify(this.normForCompare(this.originalConfig))
            const b = JSON.stringify(this.normForCompare(this.config))
            return a !== b
        },
        shouldSaveConfig() {
            // nếu bạn vẫn muốn chỉ save khi có thay đổi:
            return (
                this.configTouched &&
                this.configDirty &&
                !this.loadingConfig &&
                this.hasConfigChanged()
            )
            // nếu muốn luôn save settings mỗi khi bấm Lưu, đổi dòng trên thành: return true
        },
        markConfigTouched() {
            if (!this.loadingConfig) this.configTouched = true
        },
        markConfigDirty() {
            if (!this.loadingConfig) this.configDirty = true
        },
        shouldSaveConfig() {
            // chỉ khi user thật sự tương tác & có thay đổi & không còn trong trạng thái loading
            return (
                this.configTouched &&
                this.configDirty &&
                !this.loadingConfig &&
                this.hasConfigChanged()
            )
        },
        markConfigTouched() {
            if (!this.loadingConfig) this.configTouched = true
        },
        markConfigDirty() {
            if (!this.loadingConfig) this.configDirty = true
        },

        // lookups
        lookupFormData() {
            this.$services
                .get('/lookup/areas')
                .then((res) => {
                    this.areaOptions = this.normalizeOptions(
                        res?.data?.data || res?.data || []
                    )
                })
                .catch(() => {
                    this.areaOptions = []
                })

            const fill = (k, r) => {
                const opts = this.normalizeOptions(
                    r?.data?.data || r?.data || []
                )
                this[k] = [
                    { value: null, text: this.$t('Common.SelectNone') },
                    ...opts,
                ]
            }
            this.$services
                .get('/lookup/devices?type=1')
                .then((r) => fill('plateCamOptions', r))
                .catch(() => {})
            this.$services
                .get('/lookup/devices?type=2')
                .then((r) => fill('overviewCamOptions', r))
                .catch(() => {})
            // this.$services
            //     .get('/lookup/devices?type=3')
            //     .then((r) => fill('cardReaderOptions', r))
            //     .catch(() => {})
            // this.$services
            //     .get('/lookup/devices?type=5')
            //     .then((r) => fill('controllerOptions', r))
            //     .catch(() => {})
        },

        // load
        async load() {
            try {
                this.loaded = false
                this.loadingConfig = true
                const r = await this.$services.get(`/lanes/${this.id}`)
                this.form = this.toFormVM(r?.data?.data || r?.data || {})

                const s = await this.$services
                    .get(`/lanes/${this.id}/settings`)
                    .catch(() => null)
                const cfg = s?.data?.data || s?.data
                this.config = cfg
                    ? this.toConfigVM(cfg)
                    : { ...this.config, LaneId: this.id }

                this.originalConfig = this.clone(this.config)
                this.configTouched = false
                this.configDirty = false
            } finally {
                this.loadingConfig = false
                this.loaded = true
            }
        },

        // save
        async save() {
            if (!this.loaded || this.saving) return
            if (this.isEdit) {
                const ok = await this.$refs.obs.validate()
                if (!ok) {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: this.$t('Common.Error'),
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                        },
                    })
                    this.scrollToFirstError()
                    return
                }
            }
            this.saving = true
            try {
                // 1) Lưu Lane
                await this.$services.put(
                    `/lanes/${this.id}`,
                    this.toFormDTO(this.form)
                )

                // 2) Lưu Settings (tuỳ chính sách)
                if (this.shouldSaveConfig()) {
                    const merged = this.mergeConfig(
                        this.originalConfig,
                        this.config
                    )
                    merged.LaneId = this.id // đảm bảo laneId đúng
                    const dto = this.toConfigDTO(merged)

                    if (dto.id) {
                        await this.$services.put(
                            `/lanes/settings/${dto.id}`,
                            dto
                        )
                    } else {
                        await this.$services.post(
                            `/lanes/${this.id}/settings`,
                            dto
                        )
                    }

                    // cập nhật snapshot sau khi lưu
                    this.originalConfig = this.clone(this.toConfigVM(dto))
                    this.configTouched = false
                    this.configDirty = false
                }

                this.$toast?.success?.(this.$t('Common.Success'))
                this.isEdit = false
                await this.load()
            } catch (err) {
                this.showErrorToast(err)
            } finally {
                this.saving = false
            }
        },
        showErrorToast(error) {
            console.error('Save error:', error)
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t('Common.Error'),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text:
                        error?.response?.data?.message ||
                        error.message ||
                        'Unknown error',
                },
            })
        },
        scrollToFirstError() {
            this.$nextTick(() => {
                const el = document.querySelector('.is-invalid, .text-danger')
                if (el && el.scrollIntoView)
                    el.scrollIntoView({ behavior: 'smooth', block: 'center' })
            })
        },
        startEdit() {
            this.isEdit = true
        },
        async cancelEdit() {
            await this.load()
            this.isEdit = false
        },
    },
}
</script>

<style scoped>
.btn-120 {
    min-width: 120px;
}
.mx-50 {
    margin-left: 0.5rem;
    margin-right: 0.5rem;
}
.mb-50 {
    margin-bottom: 0.5rem;
}
</style>
