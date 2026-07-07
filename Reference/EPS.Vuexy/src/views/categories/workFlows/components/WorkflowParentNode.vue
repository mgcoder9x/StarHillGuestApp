<template>
    <b-card class="parent-node-card" :class="{ expanded: parent.isExpanded }">
        <div class="d-flex align-items-start">
            <!-- Expand/Collapse Button -->
            <b-button
                variant="link"
                size="sm"
                class="p-0 mr-1 expand-btn"
                @click="$emit('toggle-expand', parent.id)"
                v-if="hasChildren"
            >
                <Icon
                    :icon="
                        parent.isExpanded
                            ? 'mdi:chevron-down'
                            : 'mdi:chevron-right'
                    "
                    class="icon-lg"
                />
            </b-button>

            <!-- Parent Form Fields -->
            <div class="parent-fields flex-grow-1">
                <!-- Badge Number -->
                <div class="mb-1">
                    <b-badge variant="primary" pill>{{ index + 1 }}</b-badge>
                </div>
                <!-- Row 1: Name, Code, Step -->
                <b-row class="align-items-center">
                    <b-col
                        :lg="hasChildren ? 4 : 3"
                        md="6"
                        sm="12"
                        class="mb-1"
                    >
                        <validation-provider
                            #default="{ errors }"
                            rules="required"
                            :vid="`parent_name_${parent.id}`"
                            :name="
                                $t(
                                    'categories.workFlows.common.form.label.itemName'
                                )
                            "
                        >
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.workFlows.common.form.label.itemName'
                                    )
                                "
                                label-size="md"
                                class="mb-0"
                            >
                                <b-form-input
                                    :value="parent.name"
                                    :placeholder="
                                        $t(
                                            'categories.workFlows.common.form.label.itemName'
                                        )
                                    "
                                    size="sm"
                                    :disabled="disabled"
                                    @input="updateField('name', $event)"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </b-form-group>
                        </validation-provider>
                    </b-col>

                    <b-col
                        :lg="hasChildren ? 3 : 3"
                        md="6"
                        sm="12"
                        class="mb-1"
                    >
                        <b-form-group
                            :label="
                                $t(
                                    'categories.workFlows.common.form.label.itemCode'
                                )
                            "
                            label-size="md"
                            class="mb-0"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :vid="`parent_code_${parent.id}`"
                                :name="
                                    $t(
                                        'categories.workFlows.common.form.label.itemCode'
                                    )
                                "
                            >
                                <b-form-input
                                    :value="parent.code"
                                    :placeholder="
                                        $t(
                                            'categories.workFlows.common.form.label.itemCode'
                                        )
                                    "
                                    size="sm"
                                    :state="!errors[0] ? null : false"
                                    :disabled="disabled"
                                    @input="updateField('code', $event)"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>

                    <b-col
                        :lg="hasChildren ? 3 : 4"
                        md="6"
                        sm="12"
                        class="mb-1"
                    >
                        <div class="d-flex" style="gap: 15px">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.workFlows.common.form.label.step'
                                    )
                                "
                                class="mb-0"
                                label-size="md"
                                style="flex: 1"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :vid="`parent_step_${parent.id}`"
                                    :name="
                                        $t(
                                            'categories.workFlows.common.form.label.step'
                                        )
                                    "
                                >
                                    <v-select
                                        :value="parent.stepId"
                                        :options="options.steps"
                                        label="text"
                                        :reduce="(opt) => opt.id"
                                        :placeholder="
                                            $t('common.select.placeholder')
                                        "
                                        :append-to-body="true"
                                        :calculate-position="withPopper"
                                        :disabled="disabled"
                                        @input="updateField('stepId', $event)"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                            <b-form-group
                                v-if="!hasChildren"
                                :label="
                                    $t(
                                        'categories.workFlows.common.form.label.estimateTime'
                                    )
                                "
                                class="mb-0"
                                style="flex: 1"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="integer|min_value:0"
                                    :vid="`parent_estimateTime_${parent.id}`"
                                    :name="
                                        $t(
                                            'categories.workFlows.common.form.label.estimateTime'
                                        )
                                    "
                                >
                                    <b-form-input
                                        :value="parent.estimateTime"
                                        type="number"
                                        size="sm"
                                        min="0"
                                        :placeholder="
                                            $t(
                                                'categories.workFlows.common.form.label.estimateTime'
                                            )
                                        "
                                        :disabled="disabled"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        @input="
                                            updateField('estimateTime', $event)
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </div>
                    </b-col>

                    <!-- Actions column -->
                    <div
                        class="parent-actions-container ml-2 mr-auto d-flex align-items-center"
                        style="flex-direction: column"
                    >
                        <!-- Badges -->
                        <div
                            v-if="!parent.isExpanded"
                            class="d-flex gap-1 flex-wrap mb-1"
                        >
                            <b-badge
                                variant="info"
                                class="px-1 py-1"
                                v-b-tooltip.hover
                                :title="
                                    $t(
                                        'categories.workFlows.common.form.tooltip.totalChildren'
                                    )
                                "
                            >
                                <Icon icon="mdi:file-tree" class="sm-icon" />
                                {{ childCount }}
                            </b-badge>
                            <b-badge
                                variant="primary"
                                class="px-1 py-1"
                                v-b-tooltip.hover
                                :title="
                                    $t(
                                        'categories.workFlows.common.form.tooltip.totalEstimateTime'
                                    )
                                "
                            >
                                <Icon
                                    icon="mdi:clock-outline"
                                    class="sm-icon"
                                />
                                {{ totalEstimateTime }}m
                            </b-badge>
                        </div>

                        <!-- Actions -->
                        <div v-if="!disabled" class="d-flex gap-1">
                            <b-button
                                v-b-tooltip.hover
                                variant="success"
                                size="sm"
                                :title="
                                    $t(
                                        'categories.workFlows.common.form.button.addChild'
                                    ) || 'Add Child'
                                "
                                @click="$emit('add-child', parent.id)"
                            >
                                <Icon icon="mdi:plus" />
                            </b-button>
                            <b-button
                                v-b-tooltip.hover
                                variant="danger"
                                size="sm"
                                :title="$t('common.button.delete') || 'Delete'"
                                @click="$emit('remove', parent.id)"
                            >
                                <Icon icon="mdi:delete" />
                            </b-button>
                        </div>
                    </div>
                </b-row>

                <!-- Row 2: Device, EventType, WarningLevel (only show when NO children) -->
                <b-row v-if="!hasChildren" class="g-1">
                    <b-col lg="3" md="4" sm="12" class="md-1">
                        <b-form-group
                            :label="
                                $t(
                                    'categories.workFlows.common.form.label.device'
                                )
                            "
                            label-size="md"
                            class="mb-0"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :vid="`parent_device_${parent.id}`"
                                :name="
                                    $t(
                                        'categories.workFlows.common.form.label.device'
                                    )
                                "
                            >
                                <v-select
                                    :value="parent.deviceId"
                                    :options="options.devices"
                                    label="text"
                                    :reduce="(opt) => opt.id"
                                    :placeholder="
                                        $t('common.select.placeholder')
                                    "
                                    :append-to-body="true"
                                    :calculate-position="withPopper"
                                    :clearable="true"
                                    :disabled="disabled"
                                    @input="updateField('deviceId', $event)"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>

                    <b-col lg="3" md="4" sm="12" class="md-1">
                        <b-form-group
                            :label="
                                $t(
                                    'categories.workFlows.common.form.label.eventType'
                                )
                            "
                            label-size="md"
                            class="mb-0"
                        >
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :vid="`parent_eventType_${parent.id}`"
                                :name="
                                    $t(
                                        'categories.workFlows.common.form.label.eventType'
                                    )
                                "
                            >
                                <v-select
                                    :value="parent.eventTypeId"
                                    :options="options.eventTypes"
                                    label="text"
                                    :reduce="(opt) => opt.id"
                                    :placeholder="
                                        $t('common.select.placeholder')
                                    "
                                    :append-to-body="true"
                                    :calculate-position="withPopper"
                                    :clearable="true"
                                    :disabled="disabled"
                                    @input="onEventTypeChange"
                                />
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>

                    <b-col lg="3" md="4" sm="12" class="md-1">
                        <b-form-group
                            :label="
                                $t(
                                    'categories.workFlows.common.form.label.warningLevelId'
                                )
                            "
                            label-size="md"
                            class="mb-0"
                        >
                            <v-select
                                :value="parent.warningLevelId"
                                :options="filteredWarningLevels"
                                label="text"
                                :reduce="(opt) => opt.id"
                                :placeholder="$t('common.select.placeholder')"
                                :append-to-body="true"
                                :calculate-position="withPopper"
                                :clearable="true"
                                :disabled="disabled || !parent.eventTypeId"
                                @input="updateField('warningLevelId', $event)"
                            />
                        </b-form-group>
                    </b-col>
                </b-row>
            </div>
        </div>
    </b-card>
