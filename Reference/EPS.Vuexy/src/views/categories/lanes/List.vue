<template>
    <div>
        <!-- FILTERS -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
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
                        ></form-search>
                    </b-row>

                    <!-- Ẩn nút submit để Enter có tác dụng -->
                    <button type="submit" class="sr-only">
                        {{ $t('Common.Search') }}
                    </button>
                </b-form>
            </b-card-body>
        </b-card>

        <!-- ACTIONS + TABLE -->
        <b-card>
            <div class="mb-2">
                <b-button
                    v-if="authorize([authorizeName.manage])"
                    variant="primary"
                    :to="{ path: routerLink.create }"
                >
                    {{ $t('Button.Create') }}
                </b-button>
            </div>

            <BasicTable
                ref="laneTable"
                :columns="tableColumns"
                :data-url="table.dataUrl"
                :search-form="searchForm"
                sort-by="createdAt"
                :storage-name="'laneTable'"
            >
                <template v-slot:table-row="{ column, row }">
                    <!-- STATUS badge -->
                    <span v-if="column.field === 'status'">
                        <b-badge
                            :variant="
                                row.status === 1 || row.Status === 1
                                    ? 'success'
                                    : 'secondary'
                            "
                        >
                            {{
                                row.status === 1 || row.Status === 1
                                    ? $t('Common.Active')
                                    : $t('Common.Inactive')
                            }}
                        </b-badge>
                    </span>

                    <!-- ACTIONS -->
                    <span v-else-if="column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize([authorizeName.view])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path:
                                        routerLink.detail + (row.id || row.Id),
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>

                            <!-- <b-button
                                v-if="authorize([authorizeName.manage])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Edit')"
                                :to="{
                                    path:
                                        routerLink.update + (row.id || row.Id),
                                }"
                            >
                                <Icon
                                    icon="mdi:pencil-outline"
                                    class="xs-icon"
                                />
                            </b-button> -->

                            <b-button
                                v-if="authorize([authorizeName.manage])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Delete')"
                                @click="doDelete(row.id || row.Id)"
                            >
                                <Icon
                                    icon="fluent:delete-20-regular"
                                    class="xs-icon"
                                />
                            </b-button>
                        </div>
                    </span>
                    <!-- các cột khác để BasicTable tự render -->
                </template>
            </BasicTable>
        </b-card>
    </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'

