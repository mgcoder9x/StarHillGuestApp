<template>
    <b-card no-body>
        <b-card-header>
            <b-card-title>
                {{ $t('Events.VehicleMonitor.VehicleAccessGate') }}
            </b-card-title>
        </b-card-header>
        <b-card-body>
            <b-table
                :items="data"
                :fields="[
                    {
                        key: 'area',
                        label: this.$t('Events.VehicleMonitor.Area'),
                    },
                    { key: 'in', label: this.$t('Events.VehicleMonitor.In') },
                    { key: 'out', label: this.$t('Events.VehicleMonitor.Out') },
                ]"
                bordered
                small
            />
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
}
</script>
