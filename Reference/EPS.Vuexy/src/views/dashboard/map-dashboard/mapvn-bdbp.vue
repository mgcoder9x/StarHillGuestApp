<template>
    <div class="animated fadeIn map-wrapper" :style="{ height: mapHeight }">
        <b-container fluid class="p-0 h-100">
            <b-row class="no-gutters h-100">
                <!-- Cột chứa map với tỉ lệ dynamic -->
                <b-col
                    :xl="showSidebar ? 10 : 12"
                    class="h-100"
                    :class="{ 'pr-1': showSidebar }"
                >
                    <div class="map-card-border mb-0 h-100">
                        <b-card-body class="p-0 h-100">
                            <div
                                id="map-container"
                                class="position-relative h-100"
                            >
                                <l-map
                                    ref="latLngMap"
                                    class="rounded map-view"
                                    style="height: 100%; width: 100%;"
                                    :zoom="mapZoom"
                                    :center="mapCenter"
                                    :zoom-snap="0.1"
                                    :min-zoom="-5"
                                    :max-zoom="20"
                                    :crs="actualShowImgMap ? simpleCRS : undefined"
                                    :options="{
                                        attributionControl: false,
                                        zoomControl: false,
                                        closePopupOnClick: false,
                                    }"
                                    @ready="onMapReady"
                                >
                                    <!-- Chức năng, khu vực -->
                                    <l-control
                                        style="width: 45%"
                                        position="topright"
                                        class="mt-2 mr-2"
                                    >
                                        <div class="d-flex align-items-center justify-content-end gap-2">
                                            <tree-select
                                                v-model="areaSelected"
                                                :multiple="false"
                                                :options="areasTree"
                                                :normalizer="normalizer"
                                                :placeholder="
                                                    $t(
                                                        'Events.SearchForm.Placeholder.FindArea'
                                                    )
                                                "
                                                :default-expand-level="0"
                                                value-consists-of="LEAF_PRIORITY"
                                                open-direction="bottom"
                                                class="map-control-select"
                                                style="flex: 4;"
                                            >
                                            </tree-select>

                                            <tree-select
                                                v-model="eventTypeSelected"
                                                label="text"
                                                :reduce="
                                                    (eventType) => eventType.id
                                                "
                                                :options="listEventTypeByArea"
                                                :placeholder="
                                                    $t(
                                                        'Events.SearchForm.Placeholder.Feature'
                                                    )
                                                "
                                                class="map-control-select"
                                                style="flex: 5;"
                                                @input="getShowTable"
                                                @open="treeSelectOpen = true"
                                                @close="treeSelectOpen = false"
                                            >
                                            </tree-select>

                                            <b-button
                                                v-if="!showSidebar"
                                                variant="light"
                                                class="sidebar-toggle-btn"
                                                :title="$t('Map.Controls.ShowStatistics')"
                                                @click="toggleSidebar()"
                                            >
                                                <feather-icon
                                                    icon="BarChart2Icon"
                                                    size="18"
                                                />
                                            </b-button>
                                        </div>
                                    </l-control>

                                    <!-- Zoom Controls - Nằm riêng bên phải dưới toggle -->
                                    <l-control
                                        position="topright"
                                        class="mt-1 mr-2"
                                    >
                                        <div v-show="!treeSelectOpen" class="zoom-controls">
                                            <b-button
                                                variant="light"
                                                class="zoom-btn"
                                                :title="$t('Map.Controls.ZoomIn')"
                                                @click="handleZoomIn"
                                            >
                                                <feather-icon icon="PlusIcon" size="16" class="text-primary" />
                                            </b-button>
                                            <b-button
                                                variant="light"
                                                class="zoom-btn"
                                                :title="$t('Map.Controls.ZoomOut')"
                                                @click="handleZoomOut"
                                            >
                                                <feather-icon icon="MinusIcon" size="16" class="text-primary" />
                                            </b-button>
                                            <b-button
                                                variant="light"
                                                class="zoom-btn"
                                                :title="$t('Map.Controls.ResetZoom')"
                                                @click="handleZoomReset"
                                            >
                                                <feather-icon icon="MaximizeIcon" size="16" class="text-primary" />
                                            </b-button>
                                            <b-button
                                                :variant="
                                                    enableTooltip
                                                        ? 'primary'
                                                        : 'light'
                                                "
                                                class="zoom-btn"
                                                :title="$t('Map.Controls.ToggleTooltips')"
                                                @click="toggleAllTooltips"
                                            >
                                                <feather-icon 
                                                    :icon="enableTooltip ? 'EyeOffIcon' : 'EyeIcon'" 
                                                    size="16" 
                                                    class="text-primary"
                                                />
                                            </b-button>
                                        </div>
                                    </l-control>
                                    <l-tile-layer
                                        v-if="!actualShowImgMap"
                                        :url="urlOSM"
                                    />
                                    <l-image-overlay
                                        v-if="actualShowImgMap && actualUrlImgOverlay && imageBounds"
                                        :url="actualUrlImgOverlay"
                                        :bounds="imageBounds"
                                    />

                                    <!-- Map content -->
                                    <div v-if="!actualShowImgMap">
                                        <!-- Island -->
                                        <l-polygon
                                            v-for="data in polygonMap.polygonBaoQuanh"
                                            :key="data.id"
                                            :lat-lngs="data.latlngs"
                                            :options="
                                                polygonMap.polygonbao.style
                                            "
                                        >
                                        </l-polygon>
                                        <l-polygon
                                            v-for="data in polygonMap.polygonDao"
                                            :key="data.id"
                                            :lat-lngs="data.latlngs"
                                            :options="polygonMap.polygon.style"
                                        >
                                        </l-polygon>
                                        <l-marker
                                            v-for="(
                                                mark, index
                                            ) in polygonMap.markers"
                                            :key="index"
                                            :lat-lng="mark.position"
                                        >
                                            <l-icon class-name="someExtraClass">
                                                <div class="headline">
                                                    {{ mark.text }}
                                                </div>
                                            </l-icon>
                                        </l-marker>
                                        <!-- Areas -->
                                        <l-marker
                                            v-for="area in areasOnMap"
                                            :key="area.id"
                                            :lat-lng="[area.lat, area.lng]"
                                            :icon="
                                                getAreaIcon(
                                                    area,
                                                    area.showCount
                                                )
                                            "
                                            @click="areaSelected = area.id"
                                        >
                                        </l-marker>
                                    </div>
                                    <!-- Cameras -->
                                    <l-marker
                                        v-for="camera in listCameraOnMap"
                                        :key="camera.id"
                                        :ref="'marker-' + camera.id"
                                        :lat-lng="[
                                            camera.latitude,
                                            camera.longitude,
                                        ]"
                                        :icon="
                                            getCameraIcon(
                                                camera,
                                                cameraSelectedId === camera.id
                                            )
                                        "
                                        @click="loadEventsByDevice(camera.id)"
                                    >
                                        <l-tooltip
                                            v-if="
                                                enableTooltip &&
                                                camera.status == 1
                                            "
                                            :ref="'tooltip-' + camera.id"
                                            :options="{
                                                interactive: true,
                                                direction: 'left',
                                                offset: [-10, -20],
                                                permanent: false,
                                                opacity: enableTooltip ? 1 : 0,
                                                className: enableTooltip
                                                    ? 'tooltip-enabled'
                                                    : 'tooltip-disabled',
                                            }"
                                        >
                                            <div class="custom-tooltip">
                                                <div class="tooltip-title">
                                                    {{ camera.name }}
                                                </div>
                                                <!-- tooltip sự kiện người -->
                                                <div
                                                    v-if="
                                                        camera.eventTypeId ==
                                                        200
                                                    "
                                                    class="tooltip-button-group"
                                                >
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-primary"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countFace
                                                            "
                                                        >
                                                            {{
                                                                camera.countFace
                                                            }}
                                                        </span>
                                                        <span v-else>0</span>
                                                    </b-button>
                                                </div>
                                                <!-- tooltip sự kiện xe -->
                                                <div
                                                    v-else-if="
                                                        camera.eventTypeId ==
                                                        300
                                                    "
                                                    class="tooltip-button-group"
                                                >
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-success"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countVehicleIn
                                                            "
                                                            >{{
                                                                camera.countVehicleIn
                                                            }}</span
                                                        >
                                                        <span v-else>0</span>
                                                    </b-button>
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-danger"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countVehicleOut
                                                            "
                                                            >{{
                                                                camera.countVehicleOut
                                                            }}</span
                                                        >
                                                        <span v-else>0</span>
                                                    </b-button>
                                                </div>
                                                <!-- tooltip sự kiện vùng cấm -->
                                                <div
                                                    v-else-if="
                                                        camera.eventTypeId ==
                                                        400
                                                    "
                                                    class="tooltip-button-group"
                                                >
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-success"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countPerson
                                                            "
                                                            >{{
                                                                camera.countPerson
                                                            }}</span
                                                        >
                                                        <span v-else>0</span>
                                                    </b-button>
                                                </div>
                                                <!-- tooltip sự kiện cháy khói -->
                                                <div
                                                    v-else-if="
                                                        camera.eventTypeId ==
                                                        500
                                                    "
                                                    class="tooltip-button-group"
                                                >
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-danger"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countFire
                                                            "
                                                            >{{
                                                                camera.countFire
                                                            }}</span
                                                        >
                                                        <span v-else>0</span>
                                                    </b-button>
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-secondary"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countSmoke
                                                            "
                                                            >{{
                                                                camera.countSmoke
                                                            }}</span
                                                        >
                                                        <span v-else>0</span>
                                                    </b-button>
                                                </div>
                                                <!-- tooltip sự kiện băng chuyền -->
                                                <!-- <div
                                                    class="tooltip-button-group"
                                                    v-else-if="
                                                        camera.eventTypeId ==
                                                        601
                                                    "
                                                >
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-warning"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countTorn
                                                            "
                                                        >
                                                            {{
                                                                camera.countTorn
                                                            }}</span
                                                        >
                                                        <span v-else>0</span>
                                                    </b-button>
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-danger"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countOverflow
                                                            "
                                                        >
                                                            {{
                                                                camera.countOverflow
                                                            }}</span
                                                        >
                                                        <span v-else>0</span>
                                                    </b-button>
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-secondary"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countDeviated
                                                            "
                                                        >
                                                            {{
                                                                camera.countDeviated
                                                            }}</span
                                                        >
                                                        <span v-else>0</span>
                                                    </b-button>
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-primary"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countOversized
                                                            "
                                                        >
                                                            {{
                                                                camera.countOversized
                                                            }}</span
                                                        >
                                                        <span v-else>0</span>
                                                    </b-button>
                                                </div> -->
                                                <!-- tooltip sự kiện đồ bảo hộ -->
                                                <!-- <div
                                                    class="tooltip-button-group"
                                                    v-else-if="
                                                        camera.eventTypeId ==
                                                        205
                                                    "
                                                >
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-success"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countEnoughProtection
                                                            "
                                                        >
                                                            {{
                                                                camera.countEnoughProtection
                                                            }}
                                                        </span>
                                                        <span v-else>0</span>
                                                    </b-button>
                                                    <b-button
                                                        size="sm"
                                                        variant="outline-danger"
                                                        class="tooltip-button"
                                                    >
                                                        <span
                                                            v-if="
                                                                camera.countLackProtection
                                                            "
                                                        >
                                                            {{
                                                                camera.countLackProtection
                                                            }}
                                                        </span>
                                                        <span v-else>0</span>
                                                    </b-button>
                                                </div> -->
                                            </div>
                                        </l-tooltip>
                                    </l-marker>
                                </l-map>

                                <!-- Thông tin camera (nằm trong map container) -->
                                <CameraInfoPanel
                                    :show-card="showCard"
                                    :device="device"
                                    :events="events"
                                    :statistic="statistic"
                                    :is-loading-more="isLoadingMore"
                                    :has-more-events="hasMoreEvents"
                                    @close="
                                        showCard = false
                                        clearActiveCamera()
                                    "
                                    @start-live="startLive"
                                    @load-more-events="loadEvents"
                                />

                                <!-- Event Modals Container -->
                                <EventModalsContainer
                                    :show-table="showTable"
                                    :statistic-all="statisticAll"
                                    :vehicles-count="vehiclesCountI18n"
                                    :protective-count="protectiveCountI18n"
                                    @close-person="
                                        showTable.person.isShow = false
                                    "
                                    @close-vehicle="
                                        showTable.vehicle.isShow = false
                                    "
                                    @close-virtual-fence="
                                        showTable.virtualFence.isShow = false
                                    "
                                    @close-fire="showTable.fire.isShow = false"
                                    @close-conveyor="
                                        showTable.conveyorBelt.isShow = false
                                    "
                                    @close-protective="
                                        showTable.protective.isShow = false
                                    "
                                />
                            </div>
                        </b-card-body>
                    </div>
                </b-col>

                <!-- Sliderbar -->
                <b-col v-if="showSidebar" xl="2" class="pl-1 h-100">
                    <statistics-sidebar
                        v-if="showStatsPanel"
                        :visible="showStatsPanel"
                        :vehicles-count="vehiclesCountI18n"
                        :statistic-all="statisticAll"
                        @close="toggleSidebar()"
                    />

                    <!-- Sidebar danh sách camera -->
                    <div
                        v-if="showCameraPanel"
                        class="custom-stats-panel h-100"
                    >
                        <template>
                            <div class="custom-stats-header">
                                <div class="stats-header-content">
                                    <div class="stats-icon-wrapper">
                                        <feather-icon
                                            icon="CameraIcon"
                                            size="16"
                                            class="stats-icon"
                                        />
                                    </div>
                                    <div class="stats-header-text">
                                        <div class="stats-title">
                                            {{
                                                $t(
                                                    'Events.MapDashboard.ListCamera'
                                                )
                                            }}
                                        </div>
                                    </div>
                                    <div class="header-actions">
                                        <b-button
                                            variant="outline-primary"
                                            size="sm"
                                            class="toggle-search-btn"
                                            :class="{
                                                'search-active':
                                                    showCameraSearch,
                                            }"
                                            :title="
                                                showCameraSearch
                                                    ? this.$i18n.locale === 'vi'
                                                        ? 'Ẩn tìm kiếm'
                                                        : 'Hide search'
                                                    : this.$i18n.locale === 'vi'
                                                      ? 'Hiện tìm kiếm'
                                                      : 'Show search'
                                            "
                                            @click="
                                                showCameraSearch =
                                                    !showCameraSearch
                                            "
                                        >
                                            <feather-icon
                                                :icon="
                                                    showCameraSearch
                                                        ? 'EyeOffIcon'
                                                        : 'SearchIcon'
                                                "
                                                size="14"
                                            />
                                        </b-button>
                                    </div>
                                </div>
                            </div>

                            <div class="camera-list-container">
                                <!-- Search and Filter box -->
                                <div
                                    v-if="showCameraSearch"
                                    class="camera-search-container"
                                >
                                    <b-input-group class="camera-search-group">
                                        <b-form-input
                                            v-model="cameraSearchText"
                                            :placeholder="
                                                this.$i18n.locale === 'vi'
                                                    ? 'Tìm kiếm ...'
                                                    : 'Search ...'
                                            "
                                            class="camera-search-input"
                                        />
                                        <b-input-group-append>
                                            <feather-icon
                                                icon="SearchIcon"
                                                size="14"
                                                class="search-icon"
                                            />
                                        </b-input-group-append>
                                    </b-input-group>

                                    <!-- Status Filter - Toggle Design -->
                                    <div class="camera-filter-compact">
                                        <div class="filter-tabs">
                                            <div
                                                class="filter-tab online"
                                                :class="{
                                                    active:
                                                        cameraStatusFilter ===
                                                        'online',
                                                    inactive:
                                                        cameraStatusFilter ===
                                                        'offline',
                                                }"
                                                @click="
                                                    toggleStatusFilter('online')
                                                "
                                            >
                                                <div
                                                    class="status-dot online-dot"
                                                    :class="{
                                                        'dot-active':
                                                            cameraStatusFilter ===
                                                            'online',
                                                        'dot-inactive':
                                                            cameraStatusFilter ===
                                                            'offline',
                                                    }"
                                                ></div>
                                                <span class="tab-text"
                                                    >Online</span
                                                >
                                            </div>
                                            <div
                                                class="filter-tab offline"
                                                :class="{
                                                    active:
                                                        cameraStatusFilter ===
                                                        'offline',
                                                    inactive:
                                                        cameraStatusFilter ===
                                                        'online',
                                                }"
                                                @click="
                                                    toggleStatusFilter(
                                                        'offline'
                                                    )
                                                "
                                            >
                                                <div
                                                    class="status-dot offline-dot"
                                                    :class="{
                                                        'dot-active':
                                                            cameraStatusFilter ===
                                                            'offline',
                                                        'dot-inactive':
                                                            cameraStatusFilter ===
                                                            'online',
                                                    }"
                                                ></div>
                                                <span class="tab-text"
                                                    >Offline</span
                                                >
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Lặp qua từng khu vực -->
                                <div
                                    v-for="area in filteredGroupedCams"
                                    :key="area.id"
                                    class="area-group"
                                >
                                    <!-- Header khu vực -->
                                    <div
                                        class="area-header"
                                        @click="toggleArea(area.id)"
                                    >
                                        <div class="area-header-content">
                                            <feather-icon
                                                icon="MapPinIcon"
                                                size="16"
                                                class="area-icon"
                                            />
                                            <span class="area-name">{{
                                                area.name
                                            }}</span>
                                            <div class="camera-count">
                                                {{ area.cameras.length }}
                                            </div>
                                        </div>
                                        <feather-icon
                                            :icon="
                                                expandedAreas.includes(area.id)
                                                    ? 'ChevronDownIcon'
                                                    : 'ChevronRightIcon'
                                            "
                                            size="16"
                                            class="expand-icon"
                                            :class="{
                                                expanded:
                                                    expandedAreas.includes(
                                                        area.id
                                                    ),
                                            }"
                                        />
                                    </div>

                                    <!-- Danh sách camera -->
                                    <b-collapse
                                        :visible="
                                            expandedAreas.includes(area.id)
                                        "
                                        class="area-collapse"
                                    >
                                        <div class="camera-list">
                                            <div
                                                v-for="cam in area.cameras"
                                                :key="cam.id + cam.code"
                                                class="camera-item"
                                                :class="{
                                                    'camera-selected':
                                                        cameraSelectedId ===
                                                        cam.id,
                                                    'camera-offline':
                                                        cam.status === 0,
                                                    'camera-online':
                                                        cam.status === 1,
                                                }"
                                                @click="selectCam(cam)"
                                            >
                                                <div class="camera-content">
                                                    <div
                                                        class="camera-status-indicator"
                                                    ></div>
                                                    <feather-icon
                                                        icon="VideoIcon"
                                                        size="14"
                                                        class="camera-icon"
                                                    />
                                                    <span class="camera-name">{{
                                                        cam.name
                                                    }}</span>
                                                    <feather-icon
                                                        v-if="
                                                            selectedCameras.includes(
                                                                cam.id
                                                            )
                                                        "
                                                        icon="CheckCircleIcon"
                                                        size="14"
                                                        class="check-icon"
                                                    />
                                                </div>
                                            </div>
                                        </div>
                                    </b-collapse>
                                </div>
                            </div>
                        </template>
                    </div>
                </b-col>
            </b-row>
        </b-container>

        <!-- Modal video -->
        <b-modal
            v-model="showModal"
            size="xl"
            hide-footer
            header-bg-variant="secondary"
        >
            <template #modal-header="{ close }">
                <h4 class="mb-0">{{ device.text }}</h4>
                <b-button class="btn-danger" size="sm" @click="close()">
                    <feather-icon icon="XIcon" size="16" />
                </b-button>
            </template>
            <b-card-group class="mb-0">
                <b-card
                    img-top
                    no-body
                    @contextmenu.prevent="$refs.menu.open($event, 1)"
                >
                    <FlvPlayer
                        ref="liveStream"
                        :video-index="1"
                        :source="videoSource"
                    >
                        <canvas
                            id="video1"
                            class="card-img-top"
                            width="900"
                            height="400"
                        ></canvas>
                    </FlvPlayer>
                </b-card>
            </b-card-group>
        </b-modal>
    </div>
