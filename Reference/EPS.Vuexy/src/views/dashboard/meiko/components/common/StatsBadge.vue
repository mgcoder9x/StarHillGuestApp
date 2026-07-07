<template>
    <span class="stat-badge bg-light" :class="`text-${variant}`">
        <!-- <b-icon :icon="icon"></b-icon> -->
        <Icon :icon="icon" width="15" height="15" />
        {{ count }}
    </span>
</template>

<script>
export default {
    name: 'StatsBadge',
    props: {
        icon: {
            type: String,
            required: true,
        },
        count: {
            type: Number,
            required: true,
        },
        variant: {
            type: String,
            default: 'primary',
        },
    },
}
</script>

<style scoped>
.stat-badge {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    padding: 4px 10px;
    border-radius: 20px;
    font-size: 12px;
    font-weight: 700;
    border: 1px solid transparent;
    white-space: nowrap;
}
</style>
