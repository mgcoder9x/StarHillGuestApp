<template>
    <b-card no-body>
        <b-card-header>
            <b-card-title>
                {{ $t('Events.OverView.RateShortageEquip') }}
            </b-card-title>
        </b-card-header>
        <b-card-body>
            <div v-for="(item, index) in data" :key="index" class="mb-2">
                <b-badge :variant="colors[index]" pill style="width: 4rem"
                    >{{ item.percent }}%</b-badge
                >
                <span class="ml-2">{{ formatWarningLevel(item.label) }}</span>
            </div></b-card-body
        >
    </b-card>
</template>

<script>
import { ProtectiveEquipment, CarWarningType } from '@/views/events/protectiveEquipmentEvent/warningLevelData'
import protectiveEquipmentTranslations from '@/libs/i18n/locales/vi/event-protectiveEquipmentEvent.json'
import protectiveEquipmentTranslationsEn from '@/libs/i18n/locales/en/event-protectiveEquipmentEvent.json'

export default {
    props: {
        data: {
            type: Array,
            default: () => [
                {
                    label: 'Events.MapDashboard.MissingHelmet',
                    percent: 65,
                },
                {
                    label: 'Thiếu áo',
                    percent: 40,
                },
                {
                    label: 'Thiếu khẩu trang',
                    percent: 25,
                },
                {
                    label: 'Thiếu mũ & áo',
                    percent: 60,
                },
                {
                    label: 'Thiếu mũ & khẩu trang',
                    percent: 80,
                },
                {
                    label: 'Thiếu áo & khẩu trang',
                    percent: 65,
                },
                {
                    label: 'Thiếu áo & khẩu trang 80',
                    percent: 40,
                },
            ],
        },
    },
    data() {
        return {
            protectiveEquipmentTranslations,
            protectiveEquipmentTranslationsEn,
            ProtectiveEquipment,
            CarWarningType,
            colors: [
                'primary',
                'secondary',
                'info',
                'warning',
                'danger',
                'success',
                'dark',
                'primary',
            ],
        }
    },
    computed:{
        currentLocale() {
            return this.$i18n.locale
        },
    },
    watch:{
        '$i18n.locale'() {
            this.mergeWarningTranslations()
        },
    },
    created() {
        this.mergeWarningTranslations()
    },
    methods:{
        formatWarningLevel(value) {
            if (!value) return this.$t('ProtectiveEquipmentType.Unknown')

            const numValue = parseInt(value, 10)
            if (
                !isNaN(numValue) &&
                (this.ProtectiveEquipment[numValue] || this.CarWarningType[numValue])
            ) {
                const key =
                this.ProtectiveEquipment[numValue] || this.CarWarningType[numValue]
                return this.$t(key)
            }

            if (typeof value === 'string' && value.startsWith('ProtectiveEquipmentType.')) {
                return this.$t(value)
            }
            return this.$t('ProtectiveEquipmentType.Unknown')
        },
        mergeWarningTranslations() {
            if (
                this.protectiveEquipmentTranslations.ProtectiveEquipmentType ||
                this.protectiveEquipmentTranslationsEn.ProtectiveEquipmentType
            ) {
                if (this.currentLocale === 'vi') {
                this.$i18n.mergeLocaleMessage('vi', {
                    ProtectiveEquipmentType:
                    this.protectiveEquipmentTranslations.ProtectiveEquipmentType,
                })
                } else {
                this.$i18n.mergeLocaleMessage('en', {
                    ProtectiveEquipmentType:
                    this.protectiveEquipmentTranslationsEn.ProtectiveEquipmentType,
                })
                }
            }
        },
    },
}
</script>