</template>

<script>
import {
    LTooltip,
    LMap,
    LTileLayer,
    LMarker,
    LPolygon,
    LIcon,
    LControl,
    LImageOverlay,
} from 'vue2-leaflet'
import 'leaflet/dist/leaflet.css'
import openstreetmap from '@/data/openstreetmap.json'
import L from 'leaflet'

const simpleCRS = L.CRS.Simple

import getBaseUrl from '@/utils/get-baseUrl'
import signalRService from '@/utils/signalr-service'
import TreeHelper from '@/utils/treeHelper'
import useAppConfig from '@core/app-config/useAppConfig'
import { computed } from '@vue/composition-api'
import helper from '@/utils/utils.js'
import CameraInfoPanel from './CameraInfoPanel.vue'
import EventModalsContainer from './components/EventModalsContainer.vue'
import StatisticsSidebar from './StatisticsSidebar/StatisticsSidebar.vue'

export default {
    components: {
        LTooltip,
        LMap,
        LTileLayer,
        LMarker,
        LPolygon,
        LIcon,
        LControl,
        LImageOverlay,
        CameraInfoPanel,
        EventModalsContainer,
        StatisticsSidebar,
    },
    props: {
        url: {
            type: String,
            default: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
        },
        coordinate: {
            type: Object,
            default: () => ({
                lat: 15.3842,
                lng: 108.79383,
                zoom: 16,
            }),
        },
        isShowImgMap: {
            type: Boolean,
            default: false,
        },
        urlImgOverlay: {
            type: String,
            default: null,
        },
    },
    setup() {
        const nodeMediaServer = process.env.VUE_APP_NODE_MEDIA_SERVER

        // Sử dụng app config để lấy thông tin layout - CHỈ SỬ DỤNG CÁC GIỮA TRỊ CÓ SẴN
        const {
            navbarType,
            footerType,
            breadcrumbType,
            contentWidth,
            isVerticalMenuCollapsed,
            layoutType,
            isNavMenuHidden,
        } = useAppConfig()

        // Tính toán chiều cao thực tế của map wrapper
        const mapHeight = computed(() => {
            // Chiều cao navbar (dựa vào type)
            let navbarHeight = 0
            if (navbarType.value === 'floating') {
                navbarHeight = 75
                // floating navbar height + margin
            } else if (
                navbarType.value === 'sticky' ||
                navbarType.value === 'static'
            ) {
                navbarHeight = 70 // standard navbar height
            } else if (navbarType.value === 'hidden') {
                navbarHeight = 0
            }

            // Chiều cao footer (dựa vào type)
            let footerHeight = 0
            if (footerType.value === 'sticky') {
                footerHeight = 60 // sticky footer height
            } else if (footerType.value === 'static') {
                footerHeight = 55 // static footer height
            } else if (footerType.value === 'hidden') {
                footerHeight = 0
            }

            // Chiều cao breadcrumb
            let breadcrumbHeight = 0
            if (breadcrumbType.value && breadcrumbType.value !== 'hidden') {
                breadcrumbHeight = 50 // breadcrumb height nếu hiển thị
            }

            // Content padding - SỬ DỤNG LOGIC ĐƠN GIẢN HƠN
            // Dựa vào contentWidth và layout type để tính padding
            let contentPadding = 32 // default 2rem = 32px
            if (contentWidth.value === 'boxed') {
                contentPadding = 24 // 1.5rem = 24px cho boxed layout
            }
            if (isVerticalMenuCollapsed.value) {
                contentPadding -= 8 // giảm padding khi menu collapsed
            }

            // Tính toán chiều cao cuối cùng
            const totalDeduction =
                navbarHeight + footerHeight + breadcrumbHeight + contentPadding

            // Xác định chiều cao dựa vào layout (vertical/horizontal)
            let vhValue = 98
            if (layoutType.value === 'vertical') {
                vhValue = 99
            } else if (layoutType.value === 'horizontal') {
                vhValue = 92
                if (isNavMenuHidden.value) {
                    vhValue = 99 // Giữ nguyên nếu menu ẩn
                }
            }

            const result = `calc(${vhValue}vh - ${totalDeduction}px)`

            // Trả về 100% chiều cao của viewport trừ đi các phần đã trừ
            return result
        })

        return {
            mapHeight,
            nodeMediaServer,
        }
    },
    data() {
        return {
            simpleCRS, // ✅ FIX: Thêm simpleCRS để map sử dụng CRS.Simple cho image overlay
            treeSelectOpen: false, // Track tree-select open/close state
            showTable: {
                person: { id: 200, isShow: false },
                vehicle: { id: 300, isShow: false },
                virtualFence: { id: 400, isShow: false },
                fire: { id: 500, isShow: false },
                conveyorBelt: { id: 601, isShow: false },
                protective: { id: 205, isShow: false },
            },
            currentUserCompId: null,
            showCameraByArea: false,
            statsButtonActive: true, // Trạng thái active của nút thống kê - mặc định hiển thị
            cameraListButtonActive: false, // Trạng thái active của nút danh sách camera
            cameraSearchText: '', // Thêm biến để search camera
            cameraStatusFilter: 'all', // Thêm biến để filter theo status: all, online, offline
            showCameraSearch: false, // Thêm biến để show/hide khung tìm kiếm - mặc định ẩn
            internalShowImgMap: false,
            internalUrlImgOverlay: null,
            imageBounds: null,
            showModal: false,
            videoSource: null,
            polygonMap: openstreetmap,
            urlOSM: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
            vehiclesCount: [
                {
                    id: 1,
                    code: 'Moto',
                    name: 'Xe máy',
                    in: 0,
                    out: 0,
                    imgUrl: '/icon/moto.svg',
                },
                {
                    id: 2,
                    code: 'Car2',
                    name: 'Xe tải',
                    in: 0,
                    out: 0,
                    imgUrl: '/icon/car2.svg',
                },
                {
                    id: 3,
                    code: 'Car5',
                    name: '5 chỗ',
                    in: 0,
                    out: 0,
                    imgUrl: '/icon/car5.svg',
                },
                {
                    id: 4,
                    code: 'Car7',
                    name: '7 chỗ',
                    in: 0,
                    out: 0,
                    imgUrl: '/icon/car7.svg',
                },
                {
                    id: 5,
                    code: 'Car9',
                    name: '9 chỗ',
                    in: 0,
                    out: 0,
                    imgUrl: '/icon/car9.svg',
                },
                {
                    id: 6,
                    code: 'Car16',
                    name: '16 chỗ',
                    in: 0,
                    out: 0,
                    imgUrl: '/icon/car16.svg',
                },
                {
                    id: 7,
                    code: 'Car29',
                    name: '29 chỗ',
                    in: 0,
                    out: 0,
                    imgUrl: '/icon/car29.svg',
                },
                {
                    id: 8,
                    code: 'Car40',
                    name: '40 chỗ',
                    in: 0,
                    out: 0,
                    imgUrl: '/icon/car40.svg',
                },
            ],
            protectiveCount: [
                {
                    id: 1,
                    code: 'Enough',
                    name: 'Không',
                    count: 0,
                },
                {
                    id: 2,
                    code: 'Helmet',
                    name: 'Mũ',
                    count: 0,
                },
                {
                    id: 3,
                    code: 'FaceMask',
                    name: 'Khẩu trang',
                    count: 0,
                },
                {
                    id: 4,
                    code: 'Gloves',
                    name: 'Găng tay',
                    count: 0,
                },
                {
                    id: 5,
                    code: 'HelmetGloves',
                    name: 'Mũ + găng tay',
                    count: 0,
                },
                {
                    id: 6,
                    code: 'HelmetMask',
                    name: 'Mũ + khẩu trang',
                    count: 0,
                },
                {
                    id: 7,
                    code: 'MaskGloves',
                    name: 'Găng + khẩu trang',
                    count: 0,
                },
                {
                    id: 8,
                    code: 'MissingEverything',
                    name: 'Mũ + găng tay + khẩu trang',
                    count: 0,
                },
            ],
            icon: {
                camOn: L.icon({
                    iconUrl: '/map/camera-on.png',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                }),
                camOff: L.icon({
                    iconUrl: '/map/camera-off.png',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                }),
                camWarning: L.icon({
                    iconUrl: '/map/camera-warning.png',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                    className: 'blink',
                }),
                camFireWarning: L.icon({
                    iconUrl: '/map/fire-warning.png',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                    className: 'blink',
                }),
                camForbiddenArea: L.icon({
                    iconUrl: '/map/forbidden-area.png',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                    className: 'blink',
                }),
                camConveyorBelt: L.icon({
                    iconUrl: '/map/conveyor-belt.png',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                    className: 'blink',
                }),
                camPersonWarning: L.icon({
                    iconUrl: '/map/person-warning.png',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                    className: 'blink',
                }),
                camCarWarning: L.icon({
                    iconUrl: '/map/car-warning.png',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                    className: 'blink',
                }),
                point: L.icon({
                    iconUrl: '/map/point.png',
                    iconSize: [30, 30],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                }),
                pointWarning: L.icon({
                    iconUrl: '/map/point-warning.png',
                    iconSize: [30, 30],
                    iconAnchor: [10, 10],
                    tooltipAnchor: [0, -10],
                    className: 'blink',
                }),
            },
            device: {
                code: null,
                name: null,
                link: null,
                areaId: null,
                serverId: null,
                eventTypeId: null,
                status: null,
                compId: null,
                license: null,
                zoom: null,
            },
            statisticAll: {
                countFaceNotRegister: null,
                countFaceRegister: null,
                countEnoughProtection: null,
                countLackProtection: null,
                countFire: null,
                countSmoke: null,
                countPerson: null,
                countVehicle: null,
                countVehicleOut: null,
                countVehicleIn: null,
                countTooHigh: null,
                countDeviated: null,
                countOversized: null,
                countOverflow: null,
                countTorn: null,
            },
            statistic: {
                countCarIn: null,
                countCarOut: null,
                countMotoIn: null,
                countMotoOut: null,
                countEnoughProtection: null,
                countLackProtection: null,
                countFire: null,
                countSmoke: null,
                countPerson: null,
                countVehicle: null,
                countTooHigh: null,
                countOverflow: null,
                countTorn: null,
            },
            cameraSelectedId: null,
            listCamera: [],
            listCameraByEventType: [],
            listCameraOnMap: [],
            areasTree: [],
            areasOnMap: [],
            areaSelected: null,
            showCard: false,
            events: [],
            rootMapCoordinate: {
                lat: 15.3842,
                lng: 108.79383,
                zoom: 16,
            },
            // Add these new properties for pagination
            currentPage: 1,
            isLoadingMore: false,
            hasMoreEvents: true,
            itemsPerPage: 10,
            selectedDeviceId: null,

            listEventType: [],
            listEventTypeByArea: [],
            listAreaFuntion: [],
            eventTypeSelected: null,

            expandedAreas: [], // Lưu ID các khu vực được mở rộng trong sidebar
            selectedCameras: [], // Lưu ID các camera đã chọn
            cameraSelectedId: null, // ID camera đang được selected để hiển thị hiệu ứng
            listCams: [], // Danh sách tất cả camera
            devicePermissions: [], // Dữ liệu quyền truy cập camera
            enableTooltip: true,
            countAllEvent: [], // Dữ liệu đếm tất cả sự kiện
        }
    },
    computed: {
        imgUrl() {
            return getBaseUrl()
        },

        // Merge props và internal state cho image map
        actualShowImgMap() {
            // Ưu tiên props từ parent, sau đó mới đến internal state
            return this.isShowImgMap || this.internalShowImgMap
        },

        actualUrlImgOverlay() {
            // Ưu tiên props từ parent, sau đó mới đến internal state
            return this.urlImgOverlay || this.internalUrlImgOverlay
        },

        // Computed center - điều chỉnh center phù hợp với CRS
        mapCenter() {
            // Nếu dùng image overlay và có imageBounds, center ở giữa ảnh
            if (this.actualShowImgMap && this.imageBounds && this.imageBounds.length === 2) {
                const imageHeight = this.imageBounds[1][0]
                const imageWidth = this.imageBounds[1][1]
                return [imageHeight / 2, imageWidth / 2]
            }
            
            // Nếu có tọa độ từ company, ưu tiên sử dụng
            if (this.rootMapCoordinate) {
                return [this.rootMapCoordinate.lat, this.rootMapCoordinate.lng]
            }
            
            // Dùng coordinate từ props
            return [this.coordinate.lat, this.coordinate.lng]
        },

        // Computed zoom - điều chỉnh zoom phù hợp với CRS
        mapZoom() {
            // Nếu dùng image overlay, zoom nhỏ hơn để thấy được ảnh
            if (this.actualShowImgMap) {
                return 0 // Zoom 0 cho CRS.Simple
            }

            // Nếu có zoom từ company, ưu tiên sử dụng
            if (this.rootMapCoordinate) {
                return this.rootMapCoordinate.zoom
            }

            return this.coordinate.zoom // Zoom từ props cho OSM
        },

        showSidebar() {
            return this.statsButtonActive
        },

        // Computed property để xác định hiển thị phần nào
        showStatsPanel() {
            return this.statsButtonActive
        },

        // Computed property cho vehiclesCount với i18n
        vehiclesCountI18n() {
            return this.vehiclesCount.map((vehicle) => ({
                ...vehicle,
                name: this.getVehicleName(vehicle.code),
            }))
        },

        // Computed property cho protectiveCount với i18n
        protectiveCountI18n() {
            return this.protectiveCount.map((protective) => ({
                ...protective,
                name: this.getProtectiveName(protective.code),
            }))
        },

        // Computed property để filter camera theo search text và status
        filteredGroupedCams() {
            let filteredAreas = this.groupedCams

            // Filter theo search text
            if (this.cameraSearchText) {
                const searchText = this.cameraSearchText.toLowerCase().trim()
                filteredAreas = filteredAreas.map((area) => ({
                    ...area,
                    cameras: area.cameras.filter(
                        (cam) =>
                            cam.name.toLowerCase().includes(searchText) ||
                            cam.code?.toLowerCase().includes(searchText)
                    ),
                }))
            }

            // Filter theo status
            if (this.cameraStatusFilter !== 'all') {
                filteredAreas = filteredAreas.map((area) => ({
                    ...area,
                    cameras: area.cameras.filter((cam) => {
                        if (this.cameraStatusFilter === 'online') {
                            return cam.status === 1
                        }
                        if (this.cameraStatusFilter === 'offline') {
                            return cam.status === 0
                        }
                        return true
                    }),
                }))
            }

            // Chỉ trả về các area có camera
            return filteredAreas.filter((area) => area.cameras.length > 0)
        },

        // Computed property để xác định có hiển thị sidebar không
        showSidebar() {
            return this.statsButtonActive || this.cameraListButtonActive
        },

        // Computed property để xác định hiển thị phần nào
        showStatsPanel() {
            return this.statsButtonActive && !this.cameraListButtonActive
        },

        showCameraPanel() {
            return this.cameraListButtonActive
        },

        groupedCams() {
            const areasMap = {}
            const groupedArray = helper.overlapArray(
                (x, y) => x.id === y.deviceId && y.isChecked
            )(this.listCams, this.devicePermissions)
            groupedArray.forEach((cam) => {
                const { areaId } = cam
                if (!areasMap[areaId]) {
                    areasMap[areaId] = {
                        id: areaId,
                        name: cam.areaName,
                        cameras: [],
                    }
                }
                areasMap[areaId].cameras.push(cam)
            })
            return Object.values(areasMap)
        },
    },
    watch: {
        areaSelected(newVal) {
            if (this.eventTypeSelected == null && newVal != null) {
                const eventTypeByArea = this.listAreaFuntion
                    .filter((x) => x.areaId === newVal)
                    .map((x) => x.functionId.toString())
                this.listEventTypeByArea = this.listEventType.filter((x) =>
                    eventTypeByArea.includes(x.id)
                )
            } else {
                this.listEventTypeByArea = this.listEventType
            }
            this.cameraSelectedId = null
            this.showCard = false
            this.selectArea(newVal)
            this.loadAllEventByArea(newVal || 0)
            
            // Nếu không có area nào được chọn, fit lại toàn bộ map
            if (!newVal && this.actualShowImgMap && this.imageBounds) {
                this.$nextTick(() => {
                    const map = this.$refs.latLngMap?.mapObject
                    if (map) {
                        map.fitBounds(this.imageBounds, {
                            padding: [5, 5],
                            animate: true
                        })
                    }
                })
            }
            
            setTimeout(() => this.onZoomEnd(), 100)
        },
        eventTypeSelected() {
            this.clearActiveCamera()
            // ✅ CHỈ filter cameras, KHÔNG zoom map lại
            this.getDevicesByArea(this.areaSelected)
            setTimeout(() => this.onZoomEnd(), 100)
        },
    },
    async created() {
        const vm = this
        const accessToken = this.$services.getUserData()
        vm.currentUserCompId = accessToken.companyId
        
        // Kiểm tra localStorage trước nếu không có props
        if (!this.isShowImgMap && !this.urlImgOverlay) {
            const savedMapImage = localStorage.getItem('companyMapImage')
            if (savedMapImage) {
                this.internalUrlImgOverlay = savedMapImage
                this.internalShowImgMap = true
                this.calculateImageBounds(this.internalUrlImgOverlay)
            }
        }
        
        await this.loadCompanyMap()
        
        await vm.loadCams()
        await vm.loadDevicePermissions()
        await vm.getCountAllEvent()
        vm.loadAllEventByArea(0)
        vm.loadAreasTree()
        vm.loadEventType()
        vm.loadAreaFuntion()
        
        // Load thống kê ban đầu từ API để không về 0 khi refresh
        console.log('📊 [CREATED] Bắt đầu load thống kê ban đầu...')
        try {
            await vm.loadStatisticFireSmoke(0, 0)  // Load thống kê cháy/khói cho toàn bộ
            console.log('📊 [CREATED] ✅ loadStatisticFireSmoke done')
        } catch (e) { console.error('📊 [CREATED] ❌ loadStatisticFireSmoke error:', e) }
        
        try {
            await vm.loadStatisticVirtualFence(0, 0)  // Load thống kê vùng cấm cho toàn bộ
            console.log('📊 [CREATED] ✅ loadStatisticVirtualFence done')
        } catch (e) { console.error('📊 [CREATED] ❌ loadStatisticVirtualFence error:', e) }
        
        try {
            await vm.loadCountVehicleTable(0)  // Load thống kê phương tiện cho toàn bộ
            console.log('📊 [CREATED] ✅ loadCountVehicleTable done')
        } catch (e) { console.error('📊 [CREATED] ❌ loadCountVehicleTable error:', e) }
        
        try {
            await vm.loadCountProtectiveTable(0)  // Load thống kê đồ bảo hộ cho toàn bộ
            console.log('📊 [CREATED] ✅ loadCountProtectiveTable done')
        } catch (e) { console.error('📊 [CREATED] ❌ loadCountProtectiveTable error:', e) }
        
        console.log('📊 [CREATED] Hoàn thành load thống kê ban đầu!')
        // Không được phép ghi đè bằng coordinate props nếu đã tải thành công tọa độ từ company
        if (!vm.rootMapCoordinate) {
            vm.rootMapCoordinate = {
                lat: vm.coordinate.lat,
                lng: vm.coordinate.lng,
                zoom: vm.coordinate.zoom,
            }
        }
        const rest = vm.loadDevices()
        rest.then((response) => {
            const listCamera = {}
            const defaultCount = {
                countFace: 0,
                countFaceRegister: 0,
                countFaceNotRegister: 0,
                countFire: 0,
                countSmoke: 0,
                countPerson: 0,
                countVehicle: 0,
                countVehicleOut: 0,
                countVehicleIn: 0,
                countTooHigh: 0,
                countDeviated: 0,
                countOversized: 0,
                countOverflow: 0,
                countTorn: 0,
            }
            response.data.data.forEach((item) => {
                const count = vm.countAllEvent.find(
                    (x) => x.deviceId === item.id
                )
                Object.assign(item, count || defaultCount)
                if (!listCamera[item.areaId]) {
                    listCamera[item.areaId] = []
                }
                listCamera[item.areaId].push({
                    ...item,
                    blink: false,
                    selected: false,
                })
            })
            vm.listCamera = listCamera
            vm.listCameraByEventType = response.data.data.map((item) => {
                const count = vm.countAllEvent.find(
                    (x) => x.deviceId === item.id
                )
                return {
                    ...item,
                    ...(count || defaultCount), // nếu count là undefined thì vẫn an toàn
                }
            })
            
            // ✅ Gọi getDevicesByArea để khởi tạo cameras hiển thị
            vm.getDevicesByArea(vm.areaSelected)
            
            // Sau đó mới gọi onZoomEnd để cập nhật areas
            setTimeout(() => vm.onZoomEnd(), 300)
        })

        console.log('🔌 Đang kết nối SignalR...')
        await signalRService.connect('notificationHub')
        console.log('✅ SignalR đã kết nối thành công')
        
        // ✅ DEBUG: Log tất cả events từ SignalR
        console.log('📡 Đang lắng nghe các SignalR events...')
        
        // ✅ LẮNG NGHE EVENT: NewEvent (full event data với EventTypeId)
        signalRService.on('NewEvent', (data) => {
            console.log('📡 [NewEvent] Nhận Event từ SignalR:', data)
            if (data) {
                const dataJson = JSON.parse(data)
                console.log('📊 [NewEvent] Event data đã parse:', dataJson)
                this.showWarningOnMap(dataJson)
            }
        })
        
        // ✅ LẮNG NGHE NOTIFICATION: ReceiveNotification
        signalRService.on('ReceiveNotification', (data) => {
            console.log('🔔 [ReceiveNotification] Nhận Notification từ SignalR:', data)
            if (data) {
                // Parse nếu là string, hoặc dùng trực tiếp nếu là object/array
                let notifications = typeof data === 'string' ? JSON.parse(data) : data
                
                // Nếu là array, lấy phần tử đầu tiên
                if (Array.isArray(notifications)) {
                    console.log('📋 [ReceiveNotification] Array length:', notifications.length)
                    notifications = notifications[0]
                }
                
                console.log('📊 [ReceiveNotification] Notification đã parse:', notifications)
                
                // ✅ Notification ĐÃ CÓ eventTypeId, AreaId, DeviceId
                if (notifications && notifications.AreaId && notifications.DeviceId) {
                    // Tạo object giống Event format để dùng lại showWarningOnMap
                    const eventData = {
                        EventTypeId: notifications.EventTypeId || 400, // Dùng từ notification hoặc default 400
                        AreaId: notifications.AreaId,
                        DeviceId: notifications.DeviceId,
                        EventId: notifications.EventId,
                        CompId: this.currentUserCompId
                    }
                    
                    console.log('🎯 [ReceiveNotification] Chuyển đổi Notification thành Event format:', eventData)
                    this.showWarningOnMap(eventData)
                } else {
                    console.warn('❌ [ReceiveNotification] Notification thiếu AreaId hoặc DeviceId:', notifications)
                }
            }
        })
    },
    mounted() {
        // ✅ BỎ event listener zoomend - không cần thay đổi cameras theo zoom nữa
        // Cameras chỉ được quản lý bởi getDevicesByArea() dựa trên filter area/function
        // this.$nextTick(() => {
        //     this.$refs.latLngMap.mapObject.on('zoomend', this.onZoomEnd)
        // })
    },
    methods: {
        // ==================== Image Map Methods ====================

        calculateImageBounds(imageUrl) {
            const img = new Image()
            
            img.onload = () => {
                const w = img.naturalWidth
                const h = img.naturalHeight
                
                // CRS.Simple: bounds = [[minY, minX], [maxY, maxX]]
                this.imageBounds = [[0, 0], [h, w]]
                
                // Sử dụng requestAnimationFrame thay vì setTimeout
                // Đảm bảo browser đã paint xong trước khi tính toán
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => {
                        this.$nextTick(() => {
                            this.fitImageToMap(w, h)
                        })
                    })
                })
            }
            
            img.onerror = () => {
                console.error('❌ Failed to load map image:', imageUrl)
                this.imageBounds = [[0, 0], [100, 100]]
            }
            
            img.src = imageUrl
        },

        /**
         * Fit image overlay to map viewport
         * Tự động căn giữa ảnh trong viewport
         * Tách riêng logic để dễ test và maintain
         */
        fitImageToMap(imageWidth, imageHeight, retryCount = 0) {
            const map = this.$refs.latLngMap?.mapObject
            if (!map) {
                console.warn('⚠️ Map not available for fitting image bounds')
                return
            }
            
            const container = map.getContainer()
            if (!container) {
                console.warn('⚠️ Map container not available')
                return
            }

            const viewportWidth = container.clientWidth
            const viewportHeight = container.clientHeight
            
            // Invalidate size to ensure leaflet knows current dimensions
            map?.invalidateSize()
            
            // Validate dimensions - viewport phải có kích thước hợp lý
            if (!viewportWidth || !viewportHeight || viewportHeight < 100) {
                // Retry tối đa 10 lần
                if (retryCount < 10) {
                    requestAnimationFrame(() => this.fitImageToMap(imageWidth, imageHeight, retryCount + 1))
                }
                return
            }
            
            // Kiểm tra xem có khu vực được chọn không
            if (this.areaSelected) {
                // Có khu vực được chọn -> Fit theo viewport của khu vực đó
                const findAreaById = (areas, id) => {
                    return areas.reduce((acc, area) => {
                        if (area.id === id) return area
                        if (area.children) {
                            const found = findAreaById(area.children, id)
                            if (found) return found
                        }
                        return acc
                    }, null)
                }
                
                const selectedArea = findAreaById(this.areasTree, this.areaSelected)
                if (selectedArea && selectedArea.coordinates) {
                    try {
                        const coords = JSON.parse(selectedArea.coordinates)
                        const [lat, lng, zoomLevel] = coords
                        
                        // Set view theo tọa độ khu vực
                        map.setView([lat, lng], zoomLevel || 3, {
                            animate: false
                        })
                        
                        return
                    } catch (e) {
                        console.error('❌ Error parsing area coordinates:', e)
                    }
                }
            }
            
            // Không có khu vực được chọn -> Fit theo toàn cục
            // Tính zoom để toàn bộ ảnh vừa viewport
            const scaleW = viewportWidth / imageWidth
            const scaleH = viewportHeight / imageHeight
            const minScale = Math.min(scaleW, scaleH) * 0.95 // 95% để có margin nhỏ
            const minZoom = Math.log2(minScale)
            
            // Đặt minZoom để không zoom out quá nhỏ
            map.setMinZoom(minZoom)
            
            // Fit toàn bộ ảnh vào viewport, ảnh nằm giữa
            map.fitBounds(this.imageBounds, {
                padding: [5, 5], // Padding rất nhỏ
                animate: false
            })
        },

        // ==================== Zoom Control Methods ====================

        /**
         * Zoom in map
         */
        handleZoomIn() {
            const map = this.$refs.latLngMap?.mapObject
            if (!map) return
            
            map.zoomIn()
        },

        /**
         * Zoom out map
         */
        handleZoomOut() {
            const map = this.$refs.latLngMap?.mapObject
            if (!map) return
            
            map.zoomOut()
        },

        /**
         * Reset zoom về mức ban đầu và về tọa độ gốc
         */
        handleZoomReset() {
            const map = this.$refs.latLngMap?.mapObject
            if (!map) return
            
            if (this.actualShowImgMap && this.imageBounds && this.internalUrlImgOverlay) {
                // Image map: fit về ảnh
                this.calculateImageBounds(this.internalUrlImgOverlay)
            } else {
                // Normal map: reset về tọa độ gốc
                this.resetMap()
            }
        },

        // Toggle status filter method
        toggleStatusFilter(status) {
            if (this.cameraStatusFilter === status) {
                // If clicking the active filter, deactivate it (show all)
                this.cameraStatusFilter = 'all'
            } else {
                // Activate the clicked filter
                this.cameraStatusFilter = status
            }
        },

        // lookup data
        loadAreaFuntion() {
            this.$services.get('/lookup/area-function').then((response) => {
                const areaFunction = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
                this.listAreaFuntion = areaFunction
            })
        },
        getShowTable() {
            const { eventTypeSelected } = this
            if (eventTypeSelected) {
                for (const key in this.showTable) {
                    this.showTable[key].isShow =
                        this.showTable[key].id == eventTypeSelected
                }
            } else {
                for (const key in this.showTable) {
                    this.showTable[key].isShow = false
                }
            }
        },
        loadAllEventByArea(areaId) {
            this.loadCountVehicleTable(areaId)
            this.loadCountProtectiveTable(areaId)
            this.loadStatisticProtection(0)
            this.loadStatisticFireSmoke(0, areaId)
            this.loadStatisticPerson(0, areaId)
            this.loadStatisticVirtualFence(0, areaId)
            this.loadStatisticConveryor(0, areaId)
        },

        // Methods mới cho toggle panels
        toggleStatsPanel() {
            if (this.statsButtonActive) {
                // Nếu đang active, tắt đi
                this.statsButtonActive = false
            } else {
                // Nếu chưa active, bật lên và tắt panel kia
                this.statsButtonActive = true
                this.cameraListButtonActive = false
            }

            // Sync với biến cũ để tương tích
            this.showCameraByArea = this.cameraListButtonActive
            this.enableTooltip = false // Tắt tooltip khi mở panel
        },

        toggleCameraPanel() {
            if (this.cameraListButtonActive) {
                // Nếu đang active, tắt đi
                this.cameraListButtonActive = false
            } else {
                // Nếu chưa active, bật lên và tắt panel kia
                this.cameraListButtonActive = true
                this.statsButtonActive = false
            }

            // Sync với biến cũ để tương tích
            this.showCameraByArea = this.cameraListButtonActive
            this.enableTooltip = false // Tắt tooltip khi mở panel

            if (this.cameraListButtonActive) {
                this.loadCamerasByArea()
            }
        },

        toggleAllTooltips() {
            // ✅ CHỈ toggle tooltip, KHÔNG ảnh hưởng đến sidebar
            this.enableTooltip = !this.enableTooltip

            const vm = this
            this.listCameraOnMap.forEach((camera) => {
                const markerRef = vm.$refs[`marker-${camera.id}`]
                const marker = Array.isArray(markerRef)
                    ? markerRef[0]
                    : markerRef
                if (!marker || !marker.mapObject) return

                const leafletMarker = marker.mapObject
                if (!this.enableTooltip) {
                    // Khi tắt tooltip, đảm bảo đóng tất cả tooltip đang mở
                    leafletMarker.closeTooltip()
                    // Cập nhật lại tooltip options để vô hiệu hóa hoàn toàn
                    if (leafletMarker.getTooltip()) {
                        leafletMarker.getTooltip().setOpacity(0)
                    }
                }
            })
        },
        toggleCameraTooltips() {
            this.showCameraByArea = !this.showCameraByArea

            // Nếu bật camera list, tắt tooltip events để đảm bảo chỉ một nút active
            if (this.showCameraByArea) {
                this.enableTooltip = false
                // Đóng tất cả tooltips khi chuyển sang camera list
                const vm = this
                this.listCameraOnMap.forEach((camera) => {
                    const markerRef = vm.$refs[`marker-${camera.id}`]
                    const marker = Array.isArray(markerRef)
                        ? markerRef[0]
                        : markerRef
                    if (!marker || !marker.mapObject) return

                    const leafletMarker = marker.mapObject
                    leafletMarker.closeTooltip()
                    if (leafletMarker.getTooltip()) {
                        leafletMarker.getTooltip().setOpacity(0)
                    }
                })
            }
        },
        // Xem live
        async startLive(deviceId) {
            const vm = this
            vm.showModal = true
            vm.videoSource = `${vm.nodeMediaServer + vm.device.compId}/${
                vm.device.code
            }.flv`
        },
        async getCountAllEvent() {
            await this.$services
                .get('/dashboard/dashboardCountEvent')
                .then((response) => {
                    this.countAllEvent = response.data.data
                })
        },
        // lookup data
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                const eventType = TreeHelper.removeEmptyChildren(
                    response.data.data
                )

                this.listEventType = eventType.map((item) => {
                    const { text, ...rest } = item
                    return {
                        ...rest,
                        label: text,
                    }
                })
                this.listEventTypeByArea = this.listEventType
            })
        },
        async loadAreasTree() {
            const res = await this.$services.get('/lookup/areas-tree')

            // Hàm đệ quy để xử lý tất cả các nút trong cây
            const processNode = (node) => {
                // Xử lý tọa độ cho nút hiện tại
                if (node.coordinates) {
                    try {
                        const coords = JSON.parse(node.coordinates)
                        node.lat = coords[0] || null
                        node.lng = coords[1] || null
                        node.zoomLevel = coords[2] || null
                    } catch (e) {
                        console.error(
                            `Error parsing coordinates for ${node.name || node.id}:`,
                            e
                        )
                    }
                }

                // Xử lý đệ quy cho các nút con
                if (node.children && node.children.length > 0) {
                    node.children = node.children.map(processNode)
                }

                return node
            }

            // Áp dụng xử lý cho tất cả các nút trong cây
            let areasTree = res.data.data.map(processNode)

            // Tính toán số thiết bị cho từng nút
            const calculateDeviceCount = (node) => {
                const directCount =
                    this.listCamera && this.listCamera[node.id]
                        ? this.listCamera[node.id].length
                        : 0

                let childrenCount = 0

                if (node.children && node.children.length > 0) {
                    node.children.forEach((child) => {
                        calculateDeviceCount(child)
                        childrenCount += child.totalDevices || 0
                    })
                }

                node.totalDevices = directCount + childrenCount
                return node
            }

            // Áp dụng tính toán thiết bị
            areasTree = areasTree.map((node) =>
                calculateDeviceCount({ ...node })
            )

            this.areasTree = areasTree
            this.areasOnMap = this.areasTree
                .filter((item) => item.coordinates)
                .map((item) => ({ ...item, blink: false }))
        },
        loadDevices() {
            return this.$services.get(`/device/getAllCam`)
        },
        async loadEventsByDevice(deviceId) {
            const vm = this
            this.showCard = true
            this.cameraSelectedId = deviceId
            this.selectedDeviceId = deviceId

            // Reset pagination
            this.currentPage = 1
            this.hasMoreEvents = true
            this.events = []

            // Load first page
            await this.loadEvents()

            this.loadDeviceDetail(deviceId)
        },

        async loadEvents() {
            if (this.isLoadingMore || !this.hasMoreEvents) return

            this.isLoadingMore = true

            const pagination = {
                page: this.currentPage,
                itemsPerPage: this.itemsPerPage,
                sortBy: 'accessTime',
                deviceId: this.selectedDeviceId,
            }

            try {
                const formData = new URLSearchParams(pagination).toString()
                const response = await this.$services.get(`/event?${formData}`)

                const newEvents = response.data.data.data.map((item) => ({
                    eventId: item.eventId,
                    image: `${this.imgUrl}${item.image}`,
                    areaName: item.areaName,
                    deviceName: item.deviceName,
                    accessTime: this.$moment(item.accessTime).format(
                        'DD/MM/YY HH:mm:ss'
                    ),
                    accessTimeStr: item.accessTimeStr,
                    eventTypeName: item.eventTypeName,
                    lat: item.deviceLat,
                    lng: item.deviceLng,
                    warningName: item.warningName,
                }))

                // Append new events to existing events
                this.events = [...this.events, ...newEvents]

                // Check if there are more events to load
                this.hasMoreEvents = newEvents.length === this.itemsPerPage

                // Increment page counter for next load
                this.currentPage++
            } catch (error) {
                console.error('Error loading events:', error)
            } finally {
                this.isLoadingMore = false
            }
        },

        async showWarningOnMap(data) {
            const vm = this
            
            // 🔍 DEBUG: Log dữ liệu nhận được
            console.log('📊 [showWarningOnMap] Received data:', data)
            console.log('📊 [showWarningOnMap] EventTypeId:', data.EventTypeId, 'CompId:', data.CompId, 'CurrentCompId:', vm.currentUserCompId)
            console.log('📊 [showWarningOnMap] FireEvent:', data.FireEvent)

            // Người
            if (
                data.EventTypeId == 200 &&
                data.CompId === vm.currentUserCompId &&
                data.FaceEvent // ✅ Check tồn tại trước khi truy cập
            ) {
                if (
                    data.FaceEvent.PersonId != null &&
                    data.FaceEvent.PersonId !=
                        '00000000-0000-0000-0000-000000000000'
                ) {
                    this.statisticAll.countFaceRegister++
                } else {
                    this.statisticAll.countFaceNotRegister++
                }
                this.statisticAll.countFace++
                vm.listCamera[data.AreaId].map((x) => {
                    if (x.id == data.DeviceId) {
                        x.countFace++
                        if (
                            data.FaceEvent.PersonId != null &&
                            data.FaceEvent.PersonId !=
                                '00000000-0000-0000-0000-000000000000'
                        ) {
                            x.countFaceRegister++
                        } else {
                            x.countFaceNotRegister++
                        }
                    }
                })
            }
            // Đồ bảo hộ
            else if (
                data.EventTypeId == 205 &&
                data.CompId === vm.currentUserCompId
            ) {
                switch (data.WarningLevelId) {
                    case 200:
                        this.statisticAll.countEnoughProtection++
                        break
                    default:
                        this.statisticAll.countLackProtection++
                        break
                }
                vm.listCamera[data.AreaId].map((x) => {
                    if (x.id == data.DeviceId) {
                        switch (data.WarningLevelId) {
                            case 200:
                                this.protectiveCount[0].count++
                                break
                            case 201:
                                this.protectiveCount[1].count++
                                break
                            case 202:
                                this.protectiveCount[2].count++
                                break
                            case 203:
                                this.protectiveCount[3].count++
                                break
                            case 204:
                                this.protectiveCount[4].count++
                                break
                            case 205:
                                this.protectiveCount[5].count++
                                break
                            case 206:
                                this.protectiveCount[6].count++
                                break
                            case 207:
                                this.protectiveCount[7].count++
                                break
                        }
                    }
                })
            }
            // Xe
            else if (
                data.EventTypeId == 300 &&
                data.VehicleEvent && // ✅ Safe check
                data.CompId === vm.currentUserCompId
            ) {
                // Vào
                if (data.VehicleEvent.Direction == 1) {
                    if (data.VehicleEvent.VehicleType == 1) {
                        this.vehiclesCount[0].in++
                    } else if (data.VehicleEvent.VehicleType == 2) {
                        switch (data.VehicleEvent.TotalNumberOfSeats) {
                            case 2:
                                this.vehiclesCount[1].in++
                                break
                            case 5:
                                this.vehiclesCount[2].in++
                                break
                            case 7:
                                this.vehiclesCount[3].in++
                                break
                            case 9:
                                this.vehiclesCount[4].in++
                                break
                            case 16:
                                this.vehiclesCount[5].in++
                                break
                            case 29:
                                this.vehiclesCount[6].in++
                                break
                            case 40:
                                this.vehiclesCount[7].in++
                                break
                        }
                    }
                }
                // Ra
                else if (data.VehicleEvent.Direction == 2) {
                    if (data.VehicleEvent.VehicleType == 1) {
                        this.vehiclesCount[0].out++
                    } else if (data.VehicleEvent.VehicleType == 2) {
                        switch (data.VehicleEvent.TotalNumberOfSeats) {
                            case 2:
                                this.vehiclesCount[1].out++
                                break
                            case 5:
                                this.vehiclesCount[2].out++
                                break
                            case 7:
                                this.vehiclesCount[3].out++
                                break
                            case 9:
                                this.vehiclesCount[4].out++
                                break
                            case 16:
                                this.vehiclesCount[5].out++
                                break
                            case 29:
                                this.vehiclesCount[6].out++
                                break
                            case 40:
                                this.vehiclesCount[7].out++
                                break
                        }
                    }
                }
                vm.listCamera[data.AreaId].map((x) => {
                    if (x.id == data.DeviceId) {
                        switch (data.VehicleEvent.Direction) {
                            case 1:
                                x.countVehicleIn++
                                break
                            case 2:
                                x.countVehicleOut++
                                break
                        }
                    }
                })
            }
            // Vùng cấm
            else if (
                data.EventTypeId == 400 &&
                data.VirtualFenceEvent && // ✅ Safe check
                data.CompId === vm.currentUserCompId
            ) {
                switch (data.VirtualFenceEvent.TroubleType) {
                    case 1:
                        this.statisticAll.countPerson++
                        break
                    case 2:
                        this.statisticAll.countVehicle++
                        break
                }
                vm.listCamera[data.AreaId].map((x) => {
                    if (x.id == data.DeviceId) {
                        switch (data.VirtualFenceEvent.TroubleType) {
                            case 1:
                                x.countPerson++
                                break
                            case 2:
                                x.countVehicle++
                                break
                        }
                    }
                })
            }
            // Cháy khói
            else if (
                data.EventTypeId == 500 &&
                data.FireEvent && // ✅ Safe check
                data.CompId === vm.currentUserCompId
            ) {
                console.log('🔥 [FIRE EVENT] Bắt đầu xử lý sự kiện cháy/khói')
                console.log('🔥 [FIRE EVENT] FireType:', data.FireEvent.FireType)
                console.log('🔥 [FIRE EVENT] Trước: countFire=', this.statisticAll.countFire, 'countSmoke=', this.statisticAll.countSmoke)
                
                switch (data.FireEvent.FireType) {
                    case 1:
                        this.statisticAll.countSmoke++
                        console.log('🔥 [FIRE EVENT] Tăng countSmoke lên:', this.statisticAll.countSmoke)
                        break
                    case 2:
                        this.statisticAll.countFire++
                        console.log('🔥 [FIRE EVENT] Tăng countFire lên:', this.statisticAll.countFire)
                        break
                }
                vm.listCamera[data.AreaId].map((x) => {
                    if (x.id == data.DeviceId) {
                        switch (data.FireEvent.FireType) {
                            case 1:
                                x.countSmoke++
                                break
                            case 2:
                                x.countFire++
                                break
                        }
                    }
                })
            }
            // Băng chuyền
            else if (
                data.EventTypeId == 601 &&
                data.CompId === vm.currentUserCompId
            ) {
                switch (data.WarningLevelId) {
                    case 10:
                        this.statisticAll.countOverflow++
                        break
                    case 11:
                        this.statisticAll.countTooHigh++
                        break
                    case 12:
                        this.statisticAll.countTorn++
                        break
                    case 13:
                        this.statisticAll.countDeviated++
                        break
                    case 14:
                        this.statisticAll.countOversized++
                        break
                }
                vm.listCamera[data.AreaId].map((x) => {
                    if (x.id == data.DeviceId) {
                        switch (data.WarningLevelId) {
                            case 10:
                                x.countOverflow++
                                break
                            case 11:
                                x.countTooHigh++
                                break
                            case 12:
                                x.countTorn++
                                break
                            case 13:
                                x.countDeviated++
                                break
                            case 14:
                                x.countOversized++
                                break
                        }
                    }
                })
            }
            const findAreaById = (areas, id) => {
                if (!Array.isArray(areas)) return null

                return areas.reduce((acc, area) => {
                    if (area.id === id) return area
                    if (area.children && area.children.length > 0) {
                        const found = findAreaById(area.children, id)
                        if (found) return found
                    }
                    return acc
                }, null)
            }

            const areaSelect = findAreaById(this.areasOnMap, data.AreaId)
            if (areaSelect) {
                areaSelect.blink = true

                setTimeout(() => {
                    areaSelect.blink = false
                }, 10000)
            } else {
                console.warn(
                    `Area with ID ${data.AreaId} not found in current view`
                )
            }

            if (this.listCamera && this.listCamera[data.AreaId]) {
                console.log('🎯 Tìm camera trong area:', data.AreaId, 'DeviceId:', data.DeviceId)
                const deviceSelect = this.listCamera[data.AreaId].find(
                    (item) => item.id === data.DeviceId
                )

                if (deviceSelect) {
                    console.log('✅ Tìm thấy camera trong listCamera:', deviceSelect.name)
                    deviceSelect.blink = true

                    setTimeout(() => {
                        deviceSelect.blink = false
                    }, 10000)
                    
                    // ✅ Cập nhật blink cho camera trong listCameraOnMap
                    console.log('🔍 Tìm camera trong listCameraOnMap, tổng số:', this.listCameraOnMap.length)
                    const cameraOnMap = this.listCameraOnMap.find(
                        (item) => item.id === data.DeviceId
                    )
                    if (cameraOnMap) {
                        console.log('✅ Tìm thấy camera trong listCameraOnMap:', cameraOnMap.name)
                        console.log('🔥 Set blink = true, eventTypeId:', cameraOnMap.eventTypeId)
                        
                        // ✅ SỬ DỤNG Vue.set ĐỂ ĐẢM BẢO REACTIVITY
                        this.$set(cameraOnMap, 'blink', true)
                        this.$set(cameraOnMap, 'eventTypeId', data.EventTypeId)
                        
                        setTimeout(() => {
                            this.$set(cameraOnMap, 'blink', false)
                        }, 10000)
                    } else {
                        console.warn('❌ KHÔNG tìm thấy camera trong listCameraOnMap')
                        console.log('📋 Danh sách camera IDs trên map:', this.listCameraOnMap.map(c => c.id))
                    }
                } else {
                    console.warn(
                        `Device with ID ${data.DeviceId} not found in area ${data.AreaId}`
                    )
                }
            } else {
                console.warn(`No devices found for area ${data.AreaId}`)
                console.log('📋 listCamera keys:', this.listCamera ? Object.keys(this.listCamera) : 'null')
            }
        },
        async selectArea(item) {
            const vm = this
            
            // Nếu rỗng thì xem toàn bộ bản đồ (GIỮ NGUYÊN ẢNH COMPANY)
            if (!item) {
                this.resetMap()
                this.areasOnMap = this.areasTree.filter(
                    (area) => area.coordinates
                )
                
                // ✅ GỌI getDevicesByArea(null) để hiển thị TẤT CẢ cameras
                this.getDevicesByArea(null)
                return
            }
            const findAreaById = (areas, id) => {
                return areas.reduce((acc, area) => {
                    if (area.id === id) return area
                    if (area.children) {
                        const found = findAreaById(area.children, id)
                        if (found) return found
                    }
                    return acc
                }, null)
            }
            const areaSelectd = findAreaById(vm.areasTree, item)
            
            // ⚠️ Kiểm tra area có tồn tại không
            if (!areaSelectd) {
                console.error('❌ Area not found:', item)
                this.areasOnMap = []
                this.getDevicesByArea(item)
                return
            }
            
            // ⚠️ Kiểm tra area có coordinates không
            if (!areaSelectd.coordinates) {
                console.warn('⚠️ Area has no coordinates:', item)
                this.areasOnMap = []
                this.getDevicesByArea(item)
                return
            }

            // LUÔN GIỮ NGUYÊN ẢNH COMPANY - Chỉ zoom vào tọa độ của area
            // Không đổi ảnh dù area có maptype=1 hay có ảnh riêng
            const coordinates = JSON.parse(areaSelectd.coordinates)
            this.moveToCoordinate(
                coordinates[0],
                coordinates[1],
                coordinates[2]
            )
            if (areaSelectd.children) {
                this.areasOnMap = []
                this.areasOnMap = areaSelectd.children
                    .filter((area) => area.coordinates)
                    .map((area) => {
                        return {
                            ...area,
                            lat: area.coordinates
                                ? JSON.parse(area.coordinates)[0]
                                : null,
                            lng: area.coordinates
                                ? JSON.parse(area.coordinates)[1]
                                : null,
                        }
                    })
            } else {
                this.areasOnMap = []
            }

            this.getDevicesByArea(item)
        },
        
        /**
         * Hàm đệ quy lấy tất cả ID của khu vực con
         * @param {Number} areaId - ID của khu vực cha
         * @returns {Array<Number>} - Mảng chứa areaId và tất cả child area IDs
         */
        getAllChildAreaIds(areaId) {
            // Hàm đệ quy tìm area trong tree
            const findAreaInTree = (areas, targetId) => {
                for (const area of areas) {
                    if (area.id === targetId) return area
                    if (area.children && area.children.length > 0) {
                        const found = findAreaInTree(area.children, targetId)
                        if (found) return found
                    }
                }
                return null
            }
            
            // Hàm đệ quy lấy tất cả child IDs
            const collectChildIds = (area) => {
                let ids = [area.id] // Bắt đầu với chính ID của area đó
                
                if (area.children && area.children.length > 0) {
                    area.children.forEach(child => {
                        // Đệ quy lấy IDs của các con
                        ids = ids.concat(collectChildIds(child))
                    })
                }
                
                return ids
            }
            
            // Tìm area trong tree
            const targetArea = findAreaInTree(this.areasTree, areaId)
            
            if (!targetArea) {
                console.warn(`⚠️ Không tìm thấy area với ID: ${areaId}`)
                return [areaId] // Trả về chỉ areaId nếu không tìm thấy
            }
            
            // Lấy tất cả child IDs (bao gồm cả chính nó)
            return collectChildIds(targetArea)
        },
        
        getDevicesByArea(areaId) {
            const vm = this
            
            // Case 1: Có cả area VÀ function
            if (areaId && this.eventTypeSelected) {
                try {
                    // ✅ Lấy tất cả area IDs (bao gồm cả children)
                    const allAreaIds = this.getAllChildAreaIds(areaId)
                    
                    // ✅ Gộp cameras từ TẤT CẢ các areas (cha + con)
                    const allCameras = []
                    allAreaIds.forEach(id => {
                        if (this.listCamera[id]) {
                            allCameras.push(...this.listCamera[id])
                        }
                    })
                    
                    // Filter theo function VÀ tọa độ
                    const filteredCameras = allCameras.filter(
                        (item) =>
                            item.latitude !== null &&
                            item.longitude !== null &&
                            item.eventTypeId == vm.eventTypeSelected
                    )
                    
                    // ⚠️ QUAN TRỌNG: Sau khi filter, nếu không có kết quả → CLEAR map
                    this.listCameraOnMap = filteredCameras
                    
                } catch (error) {
                    console.error('❌ Error filtering cameras:', error)
                    this.listCameraOnMap = []
                }
            } 
            // Case 2: Chỉ có area, KHÔNG có function
            else if (areaId && !this.eventTypeSelected) {
                try {
                    // ✅ Lấy tất cả area IDs (bao gồm cả children)
                    const allAreaIds = this.getAllChildAreaIds(areaId)
                    
                    // ✅ Gộp cameras từ TẤT CẢ các areas (cha + con)
                    const allCameras = []
                    allAreaIds.forEach(id => {
                        if (this.listCamera[id]) {
                            allCameras.push(...this.listCamera[id])
                        }
                    })
                    
                    // Filter chỉ cameras có tọa độ
                    const filteredCameras = allCameras.filter(
                        (item) =>
                            item.latitude !== null && item.longitude !== null
                    )
                    
                    // ⚠️ QUAN TRỌNG: Sau khi filter, nếu không có kết quả → CLEAR map
                    this.listCameraOnMap = filteredCameras
                    
                } catch (error) {
                    console.error('❌ Error filtering cameras:', error)
                    this.listCameraOnMap = []
                }
            } 
            // Case 3: KHÔNG có area, chỉ có function
            else if (!areaId && this.eventTypeSelected) {
                this.listCameraOnMap = this.listCameraByEventType.filter(
                    (item) =>
                        item.latitude !== null &&
                        item.longitude !== null &&
                        item.eventTypeId == vm.eventTypeSelected
                )
            } 
            // Case 4: KHÔNG có area, KHÔNG có function → Hiển thị TẤT CẢ cameras có tọa độ
            else {
                this.listCameraOnMap = this.listCameraByEventType.filter(
                    (item) => item.latitude !== null && item.longitude !== null
                )
            }
        },
        normalizer(node) {
            if (node.children.length === 0) {
                // eslint-disable-next-line
                delete node.children
            }
            return {
                // ⚠️ CHỈ cho phép click nếu có coordinates
                // maptype = 1 chỉ để hiển thị ảnh, không đủ để zoom
                isDisabled: !node.coordinates,
            }
        },
        moveToCoordinate(lat, lng, zoom = 3) {
            // Sử dụng đối tượng bản đồ từ ref để di chuyển
            this.$refs.latLngMap.mapObject.setView([lat, lng], zoom, {
                animate: true,
            })
        },
        resetMap() {
            if (this.rootMapCoordinate) {
                this.moveToCoordinate(
                    this.rootMapCoordinate.lat,
                    this.rootMapCoordinate.lng,
                    this.rootMapCoordinate.zoom
                )
            } else {
                this.moveToCoordinate(
                    this.coordinate.lat,
                    this.coordinate.lng,
                    this.coordinate.zoom
                )
            }
        },
        getAreaIcon(area, showCount = true) {
            const baseAreaIcon = area.blink
                ? this.icon.pointWarning
                : this.icon.point

            // Lấy kích thước từ icon gốc
            const iconWidth = baseAreaIcon.options.iconSize[0]
            const iconHeight = baseAreaIcon.options.iconSize[1]

            const number = !isNaN(parseInt(area.totalDevices))
                ? parseInt(area.totalDevices)
                : 0

            const isShowCountDevices = showCount ? 'block' : 'none'

            // Thêm lớp blink vào className nếu area đang cần nhấp nháy
            const customClass = area.blink ? 'blink' : ''

            return L.divIcon({
                className: customClass, // Sử dụng lớp CSS tùy chỉnh với blink nếu cần
                html: `
                    <div style="position: relative;">
                        <img loading="lazy" src="${baseAreaIcon.options.iconUrl}" width="${iconWidth}" height="${iconHeight}" />
                        <span style="
                            position: absolute;
                            top: 35%;
                            bottom: 20%;
                            left: 50%;
                            transform: translate(-48%, -50%);
                            color: black;
                            font-size: ${Math.max(10, iconWidth / 4)}px;
                            font-weight: bold;
                            border-radius: 50%;
                            padding: 2px 4px;
                            pointer-events: none;
                            display: ${isShowCountDevices};
                            margin-bottom: -4px;
                        ">
                            ${number}
                        </span>
                    </div>
                    `,
                iconSize: baseAreaIcon.options.iconSize,
                iconAnchor: [iconWidth / 2, iconHeight / 2],
            })
        },
        getCameraIcon(camera, selected = false) {
            if (!camera) {
                return null
            }

            const blinkClass = camera.blink ? 'blink' : ''

            let baseIcon = ''

            if (camera.blink) {
                switch (camera.eventTypeId) {
                    case 200:
                        baseIcon = this.icon.camPersonWarning
                        break
                    case 300:
                        baseIcon = this.icon.camCarWarning
                        break
                    case 400:
                        baseIcon = this.icon.camForbiddenArea
                        break
                    case 500:
                        baseIcon = this.icon.camFireWarning
                        break
                    case 601:
                        baseIcon = this.icon.camConveyorBelt
                        break
                    // ... more cases
                    default:
                        baseIcon = this.icon.camWarning
                }
            } else {
                baseIcon =
                    camera.status === 1 ? this.icon.camOn : this.icon.camOff
            }
            // Lấy kích thước từ icon gốc
            const baseWidth = baseIcon.options.iconSize[0]
            const baseHeight = baseIcon.options.iconSize[1]

            // Tính toán kích thước dựa trên trạng thái selected
            const iconWidth = selected ? baseWidth * 2 : baseWidth
            const iconHeight = selected ? baseHeight * 2 : baseHeight

            return L.divIcon({
                className: `${blinkClass}`, // Kết hợp lớp nhấp nháy và viền
                html: `
                    <div style="
                        position: relative;
                        width: ${iconWidth}px;
                        height: ${iconHeight}px;
                        border: ${selected ? '3px solid #03fc56' : 'none'};
                        border-radius: 50%;
                        overflow: hidden;
                    "
                    >
                        <img loading="lazy" src="${baseIcon.options.iconUrl}" style="width: 100%; height: 100%;" />
                    </div>
                `,
                iconSize: [iconWidth, iconHeight],
                iconAnchor: [iconWidth / 2, iconHeight / 2],
            })
        },
        /**
         * Handler khi map đã sẵn sàng (mounted và rendered)
         * Fix: Đảm bảo map được center đúng khi hard refresh hoặc client mới mở
         */
        onMapReady() {
            const map = this.$refs.latLngMap?.mapObject
            if (!map) return
            
            console.log('🗺️ Map ready event triggered')
            
            // invalidateSize ngay lập tức
            map.invalidateSize()
            
            // Fit bounds với multiple retries để đảm bảo CSS đã load hết
            const fitWithRetry = (attempt = 1) => {
                if (attempt > 3) return
                
                const container = map.getContainer()
                const viewportHeight = container?.clientHeight || 0
                
                console.log(`🔄 Fit attempt ${attempt}, viewport height: ${viewportHeight}`)
                
                // Nếu viewport quá nhỏ, retry sau
                if (viewportHeight < 300) {
                    setTimeout(() => fitWithRetry(attempt + 1), 300)
                    return
                }
                
                map.invalidateSize()
                
                if (this.actualShowImgMap && this.imageBounds) {
                    map.fitBounds(this.imageBounds, {
                        padding: [5, 5],
                        animate: false
                    })
                    console.log('✅ Image bounds fitted on attempt', attempt)
                }
            }
            
            // Đợi 300ms rồi bắt đầu fit
            setTimeout(() => fitWithRetry(1), 300)
            
            // Retry thêm lần nữa sau 800ms để chắc chắn
            setTimeout(() => {
                map.invalidateSize()
                if (this.actualShowImgMap && this.imageBounds) {
                    map.fitBounds(this.imageBounds, {
                        padding: [5, 5],
                        animate: false
                    })
                    console.log('✅ Final fit bounds at 800ms')
                }
            }, 800)
        },
        onZoomEnd() {
            // ✅ ĐƠN GIẢN HÓA: Chỉ quản lý hiển thị areas theo zoom
            // ❌ KHÔNG động vào listCameraOnMap - cameras được quản lý hoàn toàn bởi getDevicesByArea()
            
            if (!this.$refs.latLngMap || this.areaSelected) return

            const currentZoom = this.$refs.latLngMap.mapObject.getZoom()
            const visibleAreas = []

            // Hàm đệ quy để quyết định hiển thị/ẩn areas dựa trên zoom
            const processAreaTree = (areas, isParentVisible = true) => {
                if (!Array.isArray(areas)) return

                areas.forEach((area) => {
                    if (!area.coordinates || !area.zoomLevel) return

                    const areaZoomLevel = parseFloat(area.zoomLevel)

                    const areaWithCoords = {
                        ...area,
                        lat: area.lat || (area.coordinates ? JSON.parse(area.coordinates)[0] : null),
                        lng: area.lng || (area.coordinates ? JSON.parse(area.coordinates)[1] : null),
                        blink: area.blink || false,
                        showCount: true,
                    }

                    // Nếu zoom đủ lớn và có khu vực con → hiển thị khu vực con, ẩn cha
                    if (currentZoom >= areaZoomLevel) {
                        if (area.children && area.children.length > 0) {
                            processAreaTree(area.children, true)
                        } else {
                            // Khu vực lá: hiển thị nếu parent visible
                            if (isParentVisible) {
                                visibleAreas.push(areaWithCoords)
                            }
                        }
                    } 
                    // Nếu zoom chưa đủ → hiển thị khu vực cha
                    else {
                        if (isParentVisible) {
                            visibleAreas.push(areaWithCoords)
                        }
                        // Không xử lý children
                        if (area.children && area.children.length > 0) {
                            processAreaTree(area.children, false)
                        }
                    }
                })
            }

            processAreaTree(this.areasTree)
            
            // CHỈ cập nhật areas, KHÔNG động vào cameras
            this.areasOnMap = visibleAreas
        },
        loadDeviceDetail(deviceId) {
            this.$services.get(`/device/${deviceId}`).then((response) => {
                this.device = response.data.data
                switch (response.data.data.eventTypeId) {
                    case 200:
                        this.loadStatisticPerson(deviceId, 0)
                        break
                    case 205:
                        this.loadStatisticProtection(deviceId)
                        break
                    case 300:
                        this.loadStatisticVehicle(deviceId)
                        break
                    case 400:
                        this.loadStatisticVirtualFence(deviceId, 0)
                        break
                    case 500:
                        this.loadStatisticFireSmoke(deviceId, 0)
                        break
                    case 601:
                        this.loadStatisticConveryor(deviceId, 0)
                        break
                    // ... more cases
                    default:
                    // Code block to execute if no case matches (optional)
                }
            })
        },

        async loadStatisticVehicle(deviceId) {
            const response = await this.$services.get(
                `/dashboard/countVehicle/${deviceId}`
            )
            this.statistic = response.data.data
        },

        async loadStatisticVirtualFence(deviceId, areaId) {
            const response = await this.$services.get(
                `/dashboard/countVirtualFence/${deviceId}/${areaId}`
            )
            // Load theo khu vực
            if (deviceId == 0) {
                this.statisticAll.countPerson = response.data.data.countPerson
                this.statisticAll.countVehicle = response.data.data.countVehicle
            }
            // Load theo thiết bị
            else if (areaId == 0) {
                this.statistic = response.data.data
            }
        },

        async loadStatisticFireSmoke(deviceId, areaId) {
            console.log('📊 [API] Calling loadStatisticFireSmoke:', deviceId, areaId)
            try {
                const response = await this.$services.get(
                    `/dashboard/countFireSmoke/${deviceId}/${areaId}`
                )
                console.log('📊 [API] loadStatisticFireSmoke response:', response.data.data)
            // Load theo khu vực
            if (deviceId == 0) {
                this.statisticAll.countFire = response.data.data.countFire
                this.statisticAll.countSmoke = response.data.data.countSmoke
            }
            // Load theo thiết bị
            else if (areaId == 0) {
                this.statistic = response.data.data
            }
            } catch (error) {
                console.error('❌ [API] loadStatisticFireSmoke error:', error)
            }
        },

        async loadStatisticProtection(deviceId) {
            const response = await this.$services.get(
                `/dashboard/countProtection/${deviceId}`
            )
            // Load theo khu vực
            if (deviceId == 0) {
                this.statisticAll.countLackProtection =
                    response.data.data.countLackProtection
                this.statisticAll.countEnoughProtection =
                    response.data.data.countEnoughProtection
            } else {
                this.statistic = response.data.data
            }
        },

        async loadStatisticPerson(deviceId, areaId) {
            const response = await this.$services.get(
                `/dashboard/countFace/${deviceId}/${areaId}`
            )
            // Load theo khu vực
            if (deviceId == 0) {
                this.statisticAll.countFaceRegister =
                    response.data.data.countFaceRegister
                this.statisticAll.countFaceNotRegister =
                    response.data.data.countFaceNotRegister
            }
            // Load theo thiết bị
            else if (areaId == 0) {
                this.statistic = response.data.data
            }
        },

        async loadStatisticConveryor(deviceId, areaId) {
            const response = await this.$services.get(
                `/dashboard/countConveryor/${deviceId}/${areaId}`
            )

            if (deviceId == 0) {
                this.statisticAll.countTorn =
                    response.data.data.countConveryorTorn
                this.statisticAll.countOverflow =
                    response.data.data.countConveryorOverflow
                this.statisticAll.countTooHigh =
                    response.data.data.countConveryorTooHigh
                this.statisticAll.countDeviated =
                    response.data.data.countDeviated
                this.statisticAll.countOversized =
                    response.data.data.countOversized
            }
            // Load theo thiết bị
            else if (areaId == 0) {
                this.statistic = response.data.data
            }
        },

        async loadCountVehicleTable(areaId) {
            const vm = this
            const effectiveAreaId = areaId ?? 0  // Fallback to 0 if null/undefined
            await this.$services
                .get(`/dashboard/dashboardCountVehicleEvent/${effectiveAreaId}`)
                .then((response) => {
                    vm.vehiclesCount[0].in = response.data.data.motoIn
                    vm.vehiclesCount[0].out = response.data.data.motoOut
                    vm.vehiclesCount[1].in = response.data.data.car2In
                    vm.vehiclesCount[1].out = response.data.data.car2Out
                    vm.vehiclesCount[2].in = response.data.data.car5In
                    vm.vehiclesCount[2].out = response.data.data.car5Out
                    vm.vehiclesCount[3].in = response.data.data.car7In
                    vm.vehiclesCount[3].out = response.data.data.car7Out
                    vm.vehiclesCount[4].in = response.data.data.car9In
                    vm.vehiclesCount[4].out = response.data.data.car9Out
                    vm.vehiclesCount[5].in = response.data.data.car16In
                    vm.vehiclesCount[5].out = response.data.data.car16Out
                    vm.vehiclesCount[6].in = response.data.data.car29In
                    vm.vehiclesCount[6].out = response.data.data.car29Out
                    vm.vehiclesCount[7].in = response.data.data.car40In
                    vm.vehiclesCount[7].out = response.data.data.car40Out

                    console.log('Vehicle Count Data:', response.data.data)
                })
        },

        async loadCountProtectiveTable(areaId) {
            const vm = this
            const effectiveAreaId = areaId ?? 0  // Fallback to 0 if null/undefined
            await this.$services
                .get(`/dashboard/mapCountProtection/${effectiveAreaId}`)
                .then((response) => {
                    vm.protectiveCount[0].count = response.data.data.enough
                    vm.protectiveCount[1].count = response.data.data.helmet
                    vm.protectiveCount[2].count = response.data.data.faceMask
                    vm.protectiveCount[3].count = response.data.data.gloves
                    vm.protectiveCount[4].count =
                        response.data.data.helmetGloves
                    vm.protectiveCount[5].count = response.data.data.helmetMask
                    vm.protectiveCount[6].count = response.data.data.maskGloves
                    vm.protectiveCount[7].count =
                        response.data.data.missingEverything
                })
        },
        getTotalVehicleIn() {
            return this.vehiclesCount.reduce(
                (total, vehicle) => total + (vehicle.in || 0),
                0
            )
        },
        getTotalVehicleOut() {
            return this.vehiclesCount.reduce(
                (total, vehicle) => total + (vehicle.out || 0),
                0
            )
        },

        toggleArea(areaId) {
            if (this.expandedAreas.includes(areaId)) {
                this.expandedAreas = this.expandedAreas.filter(
                    (id) => id !== areaId
                )
            } else {
                this.expandedAreas.push(areaId)
            }
        },
        selectCam(cam) {
            if (!this.selectedCameras.includes(cam.id)) {
                this.selectedCameras.push(cam.id)
            }
            // Set camera đang được active để hiển thị hiệu ứng background
            this.cameraSelectedId = cam.id
            // Tùy chọn: Liên kết với bản đồ
            this.loadEventsByDevice(cam.id)
        },
        // Method để clear active camera state
        clearActiveCamera() {
            this.cameraSelectedId = null
        },

        async loadCams() {
            try {
                const { data } = await this.$services.get('/device/getAllCam')
                this.listCams = data.data
            } catch (error) {
                console.error('Error loading cameras:', error)
            }
        },
        async loadDevicePermissions() {
            try {
                const { data } = await this.$services.get('/user-device')
                this.devicePermissions = data.data
            } catch (error) {
                console.error('Error loading device permissions:', error)
            }
        },

        async loadCompanyMap() {
            try {
                // Nếu props đã được truyền, ưu tiên dùng props
                if (this.isShowImgMap && this.urlImgOverlay) {
                    console.log('📸 Using map image from props:', this.urlImgOverlay)
                    return
                }

                const accessToken = this.$services.getUserData()
                const companyId = accessToken?.companyId
                
                if (!companyId) {
                    console.warn('No company ID found')
                    return
                }
                
                // Lấy thông tin company
                const response = await this.$services.get(`/company/${companyId}`)
                
                // Kiểm tra structure của response
                const companyData = response.data?.data || response.data
                
                if (!companyData) {
                    return
                }
                
                // Nếu company có ảnh map và MapType là 2 (Bản đồ ảnh)
                // MapType: 1 - Bản đồ số, 2 - Bản đồ ảnh
                if (companyData.mapType === 2 && companyData.imgPath) {
                    // Đặt URL ảnh map từ company
                    this.internalUrlImgOverlay = `${getBaseUrl()}/MapImg/${companyData.imgPath}`
                    this.internalShowImgMap = true
                    
                    // Lưu vào localStorage để dùng lại khi reload
                    localStorage.setItem('companyMapImage', this.internalUrlImgOverlay)
                    
                    console.log('📸 Loading company map image:', this.internalUrlImgOverlay)
                    this.calculateImageBounds(this.internalUrlImgOverlay)
                } else if (companyData.mapType === 1 || !companyData.mapType) {
                    // Nếu là bản đồ số hoặc chưa cấu hình thì mặc định dùng bản đồ số
                    this.internalShowImgMap = false
                    this.internalUrlImgOverlay = null
                    localStorage.removeItem('companyMapImage')
                    console.log('🗺️ Using digital map as per company configuration')
                }
                
                // Sử dụng coordinates từ company nếu có
                if (companyData.coordinates) {
                    const coordinates = JSON.parse(companyData.coordinates)
                    const lat = coordinates[0]
                    const lng = coordinates[1]
                    const zoom = coordinates[2]
                    
                    if (lat !== 0 || lng !== 0) {
                        // Lưu vào rootMapCoordinate để dùng cho resetMap và mapCenter/Zoom
                        this.rootMapCoordinate = { lat, lng, zoom }
                        
                        // Di chuyển map đến tọa độ của company
                        if (!this.actualShowImgMap) {
                            this.$nextTick(() => {
                                this.moveToCoordinate(lat, lng, zoom)
                            })
                        }
                    }
                }
            } catch (error) {
                console.error('Error loading company map:', error)
                // Giữ nguyên config mặc định nếu có lỗi
            }
        },

        toggleSidebar() {
            this.statsButtonActive = !this.statsButtonActive
            
            // Khi thay đổi độ rộng cột, tính lại zoom
            setTimeout(() => {
                this.$nextTick(() => {
                    const map = this.$refs.latLngMap?.mapObject
                    if (!map) return

                    map.invalidateSize()

                    if (this.actualShowImgMap && this.imageBounds && this.internalUrlImgOverlay) {
                        // Tính lại zoom cho viewport mới
                        this.calculateImageBounds(this.internalUrlImgOverlay)
                    } else if (this.rootMapCoordinate) {
                        map.setView(
                            [this.rootMapCoordinate.lat, this.rootMapCoordinate.lng],
                            this.rootMapCoordinate.zoom,
                            { animate: true }
                        )
                    }
                })
            }, 300)
        },

        getVehicleName(code) {
            const vehicleNames = {
                Moto: this.$t('Events.EventModals.Motorcycle') || 'Xe máy',
                Car2: this.$t('Events.EventModals.Truck') || 'Xe tải',
                Car5: this.$t('Events.EventModals.Car5Seats') || '5 chỗ',
                Car7: this.$t('Events.EventModals.Car7Seats') || '7 chỗ',
                Car9: this.$t('Events.EventModals.Car9Seats') || '9 chỗ',
                Car16: this.$t('Events.EventModals.Car16Seats') || '16 chỗ',
                Car29: this.$t('Events.EventModals.Car29Seats') || '29 chỗ',
                Car40: this.$t('Events.EventModals.Car40Seats') || '40 chỗ',
            }
            return vehicleNames[code] || code
        },

        getProtectiveName(code) {
            const protectiveNames = {
                Enough: this.$t('Events.EventModals.None'),
                Helmet: this.$t('Events.EventModals.Helmet'),
                FaceMask: this.$t('Events.EventModals.FaceMask'),
                Gloves: this.$t('Events.EventModals.Gloves'),
                HelmetGloves: this.$t('Events.EventModals.HelmetGloves'),
                HelmetMask: this.$t('Events.EventModals.HelmetMask'),
                MaskGloves: this.$t('Events.EventModals.MaskGloves'),
                MissingEverything: this.$t(
                    'Events.EventModals.HelmetMaskGloves'
                ),
            }
            return protectiveNames[code] || code
        },
    },
}
</script>

