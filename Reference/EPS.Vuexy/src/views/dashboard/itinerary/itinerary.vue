<template>
  <div class="map-wrapper">
    <l-map
      ref="latLngMap"
      :key="isShowImgMap ? 'imgMap' : 'osmMap'"
      class="rounded"
      :zoom="zoom"
      :style="{ height: mapHeight, zIndex: 1, border: '3px solid #4a90e2' }"
      :center="center"
      :zoom-snap="0.1"
      :crs="crs"
      @ready="onMapReady"
      :options="{ attributionControl: false, zoomControl: false, closePopupOnClick: false }"
    >
      <l-image-overlay
        v-if="isShowImgMap && urlImgOverlay"
        :url="urlImgOverlay"
        :bounds="imageBounds"
      />
      <!-- Control - Luôn hiển thị đầy đủ -->
      <l-control
        style="width: 40px; z-index: 1000"
        position="topright"
        class="mt-1 mr-2"
      >
        <div
          class="custom-control-box d-flex flex-column align-items-center justify-content-center"
        >
          <!-- Nút toggle sidebar - Luôn hiển thị -->
          <b-button
            variant="outline-primary"
            class="btn-circle custom-btn mb-2"
            @click="toggleSidebar(!showCard)"
            :title="sidebarTooltip"
          >
            <feather-icon
              :icon="showCard ? 'XIcon' : 'EyeIcon'"
              size="18"
            />
          </b-button>

          <!-- Toggle Simplified Tooltips Button (Loại 1) - Luôn hiển thị khi có data -->
          <b-button
            v-if="mapPoints.length > 0"
            variant="outline-success"
            class="btn-circle custom-btn mb-2"
            :class="{ 'active': showSimplifiedTooltips }"
            @click="toggleSimplifiedTooltips"
            :title="cameraEventsTooltip"
          >
            <feather-icon
              icon="TagIcon"
              size="18"
            />
          </b-button>

          <!-- Toggle Hover Tooltips Button (Loại 2) - Luôn hiển thị khi có data -->
          <b-button
            v-if="mapPoints.length > 0"
            variant="outline-info"
            class="btn-circle custom-btn mb-2"
            :class="{ 'active': showHoverTooltips }"
            @click="toggleHoverTooltips"
            :title="cameraNamestooltip"
          >
            <feather-icon
              icon="InfoIcon"
              size="18"
            />
          </b-button>
        </div>
      </l-control>

      <!-- Sidebar -->
      <SidebarMap
        :show="showCard"
        :events="events"
        :base-u-r-l="baseURL"
        :active-start-event-id="selectedStartEventId"
        :passed-event-ids="passedEventIds"
        @close="toggleSidebar(false)"
        @focus-camera="focusCamera"
        @start-route-from-event="handleStartRouteFromEvent"
        @show-modal-event="showModalEvent"
      />

      <!-- Base map -->
      <l-tile-layer
        v-if="!isShowImgMap"
        :url="urlOSM"
      />

      <!-- TrackViewer Component - Only for displaying points -->
      <TrackViewer 
        ref="trackViewer"
        :points="mapPoints"
        :all-events="events"
        :map="map"
        :auto-fit-bounds="true"
        :min-zoom="10"
        :max-zoom="16"
        :start-from-point-id="startFromPointId"
        :highlighted-point-id="highlightedPointId"
        :highlighted-route-segment="highlightedRouteSegment"
        :show-route="true"
        :is-image-map="isShowImgMap"
        @point-click="handleTrackPointClick"
        @point-hover="handleTrackPointHover"
        @fit-bounds="handleFitBounds"
        @track-updated="handleTrackUpdated"
        @progress-update="handleTrackProgressUpdate"
        @cycle-complete="handleTrackCycleComplete"
      />
    </l-map>

    <b-modal
      v-model="modalImgEventShow"
      :title="$t('Events.SearchForm.EventPhotos')"
      size="lg"
      @hidden="clearEventFiles"
    >
      <EventCarousel
        :list-event-files="listEventFiles"
        :base-u-r-l="baseURL"
      />
      <template #modal-footer><p /></template>
    </b-modal>
  </div>
