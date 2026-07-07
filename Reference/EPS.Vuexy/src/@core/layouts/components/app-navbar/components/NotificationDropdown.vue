<template>
    <b-nav-item-dropdown
        ref="dropdown"
        class="dropdown-notification mr-25"
        menu-class="dropdown-menu-media"
        right
        @shown="onDropdownShown"
        @hidden="onDropdownHidden"
        @click-outside="onClickOutside"
    >
        <template #button-content>
            <feather-icon
                :badge="isNewNotification ? '!' : ''"
                badge-classes="bg-danger"
                class="text-body"
                icon="BellIcon"
                size="21"
            />
        </template>

        <li class="dropdown-menu-header">
            <div class="dropdown-header d-flex">
                <h4 class="notification-title mb-0 mr-auto">
                    {{ $t('Notification') }}
                </h4>
                <b-badge pill variant="light-primary">
                    {{ totalNotification ? totalNotification + ' New' : '' }}
                </b-badge>
            </div>
        </li>

        <div class="media-list">
            <b-link
                v-for="notification in lstAllNotification.slice(0, 5)"
                :key="notification.id"
                @click.stop="readNotification(notification.id)"
            >
                <b-media>
                    <template #aside>
                        <font-awesome-icon
                            v-if="notification.icon != null"
                            :icon="notification.icon"
                        />
                        <Icon
                            :icon="notification.icon"
                            class="md-icon"
                            size="21"
                            :color="notification.iconColor"
                        />
                    </template>
                    <p class="media-heading">
                        <span class="font-weight-bolder">
                            {{ notification.title }}
                        </span>
                    </p>
                    <small class="notification-text">
                        {{ notification.content }}
                    </small>
                </b-media>
            </b-link>
        </div>

        <li class="dropdown-menu-footer">
            <b-button
                v-ripple.400="'rgba(255, 255, 255, 0.15)'"
                variant="primary"
                block
                @click.stop="readAllNotification()"
            >
                {{ $t('AllNotification') }}
            </b-button>
        </li>
    </b-nav-item-dropdown>
</template>

<script>
/* eslint-disable */
import {
    BNavItemDropdown,
    BBadge,
    BMedia,
    BLink,
    BAvatar,
    BButton,
    BFormCheckbox,
} from 'bootstrap-vue'
import VuePerfectScrollbar from 'vue-perfect-scrollbar'
import Ripple from 'vue-ripple-directive'
import { authorizationMixin } from '@core/mixins/ui/forms'
import signalRService from '@/utils/signalr-service'

