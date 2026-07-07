<!-- MonitorDashboard.vue -->
<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-container fluid class="px-1">
            <!-- ===== TOOLBAR ===== -->
            <div
                class="monitor-toolbar d-flex align-items-center justify-content-between"
            >
                <div class="d-flex align-items-center" style="width: 30%">
                    <span class="mr-50" style="white-space: nowrap">{{
                        $t('Live.SelectConfig') || 'Chọn cấu hình'
                    }}</span>
                    <b-form-select
                        v-model="selectedConfigId"
                        :options="configOptions"
                        size="sm"
                        class="mr-50"
                        @change="applySavedConfig"
                    />
                    <b-button
                        size="sm"
                        variant="primary"
                        class="mr-75"
                        @click="openPreConfig"
                    >
                        <Icon icon="mdi:plus" />
                    </b-button>
                    <b-button
                        size="sm"
                        variant="primary"
                        class="mr-25"
                        @click="openSaveConfig"
                    >
                        <Icon icon="mdi:content-save" />
                    </b-button>
                    <b-button
                        v-if="selectedConfigId"
                        variant="primary"
                        size="sm"
                        class="mr-75"
                        @click="deleteMonitorConfig()"
                    >
                        <Icon icon="material-symbols:delete" />
                    </b-button>
                </div>

                <div class="d-flex align-items-center">
                    <strong class="mr-1 text-uppercase">{{
                        currentGateName
                    }}</strong>
                    <div class="toolbar-icons ml-75">
                        <b-button
                            v-b-tooltip.hover="$t('Live.ChangeLanePosition')"
                            size="sm"
                            :variant="
                                reorderMode ? 'primary' : 'outline-secondary'
                            "
                            class="mr-25"
                            @click="toggleReorder"
                        >
                            <Icon icon="mdi:swap-horizontal" />
                        </b-button>

                        <b-button
                            v-b-tooltip.hover="$t('Live.History')"
                            size="sm"
                            variant="outline-secondary"
                            class="mr-25"
                            @click="showHistory = !showHistory"
                        >
                            <Icon icon="mdi:history" />
                        </b-button>

                        <!-- <b-button
                            v-b-tooltip.hover="$t('Live.SaveConfiguration')"
                            size="sm"
                            variant="success"
                            class="mr-25"
                            @click="openSaveConfig"
                        >
                            <Icon icon="mdi:database" />
                        </b-button> -->

                        <!-- Ẩn tạm danh sách camera -->
                        <!--
            <b-button
              v-b-tooltip.hover="$t('Live.CameraList')"
              size="sm"
              variant="outline-secondary"
              class="mr-25"
              @click="showCamList = !showCamList"
            >
              <Icon icon="mdi:cctv" />
            </b-button>
            -->
                    </div>
                </div>
            </div>

            <b-row no-gutters>
                <!-- ===== LEFT: LANES ===== -->
                <b-col :md="showSide ? 8 : 12" class="pr-md-50 pt-50">
                    <draggable
                        v-model="lanesView"
                        :options="{
                            animation: 180,
                            handle: '.lane-header' /* kéo ở bất kỳ vị trí header */,
                            disabled: !reorderMode,
                        }"
                    >
                        <transition-group
                            name="fade"
                            tag="div"
                            class="lanes-grid"
                            :class="{ 'single-col': lanesView.length === 1 }"
                        >
                            <div
                                v-for="lane in lanesView"
                                :key="lane.laneId"
                                class="lane-card"
                            >
                                <!-- Lane header -->
                                <div
                                    class="lane-header lane-handle"
                                    :class="lane.direction === 1 ? 'in' : 'out'"
                                >
                                    <div class="title">
                                        {{ lane.laneName }} —
                                        {{
                                            lane.vehicleType === 1
                                                ? $t('Live.Car')
                                                : $t('Live.Motor')
                                        }}
                                    </div>
                                    <div class="right">
                                        <b-badge
                                            :class="
                                                lane.direction === 1
                                                    ? 'tag-in'
                                                    : 'tag-out'
                                            "
                                        >
                                            {{
                                                lane.direction === 1
                                                    ? $t('Live.In')
                                                    : $t('Live.Out')
                                            }}
                                        </b-badge>
                                    </div>
                                </div>

                                <!-- Cameras: nếu chỉ có 1 cam thì full width -->
                                <div
                                    :class="[
                                        'cams-2',
                                        { single: isSingleCam(lane) },
                                    ]"
                                >
                                    <!-- Cam biển số -->
                                    <div
                                        v-if="lane.sources.plate"
                                        class="cam-cell"
                                    >
                                        <div class="cam-title">Cam biển số</div>
                                        <div
                                            class="player-box"
                                            :class="
                                                lane.camStatus.plate
                                                    ? 'on'
                                                    : 'off'
                                            "
                                        >
                                            <span
                                                class="status-dot"
                                                :class="
                                                    lane.camStatus.plate
                                                        ? 'on'
                                                        : 'off'
                                                "
                                            ></span>
                                            <div class="cam-overlay">
                                                {{ lane.deviceNames.plate }}
                                            </div>
                                            <FlvPlayer
                                                :source="lane.sources.plate"
                                                :video-index="`plate-${lane.laneId}`"
                                            >
                                                <canvas
                                                    :id="`videoplate-${lane.laneId}`"
                                                    class="render-canvas"
                                                />
                                            </FlvPlayer>
                                        </div>
                                    </div>

                                    <!-- Cam toàn cảnh -->
                                    <div
                                        v-if="lane.sources.ov"
                                        class="cam-cell"
                                    >
                                        <div class="cam-title">
                                            Cam toàn cảnh
                                        </div>
                                        <div
                                            class="player-box"
                                            :class="
                                                lane.camStatus.ov ? 'on' : 'off'
                                            "
                                        >
                                            <span
                                                class="status-dot"
                                                :class="
                                                    lane.camStatus.ov
                                                        ? 'on'
                                                        : 'off'
                                                "
                                            ></span>
                                            <div class="cam-overlay">
                                                {{ lane.deviceNames.ov }}
                                            </div>
                                            <FlvPlayer
                                                :source="lane.sources.ov"
                                                :video-index="`ov-${lane.laneId}`"
                                            >
                                                <canvas
                                                    :id="`videoov-${lane.laneId}`"
                                                    class="render-canvas"
                                                />
                                            </FlvPlayer>
                                        </div>
                                    </div>
                                </div>

                                <!-- Info cards -->
                                <div class="info-cards">
                                    <!-- Làn vào -->
                                    <div
                                        v-if="lane.direction === 1"
                                        class="info-card in"
                                    >
                                        <div class="row row-small">
                                            <div class="col">
                                                <label
                                                    >{{
                                                        $t('Live.EntryTimeIn')
                                                    }}:</label
                                                >
                                                <div class="val">
                                                    {{
                                                        lane.info.in.date || '-'
                                                    }}
                                                </div>
                                            </div>
                                            <div class="col col-owner">
                                                <label
                                                    >{{
                                                        $t('Live.VehicleOwner')
                                                    }}:</label
                                                >
                                                <div class="val">
                                                    {{
                                                        lane.info.in.owner ||
                                                        '-'
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                        <div class="row row-small">
                                            <div class="col col-plate">
                                                <label
                                                    >{{
                                                        $t('Live.Plate')
                                                    }}:</label
                                                >
                                                <div class="val plate">
                                                    {{
                                                        lane.info.in.plate ||
                                                        '-'
                                                    }}
                                                </div>
                                            </div>
                                            <!--{{ lane || '123' }}-->
                                            <div class="col col-img">
                                                <div class="imgbox">
                                                    <img
                                                        v-if="
                                                            lane.info.in
                                                                .plateImg
                                                        "
                                                        :src="`${baseURL}/${lane.info.in.plateImg}`"
                                                        alt="plate-in"
                                                    />
                                                    <Icon
                                                        v-else
                                                        icon="mdi:image-outline"
                                                        class="placeholder"
                                                    />
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    <!-- Làn ra -->
                                    <div v-else class="info-card out">
                                        <!--{{ lane.info.out.timeIn || '123' }}-->
                                        <div class="row row-small">
                                            <div class="col">
                                                <label
                                                    >{{
                                                        $t('Live.EntryTimeIn')
                                                    }}:</label
                                                >
                                                <div class="val">
                                                    {{
                                                        lane.info.out.timeIn ||
                                                        '-'
                                                    }}
                                                </div>
                                            </div>
                                            <div class="col">
                                                <label
                                                    >{{
                                                        $t('Live.ExitTimeOut')
                                                    }}:</label
                                                >
                                                <div class="val">
                                                    {{
                                                        lane.info.out.timeOut ||
                                                        '-'
                                                    }}
                                                </div>
                                            </div>
                                        </div>
                                        <div class="row row-small">
                                            <div class="col col-plate">
                                                <label
                                                    >{{
                                                        $t('Live.Plate')
                                                    }}:</label
                                                >
                                                <div class="val plate">
                                                    {{
                                                        lane.info.out.plate ||
                                                        '-'
                                                    }}
                                                </div>
                                            </div>
                                            <div class="col col-owner">
                                                <label
                                                    >{{
                                                        $t('Live.VehicleOwner')
                                                    }}:</label
                                                >
                                                <div class="val">
                                                    {{
                                                        lane.info.out.owner ||
                                                        '-'
                                                    }}
                                                </div>
                                            </div>
                                        </div>

                                        <div class="plate-duo">
                                            <figure class="plate-fig">
                                                <div class="plate-img">
                                                    <img
                                                        v-if="
                                                            lane.info.out
                                                                .plateImgIn
                                                        "
                                                        :src="`${baseURL}/${lane.info.out.plateImgIn}`"
                                                        alt="plate-in"
                                                    />
                                                    <Icon
                                                        v-else
                                                        icon="mdi:image-outline"
                                                        class="placeholder"
                                                    />
                                                </div>
                                                <figcaption
                                                    class="chip chip-primary"
                                                >
                                                    {{
                                                        $t(
                                                            'Live.EntryLicensePlate'
                                                        )
                                                    }}
                                                </figcaption>
                                            </figure>
                                            <figure class="plate-fig">
                                                <div class="plate-img">
                                                    <img
                                                        v-if="
                                                            lane.info.out
                                                                .plateImgOut
                                                        "
                                                        :src="`${baseURL}/${lane.info.out.plateImgOut}`"
                                                        alt="plate-out"
                                                    />
                                                    <Icon
                                                        v-else
                                                        icon="mdi:image-outline"
                                                        class="placeholder"
                                                    />
                                                </div>
                                                <figcaption
                                                    class="chip chip-danger"
                                                >
                                                    {{
                                                        $t(
                                                            'Live.ExitLicensePlate'
                                                        )
                                                    }}
                                                </figcaption>
                                            </figure>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </transition-group>
                    </draggable>
                </b-col>

                <!-- ===== RIGHT: HISTORY ===== (ẩn danh sách camera) -->
                <b-col v-if="showSide" md="4" class="pt-50">
                    <b-card v-show="showHistory" class="mb-1">
                        <div
                            class="d-flex align-items-center justify-content-between mb-50"
                        >
                            <h6 class="mb-0">
                                {{ $t('Live.ListVehicleEvent') }}
                            </h6>
                            <b-button
                                size="sm"
                                variant="outline-primary"
                                @click="loadRecentEvents"
                                >{{ $t('Button.Refresh') }}</b-button
                            >
                        </div>

                        <div class="history-scroll">
                            <b-table
                                small
                                hover
                                :items="recentEvents"
                                :fields="historyFields"
                                responsive="sm"
                                show-empty
                                empty-text="Không có dữ liệu"
                                head-variant="light"
                            >
                                <template #cell(time)="{ item }">{{
                                    item.time
                                }}</template>
                                <template #cell(plate)="{ item }">{{
                                    item.plate
                                }}</template>
                                <template #cell(vehicle)="{ item }">
                                    {{
                                        item.vehicleType === 2
                                            ? $t('Live.Car')
                                            : $t('Live.Motor')
                                    }}
                                </template>
                                <template #cell(lane)="{ item }">{{
                                    item.laneName
                                }}</template>
                                <template #cell(direction)="{ item }">{{
                                    item.direction === 1
                                        ? $t('Live.In')
                                        : $t('Live.Out')
                                }}</template>
                            </b-table>
                        </div>
                    </b-card>
                </b-col>
            </b-row>

            <!-- ===== MODALS ===== -->
            <b-modal
                id="preconfigModal"
                v-model="showPreconfig"
                :title="
                    $t('Live.AreaMonitorConfig') || 'Cấu hình khu vực giám sát'
                "
                :no-close-on-esc="true"
                :no-close-on-backdrop="true"
                hide-header-close
                size="lg"
            >
                <b-row>
                    <b-col cols="12">
                        <b-form-group
                            :label="$t('Live.Area') || 'Khu vực'"
                            label-class="required"
                        >
                            <tree-select
                                v-model="selectedAreaId"
                                :options="areaTree"
                                :multiple="false"
                                :searchable="true"
                                label="label"
                                :reduce="(n) => n.id"
                                :placeholder="
                                    $t('Common.form.placeholder.selectValue') ||
                                    '--'
                                "
                                @input="onAreaChange"
                            />
                        </b-form-group>
                    </b-col>

                    <b-col cols="12">
                        <b-form-group
                            :label="$t('Live.Lanes') || 'Làn/Cổng'"
                            label-class="required"
                        >
                            <v-select
                                v-model="selectedLaneIds"
                                :options="laneOptions"
                                multiple
                                label="text"
                                :reduce="(x) => x.id"
                                :placeholder="
                                    $t('Common.form.placeholder.selectValue') ||
                                    '--'
                                "
                            />
                        </b-form-group>
                    </b-col>
                </b-row>
                <template #modal-footer>
                    <b-button
                        variant="primary"
                        class="mr-50"
                        @click="applyPreConfig"
                    >
                        {{ $t('Button.Save') || 'Ghi lại' }}
                    </b-button>
                    <b-button
                        variant="outline-secondary"
                        @click="resetPreConfig"
                    >
                        {{ $t('Button.Cancel') || 'Hủy' }}
                    </b-button>
                </template>
            </b-modal>

            <b-modal
                id="saveConfig"
                v-model="showConfigName"
                :title="$t('Lưu cấu hình') || 'Lưu cấu hình'"
                :no-close-on-esc="true"
                :no-close-on-backdrop="true"
                hide-header-close
                size="lg"
            >
                <b-row>
                    <b-col cols="12">
                        <b-form-group
                            :label="$t('Live.ConfigName') || 'Tên cấu hình'"
                            label-class="required"
                        >
                            <validation-provider
                                #default="{ errors }"
                                :name="$t('Live.ConfigName')"
                                rules="required"
                            >
                                <b-form-input
                                    id="h-searchForm-text"
                                    v-model="name"
                                    :state="errors.length > 0 ? false : null"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                </b-row>

                <template #modal-footer>
                    <b-button
                        variant="primary"
                        class="mr-50"
                        @click="saveMonitorConfig"
                    >
                        {{ $t('Button.Save') || 'Ghi lại' }}
                    </b-button>
                    <b-button
                        variant="outline-secondary"
                        @click="closeSaveConfig"
                    >
                        {{ $t('Button.Cancel') || 'Hủy' }}
                    </b-button>
                </template>
            </b-modal>
        </b-container>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import draggable from 'vuedraggable'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import signalRService from '@/utils/signalr-service'
