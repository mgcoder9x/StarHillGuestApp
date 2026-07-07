<template>
    <div>
        <validation-observer ref="rules">
            <!-- Filters -->
            <b-card no-body>
                <b-card-body>
                    <!-- Search Form -->
                    <b-form @submit.prevent="search">
                        <b-row>
                            <!-- FromDate -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        this.$t('FireWorkEvent.Field.FromDate')
                                    "
                                    label-for="h-searchForm-dateFrom"
                                    label-cols-md="3"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        :rules="`fromDate:` + searchForm.dateTo"
                                        name="NotificationEventTemplateName"
                                    >
                                        <date-picker
                                            id="h-searchForm-dateFrom"
                                            v-model="searchForm.dateFrom"
                                            type="datetime"
                                            :locale="currentLocale"
                                            format="DD-MM-YYYY HH:mm:ss"
                                            value-type="YYYY-MM-DD HH:mm:ss"
                                            style="width: 100%"
                                            class="custom-date-picker"
                                            @change="search"
                                        ></date-picker>
                                        <small class="text-danger">{{
                                            errors[0]
                                        }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>
                            <!-- CascadeFW -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        this.$t('FireWorkEvent.Field.CascadeFW')
                                    "
                                    label-for="h-searchForm-cascadeFW"
                                    label-cols-md="3"
                                >
                                    <tree-select
                                        v-model="searchForm.cascadeFWId"
                                        :options="listCascadeFW"
                                        label="text"
                                        :reduce="(cascadeFW) => cascadeFW.id"
                                        placeholder=""
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                            <!-- ClassFW -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        this.$t('FireWorkEvent.Field.ClassFW')
                                    "
                                    label-for="h-searchForm-cascadeFW"
                                    label-cols-md="3"
                                >
                                    <!-- <v-select
                                        v-model="searchForm.classFWId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(classFWId) => classFWId.id"
                                        :options="rechangeOptions(listClassFW)"
                                        @input="search"
                                    /> -->
                                    <tree-select
                                        v-model="searchForm.classFWId"
                                        :options="listCascadeFW"
                                        label="text"
                                        :reduce="(cascadeFW) => cascadeFW.id"
                                        placeholder=""
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>
                        <b-row>
                            <!-- ToDate -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        this.$t('FireWorkEvent.Field.ToDate')
                                    "
                                    label-for="h-searchForm-dateTo"
                                    label-cols-md="3"
                                >
                                    <date-picker
                                        id="h-searchForm-dateTo"
                                        v-model="searchForm.dateTo"
                                        type="datetime"
                                        :locale="currentLocale"
                                        format="DD-MM-YYYY HH:mm:ss"
                                        value-type="YYYY-MM-DD HH:mm:ss"
                                        style="width: 100%"
                                        @change="search"
                                    ></date-picker>
                                </b-form-group>
                            </b-col>
                        </b-row>
                    </b-form>
                </b-card-body>
            </b-card>
        </validation-observer>
        <!-- Table -->
        <b-card title="">
            <div
                style="text-align: end; margin-bottom: 10px; margin-top: -10px"
            >
                <b-button>
                    <downloadExcel
                        :header="headerExcelDetail"
                        :name="$t('FireWorkEvent.Common.Header.Excel')"
                        :fetch="exportData"
                        type="xlsx"
                        :fields="export_fields_vi"
                    >
                        {{ $t('Button.ExportExcel') }}
                    </downloadExcel>
                </b-button>
            </div>

            <!-- table -->
            <BasicTable
                ref="eventTable"
                :columns="columns"
                data-url="/fireWorkEvents"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="fireWorkEventTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Device -->
                    <span
                        v-if="props.column.field == 'deviceId'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ $t(props.row.deviceName) }}
                    </span>
                    <!-- Column: CascadeFW -->
                    <span
                        v-else-if="props.column.field == 'cascadeFwId'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ $t(props.row.cascadeFWName) }}
                    </span>
                    <!-- Column: CascadeFW -->
                    <span
                        v-else-if="props.column.field == 'classFwId'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ $t(props.row.classFWName) }}
                    </span>
                    <!-- Column: Status -->
                    <span
                        v-else-if="props.column.field == 'status'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ $t(props.row.statusStr) }}
                    </span>
                    <!-- Column: FireWorkId -->
                    <span
                        v-else-if="props.column.field == 'fireWorkId'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ $t(props.row.fireWorkStr) }}
                    </span>
                    <!-- Column: Image -->
                    <span
                        v-else-if="props.column.field == 'image'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                            width: 80px;
                        "
                    >
                        <img
                            :src="`${baseURL}${props.row.filePath}`"
                            loading="lazy"
                            alt="Image"
                            style="
                                object-fit: cover;
                                border-radius: 0.3em;
                                max-width: 100%;
                            "
                            class="cursor-pointer"
                            @click="showModalEvent(props.row.eventId)"
                        />
                    </span>
                    <span v-else-if="props.column.field === 'holeIds'">
                        <div style="width: 80px">
                            <span>{{ nomalizeDate(props.row.holeIds) }}</span>
                        </div>
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <div class="center-icon text-nowrap">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                @click="goToDetail(props.row.eventId)"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                        </div>
                    </span>
                </template>
                <!-- Column: Actions -->
                <template slot="action" slot-scope="props">
                    <b-button
                        v-if="authorize(['ViewFireWorkEvent'])"
                        v-b-tooltip.hover
                        v-waves
                        variant="label-secondary"
                        class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                        :title="$t('common.button.detail')"
                        :to="{
                            path: `/events/fireWorkEvents/detail/${props.row.id}`,
                        }"
                    >
                        <feather-icon icon="EyeIcon" />
                    </b-button>
                </template>
            </BasicTable>
        </b-card>
        <!-- Image Slider -->
        <b-modal
            v-model="modalImgEventShow"
            title="Ảnh sự kiện"
            ok-title="Đóng"
            hide-header-close
            ok-only
            size="lg"
        >
            <b-carousel id="carousel-example-generic" indicators controls>
                <b-carousel-slide
                    v-for="item in listEventFiles"
                    :key="item.id"
                    :img-src="`${baseURL}${item.filePath}`"
                />
            </b-carousel>
        </b-modal>
    </div>
