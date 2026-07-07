<template>
    <validation-observer ref="rules" mode="aggressive">
        <b-container fluid>
            <b-card>
                <b-form @submit="onSubmit">
                    <b-row cols="2">
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.workFlows.common.form.label.name'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.workFlows.common.form.label.name'
                                        )
                                    "
                                >
                                    <b-form-input
                                        v-model.trim="newWorkFlow.name"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.workFlows.common.form.label.code'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.workFlows.common.form.label.code'
                                        )
                                    "
                                >
                                    <b-form-input
                                        v-model.trim="newWorkFlow.code"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.workFlows.common.form.label.status'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.workFlows.common.form.label.status'
                                        )
                                    "
                                >
                                    <b-form-checkbox
                                        v-model="newWorkFlow.status"
                                        :value="1"
                                        :unchecked-value="0"
                                        switch
                                        class="custom-control-primary"
                                    >
                                        <span class="switch-icon-left">
                                            <Icon icon="mdi:check" />
                                        </span>
                                        <span class="switch-icon-right">
                                            <Icon icon="mdi:close" />
                                        </span>
                                    </b-form-checkbox>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <b-row>
                        <b-col cols="12">
                            <!-- Tree View Container -->
                            <div class="tree-view-container">
                                <!-- Toolbar -->
                                <div
                                    class="d-flex justify-content-between align-items-center mb-2"
                                >
                                    <h5 class="mb-0">
                                        {{
                                            $t(
                                                'categories.workFlows.common.form.label.workflowSteps'
                                            ) || 'Workflow Steps'
                                        }}
                                    </h5>
                                    <b-button
                                        variant="success"
                                        size="sm"
                                        @click="addParentNode"
                                    >
                                        <Icon icon="mdi:plus" class="sm-icon" />
                                        <span class="ml-50">
                                            {{
                                                $t(
                                                    'categories.workFlows.common.form.button.addParent'
                                                ) || 'Add Parent Node'
                                            }}
                                        </span>
                                    </b-button>
                                </div>

                                <!-- Tree List -->
                                <div class="tree-list">
                                    <div
                                        v-for="(parent, index) in parentNodes"
                                        :key="parent.id"
                                        class="parent-node-wrapper mb-3"
                                    >
                                        <!-- Parent Node Component -->
                                        <WorkflowParentNode
                                            :index="index"
                                            :parent="parent"
                                            :options="parentNodeOptions"
                                            :child-count="
                                                getChildCount(parent.id)
                                            "
                                            :total-estimate-time="
                                                getParentEstimateTime(parent.id)
                                            "
                                            :has-children="
                                                getChildCount(parent.id) > 0
                                            "
                                            :is-duplicate-code="
                                                isDuplicateCodeInScope(
                                                    parent.id
                                                )
                                            "
                                            :duplicate-code-error="
                                                getDuplicateCodeErrorForNode(
                                                    parent.id
                                                )
                                            "
                                            @update:parent="updateParentNode"
                                            @toggle-expand="toggleExpand"
                                            @add-child="addChildNode"
                                            @remove="removeNode"
                                        />

                                        <!-- Children List (with draggable) -->
                                        <draggable
                                            v-if="
                                                parent.isExpanded &&
                                                parent._children &&
                                                parent._children.length >
                                                    0
                                            "
                                            v-model="parent._children"
                                            handle=".drag-handle"
                                            class="children-list"
                                            :animation="200"
                                            @end="onChildReorder(parent.id)"
                                        >
                                            <WorkflowChildNode
                                                v-for="(
                                                    child, childIndex
                                                ) in parent._children"
                                                :key="child.id"
                                                :child="child"
                                                :index="childIndex"
                                                :parent-index="index"
                                                :parent-device="parent.deviceId"
                                                :options="childNodeOptions"
                                                :is-duplicate-code="
                                                    isDuplicateCodeInScope(
                                                        child.id
                                                    )
                                                "
                                                :duplicate-code-error="
                                                    getDuplicateCodeErrorForNode(
                                                        child.id
                                                    )
                                                "
                                                @update:child="updateChildNode"
                                                @remove="removeNode"
                                            />
                                        </draggable>

                                        <!-- Empty state when parent has no children and can have children -->
                                        <!-- <div
                                            v-if="
                                                parent.isExpanded &&
                                                !hasParentCompleteData(
                                                    parent
                                                ) &&
                                                getChildren(parent.id)
                                                    .length === 0
                                            "
                                            class="ml-4 mt-2"
                                        >
                                            <b-alert
                                                show
                                                variant="info"
                                                class="mb-0"
                                            >
                                                <Icon
                                                    icon="mdi:information"
                                                    class="sm-icon"
                                                />
                                                <span class="ml-1">
                                                    {{
                                                        $t(
                                                            'categories.workFlows.common.form.message.noChildren'
                                                        ) ||
                                                        'No child nodes. Click "Add Child" to create one.'
                                                    }}
                                                </span>
                                            </b-alert>
                                        </div> -->
                                    </div>
                                </div>

                                <!-- Empty state when no parent nodes -->
                                <div
                                    v-if="parentNodes.length === 0"
                                    class="text-center py-5"
                                >
                                    <Icon
                                        icon="mdi:folder-open-outline"
                                        style="font-size: 48px"
                                        class="text-muted mb-2"
                                    />
                                    <p class="text-muted">
                                        {{
                                            $t(
                                                'categories.workFlows.common.form.message.noParents'
                                            ) ||
                                            'No workflow steps yet. Click "Add Parent Node" to start.'
                                        }}
                                    </p>
                                </div>
                            </div>
                        </b-col>
                    </b-row>

                    <div class="text-center mt-2">
                        <b-button
                            v-if="authorize(['ManageWorkFlow'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120 btn-hover-linear-primary"
                        >
                            <Icon
                                icon="material-symbols:save-outline"
                                class="sm-icon"
                            />
                            <span class="ml-50">{{
                                $t('common.button.save')
                            }}</span>
                        </b-button>
                        <b-button
                            :to="{ path: '/categories/workFlows/list' }"
                            type="reset"
                            variant="secondary"
                            title="Cancel"
                            class="btn-120 mb-50 btn-hover-linear-secondary"
                        >
                            <Icon icon="mdi:cancel" class="sm-icon" />
                            <span class="ml-50">{{
                                $t('common.button.cancel')
                            }}</span>
                        </b-button>
                    </div>
                </b-form>
            </b-card>
        </b-container>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import draggable from 'vuedraggable'
