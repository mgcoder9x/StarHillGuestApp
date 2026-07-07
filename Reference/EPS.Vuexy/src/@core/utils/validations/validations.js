/* eslint-disable */
import { extend, localize } from 'vee-validate'
import {
    required as rule_required,
    email as rule_email,
    min as rule_min,
    max as rule_max,
    confirmed as rule_confirmed,
    regex as rule_regex,
    between as rule_between,
    alpha as rule_alpha,
    integer as rule_integer,
    digits as rule_digits,
    alpha_dash as rule_alpha_dash,
    alpha_num as rule_alpha_num,
    length as rule_length,
    is_not as rule_is_not,
    min_value as rule_min_value,
} from 'vee-validate/dist/rules'
import en from 'vee-validate/dist/locale/en.json'
import vi from 'vee-validate/dist/locale/vi.json'
import i18n from '@/libs/i18n'

import {
    validatorPositive,
    validatorFromToDate,
    validatorUrlValidator,
    validatorPassword,
    validatorCreditCard,
    validatorCode,
    validatorNoSpecialChars,
} from './validators'

/* ============================
   Built-in rules (no messages)
============================= */
export const required = extend('required', rule_required)
export const email = extend('email', rule_email)
export const min = extend('min', rule_min)
export const max = extend('max', rule_max)
export const confirmed = extend('confirmed', { ...rule_confirmed })
export const regex = extend('regex', rule_regex)
export const between = extend('between', rule_between)
export const alpha = extend('alpha', rule_alpha)
export const integer = extend('integer', rule_integer)
export const digits = extend('digits', rule_digits)
/* IMPORTANT: match locale keys with underscores */
export const alphaDash = extend('alpha_dash', rule_alpha_dash)
export const alphaNum = extend('alpha_num', rule_alpha_num)
export const length = extend('length', rule_length)
export const is_not = extend('is_not', rule_is_not)
export const min_value = extend('min_value', rule_min_value)

/* ============================
   Custom rules (boolean only)
============================= */
export const positive = extend('positive', {
    validate: validatorPositive,
})

export const credit = extend('credit-card', {
    validate: validatorCreditCard,
})

/* Keep rule name = 'fromDate' to match existing templates */
export const fromDate = extend('fromDate', {
    params: ['toDate'],
    validate: validatorFromToDate,
})

export const password = extend('password', {
    validate: validatorPassword,
})

export const url = extend('url', {
    validate: validatorUrlValidator,
})

export const valid_range = extend('valid_range', {
    params: ['max', 'fieldMax'],
    validate(value, { max }) {
        if (
            value === null ||
            value === undefined ||
            value === '' ||
            max === null ||
            max === undefined ||
            max === ''
        )
            return true
        return Number(value) < Number(max)
    },
})

export const phone = extend('phone', {
    validate: (value) => /^[0-9]{10,11}$/.test(String(value || '')),
})

export const futureDate = extend('futureDate', {
    validate: (value) => {
        if (!value) return false
        const selected = new Date(
            value instanceof Date ? value : String(value).replace(' ', 'T')
        )
        const today = new Date()
        today.setHours(0, 0, 0, 0)
        return selected <= today
    },
})

export const minValue = extend('minValue', {
    params: ['max'],
    validate: (value, { max }) => Number(value) < Number(max),
})

export const validateCourseDates = extend('validateCourseDates', {
    params: ['courseStartTime'],
    validate: (value, { courseStartTime }) => {
        const startDate = new Date(String(value || '').replace(' ', 'T'))
        const courseStart = new Date(
            String(courseStartTime || '').replace(' ', 'T')
        )
        if (isNaN(courseStart.getTime())) return false
        return startDate > courseStart
    },
})

extend('time_after', {
    params: ['target'],
    validate(value, { target }) {
        if (!value || !target) return true
        const toMinutes = (t) => {
            const [h, m] = String(t).split(':').map(Number)
            return (h || 0) * 60 + (m || 0)
        }
        return toMinutes(value) > toMinutes(target)
    },
})

export const validateEndDate = extend('validateEndDate', {
    params: ['startDate', 'courseEndTime'],
    validate: (value, { startDate, courseEndTime }) => {
        const end = new Date(String(value || '').replace(' ', 'T'))
        const start = new Date(String(startDate || '').replace(' ', 'T'))
        const courseEnd = new Date(
            String(courseEndTime || '').replace(' ', 'T')
        )
        if (
            isNaN(end.getTime()) ||
            isNaN(start.getTime()) ||
            isNaN(courseEnd.getTime())
        )
            return false
        return end > start && end < courseEnd
    },
})

export const decimal = extend('decimal', {
    params: ['decimals'],
    validate: (value, { decimals }) => {
        const re = new RegExp(`^-?\\d*(\\.\\d{1,${decimals}})?$`)
        return re.test(String(value ?? ''))
    },
})

export const noSpecialChars = extend('noSpecialChars', {
    validate: validatorNoSpecialChars,
})

export const noSpecialCharsExceptUnderscore = extend(
    'noSpecialCharsExceptUnderscore',
    {
        validate: validatorCode,
    }
)

