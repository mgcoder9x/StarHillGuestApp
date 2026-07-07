<template>
    <b-card class="line-card">
        <b-row align-v="center" class="">
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
                    <!-- Loading indicator for step definitions -->
                    <b-spinner
                        v-if="!isStepDefinitionsLoaded"
                        small
                        variant="primary"
                        class="mr-2"
                    >
                    </b-spinner>
                    <!-- <a size="sm" class="btn" @click="handleToggleExpand">
                        <Icon
                            :icon="`fa7-solid:chevron-${isExpanded ? 'up' : 'down'}`"
                            width="20"
                            height="20"
                            style="color: #000"
                        />

                        {{ isExpanded ? 'Thu gọn' : 'Chi tiết' }}
                    </a> -->

                    <!-- Auto-scroll toggle -->
                    <!-- <b-button
                        v-b-tooltip.hover
                        title="Bật/tắt tự động cuộn đến bước đang thực hiện"
                        size="sm"
                        :variant="
                            autoScrollEnabled ? 'success' : 'outline-secondary'
                        "
                        class="ml-2"
                        @click="toggleAutoScroll"
                    >
                        <i
                            :class="`fas fa-${autoScrollEnabled ? 'magic' : 'hand-paper'}`"
                        ></i>
                    </b-button> -->
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
                    <StatsBadge
                        icon="lucide:play"
                        :count="stats.inProgress"
                        variant="primary"
                    />
                    <StatsBadge
                        icon="lucide:circle"
                        :count="stats.pending"
                        variant="default"
                    />
                </div>
            </b-col>
        </b-row>
        <!-- Loading State -->
        <div v-if="!isStepDefinitionsLoaded" class="text-center py-4">
            <b-spinner variant="primary"></b-spinner>
            <p class="mt-2 text-muted small">
                {{ $t('Meiko.Dashboard.ProcessConfigLoading') }}
            </p>
        </div>
        <!-- <div class="mb-1 d-flex justify-content-between align-items-center">
            <span class="progress-text">
                <i class="fas fa-chart-bar text-info mr-1"></i>
                Tiến độ: Bước {{ line.currentStep }}/{{
                    stepDefinitions.length
                }}
            </span>
            <span class="text-muted small">
                <i class="fas fa-users mr-1"></i>
                {{ line.operators.length }} người thao tác
            </span>
        </div> -->
        <!-- Carousel Progress Bar -->
        <!-- <div class="carousel-progress-bar mb-3">
            <div
                class="carousel-progress-fill"
                :style="{ width: `${progressPercentage}%` }"
            ></div>
            <span class="carousel-progress-text"
                >{{ progressPercentage }}%</span
            >
        </div> -->

        <StepGrid
            :steps="gridSteps"
            :operators="line.operators"
            :is-expanded="isExpanded"
            :line-key="lineKey"
            :line-name="line.productionLineName"
            :line-id="line.productionLineId || line.workFlowId"
            :step-definitions="stepDefinitions"
            :current-step="line.currentStep"
            :auto-scroll-enabled="autoScrollEnabled"
            :children-stats="line.childrenStats || {}"
            @step-status-click="handleStepStatusClick"
        />
        <Legend />

        <!-- <b-alert v-if="isExpanded" show variant="light" class="mt-3 mb-0 small">
            <i class="fas fa-info-circle text-info mr-1"></i>
            <strong>Chi tiết:</strong> Mỗi ô được chia theo số người tham gia
            bước đó.
        </b-alert> -->
    </b-card>
</template>

<script>
import { mapGetters, mapActions, mapState } from 'vuex'
import { getLineStats } from '@/services/step.service'
import OperatorInfo from '../common/OperatorInfo.vue'
import StatsBadge from '../common/StatsBadge.vue'
import StepGrid from '../common/StepGrid.vue'
import Legend from '../common/Legend.vue'

export default {
    name: 'LineCard',
    components: {
        OperatorInfo,
        StatsBadge,
        StepGrid,
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
    data() {
        return {
            autoScrollEnabled: true,
        }
    },
    computed: {
        ...mapGetters('dashboard', ['isLineExpanded']),
        ...mapState('dashboard', ['stepDefinitionsByWorkFlow']),
        isExpanded() {
            return this.isLineExpanded(this.lineKey)
        },
        workFlowId() {
            return this.line.workFlowId || this.line.productionLineId
        },
        stepDefinitions() {
            return this.stepDefinitionsByWorkFlow[this.workFlowId] || []
        },

        isStepDefinitionsLoaded() {
            return !!this.stepDefinitionsByWorkFlow[this.workFlowId]
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

        progressPercentage() {
            const total = this.stepDefinitions.length
            if (total === 0) return 0
            const current = this.line.currentStep
            return Math.round((current / total) * 100)
        },
    },
    watch: {
        'line.currentStep': {
            handler(newVal, oldVal) {
                if (newVal !== oldVal && this.autoScrollEnabled) {
                    console.log(
                        `LineCard: currentStep changed from ${oldVal} to ${newVal}`
                    )
                }
            },
            immediate: true,
        },
    },
    mounted() {
        console.log(this.line)
    },
    methods: {
        ...mapActions('dashboard', ['toggleLineExpand']),
        handleStepStatusClick(payload) {
            const statusMap = {
                completed: 1,
                violation: 0,
            }

            const mappedStatus = statusMap[payload.status]
            if (mappedStatus === undefined) return

            const productionLineId =
                payload.productionLineId || this.line.productionLineId
            if (!productionLineId) return

            const stepIds = Array.isArray(payload.stepIds)
                ? payload.stepIds.filter(Boolean)
                : []

            const routeData = this.$router.resolve({
                path: '/event/productionLineEvent/list',
                query: {
                    productionLineId: String(productionLineId),
                    stepId: stepIds.length ? stepIds.join(',') : undefined,
                    status: String(mappedStatus),
                },
            })

            window.open(routeData.href, '_blank')
        },
        handleToggleExpand() {
            this.toggleLineExpand(this.lineKey)
        },
        toggleAutoScroll() {
            this.autoScrollEnabled = !this.autoScrollEnabled

            this.$bvToast.toast(
                this.autoScrollEnabled
                    ? this.$t('Meiko.Dashboard.AutoScroll.Enabled')
                    : this.$t('Meiko.Dashboard.AutoScroll.Disabled'),
                {
                    title: this.$t('Meiko.Dashboard.AutoScroll.Title'),
                    variant: this.autoScrollEnabled ? 'success' : 'secondary',
                    autoHideDelay: 2000,
                    solid: true,
                }
            )
        },
    },
}
</script>
<style>
.carousel-progress-bar {
    position: relative;
    height: 8px;
    background: #e9ecef;
    border-radius: 10px;
    overflow: hidden;
}

.carousel-progress-fill {
    height: 100%;
    background: linear-gradient(90deg, #007bff 0%, #0056b3 100%);
    transition: width 0.5s ease;
    border-radius: 10px;
}

.carousel-progress-text {
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    font-size: 0.7rem;
    font-weight: 600;
    color: #495057;
    text-shadow: 0 0 3px white;
}
</style>
