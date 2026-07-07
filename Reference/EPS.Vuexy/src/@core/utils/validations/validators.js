export const validatorPositive = (value) => {
    return value >= 0
}

export const validatorPassword = (password) => {
    /* eslint-disable no-useless-escape */
    const regExp = /(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%&*()]).{8,}/
    /* eslint-enable no-useless-escape */
    const validPassword = regExp.test(password)
    return validPassword
}

export const validatorCreditCard = (creditnum) => {
    /* eslint-disable no-useless-escape */
    const cRegExp = /^(?:3[47][0-9]{13})$/
    /* eslint-enable no-useless-escape */
    const validCreditCard = cRegExp.test(creditnum)
    return validCreditCard
}

export const validatorUrlValidator = (val) => {
    if (val === undefined || val === null || val.length === 0) {
        return true
    }
    /* eslint-disable no-useless-escape */
    const re =
        /^(http[s]?:\/\/){0,1}(www\.){0,1}[a-zA-Z0-9\.\-]+\.[a-zA-Z]{2,5}[\.]{0,1}/
    /* eslint-enable no-useless-escape */
    return re.test(val)
}

export const validatorFromToDate = (fromDate, { toDate }) => {
    if (fromDate && toDate) {
        if (fromDate > toDate) {
            return false
        }
    }
    return true
}

export const validatorCode = (code) => {
    const regExp = /^[a-zA-Z0-9\s_]*$/
    const validCode = regExp.test(code)
    return validCode
}

export const validatorNoSpecialChars = (string) => {
    const regExp = /^[a-zA-Z0-9\s]*$/
    const validString = regExp.test(string)
    return validString
}