export default {
    mixins: [authorizationMixin],
    components: {
        BNavItemDropdown,
        BBadge,
        BMedia,
        BLink,
        BAvatar,
        VuePerfectScrollbar,
        BButton,
        BFormCheckbox,
    },
    directives: {
        Ripple,
    },
    data() {
        return {
            lstNotification: [],
            lstAllNotification: [],
            totalNotification: 0,
            isNewNotification:
                localStorage.getItem('isNewNotification') === 'true',
            currentSoundUrl: null,
            isClosingDropdown: false,
            /** id của thông báo cuối cùng đã phát âm thanh */
            lastPlayedNotificationId: null,
        }
    },
    async created() {
        console.log('Component created, loading notifications without sound')
        this.resetSound()
        // Lần load đầu: chỉ load danh sách, KHÔNG phát âm
        this.loadNotification(false)

        await signalRService.connect('notificationHub')
        signalRService.on('ReceiveNotification', (data) => {
            console.log('Received new notification via SignalR:', data)
            // Mỗi lần SignalR bắn: luôn gọi loadNotification(true)
            // để check và phát âm nếu có thông báo mới
            this.loadNotification(true)
        })
    },
    methods: {
        playSound() {
            if (this.currentSoundUrl) {
                console.log('Playing sound for URL:', this.currentSoundUrl)
                try {
                    const audio = new Audio(this.currentSoundUrl)
                    // đảm bảo luôn play lại từ đầu
                    audio.currentTime = 0
                    audio.play().catch((e) => {
                        console.error('Lỗi khi phát âm thanh:', e)
                    })
                } catch (e) {
                    console.error('Lỗi tạo Audio object:', e)
                }
            } else {
                console.warn('Không tìm thấy URL âm thanh để phát.')
            }
        },
        resetSound() {
            console.log('Resetting sound URL')
            this.currentSoundUrl = null
        },

        /**
         * Load notification:
         * - fromSignalR = false: load ban đầu, không phát âm
         * - fromSignalR = true: có thể phát âm nếu có noti mới
         */
        loadNotification(fromSignalR = false) {
            console.log('Loading notifications, fromSignalR:', fromSignalR)

            // 👉 BỎ maxId khỏi query, luôn lấy top mới nhất
            const url =
                '/notification?page=1&sortBy=accessDate&ItemsPerPage=20&checkAll=false'

            this.$services
                .get(url)
                .then((response) => {
                    const newNotifications = response.data?.data?.data || []

                    if (newNotifications && newNotifications.length > 0) {
                        // Merge: giữ list all, nhưng đảm bảo không duplicate
                        const existingIds = new Set(
                            this.lstAllNotification.map((x) => x.id)
                        )
                        const dedupNew = newNotifications.filter(
                            (n) => !existingIds.has(n.id)
                        )

                        if (dedupNew.length > 0) {
                            // prepend new ones
                            this.lstAllNotification.unshift(...dedupNew)
                            this.totalNotification += dedupNew.length
                        } else {
                            // Nếu tất cả đều đã có thì vẫn update danh sách
                            // để đảm bảo thứ tự mới nhất
                            this.lstAllNotification = newNotifications.slice()
                        }

                        this.totalNotification = this.lstAllNotification.length
                        this.updateNewNotificationStatus(true)

                        if (fromSignalR) {
                            const latestNotification = newNotifications[0] // giả sử sorted desc theo accessDate

                            console.log(
                                'Latest notification from server:',
                                latestNotification
                            )

                            // Chỉ phát âm nếu:
                            // 1. Có sound & urlSound
                            // 2. id khác với lần đã phát gần nhất
                            if (
                                latestNotification &&
                                latestNotification.sound === true &&
                                latestNotification.urlSound
                            ) {
                                if (
                                    this.lastPlayedNotificationId !==
                                    latestNotification.id
                                ) {
                                    this.currentSoundUrl = `${
                                        process.env.VUE_APP_BASE_URL
                                    }${latestNotification.urlSound}`
                                    console.log(
                                        'New notification with sound, URL:',
                                        this.currentSoundUrl
                                    )
                                    this.playSound()
                                    this.lastPlayedNotificationId =
                                        latestNotification.id
                                } else {
                                    console.log(
                                        'Notification already played sound, skip'
                                    )
                                }
                            } else {
                                console.log(
                                    'Latest notification has no sound config'
                                )
                                this.currentSoundUrl = null
                            }
                        } else {
                            // Load ban đầu: không phát
                            this.currentSoundUrl = null
                            console.log(
                                'Initial load, skip sound playback (fromSignalR = false)'
                            )
                        }
                    } else {
                        console.log('No notifications returned from server')
                        this.currentSoundUrl = null
                        this.totalNotification = this.lstAllNotification.length
                    }
                })
                .catch((error) => {
                    console.error('Lỗi khi tải thông báo:', error)
                    this.currentSoundUrl = null
                })
        },

        closeDropdown() {
            if (this.isClosingDropdown) {
                console.log('Dropdown is already being closed, skipping...')
                return
            }
            this.isClosingDropdown = true
            console.log('Attempting to close dropdown...')

            if (this.$refs.dropdown) {
                this.$refs.dropdown.hide()
                const dropdownElement =
                    this.$refs.dropdown.$el.querySelector('.dropdown-menu')
                if (dropdownElement) {
                    if (dropdownElement.classList.contains('show')) {
                        dropdownElement.classList.remove('show')
                        console.log('Manually removed show class from dropdown')
                    }
                    dropdownElement.style.display = 'none'
                }
                const buttonElement =
                    this.$refs.dropdown.$el.querySelector('.dropdown-toggle')
                if (buttonElement) {
                    if (buttonElement.classList.contains('show')) {
                        buttonElement.classList.remove('show')
                        console.log('Manually removed show class from button')
                    }
                    buttonElement.setAttribute('aria-expanded', 'false')
                }
                const parentDropdown = this.$refs.dropdown.$el
                if (parentDropdown.classList.contains('show')) {
                    parentDropdown.classList.remove('show')
                }
            }

            setTimeout(() => {
                this.isClosingDropdown = false
                console.log('Reset isClosingDropdown flag')
            }, 300)
        },

        readNotification(id) {
            this.resetSound()
            this.$services
                .put(`/notification/read/${id}`)
                .then((response) => {
                    this.lstAllNotification = this.lstAllNotification.filter(
                        (x) => x.id !== id
                    )
                    this.totalNotification = this.lstAllNotification.length

                    this.$nextTick(() => {
                        this.closeDropdown()
                        this.$nextTick(() => {
                            const targetPath = response.data.data.value
                            const queryString = `notificationId=${id}&refresh=${Date.now()}`
                            window.location.assign(
                                `${targetPath}?${queryString}`
                            )
                        })
                    })
                })
                .catch((error) => {
                    console.error('Lỗi khi đọc thông báo:', error)
                    this.closeDropdown()
                })
        },

        readAllNotification() {
            this.resetSound()
            const targetPath = '/notification/allNotification/list'
            this.$nextTick(() => {
                this.closeDropdown()
                this.$nextTick(() => {
                    if (this.$route.path === targetPath) {
                        this.$router.replace({
                            path: targetPath,
                            query: { refresh: Date.now() },
                        })
                    } else {
                        this.$router.push({
                            path: targetPath,
                            query: { refresh: Date.now() },
                        })
                    }
                })
            })
        },

        updateNewNotificationStatus(status) {
            this.isNewNotification = status
            localStorage.setItem('isNewNotification', status)
            console.log('Updated isNewNotification status:', status)
        },

        onDropdownShown() {
            console.log('Dropdown shown')
            this.isNewNotification = false
            localStorage.setItem('isNewNotification', false)
            if (this.isClosingDropdown) {
                console.log('Dropdown opened while closing, forcing close...')
                this.closeDropdown()
            }
        },
        onDropdownHidden() {
            console.log('Dropdown has been hidden')
            this.isClosingDropdown = false
        },
        onClickOutside() {
            console.log('Clicked outside dropdown')
            this.closeDropdown()
        },
    },
}
</script>

<style>
.dropdown-menu-media {
    display: flex;
    flex-direction: column;
    min-width: 320px;
    width: fit-content;
    box-shadow: 0 4px 24px 0 rgb(34 41 47 / 10%);
    border-radius: 0.5rem;
    display: none !important;
}
.dropdown-menu-media.show {
    display: flex !important;
}
.media-list {
    padding: 0;
    margin: 0;
    display: block;
}
.dropdown-menu-header .dropdown-header {
    padding: 0.75rem 1rem;
    white-space: nowrap;
    border-bottom: 1px solid #ededed;
}
.dropdown-notification .media-list .media {
    padding: 0.7rem 1rem;
    border-bottom: 1px solid #ededed;
}
.dropdown-menu-footer {
    padding: 0.5rem 1rem;
    margin-top: auto;
    border-top: 1px solid #ededed;
    background-color: #fff;
}
.dropdown-notification .media-list .media-heading,
.dropdown-notification .notification-text {
    white-space: normal;
    text-overflow: ellipsis;
}
</style>
