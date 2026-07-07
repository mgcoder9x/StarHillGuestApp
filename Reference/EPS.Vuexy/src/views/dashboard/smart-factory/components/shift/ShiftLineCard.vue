<template>
    <b-card>
        <b-row align-v="center">
            <b-col md="8">
                <div class="d-flex align-items-center">
                    <h4 class="mb-0 mr-3 font-weight-bold">
                        <Icon
                            icon="fa7-solid:industry"
                            width="24"
                            height="24"
                            class="text-primary"
                        />
                        {{ line.productionLineName }}
                    </h4>
                    <b-badge
                        :variant="line.isActive ? 'success' : 'secondary'"
                        class="mr-2"
                    >
                        {{ line.isActive ? 'Hoạt động' : 'Ngưng hoạt động' }}
                    </b-badge>
                    <b-spinner
                        v-if="!isStepDefinitionsLoaded"
                        small
                        variant="primary"
                        class="mr-2"
                    >
                    </b-spinner>
                </div>
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
            :steps="gridSteps"
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
import { mapGetters, mapState } from 'vuex'
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
        ...mapGetters('dashboard', ['isLineExpanded']),
        ...mapState('dashboard', ['stepDefinitionsByWorkFlow']),
        workFlowId() {
            return this.line.workFlowId || this.line.productionLineId
        },
        stepDefinitions() {
            return this.stepDefinitionsByWorkFlow[this.workFlowId] || []
        },
        isStepDefinitionsLoaded() {
            return !!this.stepDefinitionsByWorkFlow[this.workFlowId]
        },
        isExpanded() {
            return this.isLineExpanded(this.lineKey)
        },
        gridSteps() {
            return this.stepDefinitions.reduce((acc, stepDef) => {
                const stepNumber = stepDef.stepNumber
                const currentStatuses = this.line.steps?.[stepNumber]

                acc[stepNumber] =
                    currentStatuses && Object.keys(currentStatuses).length > 0
                        ? currentStatuses
                        : { __no_person__: 'pending' }

                return acc
            }, {})
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