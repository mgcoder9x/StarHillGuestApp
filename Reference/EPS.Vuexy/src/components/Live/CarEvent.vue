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
      <div>
        {{ eventData.licensePlate }}
      </div>
      <div class="text-muted small">
        {{ eventData.accessTime }}
      </div>
      <div>
        {{ $t('VehicleEvent.Field.Area') }}:
        <span class="text-primary"> {{ eventData.areaName }}</span>
      </div>
      <div  :style="{ color: eventData.warningColor }">
        {{ $t(convertStatus(eventData.warningLevelId)) }}
      </div>
      <div class="text-muted">
        {{ $t('VehicleEvent.Field.Merchandise') }}:
        <span class="text-success"> {{ $t(convertCargo(eventData.warningLevelId)) }}</span>
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
    convertCargo(warningLevelId){
      debugger
      switch (warningLevelId){
        case 303: return 'CarEvent.Common.WarningType.Wooden'
        case 304: return 'CarEvent.Common.WarningType.Iron'
        case 305: return 'CarEvent.Common.WarningType.Other'
      }
      return ""
    },
    //Nghi sơn
    // convertStatus(warningLevelId){
    //   switch (warningLevelId){
    //     case 301: return 'CarEvent.Common.WarningType.None'
    //     case 302: return 'CarEvent.Common.WarningType.Suspect'
    //     case 303: return 'CarEvent.Common.WarningType.HasMerchandise'
    //     case 304: return 'CarEvent.Common.WarningType.HasMerchandise'
    //     case 305: return 'CarEvent.Common.WarningType.HasMerchandise'
    //   }
    //   return ""
    // },
    //Novaland
    convertStatus(warningLevelId){
      switch (warningLevelId){
        case 311: return 'Phủ bạt'
        case 312: return 'Không phủ bạt'
      }
      return ""
    },
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
