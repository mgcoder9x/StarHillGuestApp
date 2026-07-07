<template>
    <validation-observer ref="rules">
        <div class="vgt-wrap">
            <vue-ads-table-tree
                ref="treeTable"
                :columns="columns"
                :classes="classes"
                :rows="rows"
            >
                <template slot="top">
                    <div></div>
                </template>
                <template slot="type" slot-scope="{ row }">
                    <select-field
                        v-if="!row._meta.parent"
                        v-model="row.type"
                        :classes="'custom-v-select'"
                        :name="'Type'"
                        :options="listType"
                        :keyword="'id'"
                        :rules="'required'"
                        :select-label="'title'"
                        :disabled="disabled"
                    >
                    </select-field>
                    <span v-else class="d-block text-left mb-md-1">{{
                        listType
                            .filter((x) => x.id === row.type)
                            .map((x) => $t(x.title))
                            .join('')
                    }}</span>
                </template>
                <template slot="conditionType" slot-scope="{ row }">
                    <select-field
                        v-if="row.type === 'group'"
                        v-model="row.conditionType"
                        :classes="'custom-v-select'"
                        :name="'ConditionType'"
                        :options="listCondition"
                        :keyword="'id'"
                        :rules="'required'"
                        :select-label="'title'"
                        :disabled="disabled"
                        @input="handleChangeConditionType(row)"
                    />
                </template>
                <template slot="property" slot-scope="{ row }">
                    <select-field
                        v-if="row.type === 'condition'"
                        v-model="row.property"
                        :classes="'custom-v-select'"
                        :name="'Property'"
                        :options="listProperty"
                        :keyword="'id'"
                        :rules="'required'"
                        :select-label="'text'"
                        :disabled="disabled"
                        @input="(prop) => handleChangeProperty(prop, row)"
                    />
                </template>
                <template slot="operator" slot-scope="{ row }">
                    <select-field
                        v-if="row.type === 'condition'"
                        v-model="row.operator"
                        :classes="'custom-v-select'"
                        :name="'Operator'"
                        :options="listOperator"
                        :keyword="'id'"
                        :rules="'required'"
                        :select-label="'title'"
                        :disabled="disabled"
                    >
                        <template #option="{ title, icon }">
                            <Icon v-if="icon" :icon="icon" class="xs-icon" />
                            <span v-else>
                                {{ title }}
                            </span>
                        </template>
                    </select-field>
                </template>
                <template slot="value" slot-scope="{ row }">
                    <b-form-group
                        :label-cols-md="0"
                        label-class="required"
                        :class="'mb-50 mb-md-1'"
                    >
                        <dynamic-form-field
                            v-if="row.type === 'condition'"
                            v-model="row.value"
                            :field-props="
                                getFieldProps(row.propertyType, row.property)
                            "
                            :type="row.propertyType || 'string'"
                        />
                    </b-form-group>
                </template>
                <template slot="action" slot-scope="{ row }">
                    <div v-if="!disabled">
                        <b-button
                            v-if="row.type === 'group'"
                            v-b-tooltip.hover
                            v-waves
                            class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                            variant="label-secondary"
                            :title="$t('Warning.Condition.ButtonTitle.Add')"
                            @click="addCondition(row)"
                        >
                            <Icon icon="material-symbols:add" class="xs-icon" />
                        </b-button>
                        <b-button
                            v-if="row.type === 'group' && !row._meta.parent"
                            v-b-tooltip.hover
                            v-waves
                            class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                            variant="label-secondary"
                            :title="
                                $t('Warning.Condition.ButtonTitle.AddGroup')
                            "
                            @click="addGroup(row)"
                        >
                            <Icon
                                icon="mdi:format-list-group-add"
                                class="xs-icon"
                            />
                        </b-button>
                        <b-button
                            v-if="row._meta.parent"
                            v-b-tooltip.hover
                            v-waves
                            class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                            variant="label-secondary"
                            :title="$t('Button.Delete')"
                            @click="deleteRow(row)"
                        >
                            <Icon
                                icon="streamline:recycle-bin-2"
                                class="xs-icon"
                            />
                        </b-button>
                    </div>
                </template>
            </vue-ads-table-tree>
        </div>
    </validation-observer>
