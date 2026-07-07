const path = require('path')
const TerserPlugin = require('terser-webpack-plugin')

const isProd = process.env.NODE_ENV === 'production'

module.exports = {
    lintOnSave: false, // Tắt ESLint
    publicPath: '/',
    productionSourceMap: false, // Không tạo .map files trong production
    css: {
        loaderOptions: {
            sass: {
                sassOptions: {
                    includePaths: ['./node_modules', './src/assets'],
                },
            },
        },
    },
    configureWebpack: (config) => {
        config.resolve = {
            ...config.resolve,
            alias: {
                vue$: path.resolve(__dirname, 'node_modules/vue/dist/vue.esm.js'),
                '@themeConfig': path.resolve(__dirname, 'themeConfig.js'),
                '@core': path.resolve(__dirname, 'src/@core'),
                '@': path.resolve(__dirname, 'src'),
                '@validations': path.resolve(
                    __dirname,
                    'src/@core/utils/validations/validations.js'
                ),
                '@axios': path.resolve(__dirname, 'src/libs/axios'),
            },
        },

        config.performance = {
            maxEntrypointSize: 512000,
            maxAssetSize: 512000,
        }

        if (isProd) {
            config.optimization = {
                ...config.optimization,
                minimizer: [
                    new TerserPlugin({
                        terserOptions: {
                            compress: {
                                drop_console: true,
                                drop_debugger: true,
                                pure_funcs: ['console.log', 'console.warn', 'console.info'],
                            },
                            output: {
                                comments: false,
                            },
                        },
                        extractComments: false,
                    }),
                ],
                splitChunks: {
                    chunks: 'all',
                    maxInitialRequests: 10,
                    minSize: 20000,
                    cacheGroups: {
                        vue: {
                            test: /[\\/]node_modules[\\/](vue|vue-router|vuex)[\\/]/,
                            name: 'chunk-vue',
                            priority: 30,
                        },
                        bootstrap: {
                            test: /[\\/]node_modules[\\/](bootstrap|bootstrap-vue)[\\/]/,
                            name: 'chunk-bootstrap',
                            priority: 25,
                        },
                        charts: {
                            test: /[\\/]node_modules[\\/](apexcharts|vue-apexcharts|echarts|vue-echarts|chart\.js|vue-chartjs)[\\/]/,
                            name: 'chunk-charts',
                            priority: 20,
                        },
                        vendors: {
                            test: /[\\/]node_modules[\\/]/,
                            name: 'chunk-vendors',
                            priority: 10,
                        },
                    },
                },
            }
        }
    },
    chainWebpack: (config) => {
        config.module
            .rule('vue')
            .use('vue-loader')
            .loader('vue-loader')
            .tap((options) => {
                // eslint-disable-next-line no-param-reassign
                options.transformAssetUrls = {
                    img: 'src',
                    image: 'xlink:href',
                    'b-avatar': 'src',
                    'b-img': 'src',
                    'b-img-lazy': ['src', 'blank-src'],
                    'b-card': 'img-src',
                    'b-card-img': 'src',
                    'b-card-img-lazy': ['src', 'blank-src'],
                    'b-carousel-slide': 'img-src',
                    'b-embed': 'src',
                }
                return options
            })

        // Xóa prefetch/preload để không load trước các chunk không cần thiết
        if (isProd) {
            config.plugins.delete('prefetch')
            config.plugins.delete('preload')
        }
    },
    transpileDependencies: ['vue-echarts', 'resize-detector'],
}