<template>
    <b-row class="gap-1" no-gutters>
        <b-col cols="12">
            <b-card class="mb-0">
                <b-card-header class="p-0">
                    <h4 class="mb-0">Thống kê dây chuyền</h4>
                </b-card-header>
                <b-form @submit.prevent="search">
                    <!-- advance search input -->
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                class="mb-0"
                                label="Ngày:"
                                label-cols-md="3"
                            >
                                <b-form-datepicker
                                    v-model="searchForm.date"
                                    placeholder="YYYY-MM-DD"
                                    :locale="currentLocale"
                                    @input="loadData"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card>
        </b-col>
        <b-col cols="12">
            <FireworkWidgets :lst-cascade-fw="lstCascadeFw" />
        </b-col>
        <b-col cols="12">
            <!-- <b-row no-gutters style="margin-left: -10px" cols-xl="4" cols-lg="3" cols-md="2">
                <b-col v-for="cascadeFw in lstCascadeFw" :key="cascadeFw.id" md="3" style="margin: 5px;">
                    <b-card class="mb-0" body-class="p-1" >
                 
                        <firework-class-states :data="cascadeFw" />
                        <fire-work-goal-overview :data="cascadeFw.overview" />
                    </b-card>
                </b-col>
            </b-row> -->
            <b-row cols-xl="4" cols-lg="3" cols-md="4"  >
                <b-col
                    v-for="cascadeFw in lstCascadeFw"
                    :key="cascadeFw.id"
                    md="4"
                    lg="3"
                    xs="1"
                    class="mb-1"
                >
                    <b-card class="mb-0" body-class="p-1">
                    <firework-class-states :data="cascadeFw" />
                    <fire-work-goal-overview :data="cascadeFw.overview" />
                    </b-card>
                </b-col>
            </b-row>
        </b-col>
    </b-row>
</template>
<script>
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'
import FireWorkGoalOverview from './FireworkGoalOverview.vue'
import FireworkClassStates from './FireworkClassStates.vue'
import FireworkWidgets from './FireworkWidgets.vue'
import { $themeColors, $z121Config } from '@themeConfig'
import moment from 'moment'

export default {
    name: 'FireworkCountByDay',
    mixins: [authorizationMixin],
    components: {
        FireWorkGoalOverview,
        FireworkClassStates,
        FireworkWidgets,
    },
    data() {
        const VIEW_MODE = {
            ALL: 0,
            NUMBER: 1,
            PERCENT: 2,
        }
        return {
            VIEW_MODE,
            chart: {
                series: [0],
                success: '0',
                error: '0',
            },
            searchForm: {
                date: moment().format('yyyy-MM-DD'),
            },

            lstCascadeFw: [],
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    methods: {
        async getTreeArea() {
            const res = await this.$services.get('/areas/tree')
            this.lstCascadeFw = res.data.data
        },
        async getFireworkCountByDay() {
            try {
                const res = await this.$services.get(
                    `/fireWorkEvents/fireworkCountByDay/${this.searchForm.date}`
                )
                return res.data.data
            } catch (error) {
                alert(error)
            }
        },
        async loadData() {
            await this.getTreeArea()
            const fireworkCountByDay = await this.getFireworkCountByDay()
            this.lstCascadeFw = this.lstCascadeFw.map((cascadeFw) => {
                const lstEventCount = fireworkCountByDay.filter(
                    (x) => x.cascadeFwId == cascadeFw.id
                )
                cascadeFw._children = cascadeFw._children.map((classFw) => {
                    const classFwDetail = lstEventCount.find(
                        (x) => x.classFwId == classFw.id
                    )
                    if (classFwDetail) {
                        const successProduct = classFwDetail.success
                            ? classFwDetail.success
                            : 0
                        const errorProduct = classFwDetail.failed
                            ? classFwDetail.failed
                            : 0
                        const performance =
                            successProduct > 0
                                ? Math.round(
                                      (successProduct * 100) /
                                          (successProduct + errorProduct)
                                  )
                                : 0
                        return {
                            ...classFw,
                            successProduct: successProduct,
                            errorProduct: errorProduct,
                            performance: performance,
                        }
                    }
                    return classFw
                })

                cascadeFw._children.sort((a, b) => a.treeIndex - b.treeIndex)

                const lastRecord = cascadeFw._children.slice(-1)[0]
                const totalSuccess = lastRecord.successProduct
                    ? lastRecord.successProduct
                    : 0
                const totalError = cascadeFw._children.reduce(
                    (total, item) =>
                        (total += item.errorProduct ? item.errorProduct : 0),
                    0
                )
                const performance = Math.round(
                    (totalSuccess * 100) / (totalSuccess + totalError)
                )

                const color =
                    performance >= $z121Config.PERFORMANCE.GOOD
                        ? $themeColors.success
                        : performance < $z121Config.PERFORMANCE.GOOD &&
                            performance >= $z121Config.PERFORMANCE.BAD
                          ? $themeColors.warning
                          : $themeColors.danger
                return {
                    ...cascadeFw,
                    totalSuccess: totalSuccess,
                    totalError: totalError,
                    performance: performance ? performance : 0,
                    overview: {
                        totalSuccess: totalSuccess.toString(),
                        totalError: totalError.toString(),
                        performance: [performance ? performance : 0],
                        color: color,
                    },
                    viewMode: this.VIEW_MODE.NUMBER,
                }
            })

            this.lstCascadeFw.sort((a, b) => a.code.localeCompare(b.code))
        },
    },
    async created() {
        await this.loadData()
    },
}
</script>

<style lang="scss" scope>
.fs-2 {
    font-size: 2rem !important;
}
</style>
