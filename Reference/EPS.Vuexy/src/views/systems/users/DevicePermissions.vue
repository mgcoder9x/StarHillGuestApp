<template>
    <div>
        <b-modal v-model="modalShow" :title="$t('System.User.DevicePermissions.DeviceAuthorization')" ok-title="Save" cancel-title="Cancel"
            :dialog-class="'custom-modal-width'" @ok="handleAddPermissions" @cancel="handleCancel">
            <b-row>
                <!-- Left column: Area selection -->
                <b-col md="4">
                    <b-card class="h-100">
                        <b-card-body class="pt-2">
                            <tree-select v-model="searchForm.areaId" :options="listArea" label="text"
                                :always-open="true" :reduce="(area) => area.id"
                                @input="filterDevicesByArea" />
                            <!-- <div class="mt-3">
                                <small class="text-muted">
                                    Tổng thiết bị: {{ filteredDevices.length }}
                                </small>
                            </div> -->
                        </b-card-body>
                    </b-card>
                </b-col>

                <!-- Right column: Device list -->
                <b-col md="8">
                    <b-card class="h-100">
                        <b-card-header class="pb-2">
                            <h6 class="mb-0">{{ $t('System.User.DevicePermissions.ListDevice') }}</h6>
                        </b-card-header>
                        <b-card-body class="pt-2 custom-card-body" style="max-height: 400px; overflow-y: auto;">
                            <div v-if="filteredDevices.length === 0" class="text-center text-muted">
                                {{ $t('System.User.DevicePermissions.NoDevice') }}
                            </div>
                            <b-row v-else style="gap: 10px">
                                <b-col v-for="device in filteredDevices" :key="device.id" md="6"
                                    class="d-flex align-items-center mb-2" style="margin-left: -10px">
                                    <b-form-checkbox :id="`device-${device.id}`" v-model="device.selected"
                                        :name="device.id" class="mr-2">
                                        {{ device.name }}
                                    </b-form-checkbox>
                                </b-col>
                            </b-row>
                        </b-card-body>
                    </b-card>
                </b-col>
            </b-row>
            <template #modal-footer>
                <button class="btn btn-primary" @click="handleAddPermissions">
                    {{ $t('common.button.save') }}
                </button>
                <button class="btn btn-secondary" @click="handleCancel">
                    {{ $t('common.button.cancel') }}
                </button>
            </template>
        </b-modal>
    </div>
</template>

<script>
import { lookupService } from '@/services'
import ToastificationContent from '@/@core/components/toastification/ToastificationContent.vue'
import TreeHelper from '@/utils/treeHelper'

export default {
    name: 'DevicePermissions',
    data() {
        return {
            deviceList: [],
            devicePermissions: [],
            modalShow: false,
            addDevicePermissions: [],
            userId: 0,
            listArea: [],
            searchForm: {
                areaId: null
            }
        }
    },
    computed: {
        filteredDevices() {
            let devices = this.deviceList

            // Filter by area if selected
            if (this.searchForm.areaId) {
                devices = devices.filter(device => device.areaId === this.searchForm.areaId)
            }

            return devices.map((device) => {
                return {
                    id: device.id,
                    name: device.text,
                    areaId: device.areaId,
                    selected: this.devicePermissions.some(
                        (permission) =>
                            +permission.deviceId === +device.id &&
                            permission.isChecked
                    ),
                }
            })
        },
    },
    watch: {
        modalShow() {
            if (this.modalShow) {
                this.loadDevices()
                this.loadUserDevicePermission()
                this.loadAreas()
            }
        },
    },
    mounted() {},
    async created() {
        await this.loadDevices()
        await this.loadUserDevicePermission()
    },
    beforeDestroy() {
        this.addDevicePermissions = []
        this.devicePermissions = []
    },
    methods: {
        clearData() {
            this.userId = null
            this.addDevicePermissions = []
            this.devicePermissions = []
            this.searchForm.areaId = null
        },
        async loadDevices() {
            try {
                lookupService.fetchDevices().then((data) => {
                    this.deviceList = data
                })
            } catch (err) {
                throw new Error(err)
            }
        },
        loadAreas() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.listArea = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
        },
        filterDevicesByArea() {
            // This method is called when area selection changes
            // The computed property filteredDevices will automatically update
        },
        async loadUserDevicePermission() {
            try {
                const { data } = await this.$services.get(
                    `/user-device/${this.userId}`
                )
                this.devicePermissions = data.data
            } catch (err) {
                throw new Error(err)
            }
        },
        showModal(userId) {
            this.userId = userId
            this.modalShow = true
        },
        handleCancel() {
            this.modalShow = false
            this.clearData()
        },
        async handleAddPermissions() {
            const { userId } = this

            // help me generate addDevicePermissions. it group form devicePermissions array and filteredDevices array
            const vm = this
            this.filteredDevices.forEach((device) => {
                const devicePermission = vm.devicePermissions.find(
                    (permission) => +permission.deviceId === +device.id
                )
                if (
                    devicePermission &&
                    devicePermission.isChecked !== device.selected
                ) {
                    vm.addDevicePermissions.push({
                        deviceId: device.id,
                        userId,
                        isChecked: device.selected,
                    })
                } else if (!devicePermission && device.selected) {
                    vm.addDevicePermissions.push({
                        deviceId: device.id,
                        userId,
                        isChecked: device.selected,
                    })
                }
            })
            try {
                await this.$services.post(
                    `/user-device`,
                    this.addDevicePermissions
                )
                this.clearData()
                this.modalShow = false
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Success.Update'),
                        icon: 'CheckIcon',
                        variant: 'success',
                    },
                })
            } catch (err) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: 'Error',
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: this.$t(`${err.response.data.errorCode}`),
                    },
                })
            }
        },
    },
}
</script>

<style scoped>
.h-100 {
    height: 100% !important;
}

::v-deep .custom-modal-width {
    max-width: 1400px !important;
    width: 90% !important;
}

.custom-card-body {
    min-height: 500px !important;
    overflow-y: auto;
}
</style>
