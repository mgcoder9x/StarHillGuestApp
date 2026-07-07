<template>
  <div>
    <!-- Main route polyline -->
    <l-polyline 
      v-if="sortedPoints.length > 1 && showRoute"
      :lat-lngs="polylineCoords"
      :color="routeColor"
      :weight="4"
      :opacity="0.8"
      :dash-array="routeDashArray"
      class="main-route"
    />

    <!-- Individual segments for highlighting -->
    <l-polyline
      v-for="(segment, index) in routeSegments"
      :key="`segment-${index}`"
      :lat-lngs="segment.coords"
      :color="getSegmentColor(segment)"
      :weight="getSegmentWeight(segment)"
      :opacity="getSegmentOpacity(segment)"
      :dash-array="getSegmentDashArray(segment)"
      @mouseover="$emit('segment-hover', segment)"
      @mouseout="$emit('segment-leave', segment)"
      :class="{ 'highlighted-route-segment': segment.highlighted }"
      class="route-segment"
    />

    <!-- Passed route progress (red line from start to current car position) -->
    <l-polyline
      v-if="passedPolylineCoords.length > 1"
      :lat-lngs="passedPolylineCoords"
      color="#ff1e1e"
      :weight="6"
      :opacity="0.95"
      class="passed-progress-route"
    />

    <!-- Moving circles for highlighted route -->
    <l-marker
      v-for="(circle, index) in movingCircles"
      :key="`moving-circle-${index}`"
      :lat-lng="circle.position"
      :icon="circle.icon"
      :z-index-offset="1500"
      class="moving-circle-marker"
    />

    <!-- Speed indicator hidden: use single moving car style -->

    <!-- Circle Markers at event centers -->
    <l-circle-marker 
      v-for="(point, index) in sortedPoints" 
      :key="'center-' + (point.id || index)"
      :lat-lng="[point.lat, point.lng]"
      :radius="getCircleRadius(point, index)"
      :color="getCircleColor(point, index)"
      :weight="getCircleWeight(point, index)"
      :fill="true"
      :fillColor="getCircleFillColor(point, index)"
      :fillOpacity="getCircleFillOpacity(point, index)"
      :z-index-offset="1000"
      @click="$emit('point-click', point, index)"
      @mouseover="$emit('point-hover', point, index)"
      @mouseout="$emit('point-leave', point, index)">
    </l-circle-marker>

    <!-- Icon markers đè lên circle markers -->
    <l-marker
      v-for="(point, index) in sortedPoints"
      :key="'icon-' + (point.id || index)"
      :lat-lng="[point.lat, point.lng]"
      :icon="getMarkerIcon(point, index)"
      :z-index-offset="2000"
      @click="handleMarkerClick(point, index)"
      @mouseover="handleMarkerHover(point, index)"
      @mouseout="handleMarkerLeave(point, index)">
    </l-marker>

    <!-- CameraTooltip Component -->
    <CameraTooltip
      ref="cameraTooltip"
      :map="map"
      :events="allEvents"
    />
  </div>
</template>

<script>
import L from 'leaflet'
import { LPolyline, LMarker, LTooltip, LCircleMarker } from 'vue2-leaflet'
import CameraTooltip from './CameraTooltip.vue'

