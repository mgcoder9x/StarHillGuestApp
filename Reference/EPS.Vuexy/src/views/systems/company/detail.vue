<template>
    <div>
        <validation-observer ref="rules">
            <!-- Card container -->
            <b-card>
                <!-- Card header -->
                <!-- Card body -->
                <b-row>
                    <b-col lg="12">
                        <!-- Form -->
                        <b-form @submit.prevent="save">
                            <b-row>
                                <!-- Profile-->
                                <b-col md="6" offset="3">
                                    <b-row>
                                        <!-- Company Code -->
                                        <b-col>
                                            <ValidationProvider
                                                :rules="`required`"
                                                v-slot="{ errors }"
                                                :name="
                                                    $t(
                                                        'System.Company.Detail.Label.Code'
                                                    )
                                                "
                                            >
                                                <b-form-group
                                                    :label="
                                                        $t(
                                                            'System.Company.Detail.Label.Code'
                                                        )
                                                    "
                                                    :label-cols="4"
                                                    :horizontal="true"
                                                    label-align-md="left"
                                                    :label-class="
                                                        editing
                                                            ? 'required'
                                                            : ''
                                                    "
                                                >
                                                    <b-form-input
                                                        type="text"
                                                        id="txt_code"
                                                        v-model="company.code"
                                                        :disabled="!editing"
                                                    >
                                                    </b-form-input>
                                                    <span
                                                        class="validate-error"
                                                    >
                                                        {{ errors[0] }}</span
                                                    >
                                                </b-form-group>
                                            </ValidationProvider>
                                        </b-col>
                                    </b-row>
                                    <!-- Company name -->
                                    <b-row>
                                        <b-col>
                                            <ValidationProvider
                                                :rules="`required`"
                                                v-slot="{ errors }"
                                                :name="
                                                    $t(
                                                        'System.Company.Detail.Label.Name'
                                                    )
                                                "
                                            >
                                                <b-form-group
                                                    :label="
                                                        $t(
                                                            'System.Company.Detail.Label.Name'
                                                        )
                                                    "
                                                    :label-cols="4"
                                                    :horizontal="true"
                                                    label-align-md="left"
                                                    :label-class="
                                                        editing
                                                            ? 'required'
                                                            : ''
                                                    "
                                                >
                                                    <b-form-input
                                                        type="text"
                                                        id="txt_name"
                                                        v-model="company.name"
                                                        :disabled="!editing"
                                                    >
                                                    </b-form-input>
                                                    <span
                                                        class="validate-error"
                                                    >
                                                        {{ errors[0] }}</span
                                                    >
                                                </b-form-group>
                                            </ValidationProvider>
                                        </b-col>
                                    </b-row>
                                    <!-- Parent Company -->
                                    <b-row>
                                        <b-col>
                                            <b-form-group
                                                :label="
                                                    this.$t(
                                                        'System.Company.Detail.Label.Parent'
                                                    )
                                                "
                                                :label-cols="4"
                                                :horizontal="true"
                                                label-align-md="left"
                                            >
                                                <Treeselect
                                                    v-if="editing"
                                                    :multiple="false"
                                                    :options="companyTree"
                                                    :reduce="(item) => item.id"
                                                    v-model="company.parentId"
                                                />
                                                <label
                                                    class="col-form-label"
                                                    v-if="!editing"
                                                    >{{
                                                        company.parentName
                                                    }}</label
                                                >
                                            </b-form-group>
                                        </b-col>
                                    </b-row>
                                    <!-- Loại bản đồ -->
                                    <b-row v-if="editing || company.mapType">
                                        <b-col>
                                            <b-form-group
                                                :label="$t('System.Company.Detail.Label.MapType')"
                                                :label-cols="4"
                                                :horizontal="true"
                                                label-align-md="left"
                                            >
                                                <b-form-radio-group
                                                    v-model="company.mapType"
                                                    :options="[{text: $t('System.Company.Detail.Label.MapTypeDigital'), value: 1}, {text: $t('System.Company.Detail.Label.MapTypeImage'), value: 2}]"
                                                    :disabled="!editing"
                                                ></b-form-radio-group>
                                            </b-form-group>
                                        </b-col>
                                    </b-row>
                                    <!-- Map Img -->
                                    <b-row v-if="company.mapType === 2">
                                        <b-col>
                                            <b-form-group
                                                :label="$t('Button.ImageFile')"
                                                :label-cols="4"
                                                :horizontal="true"
                                                label-align-md="left"
                                            >
                                                <b-form-file
                                                    v-if="editing"
                                                    v-model="selectedFile"
                                                    @change="onFileChange"
                                                    accept="image/*"
                                                    placeholder="Chọn file..."
                                                >
                                                </b-form-file>
                                                <!-- Hiển thị ảnh nếu có dữ liệu Base64 -->
                                                <b-img
                                                    v-if="company.ImgBase64 || mapImg"
                                                    :key="company.ImgBase64 || mapImg"
                                                    :src="company.ImgBase64 || mapImg"
                                                    class="preview-image mt-2"
                                                    fluid
                                                    rounded
                                                    style="max-height: 200px;"
                                                />
                                            </b-form-group>
                                        </b-col>
                                    </b-row>

                                    <b-row>
                                        <b-col>
                                            <LatLngPicker
                                                ref="mapPickerNormal"
                                                v-if="showMap && company.mapType === 1"
                                                :lat-lng="mapPicker.coordinate"
                                                @input="setCoordinates"
                                            />
                                            <LatLngPickerImage
                                                ref="mapPickerImage"
                                                v-if="showMap && company.mapType === 2"
                                                :lat-lng="mapPicker.coordinate"
                                                :map-prop="mapPicker.mapProp"
                                                :isEditing="!editing"
                                                @input="setCoordinates"
                                            />
                                        </b-col>
                                    </b-row>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col md="12">
                                    <div class="text-center">
                                        <b-button
                                            variant="info"
                                            @click="openMapPicker()"
                                        >
                                            {{ $t('Button.Coordinate') }}
                                        </b-button>
                                        <b-button
                                            type="button"
                                            variant="warning"
                                            @click="showTestZalo = true"
                                            v-if="editing"
                                            >{{
                                                $t('Button.CheckZalo')
                                            }}</b-button
                                        >
                                        <b-button
                                            type="button"
                                            variant="success"
                                            @click="showConfigZalo = true"
                                            v-if="editing"
                                        >
                                            {{
                                                $t('Button.ZaloConfig')
                                            }}</b-button
                                        >
                                        <b-button
                                            type="button"
                                            variant="primary"
                                            @click="edit"
                                            v-if="
                                                !editing &&
                                                authorize(['ManageCompany'])
                                            "
                                        >
                                            {{ $t('Button.Edit') }}</b-button
                                        >
                                        <b-button
                                            type="button"
                                            variant="outline-secondary"
                                            @click="back"
                                            v-if="!editing"
                                        >
                                            {{ $t('Button.Back') }}</b-button
                                        >
                                        <b-button
                                            type="submit"
                                            variant="primary"
                                            v-if="editing"
                                        >
                                            {{ $t('Button.Save') }}</b-button
                                        >
                                        <b-button
                                            type="button"
                                            variant="outline-secondary"
                                            @click="cancel"
                                            v-if="editing"
                                        >
                                            {{ $t('Button.Cancel') }}</b-button
                                        >
                                    </div>
                                </b-col>
                            </b-row>
                        </b-form>
                    </b-col>
                </b-row>
            </b-card>
        </validation-observer>
        <validation-observer ref="zaloForm">
            <b-modal
                v-model="showConfigZalo"
                :title="$t('Button.ZaloConfig')"
                :ok-title="$t('Button.Cancel')"
                hide-header-close
                size="lg"
            >
                <b-col>
                    <validation-provider
                        :rules="`required`"
                        v-slot="{ errors }"
                        name="App id"
                    >
                        <b-form-group
                            label="App id"
                            :label-cols="4"
                            :horizontal="true"
                            label-align-md="left"
                            label-class="required"
                        >
                            <b-form-input
                                type="text"
                                autocomplete="off"
                                id="txt_app_id"
                                v-model="company.zaloConfig.app_id"
                            >
                            </b-form-input>
                            <span class="validate-error"> {{ errors[0] }}</span>
                        </b-form-group>
                    </validation-provider>
                </b-col>

                <b-col>
                    <validation-provider
                        :rules="`required`"
                        v-slot="{ errors }"
                        name="Secret key"
                    >
                        <b-form-group
                            label="Secret key"
                            :label-cols="4"
                            :horizontal="true"
                            label-align-md="left"
                            label-class="required"
                        >
                            <b-form-input
                                type="password"
                                autocomplete="new-password"
                                id="txt_secret_key"
                                v-model="company.zaloConfig.secret_key"
                            >
                            </b-form-input>
                            <span class="validate-error"> {{ errors[0] }}</span>
                        </b-form-group>
                    </validation-provider>
                </b-col>

                <b-col>
                    <validation-provider
                        :rules="`required`"
                        v-slot="{ errors }"
                        name="Access token"
                    >
                        <b-form-group
                            label="Access token"
                            :label-cols="4"
                            :horizontal="true"
                            label-align-md="left"
                            label-class="required"
                        >
                            <b-form-input
                                type="password"
                                autocomplete="new-password"
                                id="txt_access_token"
                                v-model="company.zaloConfig.access_token"
                            >
                            </b-form-input>
                            <span class="validate-error"> {{ errors[0] }}</span>
                        </b-form-group>
                    </validation-provider>
                </b-col>

                <b-col>
                    <validation-provider
                        :rules="`required`"
                        v-slot="{ errors }"
                        name="Refresh token"
                    >
                        <b-form-group
                            label="Refresh token"
                            :label-cols="4"
                            :horizontal="true"
                            label-align-md="left"
                            label-class="required"
                        >
                            <b-form-input
                                type="password"
                                autocomplete="new-password"
                                id="txt_refresh_token"
                                v-model="company.zaloConfig.refresh_token"
                            >
                            </b-form-input>
                            <span class="validate-error"> {{ errors[0] }}</span>
                        </b-form-group>
                    </validation-provider>
                </b-col>
                <template v-slot:modal-footer>
                    <b-button variant="primary" @click="saveZaloConfig">{{
                        $t('Button.Save')
                    }}</b-button>
                    <b-button
                        variant="secondary"
                        @click="showConfigZalo = false"
                        >{{ $t('Button.Cancel') }}</b-button
                    >
                </template>
            </b-modal>
        </validation-observer>
        <validation-observer ref="zaloTest">
            <b-modal
                v-model="showTestZalo"
                :title="$t('Button.CheckZalo')"
                :ok-title="$t('Button.Cancel')"
                hide-header-close
                size="lg"
            >
                <b-col>
                    <validation-provider
                        :rules="`required`"
                        v-slot="{ errors }"
                        name="Zalo id"
                    >
                        <b-form-group
                            label="Zalo id"
                            :label-cols="4"
                            :horizontal="true"
                            label-align-md="left"
                            label-class="required"
                        >
                            <b-form-input
                                type="text"
                                autocomplete="off"
                                id="txt_app_id"
                                v-model="zalo_id"
                            >
                            </b-form-input>
                            <span class="validate-error"> {{ errors[0] }}</span>
                        </b-form-group>
                    </validation-provider>
                </b-col>

                <template v-slot:modal-footer>
                    <b-button variant="primary" @click="testZaloConfig">{{
                        $t('Button.CheckZalo')
                    }}</b-button>
                    <b-button
                        variant="secondary"
                        @click="showTestZalo = false"
                        >{{ $t('Button.Cancel') }}</b-button
                    >
                </template>
            </b-modal>
        </validation-observer>
    </div>
