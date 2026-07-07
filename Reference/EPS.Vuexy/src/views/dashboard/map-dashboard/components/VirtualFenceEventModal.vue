<template>
  <BaseEventModal 
    :show="show"
    :title="$t('Events.EventModals.VirtualFence')"
    width="300px"
    max-height="30vh"
    @close="$emit('close')"
  >
    <b-col>
      <b-form-group :label="$t('Events.EventModals.People')" label-cols-md="4">
        <b-form-input 
          :value="statistic.countPerson || 0" 
          readonly 
        />
      </b-form-group>
    </b-col>

  </BaseEventModal>
</template>

<script>
import BaseEventModal from './BaseEventModal.vue'

export default {
  name: 'VirtualFenceEventModal',
  components: {
    BaseEventModal
  },
  props: {
    show: {
      type: Boolean,
      default: false
    },
    statistic: {
      type: Object,
      default: () => ({})
    }
  },
  emits: ['close']
}
</script>