<!-- eslint-disable -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row>
                        <!-- Start: input Img -->
                        <b-col md="3">
                            <b-card class="shadow-sm">
                                <div>
                                    <div
                                        class="position-relative d-flex justify-content-between align-items-center mb-1"
                                    >
                                    <b-button-group>

                                        <b-button
                                            size="sm"
                                            variant="outline-success"
                                            @click="
                                                captureEnabled = !captureEnabled
                                            "
                                        >
                                            {{
                                                $t(
                                                    'categories.employees.createDetail.infoPhoto'
                                                )
                                            }}
                                        </b-button>

                                        <b-button
                                            size="sm"
                                            variant="outline-dark"
                                            :disabled="!isEdit"
                                            @click="takePhotoCam(1)"
                                        >
                                            {{
                                                $t(
                                                    'categories.employees.createDetail.takePhoto'
                                                )
                                            }}
                                        </b-button>

                                        <b-button
                                            size="sm"
                                            variant="outline-info"
                                            :disabled="!isEdit"
                                            @click="openWebcamModal"
                                        >
                                            {{ $t('Webcam') }}
                                        </b-button>

                                        <b-button
                                            size="sm"
                                            variant="outline-primary"
                                            :disabled="!isEdit"
                                            @click="$refs.fileInput.$el.click()"
                                        >
                                            {{
                                                $t(
                                                    'categories.employees.createDetail.chooseNewPhoto'
                                                )
                                            }}
                                        </b-button>

                                        <b-button
                                            v-if="
                                                (updateEmployee.avatarBase64 ||
                                                    viewAvatar) &&
                                                isEdit
                                            "
                                            variant="outline-danger"
                                            size="sm"
                                            class="mb-0"
                                            @click="handleVerify"
                                        >
                                            {{
                                                $t(
                                                    'categories.employees.createDetail.verify'
                                                )
                                            }}
                                        </b-button>
                                    </b-button-group>
                                    </div>

                                    <!-- ẢNH TRÁI: luôn hiển thị -->
                                    <div
                                        class="d-flex flex-row align-items-center justify-content-center gap-2"
                                    >
                                        <b-img
                                            v-if="viewAvatar"
                                            :src="viewAvatar"
                                            class="preview-img flex-fill mr-1"
                                            alt="Avatar"
                                            fluid
                                            rounded
                                        />

                                        <!-- ẢNH PHẢI: chỉ hiển thị sau khi bấm Xác thực -->
                                        <b-img
                                            v-if="showVerify && faceMatch"
                                            :src="faceMatch"
                                            alt="Ảnh xác thực"
                                            fluid
                                            rounded
                                            class="preview-img flex-fill"
                                        />
                                    </div>

                                    <!-- BADGE KẾT QUẢ: chỉ hiển thị sau khi bấm Xác thực -->
                                    <b-badge
                                        v-if="showVerify"
                                        class="d-flex justify-content-center mt-1"
                                        :variant="
                                            cameraMatch ? 'success' : 'danger'
                                        "
                                    >
                                        {{
                                            cameraMatch
                                                ? 'Hợp lệ'
                                                : 'Không hợp lệ'
                                        }}
                                    </b-badge>
                                </div>

                                <!-- Hidden file input -->
                                <b-form-file
                                    ref="fileInput"
                                    accept="image/*"
                                    plain
                                    style="display: none"
                                    :placeholder="
                                        $t(
                                            'categories.employees.common.form.label.avartarPath'
                                        )
                                    "
                                    drop-placeholder="Kéo file vào đây"
                                    @change="handleFileUpload"
                                    :disabled="!isEdit"
                                />

                                <!-- Modal xem ảnh CCCD -->
                                <b-modal
                                    v-model="captureEnabled"
                                    size="lg"
                                    hide-footer
                                >
                                    <div
                                        v-if="
                                            captureEnabled ||
                                            updateEmployee.cardFront ||
                                            updateEmployee.cardBack
                                        "
                                        class="mt-3"
                                    >
                                        <ImagePreview
                                            ref="imagePreview"
                                            :card-front="`${imgUrl}/${updateEmployee.cardFront}`"
                                            :card-back="`${imgUrl}/${updateEmployee.cardBack}`"
                                            :viewOnly="true"
                                        />
                                    </div>
                                </b-modal>
                            </b-card>
                        </b-col>
                        <!-- End: input Img -->

                        <!-- Start: input Data -->
                        <b-col md="9">
                            <!-- ====== THÔNG TIN KHÁCH ====== -->
                            <b-card
                                class="shadow-sm mb-0 border-0 section-card"
                            >
                                <b-card-header class="section-header">
                                    <h5 class="mb-0 text-primary">
                                        {{
                                            $t(
                                                'categories.employees.createDetail.guestInfo'
                                            )
                                        }}
                                    </h5>
                                </b-card-header>

                                <b-card-body class="section-body">
                                    <b-row>
                                        <!-- Loại khách -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.personType'
                                                    )
                                                "
                                                label-cols-md="4"
                                                label-class="required"
                                                :class="formGroupClass"
                                            >
                                                <validation-provider
                                                    #default="{ errors }"
                                                    rules="required"
                                                    :name="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.personType'
                                                        )
                                                    "
                                                >
                                                    <v-select
                                                        v-model="
                                                            updateEmployee.personType
                                                        "
                                                        :options="
                                                            options.personType
                                                        "
                                                        :reduce="(o) => o.value"
                                                        :disabled="!isEdit"
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.personType'
                                                            )
                                                        "
                                                    />
                                                    <small
                                                        class="text-danger"
                                                        >{{ errors[0] }}</small
                                                    >
                                                </validation-provider>
                                            </b-form-group>
                                        </b-col>

                                        <!-- Công ty -->
                                        <b-col
                                            md="6"
                                            v-if="
                                                updateEmployee.personType == 2
                                            "
                                        >
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.compName'
                                                    )
                                                "
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <!-- <tree-select
                                                    v-model="
                                                        updateEmployee.compId
                                                    "
                                                    :options="options.compTree"
                                                    label="text"
                                                    :reduce="(o) => o.id"
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.compName'
                                                        )
                                                    "
                                                    :disabled="!isEdit"
                                                /> -->
                                                <b-form-input
                                                        v-model="
                                                            updateEmployee.compGuest
                                                        "
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.compName'
                                                            )
                                                        "
                                                        :disabled="!isEdit"
                                                    />
                                            </b-form-group>
                                        </b-col>

                                        <!-- Nhà thầu -->
                                        <b-col
                                            md="6"
                                            v-if="
                                                updateEmployee.personType == 3
                                            "
                                        >
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.depName'
                                                    )
                                                "
                                                label-cols-md="4"
                                                label-class="required"
                                                :class="formGroupClass"
                                            >
                                                <validation-provider
                                                    #default="{ errors }"
                                                    rules="required"
                                                    :name="
                                                        $t(
                                                            'categories.employees.common.form.label.depName'
                                                        )
                                                    "
                                                >
                                                    <tree-select
                                                        v-model="
                                                            updateEmployee.depId
                                                        "
                                                        :options="
                                                            options.departmentTree
                                                        "
                                                        label="text"
                                                        :reduce="(o) => o.id"
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.depName'
                                                            )
                                                        "
                                                        :disabled="!isEdit"
                                                    />
                                                    <small
                                                        class="text-danger"
                                                        >{{ errors[0] }}</small
                                                    >
                                                </validation-provider>
                                            </b-form-group>
                                        </b-col>

                                        <!-- Họ tên -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.fullName'
                                                    )
                                                "
                                                label-cols-md="4"
                                                label-class="required"
                                                :class="formGroupClass"
                                            >
                                                <validation-provider
                                                    #default="{ errors }"
                                                    rules="required"
                                                    :name="
                                                        $t(
                                                            'categories.employees.common.form.label.fullName'
                                                        )
                                                    "
                                                >
                                                    <b-form-input
                                                        v-model="
                                                            updateEmployee.fullname
                                                        "
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.fullname'
                                                            )
                                                        "
                                                        :state="
                                                            errors.length
                                                                ? false
                                                                : null
                                                        "
                                                        :disabled="!isEdit"
                                                    />
                                                    <small
                                                        class="text-danger"
                                                        >{{ errors[0] }}</small
                                                    >
                                                </validation-provider>
                                            </b-form-group>
                                        </b-col>

                                        <!-- CCCD -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.citizenId'
                                                    )
                                                "
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <validation-provider
                                                    #default="{ errors }"
                                                    :rules="{
                                                        regex: /^[0-9]{9,12}$/,
                                                    }"
                                                    :name="
                                                        $t(
                                                            'categories.employees.common.form.label.citizenId'
                                                        )
                                                    "
                                                >
                                                    <b-form-input
                                                        v-model="
                                                            updateEmployee.citizenId
                                                        "
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.citizenId'
                                                            )
                                                        "
                                                        :state="
                                                            errors.length > 0
                                                                ? false
                                                                : null
                                                        "
                                                        :disabled="!isEdit"
                                                    />
                                                    <small
                                                        class="text-danger"
                                                        >{{ errors[0] }}</small
                                                    >
                                                </validation-provider>
                                            </b-form-group>
                                        </b-col>

                                        <!-- Ngày sinh & Giới tính -->
                                        <b-col md="6">
                                            <b-row>
                                                <b-col md="7">
                                                    <validation-provider
                                                        v-slot="{ errors }"
                                                        :name="
                                                            $t(
                                                                'categories.employees.common.form.label.birthday'
                                                            )
                                                        "
                                                    >
                                                        <b-form-group
                                                            :label="
                                                                $t(
                                                                    'categories.employees.common.form.label.birthday'
                                                                )
                                                            "
                                                            label-cols-md="7"
                                                            :class="
                                                                formGroupClass
                                                            "
                                                        >
                                                            <date-picker
                                                                v-model="
                                                                    updateEmployee.birthday
                                                                "
                                                                type="date"
                                                                format="DD-MM-YYYY"
                                                                value-type="YYYY-MM-DD"
                                                                style="
                                                                    width: 100%;
                                                                "
                                                                :placeholder="
                                                                    $t(
                                                                        'categories.employees.common.form.placeholder.birthday'
                                                                    )
                                                                "
                                                                :locale="
                                                                    currentLocale
                                                                "
                                                                :disabled="
                                                                    !isEdit
                                                                "
                                                                input-class="form-control"
                                                            />
                                                            <small
                                                                class="text-danger"
                                                                >{{
                                                                    errors[0]
                                                                }}</small
                                                            >
                                                        </b-form-group>
                                                    </validation-provider>
                                                </b-col>
                                                <b-col md="5">
                                                    <b-form-group
                                                        :label="
                                                            $t(
                                                                'categories.employees.common.form.label.gender'
                                                            )
                                                        "
                                                        label-cols-md="4"
                                                        :class="formGroupClass"
                                                    >
                                                        <v-select
                                                            v-model="
                                                                updateEmployee.gender
                                                            "
                                                            :options="
                                                                options.gender
                                                            "
                                                            :reduce="
                                                                (o) => o.value
                                                            "
                                                            :placeholder="
                                                                $t(
                                                                    'categories.employees.common.form.placeholder.gender'
                                                                )
                                                            "
                                                            :disabled="!isEdit"
                                                        />
                                                    </b-form-group>
                                                </b-col>
                                            </b-row>
                                        </b-col>

                                        <!-- Chức vụ -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.position'
                                                    )
                                                "
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    v-model="
                                                        updateEmployee.position
                                                    "
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.position'
                                                        )
                                                    "
                                                    :disabled="!isEdit"
                                                />
                                            </b-form-group>
                                        </b-col>

                                        <!-- Điện thoại -->
                                        <b-col md="6">
                                            <validation-provider
                                                v-slot="{ errors }"
                                                :name="
                                                    $t(
                                                        'categories.employees.common.form.label.phoneNumber'
                                                    )
                                                "
                                                rules="phone"
                                            >
                                                <b-form-group
                                                    :label="
                                                        $t(
                                                            'categories.employees.common.form.label.phoneNumber'
                                                        )
                                                    "
                                                    label-cols-md="4"
                                                    :class="formGroupClass"
                                                >
                                                    <b-form-input
                                                        v-model="
                                                            updateEmployee.phoneNumber
                                                        "
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.phoneNumber'
                                                            )
                                                        "
                                                        :disabled="!isEdit"
                                                    />
                                                    <small
                                                        class="text-danger"
                                                        >{{ errors[0] }}</small
                                                    >
                                                </b-form-group>
                                            </validation-provider>
                                        </b-col>

                                        <!-- Email -->
                                        <b-col md="6">
                                            <validation-provider
                                                v-slot="{ errors }"
                                                name="Email"
                                                rules="email"
                                            >
                                                <b-form-group
                                                    :label="
                                                        $t(
                                                            'categories.employees.common.form.label.email'
                                                        )
                                                    "
                                                    label-cols-md="4"
                                                    :class="formGroupClass"
                                                >
                                                    <b-form-input
                                                        v-model="
                                                            updateEmployee.email
                                                        "
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.email'
                                                            )
                                                        "
                                                        :disabled="!isEdit"
                                                    />
                                                    <small
                                                        class="text-danger"
                                                        >{{ errors[0] }}</small
                                                    >
                                                </b-form-group>
                                            </validation-provider>
                                        </b-col>

                                        <!-- Nhóm -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.groupName'
                                                    )
                                                "
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <v-select
                                                    v-model="
                                                        updateEmployee.groupId
                                                    "
                                                    :options="options.groups"
                                                    :reduce="
                                                        (i) => parseInt(i.id)
                                                    "
                                                    label="text"
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.groupName'
                                                        )
                                                    "
                                                    :disabled="!isEdit"
                                                    append-to-body
                                                />
                                            </b-form-group>
                                        </b-col>

                                        <!-- Địa chỉ -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.address'
                                                    )
                                                "
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    v-model="
                                                        updateEmployee.address
                                                    "
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.address'
                                                        )
                                                    "
                                                    :disabled="!isEdit"
                                                />
                                            </b-form-group>
                                        </b-col>
                                    </b-row>
                                </b-card-body>
                            </b-card>

                            <!-- ====== THÔNG TIN RA VÀO ====== -->
                            <b-card
                                class="shadow-sm mb-0 border-0 section-card"
                            >
                                <b-card-header class="section-header">
                                    <h5 class="mb-0 text-primary">
                                        {{
                                            $t(
                                                'categories.employees.createDetail.entryAndExitInfo'
                                            )
                                        }}
                                    </h5>
                                </b-card-header>

                                <b-card-body class="section-body">
                                    <b-row>
                                        <!-- Số thẻ -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.cardNumber'
                                                    )
                                                "
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    v-model="
                                                        updateEmployee.cardId
                                                    "
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.cardNumber'
                                                        )
                                                    "
                                                    :disabled="!isEdit"
                                                />
                                            </b-form-group>
                                        </b-col>

                                        <!-- Người tiếp nhận -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.receiver'
                                                    )
                                                "
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    v-model="
                                                        updateEmployee.receiver
                                                    "
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.receiver'
                                                        )
                                                    "
                                                    :disabled="!isEdit"
                                                />
                                            </b-form-group>
                                        </b-col>

                                        <!-- Ghi chú -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.notes'
                                                    )
                                                "
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    v-model="
                                                        updateEmployee.notes
                                                    "
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.notes'
                                                        )
                                                    "
                                                    :disabled="!isEdit"
                                                />
                                            </b-form-group>
                                        </b-col>

                                        <!-- Liên hệ -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.contactor'
                                                    )
                                                "
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    v-model="
                                                        updateEmployee.contactor
                                                    "
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.contactor'
                                                        )
                                                    "
                                                    :disabled="!isEdit"
                                                />
                                            </b-form-group>
                                        </b-col>

                                        <!-- Bắt đầu -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.startTime'
                                                    )
                                                "
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-row>
                                                    <b-col md="4">
                                                        <date-picker
                                                            v-model="
                                                                startTimeTime
                                                            "
                                                            type="time"
                                                            format="HH:mm"
                                                            value-type="HH:mm"
                                                            style="width: 100%"
                                                            :disabled="!isEdit"
                                                        />
                                                    </b-col>
                                                    <b-col md="8">
                                                        <date-picker
                                                            v-model="
                                                                startTimeDate
                                                            "
                                                            type="date"
                                                            format="DD-MM-YYYY"
                                                            value-type="YYYY-MM-DD"
                                                            style="width: 100%"
                                                            :disabled="!isEdit"
                                                        />
                                                    </b-col>
                                                </b-row>
                                            </b-form-group>
                                        </b-col>

                                        <!-- Kết thúc -->
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.endTime'
                                                    )
                                                "
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-row>
                                                    <b-col md="4">
                                                        <date-picker
                                                            v-model="
                                                                endTimeTime
                                                            "
                                                            type="time"
                                                            format="HH:mm"
                                                            value-type="HH:mm"
                                                            style="width: 100%"
                                                            :disabled="!isEdit"
                                                        />
                                                    </b-col>
                                                    <b-col md="8">
                                                        <date-picker
                                                            v-model="
                                                                endTimeDate
                                                            "
                                                            type="date"
                                                            format="DD-MM-YYYY"
                                                            value-type="YYYY-MM-DD"
                                                            style="width: 100%"
                                                            :disabled="!isEdit"
                                                        />
                                                    </b-col>
                                                </b-row>
                                            </b-form-group>
                                        </b-col>
                                    </b-row>
                                </b-card-body>
                            </b-card>

                            <b-card>
                                <AccessControl
                                    ref="accessControl"
                                    :disabled="!isEdit"
                                    :check-in="updateEmployee.startTime" 
                                    :check-out="updateEmployee.endTime"
                                />
                            </b-card>
                        </b-col>
                        <!-- End: input Data -->
                    </b-row>

                    <!-- Button Action -->
                    <div class="text-center">
                        <b-button
                            v-if="!isEdit"
                            type="button"
                            variant="primary"
                            class="mx-50 mb-50 btn-120"
                            @click.prevent="handleEdit"
                        >
                            {{ $t('common.button.edit') }}
                        </b-button>

                        <b-button
                            v-if="authorize(['ManageEmployee']) && isEdit"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>

                        <b-button
                            @click="navigateToList()"
                            type="button"
                            variant="outline-secondary"
                            title="Cancel"
                            class="btn-120 mb-50"
                        >
                            {{ $t('common.button.cancel') }}
                        </b-button>
                    </div>
                </b-form>
            </b-card-body>
        </b-card>

        <!-- Modal Webcam -->
        <b-modal
            id="webcam-modal"
            ref="webcamModal"
            title="Chụp ảnh từ Webcam"
            size="lg"
            :ok-only="true"
            ok-title="Đóng"
            @shown="startWebcam"
            @hidden="stopWebcam"
            @ok="stopWebcam"
        >
            <div class="text-center">
                <video
                    ref="webcamVideo"
                    autoplay
                    muted
                    playsinline
                    width="100%"
                    height="auto"
                    style="border-radius: 8px; max-height: 400px"
                ></video>

                <div class="mt-3">
                    <b-button
                        variant="primary"
                        @click="capturePhoto"
                        :disabled="!isCameraReady"
                    >
                        Chụp ảnh
                    </b-button>

                    <b-img
                        v-if="capturedPhoto"
                        :src="capturedPhoto"
                        alt="Ảnh chụp"
                        fluid
                        rounded
                        class="mt-3"
                        style="max-height: 200px"
                    />
                </div>

                <div v-if="webcamError" class="alert alert-danger mt-2">
                    {{ webcamError }}
                </div>
            </div>
        </b-modal>
    </validation-observer>
