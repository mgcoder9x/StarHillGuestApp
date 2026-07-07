<!-- src/views/vehicleSessions/Detail.vue -->
<template>
    <div>
        <!-- <b-card :header="$t('VehicleSessions.Detail.Header')"> -->
        <b-card>
            <!-- Ảnh 2x2 -->
            <div class="media-grid">
                <div class="img-box">
                    <div class="img-title">
                        {{ $t('VehicleSessions.Detail.InPlate') }}
                    </div>
                    <img
                        v-if="inPlateUrl"
                        :src="fullUrl(inPlateUrl)"
                        alt="in-plate"
                    />
                    <div v-else class="img-empty">
                        {{ $t('VehicleSessions.Detail.NoImage') }}
                    </div>
                </div>

                <div class="img-box">
                    <div class="img-title">
                        {{ $t('VehicleSessions.Detail.OutPlate') }}
                    </div>
                    <img
                        v-if="outPlateUrl"
                        :src="fullUrl(outPlateUrl)"
                        alt="out-plate"
                    />
                    <div v-else class="img-empty">
                        {{ $t('VehicleSessions.Detail.NoImage') }}
                    </div>
                </div>

                <div class="img-box">
                    <div class="img-title">
                        {{ $t('VehicleSessions.Detail.InOverview') }}
                    </div>
                    <img
                        v-if="inOverviewUrl"
                        :src="fullUrl(inOverviewUrl)"
                        alt="in-overview"
                    />
                    <div v-else class="img-empty">
                        {{ $t('VehicleSessions.Detail.NoImage') }}
                    </div>
                </div>

                <div class="img-box">
                    <div class="img-title">
                        {{ $t('VehicleSessions.Detail.OutOverview') }}
                    </div>
                    <img
                        v-if="outOverviewUrl"
                        :src="fullUrl(outOverviewUrl)"
                        alt="out-overview"
                    />
                    <div v-else class="img-empty">
                        {{ $t('VehicleSessions.Detail.NoImage') }}
                    </div>
                </div>
            </div>

            <!-- Thông tin -->
            <b-row class="mt-2">
                <b-col md="3">
                    <b-form-group
                        :label="$t('VehicleSessions.Field.LicensePlate')"
                    >
                        <b-form-input :value="detail.licensePlates" disabled />
                    </b-form-group>
                </b-col>

                <b-col md="3">
                    <b-form-group
                        :label="$t('VehicleSessions.Field.VehicleType')"
                    >
                        <b-form-input
                            :value="getVehicleTypeName(detail.vehicleType)"
                            disabled
                        />
                    </b-form-group>
                </b-col>

                <b-col md="3">
                    <b-form-group :label="$t('VehicleSessions.Field.Owner')">
                        <b-form-input :value="detail.ownerName" disabled />
                    </b-form-group>
                </b-col>

                <b-col md="3">
                    <b-form-group :label="$t('VehicleSessions.Field.Color')">
                        <b-form-input
                            :value="getColorName(detail.color)"
                            disabled
                        />
                    </b-form-group>
                </b-col>

                <b-col md="3">
                    <b-form-group :label="$t('VehicleSessions.Field.InTime')">
                        <b-form-input
                            :value="formatDt(detail.inTime)"
                            disabled
                        />
                    </b-form-group>
                </b-col>

                <b-col md="3">
                    <b-form-group :label="$t('VehicleSessions.Field.OutTime')">
                        <b-form-input
                            :value="formatDt(detail.outTime)"
                            disabled
                        />
                    </b-form-group>
                </b-col>

                <!-- <b-col md="3">
                    <b-form-group :label="$t('VehicleSessions.Field.InGate')">
                        <b-form-input :value="detail.inGateName" disabled />
                    </b-form-group>
                </b-col>

                <b-col md="3">
                    <b-form-group :label="$t('VehicleSessions.Field.OutGate')">
                        <b-form-input :value="detail.outGateName" disabled />
                    </b-form-group>
                </b-col> -->
                <b-col md="3">
                    <b-form-group :label="$t('VehicleSessions.Field.InGate')">
                        <b-form-input :value="detail.areaName" disabled />
                    </b-form-group>
                </b-col>

                <b-col md="3">
                    <b-form-group :label="$t('VehicleSessions.Field.OutGate')">
                        <b-form-input :value="detail.areaName" disabled />
                    </b-form-group>
                </b-col>

                <b-col md="3">
                    <b-form-group :label="$t('VehicleSessions.Field.Stay')">
                        <b-form-input
                            :value="detail.stayMinutes || 0"
                            disabled
                        />
                    </b-form-group>
                </b-col>

                <b-col md="3">
                    <b-form-group :label="$t('VehicleSessions.Field.Status')">
                        <b-form-input
                            :value="getStatusName(detail.status)"
                            disabled
                        />
                    </b-form-group>
                </b-col>
            </b-row>

            <!-- Footer: nút Quay lại -->
            <template #footer>
                <div class="d-flex justify-content-end">
                    <b-button variant="outline-primary" @click="goBack">
                        {{ $t('Button.Back') }}
                    </b-button>
                </div>
            </template>
        </b-card>
    </div>
