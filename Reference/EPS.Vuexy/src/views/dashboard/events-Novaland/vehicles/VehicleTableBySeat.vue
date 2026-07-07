<template>
    <b-card no-body>
        <b-card-header>
            <b-card-title>
                {{ $t('Events.VehicleMonitor.VehicleAccessComplex') }}
            </b-card-title>
        </b-card-header>
        <b-card-body>
            <b-table
                :items="data"
                :fields="[
                    {
                        key: 'type',
                        label: this.$t('Events.VehicleMonitor.Vehicle'),
                    },
                    { key: 'in', label: this.$t('Events.VehicleMonitor.In') },
                    { key: 'out', label: this.$t('Events.VehicleMonitor.Out') },
                    {
                        key: 'inSide',
                        label: this.$t('Events.VehicleMonitor.InFactory'),
                    },
                ]"
                bordered
                small
            >
                <template #cell(type)="row">
                    {{ formatVehicleLabel(row.item.type, $i18n.locale) }}
                </template>
            </b-table>
        </b-card-body>
    </b-card>
</template>

<script>
export default {
    props: {
        data: {
            type: Array,
            required: true,
        },
    },
    methods:{
        formatVehicleLabel(totalSeats, locale) {
            if (locale === 'vi') {
                return `Xe ${totalSeats} chỗ`
            } else if (locale === 'en') {
                return `${totalSeats} seat vehicle`
            }
            
        },
    }
}
</script>