</template>

<script>
/* eslint-disable */
import { $themeConfig } from '@themeConfig'
import { lookupService } from '@/services'
import { onMounted, ref } from '@vue/composition-api'
const isDevEnv = process.env.NODE_ENV == 'development'
import moment from 'moment'
export default {
    setup() {
        const { apiURL } = $themeConfig.app
        const listCascadeFW = ref([])
        const listClassFW = ref([])
        onMounted(async () => {
            listCascadeFW.value = await lookupService.getAreasTree()
            listClassFW.value = await lookupService.getAreas()
        })
        return {
            apiURL,
            listCascadeFW,
            listClassFW,
        }
    },
    data() {
        return {
            searchForm: {
                cascadeFWId: null,
                classFWId: null,
                dateFrom: null,
                dateTo: null,
                eventTypeId: 601,
            },
            headerExcelDetail: [],
            export_fields_vi: {
                Ngày: 'accessDateStr',
                Giờ: 'accessTimeStr',
                'Thiết bị': 'deviceName',
                'Dây chuyền': 'cascadeFWName',
                Lớp: 'classFWName',
                'Loại giàn phun': 'fireWorkStr',
                'Lỗ hỏng': 'holeIds',
                'Trạng thái	': 'statusStr',
            },
            baseURL: isDevEnv ? 'http://localhost:1938' : this.apiURL,
            modalImgEventShow: false,
            listEventFiles: [],
            columns: [
                {
                    label: 'FireWorkEvent.Field.Date',
                    field: 'accessDateStr',
                },
                {
                    label: 'FireWorkEvent.Field.Time',
                    field: 'accessTimeStr',
                },
                {
                    label: 'FireWorkEvent.Field.Device',
                    field: 'deviceId',
                },
                {
                    label: 'FireWorkEvent.Field.CascadeFW',
                    field: 'cascadeFwId',
                },
                {
                    label: 'FireWorkEvent.Field.ClassFW',
                    field: 'classFwId',
                },
                {
                    label: 'FireWorkEvent.Field.FireWorkType',
                    field: 'fireWorkId',
                },
                {
                    label: 'FireWorkEvent.Field.Hole',
                    field: 'holeIds',
                },
                {
                    label: 'FireWorkEvent.Field.Status',
                    field: 'status',
                },
                {
                    label: 'FireWorkEvent.Field.Image',
                    field: 'image',
                },
                {
                    label: 'FireWorkEvent.Field.Operation',
                    field: 'action',
                },
            ],
        }
    },
    created() {
        //this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    methods: {
        goToDetail(eventId) {
            this.$router.push({
                path: `/event/fireWorkEvents/Detail/${eventId}`,
                query: { from: 'Screen1' }
            });
        },
        async exportData() {
            var vm = this
            let className
            if (vm.searchForm.classFWId != null) {
                vm.listClassFW.forEach((area) => {
                    if (area.id == vm.searchForm.classFWId) {
                        className = area.text
                    }
                })
            } else {
                className = vm.$t('Export.All')
            }

            let cascadeName
            if (vm.searchForm.cascadeFWId != null) {
                vm.listCascadeFW.forEach((dv) => {
                    if (dv.Id == vm.searchForm.cascadeFWId) {
                        cascadeName = dv.text
                    }
                })
            } else {
                cascadeName = vm.$t('Export.All')
            }

            let dateFrom = moment(this.searchForm.dateFrom).format(
                'DD/MM/YYYY HH:mm'
            )
            let dateTo = moment(this.searchForm.dateTo).format(
                'DD/MM/YYYY HH:mm'
            )
            if (dateFrom == 'Invalid date') {
                dateFrom = moment('2020-01-01 00:00:00').format(
                    'DD/MM/YYYY HH:mm'
                )
            }
            if (dateTo == 'Invalid date') {
                dateTo = moment().format('DD/MM/YYYY HH:mm')
            }

            vm.headerExcelDetail = [
                vm.$t('FireWorkEvent.Common.Header.Excel'),
                `${vm.$t('Events.SearchForm.FromDate')}: ${dateFrom}    ${vm.$t('Events.SearchForm.ToDate')} :${dateTo}`,
                `${vm.$t('Events.SearchForm.ClassFW')} ${className}`,
                `${vm.$t('Events.SearchForm.CascadeFW')} ${cascadeName}`,
            ]
            var pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'accessTime',
                sortDesc: true,
            }
            //var formData = $.extend({}, pagination, vm.searchForm);
            var formData =
                new URLSearchParams(pagination).toString() +
                '&' +
                new URLSearchParams(vm.searchForm).toString()
            var response = await this.$services.get(
                vm.$refs.eventTable.dataUrl + '?' + formData
            )
            for (var i = 0; i < response.data.data.data.length; i++) {
                response.data.data.data[i].fireWorkStr = vm.$t(
                    response.data.data.data[i].fireWorkStr
                )
                response.data.data.data[i].statusStr = vm.$t(
                    response.data.data.data[i].statusStr
                )
            }
            return response.data.data.data
        },
        rechangeOptions(dataList) {
            return dataList?.map((item) => ({
                ...item,
                text: this.$t(item.text),
            }))
        },
        search() {
            this.$refs.eventTable.refresh()
        },
        showModalEvent(eventId) {
            this.loadEventFiles(eventId)
            this.modalImgEventShow = true
        },
        loadEventFiles(eventId) {
            this.$services
                .get(`/event/eventFilesById/${eventId}`)
                .then((response) => {
                    this.listEventFiles = response.data
                })
        },
        nomalizeDate(data) {
            return data?.toString().replace(/,/g, ', ')
        },
    },
}
</script>

<style lang="scss">
@import '@/assets/scss/_custom-tree-select.scss';

.center-icon {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100%; /* Đảm bảo bao phủ toàn bộ chiều cao của ô */
}
</style>
