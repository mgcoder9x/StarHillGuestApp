<template>
    <div style="height: 100%">
        <div
            v-if="statusCounts === null"
            :id="tooltipId"
            :class="['step-box', stepColorClass, 'step-no-person']"
        >
            <Icon :icon="stepIconClass" width="24" height="24" />
            <div class="step-name">{{ stepDef.name }}</div>
        </div>
        <!-- Single box -->
        <div
            v-else-if="isSingleBox"
            :id="tooltipId"
            :class="['step-box', stepColorClass]"
        >
            <!-- <i :class="`fas fa-${stepIconClass}`"></i> -->
            <Icon :icon="stepIconClass" width="24" height="24" />
            <div>{{ stepDef.name }}</div>
            <div class="step-status-counts-inline">
                <div
                    v-for="(count, status) in activeStatusCounts"
                    :key="status"
                    class="status-count-badge"
                    :class="{ 'is-clickable': canNavigateByStatus(status) }"
                    @click.stop="handleStatusBadgeClick(status)"
                >
                    <Icon
                        :icon="getIconForStatus(status)"
                        width="14"
                        height="14"
                        :style="{ color: 'white' }"
                    />
                    <span class="count-text">{{ count }}</span>
                </div>
            </div>
            <strong v-if="participatingCount > 0">
                {{ $t('Meiko.Dashboard.Participating') }}:
                <br />
                {{ participatingCount }} {{ $t('Meiko.Dashboard.People') }}
            </strong>
        </div>

        <!-- Split box -->
        <div v-else :id="tooltipId" class="step-box-split">
            <div
                v-for="(op, idx) in participatingOps"
                :key="op.personNo"
                :class="['step-segment', getOperatorStepClass(op.personNo)]"
            >
                <i
                    v-if="idx === 0"
                    :class="`fas fa-${getOperatorStepIcon(op.personNo)}`"
                    style="font-size: 0.8em"
                ></i>
                <div v-if="idx === 0" style="font-size: 0.7rem">
                    {{ stepDef.stepNumber }}
                </div>
                <i
                    v-if="idx > 0"
                    :class="`fas fa-${getOperatorStepIcon(op.personNo)}`"
                    style="font-size: 0.7em"
                ></i>
            </div>
        </div>
        <!-- Tooltip -->
        <b-tooltip :target="tooltipId" placement="top">
            <template
                v-if="
                    statusCounts &&
                    (!stepDef.children || stepDef.children.length <= 0)
                "
            >
                <strong
                    >{{ $t('Meiko.Dashboard.Step') }}
                    {{ stepDef.stepNumber }}:</strong
                >
                {{ stepDef.name }}
                <br />
                <strong>{{ $t('Meiko.Dashboard.Duration') }}:</strong>
                {{ stepDef.duration }}
                {{ $t('Meiko.Dashboard.TypeTime') }}<br />
                <span
                    v-if="statusCounts.completed > 0"
                    :style="{ color: getColorForStatus('completed') }"
                >
                    <Icon
                        :icon="getIconForStatus('completed')"
                        width="14"
                        height="14"
                        :style="{ color: getColorForStatus('completed') }"
                    />
                    <strong> {{ $t('Meiko.Dashboard.Completed') }}:</strong>
                    {{ statusCounts.completed }}<br />
                </span>
                <span
                    v-if="statusCounts.violation > 0"
                    :style="{ color: getColorForStatus('violation') }"
                >
                    <Icon
                        :icon="getIconForStatus('violation')"
                        width="14"
                        height="14"
                        :style="{ color: getColorForStatus('violation') }"
                    />
                    <strong> {{ $t('Meiko.Dashboard.Violation') }}:</strong>
                    {{ statusCounts.violation }}<br />
                </span>
            </template>
            <!-- Children info -->
            <template v-if="stepDef.children && stepDef.children.length > 0">
                <!-- <strong
                    >{{
                        $t('Meiko.Dashboard.ChildrenSteps') || 'Các bước con'
                    }}:</strong
                ><br /> -->
                <div
                    v-for="child in stepDef.children"
                    :key="child.id"
                    class="text-left"
                    style="padding-left: 8px; font-size: 1em"
                >
                    • {{ child.name }}:
                    <br />
                    <span
                        v-if="getChildViolationCount(child.id) > 0"
                        :style="{ color: getColorForStatus('violation') }"
                    >
                        ({{ getChildViolationCount(child.id) }}
                        {{ $t('Meiko.Dashboard.ViolationCountUnit') }}) /
                    </span>
                    <span
                        v-if="getChildCompletedCount(child.id) > 0"
                        :style="{ color: getColorForStatus('completed') }"
                    >
                        ({{ getChildCompletedCount(child.id) }}
                        {{ $t('Meiko.Dashboard.ValidCountUnit') }})
                    </span>
                </div>
            </template>
            <!-- <strong>Khu vực:</strong> {{ stepDef }}<br /> -->
        </b-tooltip>
    </div>
</template>

<script>
import {
    getStepStatus,
    getStepIcon,
    getParticipatingOperators,
    getStatusCounts,
    getStatusColor,
} from '@/services/step.service'

