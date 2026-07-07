<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
                        <!-- Tên mẫu -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Warning.Detail.Form.Name')"
                                label-class="required"
                                label-for="h-notification-template-name"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="NotificationEventTemplateName"
                                >
                                    <b-form-input
                                        id="h-notification-template-name"
                                        v-model.trim="notification.name"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
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
                                label-for="h-notification-template-title"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="NotificationTitle"
                                >
                                    <b-form-input
                                        id="h-notification-template-title"
                                        v-model.trim="notification.title"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
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
                                label-for="h-notification-template-url"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="NotificationUrl"
                                >
                                    <b-form-input
                                        id="h-notification-template-url"
                                        v-model.trim="notification.url"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Loại sự kiện -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Warning.Detail.Form.EventType')"
                                label-class="required"
                                label-for="h-notification-template-eventTypeId"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="NotificationEventTypeId"
                                >
                                    <v-select
                                        v-model="notification.eventTypeId"
                                        :reduce="(item) => item.id"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
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
                                />
                            </b-form-group>
                        </b-col>

                        <!-- Nội dung -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Warning.Detail.Form.Content')"
                                label-for="h-notification-template-content"
                                label-cols-md="4"
                            >
                                <content-suggest-field
                                    v-model.trim="notification.content"
                                    :field-props="{ name: 'content' }"
                                    :event-type="notification.eventTypeId"
                                />
                            </b-form-group>
                        </b-col>

                        <!-- File âm thanh cảnh báo -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t('Device.Detail.Form.SoundFile') ||
                                    'File âm thanh cảnh báo'
                                "
                                label-for="h-sound-file"
                                label-cols-md="4"
                            >
                                <b-form-file
                                    id="h-sound-file"
                                    v-model="soundFile"
                                    :placeholder="
                                        $t(
                                            'Device.Detail.Form.SoundFileHolder'
                                        ) ||
                                        'Chọn một file hoặc kéo thả vào đây...'
                                    "
                                    drop-placeholder="Kéo file vào đây..."
                                    accept=".mp3, .wav, .ogg"
                                    @input="onSoundFileChange"
                                />
                                <small class="text-muted d-block mt-25">
                                    {{
                                        $t(
                                            'Device.Detail.Form.SoundFileType'
                                        ) || 'Chỉ chấp nhận .mp3, .wav, .ogg'
                                    }}
                                </small>

                                <!-- Preview audio -->
                                <!-- <div v-if="soundPreviewUrl" class="mt-1">
                                    <audio
                                        :src="soundPreviewUrl"
                                        controls
                                        style="width: 100%"
                                    ></audio>
                                </div>  -->
                            </b-form-group>
                        </b-col>

                        <!-- Ghi chú -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t('Warning.Detail.Form.Note') || 'Ghi chú'
                                "
                                label-for="h-notification-template-note"
                                label-cols-md="4"
                            >
                                <content-suggest-field
                                    style="height: 50px"
                                    id="h-notification-template-note"
                                    v-model.trim="notification.description"
                                    :placeholder="
                                        $t(
                                            'Warning.Detail.Form.NotePlaceholder'
                                        ) || 'Nhập ghi chú...'
                                    "
                                />
                            </b-form-group>
                        </b-col>

                        <!-- Nút mở điều kiện -->
                        <b-col md="6">
                            <button
                                v-if="
                                    !showCondition && notification.eventTypeId
                                "
                                class="btn btn-primary"
                                type="button"
                                @click="showCondition = true"
                            >
                                <feather-icon icon="PlusIcon" class="mr-25" />
                                {{ $t('Warning.Detail.Form.AddCondition') }}
                            </button>

                            <b-form-group
                                v-if="showCondition"
                                :label="
                                    $t('Warning.Detail.Form.AlertCondition')
                                "
                                label-for="h-notification-template-condition"
                                label-cols-md="4"
                            >
                                <!-- Cho phép @param theo yêu cầu -->
                                <condition-suggestion
                                    ref="conditionSuggest"
                                    :event-type="notification.eventTypeId"
                                    :field-props="{
                                        placeholder: $t(
                                            'Warning.Detail.Form.SqlPlaceholder'
                                        ),
                                    }"
                                    :allow-at-param="true"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-if="authorize(['ManageEventTemplate'])"
                                type="submit"
                                variant="primary"
                                class="mr-1"
                                @click.prevent="validationForm"
                            >
                                {{ $t('Button.Save') }}
                            </b-button>
                            <b-button
                                :to="{ path: '/notification/template/list' }"
                                type="reset"
                                variant="outline-secondary"
                            >
                                {{ $t('Button.Cancel') }}
                            </b-button>
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
import ContentSuggestField from '../component/ContentSuggestField.vue'
import { ValidationObserver, ValidationProvider } from 'vee-validate'
export default {
    components: {
        ConditionSuggestion,
        IconPicker,
        ContentSuggestField,
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
                icon: { color: '#D0D2D6', iconName: null },
                conditionGroups: [],
                description: null,
            },
            // âm thanh
            soundFile: null,
            soundPreviewUrl: '',

            showCondition: false,
            lstEventTypes: [],
        }
    },
    created() {
        this.loadEventType()
    },
    beforeDestroy() {
        if (this.soundPreviewUrl) URL.revokeObjectURL(this.soundPreviewUrl)
    },
    methods: {
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                this.lstEventTypes = response.data.data
            })
        },

        onSoundFileChange(e) {
            const file = e?.target?.files?.[0] || this.soundFile

            // Clear previous preview URL
            if (this.soundPreviewUrl) {
                URL.revokeObjectURL(this.soundPreviewUrl)
                this.soundPreviewUrl = ''
            }

            if (!file) {
                this.soundFile = null
                return
            }

            this.validateSoundFile(file)
                .then(() => {
                    this.soundFile = file
                    this.soundPreviewUrl = URL.createObjectURL(file)
                })
                .catch((errorMessage) => {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title:
                                this.$i18n.locale === 'vi'
                                    ? 'File không hợp lệ'
                                    : 'Invalid File',
                            icon: 'AlertTriangleIcon',
                            variant: 'warning',
                            text: errorMessage,
                        },
                    })
                })
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

        // Improved form validation with detailed error messages
        async validateForm() {
            const isFormValid = await this.$refs.rules.validate()
            if (!isFormValid) {
                // // Get specific validation errors
                // const errors = this.$refs.rules.errors
                // const errorMessages = Object.values(errors).flat()

                // this.$toast({
                //     component: ToastificationContent,
                //     position: 'top-right',
                //     props: {
                //         title: 'Validation Error',
                //         icon: 'AlertTriangleIcon',
                //         variant: 'warning',
                //         text: this.$i18n.locale === 'vi'
                //             ? `Vui lòng điền đầy đủ các trường bắt buộc: ${errorMessages.join(', ')}`
                //             : `Please fill in all required fields: ${errorMessages.join(', ')}`
                //     },
                // })
                return false
            }
            return true
        },

        // Validate specific business rules
        // validateBusinessRules() {
        //     // Check if eventTypeId is selected when condition is shown
        //     if (this.showCondition && !this.notification.eventTypeId) {
        //         this.$toast({
        //             component: ToastificationContent,
        //             position: 'top-right',
        //             props: {
        //                 title: 'Validation Error',
        //                 icon: 'AlertTriangleIcon',
        //                 variant: 'warning',
        //                 text: this.$i18n.locale === 'vi'
        //                     ? 'Vui lòng chọn loại sự kiện trước khi thêm điều kiện'
        //                     : 'Please select event type before adding conditions'
        //             },
        //         })
        //         return false
        //     }

        //     return true
        // },

        async validationForm() {
            const isFormValid = await this.validateForm()
            if (!isFormValid) return

            // Validate business rules
            // const areBusinessRulesValid = this.validateBusinessRules()
            // if (!areBusinessRulesValid) return

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
                await this.$services.post(
                    '/notification/notificationEventTemplate',
                    formData,
                    {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    }
                )

                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Success.CreateTemplate'),
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
    },
}
</script>

<style lang="scss">
/* cân đối nhẹ layout, không ảnh hưởng phần khác */
.b-form-group > label {
    font-weight: 600;
}
.required::after {
    content: ' *';
    color: #ea5455;
}
</style>
