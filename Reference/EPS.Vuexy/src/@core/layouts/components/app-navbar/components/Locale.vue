<template>
    <b-nav-item-dropdown
        id="dropdown-grouped"
        variant="link"
        class="dropdown-language"
        right
    >
        <template #button-content>
            <b-img
                :src="currentLocale.img"
                height="14px"
                width="22px"
                :alt="currentLocale.locale"
            />
            <span class="ml-50 text-body">{{ currentLocale.name }}</span>
        </template>
        <b-dropdown-item
            v-for="localeObj in locales"
            :key="localeObj.locale"
            @click="changeLocale(localeObj)"
        >
            <b-img
                :src="localeObj.img"
                height="14px"
                width="22px"
                :alt="localeObj.locale"
            />
            <span class="ml-50">{{ localeObj.name }}</span>
        </b-dropdown-item>
    </b-nav-item-dropdown>
</template>

<script>
/* eslint-disable */
import { useRouter } from '@/@core/utils/utils'
import { loadRouteLocaleMessage } from '@/libs/i18n'
import { BNavItemDropdown, BDropdownItem, BImg } from 'bootstrap-vue'
import { localize } from 'vee-validate'
export default {
    components: {
        BNavItemDropdown,
        BDropdownItem,
        BImg,
    },
    // ! Need to move this computed property to comp function once we get to Vue 3
    computed: {
        currentLocale() {
            return this.locales.find((l) => l.locale === this.$i18n.locale)
        },
    },
    setup() {
        /* eslint-disable global-require */
        const locales = [
            {
                locale: 'vi',
                img: require('@/assets/images/flags/vi.png'),
                name: 'Tiếng Việt',
            },
            {
                locale: 'en',
                img: require('@/assets/images/flags/en.png'),
                name: 'English',
            },
            // {
            //   locale: 'fr',
            //   img: require('@/assets/images/flags/fr.png'),
            //   name: 'French',
            // },
            // {
            //   locale: 'de',
            //   img: require('@/assets/images/flags/de.png'),
            //   name: 'German',
            // },
            // {
            //   locale: 'pt',
            //   img: require('@/assets/images/flags/pt.png'),
            //   name: 'Portuguese',
            // },
        ]
        /* eslint-disable global-require */
        const { router } = useRouter()

        return {
            locales,
            router,
        }
    },
    methods: {
        changeLocale(localeObj) {
            this.$i18n.locale = localeObj.locale
            const currentPath = this.router.currentRoute.path
            loadRouteLocaleMessage(localeObj.locale, currentPath)
            localize(this.$i18n.locale)
        },
    },
}
</script>

<style></style>
