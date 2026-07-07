<template>
  <!-- Cột bên trái -->
  <div class="d-flex align-items-center">
    <div class="mr-2">
      <b-img :src="`${baseURL}${eventData.image}`" height="100" width="100" rounded class="mr-1 border" style="
              object-fit: cover;
              object-position: center;
          " />
    </div>
    <div>
      <div class="text-muted small">
        {{ eventData.accessTime }}
      </div>
      <div>
        {{ $t('VehicleEvent.Field.Area') }}:
        <span class="text-primary"> {{ eventData.areaName }}</span>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  props: {
    baseURL: {
      type: String,
      required: true,
    },
    eventData: {
      type: Object,
      required: true
    }
  },
  methods:{

  }
};
</script>

<style scoped>
.text-danger {
  color: #e74c3c !important;
}

.text-success {
  color: #27ae60 !important;
}
</style>