export default {
  name: 'TrackViewer',
  components: { LPolyline, LMarker, LTooltip, LCircleMarker, CameraTooltip },
  
  props: {
    // Main data
    points: { type: Array, default: () => [] },
    
    // Display options
    showTooltips: { type: Boolean, default: false },
    showSequenceNumbers: { type: Boolean, default: false },
    showRoute: { type: Boolean, default: true },
    maxPoints: { type: Number, default: 1000 },
    
    // Route styling
    routeColor: { type: String, default: '#007bff' },
    routeWeight: { type: Number, default: 3 },
    routeOpacity: { type: Number, default: 0.7 },
    routeDashArray: { type: String, default: null },
    
    // Highlighting
    highlightColor: { type: String, default: '#ff5722' },
    highlightWeight: { type: Number, default: 5 },
    highlightDashArray: { type: String, default: '10, 5' },
    highlightedPointId: { type: [String, Number], default: null },
    highlightedRouteSegment: { type: Object, default: null }, // { fromPointId, toPointId }
    startFromPointId: { type: [String, Number], default: null },
    
    // Segments
    segmentOpacity: { type: Number, default: 0.5 },
    
    // Circle marker styling  
    markerSize: { type: Number, default: 10 }, // Radius = 10 để có đường kính 20 (bằng icon)
    movingCarIconUrl: { type: String, default: '/map/car-move.png' },
    movingCarSize: { type: Number, default: 34 },
    
    // Tooltip options
    tooltipOptions: {
      type: Object,
      default: () => ({
        permanent: false,
        direction: 'top',
        offset: [0, -25],
        className: 'track-tooltip'
      })
    },
    
    // Auto-fit options
    autoFitBounds: { type: Boolean, default: true },
    minZoom: { type: Number, default: 10 },
    maxZoom: { type: Number, default: 16 },
    boundsOptions: {
      type: Object,
      default: () => ({
        padding: [20, 20],
        maxZoom: 16
      })
    },

    // Events data for tooltips
    allEvents: { type: Array, default: () => [] },
    map: { type: Object, default: null },
    isImageMap: { type: Boolean, default: false }
  },

  data() {
    return {
      lastFitInfo: null,
      // Icon definitions
      icons: {
        camCar: L.icon({ 
          iconUrl: '/map/car-on.png', 
          iconSize: [20, 20], 
          iconAnchor: [10, 10], // Center của icon
          tooltipAnchor: [0, -10] 
        }),
        camPerson: L.icon({ 
          iconUrl: '/map/person-on.png', 
          iconSize: [20, 20], 
          iconAnchor: [10, 10], // Center của icon
          tooltipAnchor: [0, -10] 
        }),
        camWarning: L.icon({ 
          iconUrl: '/map/warning.png', 
          iconSize: [20, 20], 
          iconAnchor: [10, 10], // Center của icon
          tooltipAnchor: [0, -10] 
        })
      },

      // Animation state
      animationTimer: null,
      animationProgress: 0,
      speedAnimationTimer: null,
      speedProgress: 0,
      lastNormalizedProgress: null
    }
  },

  computed: {
    // Sort points by timestamp
    sortedPoints() {
      if (!this.points.length) return []
      
      return [...this.points]
        .filter(p => p.lat != null && p.lng != null)
        .sort((a, b) => {
          const timeA = new Date(a.timestamp || a.accessTime || 0)
          const timeB = new Date(b.timestamp || b.accessTime || 0)
          return timeA - timeB
        })
        .slice(0, this.maxPoints)
    },

    // Polyline coordinates
    polylineCoords() {
      return this.sortedPoints.map(p => [p.lat, p.lng])
    },

    // Route segments for individual highlighting
    routeSegments() {
      const segments = []
      
      for (let i = 1; i < this.sortedPoints.length; i++) {
        const prev = this.sortedPoints[i - 1]
        const curr = this.sortedPoints[i]
        
        // Check if this segment should be highlighted
        let isHighlighted = false
        if (this.highlightedRouteSegment) {
          const { fromPointId, toPointId } = this.highlightedRouteSegment
          // Chỉ highlight segment từ fromPoint → toPoint (hướng cụ thể)
          isHighlighted = (prev.id === fromPointId && curr.id === toPointId)
        }
        
        segments.push({
          id: curr.eventId || curr.id || i,
          coords: [[prev.lat, prev.lng], [curr.lat, curr.lng]],
          highlighted: isHighlighted,
          fromPoint: prev,
          toPoint: curr,
          index: i
        })
      }
      
      return segments
    },
    
    // Calculate center point of all markers
    centerPoint() {
      if (!this.sortedPoints.length) return null
      
      const lats = this.sortedPoints.map(p => p.lat)
      const lngs = this.sortedPoints.map(p => p.lng)
      
      const centerLat = lats.reduce((a, b) => a + b, 0) / lats.length
      const centerLng = lngs.reduce((a, b) => a + b, 0) / lngs.length
      
      return { lat: centerLat, lng: centerLng }
    },

    // Calculate optimal bounds for all markers
    markersBounds() {
      if (!this.sortedPoints.length) return null
      
      const lats = this.sortedPoints.map(p => p.lat)
      const lngs = this.sortedPoints.map(p => p.lng)
      
      const bounds = {
        north: Math.max(...lats),
        south: Math.min(...lats),
        east: Math.max(...lngs),
        west: Math.min(...lngs)
      }
      
      // Add some padding to bounds (10% or minimum 0.01 degrees)
      const latPadding = Math.max((bounds.north - bounds.south) * 0.1, 0.01)
      const lngPadding = Math.max((bounds.east - bounds.west) * 0.1, 0.01)
      
      return {
        north: bounds.north + latPadding,
        south: bounds.south - latPadding,
        east: bounds.east + lngPadding,
        west: bounds.west - lngPadding
      }
    },

    // Moving circles instead of arrows
    movingCircles() {
      const circles = []

      const state = this.activeMovementState
      if (!state) return circles

      const movement = this.getPositionAndBearingOnPath(state.activePath, state.animatedProgress)

      if (!movement) return circles

      circles.push({
        position: movement.position,
        icon: this.createMovingCircleIcon(0, state.animatedProgress, movement.bearing),
        segmentId: state.highlighted ? state.highlighted.id : 'full-route'
      })
      
      return circles
    },

    activeMovementState() {
      const highlighted = this.routeSegments.find(segment => segment.highlighted)
      const startPointIndex = this.getStartPointIndex()
      const activePath = highlighted
        ? highlighted.coords
        : this.polylineCoords.slice(startPointIndex)

      if (!activePath || activePath.length < 2) return null

      const fullRouteDistance = this.getPathTotalDistance(this.polylineCoords)
      const activePathDistance = this.getPathTotalDistance(activePath)

      const speedRatio = activePathDistance > 0 && fullRouteDistance > 0
        ? fullRouteDistance / activePathDistance
        : 1

      const normalizedProgress = (this.animationProgress * speedRatio) % 1
      const animatedProgress = normalizedProgress

      return {
        highlighted,
        startPointIndex,
        activePath,
        fullRouteDistance,
        activePathDistance,
        speedRatio,
        normalizedProgress,
        animatedProgress
      }
    },

    passedPolylineCoords() {
      const state = this.activeMovementState
      if (!state) return []

      const progressPath = this.getPathUntilProgress(state.activePath, state.animatedProgress)
      if (!progressPath.length) return []

      if (!state.startPointIndex || state.startPointIndex <= 0) {
        return progressPath
      }

      const prefixPath = this.polylineCoords.slice(0, state.startPointIndex + 1)
      if (!prefixPath.length) return progressPath

      return [...prefixPath, ...progressPath.slice(1)]
    },

    // Speed indicator disabled for single moving-car style
    speedIndicator() {
      return null
    }
  },

  methods: {
    /* ========== CIRCLE MARKER METHODS ========== */

    getStartPointIndex() {
      if (!this.startFromPointId) return 0

      const idx = this.sortedPoints.findIndex(point => point.id === this.startFromPointId)
      if (idx < 0) return 0

      const maxStart = Math.max(0, this.sortedPoints.length - 2)
      return Math.min(idx, maxStart)
    },

    getSegmentColor(segment) {
      if (segment.highlighted) return this.highlightColor
      return this.routeColor
    },

    getSegmentWeight(segment) {
      return segment.highlighted ? 8 : 3
    },

    getSegmentOpacity(segment) {
      return segment.highlighted ? 1 : 0.6
    },

    getSegmentDashArray(segment) {
      return segment.highlighted ? '15, 10' : null
    },

    isPointPassed(point, index) {
      const state = this.activeMovementState
      if (!state || this.sortedPoints.length < 2) return false

      if (state.highlighted && this.highlightedRouteSegment) {
        const { fromPointId, toPointId } = this.highlightedRouteSegment
        if (point.id === fromPointId) return state.normalizedProgress >= 0.01
        if (point.id === toPointId) return state.normalizedProgress >= 0.98
        return false
      }

      if (index < state.startPointIndex) return true
      if (index === state.startPointIndex) return true

      const cumulativeDistances = this.getPathCumulativeDistances(state.activePath)
      const relativeIndex = index - state.startPointIndex
      if (!cumulativeDistances.length || relativeIndex >= cumulativeDistances.length) return false

      const traveledDistance = state.activePathDistance * state.animatedProgress
      const pointDistance = cumulativeDistances[relativeIndex]
      const epsilon = Math.max(state.activePathDistance * 0.001, 0.00001)

      return pointDistance <= traveledDistance + epsilon
    },

    emitProgressUpdate() {
      const state = this.activeMovementState
      if (!state) {
        this.$emit('progress-update', {
          passedPointIds: [],
          currentPointId: null,
          startPointId: null,
          progress: 0,
        })
        return
      }

      const cumulativeDistances = this.getPathCumulativeDistances(state.activePath)
      const traveledDistance = state.activePathDistance * state.animatedProgress
      const epsilon = Math.max(state.activePathDistance * 0.001, 0.00001)

      const passedPointIndices = []
      for (let i = 0; i < cumulativeDistances.length; i++) {
        if (cumulativeDistances[i] <= traveledDistance + epsilon) {
          passedPointIndices.push(state.startPointIndex + i)
        }
      }

      const safePassedIndices = passedPointIndices
        .filter(idx => idx >= 0 && idx < this.sortedPoints.length)

      const passedPointIds = safePassedIndices.map(idx => this.sortedPoints[idx].id)
      const currentPointId = safePassedIndices.length
        ? this.sortedPoints[safePassedIndices[safePassedIndices.length - 1]].id
        : this.sortedPoints[state.startPointIndex]?.id || null

      this.$emit('progress-update', {
        passedPointIds,
        currentPointId,
        startPointId: this.sortedPoints[state.startPointIndex]?.id || null,
        progress: state.animatedProgress,
      })
    },

    checkCycleCompletion() {
      const state = this.activeMovementState
      const currentProgress = state?.normalizedProgress

      if (currentProgress === null || currentProgress === undefined) {
        this.lastNormalizedProgress = null
        return
      }

      if (
        this.lastNormalizedProgress !== null
        && currentProgress < this.lastNormalizedProgress
      ) {
        this.$emit('cycle-complete', {
          startFromPointId: this.startFromPointId,
        })
      }

      this.lastNormalizedProgress = currentProgress
    },
    
    getCircleRadius(point, index) {
      const baseRadius = 10
      const isHighlighted = this.highlightedPointId === (point.eventId || point.id)
      
      // Cũng highlight camera đích (toPoint) nếu có route segment
      const isDestination = this.highlightedRouteSegment && 
                           point.id === this.highlightedRouteSegment.toPointId
      
      if (isHighlighted || isDestination) return baseRadius * 1.4
      return baseRadius
    },

    getCircleColor(point, index) {
      const isHighlighted = this.highlightedPointId === (point.eventId || point.id)
      const isDestination = this.highlightedRouteSegment && 
                           point.id === this.highlightedRouteSegment.toPointId
      
      if (isHighlighted) return this.highlightColor
      if (isDestination) return '#f048c9' // Đổi thành màu xanh dương như trong CSS
      
      const eventTypeId = point.eventTypeId || 300
      switch (eventTypeId) {
        case 200: return '#28a745'
        case 300: return '#007bff'
        default: return '#ffc107'
      }
    },

    getCircleWeight(point, index) {
      const isHighlighted = this.highlightedPointId === (point.eventId || point.id)
      const isDestination = this.highlightedRouteSegment && 
                           point.id === this.highlightedRouteSegment.toPointId
      
      if (isHighlighted || isDestination) return 3
      return 1
    },

    getCircleFillColor(point, index) {
      return 'transparent'
    },

    getCircleFillOpacity(point, index) {
      return 0
    },

    /* ========== ICON MARKER METHODS ========== */
    
    getMarkerIcon(point, index) {
      const eventTypeId = point.eventTypeId || 300
      const isHighlighted = this.highlightedPointId === (point.eventId || point.id)
      const isDestination = this.highlightedRouteSegment && 
                           point.id === this.highlightedRouteSegment.toPointId
      const isPassed = this.isPointPassed(point, index)
      
      // Calculate rotation for static marker
      let rotation = 0
      if (index < this.sortedPoints.length - 1) {
        const next = this.sortedPoints[index + 1]
        rotation = this.calculateBearing(point.lat, point.lng, next.lat, next.lng)
      } else if (index > 0) {
        // For the last point, use the same rotation as the previous segment
        const prev = this.sortedPoints[index - 1]
        rotation = this.calculateBearing(prev.lat, prev.lng, point.lat, point.lng)
      }

      // Icon size logic
      let iconUrl, iconSize, className = ''
      switch (eventTypeId) {
        case 200: 
          iconUrl = isPassed ? '/map/person-warning.png' : '/map/person-on.png'
          break
        case 300: 
          iconUrl = isPassed ? '/map/car-warning.png' : '/map/car-on.png'
          break
        default: 
          iconUrl = '/map/warning.png'
      }
      
      if (isHighlighted) {
        iconSize = [28, 28]
        className = 'highlighted-marker'
      } else if (isDestination) {
        iconSize = [26, 26]
        className = 'destination-marker'
      } else if (isPassed) {
        iconSize = [24, 24]
        className = 'passed-marker'
      } else {
        iconSize = [20, 20]
      }
      
      // Return a DivIcon with rotation
      return L.divIcon({
        html: `
          <div class="static-marker-container ${className}" style="width:${iconSize[0]}px; height:${iconSize[1]}px;">
            <img src="${iconUrl}" style="width:100%; height:100%; transform: rotate(${rotation}deg);" />
          </div>
        `,
        className: 'custom-static-marker',
        iconSize,
        iconAnchor: [iconSize[0] / 2, iconSize[1] / 2],
        tooltipAnchor: [0, -10]
      })
    },

    /* ========== EVENT HANDLERS ========== */
    
    handleMarkerClick(point, index) {
      this.$emit('point-click', point, index)
    },

    handleMarkerHover(point, index) {
      this.$emit('point-hover', point, index)
      
      // Luôn hiển thị hover tooltip khi hover (độc lập với persistent tooltips)
      if (this.$refs.cameraTooltip) {
        const cameraData = {
          name: point.deviceName || `Camera ${point.deviceId}`,
          latitude: point.lat,
          longitude: point.lng,
          deviceId: point.deviceId
        }
        
        this.$refs.cameraTooltip.showHoverTooltip(cameraData, [])
      }
    },

    handleMarkerLeave(point, index) {
      this.$emit('point-leave', point, index)
      
      // Ẩn hover tooltip khi rời khỏi marker
      if (this.$refs.cameraTooltip) {
        this.$refs.cameraTooltip.hideTooltip()
      }
    },

    /* ========== TOOLTIP HELPERS ========== */
    
    getRelatedEventsForCamera(point) {
      if (!this.allEvents || !point.deviceId) return []
      
      // Filter events by deviceId and sort by time
      return this.allEvents
        .filter(event => event.deviceId === point.deviceId)
        .sort((a, b) => new Date(a.accessTime) - new Date(b.accessTime))
    },

    /* ========== DIRECTION & ANIMATION METHODS ========== */
    
    // Tính bearing chính xác giữa 2 điểm (sửa lại công thức)
    calculateBearing(lat1, lng1, lat2, lng2) {
      if (this.isImageMap) {
        // Calculate bearing for image maps (Cartesian coordinates)
        // In CRS.Simple, Y is lat and X is lng.
        // atan2(dx, dy) gives angle from Y-axis clockwise.
        const dy = lat2 - lat1
        const dx = lng2 - lng1
        let angle = Math.atan2(dx, dy) * 180 / Math.PI
        return (angle + 360) % 360
      }

      const toRad = deg => deg * Math.PI / 180
      const toDeg = rad => rad * 180 / Math.PI
      
      const dLng = toRad(lng2 - lng1)
      const lat1Rad = toRad(lat1)
      const lat2Rad = toRad(lat2)
      
      const y = Math.sin(dLng) * Math.cos(lat2Rad)
      const x = Math.cos(lat1Rad) * Math.sin(lat2Rad) - 
                Math.sin(lat1Rad) * Math.cos(lat2Rad) * Math.cos(dLng)
      
      let bearing = toDeg(Math.atan2(y, x))
      return (bearing + 360) % 360
    },

    // Interpolate position giữa 2 điểm
    interpolatePosition(start, end, progress) {
      const lat = start[0] + (end[0] - start[0]) * progress
      const lng = start[1] + (end[1] - start[1]) * progress
      return [lat, lng]
    },

    // Lấy vị trí + hướng trên toàn tuyến theo progress (0..1)
    getPositionAndBearingOnPath(pathCoords, progress) {
      if (!pathCoords || pathCoords.length < 2) return null

      const normalizedProgress = Math.max(0, Math.min(1, progress))
      const segments = []
      let totalDistance = 0

      for (let i = 1; i < pathCoords.length; i++) {
        const start = pathCoords[i - 1]
        const end = pathCoords[i]

        const segmentDistance = this.calculateDistance(
          { lat: start[0], lng: start[1] },
          { lat: end[0], lng: end[1] }
        )

        segments.push({ start, end, distance: segmentDistance })
        totalDistance += segmentDistance
      }

      if (totalDistance <= 0) {
        const start = pathCoords[0]
        const end = pathCoords[1]
        return {
          position: start,
          bearing: this.calculateBearing(start[0], start[1], end[0], end[1])
        }
      }

      const targetDistance = totalDistance * normalizedProgress
      let accumulatedDistance = 0

      for (const segment of segments) {
        const nextAccumulatedDistance = accumulatedDistance + segment.distance

        if (targetDistance <= nextAccumulatedDistance) {
          const distanceInSegment = targetDistance - accumulatedDistance
          const segmentProgress = segment.distance > 0
            ? distanceInSegment / segment.distance
            : 0

          return {
            position: this.interpolatePosition(segment.start, segment.end, segmentProgress),
            bearing: this.calculateBearing(
              segment.start[0],
              segment.start[1],
              segment.end[0],
              segment.end[1]
            )
          }
        }

        accumulatedDistance = nextAccumulatedDistance
      }

      const lastSegment = segments[segments.length - 1]
      return {
        position: lastSegment.end,
        bearing: this.calculateBearing(
          lastSegment.start[0],
          lastSegment.start[1],
          lastSegment.end[0],
          lastSegment.end[1]
        )
      }
    },

    // Tính tổng chiều dài tuyến (km)
    getPathTotalDistance(pathCoords) {
      if (!pathCoords || pathCoords.length < 2) return 0

      let totalDistance = 0

      for (let i = 1; i < pathCoords.length; i++) {
        const start = pathCoords[i - 1]
        const end = pathCoords[i]

        totalDistance += this.calculateDistance(
          { lat: start[0], lng: start[1] },
          { lat: end[0], lng: end[1] }
        )
      }

      return totalDistance
    },

    // Tính khoảng cách tích lũy cho từng điểm trên tuyến
    getPathCumulativeDistances(pathCoords) {
      if (!pathCoords || !pathCoords.length) return []

      const cumulativeDistances = [0]
      let accumulatedDistance = 0

      for (let i = 1; i < pathCoords.length; i++) {
        const start = pathCoords[i - 1]
        const end = pathCoords[i]
        accumulatedDistance += this.calculateDistance(
          { lat: start[0], lng: start[1] },
          { lat: end[0], lng: end[1] }
        )
        cumulativeDistances.push(accumulatedDistance)
      }

      return cumulativeDistances
    },

    // Lấy danh sách tọa độ từ đầu tuyến đến vị trí progress hiện tại
    getPathUntilProgress(pathCoords, progress) {
      if (!pathCoords || pathCoords.length < 2) return []

      const normalizedProgress = Math.max(0, Math.min(1, progress))
      const segments = []
      let totalDistance = 0

      for (let i = 1; i < pathCoords.length; i++) {
        const start = pathCoords[i - 1]
        const end = pathCoords[i]
        const distance = this.calculateDistance(
          { lat: start[0], lng: start[1] },
          { lat: end[0], lng: end[1] }
        )
        segments.push({ start, end, distance })
        totalDistance += distance
      }

      if (totalDistance <= 0) return [pathCoords[0], pathCoords[1]]

      const targetDistance = totalDistance * normalizedProgress
      let accumulatedDistance = 0
      const resultCoords = [pathCoords[0]]

      for (const segment of segments) {
        const segmentEndDistance = accumulatedDistance + segment.distance

        if (targetDistance >= segmentEndDistance) {
          resultCoords.push(segment.end)
          accumulatedDistance = segmentEndDistance
          continue
        }

        const distanceInSegment = targetDistance - accumulatedDistance
        const segmentProgress = segment.distance > 0
          ? distanceInSegment / segment.distance
          : 0

        resultCoords.push(this.interpolatePosition(segment.start, segment.end, segmentProgress))
        break
      }

      return resultCoords
    },

    // Tạo moving car icon với animation
    createMovingCircleIcon(index, progress, bearing = 0) {
      const baseSize = this.movingCarSize
      const sizeVariation = Math.sin(progress * Math.PI * 2) * 1.2
      const size = (baseSize + sizeVariation) * 1.5
      const opacity = 0.95
      const glowIntensity = 0.18 + Math.sin(progress * Math.PI) * 0.12
      
      return L.divIcon({
        html: `
          <div class="moving-car-container" 
               style="opacity: ${opacity};">
            <div class="moving-car-icon" 
                 style="width: ${size}px; height: ${size}px;">
              <img src="${this.movingCarIconUrl}" alt="moving-car" style="width: ${size}px; height: ${size}px; transform: rotate(${bearing}deg);" />
              <div class="car-glow" style="opacity: ${glowIntensity};"></div>
            </div>
          </div>
        `,
        className: 'leaflet-moving-car',
        iconSize: [size, size],
        iconAnchor: [size/2, size/2]
      })
    },

    // Tạo speed indicator icon
    createSpeedIndicatorIcon() {
      return L.divIcon({
        html: `
          <div class="speed-indicator-container">
            <div class="speed-indicator-icon">
              <svg width="20" height="20" viewBox="0 0 20 20">
                <circle cx="10" cy="10" r="8" fill="#42f563" stroke="white" stroke-width="2"/>
                <circle cx="10" cy="10" r="4" fill="white" opacity="0.9"/>
              </svg>
            </div>
          </div>
        `,
        className: 'leaflet-speed-indicator',
        iconSize: [20, 20],
        iconAnchor: [10, 10]
      })
    },

    // Start flow animation
    startFlowAnimation() {
      this.stopFlowAnimation()
      
      // Moving car animation (slower)
      this.animationTimer = setInterval(() => {
        this.animationProgress = (this.animationProgress + 0.0016) % 1
        this.emitProgressUpdate()
        this.checkCycleCompletion()
      }, 50)
      
      // Keep internal progress for compatibility (slower)
      this.speedAnimationTimer = setInterval(() => {
        this.speedProgress = (this.speedProgress + 0.002) % 1
      }, 60)

      this.emitProgressUpdate()
      this.checkCycleCompletion()
    },

    // Stop flow animation
    stopFlowAnimation() {
      if (this.animationTimer) {
        clearInterval(this.animationTimer)
        this.animationTimer = null
      }
      
      if (this.speedAnimationTimer) {
        clearInterval(this.speedAnimationTimer)
        this.speedAnimationTimer = null
      }
      
      this.animationProgress = 0
      this.speedProgress = 0
      this.lastNormalizedProgress = null

      this.emitProgressUpdate()
    },

    /* ========== PUBLIC METHODS ========== */
    
    // Show simplified tooltips for all cameras (Loại 1)
    showAllCameraTooltips() {
      console.log('showAllCameraTooltips called - Points:', this.sortedPoints.length)
      if (this.$refs.cameraTooltip && this.sortedPoints.length > 0) {
        const cameras = this.sortedPoints.map(point => ({
          name: point.deviceName || `Camera ${point.deviceId}`,
          latitude: point.lat,
          longitude: point.lng,
          deviceId: point.deviceId
        }))
        
        console.log('Calling showSimplifiedTooltipsForAllCameras with cameras:', cameras.length)
        this.$refs.cameraTooltip.showSimplifiedTooltipsForAllCameras(
          cameras,
          (camera) => this.getRelatedEventsForCamera(camera)
        )
      }
    },

    // Show persistent name tooltips for all cameras (Loại 2)
    showAllPersistentNameTooltips() {
      console.log('showAllPersistentNameTooltips called - Points:', this.sortedPoints.length)
      if (this.$refs.cameraTooltip && this.sortedPoints.length > 0) {
        const cameras = this.sortedPoints.map(point => ({
          name: point.deviceName || `Camera ${point.deviceId}`,
          latitude: point.lat,
          longitude: point.lng,
          deviceId: point.deviceId
        }))
        
        console.log('Calling showPersistentNameTooltipsForAllCameras with cameras:', cameras.length)
        this.$refs.cameraTooltip.showPersistentNameTooltipsForAllCameras(cameras)
      }
    },

    // Hide all simplified tooltips (Loại 1)
    hideAllTooltips() {
      console.log('hideAllTooltips called')
      if (this.$refs.cameraTooltip) {
        this.$refs.cameraTooltip.clearAllPersistentTooltips()
      }
    },

    // Hide persistent name tooltips (Loại 2)
    hidePersistentNameTooltips() {
      console.log('hidePersistentNameTooltips called')
      if (this.$refs.cameraTooltip) {
        this.$refs.cameraTooltip.clearPersistentNameTooltips()
      }
    },

    // Hide current hover tooltip (deprecated)
    hideCurrentTooltip() {
      console.log('hideCurrentTooltip called (deprecated)')
      // Không cần làm gì vì không còn hover tooltips
    },

    /* ========== UTILITIES ========== */
    
    calculateDistance(point1, point2) {
      if (this.isImageMap) {
        // Euclidean distance for image maps (L.CRS.Simple)
        return Math.sqrt(Math.pow(point2.lat - point1.lat, 2) + Math.pow(point2.lng - point1.lng, 2))
      }

      // Calculate distance between two points in kilometers (Haversine)
      const R = 6371 // Earth's radius in km
      const lat1 = point1.lat * Math.PI / 180
      const lat2 = point2.lat * Math.PI / 180
      const deltaLat = (point2.lat - point1.lat) * Math.PI / 180
      const deltaLng = (point2.lng - point1.lng) * Math.PI / 180

      const a = Math.sin(deltaLat/2) * Math.sin(deltaLat/2) +
              Math.cos(lat1) * Math.cos(lat2) *
              Math.sin(deltaLng/2) * Math.sin(deltaLng/2)
      const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a))

      return R * c
    },

    // Calculate optimal zoom level based on bounds
    calculateOptimalZoom(bounds) {
      if (!bounds) return 13
      
      const latDiff = bounds.north - bounds.south
      const lngDiff = bounds.east - bounds.west
      const maxDiff = Math.max(latDiff, lngDiff)
      
      let zoom = 13
      if (maxDiff > 1) zoom = 8        // Very wide area
      else if (maxDiff > 0.5) zoom = 10  // Wide area  
      else if (maxDiff > 0.1) zoom = 12  // Medium area
      else if (maxDiff > 0.05) zoom = 14 // Small area
      else zoom = 15                     // Very small area
      
      // Apply min/max zoom constraints
      zoom = Math.max(this.minZoom, Math.min(this.maxZoom, zoom))
      
      return zoom
    },

    // Get leaflet bounds object
    getLeafletBounds() {
      if (!this.sortedPoints.length) return null
      
      const lats = this.sortedPoints.map(p => p.lat)
      const lngs = this.sortedPoints.map(p => p.lng)
      
      return L.latLngBounds([
        [Math.min(...lats), Math.min(...lngs)],
        [Math.max(...lats), Math.max(...lngs)]
      ])
    },

    // Auto-fit map to show all markers
    fitMapToMarkers() {
      if (!this.sortedPoints.length) return null
      
      const bounds = this.getLeafletBounds()
      const center = this.centerPoint
      const optimalZoom = this.calculateOptimalZoom(this.markersBounds)
      
      const fitInfo = {
        bounds,
        center: [center.lat, center.lng],
        zoom: optimalZoom,
        boundsOptions: {
          ...this.boundsOptions,
          maxZoom: Math.min(this.maxZoom, optimalZoom)
        }
      }
      
      // Lưu fitInfo để tái sử dụng
      this.lastFitInfo = { ...fitInfo }
      
      // Emit event for parent to handle map fitting
      this.$emit('fit-bounds', fitInfo)
      
      return fitInfo
    },

    // Method để parent component có thể gọi lại fitMapToMarkers
    refitMap() {
      // Sử dụng fitInfo đã lưu nếu có, hoặc tính toán lại
      const fitInfo = this.lastFitInfo || this.fitMapToMarkers()
      
      if (fitInfo) {
        this.$emit('fit-bounds', fitInfo)
      }
      
      return fitInfo
    },

    // Enhanced getBounds method
    getBounds() {
      if (!this.sortedPoints.length) return null
      
      return this.getLeafletBounds()
    }
  },

  watch: {
    points: {
      handler() {
        this.$nextTick(() => {
          const fitInfo = this.autoFitBounds ? this.fitMapToMarkers() : this.lastFitInfo

          if (this.sortedPoints.length > 1) {
            this.startFlowAnimation()
          } else {
            this.stopFlowAnimation()
          }
          
          this.$emit('track-updated', {
            points: this.sortedPoints,
            bounds: this.getBounds(),
            center: this.centerPoint,
            fitInfo
          })
        })
      },
      deep: true,
      immediate: true
    },

    highlightedRouteSegment: {
      handler(newSegment) {
        if (newSegment || this.sortedPoints.length > 1) {
          this.startFlowAnimation()
        } else {
          this.stopFlowAnimation()
        }
      },
      immediate: true
    },

    startFromPointId() {
      this.animationProgress = 0
      this.lastNormalizedProgress = null
      if (this.sortedPoints.length > 1) {
        this.startFlowAnimation()
      } else {
        this.emitProgressUpdate()
      }
    }
  },

  beforeDestroy() {
    this.stopFlowAnimation()
  }
}
</script>

