<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Device.Detail.Form.Code')"
                                label-for="h-camera-code"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    name="DeviceCode"
                                >
                                    <b-form-input
                                        id="h-camera-code"
                                        v-model.trim="device.code"
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
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Device.Detail.Form.Name')"
                                label-for="h-camera-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceName"
                                >
                                    <b-form-input
                                        id="h-camera-name"
                                        v-model.trim="device.name"
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
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Device.Detail.Form.Area')"
                                label-for="h-camera-area"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceArea"
                                >
                                    <tree-select
                                        v-model="device.areaId"
                                        :options="listArea"
                                        label="text"
                                        :reduce="(area) => area.id"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Device.Detail.Form.Function')"
                                label-for="h-camera-event-type"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceEventType"
                                >
                                    <b-form-select
                                        v-model="device.eventTypeId"
                                        :options="eventTypeOptions"
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
                                :label="this.$t('Device.Detail.Form.Status')"
                                label-for="h-camera-status"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceStatus"
                                >
                                    <b-form-select v-model="device.status">
                                        <b-form-select-option :value="1">
                                            On
                                        </b-form-select-option>
                                        <b-form-select-option :value="0">
                                            Off
                                        </b-form-select-option>
                                    </b-form-select>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Device.Detail.Form.Server')"
                                label-for="h-camera-server"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceServer"
                                >
                                    <b-form-select
                                        v-model="device.serverId"
                                        :options="listServer"
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
                                :label="this.$t('Device.Detail.Form.Link')"
                                label-for="h-camera-link"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceLink"
                                >
                                    <b-form-input
                                        id="h-camera-link"
                                        v-model.trim="device.link"
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
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Device.Detail.Form.License')"
                                label-for="h-camera-license"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceLicense"
                                >
                                    <b-form-input
                                        id="h-camera-license"
                                        v-model.trim="device.license"
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
                        <b-col md="3">
                            <b-form-group
                                :label="this.$t('Device.Detail.Form.Direction')"
                                label-for="h-camera-direction"
                                label-cols-md="5"
                            >
                                <b-form-select v-model="device.direction" id="__BVID__3340">
                                    <b-form-select-option :value="1">
                                        {{ $t('direction.in') }}
                                    </b-form-select-option>
                                    <b-form-select-option :value="2">
                                        {{ $t('direction.out') }}
                                    </b-form-select-option>
                                </b-form-select>
                            </b-form-group>
                        </b-col>
                        <b-col md="3">
                            <b-form-group
                                :label="
                                    this.$t('Device.Detail.Form.Coordinate')
                                "
                                label-for="h-camera-server"
                                label-cols-md="7"
                                id="__BVID__608__BV_label_"
                            >
                                <b-button
                                    size="sm"
                                    variant="success"
                                    @click="openMapPicker()"
                                >
                                    <Icon
                                        icon="ei:location"
                                        width="20"
                                        height="20"
                                    />
                                </b-button>
                                <LatLngPickerImage
                                    v-if="isShowImgMap"
                                    ref="mapPickerImage"
                                    :lat-lng="mapPicker.coordinate"
                                    :map-prop="mapPicker.mapProp"
                                    :cameras="allCameras"
                                    @input="setCoordinates"
                                />
                                <LatLngPicker
                                    v-else
                                    ref="mapPicker"
                                    :lat-lng="mapPicker.coordinate"
                                    :map-prop="mapPicker.mapProp"
                                    @input="setCoordinates"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Loại cam')"
                                label-for="h-camera-type"
                                label-cols-md="4"
                            >
                                <b-form-select v-model="device.camType">
                                    <b-form-select-option :value="1">
                                        {{ $t('Cam thường') }}
                                    </b-form-select-option>
                                    <b-form-select-option :value="2">
                                        {{ $t('Cam PTZ') }}
                                    </b-form-select-option>
                                </b-form-select>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <!-- 
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Device.Detail.Form.SoundFile')"
                                label-for="h-sound-file"
                                label-cols-md="4"
                            >
                                <b-form-file
                                    v-model="device.soundFile"
                                    :placeholder="
                                        this.$t(
                                            'Device.Detail.Form.SoundFileHolder'
                                        )
                                    "
                                    drop-placeholder="Kéo file vào đây..."
                                    accept=".mp3, .wav, .ogg"
                                    @input="handleFileUpload"
                                ></b-form-file>
                                <small class="text-muted">{{
                                    $t('Device.Detail.Form.SoundFileType')
                                }}</small>
                            </b-form-group>
                        </b-col>
                    </b-row> -->
                    <template v-if="device.camType === 2">
                    <b-row class="mt-3">
                        <b-col cols="12">
                            <h5
                                class="mb-3 text-primary font-weight-bold border-bottom pb-2"
                            >
                                {{
                                    $t('Thông tin camera mắt detail') ||
                                    'Thông tin camera mắt detail'
                                }}
                            </h5>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="4">
                            <b-form-group
                                :label="this.$t('RTSP Detail')"
                                label-for="h-camera-RTSP"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules=""
                                    name="RTSP"
                                >
                                <b-input-group size="md">
                                        <b-input-group-prepend is-text>
                                            <feather-icon icon="VideoIcon" />
                                        </b-input-group-prepend>
                                    <b-form-input
                                        id="h-camera-RTSP"
                                        v-model.trim="device.rtsp"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                </b-input-group>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="4">
                            <b-form-group
                                :label="this.$t('Cổng HTTP')"
                                label-for="h-camera-http"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules=""
                                    name="HTTP"
                                >
                                <b-input-group size="md">
                                        <b-input-group-prepend is-text>
                                            <feather-icon icon="GlobeIcon" />
                                        </b-input-group-prepend>
                                    <b-form-input
                                        id="h-camera-http"
                                        v-model.trim="device.httpGate"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                </b-input-group>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="4">
                            <b-form-group
                                :label="this.$t('IP')"
                                label-for="h-camera-ip"
                                label-cols-md="4"
                            >
                                <b-input-group size="md">
                                        <b-input-group-prepend is-text>
                                            <feather-icon icon="ServerIcon" />
                                        </b-input-group-prepend>
                                    <b-form-input
                                        id="h-camera-ip"
                                        v-model.trim="device.ip"
                                    />
                                </b-input-group>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    </template>
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-if="authorize(['ManageDevice'])"
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
                                :to="{ path: '/categories/devices/list' }"
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
import LatLngPicker from '@/components/LatLngPicker'
import LatLngPickerImage from '@/components/LatLngPickerImage'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import TreeHelper from '@/utils/treeHelper'
import { $themeConfig } from '@themeConfig'


