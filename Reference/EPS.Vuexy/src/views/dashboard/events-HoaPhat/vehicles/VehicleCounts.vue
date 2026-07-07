<template>
    <div>
        <template>
            <b-row>
                <b-col cols="12" md="4">
                    <b-card class="text-center h-100">
                        <b-avatar
                            class="mb-1"
                            variant="light-warning"
                            size="45"
                        >
                            <feather-icon
                                size="21"
                                icon="ArrowDownCircleIcon"
                            />
                        </b-avatar>
                        <div class="truncate">
                            <h2 class="mb-25 font-weight-bolder">
                                {{ data.inSide }}
                            </h2>
                            <span>{{
                                $t('Events.VehicleMonitor.TotalVehicelFactory')
                            }}</span>
                        </div>
                    </b-card>
                </b-col>

                <b-col cols="12" md="4">
                    <b-card class="text-center h-100">
                        <b-avatar
                            class="mb-1"
                            variant="light-success"
                            size="45"
                        >
                            <feather-icon size="21" icon="LogInIcon" />
                        </b-avatar>
                        <div class="truncate">
                            <h2 class="mb-25 font-weight-bolder">
                                {{ data.totalIn }}
                            </h2>
                            <span>{{
                                $t('Events.VehicleMonitor.TotalVehicelIn')
                            }}</span>
                        </div>
                    </b-card>
                </b-col>

                <b-col cols="12" md="4">
                    <b-card class="text-center h-100">
                        <b-avatar
                            class="mb-1"
                            variant="light-primary"
                            size="45"
                        >
                            <feather-icon size="21" icon="LogOutIcon" />
                        </b-avatar>
                        <div class="truncate">
                            <h2 class="mb-25 font-weight-bolder">
                                {{ data.totalOut }}
                            </h2>
                            <span>{{
                                $t('Events.VehicleMonitor.TotalVehicelOut')
                            }}</span>
                        </div>
                    </b-card>
                </b-col>
            </b-row>
        </template>
    </div>
</template>

<script>
export default {
    props: {
        data: {
            type: Object,
            required: true,
        },
    },
}
</script>