<style lang="scss" scoped>
/*----------------------------------------------
   * ROUTES & POLYLINES
   *----------------------------------------------*/
::v-deep .main-route {
  z-index: 100 !important;
}

::v-deep .route-segment {
  z-index: 200 !important;
  cursor: pointer;
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  
  &:hover {
    opacity: 1 !important;
    filter: drop-shadow(0 3px 6px rgba(0,0,0,0.2));
  }
  
  &.highlighted-route-segment {
    animation: dash-flow 2s linear infinite;
    filter: drop-shadow(0 6px 12px rgba(255, 87, 51, 0.4))
            drop-shadow(0 0 20px rgba(255, 87, 51, 0.2));
    box-shadow: 0 0 20px rgba(255, 87, 51, 0.5);
  }

  &.passed-route-segment {
    filter: drop-shadow(0 0 8px rgba(255, 30, 30, 0.45));
  }
}

@keyframes dash-flow {
  0% {
    stroke-dashoffset: 0;
  }
  100% {
    stroke-dashoffset: -25;
  }
}

::v-deep path[stroke="#ff5722"] {
  filter: drop-shadow(0 0 8px rgba(255, 87, 51, 0.6))
          drop-shadow(0 0 16px rgba(255, 87, 51, 0.3));
  animation: route-glow 3s ease-in-out infinite alternate;
}

