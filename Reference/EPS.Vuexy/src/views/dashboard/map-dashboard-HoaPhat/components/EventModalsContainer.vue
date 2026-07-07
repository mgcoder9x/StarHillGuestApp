<template>
  <div>
    <!-- Person Event Modal -->
    <PersonEventModal
      :show="showTable.person.isShow"
      :statistic="statisticAll"
      @close="$emit('close-person')"
    />

    <!-- Vehicle Event Modal -->
    <VehicleEventModal
      :show="showTable.vehicle.isShow"
      :vehicles="vehiclesCount"
      @close="$emit('close-vehicle')"
    />

    <!-- Virtual Fence Event Modal -->
    <VirtualFenceEventModal
      :show="showTable.virtualFence.isShow"
      :statistic="statisticAll"
      @close="$emit('close-virtual-fence')"
    />

    <!-- Fire Event Modal -->
    <FireEventModal
      :show="showTable.fire.isShow"
      :statistic="statisticAll"
      @close="$emit('close-fire')"
    />

    <!-- Conveyor Event Modal -->
    <ConveyorEventModal
      :show="showTable.conveyorBelt.isShow"
      :statistic="statisticAll"
      @close="$emit('close-conveyor')"
    />

    <!-- Protective Event Modal -->
    <ProtectiveEventModal
      :show="showTable.protective.isShow"
      :protective-items="protectiveCount"
      @close="$emit('close-protective')"
    />
  </div>
</template>

<script>
import PersonEventModal from './PersonEventModal.vue'
import VehicleEventModal from './VehicleEventModal.vue'
import VirtualFenceEventModal from './VirtualFenceEventModal.vue'
import FireEventModal from './FireEventModal.vue'
import ConveyorEventModal from './ConveyorEventModal.vue'
import ProtectiveEventModal from './ProtectiveEventModal.vue'

export default {
  name: 'EventModalsContainer',
  components: {
    PersonEventModal,
    VehicleEventModal,
    VirtualFenceEventModal,
    FireEventModal,
    ConveyorEventModal,
    ProtectiveEventModal
  },
  props: {
    showTable: {
      type: Object,
      required: true
    },
    statisticAll: {
      type: Object,
      required: true
    },
    vehiclesCount: {
      type: Array,
      required: true
    },
    protectiveCount: {
      type: Array,
      required: true
    }
  },
  emits: [
    'close-person',
    'close-vehicle', 
    'close-virtual-fence',
    'close-fire',
    'close-conveyor',
    'close-protective'
  ]
}
</script>