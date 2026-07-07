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
                                :label="$t('System.Role.SearchForm.Name')"
                                label-for="h-searchForm-rolename"
                                label-cols-md="3"
                            >
                                <b-form-input
                                    id="h-searchForm-rolename"
                                    v-model.trim="searchForm.filterText"
                                    type="text"
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
                    v-if="authorize(['ManageRole'])"
                    variant="primary"
                    :to="{ path: '/systems/roles/create' }"
                    style="margin-bottom: 15px"
                >
                    {{ $t('Button.Create') }}
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="roleTable"
                :columns="columns"
                data-url="/roles"
                :search-form="searchForm"
                storage-name="roleTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: StatusName -->
                    <span v-if="props.column.field == 'statusName'">
                        {{ $t(props.row.statusName) }}
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <span>
                            <b-button
                                v-if="authorize(['ViewRole'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('common.button.detail')"
                                :to="{
                                    path: `/systems/roles/detail/${props.row.id}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageRole'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('common.button.setup-privileges')"
                                :to="{
                                    path: `/systems/roles/privileges/${props.row.id}`,
                                }"
                            >
                                <Icon icon="hugeicons:access" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageRole'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('common.button.delete')"
                                @click="doDelete(props.row.id)"
                            >
                                <Icon
                                    icon="fluent:delete-20-regular"
                                    class="xs-icon"
                                />
                            </b-button>
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
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    mixins: [authorizationMixin],
    components: {},
    data() {
        return {
            searchForm: {
                filterText: '',
            },
            columns: [
                {
                    label: 'System.Role.Field.Name',
                    field: 'name',
                },
                {
                    label: 'System.Role.Field.Description',
                    field: 'description',
                },
                {
                    label: 'System.Role.Field.Status',
                    field: 'statusName',
                },
                {
                    label: 'System.Role.Field.Action',
                    field: 'action',
                },
            ],
        }
    },
    methods: {
        search() {
            this.$refs.roleTable.refresh()
        },
        doDelete(item) {
            // confirm text
            this.$swal({
                title: this.$t('common.confirmation.delete.title'),
                text: this.$t('common.confirmation.delete.message'),
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: this.$t('common.button.confirm'),
                cancelButtonText: this.$t('common.button.cancel'),
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-outline-danger ml-1',
                },
                buttonsStyling: false,
            }).then((result) => {
                if (result.value) {
                    this.$services
                        .delete('/roles/' + item)
                        .then((response) => {
                            this.$refs.roleTable.refresh()
                            this.$swal({
                                icon: 'success',
                                title: this.$t(
                                    'System.Role.Message.DeleteSuccess'
                                ),
                                text: '',
                                customClass: {
                                    confirmButton: 'btn btn-success',
                                },
                            })
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t(
                                        'System.Role.Message.DeleteFailure'
                                    ),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    //text: `${error.message}`,
                                    text: this.$t(
                                        `System.Role.Message.${error.message}`
                                    ),
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