import WorkflowParentNode from './components/WorkflowParentNode.vue'
import WorkflowChildNode from './components/WorkflowChildNode.vue'
import lookupService from '@/services/lookup.service'

export default {
    mixins: [authorizationMixin],
    components: {
        draggable,
        WorkflowParentNode,
        WorkflowChildNode,
    },
    data() {
        return {
            newWorkFlow: {
                items: [],
                name: null,
                code: null,
                status: 1,
                compId: null,
            },
            options: {
                steps: [],
                devices: [],
                eventTypes: [],
                eventWarningLevels: [],
            },
        }
    },
    computed: {
        statusOptions() {
            return [
                {
                    value: 1,
                    text: this.$t(
                        'categories.workFlows.list.searchForm.options.active'
                    ),
                },
                {
                    value: 0,
                    text: this.$t(
                        'categories.workFlows.list.searchForm.options.inactive'
                    ),
                },
            ]
        },
        parentNodes() {
            const parents = this.newWorkFlow.items.filter(
                (item) => !item.parentId
            )
            parents.forEach((parent) => {
                parent._children = this.getChildren(parent.id)
            })
            return parents
        },
        parentNodeOptions() {
            return {
                steps: this.options.steps,
                devices: this.options.devices,
                eventTypes: this.options.eventTypes,
                eventWarningLevels: this.options.eventWarningLevels,
            }
        },
        childNodeOptions() {
            return {
                steps: this.options.steps,
                devices: this.options.devices,
                eventTypes: this.options.eventTypes,
                eventWarningLevels: this.options.eventWarningLevels,
            }
        },
    },
    async created() {
        const accessToken = this.$services.getUserData()
        this.newWorkFlow.compId = accessToken.companyId
        await this.loadOptions()
    },
    methods: {
        // ============ Parent-Child Node Management ============
        getChildren(parentId) {
            return this.newWorkFlow.items.filter(
                (item) => item.parentId === parentId
            )
        },

        // Check if parent has ANY optional field filled (OR logic)
        // If any one field has value, children cannot be added
        hasParentCompleteData(parent) {
            return Boolean(
                parent.deviceId || parent.eventTypeId || parent.warningLevelId
            )
        },

        addParentNode() {
            this.newWorkFlow.items.push({
                id: this.generateId(),
                parentId: null,
                name: null,
                code: null,
                stepId: null,
                deviceId: null,
                eventTypeId: null,
                warningLevelId: null,
                estimateTime: null,
                isExpanded: true,
                _children: [],
            })
        },

        addChildNode(parentId) {
            const parent = this.newWorkFlow.items.find((i) => i.id === parentId)
            this.emptyWhenHasChild(parent)
            const newChild = {
                id: this.generateId(),
                parentId: parentId,
                name: null,
                code: null,
                // StepId will be auto-generated by backend
                stepId: null,
                // Device can be overridden (null means inherit)
                deviceId: null,
                eventTypeId: null,
                warningLevelId: null,
                estimateTime: null,
            }
            this.newWorkFlow.items.push(newChild)
            if (!parent._children) parent._children = []
            parent._children.push(newChild)
        },
        emptyWhenHasChild(parent) {
            parent.isExpanded = true
            parent.eventTypeId = null
            parent.warningLevelId = null
            parent.deviceId = null
            parent.estimateTime = null
        },
        updateParentNode(updatedParent) {
            const idx = this.newWorkFlow.items.findIndex(
                (i) => i.id === updatedParent.id
            )
            if (idx !== -1) {
                // Preserve _children reference
                const oldChildren = this.newWorkFlow.items[idx]._children
                this.$set(this.newWorkFlow.items, idx, {
                    ...updatedParent,
                    _children: oldChildren,
                })
            }
        },

        updateChildNode(updatedChild) {
            const idx = this.newWorkFlow.items.findIndex(
                (i) => i.id === updatedChild.id
            )
            if (idx !== -1) {
                this.$set(this.newWorkFlow.items, idx, updatedChild)
            }
        },

        toggleExpand(nodeId) {
            const node = this.newWorkFlow.items.find((i) => i.id === nodeId)
            if (node) {
                this.$set(node, 'isExpanded', !node.isExpanded)
            }
        },

        removeNode(nodeId) {
            const childrenIds = this.getChildren(nodeId).map((c) => c.id)
            childrenIds.forEach((childId) => this.removeNode(childId))
            const index = this.newWorkFlow.items.findIndex(
                (item) => item.id === nodeId
            )
            if (index !== -1) {
                const nodeToRemove = this.newWorkFlow.items[index]
                if (nodeToRemove.parentId) {
                    const parent = this.newWorkFlow.items.find((i) => i.id === nodeToRemove.parentId)
                    if (parent && parent._children) {
                        parent._children = parent._children.filter(c => c.id !== nodeId)
                    }
                }
                this.newWorkFlow.items.splice(index, 1)
            }
        },

        generateId() {
            return `temp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
        },

        onChildReorder(parentId) {
            const parent = this.newWorkFlow.items.find((i) => i.id === parentId)
            if (parent && parent._children) {
                this.newWorkFlow.items = this.newWorkFlow.items.filter(
                    (item) => item.parentId !== parentId
                )
                parent._children.forEach((child) => {
                    this.newWorkFlow.items.push(child)
                })
            }
        },

        // ============ Aggregation & Metrics ============
        getParentEstimateTime(parentId) {
            const children = this.getChildren(parentId)
            if (children.length === 0)
                return parseInt(
                    this.newWorkFlow.items.find((i) => i.id === parentId)
                        ?.estimateTime || 0
                )
            return children.reduce(
                (sum, child) => sum + (parseInt(child.estimateTime) || 0),
                0
            )
        },

        getChildCount(parentId) {
            return this.getChildren(parentId).length
        },

        // ============ Validation ============
        isDuplicateCodeInScope(nodeId) {
            const node = this.newWorkFlow.items.find((n) => n.id === nodeId)
            if (!node || !node.code || node.code.trim() === '') return false

            const siblings = this.newWorkFlow.items.filter(
                (i) => i.parentId === node.parentId && i.id !== node.id
            )

            return siblings.some(
                (s) =>
                    s.code &&
                    s.code.trim().toLowerCase() ===
                        node.code.trim().toLowerCase()
            )
        },

        getDuplicateCodeErrorForNode(nodeId) {
            if (this.isDuplicateCodeInScope(nodeId)) {
                return (
                    this.$t('categories.workFlows.error.duplicateCode') ||
                    'Mã code bị trùng lặp'
                )
            }
            return ''
        },

        hasDuplicateStepIds() {
            // Only check parent items - children's StepId is auto-generated by backend
            const stepIds = this.newWorkFlow.items
                .filter((i) => i.stepId && (i.parentId === 0 || !i.parentId))
                .map((i) => i.stepId)
            const uniqueIds = new Set(stepIds)
            return uniqueIds.size !== stepIds.length
        },

        // ============ API Calls ============
        async loadOptions() {
            await Promise.allSettled([
                lookupService.loadStepOptions(),
                lookupService.loadEventTypeOptions(),
                lookupService.loadDeviceOptions(),
                lookupService.loadEventWarningLevels(),
            ]).then((values) => {
                this.options.steps = values[0].value
                this.options.eventTypes = values[1].value
                this.options.devices = values[2].value
                this.options.eventWarningLevels = values[3].value
            })
        },

        // ============ Form Submission ============
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then((success) => {
                if (!success) {
                    return
                }

                // Validate: phải có ít nhất một bước thao tác
                if (
                    !this.newWorkFlow.items ||
                    this.newWorkFlow.items.length === 0
                ) {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: this.$t(
                                'categories.workFlows.error.CreateFail'
                            ),
                            icon: 'AlertTriangleIcon',
                            variant: 'warning',
                            text:
                                this.$t(
                                    'categories.workFlows.error.itemsRequired'
                                ) || 'Phải có ít nhất một bước thao tác',
                        },
                    })
                    return
                }

                // Validate: không được trùng StepId
                if (this.hasDuplicateStepIds()) {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: this.$t(
                                'categories.workFlows.error.CreateFail'
                            ),
                            icon: 'AlertTriangleIcon',
                            variant: 'warning',
                            text:
                                this.$t(
                                    'categories.workFlows.error.duplicateStepId'
                                ) ||
                                'Công đoạn không được trùng lặp trong một dây chuyền',
                        },
                    })
                    return
                }

                const hasDuplicates = this.newWorkFlow.items.some((item) =>
                    this.isDuplicateCodeInScope(item.id)
                )

                if (hasDuplicates) {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: this.$t(
                                'categories.workFlows.error.CreateFail'
                            ),
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                            text:
                                this.$t(
                                    'categories.workFlows.error.WORKFLOW_ITEMS_DUPLICATE'
                                ) || 'Có mã code bị trùng lặp',
                        },
                    })
                    return
                }

                this.submitForm()
            })
        },

        async submitForm() {
            try {
                // Prepare data: map tempId/tempParentId for backend processing
                const preparedItems = this.newWorkFlow.items.map((item) => {
                    const tempId = item.id // Lưu temp ID từ frontend
                    const tempParentId = item.parentId || null // null nếu là parent node

                    if (item.parentId) {
                        // Child node: stepId will be auto-generated by backend
                        return {
                            ...item,
                            id: 0, // Reset ID để backend tạo mới
                            tempId: tempId,
                            tempParentId: tempParentId,
                            stepId: item.stepId || 0,
                            deviceId: item.deviceId ?? null,
                            parentId: 0, // Will be resolved by backend
                        }
                    }

                    // Parent node
                    return {
                        ...item,
                        id: 0, // Reset ID để backend tạo mới
                        tempId: tempId,
                        tempParentId: null,
                        parentId: 0, // Root level
                    }
                })

                const payload = {
                    ...this.newWorkFlow,
                    items: preparedItems,
                }

                await this.$services.post('/work-flows', payload)

                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Success.Create'),
                        icon: 'CheckIcon',
                        variant: 'success',
                    },
                })

                this.$router.push({ path: '/categories/workFlows/list' })
            } catch (error) {
                this.showErrorToast(error)
            }
        },

        showErrorToast(error) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(`categories.workFlows.error.CreateFail`),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(
                        `categories.workFlows.error.${error.message}`
                    ),
                },
            })
        },
    },
}
</script>

<style lang="scss" scoped>
.tree-view-container {
    .tree-list {
        .parent-node-wrapper {
            position: relative;
        }

        .children-list {
            margin-top: 0.5rem;
        }
    }

    .b-alert {
        font-size: 0.875rem;

        .sm-icon {
            vertical-align: middle;
        }
    }
}

.gap-1 {
    gap: 0.25rem;
}

@media (max-width: 768px) {
    .tree-view-container {
        .children-list {
            margin-left: 0.5rem !important;
        }
    }
}

@media (max-width: 576px) {
    .tree-view-container {
        .card-body {
            padding: 0.75rem;
        }
    }
}

.sortable-ghost {
    opacity: 0.4;
    background: #dbeafe;
}

.sortable-drag {
    opacity: 0.8;
    transform: rotate(2deg);
}
</style>
