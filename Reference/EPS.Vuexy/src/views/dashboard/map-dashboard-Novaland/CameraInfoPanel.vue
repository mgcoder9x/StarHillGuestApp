<template>
    <div v-if="showCard" class="camera-info-panel">
        <div class="camera-panel-container">
            <!-- Header Section - Redesigned -->
            <div class="camera-panel-header">
                <!-- Close button - moved to absolute position -->
                <b-button
                    variant="link"
                    class="close-btn-absolute"
                    :title="$t('Events.MapDashboard.CameraInfo.Close')"
                    @click="$emit('close')"
                >
                    <feather-icon icon="XIcon" size="18" />
                </b-button>

                <div class="header-content">
                    <div class="camera-title-section">
                        <div class="camera-icon-section">
                            <feather-icon
                                icon="VideoIcon"
                                size="18"
                                class="camera-icon"
                                :class="{
                                    'camera-icon-offline': device.status === 0,
                                }"
                            />
                        </div>
                        <div class="camera-text-info">
                            <h3 class="camera-name" :title="device.name">
                                {{ device.name }}
                            </h3>
                            <div
                                v-if="device.eventTypeName"
                                class="camera-subtitle"
                            >
                                <feather-icon icon="TagIcon" size="12" />
                                <span>{{ device.eventTypeName }}</span>
                            </div>
                        </div>
                    </div>

                    <!-- Live button moved to right side -->
                    <!-- <div class="header-actions">
                        <b-button
                            :disabled="device.status == 0"
                            variant="outline-primary"
                            class="live-btn-right"
                            :title="
                                $t('Events.MapDashboard.CameraInfo.LiveView')
                            "
                            @click="$emit('start-live', device.id)"
                        >
                            <feather-icon icon="PlayIcon" size="16" />
                        </b-button>
                    </div> -->
                </div>
            </div>

            <!-- Content Section -->
            <div class="camera-panel-content">
                <div class="tab-navigation">
                    <button
                        class="tab-btn"
                        :class="{ active: activeTab === 'events' }"
                        @click="activeTab = 'events'"
                    >
                        <feather-icon icon="ActivityIcon" size="14" />
                        <span>{{
                            $t('Events.MapDashboard.CameraInfo.Events')
                        }}</span>
                    </button>
                    <!-- <button
                        class="tab-btn"
                        :class="{ active: activeTab === 'statistics' }"
                        @click="activeTab = 'statistics'"
                    >
                        <feather-icon icon="BarChart2Icon" size="14" />
                        <span>{{
                            $t('Events.MapDashboard.CameraInfo.DailyStatistics')
                        }}</span>
                    </button> -->
                </div>

                <!-- Events Tab -->
                <div v-show="activeTab === 'events'" class="tab-content">
                    <div
                        ref="eventsContainer"
                        class="events-scroll-container"
                        @scroll="handleEventsScroll"
                    >
                        <div v-if="events.length === 0" class="empty-state">
                            <feather-icon
                                icon="CalendarIcon"
                                size="36"
                                class="empty-icon"
                            />
                            <p class="empty-text">
                                {{
                                    $t(
                                        'Events.MapDashboard.CameraInfo.NoEvents'
                                    )
                                }}
                            </p>
                        </div>

                        <div
                            v-for="(event, index) in events"
                            :key="'event-' + event.eventId + '-' + index"
                            class="event-card"
                        >
                            <div class="event-image-container">
                                <img
                                    :src="event.image"
                                    class="event-image"
                                    :alt="
                                        $t(
                                            'Events.MapDashboard.CameraInfo.AlertImage'
                                        )
                                    "
                                    loading="lazy"
                                />
                                <div class="event-overlay">
                                    <feather-icon icon="ZoomInIcon" size="20" />
                                </div>
                            </div>

                            <div class="event-details">
                                <div class="event-header">
                                    <h4 class="event-area">
                                        {{ event.areaName }}
                                    </h4>
                                    <span class="event-time">{{
                                        event.accessTimeStr
                                    }}</span>
                                </div>

                                <div
                                    v-if="event.warningName"
                                    class="event-warning"
                                >
                                    <feather-icon
                                        icon="AlertTriangleIcon"
                                        size="14"
                                    />
                                    <span>{{
                                        getLocalizedWarning(event.warningName)
                                    }}</span>
                                </div>
                            </div>
                        </div>

                        <!-- Loading State -->
                        <div v-if="isLoadingMore" class="loading-state">
                            <b-spinner small variant="secondary" />
                            <span>{{
                                $t('Events.MapDashboard.CameraInfo.LoadingMore')
                            }}</span>
                        </div>

                        <!-- End State -->
                        <div
                            v-if="!hasMoreEvents && events.length > 0"
                            class="end-state"
                        >
                            <span>{{
                                $t(
                                    'Events.MapDashboard.CameraInfo.AllEventsLoaded'
                                )
                            }}</span>
                        </div>
                    </div>
                </div>

                <!-- Statistics Tab -->
                <div v-show="activeTab === 'statistics'" class="tab-content">
                    <div class="statistics-container">
                        <!-- Protective Gear Stats -->
                        <div
                            v-if="device.eventTypeId == 205"
                            class="stat-group"
                        >
                            <h4 class="stat-group-title">
                                <feather-icon icon="ShieldIcon" size="16" />
                                <span class="stat-title-text">{{
                                    this.$i18n.locale === 'vi'
                                        ? 'Đồ bảo hộ lao động'
                                        : 'Protective Gear'
                                }}</span>
                            </h4>
                            <div class="stat-items">
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{
                                            statistic.countEnoughProtection || 0
                                        }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.ProtectiveGear.Sufficient'
                                            )
                                        }}
                                    </div>
                                </div>
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countLackProtection || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.ProtectiveGear.Insufficient'
                                            )
                                        }}
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Face Recognition Stats -->
                        <div
                            v-if="device.eventTypeId == 200"
                            class="stat-group"
                        >
                            <h4 class="stat-group-title">
                                <feather-icon icon="UserIcon" size="16" />
                                <span class="stat-title-text">{{
                                    this.$i18n.locale === 'vi'
                                        ? 'Nhận diện khuôn mặt'
                                        : 'Identification'
                                }}</span>
                            </h4>
                            <div class="stat-items">
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countFaceRegister || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.FaceRecognition.Registered'
                                            )
                                        }}
                                    </div>
                                </div>
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{
                                            statistic.countFaceNotRegister || 0
                                        }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.FaceRecognition.Unregistered'
                                            )
                                        }}
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Vehicle Stats -->
                        <div
                            v-if="device.eventTypeId == 300"
                            class="stat-group"
                        >
                            <h4 class="stat-group-title">
                                <feather-icon icon="TruckIcon" size="16" />
                                <span class="stat-title-text">
                                    {{
                                        this.$i18n.locale === 'vi'
                                            ? 'Phương tiện'
                                            : 'Vehicle'
                                    }}</span
                                >
                            </h4>
                            <div class="stat-grid">
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countCarIn || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.Vehicle.CarIn'
                                            )
                                        }}
                                    </div>
                                </div>
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countCarOut || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.Vehicle.CarOut'
                                            )
                                        }}
                                    </div>
                                </div>
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countMotoIn || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.Vehicle.MotorbikeIn'
                                            )
                                        }}
                                    </div>
                                </div>
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countMotoOut || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.Vehicle.MotorbikeOut'
                                            )
                                        }}
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Restricted Zone Stats -->
                        <div
                            v-if="device.eventTypeId == 400"
                            class="stat-group"
                        >
                            <h4 class="stat-group-title">
                                <feather-icon icon="ShieldOffIcon" size="16" />
                                <span class="stat-title-text">{{
                                    this.$i18n.locale === 'vi'
                                        ? 'Vùng cấm'
                                        : 'Forbidden Zone'
                                }}</span>
                            </h4>
                            <div class="stat-items">
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countPerson || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.RestrictedZone.People'
                                            )
                                        }}
                                    </div>
                                </div>
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countVehicle || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.RestrictedZone.Vehicles'
                                            )
                                        }}
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Fire/Smoke Stats -->
                        <div
                            v-if="device.eventTypeId == 500"
                            class="stat-group"
                        >
                            <h4 class="stat-group-title">
                                <feather-icon icon="ZapIcon" size="16" />
                                <span class="stat-title-text">{{
                                    this.$i18n.locale === 'vi'
                                        ? 'Cháy & Khói'
                                        : 'Fire & Smoke'
                                }}</span>
                            </h4>
                            <div class="stat-items">
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countFire || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.FireSmoke.Fire'
                                            )
                                        }}
                                    </div>
                                </div>
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countSmoke || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.FireSmoke.Smoke'
                                            )
                                        }}
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Conveyor Belt Stats -->
                        <div
                            v-if="device.eventTypeId == 601"
                            class="stat-group"
                        >
                            <h4 class="stat-group-title">
                                <feather-icon icon="SettingsIcon" size="16" />
                                <span class="stat-title-text">{{
                                    this.$i18n.locale === 'vi'
                                        ? 'Băng tải'
                                        : 'Conveyor Belt'
                                }}</span>
                            </h4>
                            <div class="stat-grid">
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countConveryorTorn || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.ConveyorBelt.Torn'
                                            )
                                        }}
                                    </div>
                                </div>
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{
                                            statistic.countConveryorOverflow ||
                                            0
                                        }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.ConveyorBelt.Overflow'
                                            )
                                        }}
                                    </div>
                                </div>
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countDeviated || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.ConveyorBelt.Deviation'
                                            )
                                        }}
                                    </div>
                                </div>
                                <div class="stat-item">
                                    <div class="stat-value">
                                        {{ statistic.countOversized || 0 }}
                                    </div>
                                    <div class="stat-label">
                                        {{
                                            $t(
                                                'Events.MapDashboard.CameraInfo.Statistics.ConveyorBelt.Oversized'
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
    </div>
</template>

<script>
export default {
    name: 'CameraInfoPanel',
    props: {
        showCard: {
            type: Boolean,
            default: false,
        },
        device: {
            type: Object,
            required: true,
            default: () => ({
                id: null,
                name: '',
                eventTypeName: '',
                eventTypeId: null,
                status: 0,
            }),
        },
        events: {
            type: Array,
            required: true,
            default: () => [],
        },
        statistic: {
            type: Object,
            required: true,
            default: () => ({}),
        },
        isLoadingMore: {
            type: Boolean,
            default: false,
        },
        hasMoreEvents: {
            type: Boolean,
            default: true,
        },
    },
    data() {
        return {
            activeTab: 'events',
        }
    },
    methods: {
        handleEventsScroll(event) {
            const container = event.target
            const nearBottom =
                container.scrollHeight -
                    container.scrollTop -
                    container.clientHeight <
                50

            if (nearBottom && !this.isLoadingMore && this.hasMoreEvents) {
                this.$emit('load-more-events')
            }
        },
        getLocalizedWarning(warningName) {
            // Map các warning name từ API sang i18n keys
            const warningMap = {
                'Thiếu mũ':
                    'Events.MapDashboard.CameraInfo.Warnings.MissingHelmet',
                'Thiếu khẩu trang':
                    'Events.MapDashboard.CameraInfo.Warnings.MissingMask',
                'Thiếu găng tay':
                    'Events.MapDashboard.CameraInfo.Warnings.MissingGloves',
                Đủ: 'Events.MapDashboard.CameraInfo.Warnings.Sufficient',
                'Không đủ':
                    'Events.MapDashboard.CameraInfo.Warnings.Insufficient',
                'Người chưa đăng ký':
                    'Events.MapDashboard.CameraInfo.Warnings.UnregisteredPerson',
                'Phương tiện vào vùng cấm':
                    'Events.MapDashboard.CameraInfo.Warnings.VehicleInRestrictedArea',
                'Người vào vùng cấm':
                    'Events.MapDashboard.CameraInfo.Warnings.PersonInRestrictedArea',
                'Phát hiện cháy':
                    'Events.MapDashboard.CameraInfo.Warnings.FireDetected',
                'Phát hiện khói':
                    'Events.MapDashboard.CameraInfo.Warnings.SmokeDetected',
                'Băng chuyền bị rách':
                    'Events.MapDashboard.CameraInfo.Warnings.ConveyorTorn',
                'Băng chuyền tràn':
                    'Events.MapDashboard.CameraInfo.Warnings.ConveyorOverflow',
            }

            // Nếu có mapping thì dùng i18n, không thì hiển thị nguyên text
            const i18nKey = warningMap[warningName]
            return i18nKey ? this.$t(i18nKey) : warningName
        },
    },
}
</script>

<style lang="scss" scoped>
.camera-info-panel {
    position: absolute;
    top: 15px;
    left: 15px;
    width: 400px;
    max-width: 90vw;
    z-index: 1001;
    font-family:
        'Inter',
        -apple-system,
        BlinkMacSystemFont,
        sans-serif;
}

.camera-panel-container {
    background: #ffffff;
    border-radius: 12px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.12);
    overflow: hidden;
    border: 1px solid #e5e7eb;
}

.camera-panel-header {
    padding: 16px 20px;
    background: #ffffff;
    border-bottom: 1px solid #f1f3f4;
    position: relative;
}

.close-btn-absolute {
    position: absolute;
    top: 8px;
    right: 8px;
    width: 24px;
    height: 24px;
    padding: 0;
    border: none !important;
    background: transparent !important;
    color: #9ca3af;
    transition: all 0.2s ease;
    z-index: 10;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;

    &:hover {
        background: rgba(0, 0, 0, 0.05) !important;
        color: #374151;
        transform: scale(1.1);
    }

    &:active {
        transform: scale(0.95);
    }

    &:focus {
        box-shadow: none !important;
        outline: none !important;
    }

    svg {
        transition: transform 0.1s ease;
    }
}

.header-content {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
    padding-right: 32px; // Space for close button
}

.camera-title-section {
    display: flex;
    align-items: flex-start;
    gap: 12px;
    flex: 1;
    min-width: 0;
}

.camera-icon-section {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 6px;
    flex-shrink: 0;
}

.camera-icon {
    color: #10b981;

    &.camera-icon-offline {
        color: #ef4444;
    }
}

.header-actions {
    display: flex;
    align-items: flex-start;
    flex-shrink: 0;
    margin-top: 2px; // Align with camera name
}

.live-btn-right {
    width: 44px;
    height: 44px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0;
    border: 1px solid #10b981 !important;
    color: #10b981 !important;
    background: #ffffff !important;
    font-weight: 500;
    font-size: 13px;
    transition: all 0.2s ease;

    &:hover:not(:disabled) {
        background: #10b981 !important;
        color: #ffffff !important;
        border-color: #10b981 !important;
        transform: translateY(-1px);
        box-shadow: 0 4px 8px rgba(16, 185, 129, 0.25);
    }

    &:disabled {
        opacity: 0.4;
        cursor: not-allowed;
        background: #f3f4f6 !important;
        border-color: #d1d5db !important;
        color: #9ca3af !important;
        transform: none;
        box-shadow: none;
    }

    &:active:not(:disabled),
    &:focus:not(:disabled) {
        background: #10b981 !important;
        color: #ffffff !important;
        border-color: #10b981 !important;
        transform: translateY(0);
        box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3) !important;
    }

    svg {
        transition: transform 0.1s ease;
    }
}

// Remove old live button styles
.live-btn-small {
    display: none;
}

.camera-text-info {
    flex: 1;
    min-width: 0;
}

.camera-name {
    margin: 0 0 4px 0;
    font-size: 15px;
    font-weight: 600;
    color: #111827;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    line-height: 1.3;
}

.camera-subtitle {
    display: flex;
    align-items: flex-start;
    gap: 6px;
    color: #6b7280;
    font-size: 12px;
    font-weight: 500;
    margin-top: 2px;
    line-height: 1.3;

    svg {
        flex-shrink: 0;
        margin-top: 1px;
    }

    span {
        flex: 1;
        word-wrap: break-word;
        overflow-wrap: break-word;
        white-space: normal;
        line-height: 1.3;
    }
}

.action-buttons {
    display: none;
}

.action-btn {
    display: none;
}

.close-btn {
    display: none;
}

.camera-panel-content {
    padding: 0;
}

.tab-navigation {
    display: flex;
    background: #ffffff;
    border-bottom: 1px solid #e5e7eb;
    padding: 0;
}

.tab-btn {
    flex: 1;
    padding: 12px 16px;
    background: none;
    border: none;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    font-weight: 500;
    font-size: 13px;
    color: #6b7280;
    transition: all 0.2s ease;
    border-bottom: 2px solid transparent;

    &:hover {
        background: #f9fafb;
        color: #374151;
    }

    &.active {
        color: #1f2937;
        background: #ffffff;
        border-bottom-color: #3b82f6;
    }
}

.event-count {
    background: #e5e7eb;
    color: #374151;
    font-size: 10px;
    padding: 2px 6px;
    border-radius: 8px;
    min-width: 18px;
    text-align: center;
    font-weight: 600;
}

.tab-content {
    padding: 0;
}

.events-scroll-container {
    max-height: 60vh;
    overflow-y: auto;
    padding: 16px;
}

.empty-state {
    text-align: center;
    padding: 40px 20px;
    color: #9ca3af;
}

.empty-icon {
    margin-bottom: 12px;
    opacity: 0.6;
}

.empty-text {
    margin: 0;
    font-weight: 500;
    font-size: 14px;
}

.event-card {
    background: #ffffff;
    border-radius: 8px;
    overflow: hidden;
    margin-bottom: 16px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    border: 1px solid #e5e7eb;
    transition: all 0.2s ease;

    &:hover {
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    }

    &:last-child {
        margin-bottom: 0;
    }
}

.event-image-container {
    position: relative;
    width: 100%;
    height: 180px;
    overflow: hidden;

    &:hover .event-overlay {
        opacity: 1;
    }
}

.event-image {
    width: 100%;
    height: 100%;
    object-fit: cover;
    transition: transform 0.3s ease;

    .event-image-container:hover & {
        transform: scale(1.02);
    }
}

.event-overlay {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.4);
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    opacity: 0;
    transition: opacity 0.2s ease;
    cursor: pointer;
}

