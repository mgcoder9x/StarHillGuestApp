<template>
    <b-card no-body>
        <b-card-header>
            <b-card-title>
                {{ $t('Events.OverView.VirtualFenceStatistics') }}
            </b-card-title>
        </b-card-header>
        <b-card-body>
            <div v-for="(item, index) in data" :key="index" class="mb-2">
                <b-badge :variant="colors[index]" pill style="width: 4rem">{{
                    item.eventCount
                    }}</b-badge>
                <span class="ml-1">{{ formatWarningLevel(item.label) }}</span>
            </div>
        </b-card-body>
    </b-card>
</template>

<script>
import {
    ProtectiveEquipment,
    CarWarningType,
} from '@/views/events/protectiveEquipmentEvent/warningLevelData'
import protectiveEquipmentTranslations from '@/libs/i18n/locales/vi/event-protectiveEquipmentEvent.json'
import protectiveEquipmentTranslationsEn from '@/libs/i18n/locales/en/event-protectiveEquipmentEvent.json'

export default {
    props: {
        data: {
            type: Array,
            default: () => [],
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
                'light',
            ],
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    watch: {
        '$i18n.locale': function () {
            this.mergeWarningTranslations()
        },
    },
    created() {
        this.mergeWarningTranslations()
    },
    methods: {
        formatWarningLevel(value) {
            // Handle null or undefined
            if (value === null || value === undefined) {
                return this.$t('ProtectiveEquipmentType.Unknown')
            }

            const numValue = parseInt(value, 10)

            // Handle specific conveyor belt safety codes
            if (!Number.isNaN(numValue)) {
                if (
                    numValue === 2092 ||
                    numValue === 2121 ||
                    numValue === 2123
                ) {
                    return this.$t(`ProtectiveEquipmentType.Code${numValue}`)
                }
                // Handle codes 2051-2058
                if (numValue >= 2051 && numValue <= 2058) {
                    return this.$t(`ProtectiveEquipmentType.Code${numValue}`)
                }
            }

            // Handle existing ProtectiveEquipment or CarWarningType mappings
            if (
                !Number.isNaN(numValue) &&
                (this.ProtectiveEquipment[numValue] ||
                    this.CarWarningType[numValue])
            ) {
                const key =
                    this.ProtectiveEquipment[numValue] ||
                    this.CarWarningType[numValue]
                return this.$t(key)
            }

            // Handle string keys that start with ProtectiveEquipmentType.
            if (
                typeof value === 'string' &&
                value.startsWith('ProtectiveEquipmentType.')
            ) {
                return this.$t(value)
            }

            // Default to Unknown
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
                            this.protectiveEquipmentTranslations
                                .ProtectiveEquipmentType,
                    })
                } else {
                    this.$i18n.mergeLocaleMessage('en', {
                        ProtectiveEquipmentType:
                            this.protectiveEquipmentTranslationsEn
                                .ProtectiveEquipmentType,
                    })
                }
            }
        },
    },
}
</script>