import moment from 'moment'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'

export default {
    name: 'MonitorDashboard',
    mixins: [authorizationMixin],
    components: { draggable },

    data() {
        return {
            // modal
            showPreconfig: true,
            showConfigName: false,

            // saved configs
            selectedConfigId: null,
            configOptions: [],

            // pre-config
            areaTree: [],
            selectedAreaId: null,
            laneOptions: [],
            selectedLaneIds: [],

            // view
            lanesView: [],
            reorderMode: false,
            name: '',
            // side panes
            showCamList: false, // ẨN danh sách camera
            showHistory: true,

            // cam list
            cameraList: [],
            expandedAreas: [],

            // history (10 dòng)
            recentEvents: [],

            // caches
            devicesByLane: {}, // { [laneId]: { plate, ov, laneMeta } }
            historyRefreshAt: 0,
            compId: null,
            _handleNewEvent: null,
        }
    },

    computed: {
        historyFields() {
            return [
                { key: 'time', label: this.$t('Live.Time') },
                { key: 'plate', label: this.$t('Live.Plate') },
                { key: 'vehicle', label: this.$t('Live.Vehicle') },
                { key: 'lane', label: this.$t('Live.Lane') },
                { key: 'direction', label: this.$t('Live.Direction') },
            ]
        },
        baseURL() {
            return process.env.VUE_APP_BASE_URL
        },
        nodeMediaServer() {
            return process.env.VUE_APP_NODE_MEDIA_SERVER
        },
        showSide() {
            // chỉ còn History (camera list ẩn)
            return this.showHistory
        },
        currentGateName() {
            return this.currentAreaName || 'CỔNG CHÍNH'
        },
        currentAreaName() {
            const n = (this.areaTree || []).find(
                (x) => String(x.id) === String(this.selectedAreaId)
            )
            return n ? n.label : ''
        },
        groupedCams() {
            const map = {}
            ;(this.cameraList || []).forEach((cam) => {
                const id = cam.areaId || 'na'
                if (!map[id])
                    map[id] = { id, name: cam.areaName || '-', cameras: [] }
                map[id].cameras.push(cam)
            })
            return Object.values(map)
        },
    },

    async created() {
        const accessToken = await this.$services.getUserData()
        this.compId = accessToken.companyId
        await this.loadAreas()
        await this.loadConfigs()
        this.$nextTick(() => this.$bvModal.show('preconfigModal'))

        await signalRService.connect('notificationHub')

        this._handleNewEvent = (data) => {
            debugger
            let payload = data
            if (typeof payload === 'string') {
                try {
                    payload = JSON.parse(payload)
                } catch (err) {
                    console.error('[NewEvent] JSON parse error:', err, data)
                    return
                }
            }
            this.pushDataEvent(payload)
        }
        signalRService.on('NewEvent', this._handleNewEvent)
    },

    beforeDestroy() {
        if (signalRService?.off && this._handleNewEvent) {
            try {
                signalRService.off('NewEvent', this._handleNewEvent)
            } catch (_) {}
        }
    },

    methods: {
        // ===== helper =====
        isSingleCam(lane) {
            const s = lane?.sources || {}
            return !!s.plate + !!s.ov === 1
        },

        // Chuẩn hoá VehicleType: event(2=ô tô, 1=xe máy) -> lane(1=ô tô, 2=xe máy)
        normalizeEventVehicleType(v) {
            if (v === 2 || String(v) === '2') return 1 // ô tô
            if (v === 1 || String(v) === '1') return 2 // xe máy
            return null
        },

        // Tìm lane phù hợp nhất theo điểm
        findBestLaneForEvent(evt) {
            debugger
            console.log('evt', evt)
            let best = null
            let bestScore = -1

            for (const lane of this.lanesView) {
                const pair = this.devicesByLane[lane.laneId] || {}
                let score = 0

                // 1) Ưu tiên trùng thiết bị
                if (
                    String(pair?.plate?.id) === String(evt.deviceId) ||
                    String(pair?.ov?.id) === String(evt.deviceId)
                )
                    score += 6

                // 2) Khu vực
                const laneAreaId =
                    lane.areaId ??
                    pair?.laneMeta?.areaId ??
                    pair?.laneMeta?.AreaId
                if (evt.areaId && String(laneAreaId) === String(evt.areaId))
                    score += 3

                // 3) Phương tiện
                if (
                    evt.vehicleType &&
                    String(lane.vehicleType) === String(evt.vehicleType)
                )
                    score += 2

                // 4) Hướng (phụ)
                if (
                    evt.direction &&
                    String(lane.direction) === String(evt.direction)
                )
                    score += 1

                if (score > bestScore) {
                    bestScore = score
                    best = lane
                }
            }
            console.log('best', best)
            return best
        },

        // ===== configs =====
        async loadConfigs() {
            const r = await this.$services
                .get('/lanes/monitor-configs')
                .catch(() => null)
            const rows = r?.data?.data || r?.data || []
            this.configOptions = rows.map((x) => ({
                value: x.id ?? x.Id,
                text: x.name ?? x.Name,
            }))
        },
        async confirmDelete() {
            return await this.$swal.fire({
                title: this.$t('common.confirmation.delete.title'),
                text: this.$t('common.confirmation.delete.message'),
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: this.$t('Message.Agree'),
                cancelButtonText: this.$t('Message.Exit'),
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-outline-danger ml-1',
                },
                buttonsStyling: false,
            })
        },
        showSuccessToast(message) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(`Response.ErrorCode.${message}`),
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
        async applySavedConfig() {
            if (!this.selectedConfigId) return
            const r = await this.$services
                .get(`/lanes/monitor-configs/${this.selectedConfigId}`)
                .catch(() => null)
            const cfg = r?.data?.data || r?.data
            if (!cfg) return

            this.selectedAreaId = cfg.areaId ?? cfg.AreaId
            let laneIds = (cfg.laneIds ?? cfg.LaneIds) || []
            if (!Array.isArray(laneIds)) {
                //convert thành mảng số
                laneIds = laneIds.split(',').map(Number)
            }
            this.selectedLaneIds = laneIds

            await this.onAreaChange(false)
            await this.applyPreConfig()
        },

        async deleteMonitorConfig() {
            const { isConfirmed } = await this.confirmDelete()
            if (isConfirmed) {
                try {
                    await this.$services.delete(
                        `/lanes/monitor-configs/${this.selectedConfigId}`
                    )
                    this.toastSuccess('Đã xoá cấu hình')

                    // Reset tất cả về trạng thái ban đầu
                    this.selectedConfigId = null
                    this.selectedAreaId = null
                    this.selectedLaneIds = []
                    this.laneOptions = []
                    this.lanesView = []
                    this.recentEvents = []
                    this.devicesByLane = {}
                    this.name = ''
                    this.reorderMode = false

                    await this.loadConfigs()

                    // Mở lại modal cấu hình
                    // this.$nextTick(() => {
                    //     this.showPreconfig = true
                    //     this.$bvModal.show('preconfigModal')
                    // })
                } catch (err) {
                    this.toastError(
                        String(
                            err?.response?.data?.message || err?.message || err
                        )
                    )
                }
            }
        },

        // ===== pre-config =====
        async loadAreas() {
            const r = await this.$services
                .get('/lookup/areas-tree')
                .catch(() => null)
            this.areaTree = r?.data?.data || r?.data || []
        },

        async onAreaChange(resetLaneIds = true) {
            try {
                if (resetLaneIds) {
                    this.selectedLaneIds = []
                }
                console.log('selected', this.selectedAreaId)
                const r = await this.$services.get(
                    `/lanes?filterAreaId=${this.selectedAreaId}&pageSize=9999`
                )
                // const r = await this.$services.get('/lanes', {
                //     params: {
                //         filterAreaId: this.selectedAreaId,
                //         FilterAreaId: this.selectedAreaId,
                //         areaId: this.selectedAreaId,
                //         AreaId: this.selectedAreaId,
                //         pageSize: 9999,
                //     },
                // })
                const rows = this._pickArray(r)
                this.laneOptions = rows.map(this._toLaneOption)
            } catch (err) {
                console.error('onAreaChange error', err)
                this.laneOptions = []
                this.toastError(
                    'Không lấy được danh sách làn. Vui lòng thử lại.'
                )
            }
        },

        _toLaneOption(x) {
            return {
                id: x.id ?? x.Id ?? x.laneId ?? x.LaneId,
                text:
                    x.laneName ??
                    x.LaneName ??
                    x.name ??
                    x.Name ??
                    `Lane ${x.id ?? x.Id}`,
                direction: x.direction ?? x.Direction ?? 1,
                vehicleType: x.vehicleType ?? x.VehicleType ?? 1,
                areaId: x.areaId ?? x.AreaId,
                areaName: x.areaName ?? x.AreaName,
            }
        },

        async applyPreConfig() {
            if (!this.selectedAreaId || !this.selectedLaneIds.length) {
                this.toastError('Cần chọn Khu vực và Làn')
                return
            }
            this.showPreconfig = false
            this.$bvModal.hide('preconfigModal')

            this.$nextTick(async () => {
                try {
                    await this.loadLanesDevices()
                    await this.buildLanesView()
                    await this.loadRecentEvents()
                } catch (e) {
                    this.toastError(
                        'Không tải được dữ liệu sau khi lưu cấu hình.'
                    )
                }
            })
        },

        async loadCams() {
            try {
                const { data } = await this.$services.get('/device/getAllCam', {
                    params: { filterAreaId: this.selectedAreaId },
                })
                this.cameraList = data.data.map((cam) => ({
                    ...cam,
                    status: cam.status,
                    online: cam.status === 1,
                }))
            } catch (_) {
                /* bỏ qua vì tạm ẩn sidebar camera */
            }
        },

        resetPreConfig() {
            // this.selectedAreaId = null
            // this.selectedLaneIds = []
            // this.laneOptions = []
            this.$bvModal.hide('preconfigModal')
        },

        openPreConfig() {
            this.showPreconfig = true
            this.$bvModal.show('preconfigModal')
        },

        // ===== devices/sources =====
        async fetchDevice(deviceId) {
            if (!deviceId || Number(deviceId) <= 0) return null
            const r = await this.$services
                .get(`/device/${deviceId}`)
                .catch(() => null)
            const d = r?.data?.data || r?.data
            if (!d) return null
            return {
                id: d.id,
                code: d.code,
                name: d.name,
                areaId: d.areaId,
                areaName: d.areaName,
                compId: d.compId,
                online: Number(d.status) === 1,
            }
        },

        async loadLanesDevices() {
            this.devicesByLane = {}
            const tasks = this.selectedLaneIds.map(async (laneId) => {
                const lr = await this.$services
                    .get(`/lanes/${laneId}`)
                    .catch(() => null)
                const lane = lr?.data?.data || lr?.data || {}
                const plate = await this.fetchDevice(
                    lane.plateCameraId || lane.PlateCameraId
                )
                const ov = await this.fetchDevice(
                    lane.overviewCameraId || lane.OverviewCameraId
                )
                this.devicesByLane[laneId] = { plate, ov, laneMeta: lane }
            })
            await Promise.all(tasks)
        },

        buildSrc(d) {
            if (!d || !d.code || !d.compId) return null
            return `${this.nodeMediaServer}${d.compId}/${d.code}.flv`
        },

        async buildLanesView() {
            const metaMap = {}
            this.laneOptions.forEach((x) => (metaMap[x.id] = x))
            const view = []

            for (const laneId of this.selectedLaneIds) {
                const meta = metaMap[laneId] || {}
                const pair = this.devicesByLane[laneId] || {}

                view.push({
                    laneId,
                    laneName: meta?.text || `Lane ${laneId}`,
                    direction: meta?.direction ?? pair.laneMeta?.direction ?? 1,
                    vehicleType:
                        meta?.vehicleType ?? pair.laneMeta?.vehicleType ?? 1,
                    areaId:
                        meta?.areaId ??
                        pair.laneMeta?.areaId ??
                        pair.laneMeta?.AreaId ??
                        null,
                    sources: {
                        plate: this.buildSrc(pair.plate),
                        ov: this.buildSrc(pair.ov),
                    },
                    deviceNames: {
                        plate: pair?.plate?.name || 'CAM BIỂN SỐ',
                        ov: pair?.ov?.name || 'CAM TOÀN CẢNH',
                    },
                    camStatus: {
                        plate: !!pair.plate?.online,
                        ov: !!pair.ov?.online,
                    },
                    info: {
                        in: { date: '', time: '', plate: '', plateImg: '' },
                        out: {
                            timeIn: '',
                            timeOut: '',
                            plate: '',
                            owner: '',
                            plateImgIn: '',
                            plateImgOut: '',
                        },
                    },
                })
            }
            this.lanesView = view
        },

        // ===== realtime =====
        pushDataEvent(e) {
            debugger
            this.onNewEvent(e)
        },

        onNewEvent(raw) {
            debugger
            if (!raw) return
            const rawCompId =
                raw.CompId ?? raw.compId ?? raw.companyId ?? raw.CompanyId
            if (
                rawCompId &&
                this.compId &&
                String(rawCompId) !== String(this.compId)
            ) {
                // Event thuộc công ty khác → bỏ qua hoàn toàn
                console.log(
                    '[NewEvent] Bỏ qua event của công ty khác:',
                    rawCompId,
                    '(hiện tại:',
                    this.compId,
                    ')'
                )
                return
            }
            // =====================================================
            const v = raw.VehicleEvent || raw.vehicleEvent || {}
            const evt = {
                deviceId:
                    raw.DeviceId ?? raw.deviceId ?? v.DeviceId ?? v.deviceId,
                areaId: raw.AreaId ?? raw.areaId ?? null,
                direction: v.Direction ?? raw.Direction ?? 1,
                vehicleType: this.normalizeEventVehicleType(
                    v.VehicleType ?? raw.VehicleType
                ),
                license: v.LicensePlates ?? raw.LicensePlate ?? '',
                access: raw.AccessTime ?? raw._AccessTime ?? raw.accessTime,
                exit: raw.ExitTime ?? raw.exitTime,
                image: raw.Image ?? raw.image,
                plateImgIn: raw.PlateImgIn ?? raw.plateImgIn,
                plateImgOut: raw.PlateImgOut ?? raw.plateImgOut,
                owner: v.Owner ?? v.owner ?? '',
                compId: v.compId ?? raw.CompId,
            }

            const lane = this.findBestLaneForEvent(evt)
            if (!lane) return

            const accessTime = evt.access ? moment(evt.access) : moment()
            const exitTime = evt.exit ? moment(evt.exit) : null
            
            if (Number(evt.direction) === 1) {
                // VÀO
                lane.info.in.owner = evt.owner || ''
                lane.info.in.date = accessTime.format('DD/MM/YYYY HH:mm:ss')
                lane.info.in.time = accessTime.format('HH:mm')
                lane.info.in.plate = evt.license || ''
                lane.info.in.plateImg =
                    evt.plateImgIn || evt.image || lane.info.in.plateImg
            } else {
                // RA
                lane.info.out.timeIn =
                    accessTime && accessTime.isValid()
                        ? accessTime.format('DD/MM/YYYY HH:mm:ss')
                        : ''
                lane.info.out.timeOut =
                    exitTime && exitTime.isValid()
                        ? exitTime.format('DD/MM/YYYY HH:mm:ss')
                        : ''
                lane.info.out.plate = evt.license || ''
                lane.info.out.owner = evt.owner || ''
                lane.info.out.plateImgIn =
                    evt.plateImgIn || lane.info.out.plateImgIn
                lane.info.out.plateImgOut =
                    evt.plateImgOut || evt.image || lane.info.out.plateImgOut
            }
            console.log('lane after', lane)
            const now = Date.now()
            if (now - this.historyRefreshAt > 1500) {
                this.historyRefreshAt = now
                this.loadRecentEvents()
            }
        },

        // ===== history =====
        // async loadRecentEvents() {
        //     try {
        //         const r = await this.$services.get('/vehicleEvent', {
        //             params: {
        //                 filterAreaId: this.selectedAreaId,
        //                 FilterAreaId: this.selectedAreaId,
        //                 filterLaneIds: (this.selectedLaneIds || []).join(','),
        //                 page: 1,
        //                 pageSize: 10,
        //             },
        //         })
        //         const rows = this._pickArray(r)
        //         this.recentEvents = (rows || []).slice(0, 10).map((row) => ({
        //             time:
        //                 row.time ||
        //                 row.Time ||
        //                 moment(row.AccessTime || row.accessTime).format(
        //                     'HH:mm DD/MM'
        //                 ),
        //             plate: row.plate || row.Plate || row.LicensePlate || '',
        //             vehicleType: row.vehicleType || row.VehicleType || 1,
        //             laneName: row.laneName || row.LaneName || '',
        //             direction: row.direction || row.Direction || 1,
        //         }))
        //     } catch (e) {
        //         this.recentEvents = []
        //     }
        // },
        async loadRecentEvents() {
            try {
                const laneIds = (this.selectedLaneIds || [])
                    .map((id) => `laneIds=${id}`)
                    .join('&')
                const res = await this.$services.get(
                    `/vehicleEvent/recent?${laneIds}&top=10`
                )
                const data = res.data.data
                if (data) {
                    this.recentEvents = data.map((item) => ({
                        time: this.formatDate(item.accessTime),
                        plate: item.licensePlate,
                        vehicleType: item.vehicleType,
                        laneName: item.laneName,
                        direction: item.direction,
                    }))
                }
            } catch (e) {
                this.recentEvents = []
            }
        },
        formatDate(ISODate) {
            const date = new Date(ISODate)
            date.setHours(date.getHours() - 7)
            return `${date.getHours().toString().padStart(2, '0')}:${date
                .getMinutes()
                .toString()
                .padStart(
                    2,
                    '0'
                )} ${date.getDate().toString().padStart(2, '0')}/${(
                date.getMonth() + 1
            )
                .toString()
                .padStart(2, '0')}`
        },
        // ===== side panes & toolbar =====
        toggleArea(id) {
            this.expandedAreas = this.expandedAreas.includes(id)
                ? this.expandedAreas.filter((x) => x !== id)
                : [...this.expandedAreas, id]
        },
        toggleReorder() {
            this.reorderMode = !this.reorderMode
        },
        openSaveConfig() {
            if (this.selectedConfigId) {
                this.name =
                    this.configOptions.find(
                        (x) => x.value === this.selectedConfigId
                    )?.text || ''
            }

            this.showConfigName = true
            this.$bvModal.show('saveConfig')
        },
        closeSaveConfig() {
            this.name = ''
            this.$bvModal.hide('saveConfig')
        },

        async saveMonitorConfig(e) {
            e.preventDefault()
            if (!this.selectedAreaId || !this.selectedLaneIds.length) {
                this.toastError('Cần chọn Khu vực và Làn')
                return
            }

            this.$refs.rules.validate().then(async (success) => {
                if (success) {
                    const orderedLaneIds = this.lanesView
                        .map((x) => x.laneId)
                        .join(',')
                    const payload = {
                        configId: this.selectedConfigId ?? undefined,
                        areaId: this.selectedAreaId,
                        laneIds: orderedLaneIds,
                        order: orderedLaneIds,
                        name: this.name,
                    }

                    await this.$services
                        .post('/lanes/monitor-configs', payload)
                        .then(async () => {
                            this.$emit('success')
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t(`Success.Create`),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                            this.name = '' // Reset tên cấu hình sau khi lưu thành công

                            // Refresh danh sách cấu hình
                            await this.loadConfigs()

                            // Chọn option cuối cùng (cấu hình vừa lưu)
                            // if (this.configOptions.length > 0) {
                            //     this.selectedConfigId =
                            //         this.configOptions[
                            //             this.configOptions.length - 1
                            //         ].value
                            // }
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    // text: `${error.response?.data?.message || error.message}`,
                                    text: this.$t(
                                        `Lanes.Errors.${error.response?.data?.message || error.message}`
                                    ),
                                },
                            })
                        })
                    this.showConfigName = false
                }
            })
        },

        _pickArray(resp) {
            const cands = [
                resp?.data?.data,
                resp?.data?.rows,
                resp?.data?.items,
                resp?.data?.result,
                resp?.data?.Results,
                resp?.data?.Records,
                resp?.data,
                resp,
            ]
            for (const c of cands) {
                if (Array.isArray(c)) return c
                if (c && typeof c === 'object') {
                    if (Array.isArray(c.data)) return c.data
                    if (Array.isArray(c.rows)) return c.rows
                    if (Array.isArray(c.items)) return c.items
                }
            }
            return []
        },

        // ===== toast =====
        toastSuccess(title) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: { title, icon: 'CheckIcon', variant: 'success' },
            })
        },
        toastError(text) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t('Error.Error') || 'Lỗi',
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text,
                },
            })
        },
    },
}
</script>