.event-details {
    padding: 16px;
}

.event-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 12px;
    margin-bottom: 12px;
}

.event-area {
    margin: 0;
    font-size: 15px;
    font-weight: 600;
    color: #1f2937;
    flex: 1;
}

.event-time {
    font-size: 11px;
    color: #9ca3af;
    font-weight: 500;
    white-space: nowrap;
}

.event-warning {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 12px;
    background: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: 6px;
    color: #dc2626;
    font-size: 12px;
    font-weight: 500;
}

.loading-state {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 12px;
    padding: 20px;
    color: #6b7280;
    font-weight: 500;
}

.end-state {
    text-align: center;
    padding: 16px;
    color: #9ca3af;
    font-size: 12px;
    font-weight: 500;
    border-top: 1px solid #f3f4f6;
}

.statistics-container {
    padding: 20px;
    max-height: 60vh;
    overflow-y: auto;
}

.stat-group {
    margin-bottom: 24px;

    &:last-child {
        margin-bottom: 0;
    }
}

.stat-group-title {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    margin: 0 0 16px;
    font-size: 15px;
    font-weight: 600;
    color: #1f2937;
    line-height: 1.4;
}

.stat-title-text {
    flex: 1;
    line-height: 1.4;
    word-wrap: break-word;
    overflow-wrap: break-word;
}

