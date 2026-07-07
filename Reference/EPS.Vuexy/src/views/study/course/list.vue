<template>
    <div>
        <b-card>
            <b-card-body>
                <b-form>
                           <b-row>
                        <FormSearch
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
                            :multiple="searchData.multiple"
                            :locale="currentLocale"
                            :date-format-options="searchData.dateFormatOptions"
                            :value-consists-of="searchData.valueConsistsOf"
                            :model-value="searchData.modelValue"
                            @handle-binding="handleBinding"
                        />
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>

        <!-- Table -->
        <b-card title="">
            <button
                class="btn btn-primary mb-2"
                v-if="authorize(['ManageSTDCourse'])"
                @click="$router.push({ name: 'study-course-create' })"
            >
                 {{ this.$t('Button.Create') }}
            </button>
            <!-- table -->
            <BasicTable
                ref="eventWaterTable"
                :columns="Columns"
                data-url="/stdCourses"
                :search-form="searchForm"
                :sort-by="'id'"
            >
                <template slot="table-row" slot-scope="props">
                    <span
                        v-if="props.column.field == 'title'"
                        :style="ColumnsStyle"
                    >
                        {{ $t(props.row.title) }}
                    </span>
                    <span
                        v-else-if="props.column.field == 'description'"
                        :style="ColumnsStyle"
                    >
                        {{ $t(props.row.description) }}
                    </span>

                    <span v-else-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="
                                    $t(
                                        'Button.Detail'
                                    )
                                "
                                :to="{
                                    path: `/study/course/detail/${props.row.id}`,
                                }"
                                v-if="authorize(['ManageSTDCourse'])"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageSTDCourse'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="
                                    $t(
                                        'Button.Delete'
                                    )
                                "
                                @click="doDelete(props.row.id)"
                            >
                                <Icon
                                    icon="fluent:delete-20-regular"
                                    class="xs-icon"
                                />
                            </b-button>
                        </div>
                    </span>
                </template>
            </BasicTable>
        </b-card>
    </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'

export default {
    mixins: [authorizationMixin],
    data() {
        return {
            searchDatas: [
               {
                    filterName: 'title',
                    label: 'Mã/Tên',
                    placeholder:
                        'Mã/Tên',
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
                {
                    searchType: 'b-form-datepicker',
                    filterName: 'FilterFrom',
                    label: 'categories.groupAccesses.list.searchForm.label.effectiveFrom',
                    placeholder: 'common.form.placeholder.selectValue',
                    autoSearch: true,
                    dateFormatOptions: {
                        day: 'numeric',
                        month: 'long',
                        year: 'numeric',
                    },
                },
                {
                    searchType: 'b-form-datepicker',
                    filterName: 'FilterTo',
                    label: 'categories.groupAccesses.list.searchForm.label.effectiveTo',
                    placeholder: 'common.form.placeholder.selectValue',
                    autoSearch: true,
                    dateFormatOptions: {
                        day: 'numeric',
                        month: 'long',
                        year: 'numeric',
                    },
                },
            ],
            searchForm: {
                title: null,
                FilterTo:null,
                FilterFrom:null
            },

            ColumnsStyle: {
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
            },
        }
    },

    computed: {
         currentLocale() {
            return this.$i18n.locale
        },
        Columns() {
            return [
               
                {
                    label: this.$t('Mã khóa học'),
                    field: 'code',
                },
                {
                    label: this.$t('Tên khóa học'),
                    field: 'name',
                },
                {
                    label: this.$t(
                        'categories.groupAccesses.list.table.label.effectiveFrom'
                    ),
                    field: 'startTimeStr',
                },
                {
                    label: this.$t(
                        'categories.groupAccesses.list.table.label.effectiveTo'
                    ),
                    field: 'endTimeStr',
                },
                {
                    label: this.$t('Trạng thái'),
                    field: 'statusStr',
                },
                {
                    label: this.$t('Bài giảng'),
                    field: 'lessonName',
                },
                {
                    label: this.$t(
                        'categories.groupAccesses.list.table.label.action'
                    ),
                    field: 'action',
                },
            ]
        },
    },

    methods: {
         handleBinding(event) {
            debugger
            this.searchForm[event.filterName] = event.value
            if (event.autoSearch) this.refresh()
        },
        refresh() {
            this.$refs.eventWaterTable.refresh()
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
                        .delete(`/stdCourses/${id}`)
                        .then((responce) => {
                            this.refresh()
                            this.$swal({
                                icon: 'success',
                                title: this.$t('Success.Delete'),
                                text: '',
                                customClass: {
                                    confirmButton: 'btn btn-success',
                                },
                            })
                        })
                }
            })
        },
    },
}
</script>

<style>
</style>