<style scoped>
/* ===== Toolbar ===== */
.monitor-toolbar {
    padding: 8px 10px;
    border: 1px solid #e6e9ef;
    border-radius: 6px;
    background: #fff;
}
.toolbar-icons .btn {
    width: 34px;
    padding: 0;
    display: inline-flex;
    align-items: center;
    justify-content: center;
}

/* ===== Grid of lanes ===== */
.lanes-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(520px, 1fr));
    grid-gap: 12px;
}
.lanes-grid.single-col {
    grid-template-columns: 1fr; /* 1 làn => full chiều ngang */
}
@media (min-width: 1440px) {
    .lanes-grid {
        grid-template-columns: repeat(2, 1fr);
    }
}

/* ===== Lane Card ===== */
.lane-card {
    border: 1px solid #cfd6e4;
    border-radius: 8px;
    background: #fff;
    overflow: hidden;
}
.lane-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 10px;
    color: #fff;
    cursor: grab; /* kéo ở header */
}
.lane-header .title {
    font-weight: 800;
    letter-spacing: 0.3px;
    text-transform: uppercase;
}
.lane-header.in {
    background: #2e7d32;
} /* xanh vào */
.lane-header.out {
    background: #c62828;
} /* đỏ ra */
.tag-in,
.tag-out {
    padding: 2px 10px;
    border-radius: 999px;
    font-weight: 700;
    color: #fff;
    background: #00000020;
    border: 1px solid #ffffff66;
}
.lane-handle {
    cursor: grab;
}

