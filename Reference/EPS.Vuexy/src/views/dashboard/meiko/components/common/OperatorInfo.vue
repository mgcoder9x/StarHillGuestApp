<template>
    <div class="d-flex flex-wrap">
        <div
            v-for="(operator, idx) in operators"
            :key="operator.personNo"
            class="operator-badge"
        >
            <Icon icon="fa7-regular:user" width="15" height="15" />
            <strong>NT{{ idx + 1 }}:</strong>
            ({{ operator.personNo }})
        </div>
    </div>
</template>

<script>
export default {
    name: 'OperatorInfo',
    props: {
        operators: {
            type: Array,
            required: true,
        },
    },
}
</script>
