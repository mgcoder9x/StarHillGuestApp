<template>
  <transition name="sidebar-slide" style="padding-left: 5px !important">
    <b-card
      v-if="show"
      class="shadow-lg position-relative sidebar-card"
      style="width: 25vw; height: 94%; z-index: 1000 !important; margin: 20px; border-radius: 20px !important; border: none; overflow: hidden;"
      @wheel.stop
    >
      <div class="d-flex align-items-center flex-shrink-0">
        <b-button
          variant="outline-secondary"
          size="sm"
          class="position-absolute btn-icon rounded-circle p-0 close-btn"
          style="top: 10px; right: 10px; width: 28px; height: 28px; z-index: 10"
          @click="$emit('close')"
        >
          <feather-icon icon="XIcon" size="14" />
        </b-button>
      </div>

      <b-card-body class="events-container hide-scrollbar sidebar-body" ref="scrollContainer">
        <div class="custom-timeline">
          <!-- Skeleton lần đầu -->
          <div v-if="isInitialLoading" class="skeleton-container">
            <div class="loading-progress-bar"></div>
            <div class="loading-message">
              <div class="loading-spinner"></div>
              <span>{{ $t('FaceEvent.Common.LoadingData') }}</span>
            </div>

            <div
              v-for="i in 5"
              :key="`skeleton-${i}`"
              class="timeline-step skeleton-step"
              :style="{ animationDelay: `${(i - 1) * 120}ms` }"
            >
              <div class="skeleton-circle"><div class="skeleton-pulse-ring"></div></div>

              <div class="step-content">
                <div class="step-header">
                  <div class="skeleton-line skeleton-wave" :style="{ width: `${40 + Math.random() * 30}%` }"></div>
                  <div class="skeleton-time">
                    <div class="skeleton-line skeleton-wave" style="width: 100%"></div>
                    <div class="skeleton-line skeleton-wave" style="width: 80%; margin-left: auto"></div>
                  </div>
                </div>

                <div class="step-body">
                  <div class="skeleton-image skeleton-shine"></div>
                  <div class="skeleton-license skeleton-shine"></div>
                </div>
              </div>

              <div v-if="i !== 5" class="connecting-line skeleton-connecting-line"></div>
            </div>
          </div>

          <!-- Timeline -->
          <div v-else>
            <div
              v-for="(event, index) in paginatedEvents"
              :key="event.eventId || index"
              class="timeline-step"
              :class="{ 'last-step': index === paginatedEvents.length - 1 }"
              @click="handleEventClick(event, index)"
            >
              <div
                class="step-number"
                :class="{
                  'active-step-number': isActiveStartEvent(event),
                  'passed-step-number': isPassedEvent(event),
                }"
              >
                <span>{{ index + 1 }}</span>
              </div>

              <div class="step-content">
                <div class="step-header">
                  <h6 class="device-name">{{ event.deviceName }}</h6>
                  <div class="time-display">
                    <div class="time-text">{{ formatTime(event.accessTime) }}</div>
                    <div class="date-text">{{ formatDate(event.accessTime) }}</div>
                  </div>
                </div>

                <div class="step-body">
                  <div class="step-image">
                    <img
                      :src="`${baseURL}${event.image}`"
                      alt="Event Image"
                      class="event-image cursor-pointer"
                      loading="lazy"
                      @click="$emit('show-modal-event', event.eventId)"
                    />
                  </div>
                  <div class="license-info">
                    <strong v-if="event.eventTypeId == 200">{{ event.fullName }}</strong>
                    <strong v-if="event.eventTypeId == 300">{{ event.licensePlates }}</strong>
                  </div>
                </div>
              </div>

              <div v-if="index !== paginatedEvents.length - 1" class="connecting-line"></div>
            </div>

            <!-- Load more -->
            <div v-if="hasMoreEvents" class="load-more-container">
              <b-button variant="outline-primary" size="sm" @click="loadMoreEvents" :disabled="isLoading">
                <span v-if="isLoading" class="spinner-border spinner-border-sm mr-1"></span>
                <span v-else class="chevron-down-icon">▼</span>
                <span>{{ isLoading ? $t('Common.Loading') : $t('Common.LoadMore') }}</span>
              </b-button>
            </div>

            <!-- Sentinel cho auto-load (không ảnh hưởng UI) -->
            <div v-if="hasMoreEvents" ref="sentinel" style="height: 1px;"></div>
          </div>
        </div>
      </b-card-body>
    </b-card>
  </transition>
