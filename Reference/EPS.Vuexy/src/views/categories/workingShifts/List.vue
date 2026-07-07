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
                                        'categories.workingShifts.list.searchForm.label.nameOrCode'
                                    )
                                "
                                label-for="h-searchForm-code-name"
                                label-cols-md="3"
                            >
                                <b-form-input
                                    id="h-searchForm-text"
                                    v-model="searchForm.nameOrCode"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    this.$t(
                                        'categories.workingShifts.list.searchForm.label.status'
                                    )
                                "
                                label-for="h-searchForm-area"
                                label-cols-md="3"
                            >
                                <v-select
                                    v-model="searchForm.statuses"
                                    :placeholder="
                                        this.$t(
                                            'categories.workingShifts.list.searchForm.placeholder.status'
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
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.workingShifts.list.searchForm.label.startTime'
                                    )
                                "
                                label-for="h-searchForm-dateFrom"
                                label-cols-md="3"
                            >
                                <date-picker
                                    id="h-searchForm-dateFrom"
                                    v-model="searchForm.startTime"
                                    type="time"
                                    :locale="currentLocale"
                                    format="HH:mm"
                                    value-type="HH:mm:ss"
                                    style="width: 100%"
                                    @change="search"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.workingShifts.list.searchForm.label.endTime'
                                    )
                                "
                                label-for="h-searchForm-dateFrom"
                                label-cols-md="3"
                            >
                                <date-picker
                                    id="h-searchForm-endTime"
                                    v-model="searchForm.endTime"
                                    type="time"
                                    :locale="currentLocale"
                                    format="HH:mm"
                                    value-type="HH:mm:ss"
                                    style="width: 100%"
                                    @change="search"
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
                    v-if="authorize(['ManageWorkingShift'])"
                    variant="primary"
                    :to="{ path: '/categories/workingShifts/create' }"
                    style="margin-bottom: 15px"
                >
                    {{ this.$t('Button.Create') }}
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="workingShiftTable"
                :columns="columns"
                data-url="/working-shifts"
                :search-form="searchForm"
                storage-name="workingShiftTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Roles -->
                    <span v-if="props.column.field == 'status'">
                        {{ getStatusName(props.row.status) }}
                    </span>
                    <!-- Column: Date -->
                    <span v-else-if="props.column.field == 'startTime'">
                        {{ formatTime(props.row.startTime) }}
                    </span>
                    <span v-else-if="props.column.field == 'endTime'">
                        {{ formatTime(props.row.endTime) }}
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize(['ViewWorkingShift'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/categories/workingShifts/detail/${props.row.id}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageWorkingShift'])"
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
            searchForm: {
                nameOrCode: null,
                startTime: null,
                endTime: null,
                statuses: null,
                compId: null,
            },
            columns: [
                {
                    label: 'categories.workingShifts.list.table.label.code',
                    field: 'code',
                },
                {
                    label: 'categories.workingShifts.list.table.label.name',
                    field: 'name',
                },
                {
                    label: 'categories.workingShifts.list.table.label.startTime',
                    field: 'startTime',
                },
                {
                    label: 'categories.workingShifts.list.table.label.endTime',
                    field: 'endTime',
                },
                {
                    label: 'categories.workingShifts.list.table.label.status',
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
                        'categories.workingShifts.list.searchForm.options.active'
                    ),
                },
                {
                    value: 0,
                    text: this.$t(
                        'categories.workingShifts.list.searchForm.options.inactive'
                    ),
                },
            ]
        },
    },
    methods: {
        formatTime(timeString) {
            if (!timeString) return ''
            return this.$moment(timeString, 'HH:mm:ss').format('HH:mm')
        },
        getStatusName(status) {
            return (
                this.statusOptions.find((option) => option.value === status)
                    ?.text || ''
            )
        },
        search() {
            this.$refs.workingShiftTable.refresh()
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
                // this.$toast({
                //     component: ToastificationContent,
                //     position: 'top-right',
                //     props: {
                //         title: `categories.departments.error`,
                //         icon: 'CheckIcon',
                //         variant: 'success',
                //     },
                // })
                if (result.value) {
                    this.$services
                        .delete('/working-shifts/' + item)
                        .then((response) => {
                            this.$refs.workingShiftTable.refresh()
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