@keyframes route-glow {
  0% {
    filter: drop-shadow(0 0 8px rgba(255, 87, 51, 0.6))
            drop-shadow(0 0 16px rgba(255, 87, 51, 0.3));
  }
  100% {
    filter: drop-shadow(0 0 12px rgba(255, 87, 51, 0.8))
            drop-shadow(0 0 24px rgba(255, 87, 51, 0.5))
            drop-shadow(0 0 36px rgba(255, 87, 51, 0.2));
  }
}

/*----------------------------------------------
   * CIRCLE MARKERS
   *----------------------------------------------*/
::v-deep .leaflet-interactive {
  transition: all 0.3s ease;
  cursor: pointer;
  z-index: 1000 !important;
  opacity: 0.3;
  
  &:hover {
    opacity: 0.6;
    transform: scale(1.05);
  }
}

/*----------------------------------------------
   * ICON MARKERS
   *----------------------------------------------*/
::v-deep .custom-static-marker {
  background: transparent !important;
  border: none !important;

  .static-marker-container {
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.3s ease;

    img {
      transition: transform 0.3s ease;
      filter: drop-shadow(0 2px 4px rgba(0,0,0,0.2));
    }
  }
}

::v-deep .leaflet-marker-icon {
  z-index: 2000 !important;
  pointer-events: auto;
  transition: all 0.3s ease;
  
  &:hover {
    transform: scale(1.1);
    filter: drop-shadow(0 2px 4px rgba(0,0,0,0.3));
  }
  
  &.highlighted-marker {
    filter: drop-shadow(0 4px 12px rgba(255, 87, 51, 0.6)) 
            drop-shadow(0 0 8px rgba(255, 87, 51, 0.4));
    transform: scale(1.2);
    animation: pulse-highlight 1.5s ease-in-out infinite;
  }
  
  &.destination-marker {
    filter: drop-shadow(0 3px 8px rgba(44, 134, 238, 0.6)) 
            drop-shadow(0 0 6px rgba(44, 134, 238, 0.4));
    transform: scale(1.15);
    animation: pulse-destination 2s ease-in-out infinite;
  }

  &.passed-marker {
    filter: drop-shadow(0 2px 8px rgba(255, 30, 30, 0.4));
    transform: scale(1.08);
  }
}