</template>
<script>
import TreeHelper from '@/utils/treeHelper'
import Treeselect from '@riophae/vue-treeselect'
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import LatLngPicker from '@/components/LatLngPicker'
import LatLngPickerImage from '@/components/LatLngPickerImage.vue'
import { $themeConfig } from '@themeConfig'

export default {
    mixins: [authorizationMixin],
    components: {
        Treeselect,
        TreeHelper,
        LatLngPicker,
        LatLngPickerImage
    },
    data() {
        return {
            mapPicker: {
                coordinate: {
                    lat: 21.0173,
                    lng: 105.8544,
                    zoom: 6,
                },
                mapProp: {
                    center: [0, 0],
                    zoom: -1,
                    urlImgOverlay: '',
                    bounds: [],
                    opacity: 1,
                },
            },
            showMap: false,
            showTestZalo: false,
            showConfigZalo: false,
            company: {
                code: null,
                name: null,
                parentId: null,
                parentName: null,
                mapType: 1,
                ImgBase64: null,
                fileExtension: null,
                zaloConfig: {
                    app_id: null,
                    secret_key: null,
                    access_token: null,
                    refresh_token: null,
                },
                coordinates: null
            },
            zalo_id: null,
            companyId: null,
            editing: false,
            companyTree: [],
            selectedFile: null,
        }
    },
    watch:{
        showTestZalo(newValue){
            if(!newValue){
                this.zalo_id = null
            }
        }
    },
    computed: {
        mapImg() {
            if (!this.company.imgPath) return null
            const url =
                process.env.NODE_ENV === 'development'
                    ? $themeConfig.app.apiURLDev
                    : $themeConfig.app.apiURL
            return url + '/MapImg/' + this.company.imgPath
        },
    },
    created() {
        this.companyId = this.$route.params.companyId
        this.loadCompanyDetail()
        this.loadCompanyTree()
    },
    methods: {
        onFileChange(event) {
            const file = event.target.files[0]
            if (file) {
                const reader = new FileReader()
                reader.onload = (e) => {
                    const fileExtension = file.name.split('.').pop()
                    this.$set(this.company, 'ImgBase64', e.target.result)
                    this.$set(this.company, 'fileExtension', fileExtension)
                    this.$set(this.company, 'imgPath', null)
                    this.mapPicker.mapProp.urlImgOverlay = e.target.result
                }
                reader.readAsDataURL(file)
            }
        },
        openMapPicker() {
            this.showMap = true
            this.$nextTick(() => {
                if (this.company.mapType === 1) {
                    this.$refs.mapPickerNormal.toggleModal()
                } else {
                    this.$refs.mapPickerImage.toggleModal()
                }
            })
        },
        async setCoordinates(coordinate) {
            this.company.coordinates = `[${coordinate.lat}, ${coordinate.lng}, ${coordinate.zoom}]`
            this.mapPicker.coordinate = {
                lat: coordinate.lat,
                lng: coordinate.lng,
                zoom: coordinate.zoom
            }
            this.mapPicker.mapProp.zoom = coordinate.zoom
            this.mapPicker.mapProp.center = [coordinate.lat, coordinate.lng]
        },
        saveZaloConfig() {
            var vm = this
            this.$refs.zaloForm.validate().then((success) => {
                if (success) {
                    vm.showConfigZalo = false
                }
            })
        },
        testZaloConfig() {
            this.$refs.zaloTest.validate().then((success) => {
                if (success) {
                    this.$services
                        .post(
                            `/company/checkZalo/${this.companyId}/${this.zalo_id}`
                        )
                        .then((response) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.ValidInformation'),
                                    icon: 'CheckIcon',
                                    variant:'success',
                                },
                            })
                            this.showTestZalo = false
                        })
                        .catch((error) => {
                            this.showNotification(false, 'Error.NotValid')
                        })
                }
            })
        },
        loadCompanyDetail() {
            return this.$services
                .get(`/company/${this.companyId}`)
                .then((response) => {
                    this.$refs.rules.reset()
                    this.company = response.data
                    if (!this.company.mapType) {
                        this.$set(this.company, 'mapType', 1)
                    }
                    
                    console.log('Company data:', this.company)
                    console.log('company.imgPath:', this.company.imgPath)
                    console.log('mapImg:', this.mapImg)
                    
                    this.company.zaloConfig = {
                        zalo_id: null,
                        app_id: null,
                        secret_key: null,
                        access_token: null,
                        refresh_token: null,
                    }
                    
                    // Load image to mapProp
                    if (this.company.ImgBase64) {
                        this.mapPicker.mapProp.urlImgOverlay = this.company.ImgBase64
                    } else if (this.company.imgPath) {
                        this.mapPicker.mapProp.urlImgOverlay = this.mapImg
                    }
                    
                    // Load coordinates
                    if (this.company.coordinates) {
                        const coordinates = JSON.parse(this.company.coordinates)
                        this.setCoordinates({
                            lat: coordinates[0],
                            lng: coordinates[1],
                            zoom: coordinates[2],
                        })
                    }
                })
        },
        //Danh sách cong ty - tree view
        loadCompanyTree() {
            return this.$services
                .get(`/lookup/company-tree-not-comp`)
                .then((response) => {
                    this.companyTree = TreeHelper.removeEmptyChildren(
                        response.data
                    )
                })
                .catch((error) => {
                    console.log(error)
                })
        },

        edit() {
            this.editing = true
        },

        back() {
            this.$router.push({ path: '/systems/company/list' })
        },

        cancel() {
            this.loadCompanyDetail()
            this.editing = false
        },

        save() {
            this.company.name = this.company.name
                ? this.company.name.trim()
                : null
            this.company.code = this.company.code
                ? this.company.code.trim()
                : null
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .put(`/company/${this.companyId}`, this.company)
                        .then(() => {
                            this.showNotification(true)
                            this.$router.push({ path: '/systems/company/list' })
                        })
                        .catch((error) => {
                            this.showNotification(false, error.message)
                        })
                }
            })
        },

        showNotification(isSuccess, message = null) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: isSuccess
                        ?  this.$t('Success.UpdateCompany')
                        : this.$t(
                              'categories.waterWarning.Label.notification.error'
                          ),
                    icon: isSuccess ? 'CheckIcon' : 'AlertTriangleIcon',
                    variant: isSuccess ? 'success' : 'danger',
                    text: `${this.$t(message)}`,
                },
            })
        },
    },
}
</script>
<style scoped>
.btn {
    margin: 0 7px 7px;
    min-width: 115px;
}

.validate-error {
    color: red;
    font-size: 11px;
    font-style: bold;
}
</style>
