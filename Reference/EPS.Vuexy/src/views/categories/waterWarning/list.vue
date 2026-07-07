<template>
    <div>
        <b-card>
            <b-card-body>
                <b-form>
                    <b-row>
                        <b-col cols="6">
                            <b-form-group :label="$t('categories.waterWarning.Label.search.name')" label-cols-md="3">
                                <b-form-input v-model="searchString.name" @input="refresh" />
                            </b-form-group>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>

        <!-- Table -->
        <b-card title="">
            <div class="d-flex mb-2">
                <button class="btn btn-primary" v-if="authorize(['ManageWaterWarning'])"
                    @click="$router.push({ name: 'water-warning-create' })">
                    {{ $t('categories.waterWarning.Label.button.add') }}
                </button>
            </div>
            <!-- table -->
            <BasicTable ref="eventWaterTable" :columns="Columns" data-url="/water-warning" :search-form="searchString"
                :sort-by="'name'" storageName="waterWarningTable">
                <template slot="table-row" slot-scope="props">
                    <span v-if="props.column.field == 'name'" :style="ColumnsStyle">
                        {{ $t(props.row.name) }}
                    </span>
                    <span v-else-if="props.column.field == 'minValue'" :style="ColumnsStyle">
                        {{ props.row.minValue }}
                    </span>
                    <span v-else-if="props.column.field == 'maxValue'" :style="ColumnsStyle">
                        {{ props.row.maxValue }}
                    </span>
                    <span v-else-if="props.column.field == 'description'" :style="ColumnsStyle">
                        {{ $t(props.row.description) }}
                    </span>

                    <span v-else-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button v-b-tooltip.hover v-waves variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon" :title="$t(
                                    'categories.waterWarning.Label.button.detail'
                                )
                                    " :to="{
                                    path: `/categories/water-Warning/detail/${props.row.id}`,
                                }" v-if="authorize(['ManageWaterWarning'])">
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button v-if="authorize(['ManageWaterWarning'])" v-b-tooltip.hover v-waves
                                variant="label-secondary" class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon" :title="$t(
                                    'categories.waterWarning.Label.button.delete'
                                )
                                    " @click="doDelete(props.row.id)">
                                <Icon icon="fluent:delete-20-regular" class="xs-icon" />
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

export default {
    mixins: [authorizationMixin],
    data() {
        return {
            searchString: {
                name: null,
            },

            ColumnsStyle: {
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
            },
        }
    },

    computed: {
        Columns() {
            return [
                {
                    label: this.$t('categories.waterWarning.Field.Name'),
                    field: 'name',
                },
                {
                    label: this.$t('categories.waterWarning.Field.MinValue'),
                    field: 'minValue',
                },
                {
                    label: this.$t('categories.waterWarning.Field.MaxValue'),
                    field: 'maxValue',
                },
                {
                    label: this.$t('categories.waterWarning.Field.Description'),
                    field: 'description',
                },
                {
                    label: this.$t('categories.waterWarning.Field.Action'),
                    field: 'action',
                },
            ]
        },
    },

    methods: {
        refresh() {
            this.$refs.eventWaterTable.refresh()
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
            }).then((result) => {
                if (result.value) {
                    this.$services
                        .delete(`/water-warning/${id}`)
                        .then((responce) => {
                            this.refresh()
                            this.$swal({
                                icon: 'success',
                                title: this.$t('Success.Delete'),
                                text: '',
                                customClass: {
                                    confirmButton: 'btn btn-success',
                                },
                            })
                        })
                }
            })
        },
    },
}
</script>

<style></style>