<style lang="scss" scoped>
/* Thêm hiệu ứng nhấp nháy */
@keyframes blink {
    0% {
        opacity: 1;
    }

    50% {
        opacity: 0.3;
    }

    100% {
        opacity: 1;
    }
}

::v-deep .blink {
    animation: blink 1s linear infinite;
}

/* Tooltip styles */
.custom-tooltip {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 4px;
    padding: 6px 8px;
    border-radius: 8px;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
    min-width: 120px;

    .tooltip-title {
        font-size: 13px;
        font-weight: 600;
        color: #333;
        text-align: center;
        white-space: nowrap;
    }
}

.tooltip-button-group {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    justify-content: center;
    gap: 6px;
}

.tooltip-button {
    min-width: 30px;
    padding: 2px 6px;
    font-weight: bold;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 4px;
    font-size: 13px;
}

::v-deep .tooltip-disabled {
    display: none !important;
    pointer-events: none !important;
}

::v-deep .tooltip-enabled {
    display: block;
    pointer-events: auto;
}

::v-deep .leaflet-tooltip {
    transition: opacity 0.3s ease;
}

/* ===== START: MAP CONTROLS STYLES ===== */
.map-control-select {
    ::v-deep .vue-treeselect__control {
        background-color: #ffffff !important;
        border: 1px solid #e0e0e0 !important;
        border-radius: 6px !important;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1) !important;
        transition: all 0.2s ease !important;
        padding-left: 5px !important;
        padding-right: 8px !important;
    }

    &.vue-treeselect--focused ::v-deep .vue-treeselect__control,
    &.vue-treeselect--open ::v-deep .vue-treeselect__control,
    ::v-deep .vue-treeselect__control:hover {
        border-color: #0e34b1a2 !important;
    }
}

