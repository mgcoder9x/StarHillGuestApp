<template>
    <div>
        <!-- Filters -->
        <validation-observer ref="rules">
            <b-card no-body>
                <b-card-body>
                    <!-- Bọc form trong ValidationObserver để điều khiển validate -->
                    <!-- GIỮ 1 FORM DUY NHẤT -->
                    <b-form @submit.prevent="search">
                        <!-- Hàng 1 -->
                        <b-row>
                            <!-- FromDate -->
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('FaceEvent.Field.FromDate')"
                                    label-for="h-searchForm-dateFrom"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <!-- Rule fromDate truyền vào filterDateTo -->
                                    <validation-provider
                                        #default="{ errors }"
                                        :rules="`fromDate:` + searchForm.dateTo"
                                        name="FromDate"
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
                                        />
                                        <!-- Hiển thị lỗi như mẫu -->
                                        <small class="text-danger">{{
                                            errors[0]
                                        }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>

                            <!-- Area -->
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('FaceEvent.Field.Area')"
                                    label-for="h-searchForm-area"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <tree-select
                                        v-model="searchForm.areaId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(area) => area.id"
                                        :multiple="true"
                                        :options="searchDatas[2].options"
                                        :value-consists-of="'ALL'"
                                        placeholder=""
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        value-consists-of="ALL"
                                        @input="changeArea()"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Device -->
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('FaceEvent.Field.Device')"
                                    label-for="h-searchForm-device"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <tree-select
                                        v-model="searchForm.deviceId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(area) => area.id"
                                        :multiple="true"
                                        :options="listDeviceByAreaId"
                                        :value-consists-of="'ALL'"
                                        placeholder=""
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        value-consists-of="ALL"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <!-- Hàng 2 -->
                        <b-row>
                            <!-- ToDate -->
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('FaceEvent.Field.ToDate')"
                                    label-for="h-searchForm-dateTo"
                                    :label-cols-md="form.labelColsMd"
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
                                    />
                                </b-form-group>
                            </b-col>
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('Events.SearchForm.Direction')"
                                    label-for="h-searchForm-direction"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <v-select
                                        id="h-searchForm-direction"
                                        v-model="searchForm.direction"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(opt) => opt.id"
                                        :options="directionOptions"
                                        :clearable="true"
                                        :searchable="false"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                            <!-- Identification -->
                            <!-- <b-col :md="form.colMd">
                                <b-form-group
                                    :label="
                                        $t('FaceEvent.Field.Identification')
                                    "
                                    :label-cols-md="form.labelColsMd"
                                    label-for="h-searchForm-identification"
                                >
                                    <v-select
                                        id="h-searchForm-identification"
                                        v-model="searchForm.filterIdentifi"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(item) => item.id"
                                        :options="lstOptionIdentification"
                                        :multiple="false"
                                        class="treeselect-nowrap"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col> -->

                            <!-- Status -->
                            <!-- <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('FaceEvent.Field.Status')"
                                    :label-cols-md="form.labelColsMd"
                                    label-for="h-searchForm-status"
                                >
                                    <v-select
                                        id="h-searchForm-status"
                                        v-model="searchForm.filterStatus"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(item) => item.id"
                                        :options="searchDatas[5].options"
                                        :multiple="true"
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        value-consists-of="ALL"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col> -->
                        </b-row>

                        <!-- Hàng 3 -->
                        <b-row class="align-items-center">
                            <!-- Name/Code -->
                            <!-- <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('FaceEvent.Field.NameOrCode')"
                                    :label-cols-md="form.labelColsMd"
                                    label-for="h-searchForm-namecode"
                                >
                                    <b-form-input
                                        id="h-searchForm-namecode"
                                        v-model.trim="searchForm.filterNameCode"
                                        type="text"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col> -->

                            <!-- Object -->
                            <!-- <b-col :md="form.colMd">
                                <b-form-group
                                    :label="
                                        $t('FaceEvent.Detail.RecognitionObject')
                                    "
                                    :label-cols-md="form.labelColsMd"
                                    label-for="h-searchForm-status"
                                >
                                    <v-select
                                        id="h-searchForm-object"
                                        v-model="searchForm.filterPersonType"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(item) => item.id"
                                        :options="searchDatas[6].options"
                                        :multiple="false"
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        value-consists-of="ALL"
                                        @input="changePersonType"
                                    />
                                </b-form-group>
                            </b-col> -->

                            <!-- Contractor -->
                            <!-- <b-col
                                :md="form.colMd"
                                v-if="searchForm.filterPersonType == 3"
                            >
                                <b-form-group
                                    :label="$t('FaceEvent.Field.Contractor')"
                                    label-for="h-searchForm-device"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <tree-select
                                        v-model="searchForm.filterContractorId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(dept) => dept.id"
                                        :multiple="true"
                                        :options="listContractor"
                                        :value-consists-of="'ALL'"
                                        placeholder=""
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        value-consists-of="ALL"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col> -->
                            <!-- Department -->
                            <!-- <b-col
                                :md="form.colMd"
                                v-if="searchForm.filterPersonType == 1"
                            >
                                <b-form-group
                                    :label="$t('FaceEvent.Field.Department')"
                                    label-for="h-searchForm-device"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <tree-select
                                        v-model="searchForm.filterDeptId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(dept) => dept.id"
                                        :multiple="true"
                                        :options="listDepartment"
                                        :value-consists-of="'ALL'"
                                        placeholder=""
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        value-consists-of="ALL"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col> -->

                            <!-- Toggle Advanced -->
                            <!-- <b-col :md="form.colMd">
                                <b-form-group
                                    label-cols="4"
                                    class="mb-0 d-flex align-items-center"
                                    label-for="h-searchForm-search"
                                    :label="
                                        $t('FaceEvent.Field.AdvancedSearch')
                                    "
                                >
                                    <b-form-checkbox
                                        id="h-searchForm-search"
                                        v-model="showAdvanced"
                                        switch
                                        size="sm"
                                        class="mb-0"
                                    />
                                </b-form-group>
                            </b-col> -->
                        </b-row>

                        <b-collapse v-model="showAdvanced">
                            <b-row no-gutters class="align-items-stretch">
                                <!-- ẢNH -->
                                <b-col md="4" class="pr-md-50 mb-75">
                                    <div class="face-dropzone">
                                        <div class="dropzone-inner">
                                            <b-img
                                                v-if="faceImagePreview"
                                                :src="faceImagePreview"
                                                class="preview-img shadow-sm"
                                                :alt="
                                                    $t(
                                                        'FaceEvent.Field.FaceImage'
                                                    )
                                                "
                                                fluid
                                            />
                                            <div
                                                v-else
                                                class="preview-placeholder"
                                            >
                                                <Icon
                                                    icon="mdi:account-circle"
                                                    class="avatar-xxl"
                                                />
                                                <small class="text-muted mt-25">
                                                    {{
                                                        $t(
                                                            'FaceEvent.Field.DropZoneText'
                                                        )
                                                    }}
                                                </small>
                                            </div>
                                        </div>

                                        <b-form-file
                                            v-model="faceFile"
                                            accept="image/*"
                                            size="sm"
                                            browse-text="Browse"
                                            drop-placeholder="Kéo file vào đây"
                                            class="mt-50"
                                            @change="handleFileUpload"
                                        />
                                    </div>
                                </b-col>

                                <!-- SLIDER -->
                                <b-col
                                    md="4"
                                    class="pl-md-50"
                                    style="padding: 25px"
                                >
                                    <div class="face-slider w-100">
                                        <div
                                            class="d-flex align-items-center justify-content-between mb-25"
                                        >
                                            <span class="slider-label mb-0">
                                                {{
                                                    $t(
                                                        'FaceEvent.Field.IdentificationScore'
                                                    )
                                                }}
                                            </span>
                                            <span class="slider-value"
                                                >{{ identifyPercent }}%</span
                                            >
                                        </div>
                                        <input
                                            v-model.number="
                                                searchForm.identifyScore
                                            "
                                            type="range"
                                            min="0"
                                            max="1"
                                            step="0.01"
                                            class="range"
                                        />
                                        <div
                                            class="d-flex justify-content-between mt-25 text-muted xsmall"
                                        >
                                            <span>0%</span><span>50%</span
                                            ><span>100%</span>
                                        </div>
                                        <small class="text-muted d-block mt-25">
                                            {{
                                                $t(
                                                    'FaceEvent.Field.SimilarityNote',
                                                    {
                                                        percent:
                                                            identifyPercent,
                                                    }
                                                )
                                            }}
                                        </small>
                                    </div>
                                </b-col>
                            </b-row>
                        </b-collapse>

                        <!-- hidden submit để Enter -->
                        <b-button
                            type="submit"
                            style="
                                position: absolute;
                                width: 1px;
                                height: 1px;
                                overflow: hidden;
                                clip: rect(0, 0, 0, 0);
                            "
                        >
                            {{ $t('common.button.search') }}
                        </b-button>
                    </b-form>
                </b-card-body>
            </b-card>
        </validation-observer>

        <!-- Actions + Table -->
        <b-card title="">
            <div class="text-left mb-1 d-flex" style="gap: 8px">
                <b-button
                    v-waves
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                >
                    <downloadExcel
                        :header="headerExcelDetail"
                        :name="$t('FaceEvent.Header.Excel')"
                        :fetch="exportData"
                        type="xlsx"
                        :fields="export_fields_vi"
                    >
                        <div class="d-flex align-items-center">
                            <Icon
                                icon="famicons:enter-outline"
                                class="sm-icon"
                            />
                            <span class="ml-25">{{
                                $t('Button.ExportExcel')
                            }}</span>
                        </div>
                    </downloadExcel>
                </b-button>

                <b-button
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                    @click="refresh()"
                >
                    <Icon icon="famicons:enter-outline" class="sm-icon" />
                    <span class="ml-25">{{ $t('Button.Refresh') }}</span>
                </b-button>
            </div>

            <BasicTable
                ref="vuegoodTable"
                :columns="table.columns"
                :data-url="table.dataUrl"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="faceEventTable"
            >
                <template v-slot:table-row="{ column, row }">
                    <span
                        v-if="column.field === 'image'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        <img
                            :src="`${imgUrl}${row.image}`"
                            alt="Image"
                            style="
                                width: 60px;
                                height: 80px;
                                object-fit: cover;
                                border-radius: 0.3em;
                            "
                            class="cursor-pointer"
                            loading="lazy"
                            @click="openModal(`${imgUrl}${row.image}`)"
                        />
                        <!-- <div
                            v-else
                            style="
                                width: 100px;
                                height: 100px;
                                display: flex;
                                flex-direction: column;
                                justify-content: center;
                                align-items: center;
                                background-color: #f3f4f6;
                                border-radius: 0.3em;
                                color: #9ca3af;
                            "
                        >
                            <Icon
                                icon="mdi:account-circle"
                                :style="{ fontSize: '32px' }"
                            />
                            <small style="font-size: 10px; margin-top: 4px"
                                >No Image</small
                            >
                        </div> -->
                    </span>
                    <!-- Column: Gender -->
                    <span v-else-if="column.field === 'gender'">
                        <span v-if="row.gender === 1">{{
                            $t('FaceEvent.Detail.Female')
                        }}</span>
                        <span v-else-if="row.gender === 0">{{
                            $t('FaceEvent.Detail.Male')
                        }}</span>
                        <span v-else></span>
                    </span>
                    <span v-else-if="column.field === 'status'">
                        <span v-if="row.status === 1">{{
                            $t('accessType.deny')
                        }}</span>
                        <span v-else-if="row.status === 0">{{
                            $t('accessType.allow')
                        }}</span>
                        <span v-else></span>
                    </span>

                    <!-- Column: Action -->
                    <span v-else-if="column.field === 'action'">
                        <div class="center-icon text-nowrap">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/event/faceGateEvent/detail/${row.eventId}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>

                            <!-- <b-button
                                v-if="
                                    row.personId &&
                                    row.personId !==
                                        '00000000-0000-0000-0000-000000000000' &&
                                    row.userCode
                                "
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="
                                    $t('route.breadcrumb.dashboardItinerary')
                                "
                                :to="{
                                    path: `/event/faceEvent/itinerary/1/${row.personId}/${searchForm.dateFrom}/${searchForm.dateTo}`,
                                }"
                            >
                                <Icon icon="mdi:map" class="xs-icon" />
                            </b-button> -->
                        </div>
                    </span>
                </template>
            </BasicTable>

            <!-- Modal -->
            <b-modal
                v-model="isModalOpen"
                :title="$t('Events.SearchForm.EventPhotos')"
                hide-footer
                size="lg"
            >
                <b-carousel id="carousel-example-generic" indicators controls>
                    <div class="d-flex justify-content-center">
                        <img
                            :src="modalImageUrl"
                            loading="lazy"
                            alt="Full Image"
                            style="
                                height: 60vh;
                                object-fit: contain;
                                object-position: center;
                            "
                        />
                    </div>
                </b-carousel>
            </b-modal>
        </b-card>
    </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import getBaseUrl from '@/utils/get-baseUrl'