</template>

<script>
export default {
  name: 'SidebarMap',
  props: {
    show: { type: Boolean, default: false },
    events: { type: Array, default: () => [] },
    baseURL: { type: String, default: '' },
    activeStartEventId: { type: [String, Number], default: null },
    passedEventIds: { type: Array, default: () => [] },
  },
  emits: ['close', 'focus-camera', 'start-route-from-event', 'show-modal-event'],
  data() {
    return {
      pageSize: 15,
      currentPage: 1,
      isLoading: false,

      // loading lần đầu
      firstShown: false,      // đã từng hiển thị timeline chưa
      firstLoadingMs: 800,    // giữ skeleton 800ms cho mượt

      // IO
      io: null,
      scrollThreshold: 100, // px tới đáy để trigger load khi fallback
    }
  },
  computed: {
    endIdx() {
      return this.pageSize * this.currentPage
    },
    paginatedEvents() {
      return this.events.slice(0, this.endIdx)
    },
    hasMoreEvents() {
      return this.events.length > this.endIdx
    },
    isInitialLoading() {
      // chỉ hiện skeleton đúng lần đầu có dữ liệu và sidebar đang mở
      return this.show && !this.firstShown && this.events.length === 0
        ? true
        : this.show && !this.firstShown && this.events.length > 0
    },
    passedEventIdSet() {
      return new Set(this.passedEventIds || [])
    },
  },
  watch: {
    show(val) {
      if (!val) return
      this.resetScrollTop()
      if (!this.firstShown && this.events.length > 0) {
        this.runFirstReveal()
      }
      this.$nextTick(this.setupAutoLoad) // CHỈNH: thiết lập auto-load
    },
    events(newList, oldList) {
      if (this.show && !this.firstShown && newList && newList.length > 0) {
        this.runFirstReveal()
      }
      this.$nextTick(this.setupAutoLoad) // CHỈNH
    },
    hasMoreEvents() {
      this.$nextTick(this.setupAutoLoad) // CHỈNH
    },
  },
  mounted() {
    if (this.show && !this.firstShown && this.events.length > 0) {
      this.runFirstReveal()
    }
    this.$nextTick(this.setupAutoLoad) // CHỈNH
  },
  beforeDestroy() {
    this.disconnectObserver()
    this.removeScrollListener() // CHỈNH: dọn scroll listener
  },
  methods: {
    /* ---------- Init / helpers ---------- */
    resetScrollTop() {
      const el = this.$refs.scrollContainer
      if (el) el.scrollTop = 0
    },
    runFirstReveal() {
      // Giữ nguyên UX: show skeleton ~800ms lần đầu có data
      const t = setTimeout(() => {
        this.firstShown = true
        clearTimeout(t)
      }, this.firstLoadingMs)
    },

    // THÊM: Thiết lập auto-load (IO + fallback scroll)
    setupAutoLoad() {
      if (!this.show || !this.$refs.scrollContainer) {
        this.disconnectObserver()
        this.removeScrollListener()
        return
      }
      // IO
      this.observeIfNeeded()
      // Fallback scroll
      this.addScrollListener()
    },

    /* ---------- IntersectionObserver cho auto-load ---------- */
    ensureObserver() {
      if (this.io || typeof IntersectionObserver === 'undefined') return
      this.io = new IntersectionObserver((entries) => {
        entries.forEach((e) => {
          if (e.isIntersecting && this.hasMoreEvents && !this.isLoading) {
            this.loadMoreEvents()
          }
        })
      }, { root: this.$refs.scrollContainer || null, rootMargin: '0px 0px 80px 0px', threshold: 0 })
    },
    observeIfNeeded() {
      if (!this.show || !this.hasMoreEvents) { this.disconnectObserver(); return }
      this.ensureObserver()
      const s = this.$refs.sentinel
      if (this.io && s) {
        this.io.disconnect()
        this.io.observe(s)
      }
    },
    disconnectObserver() {
      if (this.io) {
        this.io.disconnect()
        this.io = null
      }
    },

    // THÊM: Scroll fallback
    addScrollListener() {
      const c = this.$refs.scrollContainer
      if (!c) return
      this.removeScrollListener()
      c.addEventListener('scroll', this.handleScroll, { passive: true })
    },
    removeScrollListener() {
      const c = this.$refs.scrollContainer
      if (c) c.removeEventListener('scroll', this.handleScroll)
    },
    handleScroll() {
      if (!this.show || !this.hasMoreEvents || this.isLoading) return
      const c = this.$refs.scrollContainer
      if (!c) return
      const distanceFromBottom = c.scrollHeight - c.scrollTop - c.clientHeight
      if (distanceFromBottom <= this.scrollThreshold) {
        this.loadMoreEvents()
      }
    },

    /* ---------- Paging ---------- */
    async loadMoreEvents() {
      if (this.isLoading || !this.hasMoreEvents) return
      this.isLoading = true
      try {
        // Giữ nhịp UX như cũ: mô phỏng trễ nhẹ 300ms (đã có comment “remove in production”)
        await new Promise(r => setTimeout(r, 300))
        this.currentPage++
        this.$nextTick(this.setupAutoLoad) // CHỈNH: thiết lập lại sau khi thêm items
      } catch (err) {
        // eslint-disable-next-line no-console
        console.error('Error loading more events:', err)
      } finally {
        this.isLoading = false
      }
    },

    /* ---------- Formatting ---------- */
    formatTime(s) {
      if (!s) return ''
      const d = new Date(s)
      const p = n => n.toString().padStart(2, '0')
      return `${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`
    },
    formatDate(s) {
      if (!s) return ''
      const d = new Date(s)
      const p = n => n.toString().padStart(2, '0')
      return `${p(d.getDate())}/${p(d.getMonth() + 1)}/${d.getFullYear()}`
    },

    /* ---------- Events ---------- */
    handleEventClick(event, index) {
      this.$emit('focus-camera', event, index)
      this.$emit('start-route-from-event', event, index)
    },

    isActiveStartEvent(event) {
      return !!this.activeStartEventId && event.eventId === this.activeStartEventId
    },

    isPassedEvent(event) {
      return this.passedEventIdSet.has(event.eventId)
    },
  },
}
</script>

