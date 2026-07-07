<template>
    <div>
        <b-card :title="$t('EventType.Detail.TitleEdit')">
            <validation-observer
                ref="formRules"
                v-slot="{ handleSubmit, invalid }"
            >
                <b-form @submit.prevent="handleSubmit(onSubmit)">
                    <b-row class="justify-content-center mt-2 mb-2">
                        <!-- Id -->
                        <b-col md="5">
                            <b-form-group
                                :label="$t('EventType.Detail.Form.Id')"
                                label-for="eventTypeId"
                                label-cols-md="4"
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
                                        disabled
                                        readonly
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Name -->
                        <b-col md="5">
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
                                        :disabled="!editing"
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

                    <div
                        class="d-flex justify-content-center mt-2 gap-1 align-items-center"
                    >
                        <template v-if="editing">
                            <b-button
                                type="submit"
                                variant="primary"
                                :disabled="isSaving"
                            >
                                <b-spinner
                                    v-if="isSaving"
                                    small
                                    class="mr-50"
                                />
                                {{ $t('Button.Save') }}
                            </b-button>
                            <b-button
                                variant="outline-secondary"
                                @click="stopEdit"
                            >
                                {{ $t('Button.Cancel') }}
                            </b-button>
                        </template>
                        <template v-else>
                            <b-button
                                variant="primary"
                                @click.prevent="startEdit"
                            >
                                {{ $t('Button.Edit') }}
                            </b-button>
                            <b-button
                                variant="outline-secondary"
                                @click="$router.push({ name: 'event-type' })"
                            >
                                {{ $t('Button.Back') }}
                            </b-button>
                        </template>
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
    name: 'EventTypeDetail',
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
            editing: false,
            form: { id: null, name: '' },
        }
    },
    created() {
        this.fetchDetail()
    },
    methods: {
        async fetchDetail() {
            try {
                const id = this.$route.params.id
                if (!id) return
                const res = await this.$services.get(`/event-types/${id}`)
                if (res.data) {
                    this.form = res.data.data
                }
            } catch (err) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Title'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: err?.message || '',
                    },
                })
            }
        },
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.fetchDetail()
        },
        async onSubmit() {
            this.isSaving = true
            try {
                // API EventTypeController update (/event-types/{id}) uses DTO
                const { data } = await this.$services.put(
                    `/event-types/${this.form.id}`,
                    {
                        id: this.form.id,
                        name: this.form.name,
                    }
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
                this.stopEdit()
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
