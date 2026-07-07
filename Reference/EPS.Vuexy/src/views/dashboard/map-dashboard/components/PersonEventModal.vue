<template>
  <BaseEventModal 
    :show="show"
    :title="$t('Events.EventModals.Recognition')"
    width="350px"
    max-height="30vh"
    @close="$emit('close')"
  >
    <b-col>
      <b-form-group :label="$t('Events.EventModals.RegisteredPeople')" label-cols-md="5">
        <b-form-input 
          :value="statistic.countFaceRegister || 0" 
          readonly 
        />
      </b-form-group>
    </b-col>
    
    <b-col>
      <b-form-group :label="$t('Events.EventModals.UnregisteredPeople')" label-cols-md="5">
        <b-form-input 
          :value="statistic.countFaceNotRegister || 0" 
          readonly 
        />
      </b-form-group>
    </b-col>
  </BaseEventModal>
</template>

<script>
import BaseEventModal from './BaseEventModal.vue'

export default {
  name: 'PersonEventModal',
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