@keyframes pulse-highlight {
  0%, 100% {
    transform: scale(1.2);
    filter: drop-shadow(0 4px 12px rgba(255, 87, 51, 0.6)) 
            drop-shadow(0 0 8px rgba(255, 87, 51, 0.4));
  }
  50% {
    transform: scale(1.3);
    filter: drop-shadow(0 6px 16px rgba(255, 87, 51, 0.8)) 
            drop-shadow(0 0 12px rgba(255, 87, 51, 0.6));
  }
}

@keyframes pulse-destination {
  0%, 100% {
    transform: scale(1.15);
    filter: drop-shadow(0 3px 8px rgba(44, 134, 238, 0.6)) 
            drop-shadow(0 0 6px rgba(44, 134, 238, 0.4));
  }
  50% {
    transform: scale(1.25);
    filter: drop-shadow(0 5px 12px rgba(44, 134, 238, 0.8)) 
            drop-shadow(0 0 10px rgba(44, 134, 238, 0.6));
  }
}

/*----------------------------------------------
   * MOVING CIRCLES
   *----------------------------------------------*/
::v-deep .moving-circle-marker {
  pointer-events: none !important;
  z-index: 1500 !important;
  
  .leaflet-marker-icon {
    border: none !important;
    background: transparent !important;
    box-shadow: none !important;
  }
}