<style lang="scss" scoped>
/*----------------------------------------------
 * SIDEBAR & CONTAINER STYLES
 *----------------------------------------------*/
/* Card sidebar chính */
.sidebar-card {
  background: rgba(255, 255, 255, 0.95) !important;
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  box-shadow: 0 25px 50px rgba(0, 0, 0, 0.15) !important;
  border: 1px solid rgba(255, 255, 255, 0.2) !important;
  z-index: 1000 !important;
}

/* Nút đóng sidebar */
.close-btn {
  background: rgba(255, 255, 255, 0.9) !important;
  border: 1px solid rgba(0, 0, 0, 0.1) !important;
  backdrop-filter: blur(10px);
  -webkit-backdrop-filter: blur(10px);
  transition: all 0.3s ease;

  &:hover {
    background: rgba(255, 255, 255, 1) !important;
    transform: scale(1.1);
    box-shadow: 0 5px 15px rgba(0, 0, 0, 0.2);
  }
}

/* Container cho các sự kiện - scrollable */
.events-container {
  max-height: 75vh;
  overflow-y: auto !important;
  padding-right: 10px;
}

/* Nội dung chính của sidebar */
.sidebar-body {
  padding: 10px !important;
  padding-top: 0px !important;
  margin-top: 35px !important;
  height: calc(100% - 20px);
  overflow-y: auto !important;

  /* Thiết lập scrollbar */
  &::-webkit-scrollbar {
    width: 8px !important;
    display: block !important;
  }

  &::-webkit-scrollbar-track {
    background: rgba(0, 0, 0, 0.05) !important;
    border-radius: 4px;
  }

  &::-webkit-scrollbar-thumb {
    background: rgba(0, 0, 0, 0.2) !important;
    border-radius: 4px;
    border: 1px solid rgba(255, 255, 255, 0.4);

    &:hover {
      background: rgba(0, 0, 0, 0.3) !important;
    }
  }

  /* Firefox scrollbar */
  scrollbar-width: thin !important;
  scrollbar-color: rgba(0, 0, 0, 0.2) rgba(0, 0, 0, 0.05) !important;
}

