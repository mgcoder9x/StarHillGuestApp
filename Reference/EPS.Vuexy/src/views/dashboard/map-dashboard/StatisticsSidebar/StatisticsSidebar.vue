<template>
    <div class="statistics-sidebar h-100">
        <!-- Header -->
        <div class="statistics-header">
            <div class="statistics-header-content">
                <div class="statistics-icon-wrapper">
                    <feather-icon
                        icon="BarChart2Icon"
                        size="16"
                        class="statistics-icon"
                    />
                </div>
                <div class="statistics-header-text">
                    <div class="statistics-title">
                        {{ $t('Events.MapDashboard.MonitoringStatistics') }}
                    </div>
                    <div class="statistics-subtitle">
                        {{ $t('Events.MapDashboard.RealtimeData') }}
                    </div>
                </div>
                <button
                    class="statistics-close-btn"
                    :title="$t('Map.Controls.HideStatistics')"
                    @click="$emit('close')"
                >
                    <feather-icon
                        icon="ChevronRightIcon"
                        size="18"
                    />
                </button>
            </div>
        </div>

        <!-- Body với groups -->
        <div class="statistics-body">
            <div class="statistics-container">
                <sidebar-group
                    v-for="group in statisticsGroups"
                    :key="group.id"
                    :icon="group.icon"
                    :title="group.title"
                    :color="group.color"
                >
                    <sidebar-item
                        v-for="item in group.items"
                        :key="item.key"
                        :color="item.color"
                        :value="item.value"
                        :label="item.label"
                        :item-class="item.itemClass || ''"
                    />
                </sidebar-group>
            </div>
        </div>
    </div>
</template>

<script>
import SidebarGroup from './SidebarGroup.vue'
import SidebarItem from './SidebarItem.vue'

export default {
    name: 'StatisticsSidebar',
    components: {
        SidebarGroup,
        SidebarItem,
    },
    props: {
        visible: {
            type: Boolean,
            default: true,
        },
        vehiclesCount: {
            type: Array,
            default: () => [],
            required: false,
        },
        statisticAll: {
            type: Object,
            default: () => ({}),
            required: false,
        },
    },
    computed: {
        totalVehicleIn() {
            return this.vehiclesCount.reduce(
                (total, vehicle) => total + (vehicle.in || 0),
                0
            )
        },

        totalVehicleOut() {
            return this.vehiclesCount.reduce(
                (total, vehicle) => total + (vehicle.out || 0),
                0
            )
        },

        statisticsGroups() {
            return [
                {
                    id: 'vehicle',
                    icon: 'TruckIcon',
                    title: 'Events.MapDashboard.Vehicle',
                    color: 'success',
                    items: [
                        {
                            key: 'in',
                            color: 'success',
                            value: this.totalVehicleIn,
                            label: 'Events.MapDashboard.In',
                            itemClass: 'w-50',
                        },
                        {
                            key: 'out',
                            color: 'danger',
                            value: this.totalVehicleOut,
                            label: 'Events.MapDashboard.Out',
                            itemClass: 'w-50',
                        },
                    ],
                },
                {
                    id: 'forbidden',
                    icon: 'ShieldOffIcon',
                    title: 'Events.MapDashboard.ForbiddenZone',
                    color: 'warning',
                    items: [
                        {
                            key: 'person',
                            color: 'warning',
                            value: this.statisticAll.countPerson || 0,
                            label: 'Events.MapDashboard.Employee',
                            itemClass: 'mr-1',
                        },
                    ],
                },
                {
                    id: 'fire',
                    icon: 'mdi:fire',
                    title: 'Events.MapDashboard.FireSmoke',
                    color: 'danger',
                    items: [
                        {
                            key: 'fire',
                            color: 'danger',
                            value: this.statisticAll.countFire || 0,
                            label: 'Events.MapDashboard.Fire',
                            itemClass: 'w-50',
                        },
                        {
                            key: 'smoke',
                            color: 'secondary',
                            value: this.statisticAll.countSmoke || 0,
                            label: 'Events.MapDashboard.Smoke',
                            itemClass: 'w-50',
                        },
                    ],
                },
            ]
        },
    },
}
</script>

<style lang="scss" scoped>
.statistics-sidebar {
    border: 3px solid #4a90e2 !important;
    border-radius: 8px;
    display: flex;
    flex-direction: column;
    overflow: hidden;
}

.statistics-header {
    background: linear-gradient(120deg, #1e3c72 0%, #2a5298 100%);
    padding: 12px 10px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
    border-bottom: none;
}

.statistics-header-content {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    gap: 8px;
    position: relative;
}

.statistics-close-btn {
    position: absolute;
    top: 50%;
    transform: translateY(-50%);
    right: -7px;
    width: 24px;
    height: 24px;
    background: transparent;
    border: 1px solid transparent;
    color: #fff;
    cursor: pointer;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
    transition: all 0.2s ease;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
    z-index: 10;

    svg {
        width: 16px;
        height: 16px;
    }

    &:hover {
        background: rgba(255, 255, 255, 0.25);
        border-color: rgba(255, 255, 255, 0.5);
        transform: translateY(calc(-50% - 1px));
        box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
    }

    &:active {
        transform: translateY(-50%);
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.15);
    }
}

.statistics-icon-wrapper {
    position: relative;
    width: 32px;
    height: 32px;
    background: rgba(255, 255, 255, 0.15);
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.statistics-icon-wrapper::before {
    content: '';
    position: absolute;
    inset: 0;
    border-radius: 8px;
    padding: 1px;
    background: linear-gradient(135deg, rgba(255, 255, 255, 0.3), rgba(255, 255, 255, 0.05));
    -webkit-mask: linear-gradient(#fff 0 0) content-box, linear-gradient(#fff 0 0);
    -webkit-mask-composite: xor;
    mask: linear-gradient(#fff 0 0) content-box, linear-gradient(#fff 0 0);
    mask-composite: exclude;
}

.statistics-icon {
    color: #fff;
    filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.2));
    z-index: 1;
}

.statistics-header-text {
    flex: 1;
}

.statistics-title {
    color: #fff;
    font-weight: 600;
    font-size: 13px;
    line-height: 1.2;
    margin-bottom: 2px;
    text-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
}

.statistics-subtitle {
    color: rgba(255, 255, 255, 0.85);
    font-size: 10px;
    font-weight: 400;
    line-height: 1.2;
    text-shadow: 0 1px 2px rgba(0, 0, 0, 0.15);
}

.statistics-body {
    flex: 1;
    overflow-y: auto;
    overflow-x: hidden;
    background: #fff;

    &::-webkit-scrollbar {
        width: 4px;
    }

    &::-webkit-scrollbar-track {
        background: #f1f1f1;
    }

    &::-webkit-scrollbar-thumb {
        background: #888;
        border-radius: 4px;

        &:hover {
            background: #555;
        }
    }
}

.statistics-container {
    display: flex;
    flex-direction: column;
    padding: 0 8px;
}

@media (min-width: 1500px) {
    .statistics-title {
        font-size: 15px;
    }

    .statistics-subtitle {
        font-size: 12px;
    }
}
</style>
