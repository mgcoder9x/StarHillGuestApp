<template>
    <b-col :md="md">
        <b-form-group :label="$t(label)" :label-cols-md="labelColsMd">
            <tree-select
                v-if="searchType == 'tree-select'"
                v-model="localValue"
                :options="rechangeOptions(options)"
                label="text"
                :reduce="(option) => option.id"
                :placeholder="$t(placeholder)"
                :multiple="multiple"
                :value-consists-of="valueConsistsOf"
                :limit="3"
                :limit-text="(count) => `+${count}`"
                class="treeselect-nowrap"
                @input="handleBinding"
            />

            <b-form-input
                v-if="searchType == 'b-form-input'"
                v-model="localValue"
                :placeholder="this.$t(placeholder)"
                type="text"
                @input="handleBinding"
            />

            <v-select
                v-if="searchType == 'v-select'"
                v-model="localValue"
                :options="rechangeOptions(options)"
                label="text"
                :placeholder="this.$t(placeholder)"
                type="text"
                :reduce="(option) => option.id"
                :multiple="multiple"
                :clearable="clearable"
                @input="handleBinding"
            />

            <b-form-datepicker
                v-if="searchType == 'b-form-datepicker'"
                v-model="localValue"
                :date-format-options="dateFormatOptions"
                reset-button
                type="datetime"
                format="DD-MM-YYYY HH:mm:ss"
                value-type="YYYY-MM-DD HH:mm:ss"
                :locale="locale"
                :placeholder="this.$t(placeholder)"
                @input="handleBinding"
            />
            <date-picker
                v-if="searchType == 'date-picker'"
                v-model="localValue"
                :date-format-options="dateFormatOptions"
                reset-button
                type="datetime"
                format="DD-MM-YYYY HH:mm:ss"
                value-type="YYYY-MM-DD HH:mm:ss"
                style="width: 100%"
                :locale="locale"
                :placeholder="this.$t(placeholder)"
                @input="handleBinding"
            />
        </b-form-group>
    </b-col>
</template>

<script>
export default {
    props: {
        // Số cột của label trên màn hình cha
        md: {
            type: Number,
            default: 6,
        },
        // Label của group form
        label: {
            type: String,
            required: true,
        },
        // Số cột của label trên màn hình md
        labelColsMd: {
            type: Number,
            default: 3,
        },
        // Placeholder của input
        placeholder: {
            type: String,
            // default: 'common.form.placeholder.selectValue',
            default: '',
        },
        // Giá trị bắt đầu của input
        modelValue: {
            type: [String, Number],
            default: null,
        },
        // Loại hiển thị của input (b-form-input, tree-select, v-select)
        searchType: {
            type: String,
            default: 'b-form-input',
        },
        // Danh sách option cho v-select hoặc tree-select
        options: {
            type: Array,
            default: () => [],
        },
        // Tên của key muốn binding với modelValue
        filterName: {
            type: String,
            required: true,
        },
        autoSearch: {
            type: Boolean,
            default: false,
        },
        multiple: {
            type: Boolean,
            default: false,
        },
        valueConsistsOf: {
            type: String,
            default: 'ALL',
        },
        locale: {
            type: String,
            default: 'vi',
        },
        dateFormatOptions: {
            type: Object,
            default: () => {},
        },
        clearable: {
            type: Boolean,
            default: true,
        },
    },
    data() {
        return {
            localValue: this.modelValue, // Sao chép giá trị ban đầu vào một biến cục bộ
        }
    },
    watch: {
        modelValue: {
            immediate: true,
            handler(newValue) {
                this.localValue = newValue // Đồng bộ lại khi `modelValue` thay đổi từ parent
            },
        },
    },
    methods: {
        rechangeOptions(dataList) {
    if (!Array.isArray(dataList)) return []
    return dataList.map((item) => ({
        ...item,
        text: this.$t(item.text),
    }))
},
        handleBinding() {
            this.$emit('handle-binding', {
                value: this.localValue,
                filterName: this.filterName,
                autoSearch: this.autoSearch,
            })
        },
    },
}
</script>

<style lang="scss" scoped>
@import '@/assets/scss/_custom-tree-select.scss';
/* Add your custom styles here if needed */
</style>
