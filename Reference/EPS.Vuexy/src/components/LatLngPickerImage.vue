<template>
    <div>
        <b-modal
            v-if="modalShow"
            v-model="modalShow"
            size="xl"
            footer-class="d-none"
            centered
        >
            <div class="position-absolute fixed-modal-bottom">
                <b-button
                    v-if="!coordinateShow"
                    class="btn-custom"
                    variant="success"
                    @click="coordinateShow = !coordinateShow"
                >
                    <Icon
                        icon="ic:outline-my-location"
                        width="20"
                        height="20"
                    />
                </b-button>
            </div>
            <div class="position-absolute fixed-modal-bottom bg-white">
                <b-card
                    v-if="coordinateShow"
                    :title="this.$t('Device.Detail.Form.Coordinate')"
                    tag="article"
                    class="mb-2"
                    style="min-width: 13rem"
                >
                    <b-card-text>
                        <b-form-group :label="this.$t('Device.Detail.Form.Longitude')" :label-cols="4">
                            <b-form-input
                                v-model="marker.lng"
                                type="number"
                                step="0.0001"
                            />
                        </b-form-group>
                        <b-form-group :label="this.$t('Device.Detail.Form.Latitude')" :label-cols="4">
                            <b-form-input
                                v-model="marker.lat"
                                type="number"
                                step="0.0001"
                            />
                        </b-form-group>
                        <b-form-group label="Zoom" :label-cols="4">
                            <b-form-input
                                v-model="zoomValue"
                                type="number"
                                min="-2"
                                max="20"
                                step="0.1"
                                @input="updateZoom(zoomValue)"
                            />
                        </b-form-group>
                    </b-card-text>
                    <b-col class="d-flex justify-content-center align-items-center">
                        <b-button
                            variant="secondary"
                            class="mr-1"
                            @click="coordinateShow = !coordinateShow"
                            >{{ $t('Button.Hide') }}</b-button
                        >
                        <b-button variant="success" @click="save" v-if="!isEditing">{{ $t('Button.Luu') }}</b-button>
                    </b-col>
                </b-card>
            </div>
            <l-map
                ref="latLngMap"
                style="height: 80vh"
                class="rounded"
                :zoom="mapProp.zoom"
                :center="mapProp.center"
                :zoom-snap="0.1"
                :crs="crs"
                :min-zoom="-2"
                :max-zoom="20"
                @ready="onMapReady"
                @click="getLatLng"
                @zoom="setZoom"
            >
                <l-image-overlay
                    v-if="mapProp.urlImgOverlay"
                    :url="mapProp.urlImgOverlay"
                    :bounds="imageBounds"
                />
                <!-- Marker hiện tại đang chọn -->
                <l-marker
                    v-if="marker.lat && marker.lng"
                    :lat-lng="[marker.lat, marker.lng]"
                    :icon="icon.picker"
                >
                    <l-tooltip :options="{ permanent: false, direction: 'top' }">
                        {{ $t('Tọa độ camera đang chọn') }}
                    </l-tooltip>
                </l-marker>
                
                <!-- Markers của các camera khác -->
                <l-marker
                    v-for="cam in otherCameras"
                    :key="cam.id"
                    :lat-lng="[cam.latitude, cam.longitude]"
                    :icon="icon.other"
                >
                    <l-tooltip :options="{ permanent: true, direction: 'top' }">
                        {{ cam.name || cam.code }}
                    </l-tooltip>
                </l-marker>
            </l-map>
        </b-modal>
    </div>
</template>

<script>
import { LMap, LMarker, LImageOverlay, LTooltip } from 'vue2-leaflet'
import 'leaflet/dist/leaflet.css'
import L from 'leaflet'

