<template>
    <b-container fluid class="px-0">
        <b-card body-class="px-1 py-1">
            <b-form>
                <b-row>
                    <b-col>
                        <b-form-group
                            label="Parameter1"
                            label-for="Parameter1"
                            label-cols-md="3"
                        >
                            <b-form-datepicker
                                v-model="reportViewer.parameters.Parameter1"
                                @change="onParameterChange"
                            />
                        </b-form-group>
                    </b-col>
                    <b-col>
                        <b-form-group
                            label="Parameter2"
                            label-for="Parameter2"
                            label-cols-md="3"
                        >
                            <b-form-datepicker
                                v-model="reportViewer.parameters.Parameter2"
                                @change="onParameterChange"
                            />
                        </b-form-group>
                    </b-col>
                    <b-col cols="12" class="text-center">
                        <b-button
                            variant="primary"
                            class=""
                            @click="executeReport"
                        >
                            Tạo báo cáo
                        </b-button>
                    </b-col>
                </b-row>
            </b-form>
        </b-card>
        <telerik-report-viewer
            ref="reportViewer"
            :report-source="reportViewer.reportSource"
            :parameters="reportViewer.parameters"
        />
    </b-container>
</template>

<script>
export default {
    name: 'TestReport',
    data() {
        return {
            reportViewer: {
                show: false,
                reportSource: 'Test.trdp',
                parameters: {
                    Parameter1: '2024-01-01 00:00:00',
                    Parameter2: '2024-02-01 00:00:00',
                },
            },
        }
    },
    methods: {
        executeReport() {
           this.$refs.reportViewer.refresh()
        },
        async onParameterChange() {
            await this.$refs.rules.validate()
            // Ensure the report is updated when the parameters change
            this.$refs.reportViewer.hideReport()
        },
    },
}
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
.reportViewer {
    position: relative;
    width: 100%;
    height: 70vh;
}
</style>
