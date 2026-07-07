<template>
    <section id="dashboard-ecommerce">
        <b-card no-body>
            <b-card-body>
                <!-- <b-row>
          <b-col cols="12">
            <b-card-group class="mb-0">
              <b-card img-top no-body>
                <img
                  src="/img/logoThanhBinh.6f47ceb1.jpg"
                  alt="Card image cap"
                  class="card-img-top"
                />
              </b-card>
            </b-card-group>
          </b-col>
        </b-row> -->

                <b-row class="match-height">
                    <b-col xl="4" md="6">
                        <ecommerce-medal :data="data.congratulations" />
                    </b-col>
                    <b-col xl="8" md="6">
                        <ecommerce-statistics :data="data.statisticsItems" />
                    </b-col>
                </b-row>

                <b-row class="match-height">
                    <b-col lg="4">
                        <b-row class="match-height">
                            <b-col lg="6" md="3" cols="6">
                                <ecommerce-order-chart
                                    :data="data.statisticsOrder"
                                />
                            </b-col>
                            <b-col lg="6" md="3" cols="6">
                                <ecommerce-profit-chart
                                    :data="data.statisticsProfit"
                                />
                            </b-col>
                            <b-col lg="12" md="6">
                                <ecommerce-earnings-chart
                                    :data="data.earningsChart"
                                />
                            </b-col>
                        </b-row>
                    </b-col>

                    <b-col lg="8">
                        <ecommerce-revenue-report :data="data.revenue" />
                    </b-col>
                </b-row>

                <b-row class="match-height">
                    <b-col lg="8">
                        <ecommerce-company-table
                            :table-data="data.companyTable"
                        />
                    </b-col>

                    <b-col lg="4" md="6">
                        <ecommerce-meetup :data="data.meetup" />
                    </b-col>

                    <b-col lg="4" md="6">
                        <ecommerce-browser-states />
                    </b-col>

                    <b-col lg="4" md="6">
                        <ecommerce-goal-overview :data="data.goalOverview" />
                    </b-col>

                    <b-col lg="4" md="6">
                        <ecommerce-transactions :data="data.transactionData" />
                    </b-col>
                </b-row>
            </b-card-body>
        </b-card>
    </section>
</template>

<script>
/* eslint-disable */
import { getUserData } from '@/auth/utils'
import EcommerceMedal from './EcommerceMedal.vue'
import EcommerceStatistics from './EcommerceStatistics.vue'
import EcommerceRevenueReport from './EcommerceRevenueReport.vue'
import EcommerceOrderChart from './EcommerceOrderChart.vue'
import EcommerceProfitChart from './EcommerceProfitChart.vue'
import EcommerceEarningsChart from './EcommerceEarningsChart.vue'
import EcommerceCompanyTable from './EcommerceCompanyTable.vue'
import EcommerceMeetup from './EcommerceMeetup.vue'
import EcommerceBrowserStates from './EcommerceBrowserStates.vue'
import EcommerceGoalOverview from './EcommerceGoalOverview.vue'
import EcommerceTransactions from './EcommerceTransactions.vue'

export default {
    components: {
        EcommerceMedal,
        EcommerceStatistics,
        EcommerceRevenueReport,
        EcommerceOrderChart,
        EcommerceProfitChart,
        EcommerceEarningsChart,
        EcommerceCompanyTable,
        EcommerceMeetup,
        EcommerceBrowserStates,
        EcommerceGoalOverview,
        EcommerceTransactions,
    },
    data() {
        return {
            data: {},
        }
    },
    created() {
        // data
        this.$http.get('/ecommerce/data').then((response) => {
            this.data = response.data

            // ? Your API will return name of logged in user or you might just directly get name of logged in user
            // ? This is just for demo purpose
            const userData = getUserData()
            this.data.congratulations.name =
                userData.fullName.split(' ')[0] || userData.username
        })
    },
}
</script>

<style lang="scss">
@import '@core/scss/vue/pages/dashboard-ecommerce.scss';
@import '@core/scss/vue/libs/chart-apex.scss';
</style>
