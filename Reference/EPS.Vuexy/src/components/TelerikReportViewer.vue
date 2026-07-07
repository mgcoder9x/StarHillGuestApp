<template>
    <b-card v-if="show" body-class="px-1 py-0 pt-1">
        <div id="reportViewer">
            <div class="d-flex justify-content-center align-content-center">
                <b-spinner class="mr-1" variant="primary" />
                <span>Loading...</span>
            </div>
        </div>
    </b-card>
</template>
<script>
export default {
    name: 'TelerikReportViewer',
    props: {
        serviceUrl: {
            type: String,
            required: false,
            default: () => `${process.env.VUE_APP_BASE_URL}/api/reports`,
        },
        reportSource: {
            type: String,
            required: true,
            default: 'Test.trdp',
        },
        parameters: {
            type: Object,
            required: false,
            // default: () => {
            //     return {
            //         Parameter1: '2024-01-01 00:00:00',
            //         Parameter2: '2024-02-01 00:00:00',
            //     }
            // },
        },
        scaleMode: {
            type: String,
            required: false,
            default: 'FIT_PAGE_WIDTH',
        },
        viewMode: {
            type: String,
            required: false,
            default: 'INTERACTIVE',
        },
        scale: {
            type: Number,
            required: false,
            default: 1.0,
        },
    },
    data() {
        return {
            show: false,
            reportViewer: {
                serviceUrl: '',
                reportSource: {
                    report: '',
                    parameters: null,
                },
                scaleMode: '',
                viewMode: '',
                scale: 0,
            },
        }
    },
    created() {
        this.reportViewer.serviceUrl = this.serviceUrl
        this.reportViewer.reportSource.report = this.reportSource
        this.reportViewer.reportSource.parameters = this.parameters
        this.reportViewer.scaleMode = this.scaleMode
        this.reportViewer.viewMode = this.viewMode
        this.reportViewer.scale = this.scale
    },
    methods: {
        refresh() {
            this.show = false
            setTimeout(() => {
                this.show = true
                this.$nextTick(() => {
                    // eslint-disable-next-line no-undef
                    $('#reportViewer').telerik_ReportViewer(this.reportViewer)
                })
            }, 100)
        },
        hideReport() {
            this.show = false
        },
    },
}
</script>
<style lang="scss" scoped>
#reportViewer {
    position: relative;
    width: 100%;
    height: 70vh;
}
</style>
