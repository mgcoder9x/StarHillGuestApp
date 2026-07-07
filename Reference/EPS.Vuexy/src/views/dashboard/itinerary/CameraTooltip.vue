<template>
  <div></div>
</template>

<script>
import L from 'leaflet'

/* ===== Constants & tiny helpers ===== */
const MAX_TOOLTIP_EVENTS = 15
const pad2 = n => n.toString().padStart(2, '0')
const fmtDate = s => { if (!s) return ''; const d = new Date(s); return `${pad2(d.getDate())}/${pad2(d.getMonth() + 1)}/${d.getFullYear()}` }
const fmtTime = s => { if (!s) return ''; const d = new Date(s); return `${pad2(d.getHours())}:${pad2(d.getMinutes())}` }
const chipCss = `
  width:24px;height:24px;background:#fff;border:2px solid #52aece;border-radius:50%;
  display:inline-flex;align-items:center;justify-content:center;color:#52aece;font-weight:700;
  font-size:12px;box-shadow:0 1px 3px rgba(0,0,0,.1);`

export default {
  name: 'CameraTooltip',
  
  props: {
    map: { type: Object, default: null },
    events: { type: Array, default: () => [] }
  },

  data() {
    return {
      tooltip: null,
      persistentTooltips: [], // Loại 1 - Simplified tooltips
      persistentNameTooltips: [] // Loại 2 - Name tooltips
    }
  },

  methods: {
    /* Build persistent name tooltip HTML - Chỉ hiển thị tên camera */
    buildPersistentNameTooltipHTML(camera) {
      const cameraName = camera.name || 'Camera'
      
      let html = `
        <div class="persistent-name-tooltip" style="padding:6px 10px;min-width:100px;max-width:250px;background:white;border-radius:6px;text-align:center;">
          <div class="tooltip-title" style="font-weight:600;color:#0f53b3;font-size:12px;line-height:1.3;word-break:break-word;white-space:normal;overflow-wrap:break-word;">
            ${cameraName}
          </div>
        </div>
      `
      
      return html
    },

    /* Build hover tooltip HTML - Hiển thị đầy đủ tên camera */
    buildHoverTooltipHTML(camera, related) {
      const cameraName = camera.name || 'Camera'
      
      let html = `
        <div class="custom-tooltip-content" style="padding:8px 12px;min-width:120px;max-width:300px;background:white;border-radius:8px;text-align:center;">
          <div class="tooltip-title" style="font-weight:600;color:#0f53b3;font-size:13px;line-height:1.4;word-break:break-word;white-space:normal;overflow-wrap:break-word;">
            ${cameraName}
          </div>
        </div>
      `
      
      return html
    },

    /* Build simplified tooltip HTML */
    buildSimplifiedTooltipHTML(camera, related) {
      const total = related.length
      const shown = related.slice(0, MAX_TOOLTIP_EVENTS)
      const hasMore = total > shown.length
      
      let html = `
        <div class="simplified-tooltip" style="padding:8px;display:flex;flex-direction:column;gap:6px;min-width:140px;">
          <div class="tooltip-title" style="font-weight:600;margin-bottom:6px;padding-bottom:5px;border-bottom:1px solid rgba(0,0,0,.08);color:#0f53b3;font-size:13px;text-align:center;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;letter-spacing:-.25px;">
            ${camera.name || 'Camera'} <span style="font-size:11px;color:#64748b;">(${total})</span>
          </div>
          <div style="display:flex;flex-wrap:wrap;gap:5px;justify-content:center;">
      `
      
      shown.forEach(ev => {
        const idx = this.events.findIndex(e => e.eventId === ev.eventId) + 1
        html += `<span style="${chipCss}">${idx}</span>`
      })
      
      if (hasMore) {
        html += `
          <span style="width:24px;height:24px;background:#f1f5f9;border:1px dashed #94a3b8;border-radius:50%;
          display:inline-flex;align-items:center;justify-content:center;color:#64748b;font-weight:700;font-size:10px;">+</span>`
      }
      
      html += `</div></div>`
      return html
    },

    /* Show persistent name tooltips for all cameras (Loại 2) */
    showPersistentNameTooltipsForAllCameras(cameras) {
      if (!this.map || !cameras) return
      
      try {
        this.clearPersistentNameTooltips()
        
        cameras.forEach(cam => {
          const tip = L.tooltip({ 
            permanent: true, 
            direction: 'top', 
            offset: [0, -10], 
            className: 'custom-device-tooltip persistent-name-tooltip' 
          })
            .setLatLng([cam.latitude, cam.longitude])
            .setContent(this.buildPersistentNameTooltipHTML(cam))
            .addTo(this.map)
            
          this.persistentNameTooltips.push(tip)
        })
        
        console.log('Persistent name tooltips shown for', cameras.length, 'cameras')
      } catch (error) {
        console.error('Error showing persistent name tooltips:', error)
      }
    },

    /* Show hover tooltip - Hiển thị khi hover */
    showHoverTooltip(camera, related) {
      if (!this.map) return
      
      try {
        this.hideTooltip()

        // Luôn hiển thị tooltip với tên camera khi hover
        const content = this.buildHoverTooltipHTML(camera, related)

        this.tooltip = L.tooltip({ 
          permanent: false, 
          direction: 'top', 
          offset: [0, -10], 
          className: 'custom-device-tooltip hover-tooltip' 
        })
          .setLatLng([camera.latitude, camera.longitude])
          .setContent(content)
          .addTo(this.map)
      } catch (error) {
        console.error('Error showing camera tooltip:', error)
      }
    },

    /* Show simplified tooltips for all cameras (Loại 1) */
    showSimplifiedTooltipsForAllCameras(cameras, getRelatedEventsCallback) {
      if (!this.map || !cameras) return
      
      try {
        this.clearAllPersistentTooltips()
        
        cameras.forEach(cam => {
          const related = getRelatedEventsCallback ? getRelatedEventsCallback(cam) : []
          if (!related.length) return
          
          const tip = L.tooltip({ 
            permanent: true, 
            direction: 'top', 
            offset: [0, -10], 
            className: 'custom-device-tooltip simplified-tooltip' 
          })
            .setLatLng([cam.latitude, cam.longitude])
            .setContent(this.buildSimplifiedTooltipHTML(cam, related))
            .addTo(this.map)
            
          this.persistentTooltips.push(tip)
        })
      } catch (error) {
        console.error('Error showing simplified tooltips:', error)
      }
    },

    /* Hide current hover tooltip */
    hideTooltip() {
      try {
        if (this.tooltip) { 
          this.tooltip.remove()
          this.tooltip = null 
        }
      } catch (error) {
        console.error('Error hiding tooltip:', error)
      }
    },

    /* Clear persistent name tooltips (Loại 2) */
    clearPersistentNameTooltips() {
      try {
        this.persistentNameTooltips.forEach(t => {
          if (t && typeof t.remove === 'function') {
            t.remove()
          }
        })
        this.persistentNameTooltips = []
      } catch (error) {
        console.error('Error clearing persistent name tooltips:', error)
      }
    },

    /* Clear all persistent tooltips (Loại 1) */
    clearAllPersistentTooltips() {
      try {
        this.persistentTooltips.forEach(t => {
          if (t && typeof t.remove === 'function') {
            t.remove()
          }
        })
        this.persistentTooltips = []
      } catch (error) {
        console.error('Error clearing persistent tooltips:', error)
      }
    },

    /* Clean up on destroy */
    cleanup() {
      this.hideTooltip()
      this.clearAllPersistentTooltips()
      this.clearPersistentNameTooltips()
    }
  },

  beforeDestroy() {
    this.cleanup()
  }
}
</script>

