<template>
    <validation-observer ref="rules">
        <b-container fluid>
            <b-card>
                <b-form @submit.prevent="onSubmit">
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
                                        v-model.trim="updatedWorkFlow.name"
                                        :disabled="!editing"
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
                                        v-model.trim="updatedWorkFlow.code"
                                        :disabled="!editing"
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
                                        v-model="updatedWorkFlow.status"
                                        :value="1"
                                        :unchecked-value="0"
                                        switch
                                        class="custom-control-primary"
                                        :disabled="!editing"
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
                                        v-if="editing"
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
                                            :disabled="!editing"
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
                                            :disabled="!editing"
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
                                                :disabled="!editing"
                                                @update:child="updateChildNode"
                                                @remove="removeNode"
                                            />
                                        </draggable>
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
                        <Transition mode="out-in">
                            <b-button
                                v-if="editing && authorize(['ManageWorkFlow'])"
                                v-waves
                                type="button"
                                variant="primary"
                                class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                                @click="validateAndSubmitForm"
                            >
                                <Icon
                                    icon="material-symbols:save-outline"
                                    class="sm-icon"
                                />
                                <span class="ml-50">
                                    {{ $t('common.button.save') }}
                                </span>
                            </b-button>
                            <b-button
                                v-if="!editing && authorize(['ManageWorkFlow'])"
                                v-waves
                                type="button"
                                variant="primary"
                                class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                                @click="startEdit"
                            >
                                <Icon
                                    icon="line-md:edit-twotone"
                                    class="sm-icon"
                                />
                                <span class="ml-25">
                                    {{ $t('common.button.edit') }}
                                </span>
                            </b-button>
                        </Transition>
                        <b-button
                            v-if="!editing"
                            v-waves
                            :to="{
                                path: '/categories/workFlows/list',
                            }"
                            type="button"
                            variant="outline-secondary"
                            class="mx-50 mb-50 btn-120 btn-hover-linear-secondary border-0"
                        >
                            <Icon
                                icon="line-md:arrow-small-left"
                                class="sm-icon"
                            />
                            <span class="ml-25">
                                {{ $t('common.button.back') }}
                            </span>
                        </b-button>
                        <b-button
                            v-if="editing"
                            v-waves
                            type="button"
                            class="mx-50 mb-50 btn-120"
                            variant="outline-secondary btn-hover-linear-secondary border-0"
                            @click="stopEdit"
                        >
                            <Icon icon="mdi:cancel" class="sm-icon" />
                            <span class="ml-50">
                                {{ $t('common.button.cancel') }}
                            </span>
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
import { createPopper } from '@popperjs/core'
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
            updatedWorkFlow: {
                items: [],
                name: null,
                code: null,
                status: null,
                compId: null,
            },
            options: {
                steps: [],
                devices: [],
                eventTypes: [],
                eventWarningLevels: [],
            },
            editing: false,
        }
    },
    computed: {
        workFlowId() {
            return this.$route.params.id
        },
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
        // Tree-view computed properties (same as Create.vue)
        parentNodes() {
            const parents = this.updatedWorkFlow.items.filter(
                (item) => !item.parentId || item.parentId === 0
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
        this.updatedWorkFlow.compId = accessToken.companyId
        await this.loadOptions()
        await this.getWorkFlow()
    },
    methods: {
        // ============ Parent-Child Node Management ============
        getChildren(parentId) {
            return this.updatedWorkFlow.items.filter(
                (item) => item.parentId === parentId
            )
        },

        hasParentCompleteData(parent) {
            return Boolean(
                parent.deviceId || parent.eventTypeId || parent.warningLevelId
            )
        },

        addParentNode() {

            this.updatedWorkFlow.items.push({
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
            console.log('Adding parent to ', this.updatedWorkFlow)
        },

        addChildNode(parentId) {
            const parent = this.updatedWorkFlow.items.find(
                (i) => i.id === parentId
            )
            this.emptyWhenHasChild(parent)
            const newChild = {
                id: this.generateId(),
                parentId: parentId,
                name: null,
                code: null,
                // StepId will be auto-generated by backend
                stepId: null,
                deviceId: null,
                eventTypeId: null,
                warningLevelId: null,
                estimateTime: null,
            }
            this.updatedWorkFlow.items.push(newChild)
            if (!parent._children) parent._children = []
            parent._children.push(newChild)
            console.log('Adding child to parentId', this.updatedWorkFlow)
        },
        emptyWhenHasChild(parent) {
            parent.isExpanded = true
            parent.eventTypeId = null
            parent.warningLevelId = null
            parent.deviceId = null
            parent.estimateTime = null
        },
        updateParentNode(updatedParent) {
            const idx = this.updatedWorkFlow.items.findIndex(
                (i) => i.id === updatedParent.id
            )
            if (idx !== -1) {
                const oldChildren = this.updatedWorkFlow.items[idx]._children
                this.$set(this.updatedWorkFlow.items, idx, {
                    ...updatedParent,
                    _children: oldChildren,
                })
            }
        },

        updateChildNode(updatedChild) {
            const idx = this.updatedWorkFlow.items.findIndex(
                (i) => i.id === updatedChild.id
            )
            if (idx !== -1) {
                this.$set(this.updatedWorkFlow.items, idx, updatedChild)
            }
        },

        toggleExpand(nodeId) {
            const node = this.updatedWorkFlow.items.find((i) => i.id === nodeId)
            if (node) {
                this.$set(node, 'isExpanded', !node.isExpanded)
            }
        },

        removeNode(nodeId) {
            const childrenIds = this.getChildren(nodeId).map((c) => c.id)
            if (childrenIds.length > 0 && parseInt(nodeId)) {
                this.showErrorToast({ message: 'D_WORKFLOW_502' })
                return
            }
            childrenIds.forEach((childId) => this.removeNode(childId))
            const index = this.updatedWorkFlow.items.findIndex(
                (item) => item.id === nodeId
            )
            if (index !== -1) {
                const nodeToRemove = this.updatedWorkFlow.items[index]
                if (nodeToRemove.parentId) {
                    const parent = this.updatedWorkFlow.items.find((i) => i.id === nodeToRemove.parentId)
                    if (parent && parent._children) {
                        parent._children = parent._children.filter(c => c.id !== nodeId)
                    }
                }
                this.updatedWorkFlow.items.splice(index, 1)
            }
        },

        generateId() {
            return `temp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
        },

        onChildReorder(parentId) {
            const parent = this.updatedWorkFlow.items.find(
                (i) => i.id === parentId
            )
            if (parent && parent._children) {
                this.updatedWorkFlow.items = this.updatedWorkFlow.items.filter(
                    (item) => item.parentId !== parentId
                )
                parent._children.forEach((child) => {
                    this.updatedWorkFlow.items.push(child)
                })
            }
        },

        // ============ Aggregation & Metrics ============
        getParentEstimateTime(parentId) {
            const children = this.getChildren(parentId)
            if (children.length === 0)
                return parseInt(
                    this.updatedWorkFlow.items.find((i) => i.id === parentId)
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
            const node = this.updatedWorkFlow.items.find((n) => n.id === nodeId)
            if (!node || !node.code || node.code.trim() === '') return false

            const siblings = this.updatedWorkFlow.items.filter(
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
            const stepIds = this.updatedWorkFlow.items
                .filter((i) => i.stepId && (i.parentId === 0 || !i.parentId))
                .map((i) => i.stepId)
            console.log('Checking duplicate stepIds', stepIds)
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

        async getWorkFlow() {
            try {
                const response = await this.$services.get(
                    `/work-flows/${this.workFlowId}`
                )
                const workFlow = response.data.data

                // Initialize items with tree structure properties
                if (workFlow.items && workFlow.items.length > 0) {
                    workFlow.items = workFlow.items.map((item) => ({
                        ...item,
                        id: item.id || this.generateId(),
                        parentId: item.parentId || 0,
                        isExpanded: workFlow.items.some(child => child.parentId === item.id),
                        _children: [],
                    }))

                    // Populate _children for each parent node
                    workFlow.items.forEach(item => {
                        if (item.parentId === 0) {
                            item._children = workFlow.items.filter(child => child.parentId === item.id)
                        }
                    })
                } else {
                    workFlow.items = []
                }

                this.updatedWorkFlow = workFlow
            } catch (error) {
                console.error('error', error)
            }
        },
        onSubmit(e) {
            e.preventDefault()
        },
        validateAndSubmitForm() {
            console.log('Validating form with data:', this.updatedWorkFlow)
            // Validate: phải có ít nhất một bước thao tác
            if (
                !this.updatedWorkFlow.items ||
                this.updatedWorkFlow.items.length === 0
            ) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('categories.workFlows.error.UpdateFail'),
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
                        title: this.$t('categories.workFlows.error.UpdateFail'),
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

            // Check for duplicate codes
            const hasDuplicates = this.updatedWorkFlow.items.some((item) =>
                this.isDuplicateCodeInScope(item.id)
            )

            if (hasDuplicates) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('categories.workFlows.error.UpdateFail'),
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

            this.$refs.rules.validate().then((success) => {
                if (!success) {
                    return
                }
                this.submitForm()
            })
        },
        async submitForm() {
            try {
                // Prepare items: handle tempId for new items, preserve parentId
                const preparedItems = this.updatedWorkFlow.items.map(
                    (item, index) => {
                        const isNewItem =
                            !item.id || typeof item.id === 'string'

                        if (isNewItem) {
                            return {
                                ...item,
                                id: 0,
                                tempId:
                                    typeof item.id === 'string'
                                        ? item.id
                                        : `temp_${Date.now()}_${index}`,
                                tempParentId:
                                    typeof item.parentId === 'string'
                                        ? item.parentId
                                        : null,
                                parentId:
                                    typeof item.parentId === 'number'
                                        ? item.parentId
                                        : 0,
                            }
                        }

                        return {
                            ...item,
                            parentId: item.parentId || 0,
                        }
                    }
                )

                const payload = {
                    ...this.updatedWorkFlow,
                    items: preparedItems,
                }

                await this.$services.put(
                    `/work-flows/${this.workFlowId}`,
                    payload
                )

                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Success.Update'),
                        icon: 'CheckIcon',
                        variant: 'success',
                    },
                })

                this.stopEdit()
            } catch (error) {
                this.showErrorToast(error)
            }
        },
        showErrorToast(error) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(`categories.workFlows.error.UpdateFail`),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(
                        `categories.workFlows.error.${error.message}`
                    ),
                },
            })
        },
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getWorkFlow()
        },
    },
}
</script>

<style lang="scss" scoped>
// .tree-view-container {
//     border: 1px solid #e9ecef;
//     border-radius: 0.5rem;
//     padding: 1rem;
//     background-color: #fafafa;

//     .dark-layout & {
//         background-color: #283046;
//         border-color: #404656;
//     }
// }

// .tree-list {
//     .parent-node-wrapper {
//         background: #fff;
//         border-radius: 0.5rem;
//         box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);

//         .dark-layout & {
//             background: #161d31;
//         }
//     }
// }

// .children-list {
//     margin-left: 2rem;
//     padding-left: 1rem;
//     border-left: 2px solid #7367f0;

//     .dark-layout & {
//         border-left-color: #7367f0;
//     }
// }
</style>