/* ============================
   i18n messages / names / fields
============================= */
localize({
    en: {
        messages: {
            ...en.messages,
            confirmed: 'Passwords do not match',
            is_not: '{_field_} is not valid',
            time_after: 'End time must be greater than start time',
            futureDate: 'Please enter a date that is not in the future',
            validateEndDate:
                'End date must be after start date and before course end date',
            validateCourseDates: 'Start date must be after course start time',
            phone: 'Invalid phone number. Must be 10–11 digits.',
            decimal: 'Maximum {decimals} decimal places allowed.',
            fromDate: 'From date must be before to date',
            'credit-card': 'It is not a valid credit card!',
            positive: 'Please enter a positive number!',
            url: 'URL is invalid',
            valid_range: '{_field_} must be less than {fieldMax}',
            minValue: '{_field_} must be less than {max}',
            noSpecialChars: 'Special characters are not allowed',
            noSpecialCharsExceptUnderscore:
                'Special characters are not allowed',
            password:
                'Your password must include at least one uppercase, one lowercase, one special character, and one digit',
            min_value: 'The value must be greater than or equal to {min}',
        },
        names: {
            TreeIndex: 'Index',
            email: 'Email',
            password: 'Password',
            AreaCode: 'area code',
            AreaName: 'area name',
            ContractorCode: 'contractor code',
            ContractorName: 'contractor name',
            ParentAreaName: 'Parent area',
            StartDate: 'start date',
            EndDate: 'end date',
        },
        fields: {
            password: {
                min: '{_field_} is too short, minimum is 8 characters',
            },
            licensePlate: {
                max: 'License plate cannot exceed 9 characters',
            },
        },
    },
    vi: {
        messages: {
            ...vi.messages,
            confirmed: 'Mật khẩu không khớp',
            is_not: '{_field_} không hợp lệ',
            time_after: 'Giờ kết thúc phải lớn hơn giờ bắt đầu',
            futureDate: 'Vui lòng nhập ngày nhỏ hơn hoặc bằng ngày hiện tại.',
            validateEndDate:
                'Ngày kết thúc phải sau ngày bắt đầu lớp học và nhỏ hơn thời gian kết thúc của khóa học!',
            validateCourseDates:
                'Vui lòng chọn ngày bắt đầu lớp học trước thời gian bắt đầu của khóa học!',
            phone: 'Số điện thoại không hợp lệ. Vui lòng nhập 10–11 chữ số.',
            decimal: 'Tối đa {decimals} chữ số sau dấu thập phân.',
            fromDate: 'Thời gian từ ngày phải nhỏ hơn thời gian đến ngày',
            'credit-card': 'Thẻ tín dụng không hợp lệ!',
            positive: 'Vui lòng nhập số dương!',
            url: 'URL không hợp lệ',
            valid_range: '{_field_} phải nhỏ hơn {fieldMax}',
            minValue: '{_field_} phải nhỏ hơn {max}',
            noSpecialChars: 'Không được nhập ký tự đặc biệt',
            noSpecialCharsExceptUnderscore: 'Không được nhập ký tự đặc biệt',
            password:
                'Mật khẩu của bạn phải chứa ít nhất một chữ hoa, một chữ thường, một ký tự đặc biệt và một chữ số',
            min_value: 'Giá trị phải lớn hơn hoặc bằng {min}',
        },
        names: {
            Role: 'Nhóm quyền',
            TreeIndex: 'Thứ tự',
            Company: 'Tên đơn vị',
            ConfirmPassword: 'Nhập lại mật khẩu',
            Email: 'Email',
            FullName: 'Họ và tên',
            Password: 'Mật khẩu',
            UserName: 'Tên tài khoản',
            RoleName: 'Tên nhóm người dùng',
            Area: 'Khu vực',
            AreaCode: 'Mã khu vực',
            DepartmentCode: 'Mã phòng ban',
            DepartmentName: 'Tên phòng ban',
            AreaName: 'Tên khu vực',
            ContractorCode: 'Mã nhà thầu',
            ContractorName: 'Tên nhà thầu',
            ParentAreaName: 'Khu vực cha',
            DeviceCode: 'Mã camera',
            DeviceName: 'Tên camera',
            DeviceArea: 'Khu vực',
            DeviceEventType: 'Chức năng',
            DeviceStatus: 'Trạng thái',
            DeviceServer: 'Máy chủ',
            DeviceLink: 'Đường dẫn',
            DeviceLicense: 'Giấy phép sản phẩm',
            AccountOldPassword: 'Mật khẩu cũ',
            AccountNewPassword: 'Mật khẩu mới',
            AccountRetypeNewPassword: 'Nhập lại mật khẩu',
            NotificationEventTemplateName: 'Tên mẫu cảnh báo',
            NotificationTitle: 'Tiêu đề cảnh báo',
            NotificationUrl: 'Đường dẫn cảnh báo',
            NotificationEventTypeId: 'Loại sự kiện cảnh báo',
            NotificationTemplateId: 'Mẫu cảnh báo',
            ClassCode: 'Mã lớp',
            ClassName: 'Tên lớp',
            ClassCourse: 'Khóa học',
            ClassLesson: 'Bài giảng',
            ClassStatus: 'Trạng thái',
            LessonTitle: 'Tiêu đề',
            LessonDescription: 'Mô tả',
            LessonPercent: 'Phần trăm cần để hoàn thành',
            LessonLevel: 'Cấp độ',
            LessonConfigTime: 'Thời gian nhận diện',
            LessonTimeStudy: 'Thời gian của bài học',
            LessonRequireMinute: 'Số phút yêu cầu',
            StartDate: 'Ngày bắt đầu',
            EndDate: 'Ngày kết thúc',
        },
        fields: {
            password: { min: '{_field_} quá ngắn. Tối thiểu là 8 ký tự' },
            licensePlate: {
                max: 'Biển số xe không được vượt quá 9 ký tự',
            },
        },
    },
})

/* Sync vee-validate locale with vue-i18n */
localize(i18n.locale)

/* Try to react to runtime language changes (support multiple setups) */
try {
    // Nuxt i18n
    // @ts-ignore
    i18n.onLanguageSwitched?.((_, newLocale) => localize(newLocale))
} catch {}
try {
    // Vue 2 + vue-i18n v8: watch locale on a vm if exposed
    // @ts-ignore
    i18n.vm?.$watch('locale', (l) => localize(l))
} catch {}
