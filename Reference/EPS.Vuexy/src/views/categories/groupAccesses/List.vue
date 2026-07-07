<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <!-- advance search input -->
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
                    <b-row v-if="dateError">
                        <b-col>
                            <small class="text-danger">{{ dateError }}</small>
                        </b-col>
                    </b-row>
                    <b-button
                        type="submit"
                        class="btn-hover-linear-primary"
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
                    class="btn-hover-linear-primary"
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
                storageName="groupAcessTable"
            >
                <template v-slot:table-row="{ column, row }">
                    <span v-if="column.field == 'effectiveFrom'">
                        {{ formatToDate(row.effectiveFrom) }}
                    </span>
                    <span v-else-if="column.field == 'effectiveTo'">
                        {{ formatToDate(row.effectiveTo) }}
                    </span>
                    <span v-else-if="column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize([authorizeName.view])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary"
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
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-danger"
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
                    <span v-else>{{ row[column.field] }}</span>
                </template>
            </BasicTable>
        </b-card>
    </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import TreeHelper from '@/utils/treeHelper'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    components: { ToastificationContent },
    mixins: [authorizationMixin],
    data() {
        return {
            searchDatas: [
                {
                    searchType: 'tree-select',
                    filterName: 'filterCompId',
                    label: 'categories.groupAccesses.list.searchForm.label.compName',
                    placeholder:
                        'categories.groupAccesses.list.searchForm.label.compName',
                    options: null,
                    autoSearch: true,
                    multiple: false,
                    modelValue: this.$services.getUserData().companyId,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterGroupId',
                    label: 'categories.groupAccesses.list.searchForm.label.groupName',
                    placeholder:
                        'categories.groupAccesses.list.searchForm.placeholder.groupName',
                    options: [],
                    autoSearch: true,
                },
                {
                    searchType: 'b-form-datepicker',
                    filterName: 'filterEffectiveFrom',
                    label: 'categories.groupAccesses.list.searchForm.label.effectiveFrom',
                    placeholder: 'common.select.placeholder',
                    autoSearch: true,
                    dateFormatOptions: {
                        day: 'numeric',
                        month: 'long',
                        year: 'numeric',
                    },
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterTimeAccessId',
                    label: 'categories.groupAccesses.list.searchForm.label.timeAccessName',
                    placeholder:
                        'categories.groupAccesses.list.searchForm.placeholder.timeAccessName',
                    options: [],
                    autoSearch: true,
                },
                {
                    searchType: 'b-form-datepicker',
                    filterName: 'filterEffectiveTo',
                    label: 'categories.groupAccesses.list.searchForm.label.effectiveTo',
                    placeholder: 'common.select.placeholder',
                    autoSearch: true,
                    dateFormatOptions: {
                        day: 'numeric',
                        month: 'long',
                        year: 'numeric',
                    },
                },
                {
                    searchType: 'tree-select',
                    filterName: 'filterAreaId',
                    label: 'categories.groupAccesses.list.searchForm.label.areaName',
                    placeholder:
                        'categories.groupAccesses.list.searchForm.placeholder.areaName',
                    options: [],
                    autoSearch: true,
                    valueConsistsOf: 'ALL',
                    multiple: true,
                },
            ],
            searchForm: {
                filterCompId: null,
                filterGroupId: null,
                filterAreaId: null,
                filterTimeAccessId: null,
                filterEffectiveFrom: null,
                filterEffectiveTo: null,
            },
            table: {
                dataUrl: '/groupAccesses',
                columns: [
                    {
                        label: 'categories.groupAccesses.list.table.label.compName',
                        field: 'compName',
                    },
                    {
                        label: 'categories.groupAccesses.list.table.label.groupName',
                        field: 'groupName',
                    },
                    {
                        label: 'categories.groupAccesses.list.table.label.timeAccessName',
                        field: 'timeAccessName',
                    },
                    {
                        label: 'categories.groupAccesses.list.table.label.areaName',
                        field: 'areaNames',
                    },
                    {
                        label: 'categories.groupAccesses.list.table.label.effectiveFrom',
                        field: 'effectiveFromStr',
                    },
                    {
                        label: 'categories.groupAccesses.list.table.label.effectiveTo',
                        field: 'effectiveToStr',
                    },
                    {
                        label: 'categories.groupAccesses.list.table.label.action',
                        field: 'action',
                    },
                ],
            },
            authorizeName: {
                manage: 'ManageTimeAccess',
                view: 'ViewTimeAccess',
            },
            routerLink: {
                create: '/categories/groupAccesses/create/',
                detail: '/categories/groupAccesses/detail/',
                delete: '/groupAccesses/',
            },
            dateError: null,
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    watch: {
        'searchForm.filterCompId'(val) {
            this.$services.get(`/lookup/groups${val ? '?compId=' + val : ''}`).then((response) => {
                this.searchDatas[1].options = response.data.data
            })
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.filterCompId = accessToken.companyId
        this.lookupData()
    },
    methods: {
        handleBinding(event) {
            this.searchForm[event.filterName] = event.value
            // Validate date range
            if (
                this.searchForm.filterEffectiveFrom &&
                this.searchForm.filterEffectiveTo &&
                new Date(this.searchForm.filterEffectiveFrom) > new Date(this.searchForm.filterEffectiveTo)
            ) {
                this.dateError = this.$t('categories.groupAccesses.list.validation.effectiveDateRange')
                return
            }
            this.dateError = null
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
                this.searchDatas[0].options = TreeHelper.removeEmptyChildren(
                    response.data
                )
            })
            const currentCompId = this.searchForm.filterCompId;
            this.$services.get(`/lookup/groups${currentCompId ? '?compId=' + currentCompId : ''}`).then((response) => {
                this.searchDatas[1].options = response.data.data
            })
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.searchDatas[5].options = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
            this.$services.get('/lookup/timeAccesses').then((response) => {
                this.searchDatas[3].options = response.data.data
            })
        },
        formatToDate(date) {
            return date ? date.split('T')[0] : null
        },
    },
}
</script>

<style lang="scss"></style>
