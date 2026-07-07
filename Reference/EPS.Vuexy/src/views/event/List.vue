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
                                label="Thời gian vào:"
                                label-for="h-searchForm-dateFrom"
                                label-cols-md="3"
                            >
                                <b-form-datepicker
                                    id="h-searchForm-text"
                                    v-model="searchForm.dateFrom"
                                    type="datetime"
                                    placeholder="Thời gian vào"
                                >
                                    ></b-form-datepicker
                                >
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                label="Thời gian ra:"
                                label-for="h-searchForm-dateFrom"
                                label-cols-md="3"
                            >
                                <b-form-datepicker
                                    id="h-searchForm-text"
                                    v-model="searchForm.dateTo"
                                    type="datetime"
                                    placeholder="Thời gian ra"
                                >
                                    ></b-form-datepicker
                                >
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                label="Khu vực:"
                                label-for="h-searchForm-area"
                                label-cols-md="3"
                            >
                                <b-form-select
                                    v-model="searchForm.areaId"
                                    :options="listArea"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                label="Thiết bị:"
                                label-for="h-searchForm-device"
                                label-cols-md="3"
                            >
                                <b-form-select
                                    v-model="searchForm.device"
                                    :options="listDevice"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                label="Loại giám sát:"
                                label-for="h-searchForm-area"
                                label-cols-md="3"
                            >
                                <b-form-select
                                    v-model="searchForm.eventTypeId"
                                    :options="listEventType"
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
            <!-- table -->
            <BasicTable
                ref="eventTable"
                :columns="columns"
                data-url="/event"
                :search-form="searchForm"
                :sort-by="'eventId'"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Image -->
                    <span
                        v-if="props.column.field == 'image'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        <img
                            loading="lazy"
                            :src="props.row.image"
                            alt="Image"
                            width="100px"
                            height="80px"
                            style="margin: 0 auto"
                        />
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <span>
                            <b-dropdown
                                variant="link"
                                toggle-class="text-decoration-none"
                                no-caret
                            >
                                <template v-slot:button-content>
                                    <feather-icon
                                        icon="MoreVerticalIcon"
                                        size="16"
                                        class="text-body align-middle mr-25"
                                    />
                                </template>
                                <b-dropdown-item
                                    v-if="authorize(['ViewDevice'])"
                                    :to="{
                                        path: `/event/vehicleEvent/detail/${props.row.eventId}`,
                                    }"
                                >
                                    <feather-icon
                                        icon="EyeIcon"
                                        class="mr-50"
                                    />
                                    <span>Xem</span>
                                </b-dropdown-item>
                                <!-- <b-dropdown-item v-if="authorize(['ManageArea'])" @click="doDelete(props.row.id)">
                  <feather-icon icon="TrashIcon" class="mr-50" />
                  <span>Xóa</span>
                </b-dropdown-item> -->
                            </b-dropdown>
                        </span>
                    </span>
                </template>
            </BasicTable>
        </b-card>
    </div>
</template>

<script>
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'

export default {
    mixins: [authorizationMixin],
    components: {},
    data() {
        return {
            searchForm: {
                eventTypeId: '',
                dateFrom: '',
                dateTo: '',
                areaId: '',
                deviceId: '',
            },
            // define options
            options: [],
            columns: [
                {
                    label: 'Khu vực',
                    field: 'areaName',
                },
                {
                    label: 'Thiết bị',
                    field: 'deviceName',
                },
                {
                    label: 'Thời gian',
                    field: 'accessTimeStr',
                },
                {
                    label: 'Loại giám sát',
                    field: 'eventTypeName',
                },
                {
                    label: 'Ảnh',
                    field: 'image',
                },
                {
                    label: 'Thao tác',
                    field: 'action',
                },
            ],
            listArea: [],
            listDevice: [],
            listEventType: [],
        }
    },
    created() {
        debugger
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        this.loadArea()
        this.loadDevice()
        this.loadEventType()
    },
    methods: {
        search() {
            this.$refs.eventTable.refresh()
        },
        doDelete(item) {
            // confirm text
            this.$swal({
                title: 'Bạn chắc chắn muốn xóa?',
                text: 'Bản ghi này sẽ không thể khôi phục lại!',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Đồng ý!',
                cancelButtonText: 'Thoát',
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-outline-danger ml-1',
                },
                buttonsStyling: false,
            }).then((result) => {
                if (result.value) {
                    this.$services.delete('/areas/' + item).then((response) => {
                        this.$refs.eventTable.refresh()
                        this.$swal({
                            icon: 'success',
                            title: 'Xóa bản ghi thành công!',
                            text: '',
                            customClass: {
                                confirmButton: 'btn btn-success',
                            },
                        })
                    })
                }
            })
        },
        //lookup data
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                this.listEventType = response.data.data
            })
        },
        loadArea() {
            this.$services.get('/lookup/area').then((response) => {
                this.listArea = response.data.data
            })
        },
        loadDevice() {
            this.$services.get('/lookup/device').then((response) => {
                this.listDevice = response.data.data
            })
        },
    },
}
</script>

<style lang="scss"></style>
