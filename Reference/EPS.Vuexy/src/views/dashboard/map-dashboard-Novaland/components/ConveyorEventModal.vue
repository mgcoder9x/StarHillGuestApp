<template>
  <BaseEventModal 
    :show="show"
    :title="$t('Events.EventModals.ConveyorBelt')"
    width="320px"
    max-height="35vh"
    @close="$emit('close')"
  >
    <b-col>
      <b-form-group :label="$t('Events.EventModals.Ripped')" label-cols-md="4">
        <b-form-input 
          :value="statistic.countTorn || 0" 
          readonly 
        />
      </b-form-group>
    </b-col>
    
    <b-col>
      <b-form-group :label="$t('Events.EventModals.Spilled')" label-cols-md="4">
        <b-form-input 
          :value="statistic.countOverflow || 0" 
          readonly 
        />
      </b-form-group>
    </b-col>
    
    <b-col>
      <b-form-group :label="$t('Events.EventModals.Deviated')" label-cols-md="4">
        <b-form-input 
          :value="statistic.countDeviated || 0" 
          readonly 
        />
      </b-form-group>
    </b-col>
    
    <b-col>
      <b-form-group :label="$t('Events.EventModals.Oversized')" label-cols-md="4">
        <b-form-input 
          :value="statistic.countOversized || 0" 
          readonly 
        />
      </b-form-group>
    </b-col>
  </BaseEventModal>
</template>

<script>
import BaseEventModal from './BaseEventModal.vue'

export default {
  name: 'ConveyorEventModal',
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