</template>

<script>
/*eslint-disable*/
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import TreeHelper from '@/utils/treeHelper'
import AccessControl from './AccessControl.vue'
import ImagePreview from './ImagePreview.vue'
import { email } from 'vuelidate/lib/validators'

export default {
    components: { AccessControl, ImagePreview },
    mixins: [authorizationMixin],
    data() {
        return {
            // verify & webcam
            showVerify: false, // CHỈ hiển thị khối verify khi true
            cameraMatch: false,
            faceMatch: null, // base64 ảnh chụp khi xác thực (không lưu server)
            webcamStream: null,
            isCameraReady: false,
            capturedPhoto: null,
            webcamError: null,

            captureEnabled: false,
            options: {
                compTree: [],
                groups: [],
                departmentTree: [],
                areaTree: null,
                gender: [
                    { value: 0, label: 'Nữ' },
                    { value: 1, label: 'Nam' },
                ],
                status: [
                    { value: 1, label: 'Đang hoạt động' },
                    { value: 2, label: 'Ngừng hoạt động' },
                ],
                personType: [
                    { value: 2, label: 'Khách' },
                    { value: 3, label: 'Nhà thầu' },
                ],
            },
            updateEmployee: {
                compId: null,
                compGuest: null,
                depId: null,
                groupId: null,
                code: null,
                fullname: null,
                jobDuties: null,
                position: null,
                birthday: null,
                gender: null,
                phoneNumber: null,
                email: null,
                avatarBase64: null,
                citizenId: null,
                cardId: null,
                receiver: null,
                address: null,
                startTime: null,
                endTime: null,
                notes: null,
                cardFront: null,
                cardBack: null,
                faceMatch: null, // giữ để tương thích payload BE nếu muốn
                contactor: null,
                personType: 2,
            },
            viewAvatar: null, // dataURL hoặc URL ảnh hiện tại (chỉ để hiển thị)
            isEdit: false,
        }
    },
    computed: {
        endTimeDate: {
            get() {
                if (!this.updateEmployee.endTime) return null
                return this.updateEmployee.endTime.split(' ')[0]
            },
            set(v) {
                const t = this.updateEmployee.endTime
                    ? this.updateEmployee.endTime.split(' ')[1]
                    : '00:00:00'
                this.updateEmployee.endTime = v ? `${v} ${t}` : null
            },
        },
        endTimeTime: {
            get() {
                if (!this.updateEmployee.endTime) return null
                return this.updateEmployee.endTime.split(' ')[1]
            },
            set(v) {
                const d = this.updateEmployee.endTime
                    ? this.updateEmployee.endTime.split(' ')[0]
                    : this.$moment().format('YYYY-MM-DD')
                this.updateEmployee.endTime = v ? `${d} ${v}` : null
            },
        },
        startTimeDate: {
            get() {
                if (!this.updateEmployee.startTime) return null
                return this.updateEmployee.startTime.split(' ')[0]
            },
            set(v) {
                const t = this.updateEmployee.startTime
                    ? this.updateEmployee.startTime.split(' ')[1]
                    : '00:00:00'
                this.updateEmployee.startTime = v ? `${v} ${t}` : null
            },
        },
        startTimeTime: {
            get() {
                if (!this.updateEmployee.startTime) return null
                return this.updateEmployee.startTime.split(' ')[1]
            },
            set(v) {
                const d = this.updateEmployee.startTime
                    ? this.updateEmployee.startTime.split(' ')[0]
                    : this.$moment().format('YYYY-MM-DD')
                this.updateEmployee.startTime = v ? `${d} ${v}` : null
            },
        },
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        imgUrl() {
            const { VUE_APP_BASE_URL: baseURL } = process.env
            return `${baseURL}/Employees`
        },
        currentLocale() {
            return this.$i18n ? this.$i18n.locale : 'vi'
        },
    },
    async created() {
        await this.loadOptions()
        await this.getData()
        const accessToken = this.$services.getUserData()
        // this.updateEmployee.contactor = accessToken.fullName
    },
    methods: {
        // ---------- Webcam ----------
        openWebcamModal() {
            this.capturedPhoto = null
            this.webcamError = null
            this.$refs.webcamModal.show()
        },
        async startWebcam() {
            try {
                this.webcamStream = await navigator.mediaDevices.getUserMedia({
                    video: {
                        width: { ideal: 640 },
                        height: { ideal: 480 },
                        facingMode: 'user',
                    },
                })
                this.$refs.webcamVideo.srcObject = this.webcamStream
                this.isCameraReady = true
            } catch (error) {
                console.error('Webcam error:', error)
                this.webcamError =
                    'Không thể truy cập webcam. Vui lòng kiểm tra quyền truy cập camera.'
                this.isCameraReady = false
            }
        },
        stopWebcam() {
            if (this.webcamStream) {
                this.webcamStream.getTracks().forEach((t) => t.stop())
                this.webcamStream = null
            }
            this.isCameraReady = false
        },
        capturePhoto() {
            const video = this.$refs.webcamVideo
            const canvas = document.createElement('canvas')
            const ctx = canvas.getContext('2d')
            canvas.width = video.videoWidth
            canvas.height = video.videoHeight
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height)
            const base64 = canvas.toDataURL('image/jpeg', 0.8)
            this.capturedPhoto = base64
            this.updateEmployee.avatarBase64 = base64
            this.viewAvatar = base64
            this.resetVerifyUi()
            this.$nextTick(() => this.$bvModal.hide('webcam-modal'))
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: 'Thành công',
                    text: 'Ảnh đã được chụp và lưu!',
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },

        // ---------- Helper: convert URL -> dataURL base64 ----------
        async urlToDataUrl(url) {
            const res = await fetch(url, { credentials: 'include' })
            const blob = await res.blob()
            return await new Promise((resolve) => {
                const reader = new FileReader()
                reader.onloadend = () => resolve(reader.result) // data:*;base64,...
                reader.readAsDataURL(blob)
            })
        },
        async getFaceBase64Payload() {
            if (
                this.updateEmployee.avatarBase64 &&
                typeof this.updateEmployee.avatarBase64 === 'string' &&
                this.updateEmployee.avatarBase64.startsWith('data:')
            ) {
                return this.updateEmployee.avatarBase64.split(',')[1]
            }
            if (this.viewAvatar && this.viewAvatar.startsWith('data:')) {
                return this.viewAvatar.split(',')[1]
            }
            if (
                this.viewAvatar &&
                (/^https?:\/\//.test(this.viewAvatar) ||
                    this.viewAvatar.startsWith('/'))
            ) {
                try {
                    const dataUrl = await this.urlToDataUrl(this.viewAvatar)
                    return dataUrl.split(',')[1]
                } catch (e) {
                    console.error('Convert URL->base64 error', e)
                    return null
                }
            }
            return null
        },

        // ---------- Chụp/Verify qua WS app local ----------
        takePhotoCam(typeTakePhoto = 1) {
            if (typeTakePhoto === 1) return this._takePhotoWS()
            return this.handleVerify()
        },
        _takePhotoWS() {
            if (!this.isEdit) return
            let requestData = { type: 4 }
            this._openWsAndSend(requestData, (data) => {
                if (data && data.faceImage) {
                    const img = 'data:image/jpeg;base64,' + data.faceImage
                    this.updateEmployee.avatarBase64 = img
                    this.viewAvatar = img
                    this.resetVerifyUi()
                } else {
                    this._toastError('Vui lòng thử lại!')
                }
            })
        },
        async handleVerify() {
            if (!this.isEdit) return
            const b64 = await this.getFaceBase64Payload()
            if (!b64) {
                this._toastError(
                    'Chưa có ảnh nguồn để xác thực (chụp/chọn ảnh trước).'
                )
                return
            }
            const requestData = { type: 7, data: b64, version: 2 }
            this._openWsAndSend(requestData, (data) => {
                this.faceMatch = data?.faceCapture
                    ? 'data:image/jpeg;base64,' + data.faceCapture
                    : null
                this.cameraMatch = !!data?.isSamePerson
                this.showVerify = true
            })
        },
        _openWsAndSend(requestData, onMessageCb) {
            this.websocket = new WebSocket('ws://localhost:9999')
            let messageReceived = false
            const timeout = setTimeout(() => {
                if (!messageReceived && this.websocket?.readyState === 1) {
                    console.log(
                        'No message received in 30s. Closing WebSocket.'
                    )
                    this.websocket.close()
                }
            }, 30000)

            this.websocket.onopen = () => {
                try {
                    this.websocket.send(JSON.stringify(requestData))
                } catch (e) {
                    console.error('WS send error:', e)
                }
            }
            this.websocket.onmessage = (jsonData) => {
                messageReceived = true
                clearTimeout(timeout)
                try {
                    const data = JSON.parse(jsonData.data)
                    onMessageCb && onMessageCb(data)
                } catch (e) {
                    console.error('WS parse error:', e)
                } finally {
                    this.websocket.close()
                }
            }
            this.websocket.onerror = (err) => {
                console.error('WebSocket error:', err)
            }
            this.websocket.onclose = () => {
                console.log('WebSocket connection closed.')
            }
        },
        resetVerifyUi() {
            // Ẩn hoàn toàn khối xác thực khi đổi ảnh hoặc load lại
            this.showVerify = false
            this.cameraMatch = false
            this.faceMatch = null
        },
        _toastError(text) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: 'Lỗi',
                    text,
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                },
            })
        },

        // ---------- Load/Get ----------
        async getData() {
            const setDate = (dateStr) => {
                if(!dateStr)
                return null;
                const d = new Date(dateStr)
                const yyyy = d.getFullYear()
                const MM = String(d.getMonth() + 1).padStart(2, '0')
                const dd = String(d.getDate()).padStart(2, '0')
                const HH = String(d.getHours()).padStart(2, '0')
                const mm = String(d.getMinutes()).padStart(2, '0')
                const ss = String(d.getSeconds()).padStart(2, '0')
                return `${yyyy}-${MM}-${dd} ${HH}:${mm}:${ss}`
            }
            try {
                const res = await this.$services.get(
                    `/employees/${this.$route.params.id}`
                )
                this.updateEmployee = res.data.data
                this.updateEmployee.groupId =
                    this.updateEmployee.groupId &&
                    this.updateEmployee.groupId > 0
                        ? parseInt(this.updateEmployee.groupId)
                        : null
                this.updateEmployee.startTime = setDate(res.data.data.startTime)
                this.updateEmployee.endTime = setDate(res.data.data.endTime)
                this.updateEmployee.contactor = res.data.data.contactor

                // Ảnh hiện tại từ server (URL)
                this.viewAvatar =
                    this.imgUrl + '/' + this.updateEmployee.avartarPath

                // VÀO LẠI TRANG: KHÔNG tự hiện verify
                this.resetVerifyUi()

                this.getAccessControllGuess()
            } catch (error) {
                console.log('error')
            }
        },
        async getAccessControllGuess() {
            const setDate = (dateStr) => {
                if(!dateStr)
                return null;
                const d = new Date(dateStr)
                const yyyy = d.getFullYear()
                const MM = String(d.getMonth() + 1).padStart(2, '0')
                const dd = String(d.getDate()).padStart(2, '0')
                const HH = String(d.getHours()).padStart(2, '0')
                const mm = String(d.getMinutes()).padStart(2, '0')
                const ss = String(d.getSeconds()).padStart(2, '0')

                return `${yyyy}-${MM}-${dd} ${HH}:${mm}:${ss}`
            }
            const roles = await this.$services.get(
                `/accessControllGuess/employee/${this.$route.params.id}`
            )
            console.log('roles', roles)
            if (roles.data.length === 0) return
            const accessControl = roles.data.map((item) => {
                return {
                ...item,
                areaId : item.areaId,
                startTime: setDate(item.startTime),
                endTime: setDate(item.endTime),
            }
            })
            console.log('accessControl', accessControl)
            this.$refs.accessControl.setAccessControl(accessControl)
        },

        handleFileUpload(event) {
            const file = event.target.files[0]
            if (file) {
                const reader = new FileReader()
                reader.onload = (e) => {
                    const base64 = e.target.result // data:*;base64,...
                    this.updateEmployee.avatarBase64 = base64
                    this.viewAvatar = base64
                    this.resetVerifyUi()
                }
                reader.onerror = () =>
                    this._toastError('Không thể đọc file, vui lòng thử lại.')
                reader.readAsDataURL(file)
            } else {
                this.updateEmployee.avatarBase64 = ''
            }
        },

        async handleEdit() {
            this._backup = JSON.parse(JSON.stringify(this.updateEmployee))
            this._backupAvatar = this.viewAvatar
            this.isEdit = true
            await this.loadOptions()
        },
        async loadOptions() {
            await this.$services.get('/lookup/company-tree').then((r) => {
                this.options.compTree = TreeHelper.removeEmptyChildren(r.data)
            })
            await this.$services.get('/lookup/groupsNotCompId').then((r) => {
                const userCompanyId = this.$services.getUserData().companyId
                this.options.groups = (r.data.data || []).filter(x => x.compId == userCompanyId)
            })
            await this.$services
                .get('/lookup/departments-tree?type=2')
                .then((r) => {
                    this.options.departmentTree = r.data.data
                })
            const response = await this.$services.get('/lookup/areas-tree')
            this.options.areaTree = TreeHelper.removeEmptyChildren(
                response.data.data
            )
        },

        async onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                if (!success) return

                // Chặn update khi thời gian phân quyền truy cập không hợp lệ
                if (
                    this.$refs.accessControl &&
                    !this.$refs.accessControl.validateAccessTimes()
                ) {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: 'Lỗi',
                            text:
                                this.$t(
                                    'categories.employees.error.accessTimeOutOfRange'
                                ) ||
                                'Thời gian phân quyền truy cập không hợp lệ',
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                        },
                    })
                    return
                }

                try {
                    // Không ép gửi faceMatch (vì chỉ là preview cục bộ). Nếu BE cần, bật dòng dưới:
                    // const payload = { ...this.updateEmployee, faceMatch: this.showVerify && this.faceMatch?.startsWith('data:') ? this.faceMatch : null }
                    const payload = { ...this.updateEmployee,
                        notes: this.updateEmployee.notes?.trim() || '',
                        address: this.updateEmployee.address?.trim() || '',
                        cardId: this.updateEmployee.cardId?.trim() || '',
                        receiver: this.updateEmployee.receiver?.trim() || '',
                        contactor: this.updateEmployee.contactor?.trim() || '',
                        compGuest: this.updateEmployee.compGuest?.trim() || '',
                        position: this.updateEmployee.position?.trim() || '',
                        fullname: this.updateEmployee.fullname?.trim() || '',
                     }
                    const res = await this.$services.put(
                        '/employees/' + this.$route.params.id,
                        payload
                    )
                    this.showSuccessToast(res.data)

                    await this.$services.put(
                        `/accessControllGuess/employee/${this.$route.params.id}`,
                        this.$refs.accessControl.getAccessControl()
                    )
                    this.isEdit = false
                    this.navigateToList()
                } catch (error) {
                    this.showErrorToast(error)
                }
            })
        },

        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(
                        `categories.guess.error.${data.errorCode || 'C_EMPLOYEES_200'}`
                    ),
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
                    title: this.$t('Error.Error'),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(
                        `categories.guess.error.${error?.errorCode || error?.message || 'UNKNOWN_ERROR'}`
                ),
                    
                },
            })
        },
        async navigateToList() {
            if (this.isEdit) {
                // Restore dữ liệu gốc trước khi disable input
                if (this._backup) {
                    Object.keys(this._backup).forEach(key => {
                        this.$set(this.updateEmployee, key, this._backup[key])
                    })
                    this.viewAvatar = this._backupAvatar
                    this._backup = null
                    this._backupAvatar = null
                }
                this.resetVerifyUi()
                await this.$nextTick()
                this.isEdit = false
                this.getAccessControllGuess()
            } else {
                this.$router.push({ path: '/categories/guess/list' })
            }
        },
    },
}
</script>

