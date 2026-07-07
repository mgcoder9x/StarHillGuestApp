<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <!-- advance search input -->
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    this.$t('Device.List.SearchForm.NameOrCode')
                                "
                                label-for="h-searchForm-code"
                                label-cols-md="3"
                            >
                                <b-form-input
                                    id="h-searchForm-text"
                                    v-model="searchForm.codeName"
                                    type="text"
                                    :placeholder="
                                        this.$t(
                                            'Device.List.SearchForm.NameOrCode'
                                        )
                                    "
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Device.List.SearchForm.Area')"
                                label-for="h-searchForm-area"
                                label-cols-md="3"
                            >
                                <tree-select
                                    v-model="searchForm.areaId"
                                    :options="listArea"
                                    label="text"
                                    :reduce="(area) => area.id"
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    this.$t('Device.List.SearchForm.Status')
                                "
                                label-for="h-searchForm-status"
                                label-cols-md="3"
                            >
                                <v-select
                                    v-model="searchForm.status"
                                    :placeholder="
                                        this.$t('Device.List.SearchForm.Status')
                                    "
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    label="text"
                                    :reduce="(status) => status.id"
                                    :options="lstStatus"
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    this.$t('Device.List.SearchForm.Function')
                                "
                                label-for="h-searchForm-event-type"
                                label-cols-md="3"
                            >
                                <v-select
                                    v-model="searchForm.eventTypeId"
                                    :placeholder="
                                        this.$t(
                                            'Device.List.SearchForm.Function'
                                        )
                                    "
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    label="text"
                                    :reduce="(evetType) => evetType.id"
                                    :options="eventTypeOptions"
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
                    v-if="authorize(['ManageDevice'])"
                    variant="primary"
                    :to="{ path: '/categories/devices/create' }"
                    style="margin-bottom: 15px"
                >
                    {{ this.$t('Button.Create') }}
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="deviceTable"
                :columns="columns"
                data-url="/device"
                :search-form="searchForm"
                storage-name="deviceTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Roles -->
                    <span
                        v-if="props.column.field == 'link'"
                        style="
                            display: -webkit-box;
                            -webkit-box-orient: vertical;
                            overflow: hidden;
                            -webkit-line-clamp: 2;
                            width: 100px;
                        "
                    >
                        {{ props.row.link }}
                    </span>
                    <span v-else-if="props.column.field === 'eventTypeId'">
                        {{ formatEventType(props.row.eventTypeId) }}
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize(['ViewDevice'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/categories/devices/detail/${props.row.id}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageDevice'])"
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
import TreeHelper from '@/utils/treeHelper'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    mixins: [authorizationMixin],
    components: { ToastificationContent },
    data() {
        return {
            searchForm: {
                codeName: '',
                compId: '',
                areaId: null,
                status: 1,
                eventTypeId: '',
            },
            // define options
            options: [],
            columns: [
                {
                    label: 'Device.List.Table.Code',
                    field: 'code',
                },
                {
                    label: 'Device.List.Table.Name',
                    field: 'name',
                },
                {
                    label: 'Device.List.Table.Area',
                    field: 'areaName',
                },
                {
                    label: 'Device.List.Table.Function',
                    field: 'eventTypeId',
                },
                {
                    label: 'Device.List.Table.Status',
                    field: 'statusName',
                },
                {
                    label: 'Device.List.Table.Server',
                    field: 'serverName',
                },
                {
                    label: 'Device.List.Table.Link',
                    field: 'link',
                },
                {
                    label: 'Device.List.Table.Operation',
                    field: 'action',
                },
            ],
            listArea: [],
            listEventType: [],
            listServer: [],
            lstStatus: [
                { text: 'On', id: 1 },
                { text: 'Off', id: 0 },
            ],
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        this.loadArea()
        this.loadEventType()
        this.loadServer()
    },
    computed: {
        eventTypeOptions() {
            const { locale } = this.$i18n
            if (locale === 'vi') {
                return this.listEventType
            }
            return this.listEventType.map((item) => ({
                ...item,
                text: item.englishName,
            }))
        },
    },
    methods: {
        formatEventType(eventTypeId) {
            const existingType = this.eventTypeOptions.find(
                (i) => parseInt(i.id, 10) === eventTypeId
            )
            return existingType
                ? this.$t(existingType.text)
                : this.$t(
                      'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Unknown'
                  )
        },
        search() {
            this.$refs.deviceTable.refresh()
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
                        .delete('/device/' + item)
                        .then((response) => {
                            this.$refs.deviceTable.refresh()
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
        //lookup data
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                this.listEventType = response.data.data
            })
        },
        loadArea() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.listArea = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
        },
        loadServer() {
            this.$services.get('/lookup/server').then((response) => {
                this.listServer = response.data.data
            })
        },
    },
}
</script>

<style lang="scss"></style>
