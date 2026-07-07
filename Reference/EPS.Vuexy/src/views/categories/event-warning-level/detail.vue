<template>
    <div>
        <b-card :title="$t('EventWarningLevel.Detail.TitleEdit')">
            <validation-observer
                ref="formRules"
                v-slot="{ handleSubmit, invalid }"
            >
                <b-form @submit.prevent="handleSubmit(onSubmit)">
                    <b-row>
                        <!-- Id -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('EventWarningLevel.Detail.Form.Id')"
                                label-for="ewlId"
                                label-cols-md="4"
                                class="mb-50 mb-md-1"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :name="
                                        $t('EventWarningLevel.Detail.Form.Id')
                                    "
                                    rules="required|min_value:1|integer"
                                >
                                    <b-form-input
                                        id="ewlId"
                                        v-model="form.id"
                                        disabled
                                        readonly
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- EventTypeId -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'EventWarningLevel.Detail.Form.EventTypeId'
                                    )
                                "
                                label-for="ewlEventTypeId"
                                label-cols-md="4"
                                class="mb-50 mb-md-1"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :name="
                                        $t(
                                            'EventWarningLevel.Detail.Form.EventTypeId'
                                        )
                                    "
                                    rules="required"
                                >
                                <v-select
                                     v-model="form.eventTypeId"
                                    :options="eventTypeOptions"
                                    :placeholder="
                                        $t('common.select.placeholder')
                                    "
                                    label="text"
                                    :multiple="false"
                                    track-by="value"
                                    :reduce="(item) => item.value"
                                    :state="
                                            errors.length > 0 ? false : null
                                        "
                                         :disabled="!editing"
                                >
                                </v-select>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Warning Level Group (Optional) -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'EventWarningLevel.Detail.Form.WorkFlowEventWarningLevelId'
                                    )
                                "
                                label-for="ewlGroupId"
                                label-cols-md="4"
                                class="mb-50 mb-md-1"
                            >
                                <div class="d-flex" style="gap: 8px">
                                    <v-select
                                        v-model="form.workFlowEventWarningLevelId"
                                        :options="warningLevelGroupOptions"
                                        :placeholder="$t('common.select.placeholder')"
                                        label="text"
                                        :multiple="false"
                                        :reduce="(item) => item.id"
                                        :clearable="true"
                                        :disabled="true"
                                        class="flex-grow-1"
                                    />
                                    <!-- <b-button
                                        v-if="editing"
                                        variant="outline-primary"
                                        size="sm"
                                        @click="openCreateGroup"
                                    >
                                        {{
                                            $t(
                                                'EventWarningLevel.Detail.Form.CreateGroup'
                                            )
                                        }}
                                    </b-button> -->
                                </div>
                            </b-form-group>
                        </b-col>

                        <!-- Title -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t('EventWarningLevel.Detail.Form.Title')
                                "
                                label-for="ewlTitle"
                                label-cols-md="4"
                                class="mb-50 mb-md-1"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :name="
                                        $t(
                                            'EventWarningLevel.Detail.Form.Title'
                                        )
                                    "
                                    rules="required|max:250"
                                >
                                    <b-form-input
                                        id="ewlTitle"
                                        v-model="form.title"
                                        :disabled="!editing"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :placeholder="
                                            $t(
                                                'EventWarningLevel.Detail.Form.TitlePlaceholder'
                                            )
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Level -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t('EventWarningLevel.Detail.Form.Level')
                                "
                                label-for="ewlLevel"
                                label-cols-md="4"
                                class="mb-50 mb-md-1"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :name="
                                        $t(
                                            'EventWarningLevel.Detail.Form.Level'
                                        )
                                    "
                                    rules="required|min_value:1|integer"
                                >
                                    <b-form-input
                                        id="ewlLevel"
                                        v-model.number="form.level"
                                        type="number"
                                        :disabled="!editing"
                                        :placeholder="
                                            $t(
                                                'EventWarningLevel.Detail.Form.Level'
                                            )
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <!-- Color -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t('EventWarningLevel.Detail.Form.Color')
                                "
                                label-for="ewlColor"
                                label-cols-md="4"
                                class="mb-50 mb-md-1"
                            >
                                <div class="d-flex align-items-center">
                                    <b-form-input
                                        id="ewlColor"
                                        v-model="form.color"
                                        type="color"
                                        :disabled="!editing"
                                        style="
                                            width: 48px;
                                            height: 38px;
                                            padding: 2px 4px;
                                            cursor: pointer;
                                            flex-shrink: 0;
                                        "
                                        class="mr-1"
                                    />
                                    <b-form-input
                                        v-model="form.color"
                                        :disabled="!editing"
                                        placeholder="#RRGGBB"
                                        style="flex: 1"
                                    />
                                </div>
                            </b-form-group>
                        </b-col>

                        <!-- Description -->
                        <b-col md="12">
                            <b-form-group
                                :label="
                                    $t(
                                        'EventWarningLevel.Detail.Form.Description'
                                    )
                                "
                                label-for="ewlDescription"
                                label-cols-md="2"
                                class="mb-50 mb-md-1"
                            >
                                <b-form-textarea
                                    id="ewlDescription"
                                    v-model="form.descreption"
                                    :disabled="!editing"
                                    rows="3"
                                    :placeholder="
                                        $t(
                                            'EventWarningLevel.Detail.Form.DescriptionPlaceholder'
                                        )
                                    "
                                />
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
                                @click="
                                    $router.push({
                                        name: 'event-warning-level',
                                    })
                                "
                            >
                                {{ $t('Button.Back') }}
                            </b-button>
                        </template>
                    </div>
                </b-form>
            </validation-observer>
        </b-card>

        <b-modal
            id="ewl-create-group"
            v-model="showCreateGroupModal"
            hide-footer
            centered
            :title="
                $t('EventWarningLevel.Detail.Form.CreateGroupTitle') ||
                'Create Category'
            "
        >
            <b-form @submit.prevent="submitCreateGroup">
                <b-form-group
                    :label="$t('EventWarningLevel.Detail.Form.GroupName')"
                    label-for="ewlGroupName"
                    label-class="required"
                >
                    <b-form-input
                        id="ewlGroupName"
                        v-model="createGroupForm.name"
                        :placeholder="
                            $t(
                                'EventWarningLevel.Detail.Form.GroupNamePlaceholder'
                            )
                        "
                    />
                </b-form-group>

                <div class="d-flex justify-content-end" style="gap: 8px">
                    <b-button
                        variant="secondary"
                        size="sm"
                        @click="showCreateGroupModal = false"
                    >
                        {{ $t('Message.Exit') || 'Close' }}
                    </b-button>
                    <b-button variant="primary" size="sm" type="submit">
                        {{ $t('Message.Agree') || 'Save' }}
                    </b-button>
                </div>
            </b-form>
        </b-modal>
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
    BFormTextarea,
    BFormSelect,
    BFormSelectOption,
    BForm,
    BModal,
} from 'bootstrap-vue'

