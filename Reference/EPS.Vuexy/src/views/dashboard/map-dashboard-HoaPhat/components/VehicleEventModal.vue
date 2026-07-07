<template>
  <BaseEventModal 
    :show="show"
    :title="$t('Events.MapDashboard.Vehicle')"
    width="350px"
    max-height="40vh"
    @close="$emit('close')"
  >
    <b-table-simple 
      class="mb-0" 
      borderless 
      small 
      responsive 
      table-class="mb-0"
      hover
    >
      <b-thead>
        <b-tr>
          <b-th>{{ $t('Events.EventModals.VehicleType') }}</b-th>
          <b-th class="text-center">{{ $t('Events.MapDashboard.In') }}</b-th>
          <b-th class="text-center">{{ $t('Events.MapDashboard.Out') }}</b-th>
        </b-tr>
      </b-thead>
      <b-tbody>
        <b-tr v-for="vehicle in vehicles" :key="vehicle.id">
          <b-td class="text-truncate custom-cell">
            <img 
              loading="lazy" 
              :src="vehicle.imgUrl" 
              class="svg-size" 
            />
            {{ vehicle.name }}
          </b-td>
          <b-td>
            <b-button 
              class="font-weight-bold py-0 w-100"
              variant="outline-success"
            >
              {{ vehicle.in }}
            </b-button>
          </b-td>
          <b-td>
            <b-button 
              class="font-weight-bold py-0 w-100"
              variant="outline-danger"
            >
              {{ vehicle.out }}
            </b-button>
          </b-td>
        </b-tr>
      </b-tbody>
    </b-table-simple>
  </BaseEventModal>
</template>

<script>
import BaseEventModal from './BaseEventModal.vue'

export default {
  name: 'VehicleEventModal',
  components: {
    BaseEventModal
  },
  props: {
    show: {
      type: Boolean,
      default: false
    },
    vehicles: {
      type: Array,
      default: () => []
    }
  },
  emits: ['close']
}
</script>

<style lang="scss" scoped>
.svg-size {
  width: 18px;
  height: 18px;
}

.custom-cell {
  max-width: 10px;
}
</style>