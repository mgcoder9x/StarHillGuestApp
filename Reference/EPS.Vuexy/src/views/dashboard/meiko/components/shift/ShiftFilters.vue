<template>
    <div class="sf-wrap d-flex align-items-center flex-wrap" style="gap:10px">
        <small class="text-muted font-weight-500 text-nowrap">
            {{ $t('Meiko.Dashboard.Filters.Label') }}:
        </small>

        <!-- Date picker -->
        <div class="d-flex align-items-center">
            <small class="text-muted mr-1 text-nowrap font-weight-500">
                {{ $t('Meiko.Dashboard.Filters.Date') }}
            </small>
            <date-picker
                :value="selectedDate"
                type="date"
                :locale="currentLocale"
                format="DD/MM/YYYY"
                value-type="YYYY-MM-DD"
                style="width: 130px"
                @input="handleDateChange"
            />
        </div>

        <!-- Shift selector -->
        <div class="d-flex align-items-center">
            <small class="text-muted mr-1 text-nowrap font-weight-500">
                {{ $t('Meiko.Dashboard.Filters.Shift') }}
            </small>
            <b-form-select
                :value="selectedShift"
                :options="shiftSelectOptions"
                size="sm"
                style="min-width: 160px; max-width: 220px"
                @change="handleShiftChange"
            >
                <template #first>
                    <b-form-select-option :value="null" disabled>
                        {{ $t('Meiko.Dashboard.Filters.SelectShift') }}
                    </b-form-select-option>
                </template>
            </b-form-select>
        </div>

        <!-- Apply button -->
        <b-button
            variant="primary"
            size="sm"
            :disabled="isLoading || !selectedShift"
            class="px-3"
            @click="apply"
        >
            <b-spinner v-if="isLoading" small class="mr-1" />
            <Icon
                v-else
                icon="lucide:refresh-cw"
                width="13"
                height="13"
                class="mr-1"
            />
            {{ $t('Meiko.Dashboard.Filters.Update') }}
        </b-button>

        <!-- "Đang xem" label -->
        <small v-if="viewingLabel" class="text-muted text-nowrap ml-2 sf-viewing">
            {{ $t('Meiko.Dashboard.Filters.Viewing') }}:
            <strong>{{ viewingLabel }}</strong>
        </small>
    </div>
</template>

<script>
import { mapState, mapActions } from 'vuex'
import moment from 'moment'

export default {
    name: 'ShiftFilters',
    props: {
        isLoading: {
            type: Boolean,
            default: false,
        },
    },
    computed: {
        ...mapState('dashboard', [
            'selectedDate',
            'selectedShift',
            'shiftOptions',
            'shiftOptionsLoaded',
        ]),
        currentLocale() {
            return this.$i18n.locale
        },
        shiftSelectOptions() {
            return (this.shiftOptions || []).map((s) => ({
                value: s.id,
                text: s.text,
            }))
        },
        viewingLabel() {
            if (!this.selectedDate || !this.selectedShift) return null
            const shift = (this.shiftOptions || []).find((s) => s.id === this.selectedShift)
            if (!shift) return null
            const dateFormatted = moment(this.selectedDate, 'YYYY-MM-DD').format('YYYY-MM-DD')
            return `${dateFormatted} - ${shift.text}`
        },
    },
    async created() {
        if (!this.shiftOptionsLoaded) {
            await this.loadShiftOptions()
        }
    },
    methods: {
        ...mapActions('dashboard', ['setSelectedDate', 'setSelectedShift', 'loadShiftOptions']),
        handleDateChange(val) {
            this.setSelectedDate(val)
        },
        handleShiftChange(val) {
            this.setSelectedShift(val)
        },
        apply() {
            this.$emit('filter-changed')
        },
    },
}
</script>

<style scoped>
.sf-viewing {
    font-size: 0.78rem;
}
</style>
