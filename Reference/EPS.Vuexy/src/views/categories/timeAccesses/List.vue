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
                    class="mb-2 btn-hover-linear-primary"
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
                storageName="timeAccessesTable"
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
                    label: 'categories.timeAccesses.list.searchForm.label.compName',
                    placeholder:
                        'categories.timeAccesses.list.searchForm.label.compName',
                    options: null,
                    autoSearch: true,
                    modelValue: this.$services.getUserData().companyId,
                },
                {
                    filterName: 'filterText',
                    label: 'categories.timeAccesses.list.searchForm.label.name',
                    placeholder:
                        'categories.timeAccesses.list.searchForm.label.name',
                },
            ],
            searchForm: {
                filterCompId: null,
                filterText: null,
            },
            table: {
                dataUrl: '/timeAccesses',
                columns: [
                    {
                        label: 'categories.timeAccesses.list.table.label.compName',
                        field: 'compName',
                    },
                    {
                        label: 'categories.timeAccesses.list.table.label.name',
                        field: 'name',
                    },
                    {
                        label: 'categories.timeAccesses.list.table.label.action',
                        field: 'action',
                    },
                ],
            },
            authorizeName: {
                manage: 'ManageTimeAccess',
                view: 'ViewTimeAccess',
            },
            routerLink: {
                create: '/categories/timeAccesses/create/',
                detail: '/categories/timeAccesses/detail/',
                delete: '/timeAccesses/',
            },
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.filterCompId = accessToken.companyId
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
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    text: '',
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: this.$t(`${error.message}`),
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
        },
    },
}
</script>

<style lang="scss"></style>
