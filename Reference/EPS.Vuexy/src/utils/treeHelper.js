function sortChildren(data) {
    return data.map((item) => {
        const newItem = { ...item }
        if (Array.isArray(newItem._children)) {
            newItem._children = sortChildren(newItem._children) // Gọi đệ quy
        }
        if (newItem._children) {
            newItem._children = newItem._children.sort(
                (a, b) => a.treeIndex - b.treeIndex
            ) // Sắp xếp theo treeIndex
        }
        return newItem
    })
}

function removeEmptyChildren(data) {
    return data.map((item) => {
        const newItem = { ...item } // Tạo bản sao để tránh thay đổi trực tiếp
        if (
            newItem.isLeaf ||
            (Array.isArray(newItem.children) && newItem.children.length === 0)
        ) {
            delete newItem.children // Xóa thuộc tính children nếu rỗng
        } else if (Array.isArray(newItem.children)) {
            newItem.children = removeEmptyChildren(newItem.children) // Gọi đệ quy
        }
        return newItem
    })
}

// Map text/name/deviceName → label đệ quy cho tree-select
function mapTextToLabel(nodes) {
    if (!Array.isArray(nodes)) return []
    return nodes.map((node) => {
        const newNode = { ...node }
        // Map text/name sang label
        if (newNode.text && !newNode.label) {
            newNode.label = newNode.text
        } else if (newNode.name && !newNode.label) {
            newNode.label = newNode.name
        } else if (newNode.deviceName && !newNode.label) {
            newNode.label = newNode.deviceName
        } else if (!newNode.label && newNode.id) {
            newNode.label = String(newNode.id)
        }
        // Xử lý đệ quy cho children
        if (Array.isArray(newNode.children) && newNode.children.length > 0) {
            newNode.children = mapTextToLabel(newNode.children)
        }
        return newNode
    })
}

// Sử dụng:
export default { removeEmptyChildren, sortChildren, mapTextToLabel }
