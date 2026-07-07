<template>
    <div class="ov-filters d-flex align-items-center flex-wrap">
        <div class="d-flex align-items-center mr-3">
            <small class="text-muted mr-1 text-nowrap font-weight-500">
                {{ $t('Meiko.Dashboard.Overview.DateFrom') }}
            </small>
            <date-picker
                :value="overviewDateFrom"
                type="date"
                :locale="currentLocale"
                format="DD/MM/YYYY"
                value-type="YYYY-MM-DD"
                style="width: 130px"
                @input="handleDateFromChange"
            />
        </div>

        <div class="d-flex align-items-center mr-3">
            <small class="text-muted mr-1 text-nowrap font-weight-500">
                {{ $t('Meiko.Dashboard.Overview.DateTo') }}
            </small>
            <date-picker
                :value="overviewDateTo"
                type="date"
                :locale="currentLocale"
                format="DD/MM/YYYY"
                value-type="YYYY-MM-DD"
                style="width: 130px"
                @input="handleDateToChange"
            />
        </div>

        <b-button
            variant="primary"
            size="sm"
            :disabled="isLoading"
            class="mr-auto px-3"
            @click="$emit('apply')"
        >
            <b-spinner v-if="isLoading" small class="mr-1" />
            <Icon
                v-else
                icon="lucide:search"
                width="13"
                height="13"
                class="mr-1"
            />
            {{ $t('Meiko.Dashboard.Overview.Apply') }}
        </b-button>

        <!-- Viewing range context -->
        <small
            v-if="overviewDateFrom && overviewDateTo"
            class="text-muted ml-3 text-nowrap"
        >
            {{ $t('Meiko.Dashboard.Overview.ViewingRange') }}:
            <strong>{{ formatDate(overviewDateFrom) }} → {{ formatDate(overviewDateTo) }}</strong>
        </small>
    </div>
</template>

<script>
import { mapState, mapActions } from 'vuex'

export default {
    name: 'OverviewFilters',
    props: {
        isLoading: {
            type: Boolean,
            default: false,
        },
    },
    computed: {
        ...mapState('dashboard', ['overviewDateFrom', 'overviewDateTo']),
        currentLocale() {
            return this.$i18n.locale
        },
    },
    methods: {
        ...mapActions('dashboard', ['setOverviewDateFrom', 'setOverviewDateTo']),
        handleDateFromChange(val) {
            this.setOverviewDateFrom(val)
        },
        handleDateToChange(val) {
            this.setOverviewDateTo(val)
        },
        formatDate(dateStr) {
            if (!dateStr) return '--'
            try {
                const [y, m, d] = dateStr.split('-')
                return `${d}/${m}/${y}`
            } catch {
                return dateStr
            }
        },
    },
}
</script>
