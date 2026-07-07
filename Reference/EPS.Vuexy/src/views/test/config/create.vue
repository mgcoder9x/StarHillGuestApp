<!-- eslint-disable -->
<template>
    <validation-observer ref="observer">
        <b-card>
            <b-card-body>
                <b-form @submit.prevent="onSubmit">
                    <validation-provider
                        rules="required"
                        v-slot="{ errors }"
                        name="Mã cấu hình"
                    >
                        <b-form-group label="Mã cấu hình">
                            <b-form-input
                                v-model="newTestConfig.Code"
                                required
                            />
                            <small class="text-danger">{{ errors[0] }}</small>
                        </b-form-group>
                    </validation-provider>
                    <validation-provider
                        rules="required"
                        v-slot="{ errors }"
                        name="Tên cấu hình"
                    >
                        <b-form-group label="Tên cấu hình">
                            <b-form-input
                                v-model="newTestConfig.Name"
                                required
                            />
                            <small class="text-danger">{{ errors[0] }}</small>
                        </b-form-group>
                    </validation-provider>
                    <b-button
                        @click="addTestConfigRule"
                        variant="info"
                        class="mt-3 float-right"
                    >
                        {{ $t('Thêm câu hỏi') }}
                    </b-button>

                    <b-table
                        :items="newTestConfig.rules"
                        :fields="tableFields"
                        responsive="sm"
                        striped
                        hover
                    >
                        <template v-slot:cell(actions)="row">
                            <b-button
                                @click="removeTestConfigRule(row.index)"
                                variant="danger"
                                size="sm"
                            >
                                {{ $t('Xóa') }}
                            </b-button>
                        </template>
                        <template v-slot:cell(questionType)="row">
                            <validation-provider
                                rules="required"
                                v-slot="{ errors }"
                                name="Loại câu hỏi"
                            >
                                <b-form-select
                                    v-model="row.item.questionType"
                                    :options="questionTypes"
                                    required
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </template>
                        <template v-slot:cell(quantity)="row">
                            <validation-provider
                                rules="required"
                                v-slot="{ errors }"
                                name="Số lượng câu"
                            >
                                <b-form-input
                                    v-model="row.item.quantity"
                                    type="number"
                                    required
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </template>
                        <template v-slot:cell(level)="row">
                            <validation-provider
                                rules="required"
                                v-slot="{ errors }"
                                name="Cấp độ"
                            >
                                <b-form-select
                                    v-model="row.item.level"
                                    :options="levels"
                                    required
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </template>
                        <template v-slot:cell(type)="row">
                            <validation-provider
                                rules="required"
                                v-slot="{ errors }"
                                name="Loại câu"
                            >
                                <b-form-select
                                    v-model="row.item.type"
                                    :options="types"
                                    required
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </template>
                        <template v-slot:cell(knockoutQuestion)="row">
                            <b-form-checkbox v-model="row.item.knockoutQuestion">
                            </b-form-checkbox>
                        </template>
                    </b-table>

                    <!-- Buttons for Save and Cancel (centered at the bottom) -->
                    <b-row class="justify-conten-center">
                        <b-col cols="12" class="text-center">
                            <button
                                class="btn mx-50 mt-2 mb-50 btn-120 btn-primary"
                                v-if="authorize(['ManageSTDConfig'])"
                                type="submit"
                            >
                                {{ $t('common.button.save') }}
                            </button>
                            <button
                                class="btn btn-120 mb-50 btn-outline-secondary mt-2"
                                @click="back()"
                            >
                                {{ $t('common.button.cancel') }}
                            </button>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>
<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    mixins: [authorizationMixin],
    data() {
        return {
            newTestConfig: {
                Code: '',
                Name: '',
                rules: [], // Array to store rules dynamically
            },
            newTestConfigRule: {
                questionType: null,
                quantity: null,
                level: null,
                type: null,
                knockoutQuestion: false
            },
            questionTypes: [
                { value: 1, text: 'Thông dụng' },
                { value: 2, text: 'Vận dụng' },
                { value: 3, text: 'Hiểu biết' },
            ],
            levels: Array.from({ length: 10 }, (_, i) => ({
                value: i + 1,
                text: `Level ${i + 1}`,
            })),
            types: [
                { value: 1, text: 'Một câu đúng' },
                { value: 2, text: 'Nhiều câu đúng' },
            ],
            tableFields: [
                { key: 'questionType', label: 'Loại câu hỏi' },
                { key: 'quantity', label: 'Số lượng câu' },
                { key: 'level', label: 'Cấp độ' },
                { key: 'type', label: 'Loại câu' },
                { key: 'knockoutQuestion', label: 'Câu hỏi điểm liệt' },
                { key: 'actions', label: 'Thao tác' },
            ],
        }
    },
    methods: {
        addTestConfigRule() {
            this.newTestConfig.rules.push({ ...this.newTestConfigRule })
            this.resetNewTestConfigRule()
        },
        removeTestConfigRule(index) {
            this.newTestConfig.rules.splice(index, 1)
        },
        async onSubmit() {
            let validate = await this.$refs.observer.validate()
            if (!validate) return
            this.$services
                .post('/stdconfigs', this.newTestConfig)
                .then(() => {
                    this.notification(true)
                    this.$router.push({ name: 'test-config-list' })
                })
                .catch((error) => {
                    this.notification(false, error.message)
                })
        },
        resetNewTestConfigRule() {
            this.newTestConfigRule = {
                questionType: null,
                quantity: null,
                level: null,
                type: null,
                knockoutQuestion: false
            }
        },
        notification(isSuccess, message) {
            this.$toast({
                component: ToastificationContent,
                position: 'bottom-right',
                props: {
                    title: isSuccess ? this.$t('Success.Create') : 'ERROR',
                    icon: 'CheckIcon',
                    variant: isSuccess ? 'success' : 'danger',
                    text: this.$t(message),
                },
            })
        },
        back() {
            this.$router.push({ name: 'test-config-list' })
        },
    },
}
</script>
