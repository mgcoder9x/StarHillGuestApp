<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <div slot="header">
                    <strong>{{
                        $t('categories.vehicles.import.header')
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
                                                'categories.vehicles.import.importPlaceholder'
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
                                href="/download/vehicle_import_template.xlsx"
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
                                    type="reset"
                                    variant="outline-secondary"
                                    title="Cancel"
                                    class="btn-120 mb-50"
                                    @click="cancel"
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

        <!-- Import Results Modal -->
        <b-modal
            id="import-results-modal"
            ref="importResultsModal"
            :title="$t('categories.employees.import.resultsTitle')"
            hide-footer
            no-close-on-backdrop
            no-close-on-esc
            size="sm"
            centered
            @hide="closeModalAndRedirect"
        >
            <div class="text-center p-3">
                <!-- Success Icon -->
                <div class="mb-3">
                    <i
                        class="fas fa-check-circle text-success"
                        style="font-size: 3rem"
                    ></i>
                </div>

                <!-- Title -->
                <h5 class="mb-3">
                    {{ $t('categories.employees.import.IMPORT_SUCCESS') }}
                </h5>

                <!-- Results Summary -->
                <div class="mb-4">
                    <div class="d-flex justify-content-between mb-2">
                        <strong
                            >{{
                                $t(
                                    'categories.employees.import.IMPORT_TOTAL_COUNT'
                                )
                            }}:</strong
                        >
                        <span>{{ importResults.total }}</span>
                    </div>
                    <div class="d-flex justify-content-between mb-2">
                        <strong
                            >{{
                                $t(
                                    'categories.employees.import.IMPORT_SUCCESS_COUNT'
                                )
                            }}:</strong
                        >
                        <span class="text-success">{{
                            importResults.successful
                        }}</span>
                    </div>
                    <div
                        v-if="importResults.failed > 0"
                        class="d-flex justify-content-between mb-0"
                    >
                        <strong
                            >{{
                                $t(
                                    'categories.employees.import.IMPORT_ERROR_COUNT'
                                )
                            }}:</strong
                        >
                        <span class="text-danger">{{
                            importResults.failed
                        }}</span>
                    </div>
                </div>

                <!-- Action Buttons -->
                <div>
                    <b-row md="12" class="d-flex justify-content-center gap-2">
                        <b-col md="6">
                            <b-button
                                v-if="importResults.downloadUrl"
                                style="width: 120px"
                                variant="outline-primary"
                                size="sm"
                                class="me-2"
                                @click="downloadReport"
                            >
                                {{ $t('categories.employees.import.Download') }}
                            </b-button>
                        </b-col>
                        <b-col md="6">
                            <b-button
                                style="width: 120px"
                                variant="primary"
                                size="sm"
                                @click="closeModalAndRedirect"
                            >
                                {{ $t('categories.employees.import.Close') }}
                            </b-button>
                        </b-col>
                    </b-row>
                </div>
            </div>
        </b-modal>
    </validation-observer>
</template>

<script>
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
// const isDevEnv = process.env.NODE_ENV == 'development'
export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            file: {
                fileData: null,
                fileName: null,
            },
            importResults: {
                success: false,
                total: 0,
                successful: 0,
                failed: 0,
                downloadUrl: null,
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
            link.href = `/download/vehicle_import_template.xlsx`
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
            const reader = new FileReader()
            reader.onload = (function () {
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
            e.preventDefault()
            try {
                const vm = this
                // Kiểm tra validation
                const success = await this.$refs.rules.validate()
                if (!success) {
                    return
                }
                // Gán ngôn ngữ cho file
                this.file.Lang = this.lang
                // e.preventDefault()
                // Gửi yêu cầu POST
                const res = await this.$services.post(
                    '/vehicles/import',
                    this.file
                )

                // Xóa file sau khi import thành công
                vm.$refs['file-input'].reset()
                vm.file.fileData = null
                vm.file.fileName = null

                // Lưu kết quả import
                this.importResults = {
                    success: true,
                    total: res.data.sumcount || 0,
                    successful: res.data.count || 0,
                    failed: (res.data.sumcount || 0) - (res.data.count || 0),
                    downloadUrl: res.data.url,
                }

                // Hiển thị modal kết quả
                this.$refs.importResultsModal.show()
                this.showSuccessToast({
                    errorCode: 'IMPORT_SUCCESS',
                    count: this.importResults.successful,
                    sumcount: this.importResults.total,
                })
            } catch (error) {
                // Hiển thị thông báo lỗi
                this.showErrorToast(error)
            }
        },

        downloadReport() {
            if (this.importResults.downloadUrl) {
                const a = document.createElement('a')
                const baseURL = process.env.VUE_APP_BASE_URL
                // const urldownload = `${baseURL}/${this.importResults.downloadUrl.replace(/\\/g, '/')}`

                const urldownload = `${baseURL}/api/vehicles/import-result?path=${this.importResults.downloadUrl.replace(/\\/g, '/')}`
                a.setAttribute('href', urldownload)
                a.setAttribute('download', '')
                a.click()

                this.$router.push({ path: '/categories/vehicles/list' })
            }
        },

        closeModalAndRedirect() {
            this.$refs.importResultsModal.hide()

            setTimeout(() => {
                this.$router.push({ path: '/categories/vehicles/list' })
            }, 500)
        },

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

<style scoped>
.stat-box {
    text-align: center;
}

.stat-number {
    font-weight: 600;
    margin-bottom: 0;
}

.stat-label {
    font-size: 0.875rem;
}

.modal-actions {
    display: flex;
    justify-content: center;
    gap: 0.5rem;
}

.bg-light-warning {
    background-color: rgba(255, 193, 7, 0.1) !important;
}

.stat-box {
    text-align: center;
}

.stat-number {
    font-weight: 600;
    margin-bottom: 0;
}

.stat-label {
    font-size: 0.875rem;
}

.modal-actions {
    display: flex;
    justify-content: center;
    gap: 0.5rem;
}

.bg-light-warning {
    background-color: rgba(255, 193, 7, 0.1) !important;
}
</style>
