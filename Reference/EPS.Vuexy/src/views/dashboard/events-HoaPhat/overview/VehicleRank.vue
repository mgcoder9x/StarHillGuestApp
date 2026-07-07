<template>
    <b-card no-body class="card-browser-states">
        <b-card-header>
            <b-card-title>
                {{ $t('Events.OverView.TopAreaMostVehicle') }}
            </b-card-title>
        </b-card-header>

        <!-- body -->
        <b-card-body>
            <div class="browser-states mb-2">
                <b-media no-body>
                    <b-media-body>
                        <h6
                            class="align-self-center my-auto font-weight-bold text-shadow"
                        >
                            {{ $t('Events.OverView.AREA') }}
                        </h6>
                    </b-media-body>
                </b-media>
                <div class="d-flex align-items-center">
                    <span
                        class="font-weight-bold text-primary font-monospace text-right d-inline-block"
                        style="width: 60px"
                    >
                        {{ $t('Events.OverView.IN') }}
                    </span>
                    <span class="mx-1"> | </span>
                    <span
                        class="font-weight-bold text-warning font-monospace text-left d-inline-block"
                        style="width: 60px"
                    >
                        {{ $t('Events.OverView.OUT') }}
                    </span>
                </div>
            </div>

            <div
                v-for="(browser, index) in data"
                :key="index"
                class="browser-states"
            >
                <b-media no-body>
                    <b-media-body>
                        <h6 class="align-self-center my-auto">
                            {{ browser.name }}
                        </h6>
                    </b-media-body>
                </b-media>
                <div class="d-flex align-items-center">
                    <span
                        class="font-weight-bold text-primary font-monospace text-right d-inline-block"
                        style="width: 60px"
                    >
                        {{ browser.in }}
                    </span>
                    <span class="mx-1"> | </span>
                    <span
                        class="font-weight-bold text-warning font-monospace text-left d-inline-block"
                        style="width: 60px"
                    >
                        {{ browser.out }}
                    </span>
                </div>
            </div>
        </b-card-body>
        <!--/ body -->
    </b-card>
</template>

<script>
import {
    BCard,
    BCardHeader,
    BCardTitle,
    BCardBody,
    BMedia,
    BMediaBody,
} from 'bootstrap-vue'

export default {
    components: {
        BCard,
        BCardHeader,
        BCardTitle,
        BCardBody,
        BMedia,
        BMediaBody,
    },
    props: {
        data: {
            type: Array,
            default: () => [
                { name: 'Mỹ Đình', in: 521, out: 7642 },
                { name: 'Long Thành', in: 9312, out: 106 },
                { name: 'Bình An', in: 65, out: 8819 },
                { name: 'Phú Nhuận', in: 720, out: 3001 },
                { name: 'Tân Phú', in: 1299, out: 64 },
            ],
        },
    },
}
</script>
