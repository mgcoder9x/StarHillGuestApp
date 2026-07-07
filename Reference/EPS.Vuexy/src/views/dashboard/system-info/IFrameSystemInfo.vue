<!-- eslint-disable vue/no-v-html -->
<template>
    <div>
        <iframe
            ref="iframe"
            :src="iframeUrl"
            frameborder="0"
            class="iframe"
        ></iframe>
    </div>
</template>
<script>
export default {
    props: {
        iframeUrl: {
            type: String,
            required: true,
        },
    },
    setup() {},
}
</script>
<style>
iframe {
    height: 100vh;
    width: 100%;
    overflow: hidden;
}
</style>
