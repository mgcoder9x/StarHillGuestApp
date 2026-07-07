<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <!-- advance search input -->
                    <b-row>
                        <form-search
                            v-for="searchData in searchDatas"
                            :key="searchData.filterName"
                            :search-type="searchData.searchType"
                            :filter-name="searchData.filterName"
                            :label="searchData.label"
                            :placeholder="searchData.placeholder"
                            :md="searchData.md"
                            :label-cols-md="searchData.labelColsMd"
                            :options="searchData.options"
                            :auto-search="searchData.autoSearch"
                            :model-value="searchData.modelValue"
                            @handle-binding="handleBinding"
                        />
                    </b-row>

                    <!-- Face search toggle -->
                    <b-row class="align-items-center mt-1">
                        <b-col md="4">
                            <b-form-group
                                :label="$t('categories.employees.common.form.label.findFace')"
                                label-for="h-face-toggle"
                                label-cols-md="4"
                                class="mb-0 d-flex align-items-center"
                            >
                                <b-form-checkbox
                                    id="h-face-toggle"
                                    v-model="showFaceSearch"
                                    switch
                                    size="sm"
                                    class="mb-0"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <!-- Face search advanced -->
                    <b-collapse v-model="showFaceSearch">
                        <b-row no-gutters class="align-items-stretch">
                            <!-- ẢNH -->
                            <b-col md="4" class="pr-md-50 mb-75">
                                <div class="face-dropzone">
                                    <div class="dropzone-inner">
                                        <b-img
                                            v-if="faceImagePreview"
                                            :src="faceImagePreview"
                                            class="preview-img shadow-sm"
                                            alt="Ảnh khuôn mặt"
                                            fluid
                                        />
                                        <div v-else class="preview-placeholder">
                                            <Icon
                                                icon="mdi:account-circle"
                                                class="avatar-xxl"
                                            />
                                            <small class="text-muted mt-25">
                                                {{$t('')}}
                                            </small>
                                        </div>
                                    </div>

                                    <b-form-file
                                        v-model="faceFile"
                                        accept="image/*"
                                        size="sm"
                                        browse-text="Browse"
                                        drop-placeholder="Kéo file vào đây"
                                        @change="handleFileUpload"
                                        class="mt-50"
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
                                        <span class="slider-label mb-0"
                                            >{{$t('categories.employees.common.form.label.pointIdentify')}}</span
                                        >
                                        <span class="slider-value"
                                            >{{ identifyPercent }}%</span
                                        >
                                    </div>
                                    <input
                                        type="range"
                                        min="0"
                                        max="1"
                                        step="0.01"
                                        v-model.number="identifyScore"
                                        class="range"
                                    />
                                    <div
                                        class="d-flex justify-content-between mt-25 text-muted xsmall"
                                    >
                                        <span>0%</span><span>50%</span
                                        ><span>100%</span>
                                    </div>
                                    <small class="text-muted d-block mt-25">
                                        {{$t('categories.employees.common.form.label.filterResult')}} ≥
                                        {{ identifyPercent }}%.
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

        <b-card title="">
            <div>
                <b-button
                    v-if="authorize([authorizeName.manage])"
                    variant="primary"
                    :to="{ path: routerLink.create }"
                    style="margin-bottom: 15px"
                >
                    {{ $t('Button.Create') }}
                </b-button>
                <b-button
                    v-if="authorize([authorizeName.manage])"
                    variant="primary"
                    :to="{ path: routerLink.import }"
                    style="margin-bottom: 15px; margin-left: 10px"
                >
                    {{ $t('Button.ImportExcel') }}
                </b-button>
            </div>

            <!-- table -->
            <BasicTable
                ref="groupTable"
                :columns="tableColumns"
                :data-url="table.dataUrl"
                :search-form="searchForm"
                sortBy="creationTime"
                :storage-name="'employeeTable'"
            >
                <template v-slot:table-row="{ column, row }">
                    <!-- Column: Action -->
                    <span v-if="column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize([authorizeName.view])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                :to="{ path: routerLink.detail + row.id }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize([authorizeName.manage])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Delete')"
                                @click="doDelete(row.id)"
                            >
                                <Icon
                                    icon="fluent:delete-20-regular"
                                    class="xs-icon"
                                />
                            </b-button>
                        </div>
                    </span>

                    <!-- Column: Avatar -->
                    <span v-else-if="column.field === 'avartarPath'">
                        <div class="text-nowrap">
                            <b-img
                                v-if="row.avartarPath"
                                :src="`${baseURL}/Employees/${row.avartarPath}`"
                                height="100"
                                width="100"
                                class="avatar-crop mr-1"
                                @click="
                                    openModal(
                                        `${baseURL}/Employees/${row.avartarPath}`
                                    )
                                "
                            />
                        </div>
                    </span>
                </template>
            </BasicTable>

            <!-- Modal xem ảnh -->
            <b-modal
                v-model="isModalOpen"
                title="Ảnh nhân viên"
                ok-title="Đóng"
                hide-header-close
                ok-only
                size="lg"
                centered
            >
                <div class="d-flex justify-content-center">
                    <img
                        :src="modalImageUrl"
                        alt="Full Image"
                        loading="lazy"
                        style="
                            height: 60vh;
                            object-fit: contain;
                            object-position: center;
                        "
                    />
                </div>
            </b-modal>
        </b-card>
    </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

