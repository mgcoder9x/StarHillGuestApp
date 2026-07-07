/**
 * Tạo function để chọn menu items
 * @param {Object} allItems - Tất cả items có thể có { areas: {...}, devices: {...} }
 * @param {Object} ParentInfo - Thông tin menu cha { title: '...', icon: '...' }
 * @returns {Function} - Function để chọn items
 */
export function createNavItemFactory(allItems, ParentInfo) {
    // Trả về 1 function
    return function selectItems(choices = {}) {
        // Nếu không truyền gì -> trả về tất cả
        if (Object.keys(choices).length === 0) {
            return [
                {
                    ...ParentInfo,
                    children: Object.values(allItems), // Tất cả items
                },
            ]
        }

        // Nếu có truyền choices -> chỉ lấy những cái = true
        const selectedItems = []

        // Duyệt qua từng lựa chọn
        for (const itemName in choices) {
            if (choices[itemName] === true) {
                // Nếu = true
                if (allItems[itemName]) {
                    // Và item tồn tại
                    selectedItems.push(allItems[itemName]) // Thì thêm vào
                }
            }
        }

        // Trả về menu với items đã chọn
        return [
            {
                ...ParentInfo,
                children: selectedItems,
            },
        ]
    }
}
