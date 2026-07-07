<!-- eslint-disable vue/html-self-closing -->
<template>
    <b-container fluid class="p-0">
        <b-card>
            <validation-observer ref="rules">
                <b-form>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.areas.common.form.label.areaCode'
                                    )
                                "
                                label-for="h-area-code"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    name="AreaCode"
                                >
                                    <b-form-input
                                        id="h-area-code"
                                        v-model="updatedArea.code"
                                        :disabled="!editing"
                                        placeholder="Mã"
                                        :state="errors.length ? false : null"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                name="AreaName"
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.areas.common.form.label.areaName'
                                        )
                                    "
                                    label-for="h-area-name"
                                    label-cols-md="4"
                                    label-class="required"
                                >
                                    <b-form-input
                                        id="h-area-name"
                                        v-model="updatedArea.name"
                                        :disabled="!editing"
                                        :placeholder="
                                            $t(
                                                'categories.areas.common.form.label.areaName'
                                            )
                                        "
                                        :state="errors.length ? false : null"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                :rules="{ is_not: parseInt(areaId) }"
                                name="ParentAreaName"
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.areas.common.form.label.parentArea'
                                        )
                                    "
                                    label-for="h-area-name"
                                    label-cols-md="4"
                                    :class="formGroupClass"
                                >
                                    <tree-select
                                        v-model="updatedArea.parentId"
                                        :disabled="!editing"
                                        :options="treeAreas"
                                        label="text"
                                        :multiple="false"
                                        track-by="id"
                                        :reduce="(item) => parseInt(item.id)"
                                        :placeholder="
                                            $t(
                                                'categories.areas.common.form.placeholder.parentArea'
                                            )
                                        "
                                        :state="errors.length ? false : null"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                name="TreeIndex"
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.areas.common.form.label.treeIndex'
                                        )
                                    "
                                    label-for="detail-area-tree-index"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        id="detail-area-tree-index"
                                        v-model="updatedArea.treeIndex"
                                        type="number"
                                        :min="0"
                                        :disabled="!editing"
                                        :placeholder="
                                            $t(
                                                'categories.areas.common.form.label.treeIndex'
                                            )
                                        "
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Device.Detail.Form.Function')"
                                label-for="h-camera-event-type"
                                label-cols-md="4"
                            >
                                <v-select
                                    v-model="updatedArea.functionIds"
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    :label="$i18n.locale === 'vi' ? 'text' : 'englishName'"
                                    :disabled="!editing"
                                    :reduce="(item) => item.id"
                                    :options="listEventType"
                                    :multiple="true"
                                    :placeholder="
                                        this.$t(
                                            'common.form.placeholder.selectValue'
                                        )
                                    "
                                >
                                </v-select>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.areas.common.form.label.note'
                                    )
                                "
                                label-for="h-note"
                                label-cols-md="4"
                            >
                                <b-form-textarea
                                    id="h-note"
                                    v-model="updatedArea.note"
                                    :disabled="!editing"
                                    :placeholder="
                                        $t(
                                            'categories.areas.common.form.label.note'
                                        )
                                    "
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    this.$t(
                                        'categories.areas.common.form.label.coordinate'
                                    )
                                "
                                label-cols-md="4"
                            >
                                <b-button
                                    size="sm"
                                    variant="success"
                                    :disabled="!editing"
                                    @click="openMapPicker()"
                                >
                                    <Icon
                                        icon="ei:location"
                                        width="20"
                                        height="20"
                                    />
                                </b-button>
                                <LatLngPickerImage
                                    v-if="isShowImgMap && showMap"
                                    ref="mapPickerImage"
                                    :lat-lng="mapPicker.coordinate"
                                    :map-prop="mapPicker.mapProp"
                                    @input="setCoordinates"
                                />
                                <LatLngPicker
                                    v-if="!isShowImgMap && showMap"
                                    ref="mapPicker"
                                    :lat-lng="mapPicker.coordinate"
                                    :map-prop="mapPicker.mapProp"
                                    @input="setCoordinates"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.areas.common.form.label.isWarehouse'
                                    )
                                "
                                label-cols-md="4"
                            >
                                <b-form-checkbox
                                    v-model="updatedArea.isWareHouse"
                                    :disabled="!editing"
                                    switch
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col>
                            <div class="text-center">
                                <Transition mode="out-in">
                                    <b-button
                                        v-if="
                                            editing && authorize(['ManageArea'])
                                        "
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
                                        v-if="
                                            !editing &&
                                            authorize(['ManageArea'])
                                        "
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
                                    :to="{ path: '/categories/areas/list' }"
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
                        </b-col>
                    </b-row>
                </b-form>
            </validation-observer>
        </b-card>
    </b-container>
