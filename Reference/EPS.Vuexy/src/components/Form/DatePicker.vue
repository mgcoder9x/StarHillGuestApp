<template>
    <validation-provider
        v-slot="{ errors, validate }"
        v-model="localValue"
        :name="fieldProps.name"
        :rules="fieldProps.rules"
    >
        <b-form-datepicker
            v-model="localValue"
            :state="errors.length > 0 ? false : null"
            v-bind="fieldProps"
            @input="onInput"
            @blur="validate"
        >
        </b-form-datepicker>
        <small v-if="errors.length > 0" class="text-danger">
            {{ errors[0] }}
        </small>
    </validation-provider>
</template>

<script>
export default {
    props: {
        value: {
            type: [String, Number],
            default: '',
        },
        fieldProps: {
            type: Object,
            default: () => ({}),
        },
    },
    data() {
        return {
            localValue: this.value,
            state: null, // Trạng thái của input
        }
    },
    watch: {
        value(newValue) {
            this.localValue = newValue
        },
        error(newError) {
            this.state = newError ? false : null // Kiểm tra lỗi và cập nhật trạng thái
        },
    },
    created() {},
    methods: {
        onInput($event) {
            this.$emit('input', $event)
        },
    },
}
</script>

<style></style>
