<template>
    <b-row no-gutters class="gap-1" cols-xl="4" cols-lg="3" cols-md="2">
        <b-col>
            <b-card
                class="mb-0"
                body-class="d-flex justify-content-between align-items-center font-weight-bolder p-1"
            >
                <span class="text-uppercase">
                    <div
                        class="b-avatar badge-light-success rounded widget-icon"
                    >
                        <Icon icon="mdi:archive-check-outline" />
                    </div>
                    Tổng thành phẩm
                </span>
                <span class="text-center fs-2 text-success">
                    {{ widget.successProduct ? widget.successProduct : 0 }}
                </span>
            </b-card>
        </b-col>
        <b-col>
            <b-card
                class="mb-0"
                body-class="d-flex justify-content-between align-items-center font-weight-bolder p-1"
            >
                <span class="text-uppercase">
                    <div
                        class="b-avatar badge-light-danger rounded widget-icon"
                    >
                        <Icon icon="mdi:archive-remove-outline" />
                    </div>
                    Tổng SP lỗi
                </span>
                <span class="text-center fs-2 text-danger">
                    {{ widget.errorProduct ? widget.errorProduct : 0 }}
                </span>
            </b-card>
        </b-col>
        <b-col v-if="widget.bestPerformance.percent">
            <b-card
                class="mb-0"
                body-class="d-flex justify-content-between align-items-center font-weight-bolder p-1"
            >
                <span class="text-uppercase text-">
                    <div
                        v-if="widget.bestPerformance"
                        class="b-avatar badge-light-success rounded widget-icon"
                    >
                        <Icon icon="ph:chart-line-up-bold" />
                    </div>
                    {{ widget.bestPerformance.name }}
                </span>
                <span class="text-success fs-2">
                    {{ widget.bestPerformance.percent }}%
                </span>
            </b-card>
        </b-col>
        <b-col v-if="widget.worstPerformance.percent">
            <b-card
                class="mb-0"
                body-class="d-flex justify-content-between align-items-center font-weight-bolder p-1"
            >
                <div class="d-flex align-items-end">
                    <div
                        class="b-avatar badge-light-danger rounded widget-icon"
                    >
                        <Icon icon="ph:chart-line-down-bold" />
                    </div>
                    <div class="text-uppercase widget-title pl-1">
                        {{ widget.worstPerformance.name }}
                    </div>
                </div>
                <div class="text-danger fs-2">
                    {{ widget.worstPerformance.percent }}%
                </div>
            </b-card>
        </b-col>
    </b-row>
</template>
<script>
/* eslint-disable */
export default {
    name: 'FireworkWidgets',
    props: {
        lstCascadeFw: {
            type: Array,
            default: () => [],
        },
    },
    watch: {
        lstCascadeFw() {
            this.widgetData = this.lstCascadeFw.map((item) => item)
            this.widgetData.sort((a, b) => b.performance - a.performance)
            const bestPerformance = this.widgetData[0]
            const worstPerformance = this.widgetData.slice(-1)[0]
            this.widget.bestPerformance = {
                name: bestPerformance.name,
                percent: bestPerformance.performance,
            }
            this.widget.worstPerformance = {
                name: worstPerformance.name,
                percent: worstPerformance.performance,
            }
            this.widget.errorProduct = this.widgetData.reduce(
                (total, item) => (total += item.totalError),
                0
            )
            this.widget.successProduct = this.widgetData.reduce(
                (total, item) => (total += item.totalSuccess),
                0
            )
        },
    },
    data() {
        return {
            widgetData: [],
            widget: {
                errorProduct: 0,
                successProduct: 0,
                bestPerformance: {
                    name: '',
                    percent: 0,
                },
                worstPerformance: {
                    name: '',
                    percent: 0,
                },
            },
        }
    },
    created() {},
}
</script>
<style>
.widget-icon {
    font-size: 2rem;
}
.widget-title {
    font-size: 1.25rem;
    vertical-align: bottom;
}
</style>
