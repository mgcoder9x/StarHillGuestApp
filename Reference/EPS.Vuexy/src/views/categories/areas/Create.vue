<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.areas.common.form.label.areaCode'
                                    )
                                "
                                label-for="create-area-code"
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
                                        id="create-area-code"
                                        v-model="newArea.code"
                                        :placeholder="
                                            $t(
                                                'categories.areas.common.form.placeholder.areaCode'
                                            )
                                        "
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
                                    label-for="create-area-name"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        id="create-area-name"
                                        v-model="newArea.name"
                                        :placeholder="
                                            $t(
                                                'categories.areas.common.form.placeholder.areaName'
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
                                :label="
                                    $t(
                                        'categories.areas.common.form.label.parentArea'
                                    )
                                "
                                label-for="create-area-parent"
                                label-cols-md="4"
                                :class="formGroupClass"
                            >
                                <tree-select
                                    id="create-area-parent"
                                    v-model="newArea.parentId"
                                    :options="treeAreas"
                                    label="text"
                                    :multiple="false"
                                    track-by="id"
                                    :disabled="disabledParent"
                                    :reduce="(item) => parseInt(item.id)"
                                    :placeholder="
                                        $t(
                                            'categories.areas.common.form.placeholder.parentArea'
                                        )
                                    "
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Device.Detail.Form.Function')"
                                label-for="h-camera-event-type"
                                label-cols-md="4"
                            >
                                <v-select
                                    v-model="newArea.functionIds"
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    :label="$i18n.locale === 'vi' ? 'text' : 'englishName'"
                                    :reduce="(item) => item.id"
                                    :options="listEventType"
                                    :multiple="true"
                                    :placeholder="
                                        this.$t(
                                            'common.form.placeholder.selectValue'
                                        )
                                    "
                                    @input="search"
                                >
                                </v-select>
                            </b-form-group>
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
                                    label-for="create-area-tree-index"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        id="create-area-tree-index"
                                        v-model="newArea.treeIndex"
                                        type="number"
                                        :min="0"
                                        :placeholder="
                                            $t(
                                                'categories.areas.common.form.label.treeIndex'
                                            )
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
                                :label="
                                    $t(
                                        'categories.areas.common.form.label.note'
                                    )
                                "
                                label-for="create-area-note"
                                label-cols-md="4"
                                :class="formGroupClass"
                            >
                                <b-form-textarea
                                    id="create-area-note"
                                    v-model="newArea.note"
                                    :placeholder="
                                        $t(
                                            'categories.areas.common.form.placeholder.note'
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
                                    @click="openMapPicker()"
                                >
                                    <Icon
                                        icon="ei:location"
                                        width="20"
                                        height="20"
                                    />
                                </b-button>
                                <LatLngPickerImage
                                    v-if="isShowImgMap"
                                    ref="mapPickerImage"
                                    :lat-lng="mapPicker.coordinate"
                                    :map-prop="mapPicker.mapProp"
                                    @input="setCoordinate"
                                />
                                <LatLngPicker
                                    v-else
                                    ref="mapPicker"
                                    :lat-lng="mapPicker.coordinate"
                                    :map-prop="mapPicker.mapProp"
                                    @input="setCoordinate"
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
                                    v-model="newArea.isWarehouse"
                                    switch
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageArea'])"
                            v-waves
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                        >
                            <Icon
                                icon="material-symbols:save-outline"
                                class="sm-icon"
                            />
                            <span class="ml-25">
                                {{ $t('common.button.save') }}
                            </span>
                        </b-button>
                        <b-button
                            v-waves
                            :to="{ path: '/categories/areas/list' }"
                            type="reset"
                            variant="secondary"
                            title="Cancel"
                            class="btn-120 mb-50 btn-hover-linear-secondary border-0"
                        >
                            <Icon icon="line-md:cancel" class="sm-icon" />
                            <span class="ml-25">
                                {{ $t('common.button.cancel') }}
                            </span>
                        </b-button>
                    </div>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
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
            treeAreas: null,
            newArea: {
                code: null,
                name: null,
                parentId: null,
                note: null,
                compId: null,
                isDelete: false,
                treeIndex: 0,
                functionIds: [],
                imgBase64: null,
                isWarehouse: false,
            },
            listEventType: [],
            companyId: null,
            companyImgPath: null,
        }
    },
    watch: {
        'newArea.parentId': function (val) {
            if (!val) {
                this.newArea.treeIndex = 0
                // this.newArea.treeIndex =
                //     this.treeAreas.find((x) => x.id == val).treeIndex + 1
            }
        },
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        disabledParent() {
            return this.$route.query?.parentId !== undefined
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
        await this.getTreeAreas()
        await this.loadCompany()
        this.loadEventType()
        this.newArea.parentId =
            this.$route.query?.parentId !== undefined
                ? this.$route.query?.parentId
                : null
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
                this.newArea.imgBase64 = reader.result
                this.newArea.fileExtension = fileExtension
            }

            reader.onerror = (error) => {
                console.error('Error converting file to Base64:', error)
            }
        },
        openMapPicker() {
            if (this.isShowImgMap) {
                this.$refs.mapPickerImage.toggleModal()
            } else {
                this.$refs.mapPicker.toggleModal()
            }
        },
        async setCoordinate(coordinate) {
            this.newArea.coordinates = `[${coordinate.lat}, ${coordinate.lng}, ${coordinate.zoom}]`
            this.mapPicker.coordinate.lat = coordinate.lat
            this.mapPicker.coordinate.lng = coordinate.lng
            this.mapPicker.coordinate.zoom = coordinate.zoom
            this.mapPicker.mapProp.zoom = this.mapPicker.coordinate.zoom
            this.mapPicker.mapProp.center = [coordinate.lat, coordinate.lng]
        },
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                this.listEventType = response.data.data
            })
        },
        async getTreeAreas() {
            try {
                const res = await this.$services.get('/lookup/areas-tree')
                this.treeAreas = TreeHelper.removeEmptyChildren(res.data.data)
            } catch (error) {
                console.log('error')
            }
        },
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then((success) => {
                if (!success) {
                    return
                    // handle validation errors...
                } else {
                    this.submitForm()
                }
            })
        },
        async submitForm() {
            try {
                this.newArea.code = this.newArea.code
                    ? this.newArea.code.trim()
                    : null
                this.newArea.name = this.newArea.name
                    ? this.newArea.name.trim()
                    : null
                this.newArea.note = this.newArea.note
                    ? this.newArea.note.trim()
                    : null
                const res = await this.$services.post('/areas', this.newArea)
                this.showSuccessToast(res.data)
                this.navigateToAreasList()
            } catch (error) {
                console.log(error)
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
        navigateToAreasList() {
            this.$router.push({ path: '/categories/areas/list' })
        },
    },
}
</script>

<style lang="scss"></style>
