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
                                :label="this.$t('Warning.List.SearchForm.Name')"
                                :placeholder="
                                    this.$t('Warning.List.SearchForm.Name')
                                "
                                label-for="h-searchForm-name"
                                label-cols-md="3"
                            >
                                <b-form-input
                                    id="h-searchForm-text"
                                    v-model="searchForm.name"
                                    :placeholder="
                                        this.$t('Warning.List.SearchForm.Name')
                                    "
                                    type="text"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    this.$t('Warning.List.SearchForm.EventType')
                                "
                                :placeholder="
                                    this.$t('Warning.List.SearchForm.EventType')
                                "
                                label-for="h-searchForm-eventTypeId"
                                label-cols-md="3"
                            >
                                <v-select
                                    v-model="searchForm.eventTypeId"
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    label="text"
                                    :reduce="(eventType) => eventType.id"
                                    :options="listEventType"
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
                    >
                        Search
                    </b-button>
                </b-form>
            </b-card-body>
        </b-card>

        <b-card title="">
            <div>
                <b-button
                    v-if="authorize(['ManageEventTemplate'])"
                    variant="primary"
                    :to="{ path: '/notification/template/create' }"
                    style="margin-bottom: 15px"
                >
                    {{ this.$t('Button.Create') }}
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="notificationTable"
                :columns="columns"
                data-url="/notification/getAllNotificationEventTemplates"
                :search-form="searchForm"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Action -->
                    <span v-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize(['ViewEventTemplate'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('common.button.detail')"
                                :to="{
                                    path: `/notification/template/detail/${props.row.id}`,
                                }"
                            >
                                <feather-icon icon="EyeIcon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageEventTemplate'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('common.button.delete')"
                                @click="doDelete(props.row.id)"
                            >
                                <feather-icon icon="TrashIcon" />
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
    components: {},
    data() {
        return {
            searchForm: {
                name: '',
                eventTypeId: null,
                companyId: null,
            },
            listEventType: [],
            columns: [
                {
                    label: 'Warning.List.Table.Name',
                    field: 'name',
                },
                {
                    label: 'Warning.List.Table.Title',
                    field: 'title',
                },
                {
                    label: 'Warning.List.Table.Content',
                    field: 'content',
                },
                {
                    label: 'Warning.List.Table.EventType',
                    field: 'eventTypeName',
                },
                {
                    label: 'Warning.List.Table.Link',
                    field: 'url',
                },
                {
                    label: 'Warning.List.Table.Operation',
                    field: 'action',
                },
            ],
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.companyId = accessToken.companyId
        this.loadEventType()
    },
    methods: {
        search() {
            this.$refs.notificationTable.refresh()
        },
        //lookup data
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                this.listEventType = response.data.data
            })
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
                        .delete(
                            '/notification/notificationEventTemplate/' + item
                        )
                        .then((response) => {
                            this.$refs.notificationTable.refresh()
                            this.$swal({
                                icon: 'success',
                                title: this.$t('Success.Delete'),
                                text: '',
                                customClass: {
                                    confirmButton: 'btn btn-success',
                                },
                            })
                        })
                        .catch((error) => {
                            debugger
                            console.log(error)
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Warning'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${this.$t(error.message)}`,
                                },
                            })
                        })
                }
            })
        },
    },
}
</script>

<style lang="scss"></style>