::v-deep .leaflet-moving-car {
  pointer-events: none;
  
  .moving-car-container {
    animation: car-float 2.4s ease-in-out infinite;
    transform-origin: center;
    
    .moving-car-icon {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
      transform-origin: center;
      animation: car-pulse 2s ease-in-out infinite;
      
      img {
        position: relative;
        z-index: 2;
        filter: drop-shadow(0 2px 8px rgba(0, 0, 0, 0.5));
      }

      .car-glow {
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        width: 130%;
        height: 130%;
        background: radial-gradient(circle, 
          rgba(22, 163, 74, 0.5) 0%, 
          rgba(34, 197, 94, 0.22) 45%, 
          rgba(74, 222, 128, 0.08) 75%,
          transparent 100%);
        border-radius: 50%;
        animation: car-glow 1.8s ease-in-out infinite;
      }
    }
  }
}

@keyframes car-float {
  0%, 100% {
    transform: translateY(0) scale(1);
  }
  50% {
    transform: translateY(-1px) scale(1.02);
  }
}

@keyframes car-pulse {
  0%, 100% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.04);
    opacity: 0.95;
  }
}

@keyframes car-glow {
  0%, 100% {
    transform: translate(-50%, -50%) scale(1);
    opacity: 0.45;
  }
  50% {
    transform: translate(-50%, -50%) scale(1.15);
    opacity: 0.2;
  }
}

/*----------------------------------------------
   * SPEED INDICATOR
   *----------------------------------------------*/
::v-deep .speed-indicator-marker {
  pointer-events: none !important;
  z-index: 1600 !important;
  
  .leaflet-marker-icon {
    border: none !important;
    background: transparent !important;
    box-shadow: none !important;
  }
}

::v-deep .leaflet-speed-indicator {
  pointer-events: none;
  z-index: 1600 !important;
  
  .speed-indicator-container {
    animation: speed-zoom 0.8s ease-in-out infinite;
    
    .speed-indicator-icon {
      filter: drop-shadow(0 4px 8px rgba(66, 245, 99, 0.5));
      
      svg {
        animation: speed-rotate 2s linear infinite;
        
        circle:first-child {
          fill: #42f563 !important;
        }
      }
    }
  }
}

@keyframes speed-zoom {
  0%, 100% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.3);
  }
}

@keyframes speed-rotate {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}

/*----------------------------------------------
   * PERFORMANCE OPTIMIZATIONS
   *----------------------------------------------*/
.leaflet-moving-circle,
.leaflet-speed-indicator {
  will-change: transform;
  backface-visibility: hidden;
  perspective: 1000px;
}
</style>


