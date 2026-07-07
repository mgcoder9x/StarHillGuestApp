<template>
    <b-card class="child-node-card ml-4 mt-2">
        <div class="d-flex align-items-center">
            <!-- Drag Handle -->
            <div
                v-b-tooltip.hover
                class="drag-handle mr-2 mt-2"
                title="Drag to reorder"
            >
                <Icon icon="mdi:drag-vertical" class="text-muted" />
            </div>

            <!-- Child Badge Number -->
            <div class="mr-2 mt-1">
                <b-badge variant="secondary" pill
                    >{{ parentIndex + 1 }}.{{ index + 1 }}</b-badge
                >
            </div>

            <!-- Child Form Fields -->
            <div class="flex-grow-1">
                <!-- Row 1: Name (33%) | Code (33%) | Device (33%) -->
                <b-row>
                    <b-col lg="4" md="4" sm="12">
                        <b-form-group
                            :label="
                                $t(
                                    'categories.workFlows.common.form.label.itemName'
                                )
                            "
                            class="mb-1"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :vid="`child_name_${child.id}`"
                                :name="
                                    $t(
                                        'categories.workFlows.common.form.label.itemName'
                                    )
                                "
                            >
                                <b-form-input
                                    :value="child.name"
                                    size="sm"
                                    :disabled="disabled"
                                    @input="updateField('name', $event)"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>

                    <b-col lg="4" md="4" sm="12">
                        <b-form-group
                            :label="
                                $t(
                                    'categories.workFlows.common.form.label.itemCode'
                                )
                            "
                            class="mb-1"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :vid="`child_code_${child.id}`"
                                :name="
                                    $t(
                                        'categories.workFlows.common.form.label.itemCode'
                                    )
                                "
                            >
                                <b-form-input
                                    :value="child.code"
                                    size="sm"
                                    :state="
                                        !errors[0] && !isDuplicateCode
                                            ? null
                                            : false
                                    "
                                    :disabled="disabled"
                                    @input="updateField('code', $event)"
                                />
                                <small class="text-danger">{{
                                    errors[0] || duplicateCodeError
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>

                    <b-col lg="4" md="4" sm="12">
                        <b-form-group
                            :label="
                                $t(
                                    'categories.workFlows.common.form.label.device'
                                )
                            "
                            class="mb-1"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :vid="`child_device_${child.id}`"
                                :name="
                                    $t(
                                        'categories.workFlows.common.form.label.device'
                                    )
                                "
                            >
                                <v-select
                                    :value="effectiveDeviceId"
                                    :options="options.devices"
                                    label="text"
                                    :reduce="(opt) => opt.id"
                                    :placeholder="devicePlaceholder"
                                    :append-to-body="true"
                                    :calculate-position="withPopper"
                                    :disabled="disabled"
                                    @input="updateField('deviceId', $event)"
                                >
                                    <template #selected-option="option">
                                        <span
                                            v-if="isInheritedDevice"
                                            class="text-muted"
                                        >
                                            <Icon
                                                icon="mdi:arrow-up-thin"
                                                class="sm-icon"
                                            />
                                            {{ option.text }}
                                        </span>
                                        <span v-else>{{ option.text }}</span>
                                    </template>
                                </v-select>
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                </b-row>

                <!-- Row 2: Event Type (33%) | Warning Level (33%) | Estimate Time (33%) -->
                <b-row>
                    <b-col lg="4" md="4" sm="12">
                        <b-form-group
                            :label="
                                $t(
                                    'categories.workFlows.common.form.label.eventType'
                                )
                            "
                            class="mb-1"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :vid="`child_eventType_${child.id}`"
                                :name="
                                    $t(
                                        'categories.workFlows.common.form.label.eventType'
                                    )
                                "
                            >
                                <v-select
                                    :value="child.eventTypeId"
                                    :options="options.eventTypes"
                                    label="text"
                                    :reduce="(opt) => opt.id"
                                    :placeholder="
                                        $t('common.select.placeholder')
                                    "
                                    :append-to-body="true"
                                    :calculate-position="withPopper"
                                    :disabled="disabled"
                                    @input="onEventTypeChange"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>

                    <b-col lg="4" md="4" sm="12">
                        <b-form-group
                            :label="
                                $t(
                                    'categories.workFlows.common.form.label.warningLevelId'
                                )
                            "
                            class="mb-1"
                        >
                            <v-select
                                :value="child.warningLevelId"
                                :options="filteredWarningLevels"
                                label="text"
                                :reduce="(opt) => opt.id"
                                :placeholder="$t('common.select.placeholder')"
                                :append-to-body="true"
                                :calculate-position="withPopper"
                                :disabled="disabled"
                                @input="updateField('warningLevelId', $event)"
                            />
                        </b-form-group>
                    </b-col>

                    <b-col lg="4" md="4" sm="12">
                        <b-form-group
                            :label="
                                $t(
                                    'categories.workFlows.common.form.label.estimateTime'
                                )
                            "
                            class="mb-1"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="integer|min_value:0"
                                :vid="`child_estimateTime_${child.id}`"
                                :name="
                                    $t(
                                        'categories.workFlows.common.form.label.estimateTime'
                                    )
                                "
                            >
                                <b-form-input
                                    :value="child.estimateTime"
                                    type="number"
                                    size="sm"
                                    :placeholder="
                                        $t(
                                            'categories.workFlows.common.form.label.estimateTime'
                                        )
                                    "
                                    :disabled="disabled"
                                    :state="errors.length > 0 ? false : null"
                                    @input="updateField('estimateTime', $event)"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                </b-row>
            </div>

            <!-- Delete Child Button -->
            <b-button
                v-if="!disabled"
                v-b-tooltip.hover
                variant="danger"
                size="sm"
                class="ml-2"
                :title="$t('common.button.delete') || 'Delete'"
                @click="$emit('remove', child.id)"
            >
                <Icon icon="mdi:delete" />
            </b-button>
        </div>
    </b-card>
</template>

<script>
import { createPopper } from '@popperjs/core'

export default {
    name: 'WorkflowChildNode',
    props: {
        child: {
            type: Object,
            required: true,
        },
        index: {
            type: Number,
            required: true,
        },
        parentIndex: {
            type: Number,
            required: true,
        },
        parentDevice: {
            type: [Number, String, null],
            default: null,
        },
        options: {
            type: Object,
            required: true,
            default: () => ({
                devices: [],
                eventTypes: [],
                eventWarningLevels: [],
            }),
        },
        isDuplicateCode: {
            type: Boolean,
            default: false,
        },
        duplicateCodeError: {
            type: String,
            default: '',
        },
        disabled: {
            type: Boolean,
            default: false,
        },
    },
    computed: {
        // Device: inherit from parent if not explicitly set
        effectiveDeviceId() {
            return this.child.deviceId ?? null
        },
        isInheritedDevice() {
            return this.child.deviceId === null && this.parentDevice !== null
        },
        devicePlaceholder() {
            if (this.isInheritedDevice) {
                const device = this.options.devices.find(
                    (d) => d.id === this.parentDevice
                )
                const inheritedText =
                    this.$t(
                        'categories.workFlows.common.form.label.inherited'
                    ) || 'Kế thừa'
                return device
                    ? `${inheritedText}: ${device.text}`
                    : this.$t('common.select.placeholder') || 'Chọn giá trị'
            }
            return this.$t('common.select.placeholder') || 'Chọn giá trị'
        },
        filteredWarningLevels() {
            if (!this.child.eventTypeId) {
                return []
            }
            return this.options.eventWarningLevels.filter(
                (lvl) =>
                    lvl.eventTypeId?.toString() ===
                    this.child.eventTypeId?.toString()
            )
        },
    },
    methods: {
        updateField(field, value) {
            // Auto-trim name and code fields
            const trimmedValue =
                ['name', 'code'].includes(field) && typeof value === 'string'
                    ? value.trim()
                    : value
            this.$emit('update:child', {
                ...this.child,
                [field]: trimmedValue,
            })
        },
        onEventTypeChange(eventTypeId) {
            // Reset warning level when event type changes
            this.$emit('update:child', {
                ...this.child,
                eventTypeId,
                warningLevelId: null,
            })
        },
        withPopper(dropdownList, component, { width }) {
            dropdownList.style.width = width
            const popper = createPopper(component.$refs.toggle, dropdownList, {
                placement: 'bottom',
                modifiers: [
                    {
                        name: 'offset',
                        options: { offset: [0, -1] },
                    },
                    {
                        name: 'toggleClass',
                        enabled: true,
                        phase: 'write',
                        fn({ state }) {
                            component.$el.classList.toggle(
                                'drop-up',
                                state.placement === 'top'
                            )
                        },
                    },
                ],
            })
            return () => popper.destroy()
        },
    },
}
</script>

<style lang="scss" scoped>
.child-node-card {
    padding: 8px 10px !important;
    margin-left: 20px !important;
    margin-top: 6px !important;
    background: #ffffff;
    border-left: 2px solid #10b981;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.06);

    &:hover {
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        border-left-color: #059669;
    }

    .drag-handle {
        cursor: grab;
        user-select: none;
        margin-top: 4px !important;

        &:active {
            cursor: grabbing;
        }

        &:hover {
            color: #059669 !important;
        }
    }

    .form-group {
        label {
            font-size: 0.7rem;
            font-weight: 600;
            color: #64748b;
            margin-bottom: 2px;
        }

        .form-control,
        .v-select {
            font-size: 0.8rem;
        }
    }
}
.card-body {
    padding: 0 !important;
}
.sm-icon {
    font-size: 0.75rem;
    vertical-align: middle;
}
</style>
