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
                storageName="machinesTable"
            >
                <template v-slot:table-row="{ column, row }">
                    <!-- format Column: UpdateTime -->
                    <span v-if="column.field === 'updateTime'">
                        {{
                            row.updateTime ? formatDateTime(row.updateTime) : ''
                        }}
                    </span>
                    <!-- Column: Action -->
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
                </template>
            </BasicTable>
        </b-card>
    </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
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
                    label: 'categories.machines.list.searchForm.label.compName',
                    placeholder:
                        'categories.machines.list.searchForm.placeholder.compName',
                    options: [],
                    autoSearch: true,
                    modelValue: this.$services.getUserData().companyId,
                },
                {
                    filterName: 'filterText',
                    label: 'categories.machines.list.searchForm.label.machineNameDescript',
                    placeholder:
                        'categories.machines.list.searchForm.placeholder.machineNameDescript',
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterAreaId',
                    label: 'categories.machines.list.searchForm.label.areaName',
                    placeholder:
                        'categories.machines.list.searchForm.placeholder.areaName',
                    options: [],
                    autoSearch: true,
                },
            ],
            searchForm: {
                filterCompId: null,
                filterText: null,
                filterAreaId: null,
            },
            table: {
                dataUrl: '/machines',
                columns: [
                    {
                        label: 'categories.machines.list.table.label.companyName',
                        field: 'compName',
                    },
                    {
                        label: 'categories.machines.list.table.label.machineName',
                        field: 'machineName',
                    },
                    {
                        label: 'categories.machines.list.table.label.description',
                        field: 'description',
                    },
                    {
                        label: 'categories.machines.list.table.label.areaName',
                        field: 'areaName',
                    },
                    {
                        label: 'categories.machines.list.table.label.updateTime',
                        field: 'updateTime',
                    },
                    {
                        label: 'categories.machines.list.table.label.status',
                        field: 'autoLaunch',
                    },
                    {
                        label: 'categories.machines.list.table.label.action',
                        field: 'action',
                    },
                ],
            },
            authorizeName: {
                manage: 'ManageMachine',
                view: 'ViewMachine',
            },
            routerLink: {
                create: '/categories/machines/create/',
                detail: '/categories/machines/detail/',
                delete: '/machines/',
            },
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        this.lookupData()
    },
    methods: {
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
            this.$services.get('/lookup/areas').then((response) => {
                this.searchDatas[2].options = response.data.data
            })
        },
        // format date time for table column updateTime field to dd/mm/yyyy hh:mm:ss
        formatDateTime(dateString) {
            const date = new Date(dateString)
            const day = String(date.getDate()).padStart(2, '0')
            const month = String(date.getMonth() + 1).padStart(2, '0')
            const year = date.getFullYear()
            const hours = String(date.getHours()).padStart(2, '0')
            const minutes = String(date.getMinutes()).padStart(2, '0')
            const seconds = String(date.getSeconds()).padStart(2, '0')
            return `${day}/${month}/${year} ${hours}:${minutes}:${seconds}`
        },
    },
}
</script>

<style lang="scss"></style>
