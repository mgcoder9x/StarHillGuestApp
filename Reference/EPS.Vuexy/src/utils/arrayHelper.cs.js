function getLabel(id, arr) {
    const item = arr.find((x) => x.id.toString() === id?.toString())
    return item ? item.label : ''
}

function getText(id, arr) {
    const item = arr.find((x) => x.id.toString() === id?.toString())
    return item ? item.label : ''
}

export { getLabel, getText }
