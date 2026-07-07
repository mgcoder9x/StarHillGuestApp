<template>
  <BaseEventModal 
    :show="show"
    :title="$t('Events.EventModals.ProtectiveEquipment')"
    width="360px"
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
          <b-th style="width: 70%">{{ $t('Events.EventModals.MissingEquipment') }}</b-th>
          <b-th style="width: 30%" class="text-center">{{ $t('Events.EventModals.Quantity') }}</b-th>
        </b-tr>
      </b-thead>
      <b-tbody>
        <b-tr v-for="protective in protectiveItems" :key="protective.id">
          <b-td class="text-truncate custom-cell" style="width: 70%">
            {{ protective.name }}
          </b-td>
          <b-td style="width: 30%">
            <b-button 
              class="font-weight-bold py-0 w-100"
              variant="outline-success"
            >
              {{ protective.count }}
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
  name: 'ProtectiveEventModal',
  components: {
    BaseEventModal
  },
  props: {
    show: {
      type: Boolean,
      default: false
    },
    protectiveItems: {
      type: Array,
      default: () => []
    }
  },
  emits: ['close']
}
</script>

