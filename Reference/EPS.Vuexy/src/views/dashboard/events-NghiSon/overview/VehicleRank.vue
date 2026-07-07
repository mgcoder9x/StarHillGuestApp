<template>
    <b-card no-body class="card-browser-states">
        <b-card-header>
            <b-card-title>
                {{ $t(header.header) }}
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
                            {{ $t(header.header1) }}
                        </h6>
                    </b-media-body>
                </b-media>
                <div class="d-flex align-items-center">
                    <span
                        class="font-weight-bold text-primary font-monospace text-center d-inline-block"
                        style="width: 80px"
                    >
                        {{ $t(header.header2) }}
                    </span>
                    <span class="mx-1"> | </span>
                    <span
                        class="font-weight-bold text-success font-monospace text-center d-inline-block"
                        style="width: 80px"
                    >
                        {{ $t(header.header3) }}
                    </span>
                    <span class="mx-1"> | </span>
                    <span
                        class="font-weight-bold text-danger font-monospace text-center d-inline-block"
                        style="width: 80px"
                    >
                        {{ $t(header.header4) }}
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
                            {{ $t(browser.name) }}
                        </h6>
                    </b-media-body>
                </b-media>
                <div class="d-flex align-items-center">
                    <span
                        class="font-weight-bold text-primary font-monospace text-center d-inline-block"
                        style="width: 80px"
                    >
                        {{ browser.data1 }}
                    </span>
                    <span class="mx-1"> | </span>
                    <span
                        class="font-weight-bold text-success font-monospace text-center d-inline-block"
                        style="width: 80px"
                    >
                        {{ browser.data2 }}
                    </span>
                    <span class="mx-1"> | </span>
                    <span
                        class="font-weight-bold text-danger font-monospace text-center d-inline-block"
                        style="width: 80px"
                    >
                        {{ browser.data3 }}
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
        header: {
            type: Object,
        },
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
