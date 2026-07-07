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
                    <b-button
                        type="submit"
                        style="
                            position: absolute;
                            width: 1px;
                            height: 1px;
                            overflow: hidden;
                            clip: rect(0, 0, 0, 0);
                        "
                        >{{ this.$t('common.button.search') }}</b-button
                    >
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
                    {{ this.$t('Button.Create') }}
                </b-button>
            
            </div>
            <!-- table -->
            <BasicTable
                ref="groupTable"
                :columns="table.columns"
                :data-url="table.dataUrl"
                :search-form="searchForm"
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
                                :to="{
                                    path: routerLink.detail + row.id,
                                }"
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
                    <span v-else-if="column.field === 'avartarPath'">
                        <div class="text-nowrap" style="text-align: center;">
                            <b-img
                                v-if="row.avartarPath"
                                :src="`${imgUrl}/${row.avartarPath}`"
                                height="100"
                                width="100"
                                class="avatar-crop mr-1"
                                @click="
                                    openModal(`${imgUrl}/${row.avartarPath}`)
                                "
                            ></b-img>
                        </div>
                    </span>
                </template>
            </BasicTable>
            <!-- Modal -->
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
                        loading="lazy"
                        alt="Full Image"
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
import getBaseUrl from '@/utils/get-baseUrl'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    components: { ToastificationContent },
    mixins: [authorizationMixin],
    data() {
        return {
            searchDatas: [
            
                {
                    filterName: 'filterNameCode',
                    label: 'study.student.list.searchForm.label.nameCode',
                    placeholder:'study.student.list.searchForm.placeholder.nameCode',
                    autoSearch: true,

                },
               
                {
                    searchType: 'v-select',
                    filterName: 'filterGender',
                    label: 'study.student.list.searchForm.label.gender',
                    placeholder:
                        'study.student.list.searchForm.placeholder.gender',
                    options: [
                        { id: 0, text: 'Nữ' },
                        { id: 1, text: 'Nam' },
                    ],
                    autoSearch: true,
                },
                {
                    filterName: 'filterCitizenId',
                    label: 'study.student.list.searchForm.label.citizenId',
                    placeholder:
                        'study.student.list.searchForm.placeholder.citizenId',
                    autoSearch: true,

                },
                {
                    searchType: 'v-select',
                    filterName: 'status',
                    label: 'categories.employees.list.searchForm.label.status',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.status',
                    options: [
                        { id: 1, text: 'Hoạt động' },
                        { id: 2, text: 'Ngừng hoạt động' },
                    ],
                    autoSearch: true,
                },
                
            ],
            searchForm: {
                filterNameCode: null,
                filterGender: null,
                filterCitizenId: null,
            },
            table: {
                dataUrl: '/stdUser',
                columns: [
                   
                    {
                        label: this.$t(
                            'study.student.list.table.label.code'
                        ),
                        field: 'code',
                    },
                    {
                        label: this.$t('Họ tên'),
                        field: 'fullName',
                    },
                    {
                        label: this.$t(
                            'study.student.list.table.label.citizenId'
                        ),
                        field: 'identityCard',
                    },
                    {
                        label: this.$t(
                            'study.student.list.table.label.gender'
                        ),
                        field: 'gender',
                        formatFn: (value) => {
                            switch (value) {
                                case 0:
                                    return 'Nữ'
                                case 1:
                                    return 'Nam'
                                default:
                                    return ''
                            }
                        },
                    },
                    {
                        label: "Trạng thái",
                        field: 'status',
                        formatFn: (value) => {
                            switch (value) {
                                case 1:
                                    return 'Hoạt động'
                                case 2:
                                    return 'Ngưng hoạt động'
                                default:
                                    return ''
                            }
                        },
                    },
                    {
                        label: 'Lớp học',
                        field: 'className',
                    },
                    {
                        label: this.$t(
                            'study.student.list.table.label.image'
                        ),
                        field: 'avartarPath',
                    },
                    {
                        label: this.$t(
                            'study.student.list.table.label.action'
                        ),
                        field: 'action',
                    },
                ],
            },
            authorizeName: {
                manage: 'ManageEmployee',
                view: 'ViewEmployee',
            },
            routerLink: {
                create: '/study/student/create/',
                detail: '/study/student/detail/',
                import: '/study/student/import/',
                delete: '/stdUser/',
            },
            isModalOpen: false,
            modalImageUrl: null,
        }
    },
    computed: {
        imgUrl() {
              const { VUE_APP_BASE_URL: baseURL } = process.env
            return `${getBaseUrl()}/STDUserss`
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        this.lookupData()
    },
    methods: {
        openModal(url) {
            this.modalImageUrl = url
            this.isModalOpen = true
        },
        handleBinding(event) {
            this.searchForm[event.filterName] = event.value
            if (event.autoSearch) this.search()
        },
        search() {
            this.$refs.groupTable.refresh()
        },
        doDelete(item) {
            // confirm text
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
                }
            })
        },

        // lookup data
        lookupData() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.searchDatas[0].options = response.data
            })
            this.$services
                .get('/lookup/departments-tree?type=1')
                .then((response) => {
                    this.searchDatas[2].options = response.data.data
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
</style>
