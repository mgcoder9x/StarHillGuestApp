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
                                min="0"
                                max="18"
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
                @ready="onMapReady"
                @click="getLatLng"
                @zoom="setZoom"
            >
                <l-tile-layer :url="urlOSM"></l-tile-layer>
                <!-- Island -->
                <l-polygon
                    v-for="data in polygonMap.polygonBaoQuanh"
                    :key="data.id"
                    :lat-lngs="data.latlngs"
                    :l-style="polygonMap.polygonbao.style"
                >
                </l-polygon>
                <l-polygon
                    v-for="data in polygonMap.polygonDao"
                    :key="data.id"
                    :lat-lngs="data.latlngs"
                    :l-style="polygonMap.polygon.style"
                >
                </l-polygon>
                <l-marker
                    v-for="(marker, index) in polygonMap.markers"
                    :key="index"
                    :lat-lng="marker.position"
                >
                    <l-icon class-name="someExtraClass">
                        <div class="headline">
                            {{ marker.text }}
                        </div>
                    </l-icon>
                </l-marker>

                <l-marker
                    v-if="marker.lat && marker.lng"
                    :lat-lng="[marker.lat, marker.lng]"
                    :icon="icon.camOn"
                ></l-marker>
            </l-map>
        </b-modal>
    </div>
</template>

<script>
import { LMap, LTileLayer, LMarker, LPolygon, LIcon } from 'vue2-leaflet'
import 'leaflet/dist/leaflet.css'
import openstreetmap from '@/data/openstreetmap.json'
import L from 'leaflet'

export default {
    components: {
        LMap,
        LTileLayer,
        LMarker,
        LPolygon,
        LIcon,
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
                center: [16.964332450605063, 105.98940145630186],
                zoom: 5,
                urlOSM: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
                urlImgOverlay: '',
                bounds: [],
                opacity: 1,
            }),
        },
        isEditing:{
            type: Object,
            default: false
        }
    },
    data() {
        return {
            polygonMap: openstreetmap,
            coordinateShow: true,
            zoomValue: null,
            urlOSM: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
            marker: { lat: null, lng: null },
            modalShow: false,
            icon: {
                camOn: L.icon({
                    iconUrl: `${window.location.origin}/map/picker.png`,
                    iconSize: [20, 20],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                }),
            },
        }
    },
    mounted() {
        if (this.latLng.lat) {
            this.mapProp.zoom = this.latLng.zoom
            this.mapProp.center = [this.latLng.lat, this.latLng.lng]
        }
        this.zoomValue = this.mapProp.zoom
    },
    methods: {
        setZoom() {
            this.zoomValue = this.$refs.latLngMap.mapObject.getZoom()
        },
        updateZoom(zoom) {
           
            this.$refs.latLngMap.mapObject.setView(this.mapProp.center, zoom);
            // setTimeout(() => {
            //      this.$refs.latLngMap.mapObject.setZoom(zoom)
            // }, 1000)
           
        },
        onMapReady() {
            setTimeout(() => {
                this.$refs.latLngMap.mapObject.invalidateSize()
            }, 0)
            setTimeout(() => {
                this.marker.lat = this.latLng.lat
                this.marker.lng = this.latLng.lng
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
