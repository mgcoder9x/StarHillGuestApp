<template>
  <div>
    <!-- Filters -->
    <b-card no-body>
      <b-card-body>
        <b-form @submit.prevent="search">
          <b-row>
            <b-col cols="6">
              <b-form-group
                :label="$t('System.User.SearchForm.Company')"
                label-for="h-searchForm-companyId"
                label-cols-md="2"
              >
                <treeselect
                  id="h-searchForm-companyId"
                  v-model="searchForm.companyId"
                  :multiple="false"
                  :options="options"
                  :normalizer="normalizer"
                  :placeholder="$t('System.User.SearchForm.Company')"
                  @input="search"
                />
              </b-form-group>
            </b-col>

            <b-col md="6">
              <b-form-group
                :label="$t('System.User.SearchForm.Name')"
                label-for="h-searchForm-username"
                label-cols-md="2"
              >
                <b-form-input
                  id="h-searchForm-username"
                  v-model.trim="searchForm.username"
                  :placeholder="$t('System.User.SearchForm.Name')"
                  type="text"
                  @input="searchDebounced"
                />
              </b-form-group>
            </b-col>
          </b-row>

          <!-- submit ẩn để Enter không reload -->
          <b-button
            type="submit"
            :aria-label="$t('Button.Search')"
            style="position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0);"
          >
            {{ $t('Button.Search') }}
          </b-button>
        </b-form>
      </b-card-body>
    </b-card>

    <!-- Actions + Table -->
    <b-card title="">
      <div>
        <b-button
          v-if="authorize(['ManageUser'])"
          variant="primary"
          :to="{ path: '/systems/users/create' }"
          class="mb-2"
        >
          {{ $t('Button.Create') }}
        </b-button>
      </div>

      <BasicTable
        ref="userTable"
        :columns="columns"
        data-url="/users/list"
        :search-form="searchForm"
        storageName="userTable"
      >
        <template v-slot:table-row="{ column, row }">
          <!-- Roles -->
          <span v-if="column.field === 'roles'">
            {{ Array.isArray(row.roles) ? row.roles[0] : row.roles }}
          </span>

          <!-- Actions -->
          <span v-else-if="column.field === 'action'">
            <div class="text-nowrap">
              <!-- Detail -->
              <b-button
                v-if="authorize(['ViewUser'])"
                v-b-tooltip.hover
                v-waves
                variant="label-secondary"
                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                :title="$t('common.button.detail')"
                :to="{ path: `/systems/users/detail/${row.id}` }"
              >
                <Icon icon="mdi:eye-outline" class="xs-icon" />
              </b-button>

              <!-- Notification setup -->
              <b-button
                v-if="authorize(['ManageUser'])"
                v-b-tooltip.hover
                v-waves
                variant="label-secondary"
                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                :title="$t('common.button.setup-notification')"
                :to="{ path: `/systems/users/notification/${row.id}` }"
              >
                <Icon icon="mdi:notification-settings-outline" class="xs-icon" />
              </b-button>

              <!-- Privileges -->
              <b-button
                v-if="authorize(['ManageUser'])"
                v-b-tooltip.hover
                v-waves
                variant="label-secondary"
                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                :title="$t('common.button.setup-privileges')"
                :to="{ path: `/systems/users/privileges/${row.id}` }"
              >
                <Icon icon="hugeicons:access" class="xs-icon" />
              </b-button>

              <!-- Device permissions -->
              <b-button
                v-if="authorize(['ManageUser'])"
                v-b-tooltip.hover
                v-waves
                variant="label-secondary"
                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                :title="$t('common.button.device-permission')"
                @click="handleShowDevicePermissions(row.id)"
              >
                <Icon icon="tabler:camera-cog" class="xs-icon" />
              </b-button>

              <!-- Lock / Unlock (chỉ admin hiện tại mới được khoá user khác không phải admin) -->
              <b-button
                v-if="authorize(['ManageUser']) && !row.isAdministrator && currentUser.isAdministrator"
                v-b-tooltip.hover
                v-waves
                variant="label-secondary"
                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                :title="!row.isLocked ? $t('common.button.account-lock') : $t('common.button.account-unlock')"
                @click="changeAccountStatus(row.id, !row.isLocked)"
              >
                <Icon :icon="row.isLocked ? 'si:unlock-muted-duotone' : 'mdi:lock'" />
              </b-button>

              <!-- Delete -->
              <b-button
                v-if="authorize(['ManageUser'])"
                v-b-tooltip.hover
                v-waves
                variant="label-secondary"
                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                :title="$t('common.button.delete')"
                @click="doDelete(row.id)"
              >
                <Icon icon="fluent:delete-20-regular" class="xs-icon" />
              </b-button>
            </div>
          </span>
        </template>
      </BasicTable>
    </b-card>

    <DevicePermissions ref="devicePermission" />
  </div>