<style lang="scss">
/*----------------------------------------------
   * TOOLTIPS & POPUPS STYLES
   *----------------------------------------------*/
/* Tooltip mạnh hơn với deep selector */
.leaflet-tooltip.custom-device-tooltip {
  padding: 0 !important;
  background: white !important;
  border: none !important;
  border-radius: 8px !important;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1) !important;

  .leaflet-tooltip-content {
    padding: 0 !important;
    margin: 0 !important;
    background: transparent !important;
    border: none !important;
  }

  /* Hover tooltip - hiển thị đầy đủ tên */
  &.hover-tooltip {
    .custom-tooltip-content {
      padding: 8px 12px !important;
      min-width: 120px !important;
      max-width: 300px !important;
      background: white !important;
      border-radius: 8px !important;
      text-align: center !important;

      .tooltip-title {
        font-weight: 600 !important;
        color: #0f53b3 !important;
        font-size: 13px !important;
        line-height: 1.4 !important;
        word-break: break-word !important;
        white-space: normal !important;
        overflow-wrap: break-word !important;
        hyphens: auto !important;
        max-width: 100% !important;
      }
    }
  }

  .custom-tooltip-content {
    padding: 10px !important;
    min-width: 180px !important;
    max-width: 280px !important;
    background: white !important;
    border-radius: 8px !important;

    .tooltip-title {
      font-weight: 550 !important;
      margin-bottom: 8px !important;
      padding-bottom: 5px !important;
      border-bottom: 1px solid rgba(0, 0, 0, 0.06) !important;
      color: #0f53b3 !important;
      font-size: 12px !important;
    }

    .tooltip-events {
      display: flex !important;
      flex-direction: column !important;
      gap: 6px !important;
    }

    .tooltip-event {
      display: flex !important;
      align-items: center !important;
      gap: 12px !important;
      font-size: 12px !important;
    }
  }
}

