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
                @click="$router.push({ name: 'test-config-create' })"
            >
                {{ this.$t('Button.Create') }}
            </button>
            <!-- table -->
            <BasicTable
                ref="eventWaterTable"
                :columns="Columns"
                data-url="/stdconfigs"
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
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/test/config/detail/${props.row.id}`,
                                }"
                                v-if="authorize(['ManageSTDConfig'])"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageSTDConfig'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Delete')"
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
                    placeholder: 'Mã/Tên',
                    autoSearch: true,
                },
            ],
            searchForm: {
                title: null,
                FilterTo: null,
                FilterFrom: null,
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
                    label: this.$t('Mã cấu hình'),
                    field: 'code',
                },
                {
                    label: this.$t('Tên cấu hình'),
                    field: 'name',
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
                        .delete(`/stdconfigs/${id}`)
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

<style></style>
