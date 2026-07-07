<template>
    <b-container fluid class="p-0">
        <b-card body-class="px-1 py-0 pt-1">
            <b-form @submit.prevent="filterChanged">
                <b-row>
                    <b-col sm="8" md="7" lg="6">
                        <b-form-group
                            :label="
                                this.$t(
                                    'categories.contractors.list.searchForm.label.contractorCodeName'
                                )
                            "
                            label-for="h-searchForm-code-name"
                            label-cols="12"
                            label-cols-sm="auto"
                        >
                            <b-form-input
                                id="h-searchForm-text"
                                v-model="searchForm.filterText"
                                :placeholder="
                                    this.$t(
                                        'categories.contractors.list.searchForm.placeholder.contractorCodeName'
                                    )
                                "
                                type="text"
                            />
                        </b-form-group>
                    </b-col>
                    <!-- <b-col cols="auto" class="m-auto m-sm-0">
                            <b-button
                                v-waves.pressed
                                class="btn-120"
                                variant="primary"
                                @click="filterChanged"
                            >
                                <Icon
                                    icon="icon-park-outline:search"
                                    class="xs-icon"
                                />
                                <span class="ml-50">
                                    {{ $t('common.button.search') }}
                                </span>
                            </b-button>
                        </b-col> -->
                </b-row>
            </b-form>
        </b-card>
        <b-card body-class="p-1">
            <div class="vgt-wrap">
                <vue-ads-table-tree
                    ref="treeTable"
                    :columns="table.columns"
                    :classes="table.classes"
                    :rows="table.rows"
                    :page="table.page"
                    :filter="table.filter"
                    @page-change="pageChange"
                >
                    <template slot="top">
                        <b-button
                            v-if="authorize(['ManageDepartment'])"
                            variant="primary"
                            :to="{ path: '/categories/contractors/create' }"
                            class="mb-1 btn-hover-linear-primary border-0"
                        >
                            {{ $t('common.button.create') }}
                        </b-button>
                    </template>
                    <template slot="toggle-children-icon" slot-scope="props">
                        <Icon v-if="props.expanded" icon="mdi:minus-box" />
                        <Icon v-else icon="mdi:plus-box" />
                    </template>
                    <template slot="index" slot-scope="props">
                        {{ props.row._meta.index + 1 }}
                    </template>
                    <template slot="action" slot-scope="props">
                        <b-button
                            v-if="
                                authorize(['ViewDepartment']) ||
                                authorize(['ManageDepartment'])
                            "
                            v-b-tooltip.hover
                            v-waves
                            variant="label-secondary"
                            class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                            :title="$t('common.button.detail')"
                            :to="{
                                path: `/categories/contractors/detail/${props.row.id}`,
                            }"
                        >
                            <Icon icon="mdi:eye-outline" class="xs-icon" />
                        </b-button>
                        <b-button
                            v-if="authorize(['ManageDepartment'])"
                            v-b-tooltip.hover
                            v-waves
                            variant="label-secondary"
                            class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-danger border-0"
                            :title="$t('common.button.delete')"
                            @click="deleteDepartment(props.row.id)"
                        >
                            <Icon
                                icon="fluent:delete-20-regular"
                                class="xs-icon"
                            />
                        </b-button>
                        <b-button
                            v-if="authorize(['ManageDepartment'])"
                            v-b-tooltip.hover
                            v-waves
                            variant="label-secondary"
                            class="btn-icon btn-hover-linear-primary border-0"
                            :title="$t('common.button.create')"
                            :to="{
                                path: `/categories/contractors/create`,
                                query: { parentId: props.row.id },
                            }"
                        >
                            <Icon icon="mdi:plus-thick" class="xs-icon" />
                        </b-button>
                    </template>
                </vue-ads-table-tree>
            </div>
        </b-card>
    </b-container>