</template>

<script>
  import useAppConfig from '@core/app-config/useAppConfig'
  import { computed } from '@vue/composition-api'
  import 'leaflet/dist/leaflet.css'
  import L from 'leaflet'
  import { LControl, LMap, LTileLayer, LImageOverlay } from 'vue2-leaflet'
  import SidebarMap from '@/views/dashboard/itinerary/SidebarMap.vue'
  import TrackViewer from './TrackViewer.vue' // Fixed: Use relative path
  import EventCarousel from '@/components/EventCarousel.vue'
  import { $themeConfig } from '@themeConfig'

  export default {
    components: {
      LMap,
      LTileLayer,
      LControl,
      SidebarMap,
      TrackViewer,
      EventCarousel,
      LImageOverlay,
    },
    props: {
      url: { type: String, default: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png' },
      propCoordinate: { type: Object, default: () => ({ lat: 15.3842, lng: 108.79383, zoom: 16 }) },
    },

    data() {
      return {
        /* Map state */
        center: [15.3842, 108.79383],
        zoom: 13,
        fitInfo: null,
        lastFitInfo: null, // Lưu fitInfo gần nhất để tái sử dụng
        map: null,

        /* Sources & overlays */
        urlOSM: this.url,
        isShowImgMap: false,
        urlImgOverlay: null,
        imageBounds: [
          [0, 0],
          [100, 100],
        ],
        crs: L.CRS.EPSG3857,
        companyId: null,
        companyImgPath: null,

        listDevices: new Map(),
        /* Data */
        events: [],
        mapPoints: [], // Danh sách các điểm sẽ hiển thị trên map (unique devices)

        /* UI */
        showCard: true,
        modalImgEventShow: false,
        listEventFiles: [],

        /* Viewport */
        viewportInitialized: false,
        rootMapCoordinate: null,
        searchForm: { type: 1, licenseOrNameCode: null, dateFrom: null, dateTo: null },

        variants: ['primary', 'success', 'danger', 'info', 'warning', 'secondary', 'dark', 'light'],

        /* Highlighting */
        highlightedPointId: null,
        highlightedRouteSegment: null,
        eventsToPointsMap: new Map(), // Map từ eventId đến point index
        startFromPointId: null,
        selectedStartEventId: null,
        passedEventIds: [],

        /* Tooltip */
        showHoverTooltips: false, // Loại 2 - Persistent name tooltips (mặc định tắt)
        showSimplifiedTooltips: false, // Loại 1 - Simplified tooltips (mặc định tắt)
      }
    },

    computed: {
      baseURL() {
        return process.env.VUE_APP_BASE_URL
      },
      eventId() {
        return this.$route.params.eventId
      },
      type() {
        return this.$route.params.type
      },
      dateFrom() {
        return this.$route.params.dateFrom
      },
      dateTo() {
        return this.$route.params.dateTo
      },
      currentLocale() {
        return this.$i18n.locale
      },
      mapImg() {
        if (!this.companyImgPath) return null
        const url =
          process.env.NODE_ENV === 'development'
            ? $themeConfig.app.apiURLDev
            : $themeConfig.app.apiURL
        return url + '/MapImg/' + this.companyImgPath
      },
    sidebarTooltip() {
      this.currentLocale
      return this.showCard
        ? this.$t('Map.Controls.HideInfo')
        : this.$t('Map.Controls.ShowInfo')
    },
    cameraEventsTooltip() {
      this.currentLocale
      return this.showSimplifiedTooltips
        ? this.$t('Map.Controls.HideCameraEvents')
        : this.$t('Map.Controls.ShowCameraEvents')
    },
    cameraNamestooltip() {
   
      this.currentLocale
      return this.showHoverTooltips
        ? this.$t('Map.Controls.HideCameraNames')
        : this.$t('Map.Controls.ShowCameraNames')
    }
  },

    setup() {
      const {
        navbarType,
        footerType,
        breadcrumbType,
        contentWidth,
        isVerticalMenuCollapsed,
        layoutType,
        isNavMenuHidden,
      } = useAppConfig()
      const mapHeight = computed(() => {
        const navH = navbarType.value === 'hidden' ? 0 : navbarType.value === 'floating' ? 75 : 70
        const footH = footerType.value === 'hidden' ? 0 : footerType.value === 'sticky' ? 60 : 55
        const bcH = breadcrumbType.value && breadcrumbType.value !== 'hidden' ? 50 : 0
        let pad = contentWidth.value === 'boxed' ? 24 : 32
        if (isVerticalMenuCollapsed.value) pad -= 8
        let vh = layoutType.value === 'horizontal' ? (isNavMenuHidden.value ? 99 : 92) : 99
        return `calc(${vh}vh - ${navH + footH + bcH + pad}px)`
      })

      return { mapHeight }
    },

    methods: {
      toggleSidebar(v) {
        this.showCard = v
        
        if (v === true) {
          // Khi mở sidebar, chỉ ẩn simplified tooltips nếu đang hiển thị
          if (this.showSimplifiedTooltips) {
            this.hideSimplifiedTooltips()
          }
          
          const savedFitInfo = this.lastFitInfo || this.fitInfo
          
          if (savedFitInfo) {
            console.log('Restoring saved fitInfo:', savedFitInfo)
            this.applyFitInfo(savedFitInfo)
          } else {
            console.log('No saved fitInfo, using default center/zoom')
            this.map.setView(this.center, this.zoom, { animate: true })
          }
        } else {
          // Khi đóng sidebar, có thể hiển thị simplified tooltips nếu đang bật
          this.$nextTick(() => {
            if (this.showSimplifiedTooltips) {
              this.showAllSimplifiedTooltips()
            }
          })
        }
        
        console.log('Sidebar toggled:', v)
      },

      // Tách logic apply fitInfo thành method riêng
      applyFitInfo(fitInfo) {
        if (!this.map || !fitInfo) return
        
        try {
          if (fitInfo.bounds && this.mapPoints.length > 1) {
            this.map.fitBounds(fitInfo.bounds, fitInfo.boundsOptions)
          } else if (fitInfo.center && fitInfo.zoom) {
            this.map.setView(fitInfo.center, fitInfo.zoom, { animate: true })
          }
          
          console.log('Applied fitInfo successfully')
        } catch (error) {
          console.error('Error applying fitInfo:', error)
        }
      },

      focusCamera(ev) {
        console.log('Focus camera:', ev)
        if (!this.map || !this.$refs.latLngMap?.mapObject) {
          console.warn('Map not available for focusCamera')
          return
        }

        try {
          this.$refs.latLngMap.mapObject.setView(
            [ev.lat || ev.latitude, ev.lng || ev.longitude],
            17,
            { animate: true }
          )
          console.log('Map focused on:', ev.lat || ev.latitude, ev.lng || ev.longitude)
        } catch (error) {
          console.error('Error focusing camera:', error)
        }
      },

      showModalEvent(eventId) {
        this.loadEventFiles(eventId)
        this.modalImgEventShow = true
      },

      clearEventFiles() {
        this.listEventFiles = []
      },

      onMapReady(map) {
        this.map = map
        console.log('Map ready:', !!this.map)
        this.$nextTick(() => {
          if (!this.viewportInitialized) {
            if (this.isShowImgMap && this.urlImgOverlay) {
              this.calculateImageBounds()
            } else {
              this.determineInitialViewport()
            }
          }
        })
      },

      highlightRoute(event) {
        console.log('Highlight route:', event)
        
        if (!event || !event.deviceId) return
        
        // Tìm device point tương ứng với event hiện tại
        const currentDevicePoint = this.mapPoints.find(point => point.deviceId === event.deviceId)
        
        if (currentDevicePoint) {
          // Highlight camera hiện tại
          this.highlightedPointId = currentDevicePoint.id
          
          // Tìm event tiếp theo theo thời gian (không phải theo index trong sidebar)
          const currentEventTime = new Date(event.accessTime)
          
          // Sắp xếp tất cả events theo thời gian và tìm event tiếp theo
          const sortedEvents = [...this.events].sort((a, b) => {
            return new Date(a.accessTime) - new Date(b.accessTime)
          })
          
          const currentEventIndex = sortedEvents.findIndex(e => e.eventId === event.eventId)
          
          if (currentEventIndex >= 0 && currentEventIndex < sortedEvents.length - 1) {
            const nextEvent = sortedEvents[currentEventIndex + 1]
            const nextDevicePoint = this.mapPoints.find(point => point.deviceId === nextEvent.deviceId)
            
            if (nextDevicePoint) {
              // Highlight route segment từ current → next
              this.highlightedRouteSegment = {
                fromPointId: currentDevicePoint.id,
                toPointId: nextDevicePoint.id,
                fromEvent: event,
                toEvent: nextEvent
              }
              
              console.log('Route highlighted:', {
                from: `${event.deviceName} (${new Date(event.accessTime).toLocaleTimeString()})`,
                to: `${nextEvent.deviceName} (${new Date(nextEvent.accessTime).toLocaleTimeString()})`,
                direction: 'forward'
              })
            }
          } else {
            // Nếu là event cuối cùng, không có route để highlight
            this.highlightedRouteSegment = null
            console.log('Last event - no next route to highlight')
          }
          
          console.log('Highlighted:', {
            currentPointId: this.highlightedPointId,
            routeSegment: this.highlightedRouteSegment
          })
        }
      },

      unhighlightRoute() {
        console.log('Unhighlight route')
        this.highlightedPointId = null
        this.highlightedRouteSegment = null
      },

      determineInitialViewport() {
        if (!this.map) {
          console.warn('Map not available for determineInitialViewport')
          return
        }

        try {
          if (this.rootMapCoordinate) {
            this.map.setView(
              [this.rootMapCoordinate.lat, this.rootMapCoordinate.lng],
              this.rootMapCoordinate.zoom
            )
          } else {
            this.map.setView(this.center, this.zoom)
          }
          this.viewportInitialized = true
          console.log('Initial viewport set')
        } catch (error) {
          console.error('Error setting initial viewport:', error)
        }
      },
      
      normalizeCoord(coord) {
        if (coord === null || coord === undefined || coord === '' || isNaN(coord)) {
          return null
        }
        const num = parseFloat(coord)
        
        if (isNaN(num)) {
          return null
        }

        // Với bản đồ số (OSM), 0 thường là tọa độ sai hoặc thiếu
        // Với bản đồ hình ảnh (Simple CRS), 0 có thể là tọa độ hợp lệ (gốc tọa độ)
        if (!this.isShowImgMap && num === 0) {
          return null
        }
        
        return Math.round(num * 1000000) / 1000000
      },
      async loadCompany() {
        try {
          if (this.companyId) {
            const response = await this.$services.get(`/company/${this.companyId}`)
            this.companyImgPath = response.data.imgPath

            if (this.companyImgPath) {
              this.urlImgOverlay = this.mapImg
              this.isShowImgMap = true
              this.crs = L.CRS.Simple
              // Set initial zoom/center for Image Map to avoid distortion if re-render is slow
              this.zoom = -1
              this.center = [0, 0]
              this.calculateImageBounds()
            } else {
              this.isShowImgMap = false
              this.crs = L.CRS.EPSG3857
              this.zoom = 13
              this.center = [15.3842, 108.79383]
            }
          }
        } catch (error) {
          console.error('Error loading company:', error)
        }
      },
      calculateImageBounds() {
        if (!this.urlImgOverlay) return

        const img = new Image()
        img.onload = () => {
          const width = img.width
          const height = img.height

          this.imageBounds = [
            [0, 0],
            [height, width],
          ]

          console.log('Image map bounds calculated:', this.imageBounds)
          
          if (this.map) {
             this.map.invalidateSize()
             // Nếu là map hình ảnh và chưa có center hợp lệ (vẫn ở [0,0] hoặc default), fit bounds
             if (this.center[0] === 0 || (this.center[0] === 15.3842 && this.center[1] === 108.79383)) {
                this.map.fitBounds(this.imageBounds)
             }
          }
        }
        img.src = this.urlImgOverlay
      },

      async loadDevices() {
        try {
          const { data } = await this.$services.get('/device/getAllCam')
          const devices = data.data || []

          devices.forEach(device => {
            const normalizedLat = this.normalizeCoord(device.latitude)
            const normalizedLng = this.normalizeCoord(device.longitude)

            if (normalizedLat !== null && normalizedLng !== null) {
              device.latitude = normalizedLat
              device.longitude = normalizedLng
              
              this.listDevices.set(device.id, {
                ...device,
                isValid: true
              })
            } else {
              console.warn(`Device ${device.id} has invalid coordinates:`, {
                originalLat: device.latitude,
                originalLng: device.longitude,
                normalizedLat,
                normalizedLng
              })
            }
          })
          console.log("Devices:", this.listDevices)
          console.log(`Loaded ${this.listDevices.size} valid devices`)
        } catch (e) {
          console.error('Error loading devices:', e)
          this.listDevices.clear()
        }
      },

       async loadEvents() {
        try {
          this.events = []
          this.mapPoints = []
          this.eventsToPointsMap.clear()
          
          const pagination = { 
            page: 1, 
            itemsPerPage: 99999, 
            sortBy: 'accessTime', 
            sortDesc: true 
          }
          
          const query = { 
            type: this.searchForm.type, 
            dateFrom: this.searchForm.dateFrom, 
            dateTo: this.searchForm.dateTo, 
            licensePlate: this.type == 2 ? this.eventId : null,
            personId: this.type == 1 ? this.eventId : null
          }
          
          console.log('Loading events with query:', query)
          
          const cleanedQuery = {}
          Object.entries(query).forEach(([key, value]) => {
            if (value !== null && value !== undefined && value !== '' && value !== 'null') {
              cleanedQuery[key] = value
            }
          })
          
          const qs = `${new URLSearchParams(pagination)}&${new URLSearchParams(cleanedQuery)}`
          console.log('Final query string:', qs)
          
          const { data } = await this.$services.get(`/dashboard/itinerary?${qs}`)
          const rawEvents = data?.data?.data || []
          
          console.log(`API Response: ${rawEvents.length} events`)
          console.log("Events Raw:", rawEvents)

          if (rawEvents.length > 0) {
            // Sắp xếp events theo thời gian để tạo route đúng thứ tự
            const sortedEvents = rawEvents.sort((a, b) => {
              return new Date(a.accessTime) - new Date(b.accessTime)
            })
            
            sortedEvents.forEach((event, eventIndex) => {
              const device = this.listDevices.get(event.deviceId)

              if (!device) {
                return
              }

              if (event.eventTypeId !== 200 && event.eventTypeId !== 300) {
                return
              }
              
              const lat = device.latitude
              const lng = device.longitude
              const eventTypeId = device.eventTypeId
              const deviceName = device.name || event.deviceName

              // Add to events array với thứ tự đã sort
              this.events.push({
                ...event,
                latitude: lat,
                longitude: lng,
                lat,
                lng,
                eventTypeId,
                deviceName,
                iconColor: this.randomVariant(),
                routeIndex: eventIndex // Thêm index cho route
              })

              const currentIndex = this.mapPoints.length
              
              const pointData = {
                id: `event-${event.eventId}`,
                eventId: event.eventId,
                deviceId: event.deviceId,
                deviceName: deviceName,
                lat: lat,
                lng: lng,
                latitude: lat,
                longitude: lng,
                eventTypeId: eventTypeId,
                index: currentIndex,
                sequenceNumber: currentIndex + 1,
                timestamp: event.accessTime,
                accessTime: event.accessTime,
                routeIndex: eventIndex
              }
              
              this.mapPoints.push(pointData)
              
              // Map event ID đến point để dễ tìm kiếm
              this.eventsToPointsMap.set(event.eventId, pointData)
            })
            
          } else {
            console.log('No events found for the given criteria')
          }
          
          // Debug: Log mapPoints structure
          console.log(`TrackViewer mapPoints:`, this.mapPoints)
          if (this.mapPoints.length > 0) {
            console.log('Sample mapPoint structure:', this.mapPoints[0])
          }
          
        } catch (e) {
          console.error('Error loading events:', e)
          
          if (e.response) {
            console.error('API Error Response:', {
              status: e.response.status,
              statusText: e.response.statusText,
              data: e.response.data,
              url: e.config?.url
            })
          }
          
          this.events = []
          this.mapPoints = []
        }
      },

      randomVariant() {
        return this.variants[Math.floor(Math.random() * this.variants.length)]
      },

      async loadEventFiles(eventId) {
        try {
          const { data } = await this.$services.get(`/event/eventFilesById/${eventId}`)
          this.listEventFiles = data || []
          console.log(`Event Files loaded:`, this.listEventFiles)
        } catch (e) {
          console.error('Error loading event files:', e)
          this.listEventFiles = []
        }
      },

      // Handle auto-fit bounds from TrackViewer
      handleFitBounds(fitInfo) {
        console.log('Fitting map to bounds:', fitInfo)
        
        if (!this.map || !fitInfo) return
        
        // Lưu fitInfo để tái sử dụng sau này
        this.fitInfo = fitInfo
        this.lastFitInfo = { ...fitInfo } // Deep copy để tránh reference issues
        
        this.applyFitInfo(fitInfo)
        
        console.log(`Map fitted to show ${this.mapPoints.length} markers at zoom ${fitInfo.zoom}`)
      },

      // Handle track updates
      handleTrackUpdated(trackData) {
        console.log('Track updated:', {
          points: trackData.points.length,
          center: trackData.center,
          hasBounds: !!trackData.bounds
        })
      },

      handleStartRouteFromEvent(event) {
        if (!event) return

        this.selectedStartEventId = event.eventId
        const startPoint = this.eventsToPointsMap.get(event.eventId)
        this.startFromPointId = startPoint?.id || `event-${event.eventId}`

        this.highlightedPointId = null
        this.highlightedRouteSegment = null
        this.passedEventIds = []
      },

      handleTrackProgressUpdate(payload) {
        const passedPointIds = payload?.passedPointIds || []
        const passedPointIdSet = new Set(passedPointIds)

        const startEventIndex = this.selectedStartEventId
          ? this.events.findIndex(event => event.eventId === this.selectedStartEventId)
          : 0

        const safeStartIndex = startEventIndex >= 0 ? startEventIndex : 0

        this.passedEventIds = this.events
          .filter((event, index) => {
            if (index < safeStartIndex) return true
            const pointId = this.eventsToPointsMap.get(event.eventId)?.id || `event-${event.eventId}`
            return passedPointIdSet.has(pointId)
          })
          .map(event => event.eventId)
      },

      handleTrackCycleComplete(payload) {
        if (!this.startFromPointId) return

        this.startFromPointId = null
        this.selectedStartEventId = null
        this.passedEventIds = []
      },

      // Toggle hover tooltips (Loại 2) - Bây giờ là persistent name tooltips
      toggleHoverTooltips() {
        if (this.showHoverTooltips) {
          this.hidePersistentNameTooltips()
        } else {
          // Tắt simplified tooltips nếu đang hiển thị
          if (this.showSimplifiedTooltips) {
            this.hideSimplifiedTooltips()
          }
          this.showAllPersistentNameTooltips()
        }
      },

      // Toggle simplified tooltips (Loại 1)
      toggleSimplifiedTooltips() {
        if (this.showSimplifiedTooltips) {
          this.hideSimplifiedTooltips()
        } else {
          // Tắt persistent name tooltips nếu đang hiển thị
          if (this.showHoverTooltips) {
            this.hidePersistentNameTooltips()
          }
          this.showAllSimplifiedTooltips()
        }
      },

      // Show persistent name tooltips for all cameras
      showAllPersistentNameTooltips() {
        if (this.$refs.trackViewer) {
          this.$refs.trackViewer.showAllPersistentNameTooltips()
          this.showHoverTooltips = true
          console.log('Persistent name tooltips enabled, simplified tooltips disabled')
        }
      },

      // Hide persistent name tooltips
      hidePersistentNameTooltips() {
        if (this.$refs.trackViewer) {
          this.$refs.trackViewer.hidePersistentNameTooltips()
          this.showHoverTooltips = false
          console.log('Persistent name tooltips disabled')
        }
      },

      // Show simplified tooltips for all cameras
      showAllSimplifiedTooltips() {
        if (this.$refs.trackViewer) {
          this.$refs.trackViewer.showAllCameraTooltips()
          this.showSimplifiedTooltips = true
          console.log('Simplified tooltips enabled, persistent name tooltips disabled')
        }
      },

      // Hide simplified tooltips
      hideSimplifiedTooltips() {
        if (this.$refs.trackViewer) {
          this.$refs.trackViewer.hideAllTooltips()
          this.showSimplifiedTooltips = false
          console.log('Simplified tooltips disabled')
        }
      },

      // Handle TrackViewer events
      handleTrackPointClick(point, index) {
        console.log('TrackViewer point clicked:', point, index)
        
        // Focus map on the clicked point
        if (this.map) {
          this.map.setView([point.lat, point.lng], 17, { animate: true })
        }
      },

      handleTrackPointHover(point, index) {
        console.log('TrackViewer point hovered:', point, index)
        // Không cần làm gì vì tooltips bây giờ đều là persistent
      },
    },

    async created() {
      if (this.propCoordinate) {
        this.center = [this.propCoordinate.lat, this.propCoordinate.lng]
        this.zoom = this.propCoordinate.zoom
        this.rootMapCoordinate = { ...this.propCoordinate }
      }

      this.searchForm.dateFrom = this.dateFrom && this.dateFrom !== 'null' ? this.dateFrom : null
      this.searchForm.dateTo = this.dateTo && this.dateTo !== 'null' ? this.dateTo : null
      this.searchForm.type = parseInt(this.type) || 1

      console.log('SearchForm initialized:', this.searchForm)

      this.companyId = JSON.parse(localStorage.getItem('userData'))?.companyId
      await this.loadCompany()
      await this.loadDevices()
      await this.loadEvents()
      
      // Debug: Check if mapPoints has data
      console.log("MapPoints for TrackViewer:", this.mapPoints)
      console.log('Total mapPoints:', this.mapPoints.length)
      
      // Remove manual map fitting - TrackViewer will handle it automatically
      // Map will auto-fit when TrackViewer emits fit-bounds event

      console.log('Component created with events:', this.events.length)
    },
  }
</script>

<style lang="scss" scoped>
  /*----------------------------------------------
   * MAP CONTROLS & GENERAL MAP SETTINGS
   *----------------------------------------------*/
  /* Ẩn điều khiển mặc định của Leaflet */
  ::v-deep .leaflet-control-zoom.leaflet-bar.leaflet-control {
    display: none;
  }

  /* Style cho l-control có z-index cao */
  ::v-deep .leaflet-control {
    z-index: 1000 !important;
  }

  /* Control box chứa các nút điều khiển */
  .custom-control-box {
    background: #fff;
    border-radius: 10px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    padding: 5px 0px 5px 0px;
    z-index: 1000 !important;
  }

  /* Nút điều khiển hình tròn */
  .btn-circle {
    width: 32px;
    height: 32px;
    border-radius: 30% !important;
    border: none !important;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0;
    transition:
      background-color 0.2s,
      box-shadow 0.2s;
    color: #808080;
    margin-bottom: 8px !important;

    &:last-child {
      margin-bottom: 0 !important;
    }

    &:hover,
    &:focus {
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      border-color: #a9a9a9;
    }

    &.active {
      background-color: #17a2b8 !important;
      color: white !important;
      border-color: #17a2b8 !important;
    }
  }

  /* Icon trong nút - Cập nhật để icon thay đổi màu khi active */
  .custom-btn .feather-icon {
    stroke: #808080;
    margin-bottom: 3px !important;
  }

  .custom-btn.active .feather-icon {
    stroke: white !important;
  }

  /* Màu khác nhau cho từng loại button */
  .btn-circle.custom-btn {
    &.active {
      color: white !important;
      
      .feather-icon {
        stroke: white !important;
      }
    }

    &[variant="outline-success"].active {
      background-color: #28a745 !important;
      border-color: #28a745 !important;
    }

    &[variant="outline-info"].active {
      background-color: #17a2b8 !important;
      border-color: #17a2b8 !important;
    }
  }
</style>
