<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <b-row class="justify-content-start mt-1 mb-1">
                        <b-col sm="8" md="7" lg="6">
                            <b-form-group
                                :label="$t('EventType.List.Search.NameCode')"
                                label-for="h-filterText"
                                label-cols="12"
                                label-cols-sm="auto"
                            >
                                <b-form-input
                                    id="h-filterText"
                                    v-model="searchForm.filterText"
                                    :placeholder="$t('EventType.List.Search.NameCodePlaceholder')"
                                    @input="onSearchInput"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>

        <b-card title="">
            <div>
                <b-button
                    variant="primary"
                    style="margin-bottom: 15px"
                    @click="openCreateModal"
                >
                    <feather-icon icon="PlusIcon" class="mr-50" />
                    {{ $t('Button.Create') }}
                </b-button>
            </div>

            <!-- Table -->
            <BasicTable
                ref="eventTypeTable"
                :columns="columns"
                data-url="/event-types"
                :search-form="searchForm"
                storage-name="eventTypeTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Action -->
                    <span v-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                @click="openEditModal(props.row)"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="btn-icon"
                                :title="$t('Button.Delete')"
                                @click="doDelete(props.row)"
                            >
                                <Icon
                                    icon="fluent:delete-20-regular"
                                    class="xs-icon"
                                />
                            </b-button>
                        </div>
                    </span>
                    <span v-else>{{
                        props.formattedRow[props.column.field]
                    }}</span>
                </template>
            </BasicTable>
        </b-card>
    </div>
</template>

<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import {
    BCard,
    BCardBody,
    BRow,
    BCol,
    BButton,
    BSpinner,
    BFormInput,
    BFormGroup,
    BForm,
} from 'bootstrap-vue'

export default {
    name: 'EventTypePage',
    components: {
        ToastificationContent,
        BCard,
        BCardBody,
        BRow,
        BCol,
        BButton,
        BSpinner,
        BFormInput,
        BFormGroup,
        BForm,
    },
    data() {
        return {
            searchForm: {
                filterText: '',
            },
            searchTimeout: null,
        }
    },
    computed: {
        columns() {
            return [
                {
                    label: 'EventType.List.Table.Id',
                    field: 'id',
                    width: '100px',
                },
                { label: 'EventType.List.Table.Name', field: 'name' },
                {
                    label: 'EventType.List.Table.Action',
                    field: 'action',
                    width: '120px',
                    sortable: false,
                },
            ]
        },
    },
    methods: {
        search() {
            this.$refs.eventTypeTable.refresh()
        },
        onSearchInput() {
            clearTimeout(this.searchTimeout)
            this.searchTimeout = setTimeout(() => {
                this.$refs.eventTypeTable.refresh()
            }, 500)
        },
        openCreateModal() {
            this.$router.push({ name: 'event-type-create' })
        },
        openEditModal(item) {
            this.$router.push({
                name: 'event-type-detail',
                params: { id: item.id },
            })
        },

        doDelete(row) {
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
            }).then(async (result) => {
                if (result.value) {
                    try {
                        // Composite PK: gửi cả id và compId
                        const compId = row.compId
                        await this.$services.delete(
                            `/event-types/${row.id}?compId=${compId}`
                        )
                        this.$toast({
                            component: ToastificationContent,
                            position: 'top-right',
                            props: {
                                title: this.$t('Success.Delete'),
                                icon: 'CheckIcon',
                                variant: 'success',
                            },
                        })
                        this.$refs.eventTypeTable.refresh()
                    } catch (err) {
                        this.$toast({
                            component: ToastificationContent,
                            position: 'top-right',
                            props: {
                                title: this.$t('Error.Title'),
                                icon: 'AlertTriangleIcon',
                                variant: 'danger',
                                text: err?.message || '',
                            },
                        })
                    }
                }
            })
        },
    },
}
</script>