// Đổi ở đây nếu route backend khác
const FACE_PERSON_API = '/employees/face-person-search'

export default {
    name: 'EmployeeListWithFaceSearch',
    components: { ToastificationContent },
    mixins: [authorizationMixin],
    data() {
        return {
            // --- SEARCH FORM GỬI LÊN API ---
            searchForm: {
                filterCompId: null,
                filterNameCode: null,
                filterDepId: null,
                filterGender: null,
                filterCitizenId: null,
                filterStatus: null,
                PersonTypeStr: 1,
                filterHasAvatar: null, // 1=có, 0=không
                filterEmployeeIdStr: '', // danh sách id sau khi face-search
            },

            // --- FILTER UI DATA (reactive, không index cứng) ---
            searchDatas: [
                {
                    searchType: 'tree-select',
                    filterName: 'filterCompId',
                    modelValue: null,
                    label: 'categories.employees.list.searchForm.label.compName',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.compName',
                    options: [],
                    autoSearch: true,
                    md: 4,
                    labelColsMd: 4,
                },
                {
                    filterName: 'filterNameCode',
                    label: 'categories.employees.list.searchForm.label.nameCode',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.nameCode',
                    options: null,
                    autoSearch: false,
                    md: 4,
                    labelColsMd: 4,
                },
                {
                    searchType: 'tree-select',
                    filterName: 'filterDepId',
                    label: 'categories.employees.list.searchForm.label.department',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.department',
                    options: [],
                    autoSearch: true,
                    md: 4,
                    labelColsMd: 4,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterGender',
                    label: 'categories.employees.list.searchForm.label.gender',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.gender',
                    options: [
                        {
                            id: 0,
                            text: this.$t('categories.employees.gender.female'),
                        },
                        {
                            id: 1,
                            text: this.$t('categories.employees.gender.male'),
                        },
                    ],
                    autoSearch: true,
                    md: 4,
                    labelColsMd: 4,
                },
                {
                    filterName: 'filterCitizenId',
                    label: 'categories.employees.list.searchForm.label.citizenId',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.citizenId',
                    options: null,
                    autoSearch: false,
                    md: 4,
                    labelColsMd: 4,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterStatus',
                    modelValue: 1,
                    label: 'categories.employees.list.searchForm.label.status',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.status',
                    options: [
                        { id: 1, text: 'StatusList.Active' },
                        { id: 2, text: 'StatusList.Inactive' },
                    ],
                    autoSearch: true,
                    md: 4,
                    labelColsMd: 4,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterHasAvatar',
                    label: 'categories.employees.list.searchForm.label.imageStatus',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.imageStatus',
                    options: [
                        {
                            id: 1,
                            text: this.$t(
                                'categories.employees.common.imageStatus.has'
                            ),
                        },
                        {
                            id: 0,
                            text: this.$t(
                                'categories.employees.common.imageStatus.none'
                            ),
                        },
                    ],
                    autoSearch: true,
                    md: 4,
                    labelColsMd: 4,
                },
            ],

            // --- BẢNG ---
            table: {
                dataUrl: '/employees',
            },

            // --- AUTH ---
            authorizeName: { manage: 'ManageEmployee', view: 'ViewEmployee' },

            // --- ROUTER ---
            routerLink: {
                create: '/categories/employees/create/',
                detail: '/categories/employees/detail/',
                import: '/categories/employees/import/',
                delete: '/employees/',
            },

            // Modal
            isModalOpen: false,
            modalImageUrl: null,

            // --- FACE SEARCH STATE ---
            showFaceSearch: false,
            faceFile: null,
            faceImagePreview: '',
            identifyScore: 0.85,
            debounceTimer: null,
        }
    },
    computed: {
        baseURL() {
            return process.env.VUE_APP_BASE_URL
        },
        identifyPercent() {
            return Math.round((this.identifyScore || 0) * 100)
        },
        // Columns để dùng được this.$t an toàn
        tableColumns() {
            return [
                {
                    label: 
                        'categories.employees.list.table.label.compName'
                    ,
                    field: 'compName',
                },
                {
                    label: 
                        'categories.employees.list.table.label.code'
                    ,
                    field: 'code',
                },
                {
                    label:
                        'categories.employees.list.table.label.name'
                    ,
                    field: 'fullname',
                },
                {
                    label:
                        'categories.employees.list.table.label.citizenId'
                    ,
                    field: 'citizenId',
                },
                {
                    label:
                        'categories.employees.list.table.label.departmentName'
                    ,
                    field: 'depName',
                },
                {
                    label:
                        'categories.employees.list.table.label.gender'
                    ,
                    field: 'gender',
                    formatFn: (v) =>
                        v === 0
                            ? this.$t('categories.employees.gender.female')
                            : v === 1
                              ? this.$t('categories.employees.gender.male')
                              : '',
                },
                {
                    label:
                        'categories.employees.list.table.label.status'
                    ,
                    field: 'status',
                    formatFn: (v) =>
                        v === 1
                            ? this.$t('StatusList.Active')
                            : v === 2
                              ? this.$t('StatusList.Inactive')
                              : '',
                },
                {
                    label:
                        'categories.employees.list.table.label.image'
                    ,
                    field: 'avartarPath',
                },
                {
                    label:
                        'categories.employees.list.table.label.action'
                    ,
                    field: 'action',
                },
            ]
        },
    },
    watch: {
        // Kéo slider -> debounce search và update CSS
        identifyScore() {
            if (!this.showFaceSearch || !this.faceImagePreview) return
            clearTimeout(this.debounceTimer)
            this.searchForm.filterEmployeeIdStr = ''
            this.debounceTimer = setTimeout(() => this.search(), 250)
            
            // Cập nhật giá trị CSS để hiển thị thanh màu
            this.$nextTick(() => {
                const sliderElement = document.querySelector('input[type="range"].range')
                if (sliderElement) {
                    sliderElement.style.setProperty('--value', `${this.identifyScore * 100}%`)
                }
            })
        },
        // Tắt tính năng -> reset
        showFaceSearch(val) {
            if (!val) {
                this.faceFile = null
                this.faceImagePreview = ''
                this.searchForm.filterEmployeeIdStr = ''
                this.identifyScore = 0.85
                this.$nextTick(() => this.search())
            }
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken?.companyId || null
        this.searchForm.filterCompId = this.searchForm.compId

        // Đặt sẵn compId vào searchDatas.filterCompId.modelValue nếu có
        const compIdx = this.searchDatas.findIndex(
            (x) => x.filterName === 'filterCompId'
        )
        if (compIdx !== -1)
            this.searchDatas[compIdx].modelValue = this.searchForm.compId

        this.lookupData()
    },
    
    mounted() {
        // Cập nhật giá trị CSS ban đầu cho thanh trượt
        this.updateRangeBackground()
    },
    
    updated() {
        // Cập nhật giá trị CSS khi component cập nhật
        this.updateRangeBackground()
    },
    methods: {
        openModal(url) {
            this.modalImageUrl = url
            this.isModalOpen = true
        },

        handleBinding(event) {
            // Ép kiểu nhẹ cho các field select
            if (
                [
                    'filterCompId',
                    'filterDepId',
                    'filterGender',
                    'filterStatus',
                    'filterHasAvatar',
                ].includes(event.filterName)
            ) {
                const v = event.value
                this.searchForm[event.filterName] =
                    v === '' || v === null || v === undefined ? null : Number(v)
            } else {
                this.searchForm[event.filterName] = event.value
            }
            if (event.autoSearch) this.search()
        },

        // Gọi đúng API tìm Person theo ảnh, map về filterEmployeeIdStr
        async ensureFaceEmployeeIds() {
            // Chỉ gọi khi có ảnh & bật tìm khuôn mặt
            if (!this.showFaceSearch || !this.faceImagePreview) return true
            if (this.searchForm.filterEmployeeIdStr) return true

            try {
                // Map filterCompId -> comIds[]
                const comIds = this.searchForm.filterCompId
                    ? [Number(this.searchForm.filterCompId)]
                    : []

                const res = await this.$services.post(FACE_PERSON_API, {
                    faceImageBase64: this.faceImagePreview, // dataURL → backend sẽ strip prefix
                    identifyScore: this.identifyScore ?? 0.8, // threshold
                    quality: 0.2, // có thể cho user chỉnh sau
                    numResult: 500, // lấy nhiều để lọc ngoài
                    comIds, // lọc theo công ty
                })

                // Response: { status, errorCode, message, data: { personIds, matches } }
                const personIds = res?.data?.data?.personIds || []

                const MAX = 5000
                this.searchForm.filterEmployeeIdStr =
                    personIds.length === 0
                        ? '00000000-0000-0000-0000-000000000000' // để trả về rỗng an toàn
                        : personIds.slice(0, MAX).join(',')

                // (optional) toast best-match
                const top = res?.data?.data?.matches?.[0]
                if (top) {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: 'Face match',
                            text: `Top: ${top.fullname || top.personCode} (${Math.round((top.score || 0) * 100)}%)`,
                            icon: 'UserIcon',
                            variant: 'info',
                        },
                    })
                }

                return true
            } catch (e) {
                console.error(e)
                let mes = this.$i18n.locale === 'vi' ? 'Tìm kiếm khuôn mặt lỗi!' : 'Face search error!'
                this.$bvToast.toast(mes, {
                    title: this.$t('Error.Error') || 'Lỗi',
                    variant: 'danger',
                    solid: true,
                })
                this.searchForm.filterEmployeeIdStr =
                    '00000000-0000-0000-0000-000000000000'
                return true
            }
        },

        async search() {
            await this.ensureFaceEmployeeIds()
            this.$refs.groupTable &&
                this.$refs.groupTable.refresh &&
                this.$refs.groupTable.refresh()
        },

        doDelete(item) {
            this.$swal({
                title: this.$t('Message.MessageDelete1'),
                text: this.$t('Message.MessageDelete2'),
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: this.$t('Message.Agree'),
                cancelButtonText: this.$t('Message.Exit'),
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-outline-danger ml-1',
                },
                buttonsStyling: false,
            }).then((result) => {
                if (result.value) {
                    this.$services
                        .delete(this.routerLink.delete + item)
                        .then(() => {
                            this.$refs.groupTable.refresh()
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Delete'),
                                    text: '',
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    text: this.$t(`${error.message}`),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                },
                            })
                        })
                }
            })
        },

        // lookup data (không index cứng, dùng findIndex theo filterName)
        lookupData() {
            // company-tree
            this.$services
                .get('/lookup/company-tree')
                .then((response) => {
                    const idx = this.searchDatas.findIndex(
                        (x) => x.filterName === 'filterCompId'
                    )
                    if (idx !== -1)
                        this.$set(
                            this.searchDatas[idx],
                            'options',
                            response?.data || []
                        )
                })
                .catch(() => {
                    const idx = this.searchDatas.findIndex(
                        (x) => x.filterName === 'filterCompId'
                    )
                    if (idx !== -1)
                        this.$set(this.searchDatas[idx], 'options', [])
                })

            // departments-tree
            this.$services
                .get('/lookup/departments-tree?type=1')
                .then((response) => {
                    const options = response?.data?.data || []
                    const idx = this.searchDatas.findIndex(
                        (x) => x.filterName === 'filterDepId'
                    )
                    if (idx !== -1)
                        this.$set(this.searchDatas[idx], 'options', options)
                })
                .catch(() => {
                    const idx = this.searchDatas.findIndex(
                        (x) => x.filterName === 'filterDepId'
                    )
                    if (idx !== -1)
                        this.$set(this.searchDatas[idx], 'options', [])
                })
        },

        handleFileUpload(e) {
            const file = e?.target?.files?.[0] || this.faceFile
            this.faceImagePreview = ''
            this.searchForm.filterEmployeeIdStr = ''

            if (file) {
                const reader = new FileReader()
                reader.onload = (evt) => {
                    this.faceImagePreview = String(evt.target.result) // dataURL
                    // Gọi search ngay – ensureFaceEmployeeIds sẽ POST trước rồi refresh
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
        
        // Cập nhật background của thanh trượt để hiển thị phần đã chọn
        updateRangeBackground() {
            this.$nextTick(() => {
                const sliderElement = document.querySelector('input[type="range"].range')
                if (sliderElement) {
                    sliderElement.style.setProperty('--value', `${this.identifyScore * 100}%`)
                }
            })
        },
    },
}
</script>

<style lang="scss" scoped>
.avatar-crop {
    object-fit: cover;
    object-position: center;
    width: 8rem;
    height: 9rem;
    cursor: pointer;
}

/* Face UI */
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
    appearance: none;
    width: 100%;
    height: 4px;
    border-radius: 999px;
    background: #e2e8f0; /* Phần chưa chọn màu xám nhạt */
    outline: none;
    position: relative;
    margin: 8px 0; /* Thêm khoảng cách trên dưới để nút kéo không bị cắt */
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
    margin-top: -6px; /* Điều chỉnh để đảm bảo nút kéo nằm chính giữa thanh trượt */
}
.range::-moz-range-thumb {
    width: 16px;
    height: 16px;
    border-radius: 50%;
    background: #fff;
    border: 3px solid #1677ff;
    box-shadow: 0 2px 6px rgba(22, 119, 255, 0.25);
    cursor: pointer;
    margin-top: -6px; /* Điều chỉnh để đảm bảo nút kéo nằm chính giữa thanh trượt */
}
.range::-moz-range-track {
    height: 4px;
    border-radius: 999px;
    background: #e2e8f0; /* Phần chưa chọn màu xám nhạt */
}
/* Tạo phần màu xanh cho đoạn đã chọn */
.range::-webkit-slider-runnable-track {
    background: linear-gradient(to right, #1677ff 0%, #1677ff var(--value, 0%), #e2e8f0 var(--value, 0%));
    height: 4px;
    border-radius: 999px;
    position: relative;
}
.range::-moz-range-progress {
    background-color: #1677ff;
    height: 4px;
    border-radius: 999px 0 0 999px;
}
.xsmall {
    font-size: 11px;
}
.mb-25 {
    margin-bottom: 0.25rem !important;
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
</style>