.stat-items {
    display: flex;
    gap: 12px;
}

.stat-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
}

.stat-item {
    background: #f9fafb;
    padding: 16px;
    border-radius: 8px;
    text-align: center;
    border: 1px solid #e5e7eb;
    transition: all 0.2s ease;
    flex: 1;

    &:hover {
        transform: translateY(-1px);
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }
}

.stat-value {
    font-size: 20px;
    font-weight: 700;
    margin-bottom: 4px;
    color: #1f2937;
}

.stat-label {
    font-size: 11px;
    font-weight: 500;
    color: #6b7280;
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

/* Custom scrollbar */
.events-scroll-container::-webkit-scrollbar,
.statistics-container::-webkit-scrollbar {
    width: 4px;
}

.events-scroll-container::-webkit-scrollbar-track,
.statistics-container::-webkit-scrollbar-track {
    background: #f3f4f6;
}

.events-scroll-container::-webkit-scrollbar-thumb,
.statistics-container::-webkit-scrollbar-thumb {
    background: #d1d5db;
    border-radius: 2px;
}

/* Responsive */
@media (max-width: 768px) {
    .camera-info-panel {
        width: calc(100vw - 30px);
        left: 15px;
        right: 15px;
    }

    .camera-panel-header {
        padding: 14px 18px;
    }

    .close-btn-absolute {
        top: 6px;
        right: 6px;
        width: 22px;
        height: 22px;
    }

    .header-content {
        padding-right: 28px;
        gap: 12px;
        flex-direction: column;
        align-items: stretch;
    }

    .camera-title-section {
        gap: 10px;
    }

    .camera-name {
        font-size: 14px;
    }

    .header-actions {
        margin-top: 8px;
        justify-content: flex-end;
    }

    .live-btn-right {
        width: 40px;
        height: 40px;
    }
}
</style>
