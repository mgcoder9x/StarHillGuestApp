<!-- CompanyList.vue -->
<template>
  <div>
    <!-- Search Form -->
    <b-card>
      <b-card-body>
        <!-- NGĂN SUBMIT KHI NHẤN ENTER -->
        <b-form @submit.prevent.stop>
          <b-row>
            <b-col cols="6">
              <b-form-group
                :label="$t('System.Company.SearchForm.Name')"
                label-cols-md="3"
              >
                <!-- CHẶN ENTER TRÊN INPUT -->
                <b-form-input
                  v-model="trimmedFilter"
                  @keydown.enter.prevent
                />
              </b-form-group>
            </b-col>
          </b-row>
        </b-form>
      </b-card-body>
    </b-card>

    <!-- Table -->
    <b-card title="">
      <div class="vgt-wrap">
        <vue-ads-table-tree
          v-if="table.rows && table.rows.length"
          ref="treeTable"
          :columns="table.columns"
          :classes="table.classes"
          :rows="table.rows"
          :page="table.page"
          :filter="table.filter"
          @page-change="pageChange"
        >
          <!-- create button -->
          <template #top>
            <b-button
              v-if="authorize(['ManageCompany'])"
              variant="primary"
              :to="{ path: '/systems/company/create' }"
              class="mb-1 btn-hover-linear-primary border-0"
              type="button"
            >
              {{ $t('common.button.create') }}
            </b-button>
          </template>

          <template #toggle-children-icon="{ expanded }">
            <Icon v-if="expanded" icon="mdi:minus-box" />
            <Icon v-else icon="mdi:plus-box" />
          </template>

          <template #index="{ row }">
            {{ row._meta.index + 1 }}
          </template>

          <template #action="{ row }">
            <!-- detail button -->
            <b-button
              v-if="authorize(['ViewCompany']) || authorize(['ManageCompany'])"
              v-b-tooltip.hover
              v-waves
              variant="label-secondary"
              class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
              :title="$t('common.button.detail')"
              :to="{ path: `/systems/company/detail/${row.id}` }"
              type="button"
            >
              <Icon icon="mdi:eye-outline" class="xs-icon" />
            </b-button>

            <!-- delete button -->
            <b-button
              v-if="authorize(['ManageCompany'])"
              v-b-tooltip.hover
              v-waves
              variant="label-secondary"
              class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-danger border-0"
              :title="$t('common.button.delete')"
              @click="remove(row.id)"
              type="button"
            >
              <Icon icon="fluent:delete-20-regular" class="xs-icon" />
            </b-button>

            <!-- Create child button -->
            <b-button
              v-if="authorize(['ManageCompany'])"
              v-b-tooltip.hover
              v-waves
              variant="label-secondary"
              class="btn-icon btn-hover-linear-primary border-0"
              :title="$t('common.button.create')"
              :to="{ path: '/systems/company/create', query: { parentId: row.id } }"
              type="button"
            >
              <Icon icon="mdi:plus-thick" class="xs-icon" />
            </b-button>
          </template>
        </vue-ads-table-tree>
      </div>
    </b-card>
  </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import VueAdsTableTree from 'vue-ads-table-tree'