</template>

<script>
/* eslint-disable */
import moment from 'moment'
import { listVehicleType, listColor } from '@/data'
import { FILETYPE } from '@/constants/fileType'

export default {
    name: 'VehicleSessionsDetail',
    data() {
        return {
            detail: {},
            inPlateUrl: null,
            outPlateUrl: null,
            inOverviewUrl: null,
            outOverviewUrl: null,
            listVehicleType,
            listColor,
        }
    },
    computed: {
        baseURL() {
            return process.env.VUE_APP_BASE_URL
        },
        // Đảm bảo đúng param (:sessionId) như route đang dùng
        sessionId() {
            return this.$route.params.sessionId || this.$route.params.id
        },
    },
    async created() {
        await this.loadDetail()
        await this.loadImages()
    },
    methods: {
        async loadDetail() {
            if (!this.sessionId) return
            const res = await this.$services.get(
                `/vehicleEvent/sessions/${this.sessionId}`
            )
            this.detail = res.data?.data || {}
        },
        async loadImages() {
            if (this.detail.eventIdIn) {
                const rIn = await this.$services.get(
                    `/event/eventFilesById/${this.detail.eventIdIn}`
                )
                const filesIn = rIn.data || []
                const plateIn = filesIn.find(
                    (f) => f.fileType === FILETYPE.LICENSE_PLATE_IMAGE
                )
                const overIn = filesIn.find((f) =>
                    [FILETYPE.OVERVIEW_IMAGE, FILETYPE.IMAGE].includes(
                        f.fileType
                    )
                )
                this.inPlateUrl = plateIn ? plateIn.filePath : null
                this.inOverviewUrl = overIn ? overIn.filePath : null
            }
            if (this.detail.eventIdOut) {
                const rOut = await this.$services.get(
                    `/event/eventFilesById/${this.detail.eventIdOut}`
                )
                const filesOut = rOut.data || []
                const plateOut = filesOut.find(
                    (f) => f.fileType === FILETYPE.LICENSE_PLATE_IMAGE
                )
                const overOut = filesOut.find((f) =>
                    [FILETYPE.OVERVIEW_IMAGE, FILETYPE.IMAGE].includes(
                        f.fileType
                    )
                )
                this.outPlateUrl = plateOut ? plateOut.filePath : null
                this.outOverviewUrl = overOut ? overOut.filePath : null
            }
        },
        formatDt(v) {
            return v ? moment(v).format('DD/MM/YYYY HH:mm:ss') : ''
        },
        getVehicleTypeName(id) {
            const x = this.listVehicleType.find((v) => v.id == id)
            return x ? this.$t(x.text) : ''
        },
        getColorName(id) {
            const x = this.listColor.find((v) => v.id == id)
            return x ? this.$t(x.text) : ''
        },
        getStatusName(s) {
            const k =
                s === 1
                    ? 'Matched'
                    : s === 2
                      ? 'InOnly'
                      : s === 3
                        ? 'OutOnly'
                        : 'Unknown'
            return this.$t(`VehicleSessions.Status.${k}`)
        },
        fullUrl(p) {
            return `${this.baseURL}${p}`
        },
        goBack() {
            if (window.history.length > 1) this.$router.back()
            else this.$router.push({ path: '/event/vehicleSessionsEvent/list' })
        },
    },
}
</script>

<style scoped>
.media-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 12px;
}
.img-box {
    width: 100%;
    height: 200px;
    border: 1px dashed #dcdcdc;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
    background: #fafafa;
}
.img-box img {
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
}
.img-title {
    position: absolute;
    top: 6px;
    left: 8px;
    font-weight: 600;
    font-size: 12px;
    color: #666;
}
.img-empty {
    color: #999;
    font-style: italic;
}
@media (max-width: 992px) {
    .media-grid {
        grid-template-columns: 1fr;
    }
    .img-box {
        height: 220px;
    }
}
</style>