/* Tooltip đơn giản */
.leaflet-tooltip.custom-device-tooltip.simplified-tooltip {
  background: rgba(255, 255, 255, 0.98) !important;
  border: none !important;
  box-shadow: 0 3px 10px rgba(0, 0, 0, 0.18) !important;
  padding: 8px 12px !important;
  border-radius: 12px !important;
  white-space: normal !important;
  min-width: auto !important;
  backdrop-filter: blur(10px) !important;
  -webkit-backdrop-filter: blur(10px) !important;
  border: 1px solid rgba(226, 232, 240, 0.6) !important;
}

/* Persistent name tooltip - chỉ hiển thị tên */
.leaflet-tooltip.custom-device-tooltip.persistent-name-tooltip {
  background: rgba(255, 255, 255, 0.95) !important;
  border: none !important;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15) !important;
  padding: 6px 10px !important;
  border-radius: 6px !important;
  white-space: normal !important;
  min-width: auto !important;
  backdrop-filter: blur(8px) !important;
  -webkit-backdrop-filter: blur(8px) !important;
  border: 1px solid rgba(15, 83, 179, 0.2) !important;

  .persistent-name-tooltip {
    .tooltip-title {
      font-weight: 600 !important;
      color: #0f53b3 !important;
      font-size: 12px !important;
      line-height: 1.3 !important;
      word-break: break-word !important;
      white-space: normal !important;
      overflow-wrap: break-word !important;
    }
  }
}

/* CSS global cho event-index trong tooltip */
.leaflet-container .custom-tooltip-content .event-index {
  width: 24px !important;
  height: 24px !important;
  background: #52aece !important;
  border: 2px solid #ffffff !important;
  border-radius: 50% !important;
  display: flex !important;
  align-items: center !important;
  justify-content: center !important;
  color: #ffffff !important;
  font-weight: 600 !important;
  font-size: 11px !important;
  line-height: 1 !important;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1) !important;
  min-width: 24px !important;
  flex-shrink: 0 !important;
  text-align: center !important;
}
</style>
