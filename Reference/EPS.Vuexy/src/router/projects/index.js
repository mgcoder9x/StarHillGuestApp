// router/projects/index.js
const project = (process.env.VUE_APP_PROJECT || 'hp').toLowerCase()

// Chỉ quét các file *.route.config.js trong thư mục này
const ctx = require.context('./', false, /\.route\.config\.js$/)

const table = ctx.keys().reduce((acc, key) => {
    const name = key
        .replace('./', '')
        .replace('.route.config.js', '')
        .toLowerCase()
    const mod = ctx(key)
    acc[name] = mod.default || mod // lấy default nếu có
    return acc
}, {})
const config = table[project] || table[project]
if (!config) {
    throw new Error(`[routes] Không tìm thấy config cho project="${project}"`)
}

export default config
