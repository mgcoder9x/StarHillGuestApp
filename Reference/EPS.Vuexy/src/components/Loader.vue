<template>
    <div v-if="isShow" class="loader-overlay">
        <div class="loader"></div>
    </div>
</template>

<script>
export default {
    name: 'Loader',
    props: {
        isShow: {
            type: Boolean,
            default: false,
        },
    },
}
</script>

<style scoped>
.loader-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.3); /* lớp phủ mờ */
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 9999; /* đè lên mọi thứ */
}

.loader {
    width: 50px;
    aspect-ratio: 1;
    border-radius: 50%;
    border: 8px solid;
    border-color: #0054a6 #0000;
    animation: spin 1s infinite;
}

@keyframes spin {
    to {
        transform: rotate(0.5turn);
    }
}
</style>