export default {
    name: 'StepBox',
    props: {
        stepDef: {
            type: Object,
            required: true,
        },
        operatorStatuses: {
            type: Object,
            required: true,
        },
        operators: {
            type: Array,
            required: true,
        },
        isExpanded: {
            type: Boolean,
            default: false,
        },
        lineKey: {
            type: String,
            required: true,
        },
        lineName: {
            type: String,
            default: '',
        },
        lineId: {
            type: [Number, String],
            default: null,
        },
        childrenStats: {
            type: Object,
            default: () => ({}),
        },
    },
    computed: {
        statusCounts() {
            return getStatusCounts(this.operatorStatuses)
        },
        activeStatusCounts() {
            if (!this.statusCounts) return {}

            const active = {}
            Object.entries(this.statusCounts).forEach(([status, count]) => {
                if (count > 0) {
                    active[status] = count
                }
            })
            return active
        },
        tooltipId() {
            return `tooltip-${this.lineKey}-${this.stepDef.stepNumber}`
        },
        overallStatus() {
            debugger
            return getStepStatus(this.operatorStatuses)
        },
        stepColorClass() {
            return `step-${this.overallStatus}`
        },
        stepIconClass() {
            return getStepIcon(this.overallStatus)
        },
        participatingOps() {
            return getParticipatingOperators(
                this.operatorStatuses,
                this.operators
            )
        },
        participatingCount() {
            return this.participatingOps.length
        },
        isSingleBox() {
            if (!this.isExpanded) return true
            if (this.participatingOps.length <= 1) return true

            // Check if all same status
            const statuses = this.participatingOps.map(
                (op) => this.operatorStatuses[op.personNo]
            )
            return new Set(statuses).size === 1
        },
    },
    methods: {
        getOperatorStepClass(personNo) {
            const status = this.operatorStatuses[personNo]
            return `step-${status}`
        },
        getOperatorStepIcon(personNo) {
            const status = this.operatorStatuses[personNo]
            return getStepIcon(status)
        },
        getIconForStatus(status) {
            return getStepIcon(status)
        },

        getColorForStatus(status) {
            return getStatusColor(status)
        },
        canNavigateByStatus(status) {
            return status === 'completed' || status === 'violation'
        },
        handleStatusBadgeClick(status) {
            if (!this.canNavigateByStatus(status)) return

            const childStepIds = (this.stepDef.children || [])
                .map((child) => child.id)
                .filter(Boolean)

            const stepIds = childStepIds.length
                ? childStepIds
                : [this.stepDef.id].filter(Boolean)

            this.$emit('status-click', {
                productionLineId: this.lineId,
                productionLineName: this.lineName,
                stepName: this.stepDef.name,
                stepId: this.stepDef.id,
                stepIds,
                stepNumber: this.stepDef.stepNumber,
                status,
            })
        },

        /**
         * Get violation count for a specific child step
         * Uses childrenStats prop from realtime API data
         */
        getChildViolationCount(childId) {
            // Violation count from childrenStats (realtime data)
            const childStats = this.childrenStats[childId]
            return childStats?.violationCount || 0
        },
        getChildCompletedCount(childId) {
            // Completed count from childrenStats (realtime data)
            const childStats = this.childrenStats[childId]
            return childStats?.completedCount || 0
        },
    },
}
</script>
<style scoped>
/* ============ Step Box No Person ============ */
.step-no-person {
    border-style: dashed !important;
    opacity: 0.8;
}

/* Header with icon and step number */
.step-box-header {
    display: flex;
    align-items: center;
    gap: 6px;
}

.step-number-badge {
    background: #007bff;
    color: white;
    padding: 2px 8px;
    border-radius: 12px;
    font-size: 0.7rem;
    font-weight: 600;
}

/* Step name */
.step-name {
    font-size: 0.8rem;
    font-weight: 600;
    color: #495057;
    text-align: center;
    line-height: 1.2;
    max-width: 100%;
    overflow: hidden;
    text-overflow: ellipsis;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
}

/* Status counts inline */
.step-status-counts-inline {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    justify-content: center;
    align-items: center;
    margin-top: 4px;
}

.status-count-badge {
    display: flex;
    align-items: center;
    gap: 3px;
    padding: 3px 8px;
    color: white;
    background: rgba(0, 0, 0, 0.05);
    border-radius: 10px;
    font-size: 0.7rem;
    font-weight: 600;
    transition: all 0.2s ease;
}

.status-count-badge.is-clickable {
    cursor: pointer;
}

.status-count-badge:hover {
    background: rgba(0, 0, 0, 0.1);
    transform: scale(1.08);
}

.count-text {
    color: white;
    min-width: 10px;
    text-align: center;
}

/* Duration */
.step-duration {
    font-size: 0.65rem;
    color: #6c757d;
    margin-top: 2px;
}

/* ============ Enhanced Split Box ============ */
.step-box-split-enhanced {
    display: flex;
    flex-direction: column;
    border: 2px solid #dee2e6;
    border-radius: 8px;
    padding: 8px;
    min-height: 90px;
    background: white;
    transition: all 0.3s ease;
}

.step-box-split-enhanced:hover {
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
    transform: translateY(-2px);
}

.step-split-header {
    display: flex;
    flex-direction: column;
    align-items: center;
    margin-bottom: 8px;
    padding-bottom: 6px;
    border-bottom: 1px solid #e9ecef;
    gap: 2px;
}

.step-number {
    font-size: 0.75rem;
    font-weight: 700;
    color: #007bff;
}

.step-name-small {
    font-size: 0.7rem;
    color: #6c757d;
    text-align: center;
}

.step-status-counts {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    justify-content: center;
    align-items: center;
}

.status-count-item {
    display: flex;
    align-items: center;
    gap: 4px;
    padding: 4px 8px;
    background: #f8f9fa;
    border-radius: 12px;
    font-size: 0.75rem;
    font-weight: 600;
    transition: all 0.2s ease;
}

.status-count-item:hover {
    background: #e9ecef;
    transform: scale(1.05);
}

/* ============ Animations ============ */
@keyframes pulse {
    0%,
    100% {
        opacity: 1;
    }
    50% {
        opacity: 0.7;
    }
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .step-name {
        font-size: 0.75rem;
    }

    .status-count-badge {
        padding: 2px 6px;
        font-size: 0.65rem;
    }
}
</style>