/* Hiệu ứng transition cho sidebar */
.sidebar-slide-enter-active,
.sidebar-slide-leave-active {
  transition: transform 0.5s ease;
  z-index: 1000 !important;
}

.sidebar-slide-enter,
.sidebar-slide-leave-to {
  transform: translateX(-100%);
  z-index: 1000 !important;
}

.sidebar-slide-enter-to,
.sidebar-slide-leave {
  transform: translateX(0);
  z-index: 1000 !important;
}

/*----------------------------------------------
 * TIMELINE & EVENT ITEM STYLES
 *----------------------------------------------*/
/* Container timeline tổng thể */
.custom-timeline {
  position: relative;
  padding: 0px;
}

/* Mỗi bước trong timeline */
.timeline-step {
  position: relative;
  display: flex;
  align-items: flex-start;
  margin-bottom: 32px;
  padding: 16px 12px;
  border-radius: 16px;
  transition: all 0.3s ease;
  cursor: pointer;
  background: rgba(248, 250, 252, 0.4);
  border: 1px solid rgba(226, 232, 240, 0.3);

  &:hover {
    background-color: rgba(148, 163, 184, 0.12);
    transform: translateX(6px);
    box-shadow: 0 8px 25px rgba(148, 163, 184, 0.15);
    border-color: rgba(148, 163, 184, 0.25);

    /* Hiệu ứng đặc biệt cho số khi hover */
    .step-number {
      background: rgba(255, 87, 51, 0.1);
      border-color: rgba(255, 87, 51, 0.3);
    }
  }

  &.last-step {
    margin-bottom: 0;
  }
}

/* Số thứ tự trong timeline */
.step-number {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
  background: rgba(248, 250, 252, 0.7);
  border: 2px solid rgba(203, 213, 225, 0.6);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 16px;
  margin-top: -7px;
  align-self: flex-start;
  position: relative;
  z-index: 2;
  backdrop-filter: blur(10px);
  -webkit-backdrop-filter: blur(10px);
  box-shadow: 0 2px 4px rgba(100, 116, 139, 0.1);

  span {
    color: #475569;
    font-weight: 700;
    font-size: 12px;
    line-height: 1;
  }
}

.step-number.active-step-number {
  background: rgba(59, 130, 246, 0.15);
  border-color: rgba(59, 130, 246, 0.45);

  span {
    color: #1d4ed8;
  }
}

.step-number.passed-step-number {
  background: rgba(34, 197, 94, 0.18);
  border-color: rgba(34, 197, 94, 0.55);

  span {
    color: #15803d;
  }
}

