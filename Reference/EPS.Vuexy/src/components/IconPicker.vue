<template>
    <div class="icon-picker">
        <!-- <HelloWorld msg="Hello Vue in CodeSandbox!" /> -->
        <section v-if="selectedIcon !== null">
            <form style="display: flex; align-items: center; gap: 10px">
                <Icon
                    :icon="selectedIcon"
                    :width="48"
                    :height="48"
                    :color="color"
                />
                <div
                    v-if="!iconInfo"
                    style="display: flex; align-items: center; gap: 10px"
                >
                    <b-dropdown
                        position="is-bottom-left"
                        append-to-body
                        aria-role="menu"
                        trap-focus
                        variant="outline-secondary"
                        v-bind="fieldProps"
                    >
                        <template #button-content>
                            <span
                                class="color-preview"
                                :style="{ backgroundColor: color }"
                            ></span>
                            <span>{{ color }}</span>
                        </template>
                        <sketch-picker
                            :value="color"
                            @input="(color) => updateColor(color.hex)"
                        />
                    </b-dropdown>
                    <b-button
                        variant="outline-secondary"
                        v-bind="fieldProps"
                        @click="clear"
                        >Clear</b-button
                    >
                </div>
            </form>
        </section>
        <section v-else class="form_search">
            <form @submit.prevent="searchIcons">
                <b-input v-model="search" v-bind="fieldProps"></b-input>
                <button class="button" type="submit">Rechercher</button>
                <span> {{ icons.length }} icones</span>
            </form>
            <div
                v-if="icons.length > 0"
                style="
                    display: grid;
                    grid-template-columns: repeat(10, 1fr);
                    border: 1px dashed green;
                    border-radius: 10px;
                    padding: 5px;
                    overflow: hidden;
                    overflow-y: auto;
                    max-height: 400px;
                    grid-gap: 5px;
                "
            >
                <div
                    v-for="icon in icons"
                    :key="icon"
                    class="icon"
                    @click="() => selectIcon(icon)"
                >
                    <b-tooltip :target="icon" triggers="hover">
                        {{ icon }}
                    </b-tooltip>
                    <Icon
                        :id="icon"
                        :icon="icon"
                        :width="48"
                        :height="48"
                        style="max-width: 100%; height: 100%"
                    />
                </div>
            </div>
        </section>
    </div>
</template>

<script>
/* eslint-disable */
export default {
    name: 'App',
    props: {
        value: {
            type: Object,
            default: () => ({ iconName: null, color: '#FFFFFF' }),
        },
        fieldProps: {
            type: Object,
            default: () => ({}),
        },
    },
    data() {
        return {
            search: '',
            icons: [],
            iconInfo: false,
            selectedIcon: this.value.iconName || null,
            color: this.value.color || 'rgb(208, 210, 214)',
            modalActive: false,
            iconifyApi: process.env.VUE_APP_ICONIFY_API,
        }
    },
    watch: {
        value: {
            handler(newValue) {
                this.selectedIcon = newValue.iconName
                this.color = newValue.color
            },
            deep: true,
        },
        async selectedIcon() {
            if (this.selectedIcon === null) {
                this.iconInfo = null
                this.search = null
                return
            }
            const [prefix] = this.selectedIcon.split(':')
            const response = await fetch(
                `${this.iconifyApi}/collections?prefix=${prefix}`
            )
            const {
                [prefix]: { palette },
            } = await response.json()
            this.iconInfo = palette
            this.emitChange()
        },

        color(newValue) {
            this.emitChange()
        },
    },

    methods: {
        async searchIcons() {
            const response = await fetch(
                `${this.iconifyApi}/search?query=${this.search}&limit=999`
            )
            const { icons } = await response.json()
            this.icons = icons
        },
        selectIcon(icon) {
            this.selectedIcon = icon
        },
        updateColor(color) {
            this.color = color
        },
        clear() {
            this.selectedIcon = null
            this.color = '#FFFFFF'
            this.icons = []
        },
        emitChange() {
            this.$emit('input', {
                iconName: this.selectedIcon,
                color: this.color,
            })
        },
    },
}
</script>

<style>
.icon {
    cursor: pointer;
    border: 1px solid grey;
    border-radius: 10px;
    height: 48px;
    max-width: 48px;
    width: 100%;
    padding: 10px;
}

.icon:hover {
    background: linear-gradient(
        270deg,
        rgba(168, 170, 174, 0.7),
        rgb(168, 170, 174)
    ) !important;
    color: #fff;
}
.color-preview {
    display: inline-block;
    width: 15px;
    height: 15px;
    margin-right: 10px;
    border: 1px solid #ccc;
    border-radius: 3px;
}
</style>
