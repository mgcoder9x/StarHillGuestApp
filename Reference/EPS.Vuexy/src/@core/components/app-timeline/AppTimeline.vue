<template>
    <ul v-bind="forwardAttrs" class="app-timeline" v-on="forwardListeners">
        <slot />
    </ul>
</template>

<script>
export default {
    inheritAttrs: false,
    computed: {
        forwardAttrs() {
            return { ...this.$attrs }
        },
        forwardListeners() {
            return { ...this.$listeners }
        },
    },
}
</script>

<style lang="scss" scoped>
.app-timeline {
    list-style: none;
    padding: 0;
    margin: 0;
    position: relative;
    margin-left: 1rem;
}
</style>
