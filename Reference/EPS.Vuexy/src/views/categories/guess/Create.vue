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
                                <CardReader
                                    ref="CardReader"
                                    class="mb-2"
                                    @person-info="onPersonInfo"
                                />
                                <hr />
                                <div>
                                    <div
                                        class="position-relative d-flex justify-content-between align-items-center mb-1"
                                    >
                                        <b-button
                                            size="sm"
                                            variant="outline-success"
                                            @click="takePhotoCam(1)"
                                        >
                                            {{
                                                $t(
                                                    'categories.employees.createDetail.takePhoto'
                                                )
                                            }}</b-button
                                        >
                                        <!-- <b-button size="sm"  variant="outline-success" @click="takePhotoWebcam">{{$t("Webcam")}}</b-button> -->
                                        <b-button
                                            size="sm"
                                            variant="outline-info"
                                            @click="openWebcamModal"
                                            >{{ $t('Webcam') }}</b-button
                                        >
                                        <b-button
                                            size="sm"
                                            variant="outline-primary"
                                            @click="$refs.fileInput.$el.click()"
                                        >
                                            {{
                                                $t(
                                                    'categories.employees.createDetail.choosePhoto'
                                                )
                                            }}
                                        </b-button>
                                        <b-button
                                            v-if="newEmployee.avatarBase64"
                                            variant="outline-danger"
                                            size="sm"
                                            class="mb-0"
                                            @click="takePhotoCam(2)"
                                        >
                                            {{
                                                $t(
                                                    'categories.employees.createDetail.verify'
                                                )
                                            }}
                                        </b-button>
                                    </div>
                                    <div
                                        class="d-flex flex-row align-items-center justify-content-center gap-2"
                                    >
                                        <b-img
                                            v-if="newEmployee.avatarBase64"
                                            :src="newEmployee.avatarBase64"
                                            alt="Ảnh 1"
                                            fluid
                                            rounded
                                            class="preview-img flex-fill mr-1"
                                        />

                                        <b-img
                                            v-if="faceMatch"
                                            :src="faceMatch"
                                            alt="Ảnh 2"
                                            fluid
                                            rounded
                                            class="preview-img flex-fill"
                                        />
                                    </div>
                                    <b-badge
                                        v-if="faceMatch"
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
                                    <b-form-file
                                        ref="fileInput"
                                        accept="image/*"
                                        plain
                                        style="display: none"
                                        :placeholder="
                                            $t(
                                                'categories.employees.common.form.label.avatarPath'
                                            )
                                        "
                                        drop-placeholder="Kéo file vào đây"
                                        @change="handleFileUpload"
                                    />
                                </div>
                            </b-card>
                        </b-col>
                        <!-- End: input Img -->
                        <!-- Start: input Data -->
                        <b-col md="9">
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
                                                        id="h-gender"
                                                        v-model="
                                                            newEmployee.PersonType
                                                        "
                                                        :options="
                                                            options.personType
                                                        "
                                                        :reduce="
                                                            (option) =>
                                                                option.value
                                                        "
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

                                        <b-col
                                            md="6"
                                            v-if="newEmployee.PersonType !== 3"
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
                                                    v-model="newEmployee.compId"
                                                    :options="options.compTree"
                                                    label="text"
                                                    :reduce="
                                                        (option) => option.id
                                                    "
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.compName'
                                                        )
                                                    "
                                                /> -->
                                                <b-form-input
                                                        id="h-compName"
                                                        v-model="
                                                            newEmployee.compGuest
                                                        "
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.compName'
                                                            )
                                                        "
                                                        
                                                    />
                                            </b-form-group>
                                        </b-col>
                                        <b-col
                                            md="6"
                                            v-if="newEmployee.PersonType == 3"
                                        >
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.contractor'
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
                                                            'categories.employees.common.form.placeholder.contractor'
                                                        )
                                                    "
                                                >
                                                    <tree-select
                                                        v-model="
                                                            newEmployee.depId
                                                        "
                                                        :options="
                                                            options.departmentTree
                                                        "
                                                        label="text"
                                                        :reduce="
                                                            (option) =>
                                                                option.id
                                                        "
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.contractor'
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
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.fullName'
                                                    )
                                                "
                                                label-for="h-fullname"
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
                                                        id="h-fullname"
                                                        v-model="
                                                            newEmployee.fullname
                                                        "
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.fullName'
                                                            )
                                                        "
                                                        :state="
                                                            errors.length
                                                                ? false
                                                                : null
                                                        "
                                                    />
                                                    <small
                                                        class="text-danger"
                                                        >{{ errors[0] }}</small
                                                    >
                                                </validation-provider>
                                            </b-form-group>
                                        </b-col>
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.citizenId'
                                                    )
                                                "
                                                label-for="h-citizenId"
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
                                                        id="h-citizenId"
                                                        v-model="
                                                            newEmployee.citizenId
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
                                                    />
                                                    <small
                                                        class="text-danger"
                                                        >{{ errors[0] }}</small
                                                    >
                                                </validation-provider>
                                            </b-form-group>
                                        </b-col>

                                        <b-col md="6">
                                            <b-row>
                                                <b-col md="7">
                                                    <b-form-group
                                                        :label="
                                                            $t(
                                                                'categories.employees.common.form.label.birthday'
                                                            )
                                                        "
                                                        label-for="h-birthday"
                                                        label-cols-md="7"
                                                        :class="formGroupClass"
                                                    >
                                                        <date-picker
                                                            id="h-birthday"
                                                            v-model="
                                                                newEmployee.birthday
                                                            "
                                                            type="date"
                                                            format="DD-MM-YYYY"
                                                            value-type="YYYY-MM-DD"
                                                            style="width: 100%"
                                                            :placeholder="
                                                                $t(
                                                                    'categories.employees.common.form.placeholder.birthday'
                                                                )
                                                            "
                                                            input-class="form-control"
                                                        />
                                                        <!-- <b-form-datepicker
                                                    id="h-birthday"
                                                    v-model="
                                                        newEmployee.birthday
                                                    "
                                                    :date-format-options="{
                                                        day: 'numeric',
                                                        month: 'long',
                                                        year: 'numeric',
                                                    }"
                                                    :locale="currentLocale"
                                                /> -->
                                                    </b-form-group>
                                                </b-col>
                                                <b-col md="5">
                                                    <b-form-group
                                                        :label="
                                                            $t(
                                                                'categories.employees.common.form.label.gender'
                                                            )
                                                        "
                                                        label-for="h-gender"
                                                        label-cols-md="4"
                                                        :class="formGroupClass"
                                                    >
                                                        <v-select
                                                            id="h-gender"
                                                            v-model="
                                                                newEmployee.gender
                                                            "
                                                            :options="
                                                                options.gender
                                                            "
                                                            :reduce="
                                                                (option) =>
                                                                    option.value
                                                            "
                                                            :placeholder="
                                                                $t(
                                                                    'categories.employees.common.form.placeholder.gender'
                                                                )
                                                            "
                                                        />
                                                    </b-form-group>
                                                </b-col>
                                            </b-row>
                                        </b-col>

                                        <!-- <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.jobDuties'
                                            )
                                        "
                                        label-for="h-jobDuties"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <b-form-input
                                            id="h-jobDuties"
                                            v-model="newEmployee.jobDuties"
                                            trim
                                            :placeholder="
                                                $t(
                                                    'categories.employees.common.form.placeholder.jobDuties'
                                                )
                                            "
                                        />
                                    </b-form-group>
                                </b-col> -->

                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.position'
                                                    )
                                                "
                                                label-for="h-position"
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    id="h-position"
                                                    v-model="
                                                        newEmployee.position
                                                    "
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.position'
                                                        )
                                                    "
                                                />
                                            </b-form-group>
                                        </b-col>
                                        <b-col md="6">
                                            <validation-provider
                                                v-slot="{ errors }"
                                                name="Số điện thoại"
                                                rules="phone"
                                            >
                                                <b-form-group
                                                    :label="
                                                        $t(
                                                            'categories.employees.common.form.label.phoneNumber'
                                                        )
                                                    "
                                                    label-for="h-phoneNumber"
                                                    label-cols-md="4"
                                                    :class="formGroupClass"
                                                >
                                                    <b-form-input
                                                        id="h-phoneNumber"
                                                        v-model="
                                                            newEmployee.phoneNumber
                                                        "
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.phoneNumber'
                                                            )
                                                        "
                                                    />
                                                    <small
                                                        class="text-danger"
                                                        >{{ errors[0] }}</small
                                                    >
                                                </b-form-group>
                                            </validation-provider>
                                        </b-col>

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
                                                    label-for="h-email"
                                                    label-cols-md="4"
                                                    :class="formGroupClass"
                                                >
                                                    <b-form-input
                                                        id="h-email"
                                                        v-model="
                                                            newEmployee.email
                                                        "
                                                        :placeholder="
                                                            $t(
                                                                'categories.employees.common.form.placeholder.email'
                                                            )
                                                        "
                                                    />
                                                    <small
                                                        class="text-danger"
                                                        >{{ errors[0] }}</small
                                                    >
                                                </b-form-group>
                                            </validation-provider>
                                        </b-col>
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
                                                        newEmployee.groupId
                                                    "
                                                    :options="options.groups"
                                                    :reduce="
                                                        (item) =>
                                                            parseInt(item.id)
                                                    "
                                                    label="text"
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.groupName'
                                                        )
                                                    "
                                                    append-to-body
                                                />
                                            </b-form-group>
                                        </b-col>
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.address'
                                                    )
                                                "
                                                label-for="h-address"
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    id="h-address"
                                                    v-model="
                                                        newEmployee.address
                                                    "
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.address'
                                                        )
                                                    "
                                                />
                                            </b-form-group>
                                        </b-col>
                                    </b-row>
                                </b-card-body>
                            </b-card>
                            <b-card
                                class="shadow-sm mb-0 border-0 section-card1"
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
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.cardNumber'
                                                    )
                                                "
                                                label-for="h-cardNumber"
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    id="h-cardNumber"
                                                    v-model="newEmployee.cardId"
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.cardNumber'
                                                        )
                                                    "
                                                />
                                            </b-form-group>
                                        </b-col>

                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.receiver'
                                                    )
                                                "
                                                label-for="h-receiver"
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    id="h-receiver"
                                                    v-model="
                                                        newEmployee.receiver
                                                    "
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.receiver'
                                                        )
                                                    "
                                                />
                                            </b-form-group>
                                        </b-col>

                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.notes'
                                                    )
                                                "
                                                label-for="h-notes"
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    id="h-notes"
                                                    v-model="newEmployee.notes"
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.notes'
                                                        )
                                                    "
                                                />
                                            </b-form-group>
                                        </b-col>
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.contactor'
                                                    )
                                                "
                                                label-for="h-contactor"
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <b-form-input
                                                    id="h-contactor"
                                                    v-model="
                                                        newEmployee.contactor
                                                    "
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.contactor'
                                                        )
                                                    "
                                                />
                                            </b-form-group>
                                        </b-col>
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.startTime'
                                                    )
                                                "
                                                label-for="h-startTime"
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <!-- <date-picker
                                                    id="h-startTime"
                                                    v-model="
                                                        newEmployee.startTime
                                                    "
                                                    type="datetime"
                                                    format="DD-MM-YYYY HH:mm:ss"
                                                    value-type="YYYY-MM-DD HH:mm:ss"
                                                    style="width: 100%"
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.startTime'
                                                        )
                                                    "
                                                    input-class="form-control"
                                                /> -->
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
                                                        />
                                                    </b-col>
                                                </b-row>
                                            </b-form-group>
                                        </b-col>
                                        <b-col md="6">
                                            <b-form-group
                                                :label="
                                                    $t(
                                                        'categories.employees.common.form.label.endTime'
                                                    )
                                                "
                                                label-for="h-endTime"
                                                label-cols-md="4"
                                                :class="formGroupClass"
                                            >
                                                <!-- <date-picker
                                                    id="h-endTime"
                                                    v-model="
                                                        newEmployee.endTime
                                                    "
                                                    type="datetime"
                                                    format="DD-MM-YYYY HH:mm:ss"
                                                    value-type="YYYY-MM-DD HH:mm:ss"
                                                    style="width: 100%"
                                                    :placeholder="
                                                        $t(
                                                            'categories.employees.common.form.placeholder.endTime'
                                                        )
                                                    "
                                                    input-class="form-control"
                                                /> -->
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
                                                            :class="
                                                                endTimeValidationError
                                                                    ? 'is-invalid'
                                                                    : ''
                                                            "
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
                                                            :class="
                                                                endTimeValidationError
                                                                    ? 'is-invalid'
                                                                    : ''
                                                            "
                                                        />
                                                    </b-col>
                                                </b-row>
                                                <small
                                                    v-if="
                                                        endTimeValidationError
                                                    "
                                                    class="text-danger"
                                                >
                                                    {{ endTimeValidationError }}
                                                </small>
                                            </b-form-group>
                                        </b-col>
                                    </b-row>
                                </b-card-body>
                            </b-card>
                            <b-card>
                                <AccessControl 
                                ref="accessControl" 
                                :check-in="newEmployee.startTime" 
                                :check-out="newEmployee.endTime"
                                />
                            </b-card>
                        </b-col>
                        <!-- End: input Data -->
                    </b-row>

                    <!-- Button Action -->
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageEmployee'])"
                            type="submit"
                            variant="primary"
                            :disabled="isSubmitting"
                            title="Save"
                            class="mx-50 mb-50 btn-120"
                        >
                            <b-spinner v-if="isSubmitting" small type="grow" />
                            <span v-else>{{ $t('common.button.save') }}</span>
                        </b-button>
                        <b-button
                            @click="navigateToList()"
                            type="reset"
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

