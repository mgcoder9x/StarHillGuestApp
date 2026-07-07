<template>
    <div class="sidebar-group">
        <div class="sidebar-group-header">
            <div class="group-icon-wrapper" :class="`icon-${color}`">
                <!-- Iconify icon (contains ':') -->
                <Icon v-if="isIconify" :icon="icon" width="14" height="14" class="group-icon" />
                <!-- Feather icon -->
                <feather-icon v-else :icon="icon" size="14" class="group-icon" />
            </div>
            <span>{{ $t(title) }}</span>
        </div>
        <div class="sidebar-group-row">
            <slot></slot>
        </div>
    </div>
</template>

<script>
import { Icon } from '@iconify/vue2'

export default {
    name: 'SidebarGroup',
    components: {
        Icon,
    },
    props: {
        icon: {
            type: String,
            required: true,
        },
        title: {
            type: String,
            required: true,
        },
        color: {
            type: String,
            default: 'primary',
        },
    },
    computed: {
        isIconify() {
            return this.icon.includes(':')
        },
    },
}
</script>

<style lang="scss" scoped>
.sidebar-group {
    width: 100%;
    margin: 10px 0 15px;
    border: 1px solid #e2e8f0;
    border-radius: 6px;
    overflow: hidden;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.08);
    transition: box-shadow 0.2s ease;

    &:hover {
        box-shadow: 0 4px 8px rgba(0, 0, 0, 0.12);
    }
}

.sidebar-group-header {
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
    gap: 8px;
}

.group-icon-wrapper {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 24px;
    height: 24px;
    background: rgba(255, 255, 255, 0.25);
    border-radius: 6px;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15), inset 0 1px 0 rgba(255, 255, 255, 0.2);
    transition: all 0.2s ease;
    flex-shrink: 0;
}

.sidebar-group:hover .group-icon-wrapper {
    background: rgba(255, 255, 255, 0.35);
    transform: scale(1.05);
    box-shadow: 0 3px 8px rgba(0, 0, 0, 0.2), inset 0 1px 0 rgba(255, 255, 255, 0.3);
}

.group-icon {
    color: #fff;
    filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.2));
}

/* Color variants for icon wrapper */
.group-icon-wrapper.icon-primary {
    background: linear-gradient(135deg, #667eea 0%, #5a67d8 100%);
}

.group-icon-wrapper.icon-success {
    background: linear-gradient(135deg, #48bb78 0%, #38a169 100%);
}

.group-icon-wrapper.icon-warning {
    background: linear-gradient(135deg, #ed8936 0%, #dd6b20 100%);
}

.group-icon-wrapper.icon-danger {
    background: linear-gradient(135deg, #fc8181 0%, #e53e3e 100%);
}

.group-icon-wrapper.icon-info {
    background: linear-gradient(135deg, #63b3ed 0%, #4299e1 100%);
}

.sidebar-group-row {
    display: flex;
    padding: 5px;
}
</style>