.gap-2 {
    gap: 8px;
}
/* ===== END: MAP CONTROLS STYLES ===== */

/* ===== START: SIDEBAR TOGGLE BUTTON STYLES ===== */
.sidebar-toggle-btn {
    min-width: 36px;
    height: 36px;
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0;
    transition: all 0.2s ease;
    flex-shrink: 0;
    background-color: #ffffff !important;
    border-color: #e0e0e0 !important;
    color: #6c757d !important;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

    &:hover {
        transform: translateY(-1px);
        box-shadow: 0 3px 6px rgba(0, 0, 0, 0.15);
        border-color: #1e3c72 !important;
        color: #1e3c72 !important;
    }

    &:active {
        transform: translateY(0);
    }
}
/* ===== END: SIDEBAR TOGGLE BUTTON STYLES ===== */

/* ===== START: CAMERA LIST SIDEBAR STYLES ===== */
.custom-stats-panel {
    border: 3px solid #4a90e2 !important;
    border-radius: 8px;
    display: flex;
    flex-direction: column;
    overflow: hidden;
}

.custom-stats-header {
    background: linear-gradient(120deg, #1e3c72 0%, #2a5298 100%);
    padding: 12px 10px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
    border-bottom: none;
}

.stats-header-content {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
}

.header-actions {
    display: flex;
    align-items: center;
    gap: 8px;
}

.toggle-search-btn {
    border: none !important;
    padding: 4px;
    border-radius: 4px;
    transition: all 0.2s ease;
    background: transparent !important;
    color: #ffffff !important;
}

.toggle-search-btn:hover {
    background: transparent !important;
    color: #ffffff !important;
    border: none !important;
    transform: scale(1.5);
}

.toggle-search-btn:focus {
    box-shadow: none !important;
    background: transparent !important;
    color: #ffffff !important;
    border: none !important;
    transform: scale(1.5);
}

.toggle-search-btn.search-active {
    background: transparent;
    border: 1px solid #ffffff;
    color: #ffffff;
    border-radius: 4px;
}

.toggle-search-btn.search-active:hover {
    background: transparent;
    color: #ffffff;
    transform: scale(1.1);
}

.toggle-search-btn.search-active:focus {
    background: transparent;
    border: 1px solid #ffffff;
    color: #ffffff;
    transform: scale(1);
}

.stats-icon-wrapper {
    background: rgba(255, 255, 255, 0.18);
    border-radius: 50%;
    width: 34px;
    height: 34px;
    display: flex;
    align-items: center;
    justify-content: center;
    margin-right: 12px;
    box-shadow: 0 2px 5px rgba(0, 0, 0, 0.15);
    position: relative;
    overflow: hidden;
}

.stats-icon-wrapper::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: linear-gradient(
        45deg,
        rgba(255, 255, 255, 0.15) 0%,
        rgba(255, 255, 255, 0) 70%
    );
    z-index: 0;
}