/* ===== 2 cameras per lane ===== */
.cams-2 {
    display: grid;
    grid-template-columns: 1fr 1fr;
    grid-gap: 8px;
    padding: 8px;
    border-top: 1px solid #eef1f6;
}
.cams-2.single {
    grid-template-columns: 1fr;
}
.cam-cell {
    position: relative; /* <<< QUAN TRỌNG: thêm dòng này */
    background: #fafafa;
    border: 1px dashed #e1e3e8;
    border-radius: 8px;
    padding: 6px;
}

/* Khi chỉ có 1 cam (full width) vẫn căn giữa đẹp */
.cams-2.single .cam-title {
    top: 12px; /* hơi xuống tí cho đẹp khi full màn hình */
    font-size: 14px;
    padding: 6px 16px;
}
.cam-title {
    position: absolute;
    top: 8px;
    left: 50%;
    transform: translateX(-50%);
    background: rgba(0, 0, 0, 0.75);
    color: #fff;
    font-weight: 700;
    font-size: 13px;
    padding: 4px 12px;
    border-radius: 6px;
    z-index: 20;
    white-space: nowrap;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.3);
    pointer-events: none; /* không che mất click nếu cần */
}
.player-box {
    height: 260px; /* mở rộng vùng video */
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 6px;
    position: relative;
    overflow: hidden;
    border: 1px solid #dfe3ec;
    background: #0b0f1a;
}
.player-box.on {
    box-shadow: inset 0 0 0 2px #10b98133;
}
.player-box.off {
    box-shadow: inset 0 0 0 2px #ef444433;
    filter: grayscale(0.25);
}
.render-canvas {
    width: 100%;
    height: 100%;
    display: block;
}
.status-dot {
    position: absolute;
    top: 8px;
    right: 8px;
    width: 10px;
    height: 10px;
    border-radius: 50%;
    box-shadow: 0 0 0 2px #ffffffcc;
}
.status-dot.on {
    background: #16a34a;
}
.status-dot.off {
    background: #ef4444;
}

