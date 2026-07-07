<template>
    <div class="step-carousel-container">
        <!-- Navigation Arrows -->
        <button
            v-if="canScrollLeft"
            aria-label="Previous"
            class="carousel-nav carousel-nav-left"
            @click="scrollLeft"
        >
            <Icon
                icon="fa7-solid:chevron-left"
                width="30"
                height="30"
                style="color: #000"
            />
        </button>
        <div
            ref="stepsContainer"
            class="step-carousel"
            @wheel="handleWheel"
            @scroll="handleScroll"
        >
            <div class="step-carousel-track">
                <div
                    v-for="(stepDef, index) in stepDefinitions"
                    :key="stepDef.stepNumber"
                    :ref="`step-${index}`"
                    class="step-carousel-item"
                    :class="{
                        'is-active': isStepInProgress(stepDef.stepNumber),
                    }"
                >
                    <StepBox
                        :step-def="stepDef"
                        :operator-statuses="steps[stepDef.stepNumber]"
                        :operators="operators"
                        :is-expanded="isExpanded"
                        :line-key="lineKey"
                        :line-name="lineName"
                        :line-id="lineId"
                        :children-stats="
                            childrenStats[stepDef.stepNumber] || {}
                        "
                        @status-click="handleStatusClick"
                    />

                    <!-- Progress Indicator -->
                    <div
                        v-if="isStepInProgress(stepDef.stepNumber)"
                        class="step-progress-indicator"
                    >
                        <div class="progress-pulse"></div>
                        <span class="progress-label">
                            {{ $t('Meiko.Dashboard.InProgress') }}
                        </span>
                    </div>
                </div>
            </div>
        </div>
        <!-- Navigation Arrows -->
        <button
            v-if="canScrollRight"
            class="carousel-nav carousel-nav-right"
            @click="scrollRight"
            aria-label="Next"
        >
            <Icon
                icon="fa7-solid:chevron-right"
                width="30"
                height="30"
                style="color: #000"
            />
        </button>

        <!-- Pagination Dots -->
        <div class="carousel-pagination">
            <button
                v-for="page in totalPages"
                :key="page"
                class="pagination-dot"
                :class="{ active: currentPage === page - 1 }"
                @click="goToPage(page - 1)"
                :aria-label="`Go to page ${page}`"
            ></button>
        </div>

        <!-- Step Counter -->
        <!-- <div class="step-counter">
            <i class="fas fa-tasks mr-2"></i>
            <span>{{ completedSteps }}/{{ totalSteps }} bước hoàn thành</span>
            <span class="ml-3 text-muted">
                Trang {{ currentPage + 1 }}/{{ totalPages }}
            </span>
        </div> -->
    </div>
</template>

<script>
import { getStepStatus } from '@/services/step.service'
import StepBox from './StepBox.vue'

