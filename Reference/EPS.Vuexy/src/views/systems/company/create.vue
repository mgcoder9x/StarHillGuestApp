<template>
    <div class="animated fadeIn">
        <validation-observer ref="rules">
            <b-row>
                <b-col lg="12">
                    <b-card>
                        <b-form @submit.prevent="save">
                            <b-row>
                                <!-- Profile-->
                                <b-col md="6" offset="3">
                                    <b-row>
                                        <!-- Company Code -->
                                        <b-col>
                                            <validation-provider
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
                                                    label-class="required"
                                                >
                                                    <b-form-input
                                                        type="text"
                                                        id="txt_code"
                                                        v-model="company.code"
                                                    >
                                                    </b-form-input>
                                                    <span
                                                        class="validate-error"
                                                    >
                                                        {{ errors[0] }}</span
                                                    >
                                                </b-form-group>
                                            </validation-provider>
                                        </b-col>
                                    </b-row>
                                    <!-- Company name -->
                                    <b-row>
                                        <b-col>
                                            <validation-provider
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
                                                    label-class="required"
                                                >
                                                    <b-form-input
                                                        type="text"
                                                        id="txt_name"
                                                        v-model="company.name"
                                                    >
                                                    </b-form-input>
                                                    <span
                                                        class="validate-error"
                                                    >
                                                        {{ errors[0] }}</span
                                                    >
                                                </b-form-group>
                                            </validation-provider>
                                        </b-col>
                                    </b-row>
                                    <!-- Parent Company -->
                                    <b-row>
                                        <b-col>
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'System.Company.Detail.Label.Parent'
                                                    )
                                                "
                                                :label-cols="4"
                                                :horizontal="true"
                                                label-align-md="left"
                                            >
                                                <Treeselect
                                                    :multiple="false"
                                                    placeholder=""
                                                    :options="treeCompany"
                                                    :reduce="(item) => item.id"
                                                    v-model="company.parentId"
                                                />
                                            </b-form-group>
                                        </b-col>
                                    </b-row>
                                    <!-- Loại bản đồ -->
                                    <b-row>
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
                                                    @change="onFileChange"
                                                    accept="image/*"
                                                    placeholder="Chọn file..."
                                                >
                                                </b-form-file>
                                                <b-img
                                                    v-if="company.ImgBase64"
                                                    :src="company.ImgBase64"
                                                    fluid
                                                    class="mt-2"
                                                    style="max-height: 200px;"
                                                />
                                            </b-form-group>
                                        </b-col>
                                    </b-row>

                                    <b-row>
                                        <b-col>
                                            <LatLngPicker
                                                v-if="company.mapType === 1"
                                                ref="mapPickerNormal"
                                                :lat-lng="mapPicker.coordinate"
                                                @input="setCoordinate"
                                            />
                                            <LatLngPickerImage
                                                v-if="company.mapType === 2"
                                                ref="mapPickerImage"
                                                :lat-lng="mapPicker.coordinate"
                                                :map-prop="mapPicker.mapProp"
                                                @input="setCoordinate"
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
                                            variant="success"
                                            @click="showConfigZalo = true"
                                        >
                                            {{
                                                $t('Button.ZaloConfig')
                                            }}</b-button
                                        >
                                        <b-button
                                            type="submit"
                                            variant="primary"
                                        >
                                            {{ $t('Button.Save') }}</b-button
                                        >
                                        <b-button
                                            type="button"
                                            variant="outline-secondary"
                                            @click="back"
                                        >
                                            {{ $t('Button.Cancel') }}</b-button
                                        >
                                    </div>
                                </b-col>
                            </b-row>
                        </b-form>
                    </b-card>
                </b-col>
            </b-row>
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
    </div>
</template>

<script>
import TreeHelper from '@/utils/treeHelper'
import Treeselect from '@riophae/vue-treeselect'
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import LatLngPicker from '@/components/LatLngPicker'
import LatLngPickerImage from '@/components/LatLngPickerImage.vue'

export default {
    components: {
        Treeselect,
        LatLngPicker,
        LatLngPickerImage
    },

    mixins: [authorizationMixin],
    data() {
        return {
            mapPicker: {
                coordinate: null,
                mapProp: {
                    center: [0, 0],
                    zoom: -1,
                    urlImgOverlay: '',
                    bounds: [],
                    opacity: 1,
                },
            },
            showConfigZalo: false,
            fileName: null,
            filePath: null,
            company: {
                code: null,
                name: null,
                parentId: null,
                level: null,
                mapType: 1,
                coordinates: null,
                ImgBase64: null,
                fileExtension: null,
                zaloConfig: {
                    app_id: null,
                    secret_key: null,
                    access_token: null,
                    refresh_token: null,
                },
            },
            treeCompany: [],
        }
    },

    created() {
        this.loadCompanyTree()
    },

    methods: {
        onFileChange(event) {
            const file = event.target.files[0]
            if (file) {
                // Lấy tên file và đường dẫn
                this.fileName = file.name
                this.filePath = file.webkitRelativePath || file.name
                
                // Đọc file và chuyển sang base64
                const reader = new FileReader()
                reader.onload = (e) => {
                    const fileExtension = file.name.split('.').pop()
                    this.company.ImgBase64 = e.target.result
                    this.company.fileExtension = fileExtension
                    this.mapPicker.mapProp.urlImgOverlay = e.target.result
                }
                reader.readAsDataURL(file)
            }
        },
        openMapPicker() {
            if (this.company.mapType === 1) {
                this.$refs.mapPickerNormal.toggleModal()
            } else {
                this.$refs.mapPickerImage.toggleModal()
            }
        }, 
        async setCoordinate(coordinate) {
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
            this.$refs.zaloForm.validate().then((success) => {
                if (success) {
                }
            })
        },
        loadCompanyTree() {
            return this.$services
                .get(`/lookup/company-tree`)
                .then((response) => {
                    this.treeCompany = TreeHelper.removeEmptyChildren(
                        response.data
                    )

                    this.company.parentId = this.$route.query?.parentId
                })
                .catch((error) => {
                    console.log(error)
                })
        },

        back() {
            this.$router.push({ path: '/systems/company/list' })
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
                        .post('/company', this.company)
                        .then((response) => {
                            this.showNotification(true)
                            this.$router.push({ path: '/systems/company/list' })
                        })
                        .catch((error) => {
                            this.showNotification(
                                false,
                                this.$t(`${error.message}`)
                            )
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
                        ? this.$t('Success.CreateCompany')
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
