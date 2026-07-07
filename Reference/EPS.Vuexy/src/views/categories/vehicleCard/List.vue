<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <!-- advance search input -->
                    <b-row>
                        <b-col md="4">
                            <b-form-group
                                :label="
                                    this.$t(
                                        'categories.vehicleCards.list.searchForm.label.cardId'
                                    )
                                "
                                label-for="h-searchForm-code-name"
                                label-cols-md="3"
                            >
                                <b-form-input
                                    id="h-searchForm-text"
                                    v-model="searchForm.filterText"
                                    type="text"
                                    :placeholder="
                                        this.$t(
                                            'categories.vehicleCards.list.searchForm.placeholder.cardId'
                                        )
                                    "
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="4">
                            <b-form-group
                                :label="
                                    this.$t(
                                        'categories.vehicleCards.list.searchForm.label.type'
                                    )
                                "
                                label-for="h-searchForm-status"
                                label-cols-md="3"
                            >
                                <v-select
                                    v-model="searchForm.type"
                                    :placeholder="
                                        this.$t(
                                            'categories.vehicleCards.list.searchForm.placeholder.type'
                                        )
                                    "
                                    label="text"
                                    :reduce="(type) => type.value"
                                    :options="typeList"
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>

                        <b-col md="4">
                            <b-form-group
                                :label="
                                    this.$t(
                                        'categories.vehicleCards.list.searchForm.label.status'
                                    )
                                "
                                label-for="h-searchForm-status"
                                label-cols-md="3"
                            >
                                <v-select
                                    v-model="searchForm.status"
                                    :placeholder="
                                        this.$t(
                                            'categories.vehicleCards.list.searchForm.placeholder.status'
                                        )
                                    "
                                    label="text"
                                    :reduce="(status) => status.value"
                                    :options="statusList"
                                    multiple
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
                    v-if="authorize([authorizeName.manage])"
                    variant="primary"
                    :to="{ path: routerLink.create }"
                    style="margin-bottom: 15px"
                >
                    {{ this.$t('Button.Create') }}
                </b-button>
            </div>
            <!-- table -->
            <BasicTable2
                ref="vehicleCardTable"
                :columns="isVietnamese ? table.columns : table.columns2"
                :data-url="table.dataUrl"
                :search-form="searchForm"
                storage-name="vehicleCardTable"
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
                                @click="onDelete(row.id)"
                            >
                                <Icon
                                    icon="fluent:delete-20-regular"
                                    class="xs-icon"
                                />
                            </b-button>
                        </div>
                    </span>
                </template>
            </BasicTable2>
        </b-card>
    </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    // components: { ToastificationContent },
    mixins: [authorizationMixin],
    data() {
        return {
            searchForm: {
                filterText: null,
                type: null,
                status: null,
            },
            table: {
                dataUrl: '/vehicle-cards',
                columns: [
                    {
                        label: 'categories.vehicleCards.list.table.label.cardId',
                        field: 'cardId',
                    },
                    {
                        label: 'categories.vehicleCards.list.table.label.cardCode',
                        field: 'cardName',
                    },
                    {
                        label: 'categories.vehicleCards.list.table.label.cardType',
                        field: 'typeName',
                    },
                    {
                        label: 'categories.vehicleCards.list.table.label.status',
                        field: 'statusName',
                    },
                    {
                        label: 'categories.vehicleCards.list.table.label.note',
                        field: 'note',
                    },
                    {
                        label: 'categories.vehicleCards.list.table.label.action',
                        field: 'action',
                    },
                ],
                columns2: [
                    {
                        label: 'categories.vehicleCards.list.table.label.cardId',
                        field: 'cardId',
                    },
                    {
                        label: 'categories.vehicleCards.list.table.label.cardCode',
                        field: 'cardName',
                    },
                    {
                        label: 'categories.vehicleCards.list.table.label.cardType',
                        field: 'typeName',
                    },
                    {
                        label: 'categories.vehicleCards.list.table.label.status',
                        field: 'enStatusName',
                    },
                    {
                        label: 'categories.vehicleCards.list.table.label.note',
                        field: 'note',
                    },
                    {
                        label: 'categories.vehicleCards.list.table.label.action',
                        field: 'action',
                    },
                ],
            },
            authorizeName: {
                manage: 'ManageVehicleCard',
                view: 'ViewVehicleCard',
            },
            routerLink: {
                create: '/categories/vehicle-cards/create/',
                detail: '/categories/vehicle-cards/detail/',
                delete: '/vehicle-cards/',
            },
        }
    },
    computed: {
        isVietnamese() {
            return this.$i18n.locale === 'vi'
        },
        statusList() {
            return [
                {
                    text: this.$t(
                        'categories.vehicleCards.enums.status.active'
                    ),
                    value: 1,
                },
                {
                    text: this.$t(
                        'categories.vehicleCards.enums.status.broken'
                    ),
                    value: 2,
                },
                {
                    text: this.$t('categories.vehicleCards.enums.status.lost'),
                    value: 3,
                },
            ]
        },
        typeList() {
            return [
                { text: 'NFC', value: 1 },
                { text: 'ETC', value: 2 },
            ]
        },
    },
    methods: {
        handleBinding(event) {
            this.searchForm[event.filterName] = event.value
            if (event.autoSearch) this.search()
        },
        search() {
            this.$refs.vehicleCardTable.refresh()
        },
        onDelete(item) {
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
                            this.$refs.vehicleCardTable.refresh()
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Delete'),
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
                                    // text: '',
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: this.$t(`${error.message}`),
                                },
                            })
                        })
                }
            })
        },

        initialDropdowns() {
            this.searchDatas[1].options = [
                { value: '1', text: 'Loại 1' },
                { value: '2', text: 'Loại 2' },
                { value: '3', text: 'Loại 3' },
            ]
            this.searchDatas[2].options = [
                { value: '1', text: 'Loại 1' },
                { value: '2', text: 'Loại 2' },
                { value: '3', text: 'Loại 3' },
            ]
        },
    },
}
</script>

<style lang="scss"></style>