/* Đường kết nối các bước */
.connecting-line {
  position: absolute;
  left: 27px;
  top: 48px;
  bottom: -24px;
  width: 2px;
  background: linear-gradient(to bottom, #e2e8f0, #f1f5f9);
  z-index: 1;
}

/* Phần nội dung của mỗi bước */
.step-content {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

/* Header của mỗi bước */
.step-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 8px;
}

/* Tên thiết bị */
.device-name {
  color: #64748b;
  font-size: 12px;
  font-weight: 600;
  margin: 0;
  line-height: 1.2;
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: calc(100% - 90px);
  letter-spacing: -0.25px;
}

/* Container cho thời gian */
.time-display {
  text-align: right;
  display: flex;
  flex-direction: column;
  gap: 3px;
  flex-shrink: 0;
  min-width: 85px;
  margin-top: 0;
  margin-left: 8px;
  padding-left: 8px;
  border-left: 1px solid rgba(203, 213, 225, 0.3);
}

/* Giờ */
.time-text {
  color: #334155;
  font-size: 12px;
  font-weight: 600;
  line-height: 1.2;
  letter-spacing: -0.25px;
}

/* Ngày */
.date-text {
  color: #94a3b8;
  font-size: 10px;
  font-weight: 500;
  line-height: 1.1;
  background: rgba(241, 245, 249, 0.5);
  border-radius: 4px;
  padding: 1px 4px;
  display: inline-block;
  margin-left: auto;
}

/* Phần nội dung chính của mỗi bước */
.step-body {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

/* Hình ảnh sự kiện */
.event-image {
  width: 80px;
  height: 60px;
  object-fit: cover;
  border-radius: 12px;
  border: 2px solid #e2e8f0;
  transition: all 0.3s ease;
  opacity: 0;
  animation: fadeIn 0.3s ease forwards;

  &:hover {
    border-color: #94a3b8;
    transform: scale(1.05);
    box-shadow: 0 8px 20px rgba(100, 116, 139, 0.2);
  }
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

/* Thông tin biển số */
.license-info {
  background: linear-gradient(
    135deg,
    rgba(148, 163, 184, 0.08) 0%,
    rgba(203, 213, 225, 0.12) 50%,
    rgba(226, 232, 240, 0.1) 100%
  );
  color: #64748b;
  font-size: 11px;
  font-weight: 600;
  line-height: 1.2;
  text-align: center;
  min-width: 75px;
  flex-shrink: 0;
  padding: 8px 10px;
  border-radius: 8px;
  box-shadow:
    0 1px 4px rgba(148, 163, 184, 0.12),
    0 1px 2px rgba(0, 0, 0, 0.04);
  transition: all 0.3s ease;
  backdrop-filter: blur(8px);
  -webkit-backdrop-filter: blur(8px);
  border: 1px solid rgba(203, 213, 225, 0.3);
  position: relative;
  overflow: hidden;

  /* Shine effect */
  &::before {
    content: '';
    position: absolute;
    top: -50%;
    left: -50%;
    width: 200%;
    height: 200%;
    background: linear-gradient(45deg, transparent, rgba(255, 255, 255, 0.15), transparent);
    transform: rotate(45deg);
    transition: all 0.6s ease;
    opacity: 0;
  }

  &:hover {
    background: linear-gradient(
      135deg,
      rgba(148, 163, 184, 0.15) 0%,
      rgba(203, 213, 225, 0.2) 50%,
      rgba(226, 232, 240, 0.18) 100%
    );
    transform: translateY(-1px) scale(1.01);
    box-shadow:
      0 4px 12px rgba(148, 163, 184, 0.18),
      0 2px 6px rgba(0, 0, 0, 0.08);
    border-color: rgba(148, 163, 184, 0.4);
    color: #1e293b;

    &::before {
      opacity: 1;
      transform: rotate(45deg) translate(50%, 50%);
    }
  }

  strong {
    display: block;
    color: inherit;
    position: relative;
    z-index: 1;
    text-shadow: 0 1px 1px rgba(255, 255, 255, 0.8);
  }
}

/* Load more button styling */
.load-more-container {
  display: flex;
  justify-content: center;
  padding: 20px 0;
  margin-top: 16px;
  border-top: 1px solid rgba(226, 232, 240, 0.5);
  
  .chevron-down-icon {
    font-size: 10px;
    margin-right: 8px;
    color: inherit;
  }
  
  .spinner-border {
    width: 1rem;
    height: 1rem;
  }
}

/* Enhanced Skeleton Loading Animation */
@keyframes shimmer {
  0% {
    background-position: -468px 0;
  }
  100% {
    background-position: 468px 0;
  }
}

@keyframes pulse {
  0% {
    opacity: 0.6;
    transform: scale(1);
  }
  50% {
    opacity: 0.4;
    transform: scale(0.98);
  }
  100% {
    opacity: 0.6;
    transform: scale(1);
  }
}

.skeleton-pulse {
  animation: pulse 1.5s infinite ease-in-out;
  position: relative;
  overflow: hidden;
  
  &::after {
    content: "";
    position: absolute;
    top: 0;
    right: 0;
    bottom: 0;
    left: 0;
    background: linear-gradient(90deg, 
      rgba(255, 255, 255, 0) 0%, 
      rgba(255, 255, 255, 0.2) 20%, 
      rgba(255, 255, 255, 0.5) 60%, 
      rgba(255, 255, 255, 0) 100%);
    animation: shimmer 2s infinite;
    transform: translateX(-100%);
  }
}

/* Skeleton UI Elements - Enhanced */
.skeleton-container {
  padding: 0;
  position: relative;
  
  &::before {
    content: "";
    position: absolute;
    top: -10px;
    left: 20px;
    height: calc(100% + 20px);
    width: 3px;
    background: linear-gradient(to bottom, #e2e8f0 0%, rgba(226, 232, 240, 0.5) 100%);
    z-index: 0;
  }
}

.skeleton-step {
  cursor: default;
  opacity: 0;
  animation: fadeIn 0.5s ease-out forwards;
  
  @for $i from 1 through 5 {
    &:nth-child(#{$i}) {
      animation-delay: #{$i * 100}ms;
    }
  }
  
  &:hover {
    transform: none;
    background: rgba(248, 250, 252, 0.4);
    box-shadow: none;
    border-color: rgba(226, 232, 240, 0.3);
  }
  
  &::after {
    content: '';
    position: absolute;
    left: 16px;
    top: 0;
    height: 100%;
    width: 4px;
    background: rgba(203, 213, 225, 0.1);
    z-index: 0;
  }
}

.skeleton-circle {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
  background: linear-gradient(145deg, rgba(203, 213, 225, 0.5), rgba(203, 213, 225, 0.2));
  border-radius: 50%;
  margin-right: 16px;
  margin-top: -7px;
  box-shadow: 0 2px 6px rgba(100, 116, 139, 0.1), inset 0 1px 1px rgba(255, 255, 255, 0.3);
  position: relative;
  z-index: 2;
  
  &::after {
    content: "";
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    border-radius: 50%;
    box-shadow: inset 0 -2px 5px rgba(0, 0, 0, 0.1);
  }
}

.skeleton-line {
  height: 12px;
  border-radius: 4px;
  margin-bottom: 8px;
  background: linear-gradient(90deg, rgba(203, 213, 225, 0.4), rgba(203, 213, 225, 0.2));
  box-shadow: 0 1px 2px rgba(100, 116, 139, 0.05);
}

.skeleton-image {
  width: 80px;
  height: 60px;
  border-radius: 12px;
  background: linear-gradient(145deg, rgba(203, 213, 225, 0.4), rgba(203, 213, 225, 0.2));
  box-shadow: 0 2px 8px rgba(100, 116, 139, 0.1), inset 0 1px 1px rgba(255, 255, 255, 0.2);
  position: relative;
  overflow: hidden;
  
  &::after {
    content: "";
    position: absolute;
    top: 0;
    right: 0;
    bottom: 0;
    left: 0;
    background: radial-gradient(circle, rgba(255, 255, 255, 0.1) 0%, transparent 70%);
  }
}

.skeleton-license {
  width: 75px;
  height: 35px;
  border-radius: 8px;
  background: linear-gradient(145deg, rgba(203, 213, 225, 0.3), rgba(203, 213, 225, 0.2));
  box-shadow: 0 1px 4px rgba(100, 116, 139, 0.05), inset 0 1px 1px rgba(255, 255, 255, 0.2);
}

/* Enhanced entrance animation with scale effect */
@keyframes fadeSlideIn {
  0% {
    opacity: 0;
    transform: translateY(15px);
  }
  70% {
    opacity: 1;
    transform: translateY(-2px);
  }
  100% {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Add animated loading indicator at the top */
.skeleton-container::after {
  content: '';
  position: absolute;
  top: -5px;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(90deg, 
    rgba(59, 130, 246, 0), 
    rgba(59, 130, 246, 0.6), 
    rgba(59, 130, 246, 0));
  background-size: 200% 100%;
  animation: loading-bar 1.5s infinite ease-in-out;
}

@keyframes loading-bar {
  0% {
    background-position: -100% 0;
  }
  100% {
    background-position: 200% 0;
  }
}

/* ---------------------------------------------- 
 * ENHANCED DYNAMIC LOADING ANIMATIONS
 * ---------------------------------------------- */

/* Progress bar at top of container */
.loading-progress-bar {
  position: absolute;
  top: 0;
  left: 0;
  height: 4px;
  width: 100%;
  background: linear-gradient(to right, 
    rgba(59, 130, 246, 0.1),
    rgba(59, 130, 246, 0.1));
  overflow: hidden;
  z-index: 10;
  
  &::before {
    content: '';
    position: absolute;
    height: 100%;
    width: 50%;
    background: linear-gradient(to right,
      rgba(59, 130, 246, 0),
      #3b82f6,
      rgba(59, 130, 246, 0));
    left: -50%;
    animation: progress-bar-slide 1.5s ease-in-out infinite;
  }
}

@keyframes progress-bar-slide {
  0% { left: -50%; }
  100% { left: 100%; }
}

/* Loading message with spinner */
.loading-message {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  margin: 10px 0 20px;
  padding: 8px 16px;
  background: rgba(59, 130, 246, 0.08);
  border-radius: 8px;
  color: #3b82f6;
  font-weight: 500;
  
  .loading-spinner {
    width: 18px;
    height: 18px;
    border: 2px solid rgba(59, 130, 246, 0.3);
    border-top-color: #3b82f6;
    border-radius: 50%;
    animation: spinner-rotate 1s linear infinite;
  }
}

@keyframes spinner-rotate {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

/* Skeleton container enhancements */
.skeleton-container {
  position: relative;
  overflow: visible;
  padding: 10px 0;
  
  /* Dynamic linear gradient line through the timeline */
  &::before {
    content: "";
    position: absolute;
    top: 0;
    left: 28px;
    height: 100%;
    width: 2px;
    background: linear-gradient(to bottom, 
      rgba(59, 130, 246, 0.2), 
      rgba(99, 102, 241, 0.3), 
      rgba(168, 85, 247, 0.2),
      rgba(236, 72, 153, 0.3));
    z-index: 0;
  }
}

/* Enhanced skeleton step animation */
.skeleton-step {
  animation: skeleton-appear 0.6s cubic-bezier(0.2, 0.8, 0.2, 1) forwards;
  opacity: 0;
  transform: translateY(15px) scale(0.98);
  will-change: transform, opacity;
  
  &:nth-child(even) {
    animation-name: skeleton-appear-alt;
  }
}

@keyframes skeleton-appear {
  0% {
    opacity: 0;
    transform: translateY(15px) scale(0.98);
  }
  70% {
    opacity: 1;
    transform: translateY(-5px) scale(1.01);
  }
  100% {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

@keyframes skeleton-appear-alt {
  0% {
    opacity: 0;
    transform: translateY(20px) scale(0.96);
  }
  70% {
    opacity: 1;
    transform: translateY(-3px) scale(1.01);
  }
  100% {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* Enhanced circle animation */
.skeleton-circle {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
  background: linear-gradient(145deg, rgba(59, 130, 246, 0.1), rgba(59, 130, 246, 0.3));
  border-radius: 50%;
  margin-right: 16px;
  margin-top: -7px;
  box-shadow: 0 0 0 4px rgba(59, 130, 246, 0.05);
  position: relative;
  z-index: 2;
}

.skeleton-pulse-ring {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 20px;
  height: 20px;
  background: rgba(59, 130, 246, 0.3);
  border-radius: 50%;
  
  &::before, &::after {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    border-radius: 50%;
    background: rgba(59, 130, 246, 0.3);
    animation: pulse-ring 2s cubic-bezier(0.215, 0.61, 0.355, 1) infinite;
  }
  
  &::after {
    animation-delay: 0.6s;
  }
}

@keyframes pulse-ring {
  0% {
    transform: scale(0.5);
    opacity: 0.8;
  }
  80%, 100% {
    opacity: 0;
    transform: scale(2);
  }
}

/* Enhanced skeleton connecting line */
.skeleton-connecting-line {
  position: absolute;
  left: 27px;
  top: 48px;
  bottom: -24px;
  width: 2px;
  background: linear-gradient(to bottom, rgba(59, 130, 246, 0.3), rgba(99, 102, 241, 0.2));
  z-index: 1;
  overflow: hidden;
  
  &::after {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 60%;
    background: linear-gradient(to bottom, 
      rgba(59, 130, 246, 0.8), 
      rgba(99, 102, 241, 0));
    animation: line-flow 2s ease-in-out infinite;
  }
}

@keyframes line-flow {
  0% { transform: translateY(-100%); }
  50% { transform: translateY(100%); }
  100% { transform: translateY(300%); }
}

/* Enhanced skeleton line effects */
.skeleton-line {
  height: 12px;
  border-radius: 4px;
  margin-bottom: 8px;
}

.skeleton-time {
  display: flex;
  flex-direction: column;
  min-width: 85px;
  gap: 5px;
}

/* Wave effect for text */
.skeleton-wave {
  background: linear-gradient(90deg, rgba(59, 130, 246, 0.1), rgba(99, 102, 241, 0.15), rgba(59, 130, 246, 0.1));
  background-size: 200% 100%;
  animation: wave-animation 2s infinite linear;
}

@keyframes wave-animation {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

/* Shine effect for images and cards */
.skeleton-shine {
  background: linear-gradient(145deg, rgba(59, 130, 246, 0.1), rgba(99, 102, 241, 0.15));
  position: relative;
  overflow: hidden;
  
  &::after {
    content: "";
    position: absolute;
    top: -50%;
    left: -50%;
    width: 200%;
    height: 200%;
    background: linear-gradient(
      90deg, 
      transparent, 
      rgba(255, 255, 255, 0.4), 
      transparent
    );
    transform: rotate(30deg);
    animation: shine 2s infinite;
  }
}

@keyframes shine {
  0% {
    transform: translateX(-100%) rotate(30deg);
  }
  100% {
    transform: translateX(100%) rotate(30deg);
  }
}

/* Enhanced image skeleton */
.skeleton-image {
  width: 80px;
  height: 60px;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05), inset 0 1px 2px rgba(255, 255, 255, 0.1);
}

/* Enhanced license plate skeleton */
.skeleton-license {
  width: 75px;
  height: 35px;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04), inset 0 1px 1px rgba(255, 255, 255, 0.1);
}

/* Add extra flair with a pulse effect on hover for skeleton steps */
.skeleton-step:hover {
  .skeleton-circle {
    box-shadow: 0 0 0 6px rgba(59, 130, 246, 0.1);
    transition: box-shadow 0.3s ease;
  }
  
  .skeleton-image, .skeleton-license {
    filter: brightness(1.05);
    transition: filter 0.3s ease;
  }
}
</style>