</template>

<script>
import SelectField from '@/components/Form/SelectField.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import { ref } from '@vue/composition-api'
import { v4 as uuidv4 } from 'uuid'
import VueAdsTableTree from 'vue-ads-table-tree'
import { lookupService } from '@/services'
// import InputField from '@/components/Form/InputField.vue'
import DynamicFormField from '@/components/Form/DynamicFormField.vue'
import Vue from 'vue'
import NotiDataHelper from '@/utils/operator'

export default {
    components: { VueAdsTableTree, SelectField, DynamicFormField },
    mixins: [authorizationMixin],
    props: {
        eventType: {
            type: [String, Number],
            required: true,
        },
        data: {
            type: Array,
            default: () => [],
        },
        disabled: {
            type: Boolean,
            default: false,
        },
    },
    setup() {
        const listType = ref(NotiDataHelper.types)
        const listProperty = ref([])
        const listCondition = ref(NotiDataHelper.conditions)
        const listOperator = ref(NotiDataHelper.operators)
        const rows = ref([])
        const isEdit = ref(false)
        const listConditionDelete = ref([])
        return {
            listType,
            listProperty,
            listCondition,
            listOperator,
            rows,
            isEdit,
            listConditionDelete,
        }
    },
    data() {
        return {
            columns: [
                {
                    property: 'type',
                    title: this.$t('Warning.Condition.Grid.Column.GroupType'),
                    direction: '',
                    filterable: false,
                    sortable: false,
                    groupable: false,
                },
                {
                    property: 'conditionType',
                    title: this.$t(
                        'Warning.Condition.Grid.Column.ConditionType'
                    ),
                    direction: '',
                    filterable: false,
                    sortable: false,
                    groupable: false,
                    groupCollapsable: false,
                    hideOnGroup: true,
                },
                {
                    property: 'property',
                    title: this.$t('Warning.Condition.Grid.Column.Property'),
                    direction: '',
                    filterable: false,
                    sortable: false,
                    groupable: false,
                    groupCollapsable: false,
                    hideOnGroup: true,
                },
                {
                    property: 'operator',
                    title: this.$t('Warning.Condition.Grid.Column.Operator'),
                    direction: '',
                    filterable: false,
                    groupable: false,
                    groupCollapsable: false,
                    sortable: false,
                    hideOnGroup: true,
                },
                {
                    property: 'value',
                    title: this.$t('Warning.Condition.Grid.Column.Value'),
                    direction: '',
                    filterable: false,
                    groupable: false,
                    groupCollapsable: false,
                    sortable: false,
                    hideOnGroup: true,
                    width: '200px', // Set the column width
                },
                {
                    property: 'action',
                    title: this.$t('categories.areas.list.table.label.action'),
                    direction: null,
                    filterable: false,
                    groupable: false,
                },
            ],
            classes: {
                table: {
                    'default-font': true,
                    'vgt-table bordered ': true,
                },
                // 'all/0': {
                //     'cell-index': true,
                //     'text-center': true,
                // },
                // 'all/1': {
                //     'cell-index': true,
                //     'text-center': true,
                // },
                // 'all/2': {
                //     'cell-action': true,
                //     'text-center': true,
                // },
                '0_-0/4': {
                    'text-center': true,
                    'w-15': true,
                },
            },
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    watch: {
        eventType(newValue, oldVal) {
            if (newValue !== oldVal) {
                this.getProperties(newValue)
            }
        },
    },
    async created() {
        this.rows = this.data
        await this.getProperties(this.eventType)
        if (this.data.length > 0 && this.disabled) {
            this.isEdit = true
        }
        if (this.data.length > 0) {
            this.initRows(this.rows, null)
        } else {
            this.addGroup(this.rows)
        }
    },

    methods: {
        getFieldProps(propertyType, property) {
            const { OBJECT_TYPES } = NotiDataHelper
            const commonProps = {
                placeholder: this.$t('common.input.placeholder'),
                id: `h-notification-template-condition-value-${property?.toLowerCase()}-${uuidv4()}`,
                name: `Value ${property}`,
                disabled: this.disabled,
                rules: 'required',
            }
            const FieldProps = {
                // [OBJECT_TYPES.NUMBER]: {
                //     ...commonProps,
                //     type: 'number',
                //     key: 'id',
                //     classes: 'custom-v-select',
                //     options: this.getOptionsData(property),
                //     keyword: 'id',
                //     rules: 'required',
                //     selectLabel: 'title',
                // },
                [OBJECT_TYPES.DATETIME]: {
                    ...commonProps,
                    type: 'datetime',
                    disabled: this.disabled,
                    locale: this.currentLocale,
                    resetButton: true,
                },
                [OBJECT_TYPES.STRING]: {
                    ...commonProps,
                    type: 'text',
                },
            }
            return (
                FieldProps[propertyType?.toLowerCase()] ||
                FieldProps[OBJECT_TYPES.STRING]
            )
        },
        getOptionsData(property) {
            const {} = {}
        },
        handleChangeProperty(prop, row) {
            const propertyType = this.listProperty
                .filter((x) => x.id === prop)
                .map((x) => x.propertyType)
                .join('')
            // eslint-disable-next-line no-param-reassign
            row.propertyType = propertyType
        },
        handleChangeConditionType(row) {
            if (row.conditionType === 'group') {
                this.rows = []
                this.addGroup(this.rows)
            } else {
                this.rows = []
                this.addCondition(this.rows)
            }
        },
        async getProperties(eventType) {
            this.listProperty =
                await lookupService.getPropertiesClass(eventType)
        },
        sleep(ms) {
            return new Promise((resolve) => setTimeout(resolve, ms))
        },
        findRowById(rows, id) {
            // eslint-disable-next-line no-restricted-syntax
            for (const row of rows) {
                if (row.id === id) {
                    return row
                }
                // eslint-disable-next-line no-underscore-dangle
                if (row._children && row._children.length) {
                    // eslint-disable-next-line no-underscore-dangle
                    const found = this.findRowById(row._children, id)
                    if (found) {
                        return found
                    }
                }
            }
            return null
        },
        async addCondition(row) {
            await this.sleep(1000)
            const newRows = [
                {
                    id: uuidv4(),
                    type: 'condition',
                    conditionType: '',
                    property: '',
                    operator: '=',
                    value: '',
                },
            ]
            this.addRows(newRows, row)
        },
        addRows(newRows, parent) {
            // eslint-disable-next-line no-underscore-dangle
            const targetRow = this.findRowById(this.rows, parent.id)
            if (targetRow) {
                this.rowsChanged(newRows, parent)
                // Update the _children array reactively
                // eslint-disable-next-line no-underscore-dangle
                targetRow._children = [...targetRow._children, ...newRows]
            } else {
                // console.error('Row not found')
            }
            if (this.rows.length === 0) {
                this.rowsChanged(newRows, null)
                this.rows = [...newRows]
            }
        },
        async addGroup(row) {
            await this.sleep(1000)
            const newRows = [
                {
                    id: uuidv4(),
                    type: 'group',
                    conditionType: 'AND',
                    property: '',
                    operator: '',
                    value: '',
                },
            ]
            this.addRows(newRows, row)
        },
        initRows(rows, parent) {
            rows.forEach((row, index) => this.initRow(row, parent, index))
            // eslint-disable-next-line function-paren-newline, no-underscore-dangle
            rows.filter((row) => row._children.length > 0).forEach(
                // eslint-disable-next-line no-underscore-dangle
                (row) => this.rowsChanged(row._children, row)
                // eslint-disable-next-line function-paren-newline
            )

            return rows
        },
        rowsChanged(rows, parent) {
            this.initRows(rows, parent)
        },
        initRow(row, parent, index, groupColumn = null) {
            // eslint-disable-next-line no-prototype-builtins
            if (!row.hasOwnProperty('_children')) {
                Vue.set(row, '_children', [])
            }

            // eslint-disable-next-line no-prototype-builtins, no-underscore-dangle
            if (!row.hasOwnProperty('_showChildren') || !row._showChildren) {
                Vue.set(row, '_showChildren', true)
            }
            if (row.property) {
                const propType = this.listProperty
                    .filter((x) => x.id === row.property)
                    .map((x) => x.propertyType)[0]
                Vue.set(row, 'propertyType', propType)
            } else {
                Vue.set(row, 'propertyType', 'string')
            }
            // eslint-disable-next-line no-prototype-builtins, no-underscore-dangle
            if (!row.hasOwnProperty('_selectable') || !row._selectable) {
                const selectable =
                    // eslint-disable-next-line no-prototype-builtins
                    parent && parent.hasOwnProperty('_selectable')
                        ? // eslint-disable-next-line no-underscore-dangle
                          parent._selectable
                        : this.selectable
                Vue.set(row, '_selectable', selectable)
            }

            // eslint-disable-next-line no-prototype-builtins
            if (!row.hasOwnProperty('_meta')) {
                Vue.set(row, '_meta', {
                    groupParent: 0,
                    // eslint-disable-next-line no-underscore-dangle
                    parent: parent ? parent._meta.parent + 1 : 0,
                    uniqueIndex: uuidv4(),
                    loading: false,
                    // eslint-disable-next-line no-underscore-dangle
                    visibleChildren: row._children,
                    index,
                    groupColumn,
                    selected: false,
                })
            }
        },
        getQuery() {
            return new Promise((resolve, reject) => {
                this.$refs.rules.validate().then((success) => {
                    debugger
                    if (success) {
                        if (this.validateQuery()) {
                            resolve(this.getData())
                        } else {
                            reject(new Error('Validation failed'))
                        }
                    } else {
                        reject(new Error('Validation failed'))
                    }
                })
            })
        },
        deleteRow(row) {
            const removeRow = (rows) => {
                let rowDeleted = false
                const updatedRows = rows.filter((r) => {
                    if (r.id === row.id) {
                        if (row.parentId) {
                            this.listConditionDelete.push(row.id)
                        }
                        rowDeleted = true
                        return false // Exclude the row to be deleted
                    }
                    // Process child rows recursively
                    // eslint-disable-next-line no-underscore-dangle
                    if (r._children) {
                        // eslint-disable-next-line no-underscore-dangle, no-param-reassign
                        r._children = removeRow(r._children)
                    }
                    return true // Keep the current row
                })

                return rowDeleted ? updatedRows : rows
            }

            // Create a new array reference for `this.rows`
            this.rows = removeRow(this.rows)
        },
        validateQuery() {
            return true
        },
        getData() {
            return {
                data: this.rows,
                listConditionDelete: this.listConditionDelete,
            }
        },
        getRowType(index, parentId) {
            if (parentId) {
                // do something
                return 0
            }
            return this.rows[index].map((x) => x.type)[0]
        },
        // getOptionsData(property){
        //     const optionsData = {

        //     }
        // }
    },
}
</script>

<style lang="scss">
@import 'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css';
.default-font {
    font-family: 'Montserrat', Helvetica, Arial, serif;
}

.swal2-styled.swal2-confirm.btn-label-primary {
    color: #7367f0;
    background: #e9e7fd;
}
.swal2-styled.swal2-cancel.btn-label-danger {
    color: #ea5455;
    background: #fad6d6;
}
.vgt-wrap {
    .vue-ads-flex.vue-ads-m-2.vue-ads-px-0.vue-ads-text-xs {
        font-size: 1rem;
    }
    .vue-ads-cursor-pointer {
        display: flex !important;
        gap: 5px;
        align-items: center;
        width: 100%;
        > i {
            flex: 0;
            margin-bottom: 1rem;
        }
        > span {
            flex: 1 1 auto;
        }
    }
}

body {
    &.dark-layout {
        button.vue-ads-ml-1.vue-ads-leading-normal.vue-ads-w-6 {
            background: #424659;
            border-color: rgba(0, 0, 0, 0);
            color: #a8aaae;
        }
    }
}
.custom-v-select {
    position: relative;
    .vs__dropdown-menu {
        max-height: 200px !important;
        overflow-y: auto;
    }
    .vs__open-indicator {
        display: none;
    }
    input {
        border: none !important;
    }
}
.w-15 {
    width: 20%;
    label {
        display: -webkit-box;
        -webkit-box-orient: vertical;
        -webkit-line-clamp: 3;
        overflow: hidden;
    }
}
</style>
