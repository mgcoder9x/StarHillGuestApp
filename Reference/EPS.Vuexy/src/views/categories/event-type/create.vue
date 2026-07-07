<template>
    <div>
        <b-card :title="$t('EventType.Detail.TitleCreate')">
            <validation-observer
                ref="formRules"
                v-slot="{ handleSubmit, invalid }"
            >
                <b-form @submit.prevent="handleSubmit(onSubmit)">
                    <b-row class="justify-content-center mt-2 mb-2">
                        <!-- Id -->
                        <b-col md="8">
                            <b-form-group
                                :label="$t('EventType.Detail.Form.Id')"
                                label-for="eventTypeId"
                                label-cols-md="3"
                                class="mb-50 mb-md-1"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :name="$t('EventType.Detail.Form.Id')"
                                    rules="required|min_value:1|integer"
                                >
                                    <b-form-input
                                        id="eventTypeId"
                                        v-model.number="form.id"
                                        type="number"
                                        min="1"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :placeholder="
                                            $t(
                                                'EventType.Detail.Form.IdPlaceholder'
                                            )
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Name -->
                        <b-col md="8">
                            <b-form-group
                                :label="$t('EventType.Detail.Form.Name')"
                                label-for="eventTypeName"
                                label-cols-md="3"
                                class="mb-50 mb-md-1"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :name="$t('EventType.Detail.Form.Name')"
                                    rules="required|max:250"
                                >
                                    <b-form-input
                                        id="eventTypeName"
                                        v-model.trim="form.name"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :placeholder="
                                            $t(
                                                'EventType.Detail.Form.NamePlaceholder'
                                            )
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <div class="d-flex justify-content-center mt-2 gap-1">
                        <b-button
                            type="submit"
                            variant="primary"
                            :disabled="isSaving"
                        >
                            <b-spinner v-if="isSaving" small class="mr-50" />
                            {{ $t('Button.Create') }}
                        </b-button>
                        <b-button
                            variant="outline-secondary"
                            class="mr-1"
                            @click="$router.push({ name: 'event-type' })"
                        >
                            {{ $t('Button.Cancel') }}
                        </b-button>
                    </div>
                </b-form>
            </validation-observer>
        </b-card>
    </div>
</template>

<script>
/* eslint-disable */
import { ValidationObserver, ValidationProvider } from 'vee-validate'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import {
    BCard,
    BRow,
    BCol,
    BButton,
    BSpinner,
    BFormInput,
    BFormGroup,
    BForm,
} from 'bootstrap-vue'

export default {
    name: 'EventTypeCreate',
    components: {
        ValidationObserver,
        ValidationProvider,
        BCard,
        BRow,
        BCol,
        BButton,
        BSpinner,
        BFormInput,
        BFormGroup,
        BForm,
    },
    data() {
        return {
            isSaving: false,
            form: { id: null, name: '' },
        }
    },
    methods: {
        async onSubmit() {
            this.isSaving = true
            try {
                const { data } = await this.$services.post(
                    '/event-types',
                    this.form
                )
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title:
                            this.$t(`EventType.error.${data.errorCode}`) ||
                            this.$t(`EventType.error.${data?.data?.errorCode}`),
                        icon: 'CheckIcon',
                        variant: 'success',
                    },
                })
                this.$router.push({ name: 'event-type' })
            } catch (err) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Title'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text:
                            this.$t(`EventType.error.${err?.errorCode}`) ||
                            this.$t(
                                `EventType.error.${err?.data?.errorCode}`
                            ) ||
                            '',
                    },
                })
            } finally {
                this.isSaving = false
            }
        },
    },
}
</script>
