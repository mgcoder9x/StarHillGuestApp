<template>
    <validation-observer ref="rules">
        <b-card-body>
            <div class="p-6 shadow-md rounded-lg max-w-3xl mx-auto">
                <!-- <h2 class="text-xl font-semibold mb-4">Create Issue</h2> -->
                <b-row>
                    <b-col md="7">
                        <b-row>
                            <b-col>
                                <b-form-group
                                    :label="this.$t('Nhà thầu')"
                                    label-cols-md="3"
                                    label-class="required"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        rules="required"
                                        :name="$t('Issue.Label.Project')"
                                    >
                                        <v-select
                                            v-model="createData.contractorId"
                                            label="text"
                                            :reduce="(area) => area.id"
                                            :options="listContractor"
                                            :disabled="!editing"
                                            @input="changeContractorId($event)"
                                        >
                                        </v-select>
                                        <small class="text-danger">{{
                                            errors[0]
                                        }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>
                        </b-row>
                        <b-row>
                            <b-col>
                                <b-form-group
                                    :label="this.$t('Nhân sự vi phạm')"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        v-model="createData.personId"
                                        label="text"
                                        :reduce="(emp) => emp.value"
                                        :options="listEmployee"
                                        :disabled="!editing"
                                        @input="changepersonId($event)"
                                    >
                                    </v-select>
                                </b-form-group>
                            </b-col>
                        </b-row>
                        <b-row>
                            <b-col md="6">
                                <b-form-group
                                    :label="this.$t('Giới tính')"
                                    label-cols-md="3"
                                >
                                    <b-form-input
                                        v-model="createData.genderStr"
                                        disabled
                                    />
                                </b-form-group>
                            </b-col>
                            <b-col md="6">
                                <b-form-group
                                    :label="this.$t('Năm sinh')"
                                    label-cols-md="3"
                                >
                                    <b-form-input
                                        v-model="createData.birthdayStr"
                                        disabled
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>
                        <b-row>
                            <b-col md="6">
                                <b-form-group
                                    :label="this.$t('Loại vi phạm')"
                                    label-cols-md="3"
                                >
                                    <b-form-input
                                        v-model="createData.eventTypeName"
                                        disabled
                                    />
                                </b-form-group>
                            </b-col>
                            <b-col md="6">
                                <b-form-group
                                    :label="this.$t('Lỗi vi phạm')"
                                    label-cols-md="3"
                                >
                                    <b-form-input
                                        v-model="createData.warningName"
                                        disabled
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>
                        <b-row>
                            <b-col md="6">
                                <b-form-group
                                    :label="this.$t('Thời gian')"
                                    label-cols-md="3"
                                >
                                    <b-form-input
                                        v-model="createData.accessTimeStr"
                                        disabled
                                    />
                                </b-form-group>
                            </b-col>
                            <b-col md="6">
                                <b-form-group
                                    :label="this.$t('Khu vực vi phạm')"
                                    label-cols-md="3"
                                >
                                    <b-form-input
                                        v-model="createData.areaName"
                                        disabled
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>
                    </b-col>
                    <b-col md="5">
                        <b-row>
                            <img
                                :src="`${baseUrl}${createData.image}`"
                                class="img-fluid cursor-pointer"
                                style="object-fit: cover; border-radius: 0.3em"
                                loading="lazy"
                            />
                        </b-row>
                    </b-col>
                </b-row>
                <b-row class="mt-2">
                    <b-col>
                        <div class="text-center">
                            <Transition mode="out-in">
                                <b-button
                                    v-if="editing"
                                    v-waves
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                                    @click="validateAndSubmitForm"
                                >
                                    <Icon
                                        icon="material-symbols:save-outline"
                                        class="sm-icon"
                                    />
                                    <span class="ml-50">
                                        {{ $t('common.button.save') }}
                                    </span>
                                </b-button>
                                <b-button
                                    v-if="!editing"
                                    v-waves
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                                    @click="startEdit"
                                >
                                    <Icon
                                        icon="line-md:edit-twotone"
                                        class="sm-icon"
                                    />
                                    <span class="ml-25">
                                        {{ $t('common.button.edit') }}
                                    </span>
                                </b-button>
                            </Transition>
                            <b-button
                                v-waves
                                type="button"
                                variant="success"
                                class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                                @click="onReport"
                            >
                                <Icon
                                    icon="material-symbols:print-outline-rounded"
                                    class="sm-icon"
                                />
                                <span class="ml-25">
                                    {{ $t('common.button.print') }}
                                </span>
                            </b-button>
                            <b-button
                                v-if="!editing"
                                v-waves
                                :to="{ path: '/reports/problem/list' }"
                                type="button"
                                variant="outline-secondary"
                                class="mx-50 mb-50 btn-120 btn-hover-linear-secondary border-0"
                            >
                                <Icon
                                    icon="line-md:arrow-small-left"
                                    class="sm-icon"
                                />
                                <span class="ml-25">
                                    {{ $t('common.button.back') }}
                                </span>
                            </b-button>
                            <b-button
                                v-if="editing"
                                v-waves
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary btn-hover-linear-secondary border-0"
                                @click="stopEdit"
                            >
                                <Icon icon="mdi:cancel" class="sm-icon" />
                                <span class="ml-50">
                                    {{ $t('common.button.cancel') }}
                                </span>
                            </b-button>
                        </div>
                    </b-col>
                </b-row>
            </div>
        </b-card-body>
        <b-modal
            v-model="showReportModal"
            size="xl"
            :title="$t('Problem.Report.Title')"
        >
            <telerik-report-viewer
                ref="reportViewer"
                :report-source="reportViewer.reportSource"
                :parameters="reportViewer.parameters"
                class="reportViewer"
            />
            <template #modal-footer>
                <b-button variant="primary" @click="showReportModal = false">
                    {{ $t('common.button.close') }}
                </b-button>
            </template>
        </b-modal>
    </validation-observer>
</template>

<script>
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    components: {},
    data() {
        return {
            createData: {
                contractorId: null,
                personId: null,
                genderStr: null,
                birthdayStr: null,
                eventTypeId: null,
                warningLevelId: null,
                accessTime: null,
                areaId: null,
                image: null,
            },
            listContractor: [],
            listEmployee: [],
            listWarningLevelId: [],
            listEventType: [],
            listGender: [
                { value: 0, text: 'Nữ' },
                { value: 1, text: 'Nam' },
            ],
            editing: false,
            showReportModal: false,
            reportViewer: {
                serviceUrl: `${process.env.VUE_APP_BASE_URL}/api/reports`,
                reportSource: 'ReportProblemDetail.trdp',
                parameters: {
                    pid: null,
                    lang: this.$i18n.locale,
                },
                scaleMode: 'FIT_PAGE_WIDTH',
                viewMode: 'INTERACTIVE',
                scale: 1.0,
                show: false,
            },
        }
    },
    computed: {
        problemId() {
            return this.$route.params.id
        },
        baseUrl() {
            return process.env.VUE_APP_BASE_URL
            // return "https://novaland.atin.vn/Service/"
        },
    },
    async created() {
        await this.featEventTypes()
        await this.loadContractor()
        // this.newData.genderStr = null
        // this.newData.birthdayStr = null
        // this.createData = JSON.parse(JSON.stringify(this.newData))
        // this.createData.genderStr = null
        // this.createData.birthdayStr = null
        await this.loadDetail()
    },
    methods: {
        featEventTypes() {
            this.$services
                .get(`/lookup/eventType?equipmentOnly=true`)
                .then((res) => {
                    this.listEventType = res.data.data
                })
        },
        getStaticName(id, lst) {
            // eslint-disable-next-line eqeqeq
            return lst.find((x) => x.id == id)?.text
        },
        formatWarningLevel(warningLevelId) {
            return this.$t(
                `Events.ProtectiveEquipmentEvent.WarningLevels.${warningLevelId}`
            )
        },
        async loadDetail() {
            await this.$services
                .get(`/problem/${this.problemId}`)
                .then((response) => {
                    this.createData = response.data.data
                    this.createData.contractorId =
                        this.createData.contractorId.toString()
                    this.createData.accessTimeStr = this.$moment(
                        this.createData.accessTime
                    ).format('DD/MM/YYYY HH:mm')
                    this.createData.warningName = this.formatWarningLevel(
                        this.createData.warningLevelId
                    )
                    this.createData.eventTypeName = this.getStaticName(
                        this.createData.eventTypeId,
                        this.listEventType
                    )
                })
            await this.$services
                .get(
                    `/lookup/departments/${this.createData.contractorId}/employees`
                )
                .then((response) => {
                    this.listEmployee = response.data.data
                    const employee = this.listEmployee.find(
                        (x) => x.id == this.createData.personId
                    )
                    this.createData.birthdayStr = employee.birthdayStr
                    if (employee.gender == 1) {
                        this.createData.genderStr = 'Nam'
                    } else if (employee.gender == 0) {
                        this.createData.genderStr = 'Nữ'
                    } else {
                        this.createData.genderStr = ''
                    }
                })
        },
        changepersonId(event) {
            console.log(event)
            this.createData.genderStr = null
            this.createData.birthdayStr = null
            if (event) {
                const employee = this.listEmployee.find((x) => x.id == event)
                console.log('employy', employee)

                this.createData.birthdayStr = employee.birthdayStr
                if (employee.gender == 1) {
                    this.createData.genderStr = 'Nam'
                } else if (employee.gender == 0) {
                    this.createData.genderStr = 'Nữ'
                } else {
                    this.createData.genderStr = ''
                }
                console.log('createdata', this.createData)
            }
        },
        async changeContractorId(event) {
            this.createData.genderStr = null
            this.createData.birthdayStr = null
            this.createData.personId = null
            if (event) {
                await this.$services
                    .get(`/lookup/departments/${event}/employees`)
                    .then((response) => {
                        this.listEmployee = response.data.data
                    })
            } else {
                this.listEmployee = []
            }
        },
        async getData() {
            const response = await this.$services.get(
                `/event/eventFilesById/${this.createData.eventId}`
            )
            const newData = response.data.map((x) => ({
                ...x,
                id: null,
                isAuto: true,
            }))
        },
        getUrl(path) {
            return `${process.env.VUE_APP_BASE_URL}/IMSFile/${path}`
            // return `http://192.168.1.85:42001/Service/IMSFile/${path}`
        },
        getUrlIsAuto(path) {
            return `${process.env.VUE_APP_BASE_URL}${path}`
            // return `http://192.168.1.85:42001/Service/${path}`
        },
        loadContractor() {
            this.$services
                .get('/lookup/departments?type=2')
                .then((response) => {
                    this.listContractor = response.data.data
                })
        },

        validateAndSubmitForm() {
            this.$refs.rules.validate().then((isValid) => {
                if (isValid) {
                    this.submitForm()
                }
            })
        },
        async submitForm() {
            try {
                const res = await this.$services.put(
                    `/problem/${this.problemId}`,
                    this.createData
                )
                this.showSuccessToast(res.data)
                this.stopEdit()
            } catch (error) {
                this.showErrorToast(error)
            }
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(`Success.Update`),
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
        showErrorToast(error) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(
                        'categories.waterWarning.Label.notification.error'
                    ),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(`${error.message}`),
                },
            })
        },
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.loadDetail()
        },
        onReport() {
            this.reportViewer.parameters.pid = this.problemId
            this.reportViewer.parameters.lang = this.$i18n.locale
            this.showReportModal = true

            // Refresh report after modal opens
            this.$nextTick(() => {
                if (this.$refs.reportViewer) {
                    this.$refs.reportViewer.refresh()
                }
            })
        },
    },
}
</script>
<style></style>