import moment from 'moment'
import TreeHelper from '@/utils/treeHelper'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'
import { ValidationObserver, ValidationProvider, extend } from 'vee-validate'

// Rule: FromDate phải <= ToDate (hiển thị lỗi ngay dưới ô Từ ngày)
// extend('fromDate', {
//     params: ['to'],
//     message: 'Thời gian từ ngày phải nhỏ hơn thời gian đến ngày',
//     validate(value, { to }) {
//         if (!value || !to) return true
//         const fmt = 'YYYY-MM-DD HH:mm:ss'
//         const f = moment(value, fmt, true)
//         const t = moment(to, fmt, true)
//         if (!f.isValid() || !t.isValid()) return true
//         return f.isSameOrBefore(t)
//     },
// })

export default {
    components: { TreeHelper, ValidationObserver, ValidationProvider },
    mixins: [authorizationMixin],
    data() {
        return {
            showAdvanced: false,
            searchDatas: [
                {
                    searchType: 'date-picker',
                    filterName: 'filterDateFrom',
                    label: 'Từ ngày',
                    locale: this.$i18n.locale,
                    style: 'width: 100%',
                    autoSearch: true,
                    md: 4,
                    modelValue: null,
                },
                {
                    searchType: 'tree-select',
                    filterName: 'filterDeviceId',
                    label: 'Thiết bị',
                    multiple: true,
                    options: [],
                    autoSearch: true,
                    md: 4,
                },
                {
                    searchType: 'tree-select',
                    filterName: 'filterAreaId',
                    label: 'Khu vực',
                    multiple: true,
                    options: [],
                    autoSearch: true,
                    md: 4,
                },
                {
                    searchType: 'date-picker',
                    filterName: 'filterDateTo',
                    label: 'Đến ngày',
                    locale: this.$i18n.locale,
                    style: 'width: 100%',
                    autoSearch: true,
                    md: 4,
                },
                {
                    filterName: 'filterNameCode',
                    label: 'Mã/Tên đối tượng',
                    options: [],
                    md: 4,
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterStatus',
                    label: 'Trạng thái',
                    options: [],
                    autoSearch: true,
                    md: 4,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterPersonType',
                    label: 'Loại đối tượng',
                    options: [],
                    autoSearch: true,
                    md: 4,
                },
            ],

            searchForm: {
                dateFrom: null,
                dateTo: null,
                areaId: null,
                deviceId: null,
                filterNameCode: null,
                filterStatus: null,
                filterIdentifi: null,
                filterPersonType: null,
                direction: null,
                // Advanced
                gender: null,
                ageFrom: null,
                ageTo: null,
                eventTypeIds: null,
                // điểm nhận diện
                identifyScore: 0.85,

                // CSV eventIds (từ POST face-search) để GET
                filterEventIdStr: '',

                // FilterMode: 1 = FaceGateEvent (chỉ hiển thị Gender và Age != null)
                filterMode: 1,
            },

            // Ảnh chỉ để hiển thị & gửi POST – KHÔNG đưa vào searchForm
            faceFile: null,
            faceImagePreview: '',

            form: { colMd: 4, labelColsMd: 4 },

            table: {
                dataUrl: '/peopleCountInOutEvent',
                columns: [
                    {
                        label: 'FaceEvent.Detail.Date',
                        field: 'accessTime',
                        formatFn: (v) => moment.utc(v).format('DD/MM/YYYY'),
                    },
                    {
                        label: 'FaceEvent.Detail.Time',
                        field: 'accessTime',
                        formatFn: (v) => moment.utc(v).format('HH:mm:ss'),
                    },
                    // { label: 'FaceEvent.Detail.UserCode', field: 'userCode' },
                    // { label: 'FaceEvent.Detail.UserName', field: 'userName' },
                    // {
                    //     label: 'FaceEvent.Detail.DeptOrContractor',
                    //     field: 'depName',
                    // },
                    { label: 'FaceEvent.Detail.AreaName', field: 'areaName' },
                    { label: 'FaceEvent.Detail.Device', field: 'deviceName' },
                    { label: 'Events.Table.Direction', field: 'directionStr' },
                    // {
                    //     label: 'FaceEvent.Detail.EventType',
                    //     field: 'eventTypeId',
                    //     formatFn: (v) => this.eventTypes[v],
                    // },
                    // {
                    //     label: 'FaceEvent.Detail.RecognitionObject',
                    //     field: 'personTypeName',
                    // },
                    // {
                    //     label: 'FaceEvent.Detail.Status',
                    //     field: 'status',
                    //     formatFn: (v) => this.getStatusName(v),
                    // },
                    { label: 'FaceEvent.Detail.Image', field: 'image' },
                    { label: 'FaceEvent.Detail.Operation', field: 'action' },
                ],
            },
            directionOptions: [
                {
                    id: 1,
                    text:
                        this.$t('direction.in') ||
                        this.$t('direction.In') ||
                        'Vào',
                },
                {
                    id: 2,
                    text:
                        this.$t('direction.out') ||
                        this.$t('direction.Out') ||
                        'Ra',
                },
            ],
            eventTypes: {},
            isModalOpen: false,
            modalImageUrl: null,

            headerExcelDetail: [],
            export_fields_vi: {
                'Thời gian': 'accessTime',
                Mã: 'userCode',
                'Người dùng': 'userName',
                'Phòng ban': 'depName',
                'Khu vực': 'areaName',
                'Thiết bị': 'deviceName',
                'Loại nhận diện': 'eventTypeId',
                'Trạng thái': 'status',
            },

            listDeviceByAreaId: [],
            lstDevice: [],
            listDepartment: [],
            listContractor: [],
            lstAllArea: [],
        }
    },
    computed: {
        lstOptionIdentification() {
            return [
                { id: 1, text: this.$t('FaceEvent.Common.Identification.Yes') },
                { id: 2, text: this.$t('FaceEvent.Common.Identification.No') },
            ]
        },
        identifyPercent() {
            return Math.round((this.searchForm.identifyScore || 0) * 100)
        },
        imgUrl() {
            const { VUE_APP_BASE_URL: baseURL } = process.env
            return `${baseURL}`
        },
        currentLocale() {
            return this.$i18n.locale
        },
    },
    watch: {
        'searchForm.identifyScore': function (val) {
            if (this.faceImagePreview && this.showAdvanced) {
                this.searchForm.filterEventIdStr = ''
                this.$nextTick(() => this.search())
            }
        },
        'searchForm.dateFrom': function () {
            if (this.faceImagePreview && this.showAdvanced)
                this.searchForm.filterEventIdStr = ''
        },
        'searchForm.dateTo': function () {
            if (this.faceImagePreview && this.showAdvanced)
                this.searchForm.filterEventIdStr = ''
        },
        showAdvanced(val) {
            if (!val) {
                this.faceFile = null
                this.faceImagePreview = ''
                this.searchForm.filterEventIdStr = ''
                this.searchForm.identifyScore = 0.85
                this.$nextTick(() => this.search())
            }
        },
        '$i18n.locale': function () {
            this.updateOptions()
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        this.searchForm.filterIdentifi = 1
        this.lookupData()
        this.updateOptions()

        const cached = getStorage('faceEventSearchForm')
        if (cached) {
            this.searchForm = { ...this.searchForm, ...cached }
            // Restore các state khác nếu có
            if (cached.showAdvanced !== undefined) {
                this.showAdvanced = cached.showAdvanced
            }
            if (cached.faceImagePreview) {
                this.faceImagePreview = cached.faceImagePreview
            }
        } else {
            this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
        }
    },
    mounted() {
        // Khi component được mount (bao gồm khi quay lại từ Detail)
        // Kiểm tra và tự động search nếu có cache
        const cached = getStorage('faceEventSearchForm')
        if (cached && this.$refs.vuegoodTable) {
            // Đợi một chút để đảm bảo component đã được khởi tạo hoàn toàn
            this.$nextTick(() => {
                this.$refs.vuegoodTable.refresh()
            })
        }
    },
    methods: {
        translateDirectionStr(directionStr) {
            const key = directionMap[directionStr]
            return key ? this.$t(`Events.Direction.${key}`) : directionStr
        },
        async changePersonType() {
            debugger
            this.searchForm.filterContractorId = null
            this.searchForm.filterDeptId = null

            // ✅ Validate form trước khi search – sẽ hiển thị lỗi dưới "Từ ngày"
            const ok = await this.$refs.rules?.validate()
            if (!ok) return

            // Nếu đang tìm theo ảnh -> POST trước để lấy eventIds
            await this.ensureFaceEventIds()

            // Lưu cache (form KHÔNG chứa base64) + các state khác
            const cacheData = {
                ...this.searchForm,
                showAdvanced: this.showAdvanced,
                faceImagePreview: this.faceImagePreview, // Lưu cả ảnh để restore
            }
            setStorage('faceEventSearchForm', cacheData, 120)

            // GET bảng
            this.$refs.vuegoodTable.refresh()
        },
        async ensureFaceEventIds() {
            if (!this.showAdvanced || !this.faceImagePreview) return true
            if (this.searchForm.filterEventIdStr) return true

            try {
                const res = await this.$services.post(
                    '/faceEvents/face-search',
                    {
                        faceImageBase64: this.faceImagePreview,
                        identifyScore: this.searchForm.identifyScore || 0.7,
                        filterDateFrom: this.searchForm.dateFrom || null,
                        filterDateTo: this.searchForm.dateTo || null,
                    }
                )
                const eventIds = res?.data?.data?.eventIds || []
                const MAX_IDS = 5000

                this.searchForm.filterEventIdStr =
                    eventIds.length === 0
                        ? '00000000-0000-0000-0000-000000000000'
                        : eventIds.slice(0, MAX_IDS).join(',')
                return true
            } catch (e) {
                console.error(e)
                this.$bvToast.toast('Tìm kiếm khuôn mặt lỗi!', {
                    title: this.$t('Error.Error') || 'Lỗi',
                    variant: 'danger',
                    solid: true,
                })
                this.searchForm.filterEventIdStr =
                    '00000000-0000-0000-0000-000000000000'
                return true
            }
        },

        async search() {
            // ✅ Validate form trước khi search – sẽ hiển thị lỗi dưới "Từ ngày"
            if (!this.$refs.rules) return
            const ok = await this.$refs.rules.validate()
            if (!ok) return

            // Nếu đang tìm theo ảnh -> POST trước để lấy eventIds
            await this.ensureFaceEventIds()

            // Lưu cache (form KHÔNG chứa base64) + các state khác
            const cacheData = {
                ...this.searchForm,
                showAdvanced: this.showAdvanced,
                faceImagePreview: this.faceImagePreview, // Lưu cả ảnh để restore
            }
            setStorage('faceEventSearchForm', cacheData, 120)

            // GET bảng
            this.$refs.vuegoodTable?.refresh()
        },

        refresh() {
            clearStorage('faceEventSearchForm')
            this.searchForm = {
                filterDateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                filterDateTo: null,
                filterAreaId: null,
                filterPersonType: null,
                filterDeviceId: null,
                filterNameCode: null,
                filterStatus: null,
                filterIdentifi: 1,
                filterMode: 1,
                gender: null,
                ageFrom: null,
                ageTo: null,
                eventTypeIds: null,

                identifyScore: 0.85,
                filterEventIdStr: '',
            }
            this.faceFile = null
            this.faceImagePreview = ''
            this.$nextTick(() => {
                this.$refs.rules?.validate() // ✅ Validate sau khi reset
                this.$refs.vuegoodTable?.refresh()
            })
        },

        getStatusName(value) {
            switch (value) {
                case 0:
                    return this.$t('accessType.allow')
                case 1:
                    return this.$t('accessType.deny')
                case 2:
                    return this.$t('accessType.expired')
                case 3:
                    return this.$t('accessType.noPermission')
                case 4:
                    return this.$t('accessType.wrongArea')
                case 5:
                    return this.$t('accessType.wrongTime')
                default:
                    return ''
            }
        },
        getRecognitionObjectName(value) {
            switch (value) {
                case 0:
                    return this.$t('recognitionObject.stranger')
                case 1:
                    return this.$t('recognitionObject.employee')
                case 2:
                    return this.$t('recognitionObject.visitor')
                default:
                    return ''
            }
        },
        openModal(url) {
            this.modalImageUrl = url
            this.isModalOpen = true
        },

        handleBinding(event) {
            this.searchForm[event.filterName] = event.value
            if (event.autoSearch) this.search()
        },

        handleFileUpload(event) {
            const file = event?.target?.files?.[0] || this.faceFile
            this.faceImagePreview = ''
            this.searchForm.filterEventIdStr = ''

            if (file) {
                const reader = new FileReader()
                reader.onload = (e) => {
                    this.faceImagePreview = String(e.target.result) // dataURL
                    this.search()
                }
                reader.onerror = () => {
                    this.$bvToast.toast(
                        'Không thể đọc file, vui lòng thử lại.',
                        {
                            title: this.$t('Error.Error'),
                            variant: 'danger',
                            solid: true,
                        }
                    )
                }
                reader.readAsDataURL(file)
            }
        },

        async exportData() {
            if (this.faceImagePreview && !this.searchForm.filterEventIdStr) {
                await this.ensureFaceEventIds()
            }

            const vm = this
            let allAreaName = vm.$t('Export.All')
            const areaName = []
            if (vm.searchForm.areaId != null && vm.searchForm.areaId != []) {
                vm.lstAllArea.forEach((area) => {
                    if (
                        (vm.searchForm.areaId || [])
                            .map(String)
                            .includes(area.id.toString())
                    ) {
                        areaName.push(area.label)
                    }
                })
                allAreaName = areaName.join(',')
            }

            let allDeviceName = vm.$t('Export.All')
            const deviceName = []
            if (
                vm.searchForm.deviceId != null &&
                vm.searchForm.deviceId !== []
            ) {
                vm.searchDatas[1].options.forEach((devi) => {
                    if ((vm.searchForm.deviceId || []).includes(devi.id)) {
                        deviceName.push(devi.label)
                    }
                })
                allDeviceName = deviceName.join(',')
            }

            // let allStatusName = vm.$t('Export.All')
            // const statusName = []
            // if (
            //     vm.searchForm.filterStatus != null &&
            //     vm.searchForm.filterStatus !== []
            // ) {
            //     vm.searchDatas[5].options.forEach((sta) => {
            //         if ((vm.searchForm.filterStatus || []).includes(sta.id)) {
            //             statusName.push(sta.text)
            //         }
            //     })
            //     allStatusName = statusName.join(',')
            // }

            // let allIden = vm.$t('Export.All')
            // if (vm.searchForm.filterIdentifi != null) {
            //     vm.lstOptionIdentification.forEach((ide) => {
            //         if (ide.id === vm.searchForm.filterIdentifi)
            //             allIden = ide.text
            //     })
            // }

            // const nameCode = vm.searchForm.filterNameCode ?? vm.$t('Export.All')
            let dateFrom = moment(this.searchForm.dateFrom).format(
                'DD/MM/YYYY HH:mm'
            )
            let dateTo = moment(this.searchForm.dateTo).format(
                'DD/MM/YYYY HH:mm'
            )
            if (dateFrom === 'Invalid date')
                dateFrom = moment('2020-01-01 00:00:00').format(
                    'DD/MM/YYYY HH:mm'
                )
            if (dateTo === 'Invalid date')
                dateTo = moment().format('DD/MM/YYYY HH:mm')
            const getDirectionLabel = () => {
                if (!this.searchForm.direction) {
                    return vm.$t('Export.All')
                }
                if (this.searchForm.direction === 1) {
                    return vm.$t('direction.in') || vm.$t('direction.In')
                }
                return vm.$t('direction.out') || vm.$t('direction.Out')
            }
            const direction = getDirectionLabel()
            vm.headerExcelDetail = [
                `${vm.$t('FaceEvent.Header.Excel')}`,
                `${vm.$t('WaterEvents.Label.FromDate')}: ${dateFrom}     ${vm.$t('WaterEvents.Label.ToDate')}: ${dateTo}`,
                `${vm.$t('WaterEvents.Label.Devices')}: ${allDeviceName}`,
                `${vm.$t('WaterEvents.Label.Areas')}: ${allAreaName}`,
                `${vm.$t('Events.SearchForm.Direction')}: ${direction}`,
                // `${vm.$t('VehicleEvent.Field.NameOrCode')}: ${nameCode}`,
                // `${vm.$t('VehicleEvent.Field.Status')}: ${allStatusName}`,
                // `${vm.$t('VehicleEvent.Field.Identification')}: ${allIden}`,
            ]
            vm.export_fields_vi = {
                [vm.$t('fireEvent.common.date')]: 'accessTime',
                [vm.$t('fireEvent.common.time')]: 'hour',
                [vm.$t('FaceEvent.Detail.AreaName')]: 'areaName',
                [vm.$t('FaceEvent.Detail.Device')]: 'deviceName',
                [vm.$t('Events.Table.Direction')]: 'directionStr',
                // [vm.$t('FaceEvent.Detail.UserCode')]: 'userCode',
                // [vm.$t('FaceEvent.Detail.UserName')]: 'userName',
                // [vm.$t('FaceEvent.Detail.Gender')]: 'genderName',
                // [vm.$t('FaceEvent.Detail.Age')]: 'age',
                // [vm.$t('FaceEvent.Detail.DepName')]: 'depName',
                // [vm.$t('FaceEvent.Detail.RecognitionObject')]: 'personTypeName',
                // [vm.$t('FaceEvent.Detail.EventType')]: 'eventTypeId',
                // [vm.$t('FaceEvent.Detail.Status')]: 'status',
            }

            const pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'accessTime',
                sortDesc: true,
            }
            const formQS = `${new URLSearchParams(pagination).toString()}&${new URLSearchParams(this.searchForm).toString()}`
            const response = await this.$services.get(
                `${this.$refs.vuegoodTable.dataUrl}?${formQS}`
            )

            for (let i = 0; i < response.data.data.data.length; i++) {
                response.data.data.data[i].status = vm.getStatusName(
                    response.data.data.data[i].status
                )
                response.data.data.data[i].eventTypeId =
                    vm.eventTypes[response.data.data.data[i].eventTypeId]
                const day = moment(
                    response.data.data.data[i].accessTime
                ).format('DD/MM/YYYY')
                const hour = moment(
                    response.data.data.data[i].accessTime
                ).format('HH:mm:ss')
                response.data.data.data[i].accessTime = day
                response.data.data.data[i].hour = hour
            }
            return response.data.data.data
        },

        lookupData() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                const areas = TreeHelper.removeEmptyChildren(response.data.data)
                this.searchDatas[2].options = areas.map((item) => {
                    const { text, ...rest } = item
                    return { label: text, ...rest }
                })
            })
            this.$services.get('/lookup/areas').then((response) => {
                const areas = TreeHelper.removeEmptyChildren(response.data.data)
                this.lstAllArea = areas.map((item) => {
                    const { text, ...rest } = item
                    return { label: text, ...rest }
                })
            })

            this.$services
                .get('/lookup/departments-tree?type=1')
                .then((response) => {
                    const dept = TreeHelper.removeEmptyChildren(
                        response.data.data
                    )
                    this.listDepartment = dept.map((item) => {
                        const { text, ...rest } = item
                        return { label: text, ...rest }
                    })
                })

            this.$services
                .get('/lookup/departments-tree?type=2')
                .then((response) => {
                    const cont = TreeHelper.removeEmptyChildren(
                        response.data.data
                    )
                    this.listContractor = cont.map((item) => {
                        const { text, ...rest } = item
                        return { label: text, ...rest }
                    })
                })

            this.$services.get('/lookup/devices').then((response) => {
                const devices = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
                this.lstDevice = devices
                    .filter((x) => x.eventTypeId === 700)
                    .map(({ text, ...rest }) => ({ ...rest, label: text }))
                console.log('this.listDeviceByAreaId:', this.lstDevice)
                this.listDeviceByAreaId = this.lstDevice
                // this.searchDatas[1].options = devices
                //     .filter((x) => x.eventTypeId == 200)
                //     .map((item) => {
                //         const { text, ...rest } = item
                //         return { label: text, ...rest }
                //     })
                // this.listDeviceByAreaId = this.searchDatas[1].options
            })
            this.$services.get('/lookup/eventType').then((response) => {
                response.data.data.forEach((item) => {
                    const [, eventType] = item.text.split('_')
                    this.eventTypes[item.id] = eventType
                })
            })
        },

        updateOptions() {
            this.searchDatas[5].options = [
                { id: 0, text: this.$t('accessType.allow') },
                { id: 1, text: this.$t('accessType.deny') },
                { id: 2, text: this.$t('accessType.expired') },
                { id: 3, text: this.$t('accessType.noPermission') },
                { id: 4, text: this.$t('accessType.wrongArea') },
                { id: 5, text: this.$t('accessType.wrongTime') },
            ]
            this.searchDatas[6].options = [
                { id: 0, text: this.$t('FaceEvent.Options.Stranger') },
                { id: 1, text: this.$t('FaceEvent.Options.Employee') },
                { id: 2, text: this.$t('FaceEvent.Options.Guest') },
                { id: 3, text: this.$t('FaceEvent.Options.Contractor') },
                { id: 4, text: this.$t('FaceEvent.Options.BlackList') },
            ]
        },

        changeArea() {
            this.searchForm.deviceId = null
            if (this.searchForm.areaId && this.searchForm.areaId.length > 0) {
                this.listDeviceByAreaId = this.lstDevice.filter((x) =>
                    this.searchForm.areaId.includes(x.areaId)
                )
            } else {
                this.listDeviceByAreaId = this.lstDevice
            }
            this.search()
        },
    },
}
</script>