/* tên camera overlay trong player */
.cam-overlay {
    position: absolute;
    left: 8px;
    top: 8px;
    padding: 2px 8px;
    border-radius: 6px;
    background: #00000055;
    color: #fff;
    font-size: 12px;
    font-weight: 700;
}

.xxl-icon {
    font-size: 46px;
}

/* ===== Info cards (gọn) ===== */
.info-cards {
    padding: 8px;
}
.info-card {
    border: 1px solid #d9dfea;
    border-radius: 6px;
    background: #fff;
    padding: 6px;
}
.info-card.in label,
.info-card.out label {
    font-size: 12px;
    color: #334155;
    margin-bottom: 2px;
}
.row-small {
    display: flex;
    gap: 8px;
    margin-bottom: 8px;
}
.row-small .col {
    flex: 1;
}
.row-small .col-plate {
    flex: 1;
    min-width: 0;
}
.row-small .col-img {
    flex: 1;
    min-width: 0;
}
.row-small .col-owner {
    flex: 1;
    min-width: 0;
}
.val {
    background: #f8fafc;
    border: 1px solid #e5e7eb;
    border-radius: 4px;
    padding: 4px 6px;
    min-height: 30px;
}
.val.plate {
    color: #10b981;
    font-weight: 700;
}
.imgbox {
    height: 90px;
    border: 1px solid #e3e8f1;
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #fff;
    margin-top: 18px;
}
.imgbox img {
    max-width: 100%;
    max-height: 100%;
    object-fit: cover;
}

