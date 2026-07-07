<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    this.$t(
                                        'categories.workFlows.list.searchForm.label.filterText'
                                    )
                                "
                                label-for="h-searchForm-code-name"
                                label-cols-md="3"
                            >
                                <b-form-input
                                    id="h-searchForm-text"
                                    v-model="searchForm.filterText"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    this.$t(
                                        'categories.workFlows.list.searchForm.label.status'
                                    )
                                "
                                label-for="h-searchForm-area"
                                label-cols-md="3"
                            >
                                <v-select
                                    v-model="searchForm.status"
                                    :placeholder="
                                        this.$t(
                                            'categories.workFlows.list.searchForm.placeholder.status'
                                        )
                                    "
                                    :options="statusOptions"
                                    label="text"
                                    :reduce="(opt) => opt.value"
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
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
                        >Search</b-button
                    >
                </b-form>
            </b-card-body>
        </b-card>

        <b-card title="">
            <div>
                <b-button
                    v-if="authorize(['ManageWorkFlow'])"
                    variant="primary"
                    :to="{ path: '/categories/workFlows/create' }"
                    style="margin-bottom: 15px"
                >
                    {{ this.$t('Button.Create') }}
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="workFlowTable"
                :columns="columns"
                data-url="/work-flows"
                :search-form="searchForm"
                storage-name="workFlowTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Roles -->
                    <span v-if="props.column.field == 'status'">
                        {{ getStatusName(props.row.status) }}
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize(['ViewWorkFlow'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/categories/workFlows/detail/${props.row.id}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageWorkFlow'])"
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
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    mixins: [authorizationMixin],
    components: { ToastificationContent },
    data() {
        return {
            productionLines: [],
            searchForm: {
                filterText: null,
                status: null,
                compId: null,
            },
            columns: [
                {
                    label: 'categories.workFlows.list.table.label.name',
                    field: 'name',
                },
                {
                    label: 'categories.workFlows.list.table.label.code',
                    field: 'code',
                },
                {
                    label: 'categories.workFlows.list.table.label.status',
                    field: 'status',
                },
                {
                    label: 'Device.List.Table.Operation',
                    field: 'action',
                },
            ],
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        // this.fetchProductionLines()
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
        statusOptions() {
            return [
                {
                    value: 1,
                    text: this.$t(
                        'categories.workFlows.list.searchForm.options.active'
                    ),
                },
                {
                    value: 0,
                    text: this.$t(
                        'categories.workFlows.list.searchForm.options.inactive'
                    ),
                },
            ]
        },
    },
    methods: {
        fetchProductionLines() {
            this.$services
                .get('/lookup/production-lines', {
                    params: { compId: this.searchForm.compId },
                })
                .then((response) => {
                    this.productionLines = response.data.data
                })
                .catch((error) => {
                    console.error(error)
                })
        },
        formatDate(dateString) {
            if (!dateString) return ''
            return new Date(dateString).toLocaleString('en-GB', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
            })
        },
        getStatusName(status) {
            return (
                this.statusOptions.find((option) => option.value === status)
                    ?.text || ''
            )
        },
        search() {
            this.$refs.workFlowTable.refresh()
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
                        .delete('/work-flows/' + item)
                        .then((response) => {
                            this.$refs.workFlowTable.refresh()
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
                                    title: 'Error',
                                    text: '',
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: this.$t(
                                        `categories.areas.error.${error.response.data.errorCode}`
                                    ),
                                },
                            })
                        })
                }
            })
        },
        showSuccessToast() {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: 'Success',
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
    },
}
</script>

<style lang="scss"></style>