</template>

<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import TreeHelper from '@/utils/treeHelper'
import LatLngPickerImage from '@/components/LatLngPickerImage'
import LatLngPicker from '@/components/LatLngPicker'
import { $themeConfig } from '@themeConfig'

export default {
    mixins: [authorizationMixin],
    components: { LatLngPickerImage, LatLngPicker },
    data() {
        return {
            mapPicker: {
                coordinate: {
                    lat: null,
                    lng: null,
                    zoom: null,
                },
                mapProp: {
                    urlImgOverlay: null,
                    center: [0, 0],
                    zoom: -1,
                },
            },
            isShowImgMap: false,
            treeAreas: [],
            updatedArea: {
                code: '',
                name: '',
                parentId: null,
                note: '',
                treeIndex: 0,
                functionIds: [],
                coordinates: null,
                mapType: null,
                isWareHouse: false,
            },
            editing: false,
            showMap: false,
            listEventType: [],
            maptypeOptions: [
                {
                    value: 0,
                    text: this.$t(
                        'categories.areas.common.form.label.maptypeValue.map'
                    ),
                },
                {
                    value: 1,
                    text: this.$t(
                        'categories.areas.common.form.label.maptypeValue.img'
                    ),
                },
            ],
            selectedFunctions: null,
            companyId: null,
            companyImgPath: null,
        }
    },
    computed: {
        areaId() {
            return this.$route.params.areaId
        },
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        mapImg() {
            if (!this.companyImgPath) return null
            const url =
                process.env.NODE_ENV === 'development'
                    ? $themeConfig.app.apiURLDev
                    : $themeConfig.app.apiURL
            return url + '/MapImg/' + this.companyImgPath
        },
    },
    async created() {
        this.companyId = JSON.parse(localStorage.getItem('userData'))?.companyId
        await this.loadEventType()
        await this.getTreeAreas()
        await this.loadCompany()
        await this.getArea()
    },
    methods: {
        async loadCompany() {
            try {
                if (this.companyId) {
                    const response = await this.$services.get(`/company/${this.companyId}`)
                    const companyData = response.data?.data || response.data
                    this.companyImgPath = companyData.imgPath
                    
                    // Set ảnh vào map nếu MapType là 2 (Bản đồ ảnh)
                    if (companyData.mapType === 2 && this.companyImgPath) {
                        this.mapPicker.mapProp.urlImgOverlay = this.mapImg
                        this.isShowImgMap = true
                    } else {
                        this.isShowImgMap = false
                        this.mapPicker.mapProp.center = [16.964332450605063, 105.98940145630186]
                        this.mapPicker.mapProp.zoom = 5
                    }
                }
            } catch (error) {
                console.error('Error loading company:', error)
            }
        },
        convertToBase64(file) {
            if (!file) return

            const reader = new FileReader()
            reader.readAsDataURL(file)

            reader.onload = () => {
                const fileExtension = file.name.split('.').pop() // Lấy đuôi file từ tên file
                this.updatedArea.imgBase64 = reader.result
                this.updatedArea.fileExtension = fileExtension
            }

            reader.onerror = (error) => {
                console.error('Error converting file to Base64:', error)
            }
        },
        openMapPicker() {
            this.showMap = true
            this.$nextTick(() => {
                if (this.isShowImgMap) {
                    this.$refs.mapPickerImage.toggleModal()
                } else {
                    this.$refs.mapPicker.toggleModal()
                }
            })
        },
        async setCoordinates(coordinate) {
            this.updatedArea.coordinates = `[${coordinate.lat}, ${coordinate.lng}, ${coordinate.zoom}]`
            this.mapPicker.coordinate.lat = coordinate.lat
            this.mapPicker.coordinate.lng = coordinate.lng
            this.mapPicker.coordinate.zoom = coordinate.zoom
            this.mapPicker.mapProp.zoom = coordinate.zoom
            this.mapPicker.mapProp.center = [coordinate.lat, coordinate.lng]
        },
        async loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                this.listEventType = response.data.data
            })
        },
        async getTreeAreas() {
            try {
                const res = await this.$services.get('/lookup/areas-tree')
                // Remove current area and its children from the tree
                const areaRemoveOwn = this.removeNodeAndChildren(
                    res.data.data,
                    parseInt(this.areaId)
                )
                this.treeAreas = TreeHelper.removeEmptyChildren(areaRemoveOwn)
            } catch (error) {
                console.error('Error in getTreeAreas', error)
            }
        },

        removeNodeAndChildren(tree, nodeIdToRemove) {
            return tree.filter((node) => {
                // If this is the node to remove, exclude it and all its children
                if (node.id === nodeIdToRemove) {
                    return false
                }

                // If this node has children, recursively filter them
                if (node.children && node.children.length > 0) {
                    node.children = this.removeNodeAndChildren(
                        node.children,
                        nodeIdToRemove
                    )
                }

                return true
            })
        },

        async getArea() {
            try {
                const res = await this.$services.get(`/areas/${this.areaId}`)
                this.updatedArea = res.data.data
                this.updatedArea.functionIds = this.updatedArea.functionIds.map(
                    (element) => {
                        element = element.toString()
                        return element // Trả về phần tử đã được chỉnh sửa
                    }
                )
                console.log('data: ', res.data.data)
                console.log('updatedArea: ', this.updatedArea)
                
                // Parse và set tọa độ hiện tại
                if (this.updatedArea.coordinates) {
                    const coordinates = JSON.parse(this.updatedArea.coordinates)
                    this.mapPicker.coordinate = {
                        lat: coordinates[0],
                        lng: coordinates[1],
                        zoom: coordinates[2],
                    }
                    this.setCoordinates({
                        lat: coordinates[0],
                        lng: coordinates[1],
                        zoom: coordinates[2],
                    })
                }
            } catch (error) {
                console.log(error)
            }
        },
        validateAndSubmitForm() {
            this.$refs.rules.validate().then((isValid) => {
                if (isValid) {
                    this.submitForm()
                }
            })
        },
        async submitForm() {
            try {
                this.updatedArea.code = this.updatedArea.code
                    ? this.updatedArea.code.trim()
                    : null
                this.updatedArea.name = this.updatedArea.name
                    ? this.updatedArea.name.trim()
                    : null
                this.updatedArea.note = this.updatedArea.note
                    ? this.updatedArea.note.trim()
                    : null
                const res = await this.$services.put(
                    `/areas/${this.$route.params.areaId}`,
                    this.updatedArea
                )
                this.showSuccessToast(res.data)
                this.stopEdit()
            } catch (error) {
                this.showErrorToast(error)
            }
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(`categories.areas.error.${data.errorCode}`),
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
        showErrorToast(error) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(
                        'categories.waterWarning.Label.notification.error'
                    ),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(`${error.message}`),
                },
            })
        },
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getArea()
        },
    },
}
</script>

<style lang="scss"></style>