export default {
    name: 'EventWarningLevelDetail',
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
        BFormTextarea,
        BFormSelect,
        BFormSelectOption,
        BForm,
        BModal,
    },
    data() {
        return {
            isSaving: false,
            editing: false,
            form: {
                id: null,
                title: '',
                descreption: '',
                level: null,
                color: '#000000',
                eventTypeId: null,
                workFlowEventWarningLevelId: null,
            },
            eventTypeOptions: [],
            warningLevelGroupOptions: [],
            showCreateGroupModal: false,
            createGroupForm: {
                name: '',
            },
        }
    },
    created() {
        this.loadEventTypes()
        this.loadWarningLevelGroups()
        this.fetchDetail()
    },
    computed: {
        filteredGroupOptions() {
            if (!this.form.eventTypeId) return this.warningLevelGroupOptions
            return this.warningLevelGroupOptions.filter(
                (g) =>
                    !g.eventTypeId ||
                    g.eventTypeId?.toString() ===
                        this.form.eventTypeId?.toString()
            )
        },
    },
    methods: {
        async loadEventTypes() {
            try {
                const res = await this.$services.get('/lookup/eventType')
                const data = res.data?.data || []
                 this.eventTypeOptions = data.map((et) => ({
                    value: parseInt(et.id),
                    text: et.text,
                }))
            } catch {
                /* ignore */
            }
        },
        async loadWarningLevelGroups() {
            try {
                const res = await this.$services.get(
                    '/lookup/workflow-event-warning-levels'
                )
                const data = res.data?.data || []
                this.warningLevelGroupOptions = data.map((item) => ({
                    ...item,
                    id: parseInt(item.id),
                }))
            } catch {
                /* ignore */
            }
        },
        async fetchDetail() {
            try {
                const id = this.$route.params.id
                if (!id) return
                const res = await this.$services.get(
                    `/event-warning-levels/${id}`
                )
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
                const { data } = await this.$services.put(
                    `/event-warning-levels/${this.form.id}`,
                    this.form
                )
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title:
                            this.$t(
                                `EventWarningLevel.error.${data?.errorCode}`
                            ) ||
                            this.$t(
                                `EventWarningLevel.error.${data?.data?.errorCode}`
                            ),
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
                        text: err?.message || err?.data?.message || '',
                    },
                })
            } finally {
                this.isSaving = false
            }
        },
        openCreateGroup() {
            this.createGroupForm = {
                name: '',
            }
            this.showCreateGroupModal = true
        },
        async submitCreateGroup() {
            if (!this.createGroupForm.name?.trim()) return
            try {
                const payload = {
                    name: this.createGroupForm.name.trim(),
                    eventTypeId: this.form.eventTypeId || null,
                }
                const res = await this.$services.post(
                    '/event-warning-levels/workflow-event-warning-levels',
                    payload
                )
                const created = res.data?.data
                await this.loadWarningLevelGroups()
                if (created?.id) {
                    this.form.workFlowEventWarningLevelId = parseInt(created.id)
                }
                this.showCreateGroupModal = false
            } catch (err) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Title'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: err?.message || err?.data?.message || '',
                    },
                })
            }
        },
    },
}
</script>