import CardReader from './CardReader.vue'
import AccessControl from './AccessControl.vue'

export default {
    components: { CardReader, AccessControl },
    mixins: [authorizationMixin],
    data() {
        return {
            isSubmitting: false,
            cameraMatch: false,
            faceMatch: null,
            webcamStream: null,
            isCameraReady: false,
            capturedPhoto: null,
            webcamError: null,
            options: {
                compTree: [],
                groups: [],
                departmentTree: [],
                areaTree: null,
                gender: [
                    { value: 0, label: 'Nữ' },
                    { value: 1, label: 'Nam' },
                ],
                personType: [
                    { value: 2, label: 'Khách' },
                    { value: 3, label: 'Nhà thầu' },
                ],
            },
            newEmployee: {
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
                personTypeStr: null,
                cardId: null,
                receiver: null,
                address: null,
                startTime: null,
                endTime: null,
                notes: null,
                contactor: null,
            },
        }
    },
    computed: {
        endTimeDate: {
            get() {
                if (!this.newEmployee.endTime) return null
                return this.newEmployee.endTime.split(' ')[0] // Lấy YYYY-MM-DD
            },
            set(value) {
                const timePart = this.newEmployee.endTime
                    ? this.newEmployee.endTime.split(' ')[1]
                    : '00:00'
                this.newEmployee.endTime = value ? `${value} ${timePart}` : null
            },
        },
        endTimeTime: {
            get() {
                if (!this.newEmployee.endTime) return null
                return this.newEmployee.endTime.split(' ')[1] // Lấy HH:mm:ss
            },
            set(value) {
                const datePart = this.newEmployee.endTime
                    ? this.newEmployee.endTime.split(' ')[0]
                    : this.$moment().format('YYYY-MM-DD')
                this.newEmployee.endTime = value ? `${datePart} ${value}` : null
            },
        },
        startTimeDate: {
            get() {
                if (!this.newEmployee.startTime) return null
                return this.newEmployee.startTime.split(' ')[0] // Lấy YYYY-MM-DD
            },
            set(value) {
                const timePart = this.newEmployee.startTime
                    ? this.newEmployee.startTime.split(' ')[1]
                    : '00:00'
                this.newEmployee.startTime = value
                    ? `${value} ${timePart}`
                    : null
            },
        },
        startTimeTime: {
            get() {
                if (!this.newEmployee.startTime) return null
                return this.newEmployee.startTime.split(' ')[1] // Lấy HH:mm:ss
            },
            set(value) {
                const datePart = this.newEmployee.startTime
                    ? this.newEmployee.startTime.split(' ')[0]
                    : this.$moment().format('YYYY-MM-DD')
                this.newEmployee.startTime = value
                    ? `${datePart} ${value}`
                    : null
            },
        },
        endTimeValidationError() {
            // Chỉ validate khi cả startTime và endTime đều có giá trị
            if (!this.newEmployee.startTime || !this.newEmployee.endTime) {
                return null
            }

            const startDateTime = this.$moment(
                this.newEmployee.startTime,
                'YYYY-MM-DD HH:mm'
            )
            const endDateTime = this.$moment(
                this.newEmployee.endTime,
                'YYYY-MM-DD HH:mm'
            )

            if (endDateTime.isSameOrBefore(startDateTime)) {
                return this.$t(
                    'categories.employees.common.form.validation.endTimeGreaterThanStartTime'
                )
            }

            return null
        },
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        currentLocale() {
            return this.$i18n ? this.$i18n.locale : 'vi' // Nếu $i18n null, mặc định 'vi'
        },
    },
    async created() {
        await this.loadOptions()
        this.newEmployee.PersonType == 1
        const accessToken = this.$services.getUserData()
        this.newEmployee.receiver = accessToken.fullName
        // Set default start time to current time if not already assigned
        if (!this.newEmployee.startTime) {
            this.newEmployee.startTime =
                this.$moment().format('YYYY-MM-DD HH:mm')
        }
    },

    methods: {
        openWebcamModal() {
            this.capturedPhoto = null // Reset ảnh chụp
            this.webcamError = null
            this.$refs.webcamModal.show()
        },
        async startWebcam() {
            try {
                this.webcamStream = await navigator.mediaDevices.getUserMedia({
                    video: {
                        width: { ideal: 640 },
                        height: { ideal: 480 },
                        facingMode: 'user', // Camera trước
                    },
                })
                this.$refs.webcamVideo.srcObject = this.webcamStream
                this.isCameraReady = true
            } catch (error) {
                console.error('Lỗi truy cập webcam:', error)
                this.webcamError =
                    'Không thể truy cập webcam. Vui lòng kiểm tra quyền truy cập camera.'
                this.isCameraReady = false
            }
        },

        stopWebcam() {
            if (this.webcamStream) {
                this.webcamStream.getTracks().forEach((track) => track.stop())
                this.webcamStream = null
            }
            this.isCameraReady = false
        },

        capturePhoto() {
            const video = this.$refs.webcamVideo
            const canvas = document.createElement('canvas')
            const context = canvas.getContext('2d')

            canvas.width = video.videoWidth
            canvas.height = video.videoHeight
            context.drawImage(video, 0, 0, canvas.width, canvas.height)

            // Chuyển thành base64 (JPEG để nén nhỏ hơn)
            const base64Image = canvas.toDataURL('image/jpeg', 0.8) // Chất lượng 80%
            this.capturedPhoto = base64Image

            // Lưu vào newEmployee.avatarBase64
            this.newEmployee.avatarBase64 = base64Image

            // Đóng modal sau khi chụp (tùy chọn, có thể thêm nút xác nhận)
            this.$nextTick(() => {
                this.$bvModal.hide('webcam-modal')
            })

            // Hiển thị toast thành công (tùy chọn)
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
        takePhotoCam(typeTakePhoto = 1) {
            let requestData = null
            this.websocket = new WebSocket('ws://localhost:9999')

            let messageReceived = false

            // Set timeout to auto-close if no message received
            const timeout = setTimeout(() => {
                if (!messageReceived) {
                    console.log(
                        'No message received in 30s. Closing WebSocket.'
                    )
                    this.websocket.close()
                }
            }, 1000 * 30) // 30 seconds

            this.websocket.onopen = () => {
                console.log('WebSocket connected.')
                if (typeTakePhoto === 1) {
                    requestData = { type: 4 }
                } else {
                    requestData = {
                        type: 7,
                        data: this.newEmployee.avatarBase64.split(',')[1],
                        version: 2,
                    }
                }
                this.websocket.send(JSON.stringify(requestData))
            }

            this.websocket.onmessage = (jsonData) => {
                messageReceived = true
                clearTimeout(timeout)
                const data = JSON.parse(jsonData.data)

                if (typeTakePhoto === 1) {
                    if (data && data.faceImage) {
                        this.newEmployee.avatarBase64 =
                            'data:image/jpeg;base64,' + data.faceImage
                    } else {
                        alert('Vui lòng thử lại!')
                    }
                } else {
                    if (data && data.isSamePerson) {
                        this.faceMatch =
                            'data:image/jpeg;base64,' + data.faceCapture
                        this.cameraMatch = data.isSamePerson
                    } else {
                        this.faceMatch =
                            'data:image/jpeg;base64,' + data.faceCapture
                        this.cameraMatch = false
                    }
                }

                this.websocket.close() // Close connection after receiving
            }

            this.websocket.onerror = (err) => {
                console.error('WebSocket error:', err)
            }

            /*************  ✨ Windsurf Command ⭐  *************/
            /**
             * Called when the WebSocket connection is closed.
             *
             * @listens {close} event
             */
            /*******  6dbb690f-88e8-44d5-8673-226c40824f19  *******/
            this.websocket.onclose = () => {
                console.log('WebSocket connection closed.')
            }
        },
        parseBirth(birth) {
            if (!birth) return null

            // Date object?
            if (birth instanceof Date && !isNaN(birth)) {
                const y = birth.getFullYear()
                const m = String(birth.getMonth() + 1).padStart(2, '0')
                const d = String(birth.getDate()).padStart(2, '0')
                return `${y}-${m}-${d}`
            }

            const s = String(birth).trim()
            const digits = s.replace(/\D/g, '')

            // helper: kiểm tra ngày/tháng hợp lệ
            const validDMY = (d, m, y) => {
                if (!(y >= 1900 && y <= 2100)) return false
                if (!(m >= 1 && m <= 12)) return false
                if (!(d >= 1 && d <= 31)) return false
                // đơn giản: không check số ngày trong từng tháng, nhưng có thể mở rộng nếu cần
                return true
            }

            // Nếu chỉ có 8 chữ số — có thể là DDMMYYYY hoặc YYYYMMDD
            if (digits.length === 8) {
                const dd = parseInt(digits.slice(0, 2), 10)
                const mm = parseInt(digits.slice(2, 4), 10)
                const yyyy_back = parseInt(digits.slice(4, 8), 10) // DDMMYYYY -> year ở cuối

                const yyyy_front = parseInt(digits.slice(0, 4), 10) // YYYYMMDD -> year ở đầu
                const mm_front = parseInt(digits.slice(4, 6), 10)
                const dd_front = parseInt(digits.slice(6, 8), 10)

                // Ưu tiên DDMMYYYY nếu phần year ở cuối hợp lệ và ngày/tháng hợp lệ
                if (validDMY(dd, mm, yyyy_back)) {
                    return `${String(yyyy_back)}-${String(mm).padStart(
                        2,
                        '0'
                    )}-${String(dd).padStart(2, '0')}`
                }

                // Nếu không, thử YYYYMMDD
                if (
                    yyyy_front >= 1900 &&
                    yyyy_front <= 2100 &&
                    mm_front >= 1 &&
                    mm_front <= 12 &&
                    dd_front >= 1 &&
                    dd_front <= 31
                ) {
                    return `${String(yyyy_front)}-${String(mm_front).padStart(
                        2,
                        '0'
                    )}-${String(dd_front).padStart(2, '0')}`
                }
            }

            // Có / - .
            const parts = s.split(/[\/\-.]/).map((x) => x.trim())
            if (parts.length === 3) {
                const [a, b, c] = parts

                // YYYY-MM-DD hoặc YYYY/M/D
                if (a.length === 4 && /^\d{4}$/.test(a)) {
                    const y = parseInt(a, 10)
                    const m = String(parseInt(b, 10) || 0).padStart(2, '0')
                    const d = String(parseInt(c, 10) || 0).padStart(2, '0')
                    if (validDMY(parseInt(d, 10), parseInt(m, 10), y)) {
                        return `${a}-${m}-${d}`
                    }
                }

                // DD-MM-YYYY hoặc MM-DD-YYYY (đuôi YYYY)
                if (c.length === 4 && /^\d{4}$/.test(c)) {
                    const d = parseInt(a, 10)
                    const m = parseInt(b, 10)
                    const y = parseInt(c, 10)

                    // Nếu m>12 chắc chắn a là ngày -> DD-MM-YYYY
                    if (m > 12 && validDMY(d, m, y)) {
                        return `${y}-${String(d).padStart(2, '0')}-${String(
                            m
                        ).padStart(2, '0')}`
                    }

                    // Nếu m<=12 và d<=12 thì mơ hồ -> ưu tiên DD-MM-YYYY (thường dùng ở VN)
                    if (d <= 31 && m <= 12 && validDMY(d, m, y)) {
                        return `${y}-${String(m).padStart(2, '0')}-${String(
                            d
                        ).padStart(2, '0')}`
                    }

                    // Nếu a>31 hoặc m không hợp lệ -> cố gắng hoán đổi nếu hợp lệ
                    if (m <= 31 && d <= 12 && validDMY(m, d, y)) {
                        // tạm coi a là tháng và b là ngày -> sử dụng y-mm-dd
                        return `${y}-${String(d).padStart(2, '0')}-${String(
                            m
                        ).padStart(2, '0')}`
                    }
                }
            }

            console.warn('Unrecognized birth format:', birth)
            return null
        },
        onPersonInfo(data) {
            console.log('Dữ liệu thô từ thiết bị:', data) // Log toàn bộ data
            console.log('Birth thô:', data.birth) // Log riêng birth
            const dataMapping = {
                fullname: 'name',
                birthday: 'birth',
                gender: 'gender',
                avatarBase64: 'base64',
                citizenId: 'id',
                address: 'address',
            }
            Object.keys(dataMapping).forEach((key) => {
                this.newEmployee[key] = data[dataMapping[key]]
                if (!data[dataMapping[key]]) return

                if (key === 'avatarBase64') {
                    this.newEmployee.avatarBase64 = `data:image/png;base64,${data.base64}`
                } else if (key === 'birthday') {
                    this.newEmployee.birthday = this.parseBirth(data.birth)

                    // (Khuyến nghị) chặn giá trị sai kiểu YYYY-MM-DD
                    if (
                        !/^\d{4}-\d{2}-\d{2}$/.test(
                            this.newEmployee.birthday || ''
                        )
                    ) {
                        console.warn(
                            'Invalid normalized birth:',
                            data.birth,
                            this.newEmployee.birthday
                        )
                        this.newEmployee.birthday = null
                    }
                    console.log(
                        'birth raw:',
                        data.birth,
                        '→',
                        this.newEmployee.birthday
                    )
                } else {
                    this.newEmployee[key] = data[dataMapping[key]]
                }
            })
            console.log('Birth sau parse:', this.newEmployee.birthday)
        },

        handleFileUpload(event) {
            const file = event.target.files[0]
            if (file) {
                const reader = new FileReader()
                reader.onload = (e) => {
                    this.newEmployee.avatarBase64 = e.target.result // data:*;base64,....
                }
                reader.onerror = () => {
                    this.errors.push('Không thể đọc file, vui lòng thử lại.')
                }
                reader.readAsDataURL(file)
            } else {
                this.newEmployee.avatarBase64 = ''
            }
        },
        async loadOptions() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.options.compTree = TreeHelper.removeEmptyChildren(
                    response.data
                )
            })
            this.$services.get('/lookup/groupsNotCompId').then((response) => {
                const userCompanyId = this.$services.getUserData().companyId
                this.options.groups = response.data.data.filter(x => x.compId == userCompanyId)
            })
            this.$services
                .get('/lookup/departments-tree?type=2')
                .then((response) => {
                    this.options.departmentTree = response.data.data
                })
            const response = await this.$services.get('/lookup/areas-tree')
            this.options.areaTree = TreeHelper.removeEmptyChildren(
                response.data.data
            )
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        generateRandomCode(length = 12) {
            const chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"
            const bytes = new Uint8Array(length)
            crypto.getRandomValues(bytes)
            return Array.from(bytes, (b) => chars[b % chars.length]).join("")
        },
        async onSubmit(e) {
            e.preventDefault()

            // Kiểm tra validation thời gian trước
            if (this.endTimeValidationError) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: 'Lỗi',
                        text: this.endTimeValidationError,
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                    },
                })
                return
            }

            // Kiểm tra thời gian phân quyền truy cập
            if (this.$refs.accessControl && !this.$refs.accessControl.validateAccessTimes()) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: 'Lỗi',
                        text: this.$t('categories.employees.error.accessTimeOutOfRange') || 'Thời gian phân quyền truy cập không nằm trong khoảng thời gian thăm!',
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                    },
                })
                return
            }

            // Validate form
            const success = await this.$refs.rules.validate()
            if (!success) {
                return // Không cần toast, VeeValidate đã handle error
            }

            this.isSubmitting = true // Bắt đầu loading

            try {
                // Set startTime nếu chưa có
                // this.newEmployee.startTime = this.$moment().format('YYYY-MM-DD HH:mm');

                console.time('submit') // Debug thời gian (tùy chọn)

                // API 1: Tạo employee
                this.newEmployee.compGuest = this.trimField(this.newEmployee.compGuest)
                this.newEmployee.fullname = this.trimField(this.newEmployee.fullname)
                this.newEmployee.position = this.trimField(this.newEmployee.position)
                let res = await this.$services.post('/employees', {
                    ...this.newEmployee,
                    cardFront: this.$refs.CardReader.person.frontImgBase64,
                    cardBack: this.$refs.CardReader.person.backImgBase64,
                    faceMatch: this.faceMatch,
                    code: this.generateRandomCode(10),
                    notes: this.newEmployee.notes?.trim() || '',
                    address: this.newEmployee.address?.trim() || '',
                    cardId: this.newEmployee.cardId?.trim() || '',
                    receiver: this.newEmployee.receiver?.trim() || '',
                    contactor: this.newEmployee.contactor?.trim() || '',
                })

                const employeeId = res.data.data

                // API 2: Tạo access control (nếu có)
                const accessControl = this.$refs.accessControl
                    .getAccessControl()
                    .filter((item) => item.areaId)
                    .map((item) => ({
                        ...item,
                        employeeId,
                    }))

                if (accessControl.length > 0) {
                    await this.$services.post(
                        '/accessControllGuess',
                        accessControl
                    )
                }

                console.timeEnd('submit') // Kết thúc debug

                // Show toast success NGAY SAU KHI DONE
                this.showSuccessToast(res.data)

                // Navigate sau toast (delay nhỏ nếu cần, nhưng mặc định nhanh)
                setTimeout(() => {
                    // Tùy chọn: Delay 500ms để user thấy toast
                    this.navigateToList()
                }, 500)
            } catch (error) {
                this.showErrorToast(error)
            } finally {
                this.isSubmitting = false // Dừng loading
            }
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t('categories.guess.error.C_EMPLOYEES_200'),
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
        showErrorToast(error) {
            console.log('error', error)
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t('Error.Error'),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(`${error.message}`),
                },
            })
        },
        navigateToList() {
            this.$refs.CardReader?.clearStorage()
            this.$router.push({ path: '/categories/guess/list' })
        },
    },
}
</script>

<style lang="scss" scoped>
/* Card chung */
.b-card {
    border-radius: 0.75rem;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08) !important;
    overflow: hidden;
}

/* Header của card */
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
    padding: 0.75rem 0rem;
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

// .shadow-sm {
//   box-shadow: 1px 0px 1px 1px rgba(197, 56, 32, 0.096) !important;
// }

.required::after {
    content: ' *';
    color: red;
}
</style>