<style lang="scss" scoped>
.b-card {
    border-radius: 0.75rem;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08) !important;
    overflow: hidden;
}
.b-card-header {
    border-bottom: none;
    font-size: 1rem;
    letter-spacing: 0.5px;
}
.preview-img {
    width: 45%;
    aspect-ratio: 1 / 1.2;
    object-fit: cover;
    border-radius: 8px;
}
.card-body {
    padding: 0.5rem;
}
.section-card {
    background-color: #ffffff;
    border-radius: 0.75rem;
    overflow: hidden;
    padding: 0.75rem -1rem;
}
.section-header {
    background-color: #f8f9fa;
    border: 1px solid #dee2e6;
    border-bottom: none;
    padding: 0.75rem 1rem;
    border-top-left-radius: 0.75rem;
    border-top-right-radius: 0.75rem;
}
.section-body {
    border: 1px solid #dee2e6;
    border-top: none;
    border-bottom-left-radius: 0.75rem;
    border-bottom-right-radius: 0.75rem;
    background-color: #fcfcfc;
    padding: 0 1.5rem 0 1.25rem;
}
h5.text-primary {
    font-weight: 600;
    font-size: 1.1rem;
    letter-spacing: 0.3px;
}
.required::after {
    content: ' *';
    color: red;
}
</style>