/* ===== Biển số RA/VÀO ===== */
.plate-duo {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 10px;
    margin-top: 6px;
}
.plate-fig {
    position: relative;
    margin: 0;
}
.plate-img {
    height: 120px;
    border: 1px solid #e3e8f1;
    border-radius: 8px;
    background: #fff;
    display: flex;
    align-items: center;
    justify-content: center;
}
.plate-img img {
    max-width: 100%;
    max-height: 100%;
    object-fit: cover;
}
.chip {
    position: absolute;
    left: 8px;
    bottom: 8px;
    padding: 2px 8px;
    border-radius: 999px;
    font-size: 11px;
    font-weight: 700;
    color: #fff;
    backdrop-filter: blur(2px);
}
.chip-primary {
    background: #2563eb;
}
.chip-danger {
    background: #dc2626;
}

/* ===== Right side ===== */
.history-scroll {
    max-height: 420px;
    overflow: auto;
}
.sidebar-scroll {
    max-height: 340px;
    overflow: auto;
}
.area-row {
    margin: 4px 0;
    padding: 4px 6px;
    display: flex;
    align-items: center;
}

/* transitions */
.fade-enter-active,
.fade-leave-active {
    transition: all 0.15s ease;
}
.fade-enter,
.fade-leave-to {
    opacity: 0;
    transform: scale(0.98);
}
</style>

