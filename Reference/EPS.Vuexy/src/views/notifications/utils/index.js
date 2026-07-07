const appendConditions = (formData, groups, prefix) => {
    groups.forEach((group, index) => {
        const currentPrefix = prefix
            ? `${prefix}.children[${index}]`
            : `ConditionGroups[${index}]`

        Object.entries(group).forEach(([condKey, condValue]) => {
            if (condKey === 'children' && Array.isArray(condValue)) {
                // Đệ quy cho children
                appendConditions(formData, condValue, currentPrefix)
            } else {
                const fieldName = `${currentPrefix}.${condKey}`
                formData.append(fieldName, condValue ?? '')
            }
        })
    })
}
export { appendConditions }
