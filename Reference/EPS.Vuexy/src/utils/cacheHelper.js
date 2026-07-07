function clearStorage(key) {
    localStorage.removeItem(key)
}

function setStorage(key, data, experiedTime = 10) {
    const expiry = new Date().getTime() + experiedTime * 1000
    localStorage.setItem(
        key,
        JSON.stringify({
            searchForm: data,
            expiry,
        })
    )
}
function getStorage(key) {
    const storage = localStorage.getItem(key)
    if (storage) {
        const data = JSON.parse(storage)
        const now = new Date().getTime()

        if (data.expiry && now > data.expiry) {
            clearStorage(key)
            return null
        }
        return data.searchForm
    }
    return null
}

export { setStorage, getStorage, clearStorage }
