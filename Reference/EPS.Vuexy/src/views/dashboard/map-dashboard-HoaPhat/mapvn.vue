<template>
    <div class="animated fadeIn map-wrapper" :style="{ height: mapHeight }">
        <b-container fluid class="p-0 h-100">
            <b-row class="no-gutters h-100">
                <!-- Cột chứa map với tỉ lệ dynamic -->
                <b-col :xl="showSidebar ? 10 : 12" class="h-100" :class="{ 'pr-1': showSidebar }">
                    <div class="map-card-border mb-0 h-100">
                        <b-card-body class="p-0 h-100">
                            <div
                                id="map-container"
                                class="position-relative h-100"
                            >
                                <l-map
                                    ref="latLngMap"
                                    class="rounded map-view"
                                    :zoom="coordinate.zoom"
                                    :center="[coordinate.lat, coordinate.lng]"
                                    :zoom-snap="0.1"
                                    :options="{
                                        attributionControl: false,
                                        zoomControl: false,
                                        closePopupOnClick: false,
                                    }"
                                >
                                    <!-- Chức năng, khu vực -->
                                    <l-control
                                        style="width: 35%"
                                        position="topright"
                                        class="mt-2"
                                    >
                                        <div
                                            class="d-flex align-items-center justify-content-center"
                                        >
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
                                                open-direction="bottom"
                                                style="
                                                    background-color: white;
                                                    flex: 4;
                                                    margin-right: 10px;
                                                "
                                            >
                                            </tree-select>

                                            <tree-select
                                                v-model="eventTypeSelected"
                                                label="text"
                                                @input="getShowTable"
                                                :reduce="
                                                    (eventType) => eventType.id
                                                "
                                                :options="listEventTypeByArea"
                                                :placeholder="
                                                    $t(
                                                        'Events.SearchForm.Placeholder.Feature'
                                                    )
                                                "
                                                style="
                                                    background-color: white;
                                                    flex: 6;
                                                    margin-right: 10px;
                                                "
                                            >
                                            </tree-select>
                                        </div>
                                    </l-control>

                                    <l-control
                                        style="width: 40px"
                                        position="topright"
                                        class="mt-1 mr-2"
                                    >
                                        <div
                                            class="custom-control-box d-flex flex-column align-items-center justify-content-center"
                                        >
                                            <b-button
                                                :variant="
                                                    statsButtonActive
                                                        ? 'primary'
                                                        : 'outline-primary'
                                                "
                                                class="btn-circle custom-btn"
                                                @click="toggleStatsPanel()"
                                                :title="
                                                    $t(
                                                        'Map.Controls.ToggleCameraEvents'
                                                    )
                                                "
                                            >
                                                <feather-icon
                                                    icon="EyeIcon"
                                                    size="18"
                                                />
                                            </b-button>
                                            <b-button
                                                :variant="
                                                    cameraListButtonActive
                                                        ? 'primary'
                                                        : 'outline-primary'
                                                "
                                                class="btn-circle custom-btn"
                                                @click="toggleCameraPanel()"
                                                :title="
                                                    $t(
                                                        'Map.Controls.ToggleCameraNames'
                                                    )
                                                "
                                            >
                                                <feather-icon
                                                    icon="CameraIcon"
                                                    size="18"
                                                />
                                            </b-button>
                                        </div>
                                    </l-control>
                                    <l-tile-layer
                                        v-if="!isShowImgMap"
                                        :url="urlOSM"
                                    />
                                    <l-image-overlay
                                        v-if="isShowImgMap && urlImgOverlay"
                                        :url="urlImgOverlay"
                                        :bounds="[
                                            [-60, -150], // Góc dưới bên trái của hình ảnh
                                            [60, 150], // Góc trên bên phải của hình ảnh
                                        ]"
                                    />

                                    <!-- Map content -->
                                    <div v-if="!isShowImgMap">
                                        <!-- Island -->
                                        <l-polygon
                                            v-for="data in polygonMap.polygonBaoQuanh"
                                            :key="data.id"
                                            :lat-lngs="data.latlngs"
                                            :options="polygonMap.polygonbao.style"
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
                                                    class="tooltip-button-group"
                                                    v-if="
                                                        camera.eventTypeId ==
                                                        200
                                                    "
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
                                                    class="tooltip-button-group"
                                                    v-else-if="
                                                        camera.eventTypeId ==
                                                        300
                                                    "
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
                                                    class="tooltip-button-group"
                                                    v-else-if="
                                                        camera.eventTypeId ==
                                                        400
                                                    "
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
                                                    class="tooltip-button-group"
                                                    v-else-if="
                                                        camera.eventTypeId ==
                                                        500
                                                    "
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
                                                <div
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
                                                </div>
                                                <!-- tooltip sự kiện đồ bảo hộ -->
                                                <div
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
                                                </div>
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

                <!-- Cột 2 với tỉ lệ 2/12 - thay b-card bằng div tùy chỉnh -->
                <b-col xl="2" class="pl-1 h-100" v-if="showSidebar">
                    <div
                        v-if="showStatsPanel"
                        class="custom-stats-panel h-100"
                    >
                        <div class="custom-stats-header">
                            <div class="stats-header-content">
                                <div class="stats-icon-wrapper">
                                    <feather-icon
                                        icon="BarChart2Icon"
                                        size="16"
                                        class="stats-icon"
                                    />
                                </div>
                                <div class="stats-header-text">
                                    <div class="stats-title">
                                        {{
                                            $t(
                                                'Events.MapDashboard.MonitoringStatistics'
                                            )
                                        }}
                                    </div>
                                    <div class="stats-subtitle">
                                        {{
                                            $t(
                                                'Events.MapDashboard.RealtimeData'
                                            )
                                        }}
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="custom-stats-body">
                            <div class="statistics-container">
                                <!-- Nhóm 1: Nhận diện người -->
                                <div class="stat-group-compact">
                                    <div class="stat-group-header-compact">
                                        <feather-icon
                                            icon="UserIcon"
                                            size="12"
                                            class="mr-1"
                                        />
                                        <span>{{
                                            $t(
                                                'Events.MapDashboard.Identification'
                                            )
                                        }}</span>
                                    </div>
                                    <div class="stat-row-compact">
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-primary"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{
                                                        statisticAll.countFaceRegister ||
                                                        0
                                                    }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.Registered'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-secondary"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{
                                                        statisticAll.countFaceNotRegister ||
                                                        0
                                                    }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.UnRegistered'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Nhóm 2: Phương tiện -->
                                <div class="stat-group-compact">
                                    <div class="stat-group-header-compact">
                                        <feather-icon
                                            icon="TruckIcon"
                                            size="12"
                                            class="mr-1"
                                        />
                                        <span>{{
                                            $t('Events.MapDashboard.Vehicle')
                                        }}</span>
                                    </div>
                                    <div class="stat-row-compact">
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-success"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{ getTotalVehicleIn() }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.In'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-danger"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{ getTotalVehicleOut() }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.Out'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Nhóm 3: Vùng cấm -->
                                <div class="stat-group-compact">
                                    <div class="stat-group-header-compact">
                                        <feather-icon
                                            icon="ShieldOffIcon"
                                            size="12"
                                            class="mr-1"
                                        />
                                        <span>{{
                                            $t(
                                                'Events.MapDashboard.ForbiddenZone'
                                            )
                                        }}</span>
                                    </div>
                                    <div class="stat-row-compact">
                                        <div
                                            class="stat-item-compact flex-fill mr-1"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-warning"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{
                                                        statisticAll.countPerson ||
                                                        0
                                                    }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.Employee'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Nhóm 4: Cháy khói -->
                                <div class="stat-group-compact">
                                    <div class="stat-group-header-compact">
                                        <feather-icon
                                            icon="ZapIcon"
                                            size="12"
                                            class="mr-1"
                                        />
                                        <span>{{
                                            $t('Events.MapDashboard.FireSmoke')
                                        }}</span>
                                    </div>
                                    <div class="stat-row-compact">
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-danger"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{
                                                        statisticAll.countFire ||
                                                        0
                                                    }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.Fire'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-secondary"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{
                                                        statisticAll.countSmoke ||
                                                        0
                                                    }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.Smoke'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Nhóm 5: Băng chuyền -->
                                <div class="stat-group-compact">
                                    <div class="stat-group-header-compact">
                                        <feather-icon
                                            icon="SettingsIcon"
                                            size="12"
                                            class="mr-1"
                                        />
                                        <span>{{
                                            $t('Events.MapDashboard.Conveyor')
                                        }}</span>
                                    </div>
                                    <div class="stat-row-compact">
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-secondary"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{
                                                        statisticAll.countTorn ||
                                                        0
                                                    }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.Torn'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-danger"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{
                                                        statisticAll.countOverflow ||
                                                        0
                                                    }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.Overflow'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                    <div class="stat-row-compact">
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-primary"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{
                                                        statisticAll.countDeviated ||
                                                        0
                                                    }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.Deviated'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-secondary"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{
                                                        statisticAll.countOversized ||
                                                        0
                                                    }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.Size'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Nhóm 6: Nhận đồ bảo hộ -->
                                <div class="stat-group-compact">
                                    <div class="stat-group-header-compact">
                                        <feather-icon
                                            icon="CodesandboxIcon"
                                            size="12"
                                            class="mr-1"
                                        />
                                        <span>{{
                                            $t(
                                                'Events.MapDashboard.ProtectiveGear'
                                            )
                                        }}</span>
                                    </div>
                                    <div class="stat-row-compact">
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-primary"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{
                                                        statisticAll.countEnoughProtection ||
                                                        0
                                                    }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.Enough'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                        <div
                                            class="stat-item-compact flex-fill w-50"
                                        >
                                            <div
                                                class="stat-indicator-compact bg-secondary"
                                            ></div>
                                            <div class="stat-content-compact">
                                                <div class="stat-value-compact">
                                                    {{
                                                        statisticAll.countLackProtection ||
                                                        0
                                                    }}
                                                </div>
                                                <div class="stat-label-compact">
                                                    {{
                                                        $t(
                                                            'Events.MapDashboard.Lack'
                                                        )
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

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
                                            :class="{ 'search-active': showCameraSearch }"
                                            @click="showCameraSearch = !showCameraSearch"
                                            :title="showCameraSearch 
                                            ? (this.$i18n.locale === 'vi' ? 'Ẩn tìm kiếm' : 'Hide search') 
                                            : (this.$i18n.locale === 'vi' ? 'Hiện tìm kiếm' : 'Show search')"
                                        >
                                            <feather-icon
                                                :icon="showCameraSearch ? 'EyeOffIcon' : 'SearchIcon'"
                                                size="14"
                                            />
                                        </b-button>
                                    </div>
                                </div>
                            </div>

                            <div class="camera-list-container">
                                <!-- Search and Filter box -->
                                <div v-if="showCameraSearch" class="camera-search-container">
                                    <b-input-group class="camera-search-group">
                                        <b-form-input
                                            v-model="cameraSearchText"
                                            :placeholder="this.$i18n.locale === 'vi' ? 'Tìm kiếm ...' : 'Search ...'"
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
                                                    active: cameraStatusFilter === 'online',
                                                    inactive: cameraStatusFilter === 'offline'
                                                }"
                                                @click="toggleStatusFilter('online')"
                                            >
                                                <div class="status-dot online-dot" :class="{ 
                                                    'dot-active': cameraStatusFilter === 'online',
                                                    'dot-inactive': cameraStatusFilter === 'offline'
                                                }"></div>
                                                <span class="tab-text">Online</span>
                                            </div>
                                            <div
                                                class="filter-tab offline"
                                                :class="{ 
                                                    active: cameraStatusFilter === 'offline',
                                                    inactive: cameraStatusFilter === 'online'
                                                }"
                                                @click="toggleStatusFilter('offline')"
                                            >
                                                <div class="status-dot offline-dot" :class="{ 
                                                    'dot-active': cameraStatusFilter === 'offline',
                                                    'dot-inactive': cameraStatusFilter === 'online'
                                                }"></div>
                                                <span class="tab-text">Offline</span>
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
                                            <span class="area-name">{{ area.name }}</span>
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
                                                'expanded': expandedAreas.includes(area.id)
                                            }"
                                        />
                                    </div>

                                    <!-- Danh sách camera -->
                                    <b-collapse
                                        :visible="expandedAreas.includes(area.id)"
                                        class="area-collapse"
                                    >
                                        <div class="camera-list">
                                            <div
                                                v-for="cam in area.cameras"
                                                :key="cam.id + cam.code"
                                                class="camera-item"
                                                :class="{
                                                    'camera-selected': cameraSelectedId === cam.id,
                                                    'camera-offline': cam.status === 0,
                                                    'camera-online': cam.status === 1
                                                }"
                                                @click="selectCam(cam)"
                                            >
                                                <div class="camera-content">
                                                    <div class="camera-status-indicator"></div>
                                                    <feather-icon
                                                        icon="VideoIcon"
                                                        size="14"
                                                        class="camera-icon"
                                                    />
                                                    <span class="camera-name">{{ cam.name }}</span>
                                                    <feather-icon
                                                        v-if="selectedCameras.includes(cam.id)"
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
import getBaseUrl from '@/utils/get-baseUrl'
import signalRService from '@/utils/signalr-service'
import TreeHelper from '@/utils/treeHelper'
import useAppConfig from '@core/app-config/useAppConfig'
import { computed } from '@vue/composition-api'
import helper from '@/utils/utils.js'
import CameraInfoPanel from './CameraInfoPanel.vue'
import EventModalsContainer from './components/EventModalsContainer.vue'

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

        //Tính toán chiều cao thực tế của map wrapper
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
            isShowImgMap: false,
            urlImgOverlay: null,
            showCard: false,
            events: [],
            rootMapCoordinate: null,
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
            enableTooltip: false,
            countAllEvent: [], // Dữ liệu đếm tất cả sự kiện
        }
    },
    computed: {
        imgUrl() {
            return getBaseUrl()
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
                filteredAreas = filteredAreas.map(area => ({
                    ...area,
                    cameras: area.cameras.filter(cam => 
                        cam.name.toLowerCase().includes(searchText) ||
                        cam.code?.toLowerCase().includes(searchText)
                    )
                }))
            }
            
            // Filter theo status
            if (this.cameraStatusFilter !== 'all') {
                filteredAreas = filteredAreas.map(area => ({
                    ...area,
                    cameras: area.cameras.filter(cam => {
                        if (this.cameraStatusFilter === 'online') {
                            return cam.status === 1
                        } else if (this.cameraStatusFilter === 'offline') {
                            return cam.status === 0
                        }
                        return true
                    })
                }))
            }
            
            // Chỉ trả về các area có camera
            return filteredAreas.filter(area => area.cameras.length > 0)
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
                const areaId = cam.areaId
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
                var eventTypeByArea = this.listAreaFuntion
                    .filter((x) => x.areaId == newVal)
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
            this.loadAllEventByArea(newVal ? newVal : 0)
            setTimeout(() => this.onZoomEnd(), 100)
        },
        eventTypeSelected() {
            this.clearActiveCamera()
            this.selectArea(this.areaSelected)
            setTimeout(() => this.onZoomEnd(), 100)
        },
    },
    async created() {
        var vm = this
        const accessToken = this.$services.getUserData()
        vm.currentUserCompId = accessToken.companyId
        await vm.loadCams()
        await vm.loadDevicePermissions()
        await vm.getCountAllEvent()
        vm.loadAllEventByArea(0)
        vm.loadAreasTree()
        vm.loadEventType()
        vm.loadAreaFuntion()
        vm.rootMapCoordinate = {
            lat: vm.coordinate.lat,
            lng: vm.coordinate.lng,
            zoom: vm.coordinate.zoom,
        }

        let rest = vm.loadDevices()
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
                var count = vm.countAllEvent.find((x) => x.deviceId == item.id)
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
                    (x) => x.deviceId == item.id
                )
                return {
                    ...item,
                    ...(count || defaultCount), // nếu count là undefined thì vẫn an toàn
                }
            })
            setTimeout(() => vm.onZoomEnd(), 300)
        })

        await signalRService.connect('notificationHub')
        signalRService.on('NewEvent', (data) => {
            if (data) {
                const dataJson = JSON.parse(data)
                this.showWarningOnMap(dataJson)
            }
        })
    },
    mounted() {
        this.$nextTick(() => {
            this.$refs.latLngMap.mapObject.on('zoomend', this.onZoomEnd)
        })
    },
    methods: {
        // Toggle status filter method
        toggleStatusFilter(status) {
            if (this.cameraStatusFilter === status) {
                // If clicking the active filter, deactivate it (show all)
                this.cameraStatusFilter = 'all';
            } else {
                // Activate the clicked filter
                this.cameraStatusFilter = status;
            }
        },
        
        //lookup data
        loadAreaFuntion() {
            this.$services.get('/lookup/area-function').then((response) => {
                let areaFunction = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
                this.listAreaFuntion = areaFunction
            })
        },
        getShowTable() {
            const eventTypeSelected = this.eventTypeSelected
            if (eventTypeSelected) {
                for (const key in this.showTable) {
                    this.showTable[key].isShow =
                        this.showTable[key].id == eventTypeSelected
                            ? true
                            : false
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
            this.enableTooltip = !this.enableTooltip
            
            // Nếu bật tooltip events, tắt camera list để đảm bảo chỉ một nút active
            if (this.enableTooltip) {
                this.showCameraByArea = false
            }
            
            var vm = this
            this.listCameraOnMap.forEach((camera) => {
                const markerRef = vm.$refs['marker-' + camera.id]
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
                var vm = this
                this.listCameraOnMap.forEach((camera) => {
                    const markerRef = vm.$refs['marker-' + camera.id]
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
            var vm = this
            vm.showModal = true
            vm.videoSource =
                vm.nodeMediaServer +
                vm.device.compId +
                '/' +
                vm.device.code +
                '.flv'
        },
        async getCountAllEvent() {
            await this.$services
                .get('/dashboard/dashboardCountEvent')
                .then((response) => {
                    this.countAllEvent = response.data.data
                })
        },
        //lookup data
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                let eventType = TreeHelper.removeEmptyChildren(
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
            var vm = this
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
            var vm = this
            console.log('data.CompId:', data.CompId)
            console.log('vm.currentUserCompId', vm.currentUserCompId)

            //Người
            if (
                data.EventTypeId == 200 &&
                data.CompId === vm.currentUserCompId
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
            //Đồ bảo hộ
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
            //Xe
            else if (
                data.EventTypeId == 300 &&
                data.VehicleEvent &&
                data.CompId === vm.currentUserCompId
            ) {
                //Vào
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
                //Ra
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
            //Vùng cấm
            else if (
                data.EventTypeId == 400 &&
                data.VirtualFenceEvent &&
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
            //Cháy khói
            else if (
                data.EventTypeId == 500 &&
                data.FireEvent &&
                data.CompId === vm.currentUserCompId
            ) {
                switch (data.FireEvent.FireType) {
                    case 1:
                        this.statisticAll.countSmoke++
                        break
                    case 2:
                        this.statisticAll.countFire++
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
            //Băng chuyền
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
                const deviceSelect = this.listCamera[data.AreaId].find(
                    (item) => item.id === data.DeviceId
                )

                if (deviceSelect) {
                    deviceSelect.blink = true

                    setTimeout(() => {
                        deviceSelect.blink = false
                    }, 10000)
                } else {
                    console.warn(
                        `Device with ID ${data.DeviceId} not found in area ${data.AreaId}`
                    )
                }
            } else {
                console.warn(`No devices found for area ${data.AreaId}`)
            }
        },
        async selectArea(item) {
            var vm = this
            // Nếu rỗng thì xem toàn bộ bản đồ
            if (!item) {
                this.isShowImgMap = false
                this.resetMap()
                this.areasOnMap = this.areasTree.filter(
                    (area) => area.coordinates
                )
                this.listCameraOnMap = []
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

            // Kiểm tra xem có phải loại hình ảnh không
            if (areaSelectd.maptype === 1) {
                this.isShowImgMap = true
                this.urlImgOverlay = `${getBaseUrl()}/MapImg/${areaSelectd.img}`
                this.moveToCoordinate(0, 0, 3)
                return
            }

            // Nếu khu vực là loại hình ảnh thì xử lý như bth
            this.isShowImgMap = false
            this.urlImgOverlay = null
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
        getDevicesByArea(areaId) {
            var vm = this
            //Có chọn khu vực
            if (areaId && this.eventTypeSelected) {
                try {
                    this.listCameraOnMap = this.listCamera[areaId].filter(
                        (item) =>
                            item.latitude !== null &&
                            item.longitude !== null &&
                            item.eventTypeId == vm.eventTypeSelected
                    )
                } catch (error) {
                    console.error('Error filtering cameras:', error)
                    this.listCameraOnMap = []
                }
            } else if (areaId && !this.eventTypeSelected) {
                try {
                    this.listCameraOnMap = this.listCamera[areaId].filter(
                        (item) =>
                            item.latitude !== null && item.longitude !== null
                    )
                } catch (error) {
                    console.error('Error filtering cameras:', error)
                    this.listCameraOnMap = []
                }
            } else if (!areaId && this.eventTypeSelected) {
                this.listCameraOnMap = this.listCameraByEventType.filter(
                    (x) => x.eventTypeId == vm.eventTypeSelected
                )
            } else {
                this.listCameraOnMap = this.listCameraByEventType
            }
        },
        normalizer(node) {
            if (node.children.length === 0) {
                // eslint-disable-next-line
                delete node.children
            }
            return {
                isDisabled: !(node.coordinates || node.maptype === 1),
            }
        },
        moveToCoordinate(lat, lng, zoom = 3) {
            // Sử dụng đối tượng bản đồ từ ref để di chuyển
            this.$refs.latLngMap.mapObject.setView([lat, lng], zoom, {
                animate: true,
            })
        },
        resetMap() {
            this.moveToCoordinate(
                this.rootMapCoordinate.lat,
                this.rootMapCoordinate.lng,
                this.rootMapCoordinate.zoom
            )
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

            var baseIcon = ''

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
        onZoomEnd() {
            if (!this.$refs.latLngMap || this.areaSelected) return

            const currentZoom = this.$refs.latLngMap.mapObject.getZoom()

            // Kết quả cuối cùng sẽ lưu ở đây
            const visibleAreas = []
            const camerasToShow = []

            // Theo dõi các khu vực có zoomLevel nhỏ hơn hoặc bằng currentZoom để hiển thị camera
            const areasWithSufficientZoom = []

            // Hàm đệ quy để duyệt cây khu vực và quyết định hiển thị/ẩn
            const processAreaTree = (areas, isParentVisible = true) => {
                if (!Array.isArray(areas)) return

                areas.forEach((area) => {
                    if (!area.coordinates || !area.zoomLevel) return

                    const areaZoomLevel = parseFloat(area.zoomLevel)

                    // Chuẩn bị thông tin khu vực đầy đủ với tọa độ
                    const areaWithCoords = {
                        ...area,
                        lat:
                            area.lat ||
                            (area.coordinates
                                ? JSON.parse(area.coordinates)[0]
                                : null),
                        lng:
                            area.lng ||
                            (area.coordinates
                                ? JSON.parse(area.coordinates)[1]
                                : null),
                        blink: area.blink || false,
                        showCount: true, // Mặc định hiển thị số, sẽ cập nhật sau
                    }

                    // TRƯỜNG HỢP ĐẶC BIỆT: Nếu là khu vực không có con và zoomLevel = currentZoom, thì ẩn luôn
                    // TRƯỜNG HỢP ĐẶC BIỆT: Nếu là khu vực không có con và zoomLevel <= currentZoom, thì ẩn luôn
                    const isLeafNode =
                        !area.children || area.children.length === 0
                    const shouldHide =
                        isLeafNode && currentZoom >= areaZoomLevel

                    if (shouldHide) {
                        // Không hiển thị khu vực, chỉ thu thập camera nếu có
                        areasWithSufficientZoom.push(area.id)
                        return // Bỏ qua việc thêm vào visibleAreas
                    }

                    // Điều kiện 1: Nếu currentZoom >= zoomLevel của khu vực cha
                    if (currentZoom >= areaZoomLevel) {
                        // Thêm area vào danh sách khu vực có đủ zoom để hiển thị camera
                        areasWithSufficientZoom.push(area.id)

                        // Khu vực này là khu vực cha và có con
                        if (area.children && area.children.length > 0) {
                            // Xử lý các khu vực con (với trạng thái cha là visible)
                            processAreaTree(area.children, true)
                        } else {
                            // Đây là khu vực lá (không có con), hiển thị nó nếu chưa bị loại bỏ ở trên
                            visibleAreas.push(areaWithCoords)
                        }
                    }
                    // Điều kiện 2: Nếu currentZoom < zoomLevel của khu vực, hiển thị khu vực cha
                    else {
                        // Chỉ hiển thị khu vực cha nếu parent của nó đang visible
                        if (isParentVisible) {
                            visibleAreas.push(areaWithCoords)
                        }

                        // Không xử lý các khu vực con (set trạng thái parent = false)
                        if (area.children && area.children.length > 0) {
                            processAreaTree(area.children, false)
                        }
                    }
                })
            }

            // Bắt đầu xử lý từ khu vực cha cao nhất
            processAreaTree(this.areasTree)

            // Thu thập camera từ những khu vực có zoomLevel <= currentZoom
            const areasWithCameras = new Set()

            areasWithSufficientZoom.forEach((areaId) => {
                if (this.listCamera && this.listCamera[areaId]) {
                    const cameras = this.listCamera[areaId].filter(
                        (camera) =>
                            camera.latitude !== null &&
                            camera.longitude !== null
                    )
                    if (cameras.length > 0) {
                        // Lưu lại khu vực có camera để sau này loại bỏ
                        areasWithCameras.add(areaId)
                        camerasToShow.push(...cameras)
                    }
                }
            })

            // Loại bỏ camera trùng lặp
            const uniqueCameras = Array.from(
                new Map(
                    camerasToShow.map((camera) => [camera.id, camera])
                ).values()
            )

            // THAY ĐỔI: Loại bỏ hoàn toàn các khu vực có camera hiển thị
            const finalVisibleAreas = visibleAreas.filter(
                (area) => !areasWithCameras.has(area.id)
            )

            // Cập nhật khu vực hiển thị - chỉ bao gồm những khu vực không có camera
            this.areasOnMap = finalVisibleAreas

            // Cập nhật camera hiển thị
            this.listCameraOnMap = uniqueCameras

            //Nếu có chức năng cam thì lọc theo chức năng
            if (this.eventTypeSelected) {
                this.listCameraOnMap = this.listCameraOnMap.filter(
                    (x) => x.eventTypeId == this.eventTypeSelected
                )
            }
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
            //Load theo khu vực
            if (deviceId == 0) {
                this.statisticAll.countPerson = response.data.data.countPerson
                this.statisticAll.countVehicle = response.data.data.countVehicle
            }
            //Load theo thiết bị
            else if (areaId == 0) {
                this.statistic = response.data.data
            }
        },

        async loadStatisticFireSmoke(deviceId, areaId) {
            const response = await this.$services.get(
                `/dashboard/countFireSmoke/${deviceId}/${areaId}`
            )
            //Load theo khu vực
            if (deviceId == 0) {
                this.statisticAll.countFire = response.data.data.countFire
                this.statisticAll.countSmoke = response.data.data.countSmoke
            }
            //Load theo thiết bị
            else if (areaId == 0) {
                this.statistic = response.data.data
            }
        },

        async loadStatisticProtection(deviceId) {
            const response = await this.$services.get(
                `/dashboard/countProtection/${deviceId}`
            )
            //Load theo khu vực
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
            //Load theo khu vực
            if (deviceId == 0) {
                this.statisticAll.countFaceRegister =
                    response.data.data.countFaceRegister
                this.statisticAll.countFaceNotRegister =
                    response.data.data.countFaceNotRegister
            }
            //Load theo thiết bị
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
            //Load theo thiết bị
            else if (areaId == 0) {
                this.statistic = response.data.data
            }
        },

        async loadCountVehicleTable(areaId) {
            var vm = this
            await this.$services
                .get(`/dashboard/dashboardCountVehicleEvent/${areaId}`)
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
            var vm = this
            await this.$services
                .get(`/dashboard/mapCountProtection/${areaId}`)
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

/* Trạng thái khi search box đang hiển thị (active state) */
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

.custom-control-box {
    background: #fff;
    border-radius: 10px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    padding: 5px 0px;
}

.btn-circle {
    width: 32px;
    height: 32px;
    border-radius: 30% !important;
    border: none !important;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0;
    transition:
        background-color 0.2s,
        box-shadow 0.2s;
    color: #808080;
    margin-bottom: 5px !important;

    &:hover,
    &:focus {
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        border-color: #a9a9a9;
    }

    .feather-icon {
        stroke: #808080;
        margin-bottom: 3px !important;
    }
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

.stats-subtitle {
    color: rgba(255, 255, 255, 0.8);
    font-size: 10px;
    font-weight: 500;
    letter-spacing: 0.3px;
}

/* Adjust body padding to account for the new header */
.custom-stats-body {
    padding: 8px 6px;
}

/* Style cho cột thông tin bên phải - Compact Version */
.statistics-container {
    height: 100%;
    display: flex;
    flex-direction: column;
}

.stat-group-compact {
    border-radius: 6px;
    border: 1px solid #e2e8f0;
    overflow: hidden;
    width: 100%;
    margin-bottom: 25px !important;
    /* giảm khoảng cách giữa các nhóm */
    padding-top: 0;
    padding-bottom: 0;
}

.stat-group-header-compact {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    padding: 6px 10px;
    font-size: 12px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.3px;
    display: flex;
    align-items: center;
    min-height: 28px;
}

.stat-item-compact {
    display: flex;
    align-items: center;
    padding: 4px 8px 6px;
    transition: background-color 0.2s ease;
}

.stat-row-compact {
    display: flex;
    padding: 0;
}

.stat-triple-row {
    display: flex;
    padding: 0;
}

.stat-indicator-compact {
    width: 3px;
    height: 24px;
    border-radius: 1.5px;
    margin-right: 8px;
    flex-shrink: 0;
}

.stat-content-compact {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    align-items: center;
}

.stat-value-compact {
    font-size: 18px;
    font-weight: 900;
    color: #1a202c;
    line-height: 1.2;
    margin-bottom: 2px;
    text-shadow: 0 1px 1px rgba(0, 0, 0, 0.05);
    letter-spacing: -0.5px;
}

.stat-label-compact {
    font-size: 9px;
    color: #718096;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0;
    line-height: 1;
}

/* Responsive adjustments for compact design */
@media (min-width: 1500px) {
    .stat-value-compact {
        font-size: 20px;
    }

    .stat-label-compact {
        font-size: 10px;
    }

    .stat-group-header-compact {
        padding: 6px 10px;
        font-size: 12px;
    }
}

/* Animation cho giá trị được cập nhật */
.stat-value-compact {
    transition: all 0.3s ease;
}

/* Hiệu ứng selected cho camera trong sidebar */
.selected-camera {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%) !important;
    color: white !important;
    transform: translateX(5px);
    box-shadow: 0 3px 10px rgba(102, 126, 234, 0.3);
    border-left: 4px solid #4a90e2 !important;
}

.selected-camera .feather-icon {
    color: white !important;
}

/* Hiệu ứng hover cho camera items */
.list-group-item.cursor-pointer:hover:not(.selected-camera) {
    background-color: #f8f9fa;
    transform: translateX(2px);
}

.list-group-item.cursor-pointer {
  padding-left: 30px;
}

/* Camera List Redesign */
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

/* Camera search styles */
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

/* Camera filter styles - Compact Design */
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
    background: #cbd5e1; /* Màu xám nhạt khi không active */
}

.offline-dot {
    background: #cbd5e1; /* Màu xám nhạt khi không active */
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

/* Custom scrollbar for camera list */
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

/* Animation for smooth transitions */
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
</style>