export default {
    components: {
        LMap,
        LMarker,
        LImageOverlay,
        LTooltip,
    },
    props: {
        latLng: {
            type: Object,
            default: () => ({
                lat: null,
                lng: null,
                zoom: null,
            }),
        },
        mapProp: {
            type: Object,
            default: () => ({
                center: [0, 0],
                zoom: 3,
                urlImgOverlay: '',
                bounds: [],
                opacity: 1,
            }),
        },
        isEditing:{
            type: Boolean,
            default: false
        },
        cameras: {
            type: Array,
            default: () => []
        }
    },
    data() {
        return {
            coordinateShow: true,
            zoomValue: null,
            marker: { lat: null, lng: null },
            modalShow: false,
            crs: L.CRS.Simple,
            imageBounds: [
                [0, 0],
                [100, 100],
            ],
            icon: {
                picker: L.icon({
                    iconUrl: `${window.location.origin}/map/picker.png`,
                    iconSize: [20, 20],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                }),
                other: L.divIcon({
                    html: '<div style="background-color: #FF6B6B; width: 15px; height: 15px; border-radius: 50%; border: 2px solid white;"></div>',
                    iconSize: [15, 15],
                    iconAnchor: [7.5, 7.5],
                    tooltipAnchor: [0, -7],
                    className: 'custom-marker-icon'
                }),
            },
        }
    },
    mounted() {
        console.log('=== LatLngPickerImage mounted ===')
        console.log('Cameras prop on mount:', this.cameras)
        console.log('Cameras length on mount:', this.cameras ? this.cameras.length : 0)
        
        if (this.latLng && this.latLng.lat) {
            this.mapProp.zoom = this.latLng.zoom
            this.mapProp.center = [this.latLng.lat, this.latLng.lng]
        }
        this.zoomValue = this.mapProp.zoom
        this.calculateImageBounds()
        
        // Force recompute after mount
        this.$nextTick(() => {
            console.log('After nextTick - cameras:', this.cameras)
            console.log('After nextTick - otherCameras:', this.otherCameras)
        })
    },
    computed: {
        otherCameras() {
            // Kiểm tra cameras có phải là array không
            if (!Array.isArray(this.cameras)) {
                console.log('Cameras is not array:', this.cameras)
                return []
            }
            
            console.log('All cameras:', this.cameras.length)
            
            // Lọc ra các camera khác (có tọa độ và không phải camera hiện tại)
            const currentDeviceId = this.$route.params.deviceId
            const filtered = this.cameras.filter(cam => {
                const hasCoordinates = cam.latitude && cam.longitude
                const isDifferent = cam.id != currentDeviceId // Dùng != thay vì !== để so sánh cả string và number
                
                if (hasCoordinates && isDifferent) {
                    console.log('Camera will be shown:', cam.name || cam.code, cam.latitude, cam.longitude)
                }
                
                return hasCoordinates && isDifferent
            })
            
            console.log('Filtered cameras:', filtered.length)
            return filtered
        }
    },
    watch: {
        'mapProp.urlImgOverlay'(newVal) {
            if (newVal) {
                this.calculateImageBounds()
            }
        },
        cameras: {
            handler(newVal) {
                console.log('Cameras prop changed:', newVal ? newVal.length : 0, newVal)
                // Force re-render computed property
                this.$forceUpdate()
            },
            deep: true
        }
    },
    methods: {
        setZoom() {
            if (this.$refs.latLngMap && this.$refs.latLngMap.mapObject) {
                this.zoomValue = this.$refs.latLngMap.mapObject.getZoom()
            }
        },
        updateZoom(zoom) {
            if (this.$refs.latLngMap && this.$refs.latLngMap.mapObject) {
                this.$refs.latLngMap.mapObject.setZoom(zoom)
            }
        },
        onMapReady() {
            setTimeout(() => {
                if (this.$refs.latLngMap && this.$refs.latLngMap.mapObject) {
                    this.$refs.latLngMap.mapObject.invalidateSize()
                }
            }, 0)
            setTimeout(() => {
                if (this.latLng) {
                    this.marker.lat = this.latLng.lat
                    this.marker.lng = this.latLng.lng
                }
            }, 0)
        },
        toggleModal() {
            this.modalShow = !this.modalShow
            this.onMapReady()
        },
        getLatLng(e) {
            this.marker.lat = e.latlng.lat.toFixed(5)
            this.marker.lng = e.latlng.lng.toFixed(5)
        },
        save() {
            this.mapProp.center = [this.marker.lat, this.marker.lng]
            this.mapProp.zoom = this.$refs.latLngMap.mapObject.getZoom()
            this.toggleModal()
            this.$emit('input', {
                lat: this.marker.lat,
                lng: this.marker.lng,
                zoom: this.$refs.latLngMap.mapObject.getZoom(),
            })
        },
        calculateImageBounds() {
            if (!this.mapProp.urlImgOverlay) return

            const img = new Image()
            img.onload = () => {
                const width = img.width
                const height = img.height
                
                // Bounds với CRS.Simple: [y, x] format
                this.imageBounds = [
                    [0, 0],
                    [height, width],
                ]
                
                // Kiểm tra nếu chưa có tọa độ (null hoặc 0) → tự động tính center
                const hasCoordinate = this.latLng && this.latLng.lat !== null && this.latLng.lat !== 0
                if (!hasCoordinate) {
                    // Tự động tính điểm trung tâm
                    const centerLat = height / 2
                    const centerLng = width / 2
                    
                    this.mapProp.center = [centerLat, centerLng]
                    this.mapProp.zoom = -1
                    this.zoomValue = -1
                    
                    // Gán luôn vào marker để hiển thị
                    this.marker.lat = centerLat
                    this.marker.lng = centerLng
                }
                
                // Cập nhật map
                if (this.$refs.latLngMap && this.$refs.latLngMap.mapObject) {
                    this.$nextTick(() => {
                        this.$refs.latLngMap.mapObject.invalidateSize()
                        if (!hasCoordinate) {
                            this.$refs.latLngMap.mapObject.fitBounds(this.imageBounds)
                        }
                    })
                }
            }
            img.src = this.mapProp.urlImgOverlay
        },
    },
}
</script>

<style scoped lang="scss">
.btn-custom {
    &.btn-danger {
        width: initial !important;
    }
}

.fixed-modal-header {
    top: 0px;
    right: 0px;
    z-index: 1000;
}

.fixed-modal-bottom {
    margin: 10px;
    bottom: 0px;
    z-index: 1000;
    width: 15%;
}
</style>
