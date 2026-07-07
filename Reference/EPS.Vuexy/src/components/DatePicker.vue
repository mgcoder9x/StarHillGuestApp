<template>
    <DatePicker
        v-bind="forwardAttrs"
        :value="modelValue"
        class="custom-datepicker"
        v-on="forwardListeners"
        @input="updateValue"
    />
</template>

<script>
import DatePicker from 'vue2-datepicker'
import 'vue2-datepicker/index.css' // Import default styles

export default {
    components: { DatePicker },
    inheritAttrs: false,
    model: {
        prop: 'value',
        event: 'input',
    },
    props: {
        value: [String, Date, Array], // Supports different date formats
    },
    computed: {
        modelValue() {
            return this.value // Correctly binding v-model
        },
        forwardAttrs() {
            return { ...this.$attrs }
        },
        forwardListeners() {
            return { ...this.$listeners }
        },
    },
    methods: {
        updateValue(newValue) {
            this.$emit('input', newValue) // Ensures proper v-model updates
        },
    },
}
</script>

<style lang="scss">
@use '../assets/scss/abstracts/variables.scss' as *;
@import '~@core/scss/base/bootstrap-extended/include';
// // Overrides user variable
@import '~@core/scss/base/components/include';
.dark-layout {
    .custom-datepicker {
        input {
            background: $theme-dark-input-bg !important;
            color: $theme-dark-body-color !important;
            border-color: $theme-dark-input-border-color !important;
        }
        .mx-icon-calendar {
            color: $theme-dark-body-color;
            font-size: 0.9375rem;
        }
    }
    .mx-datepicker-main {
        background-color: $theme-dark-input-bg !important;
        border-radius: 10px;
        border-color: $theme-dark-input-border-color !important;
        .mx-calendar-content {
            .mx-table-date {
                .active {
                    background: $v-blue-atin !important;
                    color: white !important;
                    border-radius: 50%;
                }
                td:not(.not-current-month):hover {
                    background: $theme-dark-input-border-color;
                    color: white;
                    border-radius: 50%;
                }
            }
        }
        .mx-time.mx-calendar-time {
            background-color: $theme-dark-input-bg !important;
            border-color: $theme-dark-input-border-color !important;
            color: $theme-dark-body-color !important;
            .mx-time-header,
            .mx-time-column {
                border-color: $theme-dark-input-border-color !important;
            }
            .mx-time-item {
                &:hover {
                    background: $theme-dark-input-border-color;
                }
            }
        }
    }

    /* Time picker columns (hour, minute, second) */
    .mx-btn:hover {
        color: $theme-dark-body-color !important;
    }
}
</style>
