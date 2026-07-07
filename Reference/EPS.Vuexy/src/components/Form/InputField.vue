<!-- CustomInput.vue -->
<template>
    <!-- <b-form-group
        :label="label"
        :label-cols-md="labelCol"
        label-class="required"
        :class="formGroupClass"
    > -->
    <validation-provider
        v-slot="{ errors, validate }"
        :name="fieldProps.name"
        :rules="fieldProps.rules"
    >
        <b-form-input
            v-model="localValue"
            :state="errors.length > 0 ? false : null"
            v-bind="fieldProps"
            @input="$emit('input', $event)"
            @blur="validate"
        />
        <small v-if="errors.length > 0" class="text-danger">{{
            errors[0]
        }}</small>
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
}
</script>

<style scoped></style>
