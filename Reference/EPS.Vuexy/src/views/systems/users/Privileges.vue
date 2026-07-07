<template>
    <b-card no-body>
        <b-card-body>
            <b-form>
                <b-row style="min-height: 300px">
                    <b-col md="4">
                        <b-form-group
                            label=""
                            label-for="h-compId"
                        >
                            <treeselect
                                id="module"
                                ref="moduleTree"
                                v-model="moduleId"
                                :normalizer="normalizer"
                                :always-open="true"
                                :options="allModules"
                            />
                        </b-form-group>
                    </b-col>
                    <b-col md="8">
                        <b-form-group
                            v-if="selectedModule"
                            :label="selectedModule.name"
                        >
                            <b-form-checkbox-group v-model="userPrivileges">
                                <div
                                    v-if="
                                        selectedModule.children &&
                                        selectedModule.children.length > 0
                                    "
                                >
                                    <template>
                                        <div
                                            v-for="item in selectedModule.children"
                                            :key="item.code"
                                            class="mb-2"
                                        >
                                            <span style="font-weight: bold">{{
                                                item.name
                                            }}</span>
                                            <hr />
                                            <b-row>
                                                <b-col
                                                    v-for="privilege in getModulePrivileges(
                                                        item
                                                    )"
                                                    :key="privilege.id"
                                                    md="6"
                                                >
                                                    <b-form-checkbox
                                                        :value="privilege.id"
                                                    >
                                                        {{ privilege.text }}
                                                    </b-form-checkbox>
                                                </b-col>
                                            </b-row>
                                        </div>
                                    </template>
                                </div>
                                <div v-else>
                                    <hr />
                                    <b-row>
                                        <b-col
                                            v-for="privilege in getModulePrivileges(
                                                selectedModule
                                            )"
                                            :key="privilege.id"
                                            md="6"
                                        >
                                            <b-form-checkbox
                                                :value="privilege.id"
                                            >
                                                {{ privilege.text }}
                                            </b-form-checkbox>
                                        </b-col>
                                    </b-row>
                                </div>
                            </b-form-checkbox-group>
                        </b-form-group>
                    </b-col>
                </b-row>
                <b-row class="justify-content-center">
                    <b-col cols="auto">
                        <b-button
                            v-if="authorize(['ManageUser'])"
                            type="submit"
                            variant="primary"
                            class="mr-2"
                            @click.prevent="save"
                        >
                            {{ $t('Button.Save') }}
                        </b-button>
                        <b-button
                            :to="{ path: '/systems/users/list' }"
                            type="reset"
                            variant="outline-secondary"
                        >
                            {{ $t('Button.Cancel') }}
                        </b-button>
                    </b-col>
                </b-row>
            </b-form>
        </b-card-body>
    </b-card>
</template>

<script>
/* eslint-disable */
import navMenuItems from '@/navigation/horizontal'
import Treeselect from '@riophae/vue-treeselect'
import '@riophae/vue-treeselect/dist/vue-treeselect.css'
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    mixins: [authorizationMixin],
    components: {
        Treeselect,
    },
    data() {
        return {
            userPrivileges: [],
            moduleId: null,
        }
    },
    computed: {
        selectedModule() {
            if (!this.moduleId) return null

            function findModule(modules, moduleId) {
                for (var i = 0; i < modules.length; i++) {
                    if (modules[i].code == moduleId) {
                        return modules[i]
                    } else if (
                        modules[i].children &&
                        modules[i].children.length > 0
                    ) {
                        var nested = findModule(modules[i].children, moduleId)
                        if (nested) {
                            return nested
                        }
                    }
                }

                return null
            }
            var module = findModule(this.allModules, this.moduleId)

            return module
        },
        allModules() {
            this.$i18n.locale
            const clonedItems = JSON.parse(JSON.stringify(navMenuItems)).filter((x) => x.name)
            var result = []

            for (var i = 0; i < clonedItems.length; i++) {
                var item = clonedItems[i]
                this.translateModuleItem(item)

                if (
                    this.authorize(['ManageUser']) ||
                    this.authorize(['ViewUser'])
                ) {
                    result.push(item)
                    continue
                }
                if (item.code == 'system') {
                    if (item.items && item.items.length > 0) {
                        var _items = []
                        for (var j = 0; j < item.items.length; j++) {
                            let itemChild = item.items[j]
                            if (itemChild && itemChild.code == 'manage_roles') {
                            } else {
                                _items.push(itemChild)
                            }
                        }
                        item.items = _items
                    }
                }
                result.push(item)
            }
            return result
        },
    },
    created() {
        this.$services
            .get(`/users/${this.$route.params.userId}/privileges`)
            .then((response) => {
                this.userPrivileges = response.data
            })
    },
    methods: {
        normalizer(node) {
            return {
                id: node.code,
                label: node.name,
                children: node.children,
            }
        },
        translateModuleItem(item) {
            if (!item) return

            if (item.name) {
                const translated = this.$t(item.name)
                item.name = (translated && typeof translated === 'string')
                    ? translated
                    : item.name
            }

            if (item.children && Array.isArray(item.children)) {
                item.children.forEach(child => {
                    if (child && child.name) {
                        const translatedChild = this.$t(child.name)
                        child.name = (translatedChild && typeof translatedChild === 'string')
                            ? translatedChild
                            : child.name
                    }
                })
            }
        },
        getModulePrivileges(module) {
            let requiresPrivileges = module.requiresPrivileges || []
            let result = []

            requiresPrivileges.forEach((element) => {
                let item = {
                    id: element,
                    text: this.$t('Privilege.' + element),
                }

                result.push(item)
            })

            return result
        },
        save() {
            let data = this.userPrivileges.join(',')

            this.$services
                .put(
                    `/users/${this.$route.params.userId}/privileges?privileges=${data}`
                )
                .then((response) => {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: this.$t('System.User.Message.PrivilegeSuccess'),
                            icon: 'CheckIcon',
                            variant: 'success',
                        },
                    })
                    this.cancel()
                })
                .catch((error) => {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: this.$t('System.User.Message.PrivilegeFailure'),
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                            text: `${error.response.data.error}`,
                        },
                    })
                })
        },
        cancel() {
            this.$router.push({ path: '/systems/users/list' })
        },
    },
}
</script>

