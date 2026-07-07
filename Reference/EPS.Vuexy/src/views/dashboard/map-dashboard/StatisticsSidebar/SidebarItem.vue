<template>
    <div class="sidebar-item flex-fill" :class="itemClass">
        <div class="sidebar-item-indicator" :class="indicatorClass"></div>
        <div class="sidebar-item-content">
            <div class="sidebar-item-value">{{ value }}</div>
            <div class="sidebar-item-label">{{ $t(label) }}</div>
        </div>
    </div>
</template>

<script>
export default {
    name: 'SidebarItem',
    props: {
        color: {
            type: String,
            required: true,
        },
        value: {
            type: [Number, String],
            required: true,
        },
        label: {
            type: String,
            required: true,
        },
        itemClass: {
            type: String,
            default: '',
        },
    },
    computed: {
        indicatorClass() {
            return `bg-${this.color}`
        },
    },
}
</script>

<style lang="scss" scoped>
.sidebar-item {
    display: flex;
    align-items: center;
    padding: 4px 8px 6px;
    transition: background-color 0.2s ease;
}

.sidebar-item-indicator {
    width: 3px;
    height: 24px;
    border-radius: 1.5px;
    margin-right: 8px;
    flex-shrink: 0;
}

.sidebar-item-content {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    align-items: center;
}

.sidebar-item-value {
    font-size: 18px;
    font-weight: 900;
    color: #1a202c;
    line-height: 1.2;
    margin-bottom: 2px;
    text-shadow: 0 1px 1px rgba(0, 0, 0, 0.05);
    letter-spacing: -0.5px;
    transition: all 0.3s ease;
}

.sidebar-item-label {
    font-size: 9px;
    color: #718096;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0;
    line-height: 1;
}

/* Responsive adjustments */
@media (min-width: 1500px) {
    .sidebar-item-value {
        font-size: 20px;
    }

    .sidebar-item-label {
        font-size: 10px;
    }
}
</style>
