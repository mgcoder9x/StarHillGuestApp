<!-- eslint-disable -->
<template>
    <validation-observer ref="observer">
        <b-card>
            <b-card-body>
                <b-form @submit.prevent="onSubmit">
                    <b-row cols="1" align-h="center">
                        <b-col cols="8">
                            <b-row>
                                <b-col>
                                    <validation-provider
                                        rules="required"
                                        v-slot="{ errors }"
                                        name="Cảnh báo"
                                    >
                                        <b-form-group
                                            :label="
                                                $t(
                                                    'categories.waterWarning.Field.Name'
                                                )
                                            "
                                            label-class="required"
                                            label-cols-md="4"
                                        >
                                            <b-form-input
                                                v-model.trim="Warning.name"
                                            ></b-form-input>
                                            <small class="validate-massage">
                                                {{ errors[0] }}
                                            </small>
                                        </b-form-group>
                                    </validation-provider>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <validation-provider
                                        :rules="`decimal:3|required`"
                                        v-slot="{ errors }"
                                        name="Giá trị lớn nhất"
                                    >
                                        <b-form-group
                                            :label="
                                                $t(
                                                    'categories.waterWarning.Field.MaxValue'
                                                )
                                            "
                                            label-class="required"
                                            label-cols-md="4"
                                        >
                                            <b-form-input
                                                v-model.trim="Warning.maxvalue"
                                            ></b-form-input>
                                            <small class="validate-massage">
                                                {{ errors[0] }}
                                            </small>
                                        </b-form-group>
                                    </validation-provider>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <validation-provider
                                        :rules="`decimal:3|required|minValue:${Warning.maxvalue}`"
                                        v-slot="{ errors }"
                                        name="Giá trị nhỏ nhất"
                                    >
                                        <b-form-group
                                            :label="
                                                $t(
                                                    'categories.waterWarning.Field.MinValue'
                                                )
                                            "
                                            label-class="required"
                                            label-cols-md="4"
                                        >
                                            <b-form-input
                                                v-model.trim="Warning.minvalue"
                                            ></b-form-input>
                                            <small class="validate-massage">
                                                {{ errors[0] }}
                                            </small>
                                        </b-form-group>
                                    </validation-provider>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.waterWarning.Field.Description'
                                            )
                                        "
                                        label-cols-md="4"
                                    >
                                        <b-form-textarea
                                            v-model.trim="Warning.description"
                                            rows="3"
                                            max-rows="5"
                                        ></b-form-textarea>
                                    </b-form-group>
                                </b-col>
                            </b-row>
                        </b-col>
                    </b-row>
                    <b-row class="justify-conten-center">
                        <b-col cols="12" class="text-center">
                            <button
                                class="btn mx-50 mt-2 mb-50 btn-120 btn-primary"
                                v-if="authorize(['ManageWaterWarning'])"
                                click="submit"
                            >
                                {{
                                    $t(
                                        'categories.waterWarning.Label.button.create'
                                    )
                                }}
                            </button>
                            <button
                                class="btn btn-120 mb-50 btn-outline-secondary mt-2"
                                @click="back()"
                            >
                                {{
                                    $t(
                                        'categories.waterWarning.Label.button.cancel'
                                    )
                                }}
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

/*eslint-disable*/
export default {
    mixins: [authorizationMixin],
    data() {
        return {
            Warning: {
                name: null,
                maxvalue: null,
                minvalue: null,
                description: null,
            },
        }
    },
    methods: {
        async onSubmit() {
            //validate
            let validate = await this.$refs.observer.validate()
            if (validate === false) {
                return
            }

            this.$services
                .post('/water-warning', this.Warning)
                .then((response) => {
                    this.notification(true)
                    this.$router.push({ name: 'water-warning-list' })
                })
                .catch((error) => {
                    this.notification(false, error.message)
                })
        },

        notification(isSuccess, message) {
            if (isSuccess) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'bottom-right',
                    props: {
                        title: this.$t(
                            'categories.waterWarning.Label.notification.success'
                        ),
                        icon: 'CheckIcon',
                        variant: 'success',
                    },
                })
            } else {
                this.$toast({
                    component: ToastificationContent,
                    position: 'bottom-right',
                    props: {
                        title: this.$t(
                            'categories.waterWarning.Label.notification.error'
                        ),
                        icon: 'CheckIcon',
                        variant: 'danger',
                        text: this.$t(message),
                    },
                })
            }
        },

        back() {
            this.$router.push({ name: 'water-warning-list' })
        },
    },
}
</script>
<style>
.validate-massage {
    color: red;
}
</style>
