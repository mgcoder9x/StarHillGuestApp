<template>
    <component
        :is="fieldComponent"
        :field-props="fieldProps"
        :value="value"
        @input="$emit('input', $event)"
    />
</template>

<script>
import { InputField, DatePicker } from '@/components/Form'

export default {
    name: 'DynamicFormField',
    components: {
        InputField,
        DatePicker,
    },
    props: {
        type: { type: String, required: true },
        value: { type: [String, Number, Date], default: null },
        fieldProps: { type: Object, default: () => ({}) },
    },
    computed: {
        fieldComponent() {
            const components = {
                string: 'InputField',
                int: 'InputField',
                datetime: 'DatePicker',
            }
            return (
                components[this.type.toString().toLowerCase()] || 'InputField'
            ) // Default là StringField
        },
    },
}
</script>

<style></style>
