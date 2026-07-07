<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <div slot="header">
                    <strong>{{
                        $t('categories.employees.import.header')
                    }}</strong>
                </div>
                <b-form @submit="onSubmit">
                    <b-row>
                        <b-col md="10">
                            <b-form-group
                                :label="
                                    $t('categories.employees.import.import')
                                "
                                :label-cols="2"
                                :horizontal="true"
                                label-align-md="left"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.employees.import.importRequire'
                                        )
                                    "
                                >
                                    <b-form-file
                                        id="file-input"
                                        ref="file-input"
                                        accept=".xlsx"
                                        :placeholder="
                                            $t(
                                                'categories.employees.import.importPlaceholder'
                                            )
                                        "
                                        @change="onChange($event)"
                                    ></b-form-file>
                                    <b-form-input
                                        v-model="file.fileData"
                                        type="text"
                                        style="display: none"
                                        :state="errors.length ? false : null"
                                    >
                                    </b-form-input>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="2">
                            <a
                                v-if="lang == 'vi'"
                                class="btn btn-success"
                                style="float: right; color: white"
                                @click="clickTem"
                                >{{
                                    $t(
                                        'categories.employees.import.importSample'
                                    )
                                }}</a
                            >
                            <a
                                v-if="lang == 'en'"
                                class="btn btn-success"
                                style="float: right; color: white"
                                href="download/template_Employee.xlsx"
                                >{{
                                    $t(
                                        'categories.employees.import.importSample'
                                    )
                                }}</a
                            >
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="6" offset="3">
                            <b-form-group :label-cols="4" :horizontal="true">
                                <b-button
                                    v-if="authorize(['ManageEmployee'])"
                                    type="submit"
                                    variant="primary"
                                    title="Save"
                                    class="mx-50 mb-50 btn-120"
                                >
                                    {{ $t('common.button.save') }}
                                </b-button>
                                <b-button
                                    @click="cancel"
                                    type="reset"
                                    variant="outline-secondary"
                                    title="Cancel"
                                    class="btn-120 mb-50"
                                >
                                    {{ $t('common.button.cancel') }}
                                </b-button>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="12">
                            <ImageUpload multiple_files />
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>
<script>
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
const isDevEnv = process.env.NODE_ENV == 'development'
export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            file: {
                fileData: null,
                fileName: null,
            },
        }
    },
    computed: {
        lang() {
            const language =
                localStorage.getItem('lang') == null
                    ? 'vi'
                    : localStorage.getItem('lang')
            return language
        },
    },
    created() {},
    methods: {
        clickTem() {
            const link = document.createElement('a')
            const baseURL = isDevEnv
                ? 'http://localhost:8080/'
                : 'http://192.168.1.210:42032/'
            link.href = `${baseURL}download/template_NhanVien.xlsx`
            link.click()
            document.body.removeChild(link)
        },
        onChange(evt) {
            const vm = this
            const f = evt.target.files[0] // FileList object
            const acceptExtensions = ['xlsx', 'XLSX']
            const fileExtensions = /[.]/.exec(f.name)
                ? /[^.]+$/.exec(f.name)
                : undefined
            if (
                fileExtensions &&
                !acceptExtensions.includes(fileExtensions[0])
            ) {
                this.$refs['file-input'].reset()
                alert(this.t('categories.employees.import.fileInputXlxs'))
                return
            }
            if (f.size > 5000000) {
                this.$refs['file-input'].reset()
                alert(this.t('categories.employees.import.fileInput50Mb'))
                return
            }
            var reader = new FileReader()
            reader.onload = (function (theFile) {
                return function (e) {
                    const binaryData = e.target.result
                    vm.file.fileData = window.btoa(binaryData)
                }
            })(f)
            reader.readAsBinaryString(f, vm)
            vm.file.fileName = f.name
        },
        cancel() {
            this.$router.push({ path: '/categories/employees/list' })
        },
        async onSubmit(e) {
            try {
                var vm = this;
                // Kiểm tra validation
                const success = await this.$refs.rules.validate()
                if (!success) {
                    return
                }
                // Gán ngôn ngữ cho file
                this.file.Lang = this.lang
                e.preventDefault()
                // Gửi yêu cầu POST
                const res = await this.$services.post(
                    '/employees/import',
                    this.file
                )
                debugger
                // Xóa file sau khi import thành công
                vm.$refs['file-input'].reset()
                vm.file.fileData = null
                vm.file.fileName = null
                // Tạo và kích hoạt link download
                const a = document.createElement('a')
                let baseURL = isDevEnv
                    ? 'http://localhost:8080/'
                    : 'http://192.168.1.210:42032/'
                let urldowloat = baseURL + res.data.url.replace(/\\/g, '/');
                a.setAttribute('href', urldowloat)
                a.click()
                // Hiển thị thông báo thành công
                let tong = res.data.sumcount;
                let thanhcong = res.data.count;
                this.showSuccessToast({
                    errorCode: 'IMPORT_SUCCESS',
                    count: thanhcong,
                    sumcount: tong,
                })
                debugger
                // Điều hướng đến trang danh sách
                this.$router.push({ path: '/categories/employees/list' })
            } catch (error) {
                // Hiển thị thông báo lỗi
                this.showErrorToast(error)
            }
        },
        // async onSubmit(e) {
        //     try {
        //         var vm = this;
        //         // Kiểm tra validation
        //         const success = await this.$refs.rules.validate()
        //         if (!success) {
        //             return
        //         }
        //         debugger
        //         // Gán ngôn ngữ cho file
        //         this.file.Lang = this.lang
        //         e.preventDefault()
        //         // Gửi yêu cầu POST
        //         this.$services
        //             .post('/employees/import', this.file)
        //             .done((re) => {
        //                 debugger
        //                 vm.$refs['file-input'].reset()
        //                 vm.file.fileData = null
        //                 vm.file.fileName = null
        //                 // Tạo và kích hoạt link download
        //                 let a = document.createElement('a')
        //                 a.setAttribute('href', re.url)
        //                 a.click()
        //                 // Hiển thị thông báo thành công
        //                 this.showSuccessToast({
        //                     errorCode: 'IMPORT_SUCCESS',
        //                     count: re.count,
        //                     sumcount: re.sumcount,
        //                 })
        //             })
        //         // Điều hướng đến trang danh sách
        //         this.$router.push({ path: '/categories/employees/list' })
        //     } catch (error) {
        //         // Hiển thị thông báo lỗi
        //         this.showErrorToast(error)
        //     }
        // },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(
                        `categories.employees.error.${data.errorCode}`
                    ),
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
        showErrorToast(error) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: 'Error',
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(
                        `categories.employees.error.${error.response.data.errorCode}`
                    ),
                },
            })
        },
    },
}
</script>
<style scoped></style>
