<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row cols="1" align-h="center">
                        <b-col md="8">
                            <b-form-group :label="this.$t('Tiêu đề')" label-cols-md="4" label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="Tiêu đề">
                                    <b-form-input v-model="test.title" :state="
                                            errors.length > 0 ? false : null
                                        " />
                                    <small class="text-danger">{{
                                        errors[0]
                                        }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group :label="this.$t('Mô tả')" label-cols-md="4" label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="Mô tả">
                                    <b-form-input v-model="test.description" :state="
                                            errors.length > 0 ? false : null
                                        " />
                                    <small class="text-danger">{{
                                        errors[0]
                                        }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group :label="this.$t('Lớp')" label-for="h-camera-area" label-cols-md="4"
                                label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="TestClass">
                                    <b-form-select v-model="test.classesId" :options="lstClasses" />
                                    <small class="text-danger">{{
                                        errors[0]
                                        }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group :label="this.$t('Ma trận')" label-for="h-camera-area" label-cols-md="4"
                                label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="Ma trận">
                                    <b-form-select v-model="test.configId" :options="lstConfig" />
                                    <small class="text-danger">{{
                                        errors[0]
                                        }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8" class="d-flex align-items-center">
                            <b-col md="6">
                                <b-form-group label-cols-md="8">
                                    <b-form-checkbox v-model="test.isMock">
                                        Ôn tập
                                    </b-form-checkbox>
                                </b-form-group>
                            </b-col>
                            <b-col md="6">
                                <b-form-group label-cols-md="6">
                                    <b-form-checkbox v-model="test.isFixed">
                                        Đề cố định
                                    </b-form-checkbox>
                                </b-form-group>
                            </b-col>
                        </b-col>
                        <b-col md="8" v-if="!test.isMock">
                            <b-form-group label="Ngày bắt đầu hiệu lực" label-cols-md="4" label-class="required">
                                <validation-provider v-slot="{ errors }" :rules="`required`" :name="'Ngày bắt đầu'">
                                    <b-form-datepicker v-model="test.startDate" :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }" reset-button type="datetime" :locale="currentLocale" />
                                    <small class="text-danger">{{
                                        errors[0]
                                        }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8" v-if="!test.isMock">
                            <b-form-group label="Ngày hết hiệu lực" label-cols-md="4" label-class="required">
                                <validation-provider v-slot="{ errors }"
                                    :rules="`required|validateEndDate:${test.startDate}`" :name="'Ngày hết hiệu lực'">
                                    <b-form-datepicker v-model="test.endDate" :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }" reset-button type="datetime" :locale="currentLocale" />
                                    <small class="text-danger">{{
                                        errors[0]
                                        }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group :label="this.$t('Thời gian làm bài(phút)')" label-cols-md="4"
                                label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="Thời gian làm bài">
                                    <b-form-input type="number" v-model="test.duration" :state="
                                            errors.length > 0 ? false : null
                                        " :min="0" @keydown="preventMinusKey" />
                                    <small class="text-danger">{{
                                        errors[0]
                                        }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col class="text-center">
                            <b-button v-if="authorize(['ManageSTDTest'])" type="submit" variant="primary" class="mr-1"
                                @click.prevent="save">
                                <Icon icon="material-symbols:save-outline" class="sm-icon" />
                                <span class="ml-50">
                                    {{ $t('common.button.save') }}
                                </span>
                            </b-button>
                            <b-button class="mr-1" @click="showModal">
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                                <span class="ml-50">
                                    {{ $t('common.button.show') }}
                                </span>
                            </b-button>
                            <b-button :to="{ path: '/test/test/List' }" type="reset" variant="outline-secondary">
                                <Icon icon="mdi:cancel" class="sm-icon" />
                                <span class="ml-50">
                                    {{ $t('common.button.cancel') }}
                                </span>
                            </b-button>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>
        <b-modal ref="viewModal" size="lg" hide-footer title="Danh sách câu hỏi trong bài kiểm tra">
            <template #modal-header>
                <div class="d-flex w-100 justify-content-between align-items-center">
                <h5 class="modal-title mb-0">Danh sách câu hỏi trong bài kiểm tra</h5>
                <b-button size="sm" variant="outline-primary" @click="generateQuesttion">
                    <i class="fas fa-sync-alt"></i> Tải lại
                </b-button>
                </div>
            </template>
            <div v-if="previewQuestions.length > 0">
                <b-list-group>
                    <b-list-group-item v-for="(question, index) in previewQuestions" :key="index">
                        <h5>
                            {{ index + 1 }}.
                            {{ question.content }}
                        </h5>
                        <ul class="pl-3">
                            <li v-for="(answer, idx) in question.listAnswer" :style="{color: answer.isCorrect ? 'green': 'notCorrect'}">
                                {{ answer.content }}
                                <span v-if="answer.isCorrect" class="ml-1">
                                    ✔
                                </span>
                            </li>
                        </ul>
                    </b-list-group-item>
                </b-list-group>
            </div>
            <div v-else class="text-center text-muted">
                Không có câu hỏi nào được chọn từ ma trận.
            </div>
        </b-modal>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import { BButton, BModal } from 'bootstrap-vue'

export default {
    components: {
        BButton,
        BModal,
    },
    mixins: [authorizationMixin],
    data() {
        return {
            test: {
                code: null,
                name: null,
                classesId: null,
                startDate: null,
                endDate: null,
                isMock: false,
                isFixed: false,
                configId: null,
                fixedExamJson: null
            },
            course: {
                startTime: null,
                endTime: null,
            },
            lstClasses: [],
            lstConfig: [],
            previewQuestions: [],
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.loadClasses()
        this.loadConfig()
    },
    methods: {
        showModal() {
            if (!this.test.configId) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: 'Vui lòng chọn ma trận',
                        icon: 'AlertTriangleIcon',
                        variant: 'warning',
                    },
                })
                return
            }
            if (!this.test.classesId) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: 'Vui lòng chọn lớp',
                        icon: 'AlertTriangleIcon',
                        variant: 'warning',
                    },
                })
                return
            }
            if(this.previewQuestions.length == 0){
                this.generateQuesttion()
            }
            this.$refs.viewModal.show()
        },
        async generateQuesttion() {
            this.previewQuestions = [];
            await this.$services
                .post(`/stdQuestion/${this.test.configId}/${this.test.classesId}`)
                .then((res) => {
                    this.previewQuestions = res.data.data
                })
                .catch(() => {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: 'Không thể lấy danh sách câu hỏi',
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                        },
                    })
                })
        },
        preventMinusKey(event) {
            if (event.key === '-' || event.key === 'e' || event.key === '.') {
                event.preventDefault()
            }
        },
        async save() {
            var vm = this
            if(vm.test.isFixed){
                if(vm.previewQuestions.length == 0){
                    await vm.generateQuesttion()
                }
                vm.test.fixedExamJson = JSON.stringify(vm.previewQuestions)
            }
            else{
                vm.test.fixedExamJson = null
            }
            await this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .post('/stdTest', vm.test)
                        .then((response) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Create'),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                            this.$router.push({
                                path: '/test/test/List',
                            })
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${this.$t(error.message)}`,
                                },
                            })
                        })
                }
            })
        },
        loadClasses() {
            this.$services.get('/lookup/classes').then((response) => {
                this.lstClasses = response.data.data
            })
        },
        loadConfig() {
            this.$services.get('/lookup/config').then((response) => {
                this.lstConfig = response.data.data
            })
        },
    },
}
</script>

<style lang="scss"></style>