export default {
    name: 'StepGrid',
    components: {
        StepBox,
    },
    props: {
        steps: {
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
        stepDefinitions: {
            type: Array,
            required: true,
        },
        currentStep: {
            type: Number,
            default: 0,
        },
        autoScrollEnabled: {
            type: Boolean,
            default: true,
        },
        childrenStats: {
            type: Object,
            default: () => ({}),
        },
    },
    data() {
        return {
            currentPage: 0,
            stepsPerPage: 6,
            canScrollLeft: false,
            canScrollRight: true,
            scrollTimeout: null,
        }
    },
    computed: {
        totalPages() {
            return Math.ceil(this.stepDefinitions.length / this.stepsPerPage)
        },
        totalSteps() {
            return this.stepDefinitions.length
        },
        completedSteps() {
            return Object.values(this.steps).filter((operatorStatuses) => {
                const status = getStepStatus(operatorStatuses)
                return status === 'completed'
            }).length
        },
        inProgressStepIndex() {
            return this.stepDefinitions.findIndex((stepDef) =>
                this.isStepInProgress(stepDef.stepNumber)
            )
        },
    },
    watch: {
        currentStep: {
            handler(newStep, oldStep) {
                debugger
                if (newStep !== oldStep && this.autoScrollEnabled) {
                    this.$nextTick(() => {
                        this.scrollToInProgressStep()
                    })
                }
            },
            immediate: false,
        },
    },
    mounted() {
        this.updateScrollButtons()
        this.scrollToInProgressStep()

        // Add resize listener
        window.addEventListener('resize', this.handleResize)
    },
    beforeDestroy() {
        window.removeEventListener('resize', this.handleResize)
        if (this.scrollTimeout) {
            clearTimeout(this.scrollTimeout)
        }
    },
    methods: {
        handleStatusClick(payload) {
            this.$emit('step-status-click', payload)
        },
        isStepInProgress(stepNumber) {
            const operatorStatuses = this.steps[stepNumber]
            return getStepStatus(operatorStatuses) === 'inProgress'
        },

        handleWheel(event) {
            // Enable horizontal scroll with mouse wheel
            event.preventDefault()
            const container = this.$refs.stepsContainer
            const scrollAmount = event.deltaY
            container.scrollLeft += scrollAmount

            // Disable auto-scroll temporarily when user manually scrolls
            this.$emit('update:autoScrollEnabled', false)
            if (this.scrollTimeout) {
                clearTimeout(this.scrollTimeout)
            }
            this.scrollTimeout = setTimeout(() => {
                this.$emit('update:autoScrollEnabled', true)
            }, 3000)
        },

        handleScroll() {
            this.updateScrollButtons()
            this.updateCurrentPage()
        },

        updateScrollButtons() {
            const container = this.$refs.stepsContainer
            if (!container) return

            this.canScrollLeft = container.scrollLeft > 0
            this.canScrollRight =
                container.scrollLeft <
                container.scrollWidth - container.clientWidth - 1
        },

        updateCurrentPage() {
            const container = this.$refs.stepsContainer
            if (!container) return

            const scrollPosition = container.scrollLeft
            const itemWidth = container.clientWidth / this.stepsPerPage
            this.currentPage = Math.round(
                scrollPosition / (itemWidth * this.stepsPerPage)
            )
        },

        scrollLeft() {
            const container = this.$refs.stepsContainer
            const scrollAmount = container.clientWidth
            container.scrollBy({
                left: -scrollAmount,
                behavior: 'smooth',
            })
        },

        scrollRight() {
            const container = this.$refs.stepsContainer
            const scrollAmount = container.clientWidth
            container.scrollBy({
                left: scrollAmount,
                behavior: 'smooth',
            })
        },

        goToPage(pageIndex) {
            const container = this.$refs.stepsContainer
            const itemWidth = container.clientWidth / this.stepsPerPage
            const scrollPosition = pageIndex * itemWidth * this.stepsPerPage

            container.scrollTo({
                left: scrollPosition,
                behavior: 'smooth',
            })

            this.currentPage = pageIndex
        },

        scrollToInProgressStep() {
            if (this.inProgressStepIndex === -1) return

            const stepRef = this.$refs[`step-${this.inProgressStepIndex}`]
            if (!stepRef || !stepRef[0]) return

            const container = this.$refs.stepsContainer
            const stepElement = stepRef[0]

            // Calculate scroll position to center the in-progress step
            const containerWidth = container.clientWidth
            const stepLeft = stepElement.offsetLeft
            const stepWidth = stepElement.offsetWidth
            const scrollPosition = stepLeft - containerWidth / 2 + stepWidth / 2

            container.scrollTo({
                left: Math.max(0, scrollPosition),
                behavior: 'smooth',
            })
        },

        handleResize() {
            debugger
            this.updateScrollButtons()
        },
    },
}
</script>

<style scoped>
.step-carousel-container {
    position: relative;
    padding: 0 15px 20px 15px;
}

.step-carousel {
    padding: 10px;
    overflow-x: hidden;
    overflow-y: hidden;
    scroll-behavior: smooth;
    scrollbar-width: thin;
    scrollbar-color: #cbd5e0 #f7fafc;
    -webkit-overflow-scrolling: touch;
}

.step-carousel::-webkit-scrollbar {
    height: 8px;
}

.step-carousel::-webkit-scrollbar-track {
    background: #f7fafc;
    border-radius: 10px;
}

.step-carousel::-webkit-scrollbar-thumb {
    background: #cbd5e0;
    border-radius: 10px;
    transition: background 0.3s;
}

.step-carousel::-webkit-scrollbar-thumb:hover {
    background: #a0aec0;
}

.step-carousel-track {
    display: flex;
    gap: 16px;
    padding: 10px 0;
}

.step-carousel-item {
    flex: 0 0 calc((100% - 80px) / 12);
    min-width: 80px;
    max-width: 140px;
    position: relative;
    transition: transform 0.3s ease;
    text-align: center;
}

.step-carousel-item.is-active {
    transform: scale(1.05);
    z-index: 10;
}

.step-carousel-item.is-active::before {
    content: '';
    position: absolute;
    inset: -8px;
    border: 3px solid #007bff;
    border-radius: 12px;
    animation: pulse-border 2s infinite;
    pointer-events: none;
}

@keyframes pulse-border {
    0%,
    100% {
        opacity: 1;
        transform: scale(1);
    }
    50% {
        opacity: 0.5;
        transform: scale(1.02);
    }
}

/* Navigation Arrows */
.carousel-nav {
    position: absolute;
    top: 35%;
    transform: translateY(-50%);
    width: 40px;
    height: 40px;
    border-radius: 50%;
    background: white;
    border: 2px solid #e2e8f0;
    color: #4a5568;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: all 0.3s;
    z-index: 20;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.carousel-nav:hover {
    background: #007bff;
    color: white;
    border-color: #007bff;
    transform: translateY(-50%) scale(1.1);
    box-shadow: 0 4px 12px rgba(0, 123, 255, 0.3);
}

.carousel-nav:active {
    transform: translateY(-50%) scale(0.95);
}

.carousel-nav-left {
    left: 0;
}

.carousel-nav-right {
    right: 0;
}

/* Pagination Dots */
.carousel-pagination {
    display: flex;
    justify-content: center;
    gap: 8px;
    /* margin-top: 20px; */
}

.pagination-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    background: #cbd5e0;
    border: none;
    padding: 0;
    cursor: pointer;
    transition: all 0.3s;
}

.pagination-dot:hover {
    background: #a0aec0;
    transform: scale(1.2);
}

.pagination-dot.active {
    background: #007bff;
    width: 24px;
    border-radius: 5px;
}

/* Step Counter */
.step-counter {
    text-align: center;
    margin-top: 12px;
    font-size: 0.9rem;
    color: #4a5568;
    font-weight: 500;
}

/* Progress Indicator */
.step-progress-indicator {
    position: absolute;
    bottom: -30px;
    left: 50%;
    transform: translateX(-50%);
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 4px;
}

.progress-pulse {
    width: 12px;
    height: 12px;
    background: #007bff;
    border-radius: 50%;
    animation: pulse 1.5s infinite;
}

@keyframes pulse {
    0%,
    100% {
        transform: scale(1);
        opacity: 1;
    }
    50% {
        transform: scale(1.5);
        opacity: 0.5;
    }
}

.progress-label {
    font-size: 0.7rem;
    color: #007bff;
    font-weight: 600;
    white-space: nowrap;
}

/* Responsive */
@media (max-width: 1400px) {
    .step-carousel-item {
        flex: 0 0 calc((100% - 80px) / 5);
    }
}

@media (max-width: 1200px) {
    .step-carousel-item {
        flex: 0 0 calc((100% - 64px) / 4);
    }
}

@media (max-width: 768px) {
    .step-carousel-container {
        padding: 0 15px 20px 15px;
    }

    .step-carousel-item {
        flex: 0 0 calc((100% - 32px) / 3);
        min-width: 100px;
    }

    .carousel-nav {
        width: 32px;
        height: 32px;
    }
}

@media (max-width: 576px) {
    .step-carousel-container {
        padding: 0 10px 20px 10px;
    }

    .step-carousel-item {
        flex: 0 0 calc((100% - 16px) / 2);
        min-width: 90px;
    }
}
</style>