export default {
  components: { VueAdsTableTree },
  mixins: [authorizationMixin],
  data() {
    return {
      table: {
        filter: '',
        rows: [],
        columns: [
          {
            property: 'index',
            title: this.$i18n.locale === 'en' ? 'No.' : 'STT',
            direction: '',
            filterable: false,
            groupable: false,
            collapseIcon: true,
          },
          {
            property: 'name',
            title: this.$t('System.Company.Field.Name'),
            direction: null,
            filterable: true,
          },
          {
            property: 'code',
            title: this.$t('System.Company.Field.Code'),
            direction: null,
            filterable: true,
            groupable: false,
            groupCollapsable: false,
            hideOnGroup: true,
          },
          {
            property: 'action',
            title: this.$t('System.Company.Field.Action'),
            direction: null,
            filterable: true,
            groupable: false,
            groupCollapsable: false,
            hideOnGroup: true,
          },
        ],
        classes: {
          table: {
            'default-font': true,
            'vgt-table bordered ': true,
          },
          'all/0': {
            'cell-index': true,
            'text-center': true,
          },
          'all/4': {
            'cell-action': true,
          },
          '0_-0/4': {
            'text-center': true,
          },
        },
        page: 0,
      },
      searchForm: {
        filterText: '',
        companyId: null,
      },
      treeAreas: [],
    }
  },

  watch: {
    '$i18n.locale'() {
      this.refreshTableHeader()
    },
  },

  computed: {
    // Trim để tránh khoảng trắng thừa + không trigger submit
    trimmedFilter: {
      get() {
        return this.table.filter
      },
      set(value) {
        this.table.filter = (value || '').trim()
      },
    },
  },

  async created() {
    await this.getTreeCompany()
  },

  methods: {
    async confirmDelete() {
      return await this.$swal.fire({
        title: this.$t('common.confirmation.delete.title'),
        text: this.$t('common.confirmation.delete.message'),
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: this.$t('Message.Agree'),
        cancelButtonText: this.$t('Message.Exit'),
        customClass: {
          confirmButton: 'btn btn-primary',
          cancelButton: 'btn btn-outline-danger ml-1',
        },
        buttonsStyling: false,
      })
    },

    async remove(id) {
      const { isConfirmed } = await this.confirmDelete()
      if (!isConfirmed) return

      this.$services
        .delete(`/company/${id}`)
        .then(() => this.showNotification(true))
        .catch((error) => {
          // eslint-disable-next-line no-debugger
          // debugger
          this.showNotification(false, `${this.$t(error?.error || 'Error.Error')}`)
        })
        .finally(async () => {
          await this.getTreeCompany()
        })
    },

    async getTreeCompany() {
      try {
        const res = await this.$services.get('/company/tree')
        this.table.rows = res.data.data
      } catch (e) {
        // eslint-disable-next-line no-console
        console.log('error', e)
      }
    },

    pageChange(newPage) {
      this.table.page = newPage
    },

    showNotification(isSuccess, message = null) {
      this.$toast({
        component: ToastificationContent,
        position: 'top-right',
        props: {
          title: isSuccess
            ? this.$t('common.confirmation.delete.success')
            : this.$t('Success.Warning'),
          icon: isSuccess ? 'CheckIcon' : 'AlertTriangleIcon',
          variant: isSuccess ? 'success' : 'danger',
          text: message,
        },
      })
    },

    refreshTableHeader() {
      this.table.columns[0].title =
        this.$i18n.locale === 'en' ? 'No.' : 'STT'
      this.table.columns[1].title = this.$t('System.Company.Field.Name')
      this.table.columns[2].title = this.$t('System.Company.Field.Code')
      this.table.columns[3].title = this.$t('System.Company.Field.Action')
    },
  },
}
</script>

<style lang="scss">
@import 'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css';

.default-font {
  font-family: 'Montserrat', Helvetica, Arial, serif;
}

.cell-index {
  max-width: 2.5rem;
}

.cell-action {
  width: 12rem;
}

.swal2-styled.swal2-confirm.btn-label-primary {
  color: #7367f0;
  background: #e9e7fd;
}

.swal2-styled.swal2-cancel.btn-label-danger {
  color: #ea5455;
  background: #fad6d6;
}

.vgt-wrap {
  .vue-ads-flex.vue-ads-m-2.vue-ads-px-0.vue-ads-text-xs {
    font-size: 1rem;
  }

  button.vue-ads-ml-1.vue-ads-leading-normal.vue-ads-w-6 {
    padding: 0.25rem 1.2rem 0.25rem 0.8rem;
    text-align: center;
    border-radius: 10%;
    background: #f1f1f2;
    border-color: rgba(0, 0, 0, 0);
    color: #a8aaae;

    &:hover {
      color: #ffffff !important;
      background: linear-gradient(270deg, rgba(4, 93, 165, 0.7), rgba(4, 93, 165)) !important;
      box-shadow: 0 2px 6px rgba(4, 93, 165, 0.3);
    }
  }

  button.vue-ads-ml-1.vue-ads-leading-normal.vue-ads-w-6.vue-ads-bg-teal-500.vue-ads-text-white {
    background-color: #045da5;
    color: #fff;

    &:hover {
      color: #ffffff !important;
      background: linear-gradient(270deg, rgba(4, 93, 165, 0.7), rgba(4, 93, 165)) !important;
      box-shadow: 0 2px 6px rgba(4, 93, 165, 0.3);
    }
  }
}

body.dark-layout {
  button.vue-ads-ml-1.vue-ads-leading-normal.vue-ads-w-6 {
    background: #424659;
    border-color: rgba(0, 0, 0, 0);
    color: #a8aaae;
  }
}
</style>

