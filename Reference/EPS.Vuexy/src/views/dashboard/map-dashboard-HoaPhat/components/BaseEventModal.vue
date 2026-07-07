<template>
  <transition name="slide-left">
    <b-card 
      v-if="show"
      class="shadow-sm position-absolute hide-scrollbar map-overlay-card" 
      :style="{ width, maxHeight }" 
      @wheel.stop
    >
      <b-button 
        variant="outline-secondary" 
        size="sm"
        class="position-absolute btn-icon rounded-circle p-0" 
        style="top: 2px; right: 2px; width: 28px; height: 28px;" 
        @click="$emit('close')"
      >
        <feather-icon icon="XIcon" size="16" />
      </b-button>
      
      <h5 class="text-uppercase text-center font-weight-bold">
        {{ title }}
      </h5>
      
      <slot></slot>
    </b-card>
  </transition>
</template>

<script>
export default {
  name: 'BaseEventModal',
  props: {
    show: {
      type: Boolean,
      default: false
    },
    title: {
      type: String,
      required: true
    },
    width: {
      type: String,
      default: '350px'
    },
    maxHeight: {
      type: String,
      default: '40vh'
    }
  },
  emits: ['close']
}
</script>

<style lang="scss" scoped>
.slide-left-enter-active,
.slide-left-leave-active {
  transition: all 0.5s ease;
}

.slide-left-enter,
.slide-left-leave-to {
  transform: translateX(-80px);
  opacity: 0;
}

.map-overlay-card {
  backdrop-filter: blur(5px);
  border: 1px solid rgba(0, 0, 0, 0.1);
  bottom: 20px;
  right: 20px;
  z-index: 1000;
  overflow-y: auto;
}
</style>