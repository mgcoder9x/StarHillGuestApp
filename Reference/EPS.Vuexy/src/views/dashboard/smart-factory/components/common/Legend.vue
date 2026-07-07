<template>
    <div class="d-flex align-items-center">
        <!-- <div class="header d-flex align-items-center">
            <Icon
                icon="lucide:activity"
                width="24"
                height="24"
                class="mr-1 text-primary"
            />
            <h2 class="text-dark fs-5" style="margin: 0; font-weight: bold">
                Giám sát dây chuyền Realtime
            </h2>
        </div> -->
        <div class="d-flex flex-wrap align-items-center ml-auto">
            <div
                v-for="item in legendItems"
                :key="item.status"
                class="legend-item"
            >
                <div :class="['legend-color', item.className]"></div>
                <span>{{ item.label }}</span>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    name: 'Legend',
    props: {
        items: {
            type: Array,
            default: null,
        },
    },
    computed: {
        legendItems() {
            if (this.items && this.items.length) return this.items
            return [
                {
                    status: 'completed',
                    className: 'step-completed',
                    label: this.$t('Meiko.Dashboard.Completed'),
                },
                {
                    status: 'violation',
                    className: 'step-violation',
                    label: this.$t('Meiko.Dashboard.Violation'),
                },
                {
                    status: 'inProgress',
                    className: 'step-inProgress',
                    label: this.$t('Meiko.Dashboard.InProgress'),
                },
                {
                    status: 'pending',
                    className: 'step-pending',
                    label: this.$t('Meiko.Dashboard.Pending'),
                },
            ]
        },
    },
}
</script>

<style scoped>
.legend-item {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    margin-right: 20px;
    font-size: 0.875rem;
}

.legend-color {
    width: 20px;
    height: 20px;
    border-radius: 4px;
}
</style>