</template>
<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import VueAdsTableTree from 'vue-ads-table-tree'
import { authorizationMixin } from '@core/mixins/ui/forms'
export default {
    mixins: [authorizationMixin],
    components: {
        VueAdsTableTree,
        ToastificationContent,
    },
    data() {
        return {
            table: {
                rows: [],
                columns: [
                    {
                        property: 'index',
                        title: '',
                        direction: '',
                        filterable: false,
                        groupable: false,
                        collapseIcon: true,
                    },
                    {
                        property: 'code',
                        title: this.$t(
                            'categories.contractors.list.table.label.contractorCode'
                        ),
                        direction: null,
                        filterable: true,
                    },
                    {
                        property: 'name',
                        title: this.$t(
                            'categories.contractors.list.table.label.contractorName'
                        ),
                        direction: null,
                        filterable: true,
                        groupable: false,
                        groupCollapsable: false,
                        hideOnGroup: true,
                    },

                    {
                        property: 'action',
                        title: this.$t(
                            'categories.contractors.list.table.label.action'
                        ),
                        direction: null,
                        filterable: false,
                        groupable: false,
                    },
                ],
                classes: {
                    table: {
                        'default-font': true,
                        'vgt-table bordered ': true,
                    },
                    'all/0': {
                        'cell-index': true,
                        'text-center': true,
                    },
                    'all/4': {
                        'cell-action': true,
                    },
                    '0_-0/4': {
                        'text-center': true,
                    },
                },
                filter: '',
                page: 0,
            },
            searchForm: {
                filterText: '',
                companyId: null,
            },
            treeDepartments: [],
        }
    },
    computed: {
        language() {
            return this.$i18n.locale
        },
    },
    watch: {
        language() {
            this.refreshTableHeader()
        },
    },
    async created() {
        await this.getTreeDepartments()
        const accessToken = this.$services.getUserData()
        this.searchForm.companyId = accessToken.companyId
    },
    methods: {
        async getTreeDepartments() {
            try {
                const res = await this.$services.get('/departments/tree/2')
                this.table.rows = res.data.data
            } catch {
                console.log('error')
            }
        },
        filterChanged() {
            this.table.filter = this.searchForm.filterText?.trim()
        },
        pageChange(newPage) {
            this.table.page = newPage
        },
        async confirmDelete() {
            return await this.$swal.fire({
                title: this.$t('common.confirmation.delete.title'),
                text: this.$t('common.confirmation.delete.message'),
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: this.$t('Message.Agree'),
                cancelButtonText: this.$t('Message.Exit'),
                customClass: {
                    // confirmButton: 'btn btn-label-primary mr-1',
                    // cancelButton: 'btn btn-label-danger',
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-outline-danger ml-1',
                },
                buttonsStyling: false,
            })
        },
        async deleteDepartment(id) {
            const { isConfirmed } = await this.confirmDelete()
            if (isConfirmed) {
                try {
                    const res = await this.$services.delete(
                        `/departments/${id}`
                    )
                    this.showSuccessToast(res.data)
                    await this.getTreeDepartments()
                } catch (error) {
                    this.showErrorToast(error)
                }
            }
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(
                        `categories.contractors.error.${data.errorCode}`
                    ),
                    text: '',
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
        showErrorToast(error) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t('Error.Error'),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(`${error.message}`),
                },
            })
        },
        refreshTableHeader() {
            this.table.columns[1].title = this.$t(
                'categories.contractors.list.table.label.contractorCode'
            )
            this.table.columns[2].title = this.$t(
                'categories.contractors.list.table.label.contractorName'
            )
            this.table.columns[3].title = this.$t(
                'categories.contractors.list.table.label.action'
            )
        },
    },
}
</script>

<style lang="scss">
@import 'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css';
.default-font {
    font-family: 'Montserrat', Helvetica, Arial, serif;
}
.cell-index {
    max-width: 2.5rem;
}
.cell-action {
    width: 12rem;
}
.swal2-styled.swal2-confirm.btn-label-primary {
    color: #7367f0;
    background: #e9e7fd;
}
.swal2-styled.swal2-cancel.btn-label-danger {
    color: #ea5455;
    background: #fad6d6;
}
.vgt-wrap {
    .vue-ads-flex.vue-ads-m-2.vue-ads-px-0.vue-ads-text-xs {
        font-size: 1rem;
    }
    button.vue-ads-ml-1.vue-ads-leading-normal.vue-ads-w-6 {
        padding: 0.25rem 1.2rem 0.25rem 0.8rem;
        text-align: center;
        border-radius: 10%;
        background: #f1f1f2;
        border-color: rgba(0, 0, 0, 0);
        color: #a8aaae;
    }
    button.vue-ads-ml-1.vue-ads-leading-normal.vue-ads-w-6.vue-ads-bg-teal-500.vue-ads-text-white {
        background-color: #7367f0;
        color: #fff;
    }
}
body {
    &.dark-layout {
        button.vue-ads-ml-1.vue-ads-leading-normal.vue-ads-w-6 {
            background: #424659;
            border-color: rgba(0, 0, 0, 0);
            color: #a8aaae;
        }
    }
}
</style>