</template>

<script>
/* eslint-disable */
import Treeselect from '@riophae/vue-treeselect'
import '@riophae/vue-treeselect/dist/vue-treeselect.css'
import { authorizationMixin } from '@core/mixins/ui/forms'
import DevicePermissions from './DevicePermissions.vue'

export default {
  mixins: [authorizationMixin],
  components: {
    Treeselect,
    DevicePermissions,
  },
  data() {
    return {
      searchForm: {
        username: '',
        companyId: null,
      },
      options: [],
      columns: [
        { label: 'System.User.Field.Company', field: 'companyName' },
        { label: 'System.User.Field.Name', field: 'username' },
        { label: 'System.User.Field.Role', field: 'roles' },
        { label: 'System.User.Field.PhoneNumber', field: 'phoneNumber' },
        { label: 'System.User.Field.Action', field: 'action' },
      ],
      _debounceTimer: null,
    }
  },
  computed: {
    currentUser() {
      return this.$services.getUserData()
    },
  },
  created() {
    this.loadCompanyTree()
        const currentUser = this.$services.getUserData()
        this.searchForm.companyId = currentUser?.companyId || null
  },
  methods: {
    // debounce 300ms cho ô tên
    searchDebounced() {
      clearTimeout(this._debounceTimer)
      this._debounceTimer = setTimeout(() => this.search(), 300)
    },

    handleShowDevicePermissions(userId) {
      this.$refs.devicePermission.showModal(userId)
    },

    // Tree company
    loadCompanyTree() {
      this.$services.get('/lookup/company-tree').then((res) => {
        this.options = res.data
      })
    },

    // Chuẩn hoá node cho Treeselect (đảm bảo có id/label và bỏ children rỗng)
    normalizer(node) {
      return {
        id: node.id,
        label: node.label || node.text || node.name || '',
        children:
          node.children && node.children.length ? node.children : undefined,
      }
    },

    search() {
      this.$refs.userTable && this.$refs.userTable.refresh()
    },

    // Xoá user
    doDelete(id) {
      this.$swal({
        icon: 'warning',
        title: this.$t('common.confirmation.delete.title'),
        text: this.$t('common.confirmation.delete.message'),
        showCancelButton: true,
        confirmButtonText: this.$t('common.button.confirm'),
        cancelButtonText: this.$t('common.button.cancel'),
        customClass: {
          confirmButton: 'btn btn-primary',
          cancelButton: 'btn btn-outline-danger ml-1',
        },
        buttonsStyling: false,
      }).then((result) => {
        if (!result.value) return
        this.$services
          .delete(`/users/${id}`)
          .then(() => {
            this.search()
            this.$swal({
              icon: 'success',
              title: this.$t('common.confirmation.delete.success'),
              customClass: { confirmButton: 'btn btn-success' },
            })
          })
          .catch((e) => {
            this.$swal({
              icon: 'error',
              title: this.$t('Error.Error'),
              text: (e && (e.message || e.error)) || '',
              customClass: { confirmButton: 'btn btn-danger' },
            })
          })
      })
    },

    // Khoá/Mở tài khoản
    changeAccountStatus(userId, lock) {
      this.$services
        .patch(`/users/${userId}`, {
          isLocked: lock,
          LockoutEnd: new Date('9999-12-31'),
        })
        .then(() => {
          this.search()
          this.$swal({
            icon: 'success',
            title: this.$t(
              lock
                ? 'System.User.Message.LockSuccess'
                : 'System.User.Message.UnlockSuccess'
            ),
            customClass: { confirmButton: 'btn btn-success' },
          })
        })
        .catch((e) => {
          this.$swal({
            icon: 'error',
            title: this.$t('Error.Error'),
            text: (e && (e.message || e.error)) || '',
            customClass: { confirmButton: 'btn btn-danger' },
          })
        })
    },
  },
}
</script>

<style lang="scss"></style>

