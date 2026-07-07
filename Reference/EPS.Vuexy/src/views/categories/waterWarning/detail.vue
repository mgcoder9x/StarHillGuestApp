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
                                        v-slot="{ errors }"
                                        rules="required"
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
                                                :readonly="!editing"
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
                                        v-slot="{ errors }"
                                        :rules="`decimal:3|required`"
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
                                                v-model.trim="Warning.maxValue"
                                                :readonly="!editing"
                                                type="number"
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
                                        v-slot="{ errors }"
                                        :rules="`decimal:3|required|minValue:${Warning.maxValue}`"
                                        name="Giá trị nhỏ nhất"
                                    >
                                        <b-form-group
                                            :label="
                                                $t(
                                                    'categories.waterWarning.Field.MinValue'
                                                )
                                            "
                                            label-cols-md="4"
                                            label-class="required"
                                        >
                                            <b-form-input
                                                v-model.trim="Warning.minValue"
                                                type="number"
                                                :readonly="!editing"
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
                                            this.$t(
                                                'categories.waterWarning.Field.Description'
                                            )
                                        "
                                        label-cols-md="4"
                                    >
                                        <b-form-textarea
                                            v-model.trim="Warning.description"
                                            rows="3"
                                            max-rows="5"
                                            :readonly="!editing"
                                        ></b-form-textarea>
                                    </b-form-group>
                                </b-col>
                            </b-row>
                        </b-col>
                    </b-row>
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
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    data() {
        return {
            editing: false,
            edited: false,
            Warning: [],
        }
    },
    async created() {
        this.loadData()
    },
    methods: {
        async onSubmit() {
			// validate
			const validate = await this.$refs.observer.validate()
			if (validate === false) {
				return
			}

			this.$services
				.put(`/water-warning/${this.$route.params.id}`, this.Warning)
				.then((res) => {
					//this.$router.push({ name: "water-warning-list" })
					//this.notification(true)
					this.$toast({
						component: ToastificationContent,
						position: 'top-right',
						props: {
							title: this.$t('Success.Update'),
							icon: 'CheckIcon',
							variant: 'success',
						},
					})
					this.cancel()
				})
				.catch((err) => {
					// this.notification(false, err.response.data.message)
					this.$toast({
						component: ToastificationContent,
						position: 'top-right',
						props: {
							title: this.$t('Error.Error'),
							icon: 'AlertTriangleIcon',
							variant: 'danger',
							text: `${this.$t(
								err.message
							)}`,
						},
					})
				})

		},
        cancel() {
            this.editing = false
            this.loadData()
        },
        back() {
            this.$router.push({ name: 'water-warning-list' })
        },
        loadData() {
            this.$services
                .get(`/water-warning/${this.$route.params.id}`)
                .then((res) => {
                    this.Warning = res.data.data
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
                        text: message,
                    },
                })
            }
        },
    },
}
</script>
<style>
.validate-massage {
    color: red;
}
</style>
