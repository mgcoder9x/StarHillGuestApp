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
                                    this.$t(
                                        'categories.controllers.list.searchForm.label.filterText'
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
                                            'categories.controllers.list.searchForm.placeholder.filterText'
                                        )
                                    "
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
            <BasicTable
                ref="controllerTable"
                :columns="table.columns"
                :data-url="table.dataUrl"
                :search-form="searchForm"
                storage-name="controllerTable"
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
            </BasicTable>
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
            },
            table: {
                dataUrl: '/controllers',
                columns: [
                    {
                        label: 'categories.controllers.list.table.label.code',
                        field: 'code',
                    },
                    {
                        label: 'categories.controllers.list.table.label.name',
                        field: 'name',
                    },
                    {
                        label: 'categories.controllers.list.table.label.ip',
                        field: 'ip',
                    },
                    {
                        label: 'categories.controllers.list.table.label.action',
                        field: 'action',
                    },
                ],
            },
            authorizeName: {
                manage: 'ViewController',
                view: 'ManageController',
            },
            routerLink: {
                create: '/categories/controllers/create/',
                detail: '/categories/controllers/detail/',
                delete: '/controllers/',
            },
        }
    },
    methods: {
        handleBinding(event) {
            this.searchForm[event.filterName] = event.value
            if (event.autoSearch) this.search()
        },
        search() {
            this.$refs.controllerTable.refresh()
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
                            this.$refs.controllerTable.refresh()
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
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: this.$t(`${error.message}`),
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