.stats-icon {
    color: #fff;
    filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.2));
    position: relative;
    z-index: 1;
}

.stats-header-text {
    flex: 1;
}

.stats-title {
    color: #fff;
    font-weight: 700;
    font-size: 14px;
    letter-spacing: 0.5px;
    line-height: 1.2;
    margin-bottom: 2px;
    text-shadow: 0 1px 2px rgba(0, 0, 0, 0.2);
}

.camera-list-container {
    padding: 8px;
    max-height: calc(100vh - 200px);
    overflow-y: auto;
    overflow-x: hidden;
}

.area-group {
    margin-bottom: 12px;
    border-radius: 12px;
    background: #fff;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
    border: 1px solid #e2e8f0;
    overflow: hidden;
    transition: all 0.3s ease;
}

.area-group:hover {
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
    border-color: #4a90e2;
}

.area-header {
    padding: 12px 16px;
    background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: space-between;
    transition: all 0.3s ease;
    border-bottom: 1px solid rgba(226, 232, 240, 0.5);
}

.area-header:hover {
    background: linear-gradient(135deg, #e2e8f0 0%, #cbd5e0 100%);
}

.area-header-content {
    display: flex;
    align-items: center;
    gap: 10px;
    flex: 1;
}

.area-icon {
    color: #4a90e2;
    background: rgba(74, 144, 226, 0.1);
    border-radius: 6px;
    padding: 4px;
    width: 24px;
    height: 24px;
}

.area-name {
    font-weight: 600;
    font-size: 13px;
    color: #2d3748;
    flex: 1;
}

.camera-count {
    background: #4a90e2;
    color: white;
    font-size: 11px;
    font-weight: 600;
    padding: 2px 8px;
    border-radius: 12px;
    min-width: 20px;
    text-align: center;
}

.expand-icon {
    color: #718096;
    transition: transform 0.3s ease;
}

.expand-icon.expanded {
    transform: rotate(180deg);
}

.area-collapse {
    border-top: none;
}

.camera-list {
    padding: 8px 12px;
    background: #fafbfc;
}

.camera-item {
    padding: 8px 12px;
    margin-bottom: 4px;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.3s ease;
    border: 1px solid transparent;
    background: white;
}

.camera-item:last-child {
    margin-bottom: 0;
}

.camera-item:hover {
    background: #f1f5f9;
    transform: translateX(4px);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.camera-item.camera-selected {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    transform: translateX(6px);
    box-shadow: 0 4px 16px rgba(102, 126, 234, 0.3);
    border-color: #4a90e2;
}

.camera-item.camera-selected:hover {
    transform: translateX(6px);
}

.camera-content {
    display: flex;
    align-items: center;
    gap: 8px;
}

.camera-status-indicator {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    flex-shrink: 0;
}

.camera-item.camera-online .camera-status-indicator {
    background: #48bb78;
    box-shadow: 0 0 0 2px rgba(72, 187, 120, 0.3);
}

.camera-item.camera-offline .camera-status-indicator {
    background: #f56565;
    box-shadow: 0 0 0 2px rgba(245, 101, 101, 0.3);
}

.camera-item.camera-selected .camera-status-indicator {
    background: white;
    box-shadow: 0 0 0 2px rgba(255, 255, 255, 0.3);
}

.camera-icon {
    color: #4a90e2;
    flex-shrink: 0;
}

.camera-item.camera-offline .camera-icon {
    color: #f56565;
}

.camera-item.camera-selected .camera-icon {
    color: white;
}

.camera-name {
    font-size: 12px;
    font-weight: 500;
    color: #2d3748;
    flex: 1;
    overflow: visible;
    white-space: normal;
    word-wrap: break-word;
    line-height: 1.3;
}

.camera-item.camera-offline .camera-name {
    color: #718096;
}

.camera-item.camera-selected .camera-name {
    color: white;
    font-weight: 600;
}

.check-icon {
    color: #48bb78;
    flex-shrink: 0;
}

.camera-item.camera-selected .check-icon {
    color: white;
}

.camera-search-container {
    padding: 12px;
    background: #f8fafc;
    border-bottom: 1px solid #e2e8f0;
    margin-bottom: 8px;
}

.camera-search-group {
    position: relative;
}

.camera-search-input {
    border: 1px solid #d1d5db;
    border-radius: 8px;
    padding: 8px 12px;
    font-size: 12px;
    background: white;
    transition: all 0.3s ease;
}

.camera-search-input:focus {
    border-color: #4a90e2;
    box-shadow: 0 0 0 3px rgba(74, 144, 226, 0.1);
    outline: none;
}

.camera-search-group .input-group-append {
    position: absolute;
    right: 8px;
    top: 50%;
    transform: translateY(-50%);
    background: transparent;
    border: none;
    pointer-events: none;
    z-index: 10;
}

.search-icon {
    color: #9ca3af;
}

.camera-filter-compact {
    margin-top: 6px;
}

.filter-tabs {
    display: flex;
    background: #f1f5f9;
    border-radius: 8px;
    padding: 2px;
    gap: 1px;
    width: fit-content;
    margin-left: auto;
}

.filter-tab {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 3px;
    padding: 4px 8px;
    border-radius: 6px;
    font-size: 10px;
    font-weight: 500;
    color: #64748b;
    cursor: pointer;
    transition: all 0.2s ease;
    background: transparent;
    border: 1px solid transparent;
    position: relative;
    min-width: 65px;
}

.filter-tab:hover:not(.active) {
    background: rgba(255, 255, 255, 0.8);
    color: #475569;
    transform: translateY(-0.5px);
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.filter-tab:active {
    transform: translateY(0px);
    transition: all 0.1s ease;
}

.filter-tab.active {
    background: linear-gradient(135deg, #6ba3f5 0%, #4a90e2 100%);
    color: white;
    box-shadow: 0 3px 8px rgba(107, 163, 245, 0.25);
    transform: translateY(-1px);
    font-weight: 600;
    border: 1px solid rgba(107, 163, 245, 0.3);
}

.filter-tab.online.active {
    color: #059669;
    background: linear-gradient(135deg, white 0%, #f0fdf4 100%);
    border: 1px solid rgba(16, 185, 129, 0.2);
}

.filter-tab.offline.active {
    color: #dc2626;
    background: linear-gradient(135deg, white 0%, #fef2f2 100%);
    border: 1px solid rgba(239, 68, 68, 0.2);
}

.filter-tab.inactive {
    color: #9ca3af;
    opacity: 0.6;
}

.filter-tab.inactive:hover {
    opacity: 0.8;
    color: #6b7280;
}

.tab-text {
    line-height: 1;
}

.status-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    flex-shrink: 0;
    transition: all 0.2s ease;
}

.online-dot {
    background: #cbd5e1;
}

.offline-dot {
    background: #cbd5e1;
}

.online-dot.dot-active {
    background: #10b981;
    box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.3);
}

.offline-dot.dot-active {
    background: #ef4444;
    box-shadow: 0 0 0 2px rgba(239, 68, 68, 0.3);
}

.online-dot.dot-inactive {
    background: #f1f5f9;
    border: 1px solid #cbd5e1;
}

.offline-dot.dot-inactive {
    background: #f1f5f9;
    border: 1px solid #cbd5e1;
}

.filter-tab.online:hover:not(.active) .online-dot {
    background: #059669;
}

.filter-tab.offline:hover:not(.active) .offline-dot {
    background: #dc2626;
}

.camera-list-container::-webkit-scrollbar {
    width: 4px;
}

.camera-list-container::-webkit-scrollbar-track {
    background: #f1f1f1;
    border-radius: 2px;
}

.camera-list-container::-webkit-scrollbar-thumb {
    background: #cbd5e0;
    border-radius: 2px;
}

.camera-list-container::-webkit-scrollbar-thumb:hover {
    background: #a0aec0;
}

@keyframes slideIn {
    from {
        opacity: 0;
        transform: translateY(-10px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.area-collapse .camera-list {
    animation: slideIn 0.3s ease-out;
}
/* ===== END: CAMERA LIST SIDEBAR STYLES ===== */

/* ===== START: ZOOM CONTROLS STYLES ===== */
.zoom-controls {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.zoom-btn {
    width: 36px;
    height: 36px;
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0;
    transition: all 0.2s ease;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    background-color: #ffffff !important;
    border-color: #e0e0e0 !important;

    &:hover {
        transform: translateY(-1px);
        box-shadow: 0 3px 6px rgba(0, 0, 0, 0.15);
        border-color: #0e34b1a2 !important;
    }

    &:active {
        transform: translateY(0);
    }

    &:focus {
        box-shadow: 0 0 0 0.2rem rgba(14, 52, 177, 0.25) !important;
    }
}
/* ===== END: ZOOM CONTROLS STYLES ===== */

/* ========================================
   BLINK ANIMATION FOR WARNING ICONS
   ======================================== */
@keyframes blink {
    0%, 100% {
        opacity: 1;
        transform: scale(1);
    }
    50% {
        opacity: 0.4;
        transform: scale(1.1);
    }
}

.blink {
    animation: blink 1s ease-in-out infinite;
}

.blink img {
    animation: blink 1s ease-in-out infinite;
}
/* Map display fixes */
.map-view {
    height: 100% !important;
    width: 100% !important;
    min-height: 400px;
}

#map-container {
    height: 100% !important;
}

.map-wrapper {
    overflow: hidden;
}
</style>
