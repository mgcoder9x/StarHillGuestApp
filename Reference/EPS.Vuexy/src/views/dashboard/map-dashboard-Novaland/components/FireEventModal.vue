<template>
  <BaseEventModal 
    :show="show"
    :title="$t('Events.EventModals.FireAndSmoke')"
    width="300px"
    max-height="30vh"
    @close="$emit('close')"
  >
    <b-col>
      <b-form-group :label="$t('Events.MapDashboard.Fire')" label-cols-md="4">
        <b-form-input 
          :value="statistic.countFire || 0" 
          readonly 
        />
      </b-form-group>
    </b-col>
    
    <b-col>
      <b-form-group :label="$t('Events.MapDashboard.Smoke')" label-cols-md="4">
        <b-form-input 
          :value="statistic.countSmoke || 0" 
          readonly 
        />
      </b-form-group>
    </b-col>
  </BaseEventModal>
</template>

<script>
import BaseEventModal from './BaseEventModal.vue'

export default {
  name: 'FireEventModal',
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