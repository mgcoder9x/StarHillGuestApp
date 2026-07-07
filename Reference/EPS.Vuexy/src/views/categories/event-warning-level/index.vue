<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <b-row class="justify-content-start mt-1 mb-1">
                        <b-col sm="12" md="6" lg="6">
                            <b-form-group
                                :label="
                                    $t('EventWarningLevel.List.Search.Title')
                                "
                                label-for="h-filterText"
                                label-cols="12"
                                label-cols-sm="auto"
                            >
                                <b-form-input
                                    id="h-filterText"
                                    v-model="searchForm.filterText"
                                    :placeholder="
                                        $t(
                                            'EventWarningLevel.List.Search.TitlePlaceholder'
                                        )
                                    "
                                    @input="onSearchInput"
                                />
                            </b-form-group>
                        </b-col>

                        <b-col sm="12" md="6" lg="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'EventWarningLevel.List.Search.EventType'
                                    )
                                "
                                label-for="h-filterText"
                                label-cols="12"
                                label-cols-sm="auto"
                            >
                                <v-select
                                    v-model="searchForm.eventTypeId"
                                    :options="eventTypeOptions"
                                    :placeholder="
                                        $t('common.select.placeholder')
                                    "
                                    label="text"
                                    :multiple="false"
                                    track-by="value"
                                    :reduce="(item) => item.value"
                                    @input="onSearchInput"
                                >
                                </v-select>
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
                ref="warningLevelTable"
                :columns="columns"
                data-url="/event-warning-levels"
                :search-form="searchForm"
                storage-name="warningLevelTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Color -->
                    <span v-if="props.column.field === 'color'">
                        <span
                            v-if="props.row.color"
                            :style="{
                                background: props.row.color,
                                display: 'inline-block',
                                width: '20px',
                                height: '20px',
                                borderRadius: '4px',
                                verticalAlign: 'middle',
                                marginRight: '6px',
                                border: '1px solid #ddd',
                            }"
                        />
                        <span>{{ props.row.color }}</span>
                    </span>

                    <!-- Column: EventType name -->
                    <span v-else-if="props.column.field === 'eventTypeId'">
                        {{ getEventTypeName(props.row.eventTypeId) }}
                    </span>

                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
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
                                @click="doDelete(props.row.id)"
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
    BFormTextarea,
    BFormSelect,
    BFormSelectOption,
    BForm,
} from 'bootstrap-vue'

export default {
    name: 'EventWarningLevelPage',
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
        BFormTextarea,
        BFormSelect,
        BFormSelectOption,
        BForm,
    },
    data() {
        return {
            searchForm: {
                filterText: '',
                eventTypeId: null,
            },
            searchTimeout: null,
            eventTypeOptions: [],
        }
    },
    created() {
        this.loadEventTypes()
    },
    computed: {
        columns() {
            return [
                {
                    label: 'EventWarningLevel.List.Table.Id',
                    field: 'id',
                    width: '80px',
                },
                {
                    label: 'EventWarningLevel.List.Table.Title',
                    field: 'title',
                },
                {
                    label: 'EventWarningLevel.List.Table.Level',
                    field: 'level',
                    width: '80px',
                },
                {
                    label: 'EventWarningLevel.List.Table.Color',
                    field: 'color',
                    width: '120px',
                    sortable: false,
                },
                {
                    label: 'EventWarningLevel.List.Table.EventTypeId',
                    field: 'eventTypeId',
                },
                {
                    label: 'EventWarningLevel.List.Table.Action',
                    field: 'action',
                    width: '120px',
                    sortable: false,
                },
            ]
        },
    },
    methods: {
        search() {
            this.$refs.warningLevelTable.refresh()
        },
        onSearchInput() {
            clearTimeout(this.searchTimeout)
            this.searchTimeout = setTimeout(() => {
                this.$refs.warningLevelTable.refresh()
            }, 500)
        },
        async loadEventTypes() {
            try {
                const res = await this.$services.get('/lookup/eventType', {
                })
                const data = res.data?.data || []
                this.eventTypeOptions = data.map((et) => ({
                    value: et.id,
                    text: et.text,
                }))
            } catch {
                /* ignore */
            }
        },
        getEventTypeName(id) {
            return this.eventTypeOptions.find((o) => o.value === id)?.text || id
        },
        openCreateModal() {
            this.$router.push({ name: 'event-warning-level-create' })
        },
        openEditModal(item) {
            this.$router.push({
                name: 'event-warning-level-detail',
                params: { id: item.id },
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
            }).then(async (result) => {
                if (result.value) {
                    try {
                        await this.$services.delete(
                            `/event-warning-levels/${id}`
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
                        this.$refs.warningLevelTable.refresh()
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
