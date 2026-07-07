<!-- src/views/notifications/template/Detail.vue -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="validationForm">
                    <b-row>
                        <!-- Tên mẫu -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Warning.Detail.Form.Name')"
                                label-class="required"
                                label-for="h-name"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="NotificationEventTemplateName"
                                >
                                    <b-form-input
                                        id="h-name"
                                        v-model.trim="notification.name"
                                        :disabled="!editing"
                                        :state="errors.length ? false : null"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Tiêu đề -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Warning.Detail.Form.Title')"
                                label-class="required"
                                label-for="h-title"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="NotificationTitle"
                                >
                                    <b-form-input
                                        id="h-title"
                                        v-model.trim="notification.title"
                                        :disabled="!editing"
                                        :state="errors.length ? false : null"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Đường dẫn -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Warning.Detail.Form.Link')"
                                label-class="required"
                                label-for="h-url"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="NotificationUrl"
                                >
                                    <b-form-input
                                        id="h-url"
                                        v-model.trim="notification.url"
                                        :disabled="!editing"
                                        :state="errors.length ? false : null"
                                        :placeholder="
                                            $t('Warning.Detail.Form.Link') ||
                                            'https://...'
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Sự kiện -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Warning.Detail.Form.EventType')"
                                label-class="required"
                                label-for="h-eventType"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="NotificationEventTypeId"
                                >
                                    <v-select
                                        id="h-eventType"
                                        v-model="notification.eventTypeId"
                                        :reduce="(o) => o.id"
                                        :disabled="!editing"
                                        :state="errors.length ? false : null"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :options="lstEventTypes"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Icon -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Warning.Detail.Form.Icon')"
                                label-for="h-icon"
                                label-cols-md="4"
                            >
                                <icon-picker
                                    id="h-icon"
                                    v-model="notification.icon"
                                    :field-props="{ disabled: !editing }"
                                />
                            </b-form-group>
                        </b-col>

                        <!-- Nội dung -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Warning.Detail.Form.Content')"
                                label-for="h-content"
                                label-cols-md="4"
                            >
                                <b-form-textarea
                                    id="h-note"
                                    v-model.trim="notification.content"
                                    :placeholder="placeholderNote"
                                    :disabled="!editing"
                                    rows="2"
                                    max-rows="4"
                                />
                            </b-form-group>
                        </b-col>

                        <!-- File âm thanh cảnh báo -->
                        <b-col md="6">
                            <b-form-group
                                :label="labelSoundFile"
                                label-for="h-sound-file"
                                label-cols-md="4"
                            >
                                <!-- CHẾ ĐỘ XEM: chỉ hiện tên file -->
                                <div v-if="!editing">
                                    <b-form-input
                                        id="h-notification-soundFilePath"
                                        :value="
                                            fileNameFromPath(
                                                notification.soundFilePath
                                            )
                                        "
                                        disabled
                                    />
                                    <div
                                        v-if="!notification.soundFilePath"
                                        class="text-muted mt-25"
                                    >
                                        {{ noDataText }}
                                    </div>
                                </div>

                                <!-- CHẾ ĐỘ SỬA: chọn file mới, không preview -->
                                <div v-else>
                                    <b-form-file
                                        id="h-sound-file"
                                        v-model="soundFile"
                                        :placeholder="
                                            fileNameFromPath(
                                                notification.soundFilePath
                                            )
                                        "
                                        :drop-placeholder="soundDropPlaceholder"
                                        accept=".mp3,.wav,.ogg"
                                    />
                                    <small class="text-muted d-block mt-25">{{
                                        soundAcceptNote
                                    }}</small>
                                    <small
                                        v-if="notification.soundFilePath"
                                        class="text-muted d-block mt-25"
                                    >
                                    </small>
                                </div>
                            </b-form-group>
                        </b-col>

                        <!-- Ghi chú -->
                        <b-col md="6">
                            <b-form-group
                                :label="labelNote"
                                label-for="h-note"
                                label-cols-md="4"
                            >
                                <b-form-textarea
                                    id="h-note"
                                    v-model.trim="notification.description"
                                    :placeholder="placeholderNote"
                                    :disabled="!editing"
                                    rows="2"
                                    max-rows="4"
                                />
                            </b-form-group>
                        </b-col>

                        <!-- Điều kiện cảnh báo -->
                        <b-col v-if="notification.eventTypeId" md="6">
                            <b-form-group
                                :label="
                                    $t('Warning.Detail.Form.AlertCondition')
                                "
                                label-for="h-condition"
                                label-cols-md="4"
                            >
                                <condition-suggestion
                                    ref="conditionSuggest"
                                    :event-type="
                                        String(notification.eventTypeId || '')
                                    "
                                    :field-props="{
                                        placeholder: $t(
                                            'Warning.Detail.Form.SqlPlaceholder'
                                        ),
                                    }"
                                    :data="notification.conditionGroups || []"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <!-- Actions -->
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-if="
                                    editing &&
                                    authorize(['ManageEventTemplate'])
                                "
                                type="button"
                                variant="primary"
                                class="mx-50 mb-50 btn-120"
                                @click="validationForm"
                                >{{ $t('Button.Save') }}</b-button
                            >

                            <b-button
                                v-if="
                                    !editing &&
                                    authorize(['ManageEventTemplate'])
                                "
                                type="button"
                                variant="primary"
                                class="mx-50 mb-50 btn-120"
                                @click="onEditing"
                                >{{ $t('Button.Edit') }}</b-button
                            >

                            <b-button
                                v-if="!editing"
                                :to="{ path: '/notification/template/list' }"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                                >{{ $t('Button.Back') }}</b-button
                            >

                            <b-button
                                v-if="editing"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                                @click="cancel"
                                >{{ $t('Button.Cancel') }}</b-button
                            >
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import ConditionSuggestion from '@/components/ConditionSuggestion.vue'
import IconPicker from '@/components/IconPicker.vue'
import { ValidationObserver, ValidationProvider } from 'vee-validate'
export default {
    name: 'NotificationEventTemplateDetail',
    components: {
        ConditionSuggestion,
        IconPicker,
        ValidationObserver,
        ValidationProvider,
    },
    mixins: [authorizationMixin],

    data() {
        return {
            notification: {
                name: null,
                title: null,
                content: null,
                eventTypeId: null,
                url: null,
                icon: { iconName: null, color: null },
                description: null,
                conditionGroups: [],
                soundFilePath: null, // giữ path/filename hiện tại từ API
            },

            lstEventTypes: [],
            editing: false,

            // file mới (khi Sửa)
            soundFile: null,
        }
    },

    computed: {
        id() {
            return this.$route.params.id
        },

        // labels/placeholders
        labelNote() {
            return this.$t('Warning.Detail.Form.Note') || 'Ghi chú'
        },
        placeholderNote() {
            return (
                this.$t('Warning.Detail.Form.NotePlaceholder') ||
                'Nhập ghi chú...'
            )
        },
        labelSoundFile() {
            return (
                this.$t('Device.Detail.Form.SoundFile') ||
                'File âm thanh cảnh báo'
            )
        },
        soundPlaceholder() {
            return (
                this.$t('Device.Detail.Form.SoundFileHolder') ||
                'Chọn một file hoặc kéo thả vào đây...'
            )
        },
        soundDropPlaceholder() {
            return 'Kéo file vào đây...'
        },
        soundAcceptNote() {
            return (
                this.$t('Device.Detail.Form.SoundFileType') ||
                'Chỉ chấp nhận .mp3, .wav, .ogg'
            )
        },
        // noDataText() {
        //     return this.$t('Common.NoData') || 'Không có dữ liệu'
        // },
    },

    async created() {
        await this.loadEventType()
        await this.loadDetail()
    },

    methods: {
        fileNameFromPath(p) {
            if (!p) return ''
            try {
                return decodeURIComponent(String(p).split('/').pop() || '')
            } catch {
                return String(p)
            }
        },

        loadEventType() {
            this.$services.get('/lookup/eventType').then((res) => {
                this.lstEventTypes = (res.data.data || []).map((item) => {
                    return { ...item, id: Number(item.id) }
                })
            })
        },

        async loadDetail() {
            const res = await this.$services.get(
                `/notification/notificationEventTemplate/${this.id}`
            )
            const d = res?.data?.data || {}

            this.notification = {
                name: d.name ?? null,
                title: d.title ?? null,
                content: d.content ?? null,
                eventTypeId: Number(d.eventTypeId) || null,
                url: d.url ?? null,
                icon: d.icon ?? { iconName: null, color: null },
                description: d.description ?? d.note ?? null,
                conditionGroups: Array.isArray(d.conditionGroups)
                    ? d.conditionGroups
                    : [],
                // CHỐT: ánh xạ mọi biến thể backend -> soundFilePath
                soundFilePath:
                    d.soundUrl ||
                    d.soundFileUrl ||
                    d.soundPath ||
                    d.soundFilePath ||
                    null,
            }

            this.soundFile = null
        },

        cancel() {
            this.editing = false
            this.loadDetail()
        },

        onEditing() {
            this.editing = true
        },

        async validationForm() {
            const isFormValid = await this.validateForm()
            if (!isFormValid) return

            if (this.soundFile) {
                try {
                    await this.validateSoundFile(this.soundFile)
                } catch (errorMessage) {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title:
                                this.$i18n.locale === 'vi'
                                    ? 'File âm thanh không hợp lệ'
                                    : 'Invalid Audio File',
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                            text: errorMessage,
                        },
                    })
                    return
                }
            }

            // Extract condition data with improved error handling
            if (this.$refs.conditionSuggest) {
                try {
                    const conditionData =
                        await this.$refs.conditionSuggest.getQuery()
                    this.notification.conditionGroups = conditionData?.data
                        ? [conditionData.data]
                        : []
                } catch (err) {
                    console.error('Error extracting condition data:', err)
                    this.notification.conditionGroups = []

                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title:
                                this.$i18n.locale === 'vi'
                                    ? 'Lỗi điều kiện'
                                    : 'Condition Error',
                            icon: 'AlertTriangleIcon',
                            variant: 'warning',
                            text:
                                this.$i18n.locale === 'vi'
                                    ? 'Không thể xử lý điều kiện SQL'
                                    : 'Cannot process SQL condition',
                        },
                    })
                }
            }

            await this.submitForm()
        },

        async validateForm() {
            const isFormValid = await this.$refs.rules.validate()
            if (!isFormValid) {
                // Get specific validation errors
                // const errors = this.$refs.rules.errors
                // const errorMessages = Object.values(errors).flat()

                // this.$toast({
                //     component: ToastificationContent,
                //     position: 'top-right',
                //     props: {
                //         title: 'Validation Error',
                //         icon: 'AlertTriangleIcon',
                //         variant: 'warning',
                //         text:
                //             this.$i18n.locale === 'vi'
                //                 ? `Vui lòng điền đầy đủ các trường bắt buộc: ${errorMessages.join(', ')}`
                //                 : `Please fill in all required fields: ${errorMessages.join(', ')}`,
                //     },
                // })
                return false
            }
            return true
        },

        async validateSoundFile(file) {
            try {
                // Validate file type
                await this.validateFileType(file)

                // Validate file size
                await this.validateFileSize(file)

                // Validate audio duration
                await this.validateAudioDuration(file)

                return true
            } catch (error) {
                throw error
            }
        },

        validateFileType(file) {
            return new Promise((resolve, reject) => {
                const allowedTypes = [
                    'audio/mpeg',
                    'audio/wav',
                    'audio/ogg',
                    'audio/mp3',
                ]
                const allowedExtensions = ['.mp3', '.wav', '.ogg']
                const fileExtension = file.name
                    .toLowerCase()
                    .substring(file.name.lastIndexOf('.'))

                if (
                    !allowedTypes.includes(file.type) &&
                    !allowedExtensions.includes(fileExtension)
                ) {
                    reject(
                        this.$i18n.locale === 'vi'
                            ? 'Chỉ chấp nhận file âm thanh .mp3, .wav, .ogg'
                            : 'Only audio files .mp3, .wav, .ogg are accepted'
                    )
                } else {
                    resolve()
                }
            })
        },

        validateFileSize(file) {
            return new Promise((resolve, reject) => {
                const maxSize = 20 * 1024 * 1024 // 20MB
                if (file.size > maxSize) {
                    reject(
                        this.$i18n.locale === 'vi'
                            ? 'Kích thước file vượt quá 20MB'
                            : 'File size exceeds 20MB'
                    )
                } else {
                    resolve()
                }
            })
        },

        validateAudioDuration(file) {
            return new Promise((resolve, reject) => {
                const audio = new Audio()

                // Add timeout to prevent hanging
                const timeout = setTimeout(() => {
                    reject('Audio loading timeout')
                }, 10000)

                audio.addEventListener('loadedmetadata', () => {
                    clearTimeout(timeout)

                    const duration = audio.duration
                    const maxDuration = 60 // 30 seconds

                    if (isNaN(duration)) {
                        reject('Cannot determine audio duration')
                        return
                    }

                    if (duration > maxDuration) {
                        const errorMsg =
                            this.$i18n.locale === 'vi'
                                ? 'File âm thanh không được dài quá 60 giây'
                                : 'Audio files must not exceed 60 seconds'

                        reject(errorMsg)
                    } else {
                        resolve()
                    }

                    // Cleanup
                    URL.revokeObjectURL(audio.src)
                })

                audio.addEventListener('error', (e) => {
                    clearTimeout(timeout)

                    const errorMsg =
                        this.$i18n.locale === 'vi'
                            ? 'Không thể xử lý file âm thanh này'
                            : 'Cannot process this audio file'

                    reject(errorMsg)
                })

                try {
                    const audioSrc = URL.createObjectURL(file)
                    audio.src = audioSrc
                } catch (e) {
                    clearTimeout(timeout)
                    reject('Cannot create audio URL')
                }
            })
        },

        async submitForm() {
            const formData = new FormData()

            Object.entries(this.notification).forEach(([key, value]) => {
                if (value === null || value === undefined) return

               if (key === 'conditionGroups') {
                    const appendConditions = (groups, prefix) => {
                        groups.forEach((group, index) => {
                            const currentPrefix = prefix
                                ? `${prefix}.children[${index}]`
                                : `ConditionGroups[${index}]`

                            Object.entries(group).forEach(
                                ([condKey, condValue]) => {
                                    if (
                                        condKey === 'children' &&
                                        Array.isArray(condValue)
                                    ) {
                                        // Đệ quy cho children
                                        appendConditions(
                                            condValue,
                                            currentPrefix
                                        )
                                    } else {
                                        const fieldName = `${currentPrefix}.${condKey}`
                                        formData.append(
                                            fieldName,
                                            condValue ?? ''
                                        )
                                    }
                                }
                            )
                        })
                    }

                    if (Array.isArray(value) && value.length > 0) {
                        appendConditions(value)
                    }
                } else if (key === 'icon') {
                    if (value && typeof value === 'object') {
                        formData.append('Icon.IconName', value.iconName || '')
                        formData.append('Icon.Color', value.color || '')
                    }
                } else {
                    const processedValue =
                        typeof value === 'string' ? value.trim() : value
                    formData.append(key, processedValue)
                }
            })

            if (this.soundFile) {
                formData.append(
                    'soundFile',
                    this.soundFile,
                    this.soundFile.name
                )
            }

            try {
                await this.$services.put(
                    `/notification/notificationEventTemplate/${this.id}`,
                    formData,
                    {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    }
                )

                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Success.UpdateTemplate'),
                        icon: 'CheckIcon',
                        variant: 'success',
                    },
                })

                this.$router.push({ path: '/notification/template/list' })
            } catch (error) {
                console.error('Create template error:', error)

                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text:
                            error?.response?.data?.message ||
                            error.message ||
                            'Có lỗi xảy ra',
                    },
                })
            }
        },

        // Handle condition suggestion errors
        handleConditionError(errorMessage) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title:
                        this.$i18n.locale === 'vi'
                            ? 'Lỗi điều kiện'
                            : 'Condition Error',
                    icon: 'AlertTriangleIcon',
                    variant: 'warning',
                    text:
                        errorMessage ||
                        (this.$i18n.locale === 'vi'
                            ? 'Không thể tải thuộc tính điều kiện'
                            : 'Cannot load condition properties'),
                },
            })
        },
    },
}
</script>

<style lang="scss">
.btn-120 {
    min-width: 120px;
}

.b-form-group > label {
    font-weight: 600;
}

.required::after {
    content: ' *';
    color: #ea5455;
}
</style>
