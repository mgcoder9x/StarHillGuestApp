const operators = [
    {
        title: '=',
        icon: 'material-symbols:equal',
        id: '=',
    },
    {
        title: '!=',
        icon: 'ic:baseline-not-equal',
        id: '!=',
    },
    {
        title: '>',
        icon: 'ic:baseline-greater-than',
        id: '>',
    },
    {
        title: '>=',
        icon: 'ic:baseline-greater-than-equal',
        id: '>=',
    },
    {
        title: '<',
        icon: 'ic:baseline-less-than',
        id: '<',
    },
    {
        title: '<=',
        icon: 'ic:baseline-less-than-equal',
        id: '<=',
    },
    {
        title: 'NI',
        icon: 'ph:not-member-of-thin',
        id: 'not in',
    },
    {
        title: 'IN',
        icon: 'ph:member-of-thin',
        id: 'in',
    },
    {
        title: 'SW',
        icon: '',
        id: 'STARTSWITH',
    },
    {
        title: 'EW',
        icon: '',
        id: 'ENDSWITH',
    },
    {
        title: 'LIKE',
        icon: '',
        id: 'LIKE',
    },
]
const conditions = [
    {
        title: 'AND',
        id: 'AND',
    },
    {
        title: 'OR',
        id: 'OR',
    },
]
const types = [
    {
        title: 'Warning.Condition.Common.GroupType',
        id: 'group',
    },
    {
        title: 'Warning.Condition.Common.Condition',
        id: 'condition',
    },
]
const OBJECT_TYPES = {
    DATETIME: 'datetime',
    STRING: 'string',
    NUMBER: 'int32',
    BOOLEAN: 'boolean',
    OBJECT: 'object',
    ARRAY: 'array',
}

export default { operators, conditions, types, OBJECT_TYPES }