export default {
    mixins: [authorizationMixin],
    components: { LatLngPicker, LatLngPickerImage },
    data() {
        return {
            mapPicker: {
                coordinate: {
                    lat: null,
                    lng: null,
                    zoom: null,
                },
                mapProp: {
                    urlImgOverlay: null,
                    center: [0, 0],
                    zoom: -1,
                },
            },
            isShowImgMap: false,
            companyId: null,
            companyImgPath: null,
            device: {
                code: null,
                name: null,
                link: null,
                areaId: null,
                serverId: null,
                eventTypeId: null,
                status: null,
                compId: null,
                longitude: null,
                latitude: null,
                license: null,
                zoom: null,
                direction: null,
                // Thêm mới trường dữ liệu cho file âm thanh
                soundFile: null,
                rtsp: null,
                ip: null,
                httpGate: 80,
                camType: null,
            },
            listArea: [],
            listEventType: [],
            listServer: [],
            allCameras: [],
        }
    },
    computed: {
        eventTypeOptions() {
            return this.listEventType.map((item) => ({
                value: item.id,
                text:
                    this.$i18n.locale === 'vi'
                        ? item.text
                        : item.englishName || item.text,
            }))
        },
        coordinate() {
            return {
                lat: this.device.latitude,
                lng: this.device.longitude,
            }
        },
        mapImg() {
            if (!this.companyImgPath) return null
            const url =
                process.env.NODE_ENV === 'development'
                    ? $themeConfig.app.apiURLDev
                    : $themeConfig.app.apiURL
            return url + '/MapImg/' + this.companyImgPath
        },
    },
    watch: {
    'device.camType'(newVal) {
        if (newVal === 1) { // Cam thường
            this.device.rtsp = null
            this.device.httpGate = null
            this.device.ip = null
        }
    }
},
    async created() {
        const accessToken = this.$services.getUserData()
        this.companyId = accessToken.companyId
        this.device.compId = accessToken.companyId
        await this.loadCompany()
        this.loadArea()
        this.loadEventType()
        this.loadServer()
        this.loadAllCameras()
    },
    methods: {
        async loadCompany() {
            try {
                if (this.companyId) {
                    const response = await this.$services.get(`/company/${this.companyId}`)
                    this.companyImgPath = response.data.imgPath
                    
                    // Set ảnh vào map
                    if (this.companyImgPath) {
                        this.mapPicker.mapProp.urlImgOverlay = this.mapImg
                        this.isShowImgMap = true
                    } else {
                        this.isShowImgMap = false
                        this.mapPicker.mapProp.center = [16.964332450605063, 105.98940145630186]
                        this.mapPicker.mapProp.zoom = 5
                    }
                }
            } catch (error) {
                console.error('Error loading company:', error)
            }
        },
        openMapPicker() {
            if (this.isShowImgMap) {
                this.$refs.mapPickerImage.toggleModal()
            } else {
                this.$refs.mapPicker.toggleModal()
            }
        },
        async setCoordinates(coordinate) {
            this.device.longitude = Number(coordinate.lng)
            this.device.latitude = Number(coordinate.lat)
            this.device.zoom = Number(coordinate.zoom)

            this.mapPicker.coordinate.lat = coordinate.lat
            this.mapPicker.coordinate.lng = coordinate.lng
            this.mapPicker.coordinate.zoom = coordinate.zoom
            this.mapPicker.mapProp.zoom = this.mapPicker.coordinate.zoom
            this.mapPicker.mapProp.center = [coordinate.lat, coordinate.lng]
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        handleFileUpload(file) {
            this.device.soundFile = file
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                debugger
                if (success) {
                    this.device.code = this.trimField(this.device.code)
                    this.device.name = this.trimField(this.device.name)
                    this.device.link = this.trimField(this.device.link)
                    this.device.license = this.trimField(this.device.license)

                    // Tạo một đối tượng FormData mới để gửi dữ liệu và file
                    const formData = new FormData()

                    // Thêm tất cả các trường dữ liệu của thiết bị vào FormData
                    for (const key in this.device) {
                        if (
                            Object.prototype.hasOwnProperty.call(
                                this.device,
                                key
                            )
                        ) {
                            const value = this.device[key]

                            // Xử lý đặc biệt cho soundFile
                            if (key === 'soundFile' && value) {
                                formData.append(key, value, value.name)
                            }
                            // Bỏ qua các field null hoặc undefined
                            else if (value !== null && value !== undefined) {
                                formData.append(key, value)
                            }
                        }
                    }

                    this.$services
                        .post('/device', formData, {
                            headers: {
                                'Content-Type': 'multipart/form-data',
                            },
                        })
                        .then((response) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.CreateCamera'),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                            this.$router.push({
                                path: '/categories/devices/list',
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

        //lookup data
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                this.listEventType = response.data.data
            })
        },
        loadArea() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.listArea = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
        },
        loadServer() {
            this.$services.get('/lookup/server').then((response) => {
                this.listServer = response.data.data
            })
        },
        async loadAllCameras() {
            try {
                const response = await this.$services.get('/device', {
                    params: {
                        page: 1,
                        perPage: 1000,
                        sortBy: 'id',
                        sortDesc: false
                    }
                })
                
                // API trả về structure: { data: { data: [...] } }
                let cameras = []
                if (response.data && response.data.data) {
                    // Nếu data.data là object có property data (nested)
                    if (response.data.data.data && Array.isArray(response.data.data.data)) {
                        cameras = response.data.data.data
                    } 
                    // Nếu data.data là array luôn
                    else if (Array.isArray(response.data.data)) {
                        cameras = response.data.data
                    }
                }
                
                this.allCameras = cameras
            } catch (error) {
                console.error('Error loading cameras:', error)
                this.allCameras = []
            }
        },
    },
}
</script>

<style lang="scss">
#__BVID__3340{
    margin-left: 100px;
}
#__BVID__608__BV_label_{
    margin-left: 200px;
}
</style>