</template>

<script>
import { createPopper } from '@popperjs/core'

export default {
    name: 'WorkflowParentNode',
    props: {
        parent: {
            type: Object,
            required: true,
        },
        options: {
            type: Object,
            required: true,
            default: () => ({
                steps: [],
                devices: [],
                eventTypes: [],
                eventWarningLevels: [],
            }),
        },
        childCount: {
            type: Number,
            default: 0,
        },
        totalEstimateTime: {
            type: Number,
            default: 0,
        },
        hasChildren: {
            type: Boolean,
            default: false,
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
        index: {
            type: Number,
            required: true,
        },
    },
    computed: {
        // Chỉ cần MỘT trong 4 field có giá trị → disable thêm children (OR logic)
        hasAnyOptionalField() {
            console.log(this.parent)
            console.log(
                Boolean(
                    this.parent.deviceId ||
                    this.parent.eventTypeId ||
                    this.parent.warningLevelId
                )
            )
            return Boolean(
                this.parent.deviceId ||
                this.parent.eventTypeId ||
                this.parent.warningLevelId
            )
        },
        filteredWarningLevels() {
            if (!this.parent.eventTypeId) {
                return []
            }
            return (this.options.eventWarningLevels || []).filter(
                (lvl) =>
                    lvl.eventTypeId?.toString() ===
                    this.parent.eventTypeId?.toString()
            )
        },
        tooltipId() {
            return `tooltip-${this.parent.id}`
        },
    },
    methods: {
        updateField(field, value) {
            // Auto-trim name and code fields
            const trimmedValue =
                ['name', 'code'].includes(field) && typeof value === 'string'
                    ? value.trim()
                    : value
            this.$emit('update:parent', {
                ...this.parent,
                [field]: trimmedValue,
            })
        },
        onEventTypeChange(eventTypeId) {
            // Reset warning level when event type changes
            this.$emit('update:parent', {
                ...this.parent,
                eventTypeId,
                warningLevelId: null,
            })
        },
        withPopper(dropdownList, component, { width }) {
            dropdownList.style.width = width
            const popper = createPopper(component.$refs.toggle, dropdownList, {
                placement: 'bottom',
                modifiers: [
                    { name: 'offset', options: { offset: [0, -1] } },
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
.parent-node-card {
    padding: 8px 10px !important;
    background: linear-gradient(135deg, #f8fafc 0%, #e0e7ff 100%);
    border-left: 3px solid #3b82f6;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);

    &:hover {
        box-shadow: 0 2px 6px rgba(0, 0, 0, 0.08);
    }

    &.expanded {
        border-left-color: #2563eb;
    }

    .expand-btn {
        color: #3b82f6;
        text-decoration: none !important;
        min-width: 24px;

        &:hover:not(:disabled) {
            color: #2563eb;
        }

        &:disabled {
            opacity: 0.4;
            cursor: not-allowed;
        }

        .icon-lg {
            font-size: 1.1rem;
        }
    }

    .parent-fields {
        min-width: 0;

        .form-group {
            label {
                font-size: 0.85rem;
                font-weight: 600;
                color: #475569;
                margin-bottom: 2px;
            }
        }

        .form-control,
        .v-select {
            font-size: 0.85rem;
        }
    }
}

.g-1 {
    --bs-gutter-x: 0.5rem;
    --bs-gutter-y: 0.25rem;
}

.gap-1 {
    gap: 0.25rem;
}

.badge {
    font-size: 0.7rem;

    .sm-icon {
        font-size: 0.75rem;
        vertical-align: middle;
        margin-right: 2px;
    }
}
</style>
