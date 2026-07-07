<template>
    <!-- <b-card> -->
    <b-row align-v="center">
        <b-col md="6">
            <b-form-group
                :label="`${$t('Meiko.Dashboard.Filters.Date')}:`"
                label-cols="3"
                label-for="date-input"
            >
                <date-picker
                    id="h-selected-date"
                    :value="selectedDate"
                    type="datetime"
                    :locale="currentLocale"
                    format="DD-MM-YYYY"
                    value-type="YYYY-MM-DD"
                    style="width: 100%"
                    @input="handleDateChange"
                ></date-picker>
            </b-form-group>
        </b-col>
        <b-col md="6">
            <b-form-group
                :label="`${$t('Meiko.Dashboard.Filters.Shift')}:`"
                label-cols="3"
                label-for="shift-select"
            >
                <b-form-select
                    id="shift-select"
                    :value="selectedShift"
                    :options="shiftOptions"
                    @change="handleShiftChange"
                />
            </b-form-group>
        </b-col>
    </b-row>
    <!-- </b-card> -->
</template>

<script>
import { mapState, mapMutations } from 'vuex'

export default {
    name: 'ShiftFilters',
    computed: {
        ...mapState('dashboard', [
            'selectedDate',
            'selectedShift',
            'shiftOptions',
        ]),
        currentLocale() {
            return this.$i18n.locale
        },
    },
    methods: {
        ...mapMutations('dashboard', [
            'SET_SELECTED_DATE',
            'SET_SELECTED_SHIFT',
        ]),
        handleDateChange(value) {
            this.SET_SELECTED_DATE(value)
            this.handleFilterChange()
        },
        handleShiftChange(value) {
            this.SET_SELECTED_SHIFT(value)
            this.handleFilterChange()
        },
        handleFilterChange() {
            this.$emit('filter-changed')
        },
    },
}
</script>
