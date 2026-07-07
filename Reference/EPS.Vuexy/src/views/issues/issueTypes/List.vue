<template>
  <div>
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <b-row>
                        <b-col sm="8" md="7" lg="6">
                            <b-form-group
                                :label="
                                    this.$t(
                                        'IssueTypes.List.SearchForm.IssueTypeId'
                                    )
                                "
                                label-for="h-searchForm-id-code"
                                label-cols="12"
                                label-cols-sm="auto"
                            >
                                <b-form-input
                                    id="h-searchForm-text"
                                    v-model="searchForm.id"
                                    type="text"
                                    :placeholder="
                                        this.$t(
                                            'IssueTypes.List.SearchForm.IssueTypeId'
                                        )
                                    "
                                />
                            </b-form-group>
                        </b-col>
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
            </b-card-body>
        </b-card>

        <b-card title="">
            <div>
                <b-button
                    v-if="authorize(['ManageIssueType'])"
                    variant="primary"
                    :to="{ path: '/issue/issueTypes/create' }"
                    style="margin-bottom: 15px"
                >
                    {{ this.$t('Button.Create') }}
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="issueTypeTable"
                :columns="columns"
                data-url="/issueType"
                :search-form="searchForm"
                storageName="issueTypeTable"
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

                    <!-- Column: Color -->
                    <span v-else-if="props.column.field === 'color'">
                        <b-badge
                            :style="{ 
                            backgroundColor: props.row.color, 
                            color: '#fff', 
                            padding: '5px 12px',
                            borderRadius: '20px',
                            fontWeight: 'bold'
                            }"
                        >
                            {{ props.row.color}}
                        </b-badge>
                    </span>

                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize(['ViewIssueType'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/issue/issueTypes/detail/${props.row.id}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageIssueType'])"
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
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'

export default {
    mixins: [authorizationMixin],
    components: { ToastificationContent },
    data() {
        return {
            searchForm: {
                id: null,
                compId: null,
            },
            // define options
            options: [],
            columns: [
                {
                    label: 'IssueTypes.List.Table.IssueTypeId',
                    field: 'id',
                },
                {
                    label: 'IssueTypes.List.Table.Name',
                    field: 'name',
                },
                {
                    label: 'IssueTypes.List.Table.Color',
                    field: 'color',
                },
                {
                    label: 'IssueTypes.List.Table.Operation',
                    field: 'action',
                },
            ],
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
    },
    methods: {
        search() {
            this.$refs.issueTypeTable.refresh()
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
                        .delete('/issueType/' + item)
                        .then((response) => {
                            this.$refs.issueTypeTable.refresh()
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
                        .catch(error => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: 'Error',
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: this.$t(
                                        `IssueTypes.error.${error.errorCode}`
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

<style>

</style>