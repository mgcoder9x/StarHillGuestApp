<template>
    <validation-provider
        v-slot="{ errors, validate }"
        :name="name"
        :rules="rules"
    >
        <b-form-group
            :label="label"
            :label-cols-md="labelCol"
            label-class="required"
            :class="formGroupClass"
        >
            <v-select
                :id="name"
                v-model="localValue"
                :class="classes"
                :reduce="assignValue"
                :dir="$store.state.appConfig.isRTL ? 'rtl' : 'ltr'"
                :label="selectLabel"
                :options="rechangeOptions(options)"
                :disabled="disabled"
                @input="onInput"
                @blur="validate"
            >
                <template #option="{ ...slotProps }">
                    <slot name="option" v-bind="slotProps" />
                </template>
            </v-select>
            <small v-if="errors" class="text-danger">{{ errors[0] }}</small>
        </b-form-group>
    </validation-provider>
</template>
<script>
export default {
    name: 'SelectField',
    props: {
        name: {
            type: String,
            required: true,
        },
        label: {
            type: String,
            default: '',
        },
        selectLabel: {
            type: String,
            default: '',
        },
        options: {
            type: Array,
            required: true,
        },
        rules: {
            type: [String, Object],
            required: true,
        },
        value: {
            type: [String, Number, Array],
            default: '',
        },
        labelCol: {
            type: String,
            default: '0',
        },
        classes: {
            type: String,
            default: '',
        },
        keyword: {
            type: String,
            required: true,
        },
        disabled: {
            type: Boolean,
            default: false,
        },
    },
    data() {
        return {
            localValue: this.value,
        }
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
    },
    watch: {
        value(newValue) {
            this.localValue = newValue
        },
    },
    methods: {
        assignValue(item) {
            return item[this.keyword.toString()]
        },
        onInput() {
            this.$emit('input', this.localValue)
        },
        rechangeOptions(dataList) {
            return dataList.map((item) => ({
                ...item,
                title: this.$t(item.title),
            }))
        },
    },
}
</script>
<style></style>
