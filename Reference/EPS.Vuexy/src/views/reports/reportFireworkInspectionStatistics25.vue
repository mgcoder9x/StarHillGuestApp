<template>
    <b-container fluid class="px-0">
        <b-card body-class="px-1 py-1">
            <validation-observer ref="rules">
                <b-form>
                    <b-row>
                        <b-col>
                            <b-form-group
                                label-for="cascade_id"
                                :label="$t('reports.labels.cascade')"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="cascade"
                                >
                                    <b-form-select
                                        id="cascade_id"
                                        v-model="
                                            reportViewer.parameters.cascade_id
                                        "
                                        :options="treeAreas"
                                        label="text"
                                        track-by="id"
                                        :reduce="(item) => parseInt(item.value)"
                                        required
                                        :placeholder="
                                            $t(
                                                'categories.areas.common.form.placeholder.parentArea'
                                            )
                                        "
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col>
                            <b-form-group
                                :label="$t('reports.labels.start_date')"
                                label-for="start_date"
                                label-cols-md="3"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="start_date"
                                >
                                    <b-form-datepicker
                                        id="start_date"
                                        v-model="
                                            reportViewer.parameters.start_date
                                        "
                                        :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }"
                                        required
                                        type="datetime"
                                        :locale="locale"
                                        @change="onParameterChange"
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col>
                            <b-form-group
                                :label="$t('reports.labels.end_date')"
                                label-for="end_date"
                                label-cols-md="3"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="end_date"
                                >
                                    <b-form-datepicker
                                        id="end_date"
                                        v-model="
                                            reportViewer.parameters.end_date
                                        "
                                        :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }"
                                        type="date"
                                        :locale="locale"
                                        required
                                        @change="onParameterChange"
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
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
            </validation-observer>
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
            treeAreas: null,
            reportViewer: {
                show: false,
                reportSource: 'Firework_Inspection_Statistics_25.trdp',
                parameters: {
                    cascade_name: 'Test',
                    cascade_id: 65,
                    comp_id: 35,
                    start_date: this.$moment()
                        .startOf('month')
                        .format('YYYY-MM-DD'),
                    end_date: this.$moment().format('YYYY-MM-DD'),
                },
            },
        }
    },
    computed: {
        locale() {
            return this.$i18n.locale
        },
    },
    created() {
        this.getTreeAreas()
    },
    methods: {
        async getTreeAreas() {
            const res = await this.$services.get('/areas/cascade')
            this.treeAreas = res.data.data
        },
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
<style scoped></style>
