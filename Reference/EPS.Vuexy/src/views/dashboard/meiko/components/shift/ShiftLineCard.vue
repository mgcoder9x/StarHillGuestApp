<template>
    <b-card>
        <b-row align-v="center">
            <b-col md="8">
                <h4 class="mb-2 font-weight-bold">
                    <Icon
                        icon="fa7-solid:industry"
                        width="24"
                        height="24"
                        class="text-primary"
                    />
                    {{ line.productionLineName }}
                </h4>
                <b-spinner
                    v-if="!isStepDefinitionsLoaded"
                    small
                    variant="primary"
                    class="mr-2"
                >
                </b-spinner>
                <!-- <OperatorInfo :operators="line.operators" /> -->
            </b-col>
            <b-col md="4" class="text-md-right ml-auto">
                <div
                    class="d-flex flex-wrap justify-content-md-end"
                    style="gap: 10px"
                >
                    <StatsBadge
                        icon="lucide:circle-check-big"
                        :count="stats.completed"
                        variant="success"
                    />
                    <StatsBadge
                        icon="lucide:circle-alert"
                        :count="stats.violations"
                        variant="warning"
                    />
                    <!-- <StatsBadge
                        icon="lucide:play"
                        :count="stats.inProgress"
                        variant="primary"
                    /> -->
                    <StatsBadge
                        icon="lucide:circle"
                        :count="stats.pending"
                        variant="default"
                    />
                </div>
            </b-col>
        </b-row>
        <StepGrid
            :steps="line.steps"
            :operators="line.operators"
            :is-expanded="isExpanded"
            :line-key="lineKey"
            :step-definitions="stepDefinitions"
            :current-step="line.currentStep"
            :children-stats="line.childrenStats || {}"
        />
        <Legend />
    </b-card>
</template>

<script>
import { mapGetters } from 'vuex'
import { getLineStats, getStepStatus } from '@/services/step.service'
import OperatorInfo from '../common/OperatorInfo.vue'
import StepGrid from '../common/StepGrid.vue'
import StatsBadge from '../common/StatsBadge.vue'
import Legend from '../common/Legend.vue'

export default {
    name: 'ShiftLineCard',
    components: {
        OperatorInfo,
        StepGrid,
        StatsBadge,
        Legend,
    },
    props: {
        lineKey: {
            type: String,
            required: true,
        },
        line: {
            type: Object,
            required: true,
        },
        lineNumber: {
            type: Number,
            required: true,
        },
    },
    computed: {
        ...mapGetters('dashboard', [
            'isLineExpanded',
            'getStepDefinitionsForLine',
            'isStepDefinitionsLoadedForLine',
        ]),
        stepDefinitions() {
            return this.getStepDefinitionsForLine(this.lineKey)
        },
        isStepDefinitionsLoaded() {
            return this.isStepDefinitionsLoadedForLine(this.lineKey)
        },
        isExpanded() {
            return this.isLineExpanded(this.lineKey)
        },
        stats() {
            return getLineStats(this.line)
        },
    },

    methods: {
        hasViolation(stepNumber) {
            const operatorStatuses = this.line.steps[stepNumber]
            return getStepStatus(operatorStatuses) === 'violation'
        },
    },
}
</script>