<style lang="scss" scoped>
/* giữ nguyên style của bạn */
.avatar-crop {
    object-fit: cover;
    object-position: center;
    width: 8rem;
    height: 9rem;
    cursor: pointer;
}
.center-icon {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 4px;
}
.face-dropzone {
    padding: 0.5rem;
    border: 1px dashed rgba(24, 144, 255, 0.28);
    border-radius: 10px;
    background: linear-gradient(180deg, #f9fbff 0%, #fff 100%);
}
.dropzone-inner {
    min-height: 200px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #fff;
    border: 1px dashed rgba(24, 144, 255, 0.22);
    border-radius: 8px;
}
.preview-img {
    max-height: 192px;
    object-fit: contain;
    object-position: center;
    border-radius: 8px;
}
.preview-placeholder {
    display: flex;
    flex-direction: column;
    align-items: center;
    color: #98a2b3;
}
.avatar-xxl {
    width: 84px;
    height: 84px;
    opacity: 0.45;
}
.face-slider {
    padding: 0.75rem;
    border: 1px dashed rgba(24, 144, 255, 0.2);
    border-radius: 10px;
    background: #fbfdff;
    min-height: 200px;
    display: flex;
    flex-direction: column;
    justify-content: center;
}
.slider-label {
    font-weight: 600;
    color: #44556b;
}
.slider-value {
    font-weight: 700;
    color: #1677ff;
}
.range {
    -webkit-appearance: none;
    width: 100%;
    height: 4px;
    border-radius: 999px;
    background: linear-gradient(90deg, #1677ff 0%, #7cc4ff 100%);
    outline: none;
}
.range::-webkit-slider-thumb {
    -webkit-appearance: none;
    appearance: none;
    width: 16px;
    height: 16px;
    border-radius: 50%;
    background: #fff;
    border: 3px solid #1677ff;
    box-shadow: 0 2px 6px rgba(22, 119, 255, 0.25);
    cursor: pointer;
}
.range::-moz-range-thumb {
    width: 16px;
    height: 16px;
    border-radius: 50%;
    background: #fff;
    border: 3px solid #1677ff;
    box-shadow: 0 2px 6px rgba(22, 119, 255, 0.25);
    cursor: pointer;
}
.range::-moz-range-track {
    height: 4px;
    border-radius: 999px;
    background: linear-gradient(90deg, #1677ff 0%, #7cc4ff 100%);
}
.xsmall {
    font-size: 11px;
}
.mb-25 {
    margin-bottom: 0.25rem !important;
}
.mb-50 {
    margin-bottom: 0.5rem !important;
}
.mb-75 {
    margin-bottom: 0.75rem !important;
}
.pr-md-50 {
    @media (min-width: 768px) {
        padding-right: 0.5rem !important;
    }
}
.pl-md-50 {
    @media (min-width: 768px) {
        padding-left: 0.5rem !important;
    }
}
label[for],
.adv-inline__label {
    font-weight: 600;
    color: #44556b;
}
</style>
