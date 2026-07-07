<!-- eslint-disable vue/html-self-closing -->
<template>
    <div>
        <validation-observer ref="rules">
            <!-- Filters -->
            <b-card no-body>
                <b-card-body>
                    <b-form @submit.prevent="search">
                        <b-row>
                            <!-- Từ ngày -->
                            <b-col md="6">
                                <b-form-group
                                    :label="
                                        $t('Warning.List.SearchForm.FromDate')
                                    "
                                    label-for="fromDate"
                                    label-cols-md="3"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        :rules="`fromDate:` + searchForm.dateTo"
                                        name="NotificationEventTemplateName"
                                    >
                                        <b-form-datepicker
                                            id="fromDate"
                                            v-model="searchForm.dateFrom"
                                            :date-format-options="{
                                                day: 'numeric',
                                                month: 'long',
                                                year: 'numeric',
                                            }"
                                            :state="
                                                searchForm.dateFrom.checkDate
                                                    ? !searchForm.dateFrom
                                                    : null
                                            "
                                            reset-button
                                            type="datetime"
                                            @input="search"
                                        />
                                        <small class="text-danger">
                                            {{ errors[0] }}
                                        </small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>

                            <!-- Trạng thái đã xem / chưa xem -->
                            <b-col md="6">
                                <b-form-group
                                    :label="
                                        $t('Warning.List.SearchForm.Status')
                                    "
                                    label-for="h-searchForm-status"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        v-model="searchForm.read"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(item) => item.id"
                                        :options="rechangeOptions(listStatus)"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Đến ngày -->
                            <b-col md="6">
                                <b-form-group
                                    :label="
                                        $t('Warning.List.SearchForm.ToDate')
                                    "
                                    label-for="toDate"
                                    label-cols-md="3"
                                >
                                    <b-form-datepicker
                                        id="toDate"
                                        v-model="searchForm.dateTo"
                                        :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }"
                                        reset-button
                                        type="datetime"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Khu vực -->
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('Device.List.SearchForm.Area')"
                                    label-for="h-searchForm-area"
                                    label-cols-md="3"
                                >
                                    <tree-select
                                        v-model="searchForm.areaId"
                                        :options="listArea"
                                        label="text"
                                        :reduce="(item) => item.id"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Chế độ (On / Off) -->
                            <b-col md="6">
                                <b-form-group
                                    label="Chế độ"
                                    label-for="h-searchForm-mode"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        id="h-searchForm-mode"
                                        v-model="searchForm.isBlocked"
                                        label="text"
                                        :options="pauseFilterOptions"
                                        :reduce="(status) => status.value"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <!-- submit ẩn -->
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
                            Search
                        </b-button>
                    </b-form>
                </b-card-body>
            </b-card>
        </validation-observer>

        <!-- Bảng cảnh báo -->
        <b-card title="">
            <BasicTable
                ref="eventTable"
                :columns="columns"
                data-url="/notification"
                :search-form="searchForm"
                :sort-by="'accessDate'"
                :highlight-row="true"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Ảnh -->
                    <span
                        v-if="props.column.field === 'image'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        <img
                            :src="`${baseURL}${props.row.image}`"
                            loading="lazy"
                            alt="Image"
                            width="120"
                            height="80"
                            style="object-fit: cover; border-radius: 0.3em"
                            class="cursor-pointer"
                            @click="showModalEvent(props.row.eventId)"
                        />
                    </span>

                    <!-- Trạng thái đã xem -->
                    <span v-else-if="props.column.field === 'readStr'">
                        {{ formatStatus(props.row.readStr) }}
                    </span>

                    <!-- Cột CHẾ ĐỘ: On / Off (x phút | x giờ) -->
                    <span v-else-if="props.column.field === 'mode'">
                        <span class="text-danger">
                            {{ getPauseLabel(props.row) }}
                        </span>
                    </span>

                    <!-- Thao tác -->
                    <span v-else-if="props.column.field === 'action'">
                        <div class="center-icon text-nowrap">
                            <!-- Xem chi tiết -->
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                :href="props.row.url"
                                @click="readNotification(props.row.id)"
                                target="_blank"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>

                            <!-- Tạo Issue -->
                            <b-button
                                v-b-tooltip.hover
                                v-if="authorize(['ManageIssue'])"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                variant="label-secondary"
                                :title="$t('Button.CreateIssue')"
                                @click="createIssue(props.row)"
                            >
                                <Icon icon="mdi:alert" class="xs-icon" />
                            </b-button>

                            <!-- Tạm dừng nhận cảnh báo (modal chi tiết) -->
                            <b-button
                                v-b-tooltip.hover
                                size="sm"
                                variant="outline-danger"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                title="Tạm dừng nhận cảnh báo"
                                @click="openPauseModal(props.row)"
                            >
                                <Icon
                                    icon="mdi:bell-off-outline"
                                    class="xs-icon"
                                />
                            </b-button>
                        </div>
                    </span>

                    <!-- Các cột khác giữ nguyên -->
                    <span v-else>
                        {{ props.formattedRow[props.column.field] }}
                    </span>
                </template>
            </BasicTable>
        </b-card>

        <!-- Modal tạo Issue -->
        <b-modal
            v-model="modalCreateIssue"
            :title="$t('Issue.Header')"
            size="lg"
            hide-footer
        >
            <create-issue
                :createData="dataCreateIssue"
                @success="modalCreateIssue = false"
            />
        </b-modal>

        <!-- Modal xem ảnh sự kiện -->
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

        <!-- Modal tạm dừng nhận cảnh báo -->
        <b-modal
            v-model="modalPause"
            title="Xác nhận Không gửi cảnh báo"
            hide-footer
            size="lg"
            header-class="pause-modal-header"
        >
            <!-- Nếu đang bị chặn, hiển thị thời gian hiện tại -->
            <p v-if="pauseInfo.isBlocked" class="mb-1">
                Hệ thống <strong>đang tạm dừng gửi cảnh báo</strong> từ:
            </p>
            <ul v-if="pauseInfo.isBlocked" class="mb-2">
                <li>
                    Bắt đầu:
                    <strong>{{ formatDateTime(pauseInfo.startTime) }}</strong>
                </li>
                <li>
                    Kết thúc:
                    <strong>{{ formatDateTime(pauseInfo.endTime) }}</strong>
                </li>
            </ul>

            <p class="mb-2">
                Bạn có chắc chắn xác nhận
                <strong>tạm dừng nhận cảnh báo mới</strong>
                từ thiết bị
                <strong>
                    {{
                        currentPauseRow && currentPauseRow.deviceName
                            ? currentPauseRow.deviceName
                            : '[Thiết bị]'
                    }}
                </strong>
                - vùng/khu vực
                <strong>
                    {{
                        currentPauseRow && currentPauseRow.areaName
                            ? currentPauseRow.areaName
                            : '[Khu vực]'
                    }}
                </strong>
                <span v-if="pauseInfo.isBlocked">
                    (thời gian chặn sẽ được
                    <strong>kéo dài thêm</strong>
                    theo giá trị dưới đây).
                </span>
                <span v-else>
                    (thời gian chặn sẽ được tính từ
                    <strong> {{ formatDateTime(nowPauseBaseTime) }} </strong>).
                </span>
            </p>

            <b-form @submit.prevent="savePauseConfig">
                <b-form-group label="Tuỳ chọn thời gian">
                    <b-row>
                        <b-col cols="7">
                            <b-form-input
                                v-model.number="pauseForm.timeValue"
                                type="number"
                                min="1"
                            />
                        </b-col>
                        <b-col cols="5">
                            <b-form-select
                                v-model="pauseForm.timeUnit"
                                :options="timeUnitOptions"
                            />
                        </b-col>
                    </b-row>
                </b-form-group>

                <div class="d-flex justify-content-end">
                    <b-button class="mr-1" type="submit" variant="primary">
                        {{ $t('Button.Save') || 'Lưu' }}
                    </b-button>
                    <b-button variant="secondary" @click="modalPause = false">
                        {{ $t('Button.Cancel') || 'Hủy' }}
                    </b-button>
                </div>
            </b-form>
        </b-modal>
    </div>