export default {
    name: 'LaneList',
    mixins: [authorizationMixin],
    data() {
        return {
            // payload gửi lên API
            searchForm: {
                filterText: null, // Mã/Tên làn
                filterFunction: null, // 1/2/3
                filterAreaId: null, // Id khu vực
                filterStatus: null, // 1/0
            },

            // UI filter
            searchDatas: [
                {
                    filterName: 'filterText',
                    label: 'Lanes.List.Filters.Keyword',
                    // placeholder: 'Lanes.List.Filters.KeywordPH',
                    options: null,
                    autoSearch: false,
                    md: 6,
                    labelColsMd: 3,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterFunction',
                    label: 'Lanes.List.Filters.Function',
                    // placeholder: 'Common.All',
                    options: [
                        // { id: null, text: this.$t('Common.All') },
                        {
                            id: 1,
                            text: 'Lanes.Enums.Function.Monitor',
                        },
                        { id: 2, text: 'Lanes.Enums.Function.Charge' },
                        { id: 3, text: 'Lanes.Enums.Function.Toll' },
                    ],
                    autoSearch: true,
                    md: 6,
                    labelColsMd: 3,
                },
                {
                    searchType: 'tree-select',
                    filterName: 'filterAreaId',
                    label: 'Lanes.List.Filters.Area',
                    // placeholder: 'Common.All',
                    options: [],
                    autoSearch: true,
                    md: 6,
                    labelColsMd: 3,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterStatus',
                    label: 'Lanes.List.Filters.Status',
                    // placeholder: 'Common.All',
                    options: [
                        // { id: null, text: this.$t('Common.All') },
                        { id: 1, text: 'Common.Active' },
                        { id: 0, text: 'Common.Inactive' },
                    ],
                    autoSearch: true,
                    md: 6,
                    labelColsMd: 3,
                },
            ],

            // bảng
            table: { dataUrl: '/lanes' },
            tableColumns: [
                {
                    label: 'Lanes.List.Columns.Code',
                    field: 'laneCode',
                },
                {
                    label: 'Lanes.List.Columns.Name',
                    field: 'laneName',
                },
                {
                    label: 'Lanes.List.Columns.Area',
                    field: 'areaName',
                },
                {
                    label: 'Lanes.List.Columns.Status',
                    field: 'status',
                },
                { label: 'Common.Actions', field: 'action' },
            ],
            // quyền & route
            authorizeName: { manage: 'ManageLane', view: 'ViewLane' },
            routerLink: {
                create: '/categories/lane/create/',
                update: '/categories/lane/update/',
                detail: '/categories/lane/detail/',
                delete: '/lanes/',
            },
        }
    },
    // computed: {
    //     tableColumns() {
    //         // Ẩn cột Function nếu backend chưa trả field này trong list
    //         return [
    //             {
    //                 label: this.$t('Lanes.List.Columns.Code'),
    //                 field: 'laneCode',
    //             },
    //             {
    //                 label: this.$t('Lanes.List.Columns.Name'),
    //                 field: 'laneName',
    //             },
    //             {
    //                 label: this.$t('Lanes.List.Columns.Area'),
    //                 field: 'areaName',
    //             },
    //             {
    //                 label: this.$t('Lanes.List.Columns.Status'),
    //                 field: 'status',
    //             },
    //             { label: this.$t('Common.Actions'), field: 'action' },
    //         ]
    //     },
    // },
    created() {
        this.lookupData()
    },
    methods: {
        // MAP response -> { items, total } cho BasicTable
        lanesAdapter(raw) {
            // raw có thể là axios response hoặc payload
            const outer = raw && raw.data ? raw.data : raw
            return {
                items: outer && outer.data ? outer.data : [],
                total:
                    (outer &&
                        (outer.totalRows != null
                            ? outer.totalRows
                            : outer.total)) ||
                    0,
            }
        },

        handleBinding(event) {
            const k = event.filterName
            if (
                ['filterFunction', 'filterAreaId', 'filterStatus'].includes(k)
            ) {
                const v = event.value
                this.searchForm[k] =
                    v === '' || v === null || v === undefined ? null : Number(v)
            } else {
                this.searchForm[k] = event.value
            }
            if (event.autoSearch) this.search()
        },

        search() {
            this.$refs.laneTable &&
                this.$refs.laneTable.refresh &&
                this.$refs.laneTable.refresh()
        },

        // chuẩn hoá nodes cho tree-select
        normalizeAreaNodes(list) {
            if (!Array.isArray(list)) return []
            return list.map((n) => ({
                id: n.id != null ? n.id : n.Id != null ? n.Id : n.value,
                label:
                    n.label != null
                        ? n.label
                        : n.name != null
                          ? n.name
                          : n.Label,
                children: this.normalizeAreaNodes(
                    n.children || n.Children || []
                ),
            }))
        },

        // nạp khu vực
        lookupData() {
            this.$services
                .get('/lookup/areas-tree')
                .then((response) => {
                    const raw =
                        response &&
                        response.data &&
                        Array.isArray(response.data.data)
                            ? response.data.data
                            : Array.isArray(response?.data)
                              ? response.data
                              : []
                    const options = this.normalizeAreaNodes(raw)
                    const idx = this.searchDatas.findIndex(
                        (x) => x.filterName === 'filterAreaId'
                    )
                    if (idx !== -1)
                        this.$set(this.searchDatas[idx], 'options', options)
                })
                .catch(() => {
                    const idx = this.searchDatas.findIndex(
                        (x) => x.filterName === 'filterAreaId'
                    )
                    if (idx !== -1)
                        this.$set(this.searchDatas[idx], 'options', [])
                })
        },

        doDelete(id) {
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
                        .delete(this.routerLink.delete + id)
                        .then(() => {
                            this.search()
                            this.$toast({
                                title: this.$t('Success.Delete'),
                                icon: 'CheckIcon',
                                variant: 'success',
                            })
                        })
                        .catch((error) => {
                            this.$toast({
                                title: this.$t('Error.Error'),
                                text: this.$t(
                                    String((error && error.message) || '')
                                ),
                                icon: 'AlertTriangleIcon',
                                variant: 'danger',
                            })
                        })
                }
            })
        },
    },
}
</script>

<style scoped>
.sr-only {
    position: absolute !important;
    width: 1px !important;
    height: 1px !important;
    padding: 0 !important;
    margin: -1px !important;
    overflow: hidden !important;
    clip: rect(0, 0, 0, 0) !important;
    border: 0 !important;
}
</style>
