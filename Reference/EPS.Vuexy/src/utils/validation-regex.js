import { extend } from 'vee-validate'

/**
 * Tạo một quy tắc validation dựa trên regex.
 * @param {string} ruleName - Tên của quy tắc validation.
 * @param {string} pattern - Biểu thức chính quy cho validation.
 * @param {string} errorMessage - Thông báo lỗi khi validation thất bại.
 * @param {string} flag - Cờ nếu có bội quy tắc chính quy.
 */
function createRegexValidationRule(
    ruleName,
    pattern,
    errorMessage,
    flag = null
) {
    extend(ruleName, {
        validate(value) {
            const regex = flag ? new RegExp(pattern, flag) : new RegExp(pattern)
            return regex.test(value)
        },
        message: errorMessage,
    })
}

export default createRegexValidationRule
