<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <!-- ========== FORM THÔNG TIN CAMERA ========== -->
                <b-form>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Device.Detail.Form.Code')"
                                label-for="h-camera-code"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    name="DeviceCode"
                                >
                                    <b-form-input
                                        id="h-camera-code"
                                        v-model.trim="device.code"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <b-col md="6">
                            <b-form-group
                                :label="$t('Device.Detail.Form.Name')"
                                label-for="h-camera-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceName"
                                >
                                    <b-form-input
                                        id="h-camera-name"
                                        v-model.trim="device.name"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Device.Detail.Form.Area')"
                                label-for="h-camera-area"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceArea"
                                >
                                    <tree-select
                                        v-if="editing"
                                        v-model="device.areaId"
                                        :options="listArea"
                                        label="text"
                                        :reduce="(area) => area.id"
                                    />
                                    <b-form-input
                                        v-else
                                        v-model="device.areaName"
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <b-col md="6">
                            <b-form-group
                                :label="$t('Device.Detail.Form.Function')"
                                label-for="h-camera-event-type"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceEventType"
                                >
                                    <b-form-select
                                        v-if="editing"
                                        v-model="device.eventTypeId"
                                        :options="eventTypeOptions"
                                    />
                                    <b-form-input
                                        v-else
                                        :value="displayEventTypeName"
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Device.Detail.Form.Status')"
                                label-for="h-camera-status"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceStatus"
                                >
                                    <b-form-select
                                        v-if="editing"
                                        v-model="device.status"
                                    >
                                        <b-form-select-option :value="1">
                                            On
                                        </b-form-select-option>
                                        <b-form-select-option :value="0">
                                            Off
                                        </b-form-select-option>
                                    </b-form-select>
                                    <b-form-input
                                        v-else
                                        v-model="device.statusName"
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <b-col md="6">
                            <b-form-group
                                :label="$t('Device.Detail.Form.Server')"
                                label-for="h-camera-server"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceServer"
                                >
                                    <b-form-select
                                        v-if="editing"
                                        v-model="device.serverId"
                                        :options="listServer"
                                    />
                                    <b-form-input
                                        v-else
                                        v-model="device.serverName"
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Device.Detail.Form.Link')"
                                label-for="h-camera-link"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceLink"
                                >
                                    <b-form-input
                                        id="h-camera-link"
                                        v-model.trim="device.link"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <b-col md="6">
                            <b-form-group
                                :label="$t('Device.Detail.Form.License')"
                                label-for="h-camera-license"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceLicense"
                                >
                                    <b-form-input
                                        id="h-camera-license"
                                        v-model.trim="device.license"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-row>
                                <b-col md="8">
                                    <b-form-group
                                :label="$t('Device.Detail.Form.Direction')"
                                label-for="h-camera-direction"
                                label-cols-md="6"
                                style="flex: 1 1 45%"
                            >
                                <b-form-select
                                    v-if="editing"
                                    v-model="device.direction"
                                    class="direction_input"
                                    :disabled="!editing"
                                >
                                    <b-form-select-option :value="1">
                                        {{ $t('direction.in') }}
                                    </b-form-select-option>
                                    <b-form-select-option :value="2">
                                        {{ $t('direction.out') }}
                                    </b-form-select-option>
                                </b-form-select>
                                <b-form-input
                                    v-else
                                    v-model="device.directionStr"
                                    :disabled="!editing"
                                    />
                                    <!-- class="direction_input" -->
                            </b-form-group>
                            
                                </b-col>
                                <b-col md="4">
                                    <b-form-group
                                :label="$t('Device.Detail.Form.Coordinate')"
                                label-for="h-camera-coordinate"
                                label-cols-md="4"
                            >
                                <b-button
                                    size="sm"
                                    variant="success"
                                    @click="openMapPicker()"
                                    :disabled="!editing"
                                >
                                    <Icon
                                        icon="ei:location"
                                        width="20"
                                        height="20"
                                    />
                                </b-button>

                                <LatLngPicker
                                    v-if="!isShowMapImg && showMap"
                                    ref="mapPicker"
                                    :lat-lng="mapPicker.coordinate"
                                    :isEditing="!editing"
                                    @input="setCoordinates"
                                />

                                <LatLngPickerImage
                                    v-if="isShowMapImg && showMap"
                                    ref="mapPickerImage"
                                    :lat-lng="mapPicker.coordinate"
                                    :map-prop="mapPicker.mapProp"
                                    :isEditing="!editing"
                                    :cameras="allCameras"
                                    @input="setCoordinates"
                                />
                            </b-form-group>
                                </b-col>
                            </b-row>
                        </b-col>


                        <b-col md="6" class="flex align-items-center nowrap">
                                <b-form-group
                                :label="$t('Loại cam')"
                                label-for="h-camera-type"
                                label-cols-md="4"
                            >
                                <b-form-select
                                    v-if="editing"
                                    v-model="device.camType"
                                    :disabled="!editing"
                                >
                                    <b-form-select-option :value="1">
                                        {{ $t('Cam thường') }}
                                    </b-form-select-option>
                                    <b-form-select-option :value="2">
                                        {{ $t('Cam PTZ') }}
                                    </b-form-select-option>
                                </b-form-select>
                                <b-form-input
                                    v-else
                                    v-model="device.camTypeStr"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <!-- ========== THÔNG TIN CAMERA DETAIL (PTZ) ========== -->
                    <template v-if="device.camType === 2">
                        <b-row class="mt-1">
                            <b-col cols="12">
                                <h5
                                    class="mb-1 text-primary font-weight-bold border-bottom pb-1"
                                >
                                    {{
                                        $t('Thông tin camera detail') ||
                                        'Thông tin camera detail'
                                    }}
                                </h5>
                            </b-col>
                        </b-row>

                        <b-row>
                            <b-col md="4">
                                <b-form-group
                                    label="RTSP Stream"
                                    label-for="h-camera-rtsp"
                                    label-cols-md="5"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        name="RTSP"
                                        rules=""
                                    >
                                        <b-input-group size="md">
                                            <b-input-group-prepend is-text>
                                                <feather-icon
                                                    icon="VideoIcon"
                                                />
                                            </b-input-group-prepend>
                                            <b-form-input
                                                id="h-camera-rtsp"
                                                v-model.trim="device.rtsp"
                                                :disabled="!editing"
                                                :state="
                                                    errors.length > 0
                                                        ? false
                                                        : null
                                                "
                                            />
                                        </b-input-group>
                                        <small class="text-danger">
                                            {{ errors[0] }}
                                        </small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>

                            <b-col md="4">
                                <b-form-group
                                    label="Cổng HTTP"
                                    label-for="h-camera-http"
                                    label-cols-md="5"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        name="HTTPPort"
                                        rules="integer"
                                    >
                                        <b-input-group size="md">
                                            <b-input-group-prepend is-text>
                                                <feather-icon
                                                    icon="GlobeIcon"
                                                />
                                            </b-input-group-prepend>
                                            <b-form-input
                                                id="h-camera-http"
                                                v-model.trim="device.httpGate"
                                                :disabled="!editing"
                                                :state="
                                                    errors.length > 0
                                                        ? false
                                                        : null
                                                "
                                            />
                                        </b-input-group>
                                        <small class="text-danger">
                                            {{ errors[0] }}
                                        </small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>

                            <b-col md="4">
                                <b-form-group
                                    :label="$t('Địa chỉ IP') || 'Địa chỉ IP'"
                                    label-for="h-camera-ip"
                                    label-cols-md="5"
                                >
                                    <b-input-group size="md">
                                        <b-input-group-prepend is-text>
                                            <feather-icon icon="ServerIcon" />
                                        </b-input-group-prepend>
                                        <b-form-input
                                            id="h-camera-ip"
                                            v-model.trim="device.ip"
                                            :disabled="!editing"
                                        />
                                    </b-input-group>
                                </b-form-group>
                            </b-col>
                        </b-row>
                    </template>

                    <!-- ========== BUTTONS ========== -->
                    <b-row>
                        <b-col>
                            <div class="text-center">
                                <b-button
                                    v-if="
                                        editing && authorize(['ManageDevice'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="validationForm"
                                >
                                    {{ $t('Button.Save') }}
                                </b-button>

                                <b-button
                                    v-if="
                                        !editing && authorize(['ManageDevice'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="edit"
                                >
                                    {{ $t('Button.Edit') }}
                                </b-button>

                                <b-button
                                    v-if="!editing"
                                    :to="{ path: '/categories/devices/list' }"
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary"
                                >
                                    {{ $t('Button.Back') }}
                                </b-button>

                                <b-button
                                    v-if="editing"
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary"
                                    @click="cancel"
                                >
                                    {{ $t('Button.Cancel') }}
                                </b-button>
                            </div>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>

        <!-- ========== PTZ + PRESET ========== -->
        <b-card v-if="editing && device.camType === 2" no-body class="mt-2">
            <b-card-body>
                <!-- Context menu -->
                <vue-context ref="menu">
                    <li v-if="authorize(['ManageDevice'])">
                        <b-link
                            class="d-flex align-items-center"
                            @click="onClickMenu($event.target.innerText, 'DRA')"
                        >
                            <feather-icon icon="SquareIcon" size="16" />
                            <span class="ml-75">
                                {{ $t('Live.RecognitionArea') }}
                            </span>
                        </b-link>
                    </li>
                </vue-context>

                <!-- Video 1 -->
                <b-row class="mt-1">
                    <b-col md="12">
                        <b-card
                            img-top
                            no-body
                            @contextmenu.prevent="$refs.menu.open($event, 1)"
                        >
                            <FlvPlayer
                                ref="player1"
                                :video-index="1"
                                :live-code="1.1"
                                :source="videoSource"
                                :display="1"
                                :type-method="() => {}"
                                @base64-img="base64Img"
                            >
                                <canvas
                                    id="video1"
                                    class="card-img-top"
                                    width="900"
                                    height="500"
                                />
                            </FlvPlayer>
                        </b-card>
                    </b-col>
                </b-row>

                <!-- Nút PTZ -->
                <b-row class="pb-1">
                    <b-col md="4">
                        <div class="d-flex justify-content-around">
                            <b-button
                                variant="outline-danger"
                                class="btn btn-circle btn-outline-danger flv-up-btn"
                                @click="clickUp"
                                v-b-tooltip.hover
                                :title="$t('Lên')"
                            >
                                <Icon icon="mdi:arrow-up" />
                            </b-button>
                            <b-button
                                variant="outline-info"
                                class="btn btn-circle btn-outline-info flv-down-btn"
                                @click="clickDown"
                                v-b-tooltip.hover
                                :title="$t('Xuống')"
                            >
                                <Icon icon="mdi:arrow-down" />
                            </b-button>
                            <b-button
                                variant="outline-primary"
                                class="btn btn-circle btn-outline-primary flv-right-btn"
                                @click="clickRight"
                                v-b-tooltip.hover
                                :title="$t('Phải')"
                            >
                                <Icon icon="mdi:arrow-right" />
                            </b-button>
                            <b-button
                                variant="outline-success"
                                class="btn btn-circle btn-outline-success flv-left-btn"
                                @click="clickLeft"
                                v-b-tooltip.hover
                                :title="$t('Trái')"
                            >
                                <Icon icon="mdi:arrow-left" />
                            </b-button>
                        </div>
                        <b-button
                            variant="outline-dark"
                            class="btn btn-circle btn-outline-dark flv-zoom_in-btn"
                            @click="clickZoomIn"
                            v-b-tooltip.hover
                            :title="$t('ZoomIn')"
                        >
                            <Icon icon="mdi:magnify-plus" />
                        </b-button>
                        <b-button
                            variant="outline-dark"
                            class="btn btn-circle btn-outline-dark flv-zoom_out-btn"
                            @click="clickZoomOut"
                            v-b-tooltip.hover
                            :title="$t('ZoomOut')"
                        >
                            <Icon icon="mdi:magnify-minus" />
                        </b-button>
                    </b-col>
                    <!-- Movement Velocity -->
                    <b-col md="4">
                    <b-form-group
                        label="Tốc độ quay:"
                        label-for="ptz-velocity"
                        label-class="mb-0 mr-1"
                        label-cols="auto"
                        class="mb-0"
                    >
                        <div class="range-wrap range-tight">
                        <b-form-input
                            id="ptz-velocity"
                            type="range"
                            min="0.1"
                            max="1"
                            step="0.1"
                            class="range"
                            v-model.number="velocity"
                            :disabled="!editing"
                            @input="updateVelocityBubble"
                        />
                        <!-- bubble -->
                        <output class="bubble" :style="velocityBubbleStyle">
                            {{ velocity }}
                        </output>
                        </div>
                    </b-form-group>
                    </b-col>

                    <!-- Auto-Stop Timeout -->
                    <b-col md="4">
                    <b-form-group
                        label="Thời gian dừng quay(s):"
                        label-for="ptz-timeout"
                        label-class="mb-0 mr-1"
                        label-cols="auto"
                        class="mb-0"
                    >
                        <div class="range-wrap range-tight">
                        <b-form-input
                            id="ptz-timeout"
                            type="range"
                            min="0.5"
                            max="30"
                            step="0.5"
                            class="range"
                            v-model.number="timeout"
                            :disabled="!editing"
                            @input="updateTimeoutBubble"
                        />
                        <!-- bubble -->
                        <output class="bubble" :style="timeoutBubbleStyle">
                            {{ timeout }}s
                        </output>
                        </div>
                    </b-form-group>
                    </b-col>

                </b-row>

                <!-- Bảng preset + Video2 -->
                <b-row class="mt-3">
                    <b-col md="4">
                        <h6 class="mb-2">Danh sách Preset</h6>
                        <b-table
                            responsive
                            hover
                            small
                            :items="device.presetList || []"
                            :fields="presetFields"
                            class="mb-0"
                        >
                            <template #cell(presetNo)="data">
                                {{ data.item.presetNo }}
                            </template>

                            <template #cell(actions)="data">
                                <b-button
                                    size="sm"
                                    variant="outline-primary"
                                    class="mr-1"
                                    @click="gotoPreset(data.item.token)"
                                    title="Go to preset"
                                >
                                    <feather-icon icon="ArrowRightCircleIcon" />
                                </b-button>
                                <b-button
                                    size="sm"
                                    variant="outline-info"
                                    class="mr-1"
                                    @click="editPreset(data.item)"
                                    title="Sửa preset"
                                >
                                    <feather-icon icon="EditIcon" />
                                </b-button>
                                <b-button
                                    size="sm"
                                    variant="outline-danger"
                                    @click="deletePreset(data.item.token)"
                                    title="Xóa preset"
                                >
                                    <feather-icon icon="Trash2Icon" />
                                </b-button>
                            </template>
                        </b-table>

                        <div
                            v-if="
                                !device.presetList ||
                                device.presetList.length === 0
                            "
                            class="text-center text-muted py-5"
                        >
                            Chưa có preset nào được thiết lập
                        </div>
                    </b-col>

                    <b-col md="8">
                        
                        <div
                            class="position-relative border rounded overflow-hidden bg-black"
                        >
                            <FlvPlayer
                                ref="player2"
                                :video-index="2"
                                :source="videoSource1"
                                style="height: 560px"
                                :live-code="2.1"
                                :type-method="() => {}"
                            >
                                <canvas
                                    id="video2"
                                    width="900"
                                    height="500"
                                    class="w-100 h-100"
                                />
                            </FlvPlayer>
                        </div>
                    </b-col>
                </b-row>

                <!-- Modal vẽ polygon -->
                <b-modal
                    v-model="modalDraw"
                    :title="$t('Live.Draw')"
                    ok-title="Đóng"
                    hide-header-close
                    ok-only
                    modal-class="custom-modal-draw"
                >
                    <div>
                        <v-stage ref="stage" :config="configKonva">
                            <v-layer ref="drawLayer" @click="layerClick">
                                <v-image :config="{ image }" />

                                <template v-if="isDrawing">
                                    <!-- Polygon -->
                                    <PolygonEditor
                                        v-for="(poly, idx) in polygons"
                                        ref="polygon"
                                        :key="idx"
                                        :points.sync="poly.points"
                                        :active="idx === activeIdx"
                                        :meta-data="polygonMetaData"
                                        @polygon-click="setActive(idx)"
                                        @update:points="
                                            onPolygonUpdated(idx, $event)
                                        "
                                        @isInPolygon="isInPolygon = $event"
                                    />

                                    <!-- LABEL: 1 dòng chữ ở góc trên trái polygon -->
                                    <v-text
                                        v-for="(poly, idx) in polygons"
                                        :key="'label-' + idx"
                                        :config="
                                            getPolygonLabelConfig(poly, idx)
                                        "
                                    />
                                </template>
                            </v-layer>
                        </v-stage>
                    </div>

                    <template #modal-footer>
                        <div class="d-flex justify-content-between w-100">
                            <div>
                                <b-button
                                    v-if="isDrawing && activeIdx !== null"
                                    variant="danger"
                                    class="mr-1"
                                    @click="deletePolygonAndPreset"
                                >
                                    {{ $t('Button.Delete') }}
                                </b-button>
                                <b-button
                                    v-if="isDrawing && polygons.length"
                                    variant="primary"
                                    class="mr-1"
                                    @click="
                                        polygons.splice(0, polygons.length)
                                        activeIdx = null
                                    "
                                >
                                    {{ $t('Button.Refresh') }}
                                </b-button>
                            </div>

                            <div>
                                <b-button
                                    v-if="!isDrawing"
                                    variant="primary"
                                    class="mr-1"
                                    @click="
                                        isDrawing = true
                                        activeIdx = null
                                    "
                                >
                                    {{ $t('Button.Edit') }}
                                </b-button>
                                <b-button
                                    v-if="isDrawing"
                                    variant="primary"
                                    class="mr-1"
                                    @click="openModalSavePolygon"
                                >
                                    {{ $t('Button.Save') }}
                                </b-button>
                                <b-button @click="hideModelPolygon">
                                    {{ $t('Button.Exit') }}
                                </b-button>
                            </div>
                        </div>
                    </template>
                </b-modal>

                <!-- Modal lưu preset -->
                <b-modal
                    id="presetModal"
                    v-model="showPresetModal"
                    :title="isEditingPreset ? 'Sửa Preset' : 'Thêm Preset'"
                    :no-close-on-esc="true"
                    :no-close-on-backdrop="true"
                    hide-header-close
                    size="lg"
                >
                    <b-row>
                        <b-col cols="12">
                            <b-form-group
                                :label="$t('Tên Preset')"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    name="Tên preset"
                                    rules="required"
                                >
                                    <b-input-group size="md">
                                        <b-form-input
                                            id="h-preset-name"
                                            v-model="preset.name"
                                        />
                                    </b-input-group>
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <template #modal-footer>
                        <b-button
                            variant="primary"
                            class="mr-50"
                            @click="savePreset"
                        >
                            {{ $t('Button.Save') || 'Ghi lại' }}
                        </b-button>
                        <b-button
                            variant="outline-secondary"
                            @click="hideModel"
                        >
                            {{ $t('Button.Cancel') || 'Hủy' }}
                        </b-button>
                    </template>
                </b-modal>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import LatLngPicker from '@/components/LatLngPicker'
import LatLngPickerImage from '@/components/LatLngPickerImage'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import VueContext from 'vue-context'
import TreeHelper from '@/utils/treeHelper'
import getBaseUrl from '@/utils/get-baseUrl'
import PolygonEditor from '@/views/live/PolygonEditor.vue'
import { $themeConfig } from '@themeConfig'
import { v4 as uuidv4 } from 'uuid'
const isDevEnv = process.env.NODE_ENV === 'development'

export default {
    mixins: [authorizationMixin],
    components: {
        LatLngPicker,
        LatLngPickerImage,
        VueContext,
        PolygonEditor,
    },
    data() {
        return {
            mapPicker: {
                coordinate: {
                    lat: null,
                    lng: null,
                    zoom: null
                },
                mapProp: {
                    urlImgOverlay: null,
                    center: [0, 0],
                    zoom: -1,
                },
            },
            _ptzTimeout: null,
            velocity: 0.1,
            timeout: 0.5,
            velocityBubbleStyle: { left: '50%' },
            timeoutBubbleStyle: { left: '50%' },
            presetIndex: 0,
            activeIdx: null,
            establishing: false,
            videoSource: null,
            videoSource1: null,
            presetFields: [
                {
                    key: 'presetNo',
                    label: 'Preset',
                    thStyle: { width: '100px' },
                },
                {
                    key: 'actions',
                    label: 'Thao tác',
                    thStyle: { width: '120px' },
                },
            ],
            polygons: [], // danh sách polygon đang vẽ
            image: null,
            configKonva: {
                width: 960,
                height: 540,
            },
            isInPolygon: false,
            showPresetModal: false,
            polygonMetaData: [],
            modalDraw: false,
            drawWidth: 960,
            drawHeight: 540,
            isDrawing: false,
            arrcicle: [],
            isEditingPreset: false, // đang edit preset hay tạo mới
            editingPresetToken: null, // token của preset đang edit
            device: {
                code: null,
                name: null,
                link: null,
                areaId: null,
                serverId: null,
                eventTypeId: null,
                status: null,
                compId: null,
                license: null,
                zoom: null,
                direction: null,
                soundFile: null,
                soundFilePath: null,
                rtsp: null,
                ip: null,
                httpGate: null,
                lstPreset: [],
                presetCoordinates: null,
                presetList: [],
            },
            preset: {
                deviceId: null,
                presetNo: null,
                name: null,
                x: null,
                y: null,
                z: null,
                polygonId: null,
            },
            lstPreset: [
                { id: 0, text: '0' },
                { id: 1, text: '1' },
                { id: 2, text: '2' },
                { id: 3, text: '3' },
                { id: 4, text: '4' },
                { id: 5, text: '5' },
                { id: 6, text: '6' },
                { id: 7, text: '7' },
                { id: 8, text: '8' },
                { id: 9, text: '9' },
                { id: 10, text: '10' },
                { id: 11, text: '11' },
                { id: 12, text: '12' },
                { id: 13, text: '13' },
                { id: 14, text: '14' },
                { id: 15, text: '15' },
            ],
            searchForm: {
                deviceId: this.$route.params.id,
                preset: 0,
            },
            lstPresetChild: [],
            messageQueue: [],
            reconnectAttempts: 0,
            maxReconnectAttempts: 5,
            isReconnecting: false,
            camIndexSelecting: 0,
            listArea: [],
            listEventType: [],
            listServer: [],
            editing: false,
            isShowMapImg: false,
            showMap: false,
            companyId: null,
            companyImgPath: null,
            allCameras: [],
        }
    },
    watch: {
        'device.areaId': {
            deep: true,
            handler() {
                const findAreaById = (areas, id) => {
                    return areas.reduce((acc, area) => {
                        if (area.id === id) return area
                        if (area.children) {
                            const found = findAreaById(area.children, id)
                            if (found) return found
                        }
                        return acc
                    }, null)
                }
                // Luôn dùng ảnh công ty thay vì ảnh area
                if (this.companyImgPath) {
                    this.isShowMapImg = true
                    this.mapPicker.mapProp.urlImgOverlay = this.mapImg
                }
            },
        },
    },
    mounted() {
        this.updateVelocityBubble(this.velocity)
        this.updateTimeoutBubble(this.timeout)
    },

    computed: {
        eventTypeOptions() {
            return this.listEventType.map((item) => ({
                value: item.id,
                text:
                    this.$i18n.locale === 'vi'
                        ? item.text
                        : item.englishName || item.text,
            }))
        },
        displayEventTypeName() {
            if (this.$i18n.locale === 'vi') return this.device.eventTypeName
            const eventType = this.listEventType.find(
                (x) => x.id == this.device.eventTypeId
            )
            return eventType?.englishName || this.device.eventTypeName
        },
        deviceId() {
            return this.$route.params.deviceId
        },
        coordinate() {
            return {
                lat: this.device.latitude,
                lng: this.device.longitude,
                zoom: this.device.zoom,
            }
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
        this.socketId = this.uuidv4()
        this.nodeMediaServer = process.env.VUE_APP_NODE_MEDIA_SERVER
        this.nodeMediaServerPtz = process.env.VUE_APP_NODE_MEDIA_SERVER_PTZ
        const accessToken = this.$services.getUserData()
        this.companyId = accessToken.companyId
        this.device.compId = accessToken.companyId
        this.initWebSocket()
        await this.loadCompany()
        this.loadArea()
        this.loadEventType()
        this.loadServer()
        this.loadDeviceDetail()
        this.loadAllCameras()
    },
    methods: {
        // ========== WEBSOCKET ==========
        initWebSocket() {
            this.connection = new WebSocket(
                isDevEnv
                    ? 'ws://localhost:1938/ws'
                    : process.env.VUE_APP_SIGNALR_URL
            )
            this.connection.onopen = () => {
                this.reconnectAttempts = 0
                this.isReconnecting = false
                this.flushMessageQueue()
                this.sendMessage(
                    `{"EVENT_ID": 1, "DEVICE_ID": "${this.socketId}", "DEVICE_TYPE": 2}`
                )
            }
            this.connection.onerror = () => {
                this.loadLocation && this.loadLocation(0)
                this.handleReconnect()
            }
            this.connection.onclose = (event) => {
                if (!event.wasClean) {
                    this.handleReconnect()
                }
            }
            this.connection.onmessage = (event) => {
                this.handleSocketMessage(event)
            }
        },
        async handleSocketMessage(event) {
            const data = JSON.parse(event.data)
            if (data.code === 200 && data.presetCoordinate) {
                this.preset.deviceId = this.device.id
                if (this.device.deviceFunction === 1) {
                    this.preset.presetNo = this.point.preset
                }
                this.preset.x = data.presetCoordinate.x
                this.preset.y = data.presetCoordinate.y
                this.preset.z = data.presetCoordinate.z
                this.savePresetCoordinate &&
                    this.savePresetCoordinate(
                        data.presetCoordinate,
                        this.point.preset
                    )
            }

            if (data.EVENT_ID === 5) {
                this.wsMsgCount = (this.wsMsgCount || 0) + 1
                this.msgReceived = true
                const image3 = document.getElementById('device_image')
                this.REGIONS_NUMBDB = data.REGIONS_NUM
                this.imagesrc = `data:image/png;base64,${data.FRAME}`
                const y = this.iwidth / image3.clientWidth
                image3.style.height = `${this.iheight / y}px`
                const image = new window.Image()
                image.width = image3.clientWidth
                image.height = this.iheight / y
                image.src = this.imagesrc
                image3.src = this.imagesrc
                const canvas = document.getElementById('drawing_canvas')
                canvas.width = image3.clientWidth
                canvas.height = image3.clientHeight
                if (image3) {
                    this.tlh = this.iheight / image3.clientHeight
                    this.tlw = this.iwidth / image3.clientWidth
                }
                this.loadLocation && (await this.loadLocation(0))
            }
        },
        handleReconnect() {
            if (
                this.reconnectAttempts < this.maxReconnectAttempts &&
                !this.isReconnecting
            ) {
                this.isReconnecting = true
                const delay = Math.min(
                    1000 * Math.pow(2, this.reconnectAttempts),
                    30000
                )
                setTimeout(() => {
                    this.reconnectAttempts++
                    this.initWebSocket()
                }, delay)
            }
        },
        flushMessageQueue() {
            while (this.messageQueue.length > 0) {
                const message = this.messageQueue.shift()
                this.connection.send(message)
            }
        },
        sendMessage(message) {
            if (this.connection.readyState === WebSocket.OPEN) {
                this.connection.send(message)
            } else {
                this.messageQueue.push(message)
                if (
                    this.connection.readyState === WebSocket.CLOSED &&
                    !this.isReconnecting
                ) {
                    this.handleReconnect()
                }
            }
        },
        uuidv4() {
            return ([1e7] + -1e3 + -4e3 + -8e3 + -1e11).replace(/[018]/g, (c) =>
                (
                    c ^
                    (crypto.getRandomValues(new Uint8Array(1))[0] &
                        (15 >> (c / 4)))
                ).toString(16)
            )
        },
        calcBubbleStyle(value, min, max) {
            const v = Number(value)
            const mi = Number(min)
            const ma = Number(max)
            const percent = (v - mi) / (ma - mi) || 0

            // Công thức nhỏ để bubble không lệch ra ngoài 2 mép
            const pos = `calc(${percent * 100}% + (${8 - percent * 16}px))`

            return { left: pos }
        },

        updateVelocityBubble(value) {
            this.velocityBubbleStyle = this.calcBubbleStyle(
            value,
            0.1,
            1
            )
        },

        updateTimeoutBubble(value) {
            this.timeoutBubbleStyle = this.calcBubbleStyle(
            value,
            0.5,
            30
            )
        },
        // ========== VIDEO / PTZ ==========
        getVideo() {
            const accessToken = this.$services.getUserData()
            console.log(accessToken)
            this.videoSource =
                this.nodeMediaServerPtz + '/liveoverview/' + this.device.code + '.flv'
            // 'rtmp://192.168.1.112:42002/liveoverview/CAM_VUNG_CAM.flv'
            this.videoSource1 =
                this.nodeMediaServer + accessToken.companyId + '/' + this.device.code + '_PTZ.flv'
                // 'rtmp://192.168.1.112:42002/livedetail/CAM_VUNG_CAM.flv'

        },
        stopMovement() {
            this.$services.post(
                `/device/preset/stop_movement?deviceCode=${this.device.code}`
            )
        },
        _ptzCommand(command, payload = {}) {
            const velocity = Number(this.velocity) || 0.1
            const timeout = Number(this.timeout) || 0.5
            this.$services.post(command, {
                camera_id: this.device.code,
                velocity,
                timeout,
                ...payload,
            })

            // if (this._ptzTimeout) clearTimeout(this._ptzTimeout)
            // this._ptzTimeout = setTimeout(() => {
            //     this.stopMovement()
            //     this._ptzTimeout = null
            // }, 1000)
        },
        clickUp() {
            this._ptzCommand('/device/ptz_up')
        },
        clickDown() {
            this._ptzCommand('/device/ptz_down')
        },
        clickRight() {
            this._ptzCommand('/device/ptz_right')
        },
        clickLeft() {
            this._ptzCommand('/device/ptz_left')
        },
        clickZoomIn() {
            this._ptzCommand('/device/ptz_zoomin')
        },
        clickZoomOut() {
            this._ptzCommand('/device/ptz_zoomout')
        },
        beforeDestroy() {
            if (this._ptzTimeout) clearTimeout(this._ptzTimeout)
        },

        // ========== PRESET ==========
        async fetchPresets() {
            try {
                const res = await this.$services.get(
                    `/device/presetList?deviceCode=${this.device.code}`
                )
                this.device = {
                    ...this.device,
                    presetList: res.data.data || [],
                }
            } catch (error) {
                this.$bvToast.toast('Không thể tải danh sách preset', {
                    title: 'Lỗi',
                    variant: 'danger',
                })
            }
        },
        gotoPreset(token) {
            this.$services.post(
                `/device/gotoPreset/${token}?deviceCode=${this.device.code}`
            )
        },
        async editPreset(presetItem) {
            try {
                // Chuyển sang chế độ editing nếu chưa editing
                if (!this.editing) {
                    this.editing = true
                    this.getVideo(this.deviceId)
                }
                
                // Load tất cả cameras để hiển thị trên map
                await this.loadAllCameras()
                
                // Load polygon của preset này
                await this.getFrameSelecting(this.device.id)
                
                // Đợi một chút để đảm bảo polygons đã được set
                await this.$nextTick()
                
                // Tìm polygon tương ứng với preset
                const polygonIndex = this.polygons.findIndex(
                    p => p.presetToken === presetItem.token
                )
                
                if (polygonIndex !== -1) {
                    // Nếu tìm thấy polygon, set active và mở modal vẽ
                    this.activeIdx = polygonIndex
                    
                    // Set thông tin preset để khi save sẽ update
                    const polygon = this.polygons[polygonIndex]
                    this.preset.name = presetItem.presetName
                    this.preset.polygonId = polygon.id
                    this.isEditingPreset = true
                    this.editingPresetToken = presetItem.token
                    
                    console.log('Opening modal with polygon:', polygonIndex, this.polygons[polygonIndex])
                    console.log('Setting isDrawing and modalDraw to true')
                    
                    // Set isDrawing trước khi mở modal
                    this.isDrawing = true
                    this.modalDraw = true
                    
                    // Đảm bảo polygon vẫn active sau khi modal mở
                    await this.$nextTick()
                    console.log('After nextTick - activeIdx:', this.activeIdx, 'isDrawing:', this.isDrawing)
                } else {
                    // Nếu không tìm thấy polygon, vẫn cho phép edit preset
                    this.preset.name = presetItem.presetName
                    this.isEditingPreset = true
                    this.editingPresetToken = presetItem.token
                    
                    this.modalDraw = true
                    this.isDrawing = true
                    this.$toast({
                        component: ToastificationContent,
                        props: {
                            title: 'Preset chưa có vùng nhận diện',
                            icon: 'InfoIcon',
                            variant: 'info',
                            text: 'Bạn có thể vẽ vùng nhận diện mới cho preset này',
                        },
                    })
                }
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    props: {
                        title: 'Lỗi',
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: 'Không thể load polygon',
                    },
                })
            }
        },
        async deletePreset(token) {
            const result = await this.$swal({
                title: 'Xác nhận xóa preset?',
                text: 'Bạn có chắc chắn muốn xóa preset này?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Đồng ý',
                cancelButtonText: 'Hủy bỏ',
                customClass: {
                    confirmButton: 'btn btn-danger',
                    cancelButton: 'btn btn-outline-secondary ml-1',
                },
                buttonsStyling: false,
            })
            if (!result.isConfirmed) return

            try {
                await this.$services.delete(
                    `/device/preset/${token}?deviceCode=${this.device.code}`
                )
                this.$toast({
                    component: ToastificationContent,
                    props: {
                        title: 'Xóa preset thành công!',
                        icon: 'Trash2Icon',
                        variant: 'success',
                    },
                })
                await this.fetchPresets()
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    props: {
                        title: 'Lỗi',
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text:
                            error.response?.data?.message ||
                            'Không thể xóa preset',
                    },
                })
            }
        },
        generateToken() {
        // Lấy timestamp hiện tại theo milliseconds (từ 1970)
            const timestamp = Date.now(); // ví dụ: 1735200000000 (13 chữ số)

            // Chuyển timestamp sang hex (thường 11 ký tự hex)
            let hex = timestamp.toString(16);

            // Lấy random thêm 4-5 ký tự hex để đủ độ dài và unique
            const random = Math.random().toString(16).substring(2, 6); // ví dụ "ab12"

            // Ghép: lấy 6 ký tự đầu từ timestamp hex + 4 ký tự random → tổng 10 ký tự
            let token = (hex + random).substring(0, 10).toUpperCase(); // hoặc .toLowerCase() nếu muốn chữ thường

            // Đảm bảo đúng 10 ký tự
            while (token.length < 10) {
                token += Math.random().toString(16).substring(2, 2 + (10 - token.length)).toUpperCase();
            }

            return token; // Ví dụ: "18F6A0B1E3" – thay đổi theo thời gian thực
        },
        async savePreset() {
            // Kiểm tra có polygon được vẽ không trước
            if (this.activeIdx === null || !this.polygons[this.activeIdx]) {
                // Highlight input tên preset
                const nameInput = document.getElementById('h-preset-name')
                if (nameInput) {
                    nameInput.focus()
                    nameInput.classList.add('is-invalid')
                    setTimeout(() => nameInput.classList.remove('is-invalid'), 3000)
                }
                
                // Hiển thị thông báo lỗi
                this.$toast({
                    component: ToastificationContent,
                    props: {
                        title: 'Chưa vẽ vùng nhận diện',
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: 'Vui lòng vẽ vùng nhận diện trước khi lưu preset',
                    },
                })
                return // Không cho phép lưu
            }

            const currentPolygon = this.polygons[this.activeIdx]
            if (!currentPolygon.points || currentPolygon.points.length < 6) {
                // Highlight input tên preset
                const nameInput = document.getElementById('h-preset-name')
                if (nameInput) {
                    nameInput.focus()
                    nameInput.classList.add('is-invalid')
                    setTimeout(() => nameInput.classList.remove('is-invalid'), 3000)
                }
                
                // Hiển thị thông báo lỗi
                this.$toast({
                    component: ToastificationContent,
                    props: {
                        title: 'Vùng nhận diện không hợp lệ',
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: 'Vùng nhận diện phải có ít nhất 3 điểm',
                    },
                })
                return // Không cho phép lưu
            }

            this.$refs.rules.validate().then(async (success) => {
                if (!success) return
                try {
                    const polygonId = await this.savePolygons()
                    
                    // Nếu không có polygonId, không cho phép lưu
                    if (!polygonId) {
                        return
                    }
                    
                    if (this.isEditingPreset && this.editingPresetToken) {
                    // UPDATE preset
                        const payload = {
                            presetCode: this.editingPresetToken,
                            presetName: this.preset.name,
                            camera_id: this.device.code,
                            deviceId: this.device.id,
                        }
                        if (polygonId) payload.polygonId = polygonId

                        await this.$services.put(
                            `/device/preset/${this.editingPresetToken}`,
                            payload
                        )

                        this.$toast({
                            component: ToastificationContent,
                            props: {
                                title: 'Cập nhật preset thành công',
                                icon: 'CheckIcon',
                                variant: 'success',
                            },
                        })
                    } else {
                    
                    const token = this.generateToken();

                    const payload = {
                        presetCode: token,
                        presetName: this.preset.name,
                        camera_id: this.device.code,
                        deviceId: this.device.id,
                    }
                    if (polygonId) payload.polygonId = polygonId

                    await this.$services.post('/device/preset', payload)

                    this.$toast({
                        component: ToastificationContent,
                        props: {
                            title: 'Thêm preset thành công',
                            icon: 'CheckIcon',
                            variant: 'success',
                        },
                    })
                }
                    await this.fetchPresets()

                    this.showPresetModal = false
                    this.modalDraw = false
                    this.isDrawing = false
                    this.preset.name = null
                    this.isEditingPreset = false
                    this.editingPresetToken = null
                } catch (error) {
                    this.$toast({
                        component: ToastificationContent,
                        props: {
                            title: 'Lỗi',
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                            text:
                                error.response?.data?.message ||
                                'Không thể lưu preset',
                        },
                    })
                }
            })
        },
        async savePresetModal() {
            this.showPresetModal = true
        },
        hideModel() {
            this.showPresetModal = false
            this.preset.name = null
            this.isEditingPreset = false
            this.editingPresetToken = null
        },

        // ========== POLYGON ==========
        onPolygonUpdated(idx, newPoints) {
            this.polygons[idx].points = newPoints
        },
        async onClickMenu(text, code) {
            if (code === 'DRA') {
                await this.getFrameSelecting(this.device.id)
            }
        },
        async getFrameSelecting() {
            this.$refs.player1.getFrame()
            await this.getInfoSelecting(this.$refs.player1)

            this.$services
                .get(`/lookup/polygon-function/${this.camIdSelecting}`)
                .then((res) => {
                    const data = res.data.data || '[]'
                    const functions = JSON.parse(data) || []
                    this.polygonMetaData = functions.length ? functions : []
                })
        },
        async getInfoSelecting(device) {
            this.camIdSelecting = this.deviceId
            this.p_width = device.width
            this.p_height = device.height

            try {
                const res = await this.$services.get(
                    `/device/polygon/${this.camIdSelecting}`
                )
                const list = res.data.data || []

                this.polygons = []

                list.forEach((item) => {
                    const raw = item.decimalPointArrays || item.pointArrays
                    if (!raw) return
                    let arr
                    try {
                        arr = JSON.parse(raw)
                    } catch (e) {
                        return
                    }
                    if (!Array.isArray(arr)) return
                    arr.forEach((p) => {
                        if (!p.points || p.points.length < 6) return
                        this.polygons.push({
                            points: p.points,
                            id: item.id,
                            presetId: item.presetId,
                            presetName: item.presetName,
                            presetToken: item.presetToken,
                        })
                    })
                })

                // Không set activeIdx = null ở đây vì có thể đang edit preset
                // this.activeIdx = null
            } catch {
                this.polygons = []
                // this.activeIdx = null
            }
        },
        async openModalSavePolygon() {
            // Load tất cả cameras để hiển thị trên map
            await this.loadAllCameras()
            
            // Nếu đang edit polygon có preset thì load preset đó
            if (this.activeIdx !== null) {
                const currentPolygon = this.polygons[this.activeIdx]
                if (currentPolygon.presetToken && currentPolygon.presetName) {
                    this.isEditingPreset = true
                    this.editingPresetToken = currentPolygon.presetToken
                    this.preset.name = currentPolygon.presetName
                    this.preset.polygonId = currentPolygon.id
                } else {
                    this.isEditingPreset = false
                    this.editingPresetToken = null
                    this.preset.name = null
                }
            }
            this.showPresetModal = true
        },
        // async savePolygons() {
        //     // không có polygon thì không lưu
        //     if (
        //         !this.$refs.polygon ||
        //         !this.polygons ||
        //         !this.polygons.length
        //     ) {
        //         return null
        //     }

        //     const polygonsMetaData = {}
        //     this.$refs.polygon.forEach((item, index) => {
        //         const res = item.getMetaData()
        //         polygonsMetaData[index] = { ...res }
        //     })

        //     this.isDrawing = false
            
        //     debugger
        //     const saveData = {
        //         deviceId: this.camIdSelecting || this.deviceId,
        //         decimalPointArrays: JSON.stringify(this.polygons),
        //         width: this.p_width,
        //         height: this.p_height,
        //         drawWidth: this.drawWidth,
        //         drawHeight: this.drawHeight,
        //         strType: 'polygon',
        //         polygonsMetaData: JSON.stringify(polygonsMetaData),
        //     }
        //     console.log(saveData)
        //     try {
        //         const res = await this.$services.post(
        //             '/device/polygon',
        //             saveData
        //         )
        //         const id =
        //             typeof res.data === 'object'
        //                 ? res.data.data || res.data.id
        //                 : res.data
        //         this.polygonId = id
        //         return id
        //     } catch (error) {
        //         this.$toast({
        //             component: ToastificationContent,
        //             position: 'top-right',
        //             props: {
        //                 title: this.$t('Error.Error'),
        //                 icon: 'AlertTriangleIcon',
        //                 variant: 'danger',
        //                 text:
        //                     error.response?.data?.message ||
        //                     'Không thể lưu polygon',
        //             },
        //         })
        //         throw error
        //     }
        // },
async savePolygons() {
    debugger
    if (this.activeIdx === null || !this.polygons[this.activeIdx]) {
        this.$toast({
            component: ToastificationContent,
            props: {
                title: 'Vui lòng chọn hoặc vẽ một vùng nhận diện',
                icon: 'AlertTriangleIcon',
                variant: 'warning',
            },
        })
        return null
    }

    const currentPolygon = this.polygons[this.activeIdx]
    if (!currentPolygon.points || currentPolygon.points.length < 6) {
        this.$toast({
            component: ToastificationContent,
            props: {
                title: 'Vùng nhận diện phải có ít nhất 3 điểm',
                icon: 'AlertTriangleIcon',
                variant: 'warning',
            },
        })
        return null
    }

    // Lấy metadata của polygon đang active (nếu có)
    const polygonRef = this.$refs.polygon?.[this.activeIdx]
    const metaData = polygonRef ? polygonRef.getMetaData() : {}

    // Format metadata as object with index as key: { "0": {...} }
    const metaDataDict = metaData && Object.keys(metaData).length > 0 
        ? { 0: metaData } 
        : {}

    const saveData = {
        deviceId: this.deviceId,
        decimalPointArrays: JSON.stringify([{ points: currentPolygon.points }]),
        width: this.p_width,
        height: this.p_height,
        drawWidth: this.drawWidth,
        drawHeight: this.drawHeight,
        strType: 'polygon',
        polygonsMetaData: JSON.stringify(metaDataDict),
        }

    try {
        let polygonId

        // Nếu polygon đã có id thì UPDATE, không thì CREATE
        if (currentPolygon.id) {
            // UPDATE polygon
            await this.$services.put(
                `/device/polygon/${currentPolygon.id}`,
                saveData
            )
            polygonId = currentPolygon.id
            
            // Cập nhật lại polygon trong array, giữ nguyên các thông tin preset
            this.$set(this.polygons, this.activeIdx, {
                ...currentPolygon,
                points: currentPolygon.points,
            })
            
            this.$toast({
                component: ToastificationContent,
                props: {
                    title: 'Cập nhật vùng nhận diện thành công!',
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        } else {
            // CREATE polygon mới
            const res = await this.$services.post('/device/polygon', saveData)
            polygonId = res.data?.data || res.data?.id || res.data

            // Ghi đè lại polygon vừa lưu để có id
            this.$set(this.polygons, this.activeIdx, {
                ...currentPolygon,
                id: polygonId,
            })

            this.$toast({
                component: ToastificationContent,
                props: {
                    title: 'Lưu vùng nhận diện thành công!',
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        }
        return polygonId
    } catch (error) {
        this.$toast({
            component: ToastificationContent,
            props: {
                title: 'Lỗi lưu polygon',
                icon: 'AlertTriangleIcon',
                variant: 'danger',
                text: error.response?.data?.message || 'Không thể lưu',
            },
        })
        throw error
    }
},

        setActive(idx) {
            this.activeIdx = this.activeIdx === idx ? null : idx
        },
        async deletePolygonAndPreset() {
            if (this.activeIdx === null) return

            const currentPolygon = this.polygons[this.activeIdx]
            
            // Xác nhận xóa
            const result = await this.$swal({
                title: 'Xác nhận xóa?',
                text: currentPolygon.presetToken 
                    ? 'Xóa vùng nhận diện này sẽ xóa cả preset tương ứng. Bạn có chắc chắn?'
                    : 'Bạn có chắc chắn muốn xóa vùng nhận diện này?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Đồng ý',
                cancelButtonText: 'Hủy bỏ',
                customClass: {
                    confirmButton: 'btn btn-danger',
                    cancelButton: 'btn btn-outline-secondary ml-1',
                },
                buttonsStyling: false,
            })

            if (!result.isConfirmed) return

            try {
                // Nếu polygon có preset token, xóa preset trước
                if (currentPolygon.presetToken) {
                    try {
                        await this.$services.delete(
                            `/device/preset/${currentPolygon.presetToken}?deviceCode=${this.device.code}`
                        )
                    } catch (error) {
                        console.error('Error deleting preset:', error)
                    }
                }

                // Xóa polygon nếu đã được lưu vào DB
                if (currentPolygon.id) {
                    try {
                        await this.$services.delete(`/device/polygon/${currentPolygon.id}`)
                    } catch (error) {
                        console.error('Error deleting polygon:', error)
                    }
                }

                // Xóa polygon khỏi array
                this.polygons.splice(this.activeIdx, 1)
                this.activeIdx = null

                this.$toast({
                    component: ToastificationContent,
                    props: {
                        title: 'Xóa thành công',
                        icon: 'CheckIcon',
                        variant: 'success',
                        text: currentPolygon.presetToken 
                            ? 'Đã xóa vùng nhận diện và preset'
                            : 'Đã xóa vùng nhận diện',
                    },
                })

                // Refresh danh sách preset
                await this.fetchPresets()
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    props: {
                        title: 'Lỗi',
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: error.response?.data?.message || 'Không thể xóa',
                    },
                })
            }
        },
        base64Img(e) {
            if (!e) {
                alert('Vui lòng thử lại')
                return
            }
            
            console.log('base64Img called - modalDraw:', this.modalDraw)
            
            // Chỉ gọi showModalDraw nếu modal chưa mở
            if (!this.modalDraw) {
                this.showModalDraw()
            }
            
            const image = new window.Image()
            image.src = e
            image.width = this.drawWidth
            image.height = this.drawHeight
            image.onload = () => {
                this.image = image
            }
        },
    
        showModalDraw() {
            console.log('showModalDraw called - current state:', {
                modalDraw: this.modalDraw,
                isDrawing: this.isDrawing,
                activeIdx: this.activeIdx,
                isEditingPreset: this.isEditingPreset
            })
            
            // Nếu modal đã mở và đang edit preset, không reset state
            if (this.modalDraw && this.isEditingPreset) {
                console.log('Modal already open and editing preset - skip reset')
                return
            }
            
            this.modalDraw = true
            this.isDrawing = false
            this.activeIdx = null // không focus polygon nào khi mới mở
        },
        layerClick() {
            if(!this.isDrawing) return
            const { x, y } = this.$refs.stage.getNode().getPointerPosition()
            if (this.activeIdx !== null) {
                if (!this.isInPolygon) {
                    this.$refs.polygon[this.activeIdx].addPoint(x, y)
                }
                return
            }
            if (!this.isInPolygon) {
                this.addPolygon(x, y)
            }
        },
        addPolygon(x, y) {
            const points = [x, y]
            this.polygons.push({ points })
            this.activeIdx = this.polygons.length - 1
        },
        hideModelPolygon() {
            this.isDrawing = false
            this.modalDraw = false
        },

        // label trên polygon: lấy góc trên trái
        getPolygonTopLeft(points) {
            if (!points || points.length < 2) return { x: 0, y: 0 }
            let minX = points[0]
            let minY = points[1]
            for (let i = 2; i < points.length; i += 2) {
                if (points[i] < minX) minX = points[i]
                if (points[i + 1] < minY) minY = points[i + 1]
            }
            return { x: minX, y: minY }
        },
        getPolygonLabel(poly, idx) {
            if (poly.presetName && poly.presetName.trim) return poly.presetName
            if (poly.presetToken) return poly.presetToken
            return `Preset ${idx + 1}`
        },
        getPolygonLabelConfig(poly, idx) {
            const { x, y } = this.getPolygonTopLeft(poly.points)
            return {
                x: x + 4,
                y: y - 8,
                text: this.getPolygonLabel(poly, idx),
                fontSize: 14,
                fontStyle: 'bold',
                fill: '#ffff00', // chữ vàng
                shadowColor: 'black',
                shadowBlur: 2,
            }
        },

        // ========== MAP / FORM ==========
        async loadCompany() {
            try {
                if (this.companyId) {
                    const response = await this.$services.get(`/company/${this.companyId}`)
                    this.companyImgPath = response.data.imgPath
                    
                    // Set ảnh vào map
                    if (this.companyImgPath) {
                        this.mapPicker.mapProp.urlImgOverlay = this.mapImg
                        this.isShowMapImg = true
                    } else {
                        this.isShowMapImg = false
                        this.mapPicker.mapProp.center = [16.964332450605063, 105.98940145630186]
                        this.mapPicker.mapProp.zoom = 5
                    }
                }
            } catch (error) {
                console.error('Error loading company:', error)
            }
        },
        openMapPicker() {
            this.showMap = true
            this.$nextTick(() => {
                if (this.isShowMapImg) {
                    this.$refs.mapPickerImage.toggleModal()
                } else {
                    this.$refs.mapPicker.toggleModal()
                }
            })
        },
        async setCoordinates(coordinate) {
            this.device.longitude = Number(coordinate.lng)
            this.device.latitude = Number(coordinate.lat)
            this.device.zoom = Number(coordinate.zoom)

            this.mapPicker.coordinate = { ...coordinate }
            this.mapPicker.mapProp.zoom = coordinate.zoom
            this.mapPicker.mapProp.center = [coordinate.lat, coordinate.lng]
        },
        loadDeviceDetail() {
            this.$services.get(`/device/${this.deviceId}`).then((response) => {
                this.device = {
                    ...response.data.data,
                    soundFile: null,
                }
                if (!this.msgReceived && this.device.deviceFunction === 1) {
                    this.changeFrameByPreset && this.changeFrameByPreset(0)
                }
                if (this.device.lstPreset) {
                    this.lstPresetChild = this.device.lstPreset
                    this.device.preset = this.device.lstPreset.join(', ')
                }
                switch (this.device.direction) {
                    case 1:
                        this.device.directionStr = this.$t('direction.in')
                        break
                    case 2:
                        this.device.directionStr = this.$t('direction.out')
                        break
                }
                switch (this.device.camType) {
                    case 1:
                        this.device.camTypeStr = this.$t('cam thường')
                        break
                    case 2:
                        this.device.camTypeStr = this.$t('Cam PTZ')
                    this.fetchPresets()
                        break
                }
                // console.log('trước khi chạy')
                // this.fetchPresets()
                // console.log('sau khi chạy')

                this.mapPicker.coordinate.lat = this.device.latitude
                this.mapPicker.coordinate.lng = this.device.longitude
                this.mapPicker.coordinate.zoom = this.device.zoom
            })
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                if (!success) return

                this.device.code = this.trimField(this.device.code)
                this.device.name = this.trimField(this.device.name)
                this.device.link = this.trimField(this.device.link)
                this.device.license = this.trimField(this.device.license)

                const formData = new FormData()
                for (const key in this.device) {
                    if (
                        !Object.prototype.hasOwnProperty.call(this.device, key)
                    ) {
                        continue
                    }
                    if (
                        key === 'areaName' ||
                        key === 'eventTypeName' ||
                        key === 'serverName' ||
                        key === 'statusName'
                    ) {
                        continue
                    }
                    if (key === 'soundFile') {
                        if (this.device.soundFile instanceof File) {
                            formData.append(
                                'soundFile',
                                this.device.soundFile,
                                this.device.soundFile.name
                            )
                        } else {
                            formData.append('soundFile', '')
                        }
                    } else if (this.device[key] !== null) {
                        formData.append(key, this.device[key])
                    }
                }

                this.$services
                    .put(`/device/${this.deviceId}`, formData, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    })
                    .then(() => {
                        this.$toast({
                            component: ToastificationContent,
                            position: 'top-right',
                            props: {
                                title: this.$t('Success.UpdateCamera'),
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
                                title: this.$t('Error.Error'),
                                icon: 'AlertTriangleIcon',
                                variant: 'danger',
                                text: `${this.$t(error.message)}`,
                            },
                        })
                    })
            })
        },
        loadEventType() {
            this.$services.get('/lookup/eventType').then((res) => {
                this.listEventType = res.data.data
            })
        },
        loadArea() {
            this.$services.get('/lookup/areas-tree').then((res) => {
                this.listArea = TreeHelper.removeEmptyChildren(res.data.data)
            })
        },
        loadServer() {
            this.$services.get('/lookup/server').then((res) => {
                this.listServer = res.data.data
            })
        },
        edit() {
            this.editing = true
            this.getVideo(this.deviceId)
        },
        cancel() {
            this.editing = false
            this.loadDeviceDetail()
        },
        async loadAllCameras() {
            try {
                const response = await this.$services.get('/device', {
                    params: {
                        page: 1,
                        perPage: 1000, // Lấy nhiều để chắc chắn có đủ
                        sortBy: 'id',
                        sortDesc: false
                    }
                })
                console.log('API response full:', response.data)
                
                // API trả về structure: { data: { data: [...] } }
                let cameras = []
                if (response.data && response.data.data) {
                    // Nếu data.data là object có property data (nested)
                    if (response.data.data.data && Array.isArray(response.data.data.data)) {
                        cameras = response.data.data.data
                    } 
                    // Nếu data.data là array luôn
                    else if (Array.isArray(response.data.data)) {
                        cameras = response.data.data
                    }
                }
                
                this.allCameras = cameras
                console.log('Loaded cameras count:', this.allCameras.length)
                console.log('Cameras with coordinates:', this.allCameras.filter(c => c.latitude && c.longitude).length)
            } catch (error) {
                console.error('Error loading cameras:', error)
                this.allCameras = []
            }
        },
    },
}
</script>

<style lang="scss">
@import '@core/scss/vue/libs/vue-context.scss';

.zoom {
    width: 100%;
    height: 100%;
    transform-origin: 0 0;
    transform: scale(1) translate(0, 0);
    cursor: grab;
}

.zoom_outer {
    overflow: hidden !important;
}

.flv-zoom_in-btn {
    position: absolute;
    top: 0;
    right: 100px;
}

.flv-zoom_out-btn {
    position: absolute;
    top: 0;
    right: 60px;
}

.flv-up-btn {
    position: absolute;
    top: -20px;
    right: 190px;
}

#drawing_canvas {
    border: 1px solid red;
    z-index: 10;
}

.flv-down-btn {
    position: absolute;
    top: 20px;
    right: 190px;
}

.flv-right-btn {
    position: absolute;
    top: 0;
    right: 155px;
}

.flv-left-btn {
    position: absolute;
    top: 0;
    right: 225px;
}

.btn-circle {
    width: 30px;
    height: 30px;
    text-align: center;
    padding: 6px 0;
    font-size: 12px;
    line-height: 1.428571429;
    border-radius: 35px;
}

.range-wrap {
    position: relative;
    margin: 0 auto 3rem;
}

.range {
    width: 100%;
}

.custom-modal-draw .modal-dialog {
    max-width: 1000px;
    width: 90%;
}

.custom-modal-draw .modal-content {
    border-radius: 12px;
}

.is-invalid {
    border-color: #ea5455 !important;
    box-shadow: 0 0 0 0.2rem rgba(234, 84, 85, 0.25) !important;
}
.range-wrap.range-tight {
    margin: 0;                 // bỏ khoảng cách trên/dưới
    margin-top: 0.25rem;       // sát label hơn
    margin-bottom: 1.25rem;    // vẫn chừa chút chỗ cho bubble
}
.range-wrap .bubble {
  position: absolute;
  top: 25px;
  /* left set bằng :style trong Vue */
  transform: translateX(-50%);
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  white-space: nowrap;
  background: #fd0d0d; // hoặc màu bạn thích
  color: #fff;
}

</style>