</template>

<script>
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'
import { $themeConfig } from '@themeConfig'
import TreeHelper from '@/utils/treeHelper'
import moment from 'moment'

const isDevEnv = process.env.NODE_ENV === 'development'

export default {
    mixins: [authorizationMixin],
    components: {},
    setup() {
        const { apiURL } = $themeConfig.app
        return { apiURL }
    },
    data() {
        return {
            modalImgEventShow: false,
            modalCreateIssue: false,
            modalPause: false,
            dataCreateIssue: null,
            currentPauseRow: null,
            pauseInfo: {
                isBlocked: false,
                startTime: null,
                endTime: null,
            },
            pauseForm: {
                timeValue: 30,
                timeUnit: 'minute', // minute | hour
            },
            timeUnitOptions: [
                { value: 'minute', text: 'Phút' },
                { value: 'hour', text: 'Giờ' },
            ],
            nowPauseBaseTime: null,

            // cache trạng thái chặn theo từng (deviceId, eventTypeId)
            // key: `${deviceId}_${eventTypeId}`
            pauseStatusMap: {},

            srcImgEvent: '',
            baseURL: isDevEnv ? 'http://localhost:1938' : this.apiURL,
            searchForm: {
                checkAll: true,
                dateFrom: '',
                dateTo: '',
                read: '',
                areaId: null,
                compId: null,
                // filter chế độ gửi xuống BE: null = tất cả, true = Off, false = On
                isBlocked: null,
            },
            deviceName: null,
            columns: [
                {
                    label: 'Warning.List.Table.Title',
                    field: 'title',
                },
                {
                    label: 'Warning.List.Table.Content',
                    field: 'content',
                },
                {
                    label: 'Events.Table.Time',
                    field: 'accessDate',
                    formatFn(value) {
                        return moment.utc(value).format('DD/MM/YYYY HH:mm:ss')
                    },
                },
                {
                    label: 'Events.Table.Area',
                    field: 'areaName',
                },
                {
                    label: 'Warning.List.Table.Status',
                    field: 'readStr',
                },
                {
                    // Cột "Chế độ"
                    label: 'Chế độ',
                    field: 'mode',
                },
                {
                    label: 'Warning.List.Table.Operation',
                    field: 'action',
                },
            ],
            listStatus: [
                {
                    text: 'Warning.List.SearchForm.Read',
                    id: true,
                },
                {
                    text: 'Warning.List.SearchForm.NoRead',
                    id: false,
                },
            ],
            // Filter On / Off cho thanh tìm kiếm – FE chỉ gửi bool cho BE
            pauseFilterOptions: [
                { value: false, text: 'On' },
                { value: true, text: 'Off' },
            ],
            listArea: [],
            listDevice: [],
            listEventType: [],
            listEventFiles: [],
            individuallyPaused: new Set(),
            individualPauseLabels: {},
            individualPauseGroups: {},
            // Lưu vào localStorage
            PAUSE_INDIVIDUAL_KEY: 'warning_individual_paused_v1',
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
        this.loadArea()
        // khôi phục pause riêng khi reload
        this.restoreIndividualPauseFromStorage()
    },
    methods: {
        // Khôi phục từ localStorage
        restoreIndividualPauseFromStorage() {
            try {
                const saved = localStorage.getItem(this.PAUSE_INDIVIDUAL_KEY)
                if (saved) {
                    const data = JSON.parse(saved)
                    this.individuallyPaused = new Set(data.ids || [])
                    this.individualPauseLabels = data.labels || {}
                    this.individualPauseGroups = data.groups || {}
                }
            } catch (e) {
                console.warn('Lỗi load pause riêng từ localStorage', e)
            }
        },

        // Lưu vào localStorage mỗi khi bấm tạm dừng
        saveIndividualPauseToStorage() {
            const data = {
                ids: Array.from(this.individuallyPaused),
                labels: this.individualPauseLabels,
                groups: this.individualPauseGroups,
            }
            localStorage.setItem(this.PAUSE_INDIVIDUAL_KEY, JSON.stringify(data))
        },
        rechangeOptions(dataList) {
            return dataList.map((item) => ({
                ...item,
                text: this.$t(item.text),
            }))
        },
        createIssue(data) {
            const clone = JSON.parse(JSON.stringify(data))
            clone.description = data.content
            this.dataCreateIssue = clone
            this.modalCreateIssue = true
        },
        readNotification(id) {
            const row = this.$refs.eventTable.rows.find((x) => x.id === id)
            if (row) {
                row.read = true
                row.readStr = 'Đã xem'
            }
            this.$services.put(`/notification/read/${id}`)
        },
        search() {
            // mỗi lần search lại thì clear cache trạng thái chặn (để tính lại On/Off)
            this.pauseStatusMap = {}
            this.individuallyPaused = new Set()        // xóa pause riêng
            this.individualPauseLabels = {}              // xóa nhãn riêng
            this.$refs.eventTable.refresh()
        },
        // lookup
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                this.listEventType = response.data.data
            })
        },
        loadArea() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.listArea = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
        },
        loadDevice() {
            this.$services.get('/lookup/devices').then((response) => {
                this.listDevice = response.data.data
            })
        },
        loadEventFiles(eventId) {
            this.$services
                .get(`/event/eventFilesById/${eventId}`)
                .then((response) => {
                    this.listEventFiles = response.data
                })
        },
        showModalEvent(eventId) {
            this.loadEventFiles(eventId)
            this.modalImgEventShow = true
        },
        formatStatus(status) {
            return status === 'Đã xem'
                ? this.$t('Warning.List.SearchForm.Read')
                : this.$t('Warning.List.SearchForm.NoRead')
        },
        formatDateTime(value) {
            if (!value) return ''
            return moment(value).format('DD/MM/YYYY HH:mm:ss')
        },

        // ====== Modal tạm dừng nhận cảnh báo ======
        openPauseModal(row) {
            this.currentPauseRow = row
            this.pauseForm.timeValue = 30
            this.pauseForm.timeUnit = 'minute'
            this.pauseInfo = {
                isBlocked: false,
                startTime: null,
                endTime: null,
            }

            // thời điểm mở modal = "bây giờ"
            this.nowPauseBaseTime = new Date()

            const compId = this.searchForm.compId
            const deviceId = row.deviceId
            const eventTypeId = row.eventTypeId

            // lấy tên thiết bị như cũ...
            this.$services.get('/lookup/devices').then((response) => {
                let devices = TreeHelper.removeEmptyChildren(response.data.data)
                this.lstDevice = devices.filter((x) => x.id == deviceId)

                if (this.lstDevice.length > 0) {
                    this.deviceName = this.lstDevice[0].text
                    row.deviceName = this.deviceName
                }
            })

            const url = `/notification/device-alert-config/current?compId=${compId}&deviceId=${deviceId}&eventTypeId=${eventTypeId}`

            this.$services
                .get(url)
                .then((resp) => {
                    const data = resp.data.data
                    if (data && data.isBlocked) {
                        // ĐANG bị chặn -> dùng cấu hình đã lưu
                        this.pauseInfo.isBlocked = true
                        this.pauseInfo.startTime = data.startTime
                        this.pauseInfo.endTime = data.endTime
                    } else {
                        // KHÔNG bị chặn -> lấy thời điểm hiện tại (đã gán ở trên)
                        this.pauseInfo.isBlocked = false
                        this.pauseInfo.startTime = null
                        this.pauseInfo.endTime = null
                    }
                    this.modalPause = true
                })
                .catch(() => {
                    // lỗi -> coi như chưa bị chặn, dùng thời gian hiện tại
                    this.pauseInfo.isBlocked = false
                    this.pauseInfo.startTime = null
                    this.pauseInfo.endTime = null
                    this.modalPause = true
                })
            // Kiểm tra xem dòng này đã từng được pause riêng chưa
            if (this.individuallyPaused.has(row.id)) {
                this.pauseInfo.isBlocked = true
                // có thể set thời gian giả để hiển thị
            }
        },
        async savePauseConfig() {
            if (!this.currentPauseRow) return

            const value = Number(this.pauseForm.timeValue || 0)
            if (!value || value <= 0) {
                this.$bvToast.toast('Thời gian phải lớn hơn 0', {
                    variant: 'danger',
                    solid: true,
                })
                return
            }

            const payload = {
                deviceId: this.currentPauseRow.deviceId,
                eventTypeId: this.currentPauseRow.eventTypeId,
                compId: this.searchForm.compId,
                timeValue: value,
                timeUnit: this.pauseForm.timeUnit, // "minute" | "hour"
            }

            try {
                const resp = await this.$services.post(
                    '/notification/device-alert-config/pause',
                    payload
                )

                const data = resp.data.data
                let endTime = data?.endTime ? this.formatDateTime(data.endTime) : null

                // Tính nhãn hiển thị
                const unitText = this.pauseForm.timeUnit === 'minute' ? 'phút' : 'giờ'
                const label = `Off (${value} ${unitText})`

                // Đánh dấu: chỉ dòng này được pause riêng (dù backend pause chung)
                this.individuallyPaused.add(this.currentPauseRow.id)
                this.$set(this.individualPauseLabels, this.currentPauseRow.id, {
                    label,
                    until: endTime
                })

                // Gán thẳng vào row để Vue nhận diện thay đổi (nếu cần)
                this.$set(this.currentPauseRow, 'individualPaused', true)

                // LƯU VÀO LOCALSTORAGE ĐỂ RELOAD VẪN NHỚ!!!
                this.saveIndividualPauseToStorage()

                this.$bvToast.toast(
                    endTime
                        ? `Đã tạm dừng riêng cảnh báo này đến ${endTime}`
                        : 'Đã tạm dừng riêng cảnh báo này',
                    { variant: 'success', solid: true }
                )

                this.modalPause = false

                // Refresh bảng để các dòng khác không bị "nhiễm" trạng thái Off chung
                this.$refs.eventTable.refresh()

            } catch (e) {
                this.$bvToast.toast('Lưu cấu hình thất bại', {
                    variant: 'danger',
                    solid: true,
                })
            }
        },

        // ====== HỖ TRỢ CỘT "CHẾ ĐỘ" ======
        getPauseLabel(row) {
            if (this.individuallyPaused.has(row.id)) {
                const info = this.individualPauseLabels[row.id]
                return info?.label || 'Off'
            }

            // Nếu chưa pause riêng → vẫn dùng logic cũ (pause chung theo device + eventType)
            const compId = this.searchForm.compId
            if (!compId || !row || !row.deviceId || !row.eventTypeId) return 'On'

            const key = `${row.deviceId}_${row.eventTypeId}`
            const status = this.pauseStatusMap[key]

            if (!status) {
                this.fetchPauseStatus(row, key)
                return '...'
            }

            if (status.loading) return '...'

            return status.label || 'On'
        },

        fetchPauseStatus(row, key) {
            const compId = this.searchForm.compId
            if (!compId) return

            if (this.pauseStatusMap[key]?.loading) return

            // set loading
            this.$set(this.pauseStatusMap, key, {
                loading: true,
                label: '...',
            })

            const url = `/notification/device-alert-config/current?compId=${compId}&deviceId=${row.deviceId}&eventTypeId=${row.eventTypeId}`

            this.$services
                .get(url)
                .then((resp) => {
                    const data = resp.data.data

                    let label = 'On'

                    if (
                        data &&
                        data.isBlocked &&
                        data.startTime &&
                        data.endTime
                    ) {
                        const start = moment(data.startTime)
                        const end = moment(data.endTime)
                        const totalMinutes = end.diff(start, 'minutes')

                        if (totalMinutes > 0) {
                            if (totalMinutes % 60 === 0) {
                                const hours = totalMinutes / 60
                                label = `Off (${hours} giờ)`
                            } else {
                                label = `Off (${totalMinutes} phút)`
                            }
                        }
                    }

                    this.$set(this.pauseStatusMap, key, {
                        loading: false,
                        label,
                    })
                })
                .catch(() => {
                    // lỗi -> coi như On
                    this.$set(this.pauseStatusMap, key, {
                        loading: false,
                        label: 'On',
                    })
                })
        },
    },
}
</script>

<style lang="scss">
/* header modal tạm dừng cảnh báo */
.modal-header.pause-modal-header {
    background-color: #0050a8 !important; /* xanh đậm */
    color: #fff !important;
}

/* tiêu đề modal */
.modal-header.pause-modal-header .modal-title {
    color: #fff !important;
    font-weight: 600;
}

/* nút X */
.modal-header.pause-modal-header .close {
    color: #fff !important;
    opacity: 1;
}
</style>
