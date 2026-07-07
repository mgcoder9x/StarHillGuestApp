<template>
    <validation-observer ref="observer">
        <b-card>
            <b-card-body>
                <b-form @submit.prevent="onSubmit">
                    <!-- Input for Mã cấu hình -->
                    <validation-provider
                        rules="required"
                        v-slot="{ errors }"
                        name="Mã cấu hình"
                    >
                        <b-form-group label="Mã cấu hình">
                            <b-form-input
                                v-model="newTestConfig.code"
                                required
                                :disabled="!editing"
                            />
                            <small class="text-danger">{{ errors[0] }}</small>
                        </b-form-group>
                    </validation-provider>

                    <!-- Input for Tên cấu hình -->
                    <validation-provider
                        rules="required"
                        v-slot="{ errors }"
                        name="Tên cấu hình"
                    >
                        <b-form-group label="Tên cấu hình">
                            <b-form-input
                                v-model="newTestConfig.name"
                                required
                                :disabled="!editing"
                            />
                            <small class="text-danger">{{ errors[0] }}</small>
                        </b-form-group>
                    </validation-provider>

                    <!-- Button to add test config rules -->
                    <b-button
                        @click="addTestConfigRule"
                        variant="info"
                        class="mt-3 float-right"
                        :disabled="!editing"
                    >
                        {{ $t('Thêm câu hỏi') }}
                    </b-button>

                    <!-- Table for test config rules -->
                    <b-table
                        :items="newTestConfig.rules"
                        :fields="tableFields"
                        responsive="sm"
                        striped
                        hover
                    >
                        <!-- Action Buttons: Edit/Delete -->
                        <template v-slot:cell(actions)="row">
                            <b-button
                                @click="removeTestConfigRule(row.index)"
                                variant="danger"
                                size="sm"
                                :disabled="!editing"
                            >
                                {{ $t('Xóa') }}
                            </b-button>
                        </template>

                        <!-- Other table fields with validation -->
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
                                    :disabled="!editing"
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
                                    :disabled="!editing"
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
                                    :disabled="!editing"
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
                                    :disabled="!editing"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </template>
                        <template v-slot:cell(knockoutQuestion)="row">
                            <b-form-checkbox v-model="row.item.knockoutQuestion" :disabled="!editing">
                            </b-form-checkbox>
                        </template>
                    </b-table>

                    <!-- Save and Cancel Buttons -->
                    <b-row class="justify-conten-center">
                        <b-col cols="12" class="text-center">
                            <Transition mode="out-in">
                                <button
                                    v-if="!editing"
                                    key="first"
                                    class="btn mx-50 mt-2 mb-50 btn-120 btn-primary"
                                    @click.prevent="editing = true"
                                >
                                    {{
                                        $t(
                                            'categories.waterWarning.Label.button.update'
                                        )
                                    }}
                                </button>
                                <button
                                    v-else
                                    key="second"
                                    class="btn mx-50 mt-2 mb-50 btn-120 btn-primary"
                                    @click.prevent="onSubmit()"
                                >
                                    {{
                                        $t(
                                            'categories.waterWarning.Label.button.save'
                                        )
                                    }}
                                </button>
                            </Transition>
                            <Transition mode="out-in">
                                <button
                                    v-if="!editing"
                                    key="first"
                                    class="btn btn-120 mb-50 btn-outline-secondary mt-2"
                                    @click.prevent="back()"
                                >
                                    {{
                                        $t(
                                            'categories.waterWarning.Label.button.back'
                                        )
                                    }}
                                </button>
                                <button
                                    v-else
                                    key="first"
                                    class="btn btn-120 mb-50 btn-outline-secondary mt-2"
                                    @click.prevent="cancel()"
                                >
                                    {{
                                        $t(
                                            'categories.waterWarning.Label.button.cancel'
                                        )
                                    }}
                                </button>
                            </Transition>
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
            editing: false, // Initialize the editing state to false
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
    mounted() {
        debugger
        const id = this.$route.params.configId // Assuming the ID is passed as a URL parameter
        if (id) {
            this.loadData(id) // Fetch data based on the ID
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
        configId() {
            debugger
            return this.$route.params.configId
        },
    },
    methods: {
        // Fetch the data based on the provided ID
        async loadData(id) {
            try {
                debugger
                const response = await this.$services.get(`/stdconfigs/${id}`)
                this.newTestConfig = response.data.data // Populate the form with the response data
            } catch (error) {
                this.notification(false, error.message)
            }
        },
        addTestConfigRule() {
            this.newTestConfig.rules.push({ ...this.newTestConfigRule })
            this.resetNewTestConfigRule()
        },
        removeTestConfigRule(index) {
            this.newTestConfig.rules.splice(index, 1)
        },
        async onSubmit() {
            debugger
            let validate = await this.$refs.observer.validate()
            if (!validate) return
            this.$services
                .put(`/stdconfigs/${this.configId}`, this.newTestConfig)
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
        cancel() {
            this.editing = false
            this.loadData(this.$route.params.configId) // Reset form data on cancel
        },
    },
}
</script>
