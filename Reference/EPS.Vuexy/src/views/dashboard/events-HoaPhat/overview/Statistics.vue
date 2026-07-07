<template>
    <b-card v-if="data && data.length > 0" no-body style="margin-bottom: -3rem">
        <b-card-body>
            <b-row class="d-flex justify-content-center">
                <b-col
                    v-for="item in data.filter(
                        (item) => item.dataStats.length > 0
                    )"
                    :key="item.icon"
                >
                    <b-card
                        class="text-white shadow-sm border-primary"
                        style="border-width: 2px; height: 85%"
                    >
                        <h4 class="card-title text-center">
                            {{ $t(item.title) }}
                        </h4>
                        <div class="d-flex justify-content-center">
                            <b-avatar
                                v-for="(stat, index) in item.dataStats"
                                :key="index"
                                size="48"
                                class="mx-1"
                                :variant="colors[index]"
                            >
                                {{ stat.value }}
                            </b-avatar>
                        </div>
                        <div class="mt-2">
                            <div
                                v-for="(stat, index) in item.dataStats"
                                :key="index"
                                class="d-flex align-items-center"
                                style="margin-bottom: 5px"
                            >
                                <div
                                    class="rounded-circle mr-1"
                                    :class="`bg-${colors[index]}`"
                                    style="width: 25px; height: 25px"
                                >
                                    <i class="fa fa-circle"></i>
                                </div>
                                <small class="text-dark"
                                    >{{ $t(stat.label) }}
                                </small>
                            </div>
                        </div>
                    </b-card>
                </b-col>
            </b-row>
        </b-card-body>
    </b-card>
</template>

<script>
import { BCard, BCardBody, BRow, BCol, BAvatar } from 'bootstrap-vue'

export default {
    components: {
        BRow,
        BCol,
        BCard,
        BCardBody,
        BAvatar,
    },
    props: {
        data: {
            type: Array,
            default: () => [],
        },
    },
    data() {
        return {
            colors: [
                'primary',
                'success',
                'warning',
                'info',
                'danger',
                'secondary',
            ],
        }
    },
}
</script>
