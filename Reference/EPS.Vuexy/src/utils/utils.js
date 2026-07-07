import { v4 as uuidv4 } from 'uuid'

export const debounce = (callback, wait) => {
    let timeoutId = null
    // eslint-disable-next-line func-names
    return function (...args) {
        const context = this // Capture `this` from the caller
        window.clearTimeout(timeoutId)
        timeoutId = window.setTimeout(() => {
            callback.apply(context, args) // Use the captured context
        }, wait)
    }
}
export function getSelectedText(list, selectedIds, fallbackText) {
    if (Array.isArray(selectedIds) && selectedIds.length > 0) {
        return selectedIds
            .map((id) => {
                const item = list.find((dv) => dv.Id === id)
                return item ? item.text : null
            })
            .filter(Boolean) // Remove null/undefined values
            .join(', ') // Join the matching texts with a comma
    }
    if (
        !selectedIds ||
        !Array.isArray(selectedIds.length) ||
        selectedIds.length === 0
    ) {
        return fallbackText
    }

    const item = list.find((dv) => dv.Id === selectedIds)
    return item ? item.text : fallbackText
}

function parseCondition(input) {
    if (!input) return null
    // Helper functions remain the same
    function createConditionNode(property, operator, value, parentId = null) {
        return {
            id: uuidv4(),
            parentId,
            type: 'condition',
            conditionType: null,
            Property: property,
            Operator: operator,
            Value: value,
        }
    }

    function createGroupNode(conditionType, children = [], parentId = null) {
        return {
            id: uuidv4(),
            parentId,
            type: 'group',
            conditionType,
            Property: null,
            Operator: null,
            Value: null,
            children,
        }
    }

    function parseSimpleCondition(condition) {
        // const regex = /(\w+)\s*(=|!=|>|<|>=|<=)\s*('?\w[^']*'?)$/
        const regex = /^(\w+)\s*(=|!=|>|<|>=|<=)\s*(('[^']*')|(@\w+)|([\w.]+))$/
        const match = condition.trim().match(regex)
        if (match) {
            return createConditionNode(
                match[1],
                match[2],
                match[3].replace(/'/g, '')
            )
        }
        throw new Error(`Invalid condition: ${condition}`)
    }

    function parseComplexCondition(input, parentId = null) {
        input = input.trim()

        // Step 1: Handle parentheses first
        while (input.startsWith('(') && input.endsWith(')')) {
            const trimmed = input.substring(1, input.length - 1).trim()
            if (trimmed.includes('(') || trimmed.includes(')')) break // Nested parentheses
            input = trimmed
        }

        // Step 2: Split by top-level logical operators (AND, OR)
        const logicalOperatorRegex = /\s+(AND|OR)\s+(?![^()]*\))/i
        const split = input.split(logicalOperatorRegex)

        if (split.length > 1) {
            const group = createGroupNode(split[1].toUpperCase(), [], parentId)
            const currentGroup = group

            for (let i = 0; i < split.length; i += 2) {
                const part = split[i].trim()
                if (!part) continue
                const child = parseComplexCondition(part, currentGroup.id)
                currentGroup.children.push(child)
            }

            return group
        }

        // Step 3: If no logical operators at this level, check for a single condition
        if (!input.includes('(')) {
            return parseSimpleCondition(input)
        }

        // Step 4: Handle nested groups
        const nestedGroupRegex = /\(([^()]+)\)/g
        let match
        const children = []
        while ((match = nestedGroupRegex.exec(input)) !== null) {
            children.push(parseComplexCondition(match[1], parentId))
        }

        if (children.length > 1) {
            throw new Error(`Ambiguous input: ${input}`)
        }

        return children[0]
    }

    return parseComplexCondition(input)
}
function generateConditionString(node) {
    if (node.type === 'condition') {
        // Nếu là điều kiện, trả về chuỗi điều kiện đơn giản

        return `${node.property} ${node.operator} ${node.value}`
    }
    if (node.type === 'group') {
        // Nếu là nhóm, xây dựng chuỗi cho các nhóm con
        const childrenStrings = node.children.map((child) =>
            generateConditionString(child)
        )
        return `(${childrenStrings.join(` ${node.conditionType} `)})`
    }
    return null
}
const overlapArray = (equal) => (arr1, arr2) => {
    if (typeof equal !== 'function') {
        throw new Error('equal must be a function')
    }
    if (Array.isArray(arr1) && Array.isArray(arr2)) {
        return arr1.filter((x) => arr2.some((y) => equal(x, y)))
    }
    throw new Error('arr1 and arr2 must be an array')
}
export default {
    debounce,
    parseCondition,
    generateConditionString,
    overlapArray,
}
