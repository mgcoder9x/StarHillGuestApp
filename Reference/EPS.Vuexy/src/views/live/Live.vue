<template>
    <validation-observer ref="rules">
        <div>
            <b-row>
                <transition name="slide">
                    <b-col :md="isSidebarContent ? 9 : 12">
                        <b-card no-body>
                            <b-card-header
                                class="d-flex align-items-center justify-content-between flex-wrap"
                                style="padding: 10px"
                            >
                                <!-- Stream config -->
                                <b-col md="4">
                                    <b-form-group
                                        :label="
                                            $t('FormGroupName.StreamConfig')
                                        "
                                        label-for="create-area-code"
                                        label-cols-md="5"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <div class="d-flex align-items-center">
                                            <v-select
                                                v-model="streamConfigId"
                                                :dir="
                                                    $store.state.appConfig.isRTL
                                                        ? 'rtl'
                                                        : 'ltr'
                                                "
                                                :options="
                                                    rechangeOptions(
                                                        listStreamConfig
                                                    )
                                                "
                                                label="label"
                                                :reduce="
                                                    (streamConfig) =>
                                                        streamConfig.id
                                                "
                                                class="flex-grow-1 mr-2"
                                                @input="getStreamConfig"
                                            />
                                        </div>
                                    </b-form-group>
                                </b-col>
                                <div class="d-flex align-items-center ml-auto">
                                    <b-button
                                        v-if="!streamConfigId"
                                        variant="primary"
                                        size="sm"
                                        class="mr-1"
                                        :title="$t('Title.Add')"
                                        @click="modalAddStreamConfig = true"
                                    >
                                        <Icon
                                            icon="material-symbols:add"
                                            style="
                                                font-size: 16px;
                                                color: #e2e8f0;
                                            "
                                        />
                                    </b-button>
                                    <b-button
                                        v-if="streamConfigId"
                                        variant="primary"
                                        size="sm"
                                        class="mr-1"
                                        :title="$t('Title.Update')"
                                        @click="modalStreamConfig = true"
                                    >
                                        <Icon
                                            icon="material-symbols:save-outline"
                                            style="font-size: 16px"
                                        />
                                    </b-button>
                                    <b-button
                                        v-if="streamConfigId"
                                        variant="primary"
                                        size="sm"
                                        class="mr-1"
                                        :title="$t('Title.Delete')"
                                        @click="doDelete()"
                                    >
                                        <Icon
                                            icon="material-symbols:delete"
                                            style="font-size: 16px"
                                        />
                                    </b-button>
                                    <b-button
                                        size="sm"
                                        class="mr-1"
                                        :title="$t('Title.ViewEvent')"
                                        @click="
                                            isSidebarContent = !isSidebarContent
                                        "
                                    >
                                        <Icon
                                            icon="material-symbols:visibility"
                                            style="
                                                font-size: 16px;
                                                color: #e2e8f0;
                                            "
                                        />
                                    </b-button>
                                    <b-button
                                        size="sm"
                                        class="mr-1"
                                        :title="$t('Title.ViewCamera')"
                                        @click="
                                            isSidebarActive = !isSidebarActive
                                        "
                                    >
                                        <Icon
                                            icon="material-symbols:camera"
                                            style="
                                                font-size: 16px;
                                                color: #e2e8f0;
                                            "
                                        />
                                    </b-button>
                                    <!-- <b-col md="2"> -->
                                    <b-dropdown
                                        variant="link"
                                        no-caret
                                        class="chart-dropdown"
                                        toggle-class="p-0"
                                        right
                                    >
                                        <template #button-content>
                                            <Icon
                                                icon="mingcute:more-2-line"
                                                class="md-icon text-body cursor-pointer"
                                            />
                                        </template>
                                        <b-dropdown-item
                                            href="#"
                                            @click="changeMonitor(1)"
                                        >
                                            <Icon
                                                icon="material-symbols:screenshot-monitor-outline-rounded"
                                                class="md-icon"
                                            />
                                            <span
                                                class="align-text-bottom line-height-1"
                                                >1</span
                                            >
                                        </b-dropdown-item>
                                        <b-dropdown-item
                                            href="#"
                                            @click="changeMonitor(4)"
                                        >
                                            <Icon
                                                icon="material-symbols:screenshot-monitor-outline-rounded"
                                                class="md-icon"
                                            />
                                            <span
                                                class="align-text-bottom line-height-1"
                                                >4</span
                                            >
                                        </b-dropdown-item>
                                        <b-dropdown-item
                                            href="#"
                                            @click="changeMonitor(5)"
                                        >
                                            <Icon
                                                icon="material-symbols:screenshot-monitor-outline-rounded"
                                                class="md-icon"
                                            />
                                            <span
                                                class="align-text-bottom line-height-1"
                                                >5</span
                                            >
                                        </b-dropdown-item>
                                        <b-dropdown-item
                                            href="#"
                                            @click="changeMonitor(6)"
                                        >
                                            <Icon
                                                icon="material-symbols:screenshot-monitor-outline-rounded"
                                                class="md-icon"
                                            />
                                            <span
                                                class="align-text-bottom line-height-1"
                                                >6</span
                                            >
                                        </b-dropdown-item>
                                        <b-dropdown-item
                                            href="#"
                                            @click="changeMonitor(9)"
                                        >
                                            <Icon
                                                icon="material-symbols:screenshot-monitor-outline-rounded"
                                                class="md-icon"
                                            />
                                            <span
                                                class="align-text-bottom line-height-1"
                                                >9</span
                                            >
                                        </b-dropdown-item>
                                    </b-dropdown>
                                </div>
                            </b-card-header>
                            <b-card-body>
                                <!-- context menu-->
                                <vue-context ref="menu" v-slot="{ data }">
                                    <li>
                                        <b-link
                                            class="d-flex align-items-center"
                                            @click="
                                                onClickMenu(
                                                    $event.target.innerText,
                                                    data,
                                                    'SCR'
                                                )
                                            "
                                        >
                                            <feather-icon
                                                icon="MousePointerIcon"
                                                size="16"
                                            />
                                            <span class="ml-75">{{
                                                $t('Live.SelectCam')
                                            }}</span>
                                        </b-link>
                                        <!-- <a href="#" @click.prevent="onClickMenu($event.target.innerText, data, 'SCR')">Chọn camera</a> -->
                                    </li>
                                    <li
                                        v-if="authorize(['IdentificationZone'])"
                                    >
                                        <b-link
                                            class="d-flex align-items-center"
                                            @click="
                                                onClickMenu(
                                                    $event.target.innerText,
                                                    data,
                                                    'DRA'
                                                )
                                            "
                                        >
                                            <feather-icon
                                                icon="SquareIcon"
                                                size="16"
                                            />
                                            <span class="ml-75">{{
                                                $t('Live.RecognitionArea')
                                            }}</span>
                                        </b-link>
                                        <!-- <a href="#" @click.prevent="onClickMenu($event.target.innerText, data, 'DRA')">Vẽ vùng nhận
                            diện</a> -->
                                    </li>
                                </vue-context>
                                <!-- context menu-->
                                <b-col v-if="monitor === 1">
                                    <b-card-group class="mb-0">
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 1)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player1"
                                                :video-index="1"
                                                :source="videoSource1"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_1
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(1)
                                                "
                                            />
                                        </b-card>
                                    </b-card-group>
                                </b-col>
                                <b-col v-if="monitor === 4">
                                    <b-card-group class="mb-0">
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 1)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player1"
                                                :video-index="1"
                                                :source="videoSource1"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_1
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(1)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 2)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player2"
                                                :video-index="2"
                                                :source="videoSource2"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_2
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(2)
                                                "
                                            />
                                        </b-card>
                                    </b-card-group>
                                    <b-card-group class="mb-0">
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 3)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player3"
                                                :video-index="3"
                                                :source="videoSource3"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_3
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(3)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 4)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player4"
                                                :video-index="4"
                                                :source="videoSource4"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_4
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(4)
                                                "
                                            />
                                        </b-card>
                                    </b-card-group>
                                </b-col>
                                <b-col v-if="monitor === 5">
                                    <b-card-group class="mb-0">
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 1)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player1"
                                                :video-index="1"
                                                :source="videoSource1"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_1
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(1)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 2)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player2"
                                                :video-index="2"
                                                :source="videoSource2"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_2
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(2)
                                                "
                                            />
                                        </b-card>
                                    </b-card-group>
                                    <b-card-group class="mb-0">
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 3)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player3"
                                                :video-index="3"
                                                :source="videoSource3"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_3
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(3)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 4)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player4"
                                                :video-index="4"
                                                :source="videoSource4"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_4
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(4)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 5)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player5"
                                                :video-index="5"
                                                :source="videoSource5"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_5
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(5)
                                                "
                                            />
                                        </b-card>
                                    </b-card-group>
                                </b-col>
                                <b-col v-if="monitor === 6">
                                    <b-card-group class="mb-0">
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 1)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player1"
                                                :video-index="1"
                                                :source="videoSource1"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_1
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(1)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 2)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player2"
                                                :video-index="2"
                                                :source="videoSource2"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_2
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(2)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 3)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player3"
                                                :video-index="3"
                                                :source="videoSource3"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_3
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(3)
                                                "
                                            />
                                        </b-card>
                                    </b-card-group>
                                    <b-card-group class="mb-0">
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 4)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player4"
                                                :video-index="4"
                                                :source="videoSource4"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_4
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(4)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 5)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player5"
                                                :video-index="5"
                                                :source="videoSource5"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_5
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(5)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 6)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player6"
                                                :video-index="6"
                                                :source="videoSource6"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_6
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(6)
                                                "
                                            />
                                        </b-card>
                                    </b-card-group>
                                </b-col>
                                <b-col v-if="monitor === 9">
                                    <b-card-group class="mb-0">
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 1)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player1"
                                                :video-index="1"
                                                :source="videoSource1"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_1
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(1)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 2)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player2"
                                                :video-index="2"
                                                :source="videoSource2"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_2
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(2)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 3)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player3"
                                                :video-index="3"
                                                :source="videoSource3"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_3
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(3)
                                                "
                                            />
                                        </b-card>
                                    </b-card-group>
                                    <b-card-group class="mb-0">
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 4)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player4"
                                                :video-index="4"
                                                :source="videoSource4"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_4
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(4)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 5)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player5"
                                                :video-index="5"
                                                :source="videoSource5"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_5
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(5)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 6)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player6"
                                                :video-index="6"
                                                :source="videoSource6"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_6
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(6)
                                                "
                                            />
                                        </b-card>
                                    </b-card-group>
                                    <b-card-group class="mb-0">
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 7)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player7"
                                                :video-index="7"
                                                :source="videoSource7"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_7
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(7)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 8)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player8"
                                                :video-index="8"
                                                :source="videoSource8"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_8
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(8)
                                                "
                                            />
                                        </b-card>
                                        <b-card
                                            img-top
                                            no-body
                                            @contextmenu.prevent="
                                                $refs.menu.open($event, 9)
                                            "
                                        >
                                            <WebRTCPlayer
                                                ref="player9"
                                                :video-index="9"
                                                :source="videoSource9"
                                                custom-class="card-img-top"
                                                :wrapper-class="[
                                                    isSelected_9
                                                        ? 'player-box-selected'
                                                        : 'player-box',
                                                ]"
                                                @base64-img="base64Img"
                                                @wrapper-click="
                                                    canvasSelected(9)
                                                "
                                            />
                                        </b-card>
                                    </b-card-group>
                                </b-col>
                            </b-card-body>
                        </b-card>
                    </b-col>
                </transition>
                <transition name="slide">
                    <b-col
                        v-if="isSidebarContent"
                        :md="isSidebarContent ? 3 : 0"
                    >
                        <b-card
                            v-for="item in filteredEventList.slice(0, 6)"
                            id="eventItem"
                            :key="item.eventId"
                            no-body
                            :class="{ 'flash-item': item.isNew }"
                            style="padding: 1rem; margin-bottom: 1rem"
                        >
                            <div v-if="item.eventTypeId == 800">
                                <CarEvent
                                    :base-u-r-l="baseURL"
                                    :event-data="item"
                                />
                            </div>
                            <div v-else>
                                <AllEvent
                                    :base-u-r-l="baseURL"
                                    :event-data="item"
                                />
                            </div>
                        </b-card>
                    </b-col>
                </transition>
            </b-row>

            <!-- Vẽ polygon -->
            <b-modal
                v-if="modalDraw"
                v-model="modalDraw"
                :title="$t('Live.Draw')"
                ok-title="Đóng"
                hide-header-close
                ok-only
                size="lg"
            >
                <div>
                    <v-stage ref="stage" :config="configKonva">
                        <v-layer ref="drawLayer" @click="layerClick">
                            <v-image :config="{ image: image }" />

                            <template v-if="isDrawing">
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
                            </template>
                        </v-layer>
                    </v-stage>
                </div>
                <template #modal-footer>
                    <div class="d-flex justify-content-between w-100">
                        <!-- Bên trái -->
                        <div>
                            <b-button
                                v-if="isDrawing"
                                variant="danger"
                                class="mr-1"
                                @click="
                                    polygons.splice(activeIdx, 1)
                                    activeIdx = null
                                "
                            >
                                {{ $t('Button.Delete') }}
                            </b-button>
                            <b-button
                                v-if="isDrawing"
                                variant="primary"
                                class="mr-1"
                                @click="
                                    activeIdx = null
                                    polygons.splice(0, polygons.length)
                                "
                            >
                                {{ $t('Button.Refresh') }}
                            </b-button>
                        </div>

                        <!-- Bên phải -->
                        <div>
                            <b-button
                                v-if="!isDrawing"
                                variant="primary"
                                class="mr-1"
                                @click="isDrawing = true"
                            >
                                {{ $t('Button.Edit') }}
                            </b-button>
                            <b-button
                                v-if="isDrawing"
                                variant="primary"
                                class="mr-1"
                                @click="savePolygons"
                            >
                                {{ $t('Button.Save') }}
                            </b-button>
                            <b-button @click="cancel">{{
                                $t('Button.Exit')
                            }}</b-button>
                        </div>
                    </div>
                </template>
            </b-modal>

            <!-- Danh sách camera -->
            <b-sidebar
                id="sidebar-add-new-event"
                v-model="isSidebarActive"
                sidebar-class="sidebar-xl"
                :visible="isSidebarActive"
                bg-variant="white"
                shadow
                backdrop
                no-header
                right
            >
                <template>
                    <!-- Header -->
                    <div
                        class="d-flex justify-content-between align-items-center content-sidebar-header px-2 py-1"
                    >
                        <h5 class="mb-0">
                            {{ $t('Events.MapDashboard.ListCamera') }}
                        </h5>
                        <div>
                            <feather-icon
                                class="ml-1 cursor-pointer"
                                icon="XIcon"
                                size="16"
                                @click="hideSidebar"
                            />
                        </div>
                    </div>
                    <b-list-group class="list-group-filters">
                        <!-- Lặp qua từng khu vực -->
                        <b-list-group-item
                            v-for="area in groupedCams"
                            :key="area.id"
                            class="cursor-pointer p-0"
                            @click="toggleArea(area.id)"
                        >
                            <!-- Tiêu đề khu vực (có thể bấm để mở rộng/thu gọn) -->
                            <div style="margin-block: 5px; padding: 5px">
                                <feather-icon
                                    :icon="
                                        expandedAreas.includes(area.id)
                                            ? 'ChevronDownIcon'
                                            : 'ChevronRightIcon'
                                    "
                                    size="18"
                                    class="mr-75"
                                />
                                <span class="align-text-bottom line-height-1">{{
                                    area.name
                                }}</span>
                            </div>
                            <!-- Danh sách camera trong khu vực, chỉ hiển thị khi khu vực được mở -->
                            <b-collapse
                                :visible="expandedAreas.includes(area.id)"
                            >
                                <b-list-group>
                                    <b-list-group-item
                                        v-for="cam in area.cameras"
                                        :key="cam.id + cam.code"
                                        class="cursor-pointer pl-4"
                                        :disabled="
                                            selectedCameras.includes(cam.id)
                                        "
                                        style="
                                            border-left: none;
                                            border-radius: 0;
                                        "
                                        @click="selectCam(cam)"
                                    >
                                        <feather-icon
                                            v-if="
                                                selectedCameras.includes(cam.id)
                                            "
                                            icon="CheckIcon"
                                            size="18"
                                            class="mr-75"
                                        />
                                        <feather-icon
                                            icon="VideoIcon"
                                            size="18"
                                            class="mr-75"
                                        />
                                        <span
                                            class="align-text-bottom line-height-1"
                                            :style="
                                                cam.status == 0
                                                    ? 'color:red'
                                                    : selectedCameras.includes(
                                                            cam.id
                                                        )
                                                      ? 'color:green'
                                                      : 'color:blue'
                                            "
                                            >{{ cam.name }}</span
                                        >
                                    </b-list-group-item>
                                </b-list-group>
                            </b-collapse>
                        </b-list-group-item>
                    </b-list-group>
                </template>
            </b-sidebar>

            <!-- Lưu cấu hình -->
            <b-modal
                v-model="modalStreamConfig"
                :title="$t('StreamConfig.Save')"
                ok-title="Đóng"
                hide-header-close
                ok-only
                size="lg"
            >
                <div>
                    <b-form-group
                        :label="$t('StreamConfig.Name')"
                        label-for="h-config-name"
                        label-cols-md="4"
                        label-class="required"
                        class="mb-50 mb-md-1"
                    >
                        <validation-provider
                            #default="{ errors }"
                            rules="required"
                            :name="$t('StreamConfig.Name')"
                        >
                            <b-form-input
                                id="h-area-code"
                                v-model="streamConfig.configName"
                                :placeholder="$t('StreamConfig.Name')"
                            />
                            <small class="text-danger">{{ errors[0] }}</small>
                        </validation-provider>
                    </b-form-group>
                </div>
                <template #modal-footer>
                    <div class="w-100" style="text-align: right">
                        <b-button
                            variant="primary"
                            class="mr-1"
                            @click="saveConfig(true)"
                        >
                            {{ $t('Button.Save') }}
                        </b-button>
                        <b-button @click="modalStreamConfig = false">
                            {{ $t('Button.Exit') }}
                        </b-button>
                    </div>
                </template>
            </b-modal>

            <!-- Thêm mới cấu hình -->
            <b-modal
                v-model="modalAddStreamConfig"
                :title="$t('StreamConfig.Create')"
                ok-title="Đóng"
                hide-header-close
                ok-only
                size="lg"
            >
                <div>
                    <b-form-group
                        :label="$t('StreamConfig.Name')"
                        label-for="h-config-name"
                        label-cols-md="4"
                        label-class="required"
                        class="mb-50 mb-md-1"
                    >
                        <validation-provider
                            #default="{ errors }"
                            rules="required"
                            :name="$t('StreamConfig.Name')"
                        >
                            <b-form-input
                                id="h-area-code"
                                v-model="streamConfig.configName"
                                :placeholder="$t('StreamConfig.Name')"
                            />
                            <small class="text-danger">{{ errors[0] }}</small>
                        </validation-provider>
                    </b-form-group>
                </div>
                <template #modal-footer>
                    <div class="w-100" style="text-align: right">
                        <b-button
                            variant="primary"
                            class="mr-1"
                            @click="saveConfig(false)"
                        >
                            {{ $t('Button.Save') }}
                        </b-button>
                        <b-button @click="modalAddStreamConfig = false">
                            {{ $t('Button.Exit') }}
                        </b-button>
                    </div>
                </template>
            </b-modal>
        </div>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import SelectCameraSidebar from './SelectCameraSidebar.vue'
import VueContext from 'vue-context'
import { authorizationMixin } from '@core/mixins/ui/forms'
import helper from '@/utils/utils.js'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'
import PolygonEditor from './PolygonEditor.vue'
import signalRService from '@/utils/signalr-service'
import moment from 'moment'
import CarEvent from '@/components/Live/CarEvent'
import AllEvent from '@/components/Live/AllEvent'
import WebRTCPlayer from '@/components/WebRTCPlayer.vue'
import { v4 as uuidv4 } from 'uuid'
export default {
    mixins: [authorizationMixin],
    components: {
        SelectCameraSidebar,
        VueContext,
        PolygonEditor,
        CarEvent,
        AllEvent,
        WebRTCPlayer,
    },
    data() {
        return {
            image: null,
            listStreamConfig: [],
            configKonva: {
                width: 960,
                height: 540,
            },

            configCircle: {
                x: 100,
                y: 100,
                radius: 70,
                fill: 'red',
                stroke: 'black',
                strokeWidth: 4,
            },
            arrpoints: [],
            arrcicle: [],

            modalDraw: false, //Show modal vẽ vùng nhận diện
            modalStreamConfig: false, //Show modal lưu cấu hình
            modalAddStreamConfig: false, //Show modal lưu cấu hình

            monitor: 4,

            //selected cam
            isSelected_1: false,
            isSelected_2: false,
            isSelected_3: false,
            isSelected_4: false,
            isSelected_5: false,
            isSelected_6: false,
            isSelected_7: false,
            isSelected_8: false,
            isSelected_9: false,

            //Soure cam
            videoSource1: null,
            videoSource2: null,
            videoSource3: null,
            videoSource4: null,
            videoSource5: null,
            videoSource6: null,
            videoSource7: null,
            videoSource8: null,
            videoSource9: null,

            //Giá trị show sidebar
            isSidebarActive: false,
            isSidebarContent: false,

            //Giá trị vị trí cam đang chọn
            camIndexSelecting: 0,

            listCams: [],

            polygon: null,
            isPolygonAdded: false, //Giá trị đã add polygon
            camIdSelecting: 0, //Giá trị id cam đang select
            polygonId: 0,
            p_width: 0, //Giá trị chiều rộng màn được chọn
            p_height: 0, //Giá trị chiều cao màn được chọn
            drawWidth: 960,
            drawHeight: 540,
            isDrawing: false,
            streamConfig: {
                userId: null,
                configName: null,
                monitorCount: null,
                streamDetail: [
                    {
                        id: null,
                        deviceId: null,
                    },
                ],
            },
            streamConfigId: null,
            expandedAreas: [],
            selectedCameras: [],
            devicePermissions: [],
            filteredEventList: [],
            polygons: [
                { points: [100, 100, 200, 100, 200, 200, 100, 200] }, // polygon demo
            ],
            activeIdx: null,
            compId: null,

            isInPolygon: false, // Kiểm tra trạng thái chuột có đang trỏ vào polygon nào k
            polygonMetaData: [],

            // Map camera_code → playerIndex (để lookup khi nhận bản tin)
            cameraCodeMap: {},

            // Lưu thông tin camera cho mỗi player slot (persist qua layout change)
            playerSlots: Array.from({ length: 9 }, () => ({
                camId: 0,
                cameraName: '',
                cameraCode: '',
            })),
        }
    },
    setup() {
        // App Name
        const nodeMediaServer = process.env.VUE_APP_NODE_MEDIA_SERVER
        const mediaMTX = process.env.VUE_APP_MEDIAMTX

        return {
            nodeMediaServer,
            mediaMTX,
        }
    },
    computed: {
        baseURL() {
            return process.env.VUE_APP_BASE_URL
            //return "https://demo.atin.vn/Service"
        },
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        groupedCams() {
            const areasMap = {}
            const groupedArray = helper.overlapArray(
                (x, y) => x.id === y.deviceId && y.isChecked
            )(this.listCams, this.devicePermissions)
            groupedArray.forEach((cam) => {
                const areaId = cam.areaId
                if (!areasMap[areaId]) {
                    areasMap[areaId] = {
                        id: areaId,
                        name: cam.areaName,
                        cameras: [],
                    }
                }
                areasMap[areaId].cameras.push(cam)
            })

            return Object.values(areasMap)
        },
    },
    watch: {
        async streamConfigId(newVal, oldVal) {
            // reset toàn bộ player & source
            for (let i = 1; i <= 9; i++) {
                this[`videoSource${i}`] = null
                if (this.$refs[`player${i}`]) {
                    this.$refs[`player${i}`].stop()
                    this.$refs[`player${i}`].camId = null
                    this.$refs[`player${i}`].fps = 0
                    this.$refs[`player${i}`].cameraName = null
                    this.$refs[`player${i}`].cameraCode = ''
                    // Clear AI overlays
                    this.$refs[`player${i}`].clearDetections?.()
                    this.$refs[`player${i}`].clearZones?.()
                }
                this[`isSelected_${i}`] = false
            }
            // Reset camera code map
            this.cameraCodeMap = {}
            // Reset player slots
            this.playerSlots = Array.from({ length: 9 }, () => ({
                camId: 0,
                cameraName: '',
                cameraCode: '',
            }))

            // dọn DOM cũ xong đã
            await this.$nextTick()

            if (newVal) {
                // CHỈ lưu storage khi có id hợp lệ
                setStorage('streamConfigId', newVal, 120)

                let cfg = this.listStreamConfig.find((x) => x.id == newVal)
                if (!cfg) return

                // chuẩn hoá dữ liệu streamConfig
                this.streamConfig = {
                    id: cfg.id,
                    userId: cfg.userId,
                    monitorCount: Number(cfg.monitorCount),
                    configName: cfg.text,
                    streamDetail: cfg.streamDetail || [],
                }

                // set monitor và CHỜ render xong layout 1/4/5/6/9
                this.monitor = Number(this.streamConfig.monitorCount || 1)
                await this.$nextTick()

                // lúc này refs 5..9 đã có → load ok
                await this.loadStreaming()
            } else {
                // KHÔNG ghi đè storage khi newVal = null (tránh mất id đã lưu)
                this.streamConfig = {
                    id: null,
                    userId: null,
                    configName: null,
                    monitorCount: null,
                    streamDetail: [],
                }
                this.selectedCameras = []
                this.camIndexSelecting = 0
                await this.$nextTick()
                this.loadStreaming()
            }
        },
    },
    async created() {
        //Ẩn thanh menu
        // this.$store.commit('verticalMenu/UPDATE_VERTICAL_MENU_COLLAPSED', true)
        await this.loadDevicePermissions()
        await this.loadCams()
        await this.loadStreamConfig()

        const accessToken = this.$services.getUserData()
        this.compId = accessToken.companyId

        const savedId = getStorage('streamConfigId')
        // chỉ gán nếu id còn tồn tại trong list
        this.streamConfigId = this.listStreamConfig?.some(
            (x) => x.id == savedId
        )
            ? savedId
            : null

        await signalRService.connect('notificationHub')
        signalRService.on('NewEvent', (data) => {
            if (data) {
                const dataJson = JSON.parse(data)
                this.pushDataEvent(dataJson)
            }
        })

        // Nhận bản tin bbox detection từ AI
        signalRService.on('BboxDetection', (data) => {
            if (data) {
                const dataJson =
                    typeof data === 'string' ? JSON.parse(data) : data
                this.onBboxReceived(dataJson)
            }
        })
    },
    methods: {
        pushDataEvent(event) {
            var vm = this
            // var listCamId = this.streamConfig.streamDetail.map(
            //     (x) => x.deviceId
            // )
            // if (listCamId.includes(event.DeviceId)) {
            if (this.selectedCameras.includes(event.DeviceId)) {
                var object = {}
                if (event.EventTypeId == 800) {
                    object = {
                        image: event.Image,
                        eventTypeId: event.EventTypeId,
                        eventId: event.EventId,
                        areaName: event.AreaName,
                        accessTime: moment(event.AccessTime).format(
                            'HH:mm - DD/MM/YYYY'
                        ),
                        licensePlate: event.ContEvent.LicensePlate,
                        warningLevelId: event.WarningLevelId,
                        warningColor: event.WarningColor,
                        isNew: true,
                    }
                } else {
                    object = {
                        image: event.Image,
                        eventTypeId: event.EventTypeId,
                        eventId: event.EventId,
                        areaName: event.AreaName,
                        accessTime: moment(event.AccessTime).format(
                            'HH:mm - DD/MM/YYYY'
                        ),
                        isNew: true,
                    }
                }
                this.filteredEventList.unshift(object)
                setTimeout(() => {
                    if (this.filteredEventList.length > 0) {
                        var a = this.filteredEventList.find(
                            (x) => x.eventId == event.EventId
                        )
                        a.isNew = false
                    }
                }, 3000)
            }
        },
        /**
         * Xử lý bản tin bbox detection từ AI
         * Format: { camera_code, timestamp, detections: [{ id, class, confidence, bbox: [x1,y1,x2,y2], color, label }] }
         */
        onBboxReceived(data) {
            console.log(
                '[BBOX] Received data:',
                JSON.stringify(data).substring(0, 200)
            )

            const cameraCode = data.camera_code
            let playerIdx = this.cameraCodeMap[cameraCode]

            // Fallback: nếu chưa có trong map, tìm player nào có cameraCode khớp
            if (!playerIdx) {
                for (let i = 1; i <= 9; i++) {
                    const ref = this.$refs[`player${i}`]
                    if (ref && ref.cameraCode === cameraCode) {
                        playerIdx = i
                        this.$set(this.cameraCodeMap, cameraCode, i)
                        console.log(`[BBOX] Map ${cameraCode} → player${i}`)
                        break
                    }
                }
            }
            // Fallback 2: tìm qua listCams code → player camId
            if (!playerIdx) {
                const cam = this.listCams.find((c) => c.code === cameraCode)
                if (cam) {
                    for (let i = 1; i <= 9; i++) {
                        const ref = this.$refs[`player${i}`]
                        if (ref && ref.camId === cam.id) {
                            playerIdx = i
                            this.$set(this.cameraCodeMap, cameraCode, i)
                            console.log(
                                `[BBOX] Map ${cameraCode} → player${i} (via camId)`
                            )
                            break
                        }
                    }
                }
            }
            if (!playerIdx) {
                console.warn(
                    `[BBOX] Không tìm thấy player cho camera ${cameraCode}`
                )
                return
            }

            const playerRef = this.$refs[`player${playerIdx}`]
            if (!playerRef) {
                console.warn(
                    `[BBOX] Player ref không tồn tại: player${playerIdx}`
                )
                return
            }

            console.log(
                `[BBOX] Sending ${data.detections?.length || 0} detections to player${playerIdx}`
            )
            // Gửi detections vào player để vẽ overlay
            playerRef.setDetections(data.detections, 3000)
        },

        /**
         * Xử lý bản tin zone event từ AI
         * Format: { ai_modules, camera_code, event_time, entity_type, entity_id, payload: { zone_id, zone_name, direction } }
         */
        /**
         * Load polygon từ API và vẽ persistent zone overlay lên player.
         * Gọi khi chọn camera.
         * @param {number} camId - ID device/camera
         * @param {number} playerIdx - Index player (1-9)
         */
        loadPolygonForCamera(camId, playerIdx) {
            const playerRef = this.$refs[`player${playerIdx}`]
            if (!playerRef) return

            // Clear zone cũ trước khi load mới
            playerRef.clearPersistentZones()

            this.$services
                .get(`/device/polygon/${camId}`)
                .then((res) => {
                    if (!res.data?.data) {
                        console.log(
                            `[ZONE] Không có polygon cho camera ${camId}`
                        )
                        return
                    }

                    const polygonData = res.data.data
                    let zones = []

                    // Ưu tiên 1: parse polygonAI (đã có tọa độ normalized 0-1)
                    if (polygonData.polygonAI) {
                        try {
                            const aiData =
                                typeof polygonData.polygonAI === 'string'
                                    ? JSON.parse(polygonData.polygonAI)
                                    : polygonData.polygonAI
                            const jsonPolygons = aiData.jsonPointPolygon || []

                            zones = jsonPolygons.map((poly, idx) => ({
                                zoneId: `zone_${idx}`,
                                zoneName: `Zone ${idx + 1}`,
                                pointPolygon: poly.pointPolygon || [],
                            }))
                            console.log(
                                `[ZONE] Parsed ${zones.length} zones from polygonAI for cam ${camId}`
                            )
                        } catch (e) {
                            console.error('[ZONE] Error parsing polygonAI:', e)
                        }
                    }

                    // Ưu tiên 2: parse decimalPointArrays nếu có
                    if (zones.length === 0 && polygonData.decimalPointArrays) {
                        try {
                            const savedPolygons = JSON.parse(
                                polygonData.decimalPointArrays
                            )
                            const drawW = polygonData.drawWidth || 960
                            const drawH = polygonData.drawHeight || 540

                            zones = savedPolygons.map((poly, idx) => ({
                                zoneId: `zone_${idx}`,
                                zoneName: `Zone ${idx + 1}`,
                                points: (poly.points || []).map((val, i) =>
                                    i % 2 === 0 ? val / drawW : val / drawH
                                ),
                            }))
                            console.log(
                                `[ZONE] Parsed ${zones.length} zones from decimalPointArrays for cam ${camId}`
                            )
                        } catch (e) {
                            console.error(
                                '[ZONE] Error parsing decimalPointArrays:',
                                e
                            )
                        }
                    }

                    // Ưu tiên 3: parse pointArrays (pixel) → normalize
                    if (zones.length === 0 && polygonData.pointArrays) {
                        try {
                            const savedPolygons = JSON.parse(
                                polygonData.pointArrays
                            )
                            const drawW = polygonData.drawWidth || 960
                            const drawH = polygonData.drawHeight || 540

                            zones = savedPolygons.map((poly, idx) => ({
                                zoneId: `zone_${idx}`,
                                zoneName: `Zone ${idx + 1}`,
                                points: (poly.points || []).map((val, i) =>
                                    i % 2 === 0 ? val / drawW : val / drawH
                                ),
                            }))
                            console.log(
                                `[ZONE] Parsed ${zones.length} zones from pointArrays for cam ${camId}`
                            )
                        } catch (e) {
                            console.error(
                                '[ZONE] Error parsing pointArrays:',
                                e
                            )
                        }
                    }

                    if (zones.length > 0) {
                        console.log(
                            `[ZONE] Setting ${zones.length} persistent zones on player${playerIdx}`
                        )
                        playerRef.setPersistentZones(zones)
                    }
                })
                .catch((err) => {
                    console.error(
                        `[ZONE] API error loading polygon for cam ${camId}:`,
                        err
                    )
                })
        },

        onPolygonUpdated(idx, newPoints) {
            this.polygons[idx].points = newPoints
        },
        /** khi click layer trống → tạo polygon mới */
        layerClick(e) {
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
        /** tạo vùng mặc định */
        addPolygon(x, y) {
            const points = [x, y]
            this.polygons.push({ points })
            this.activeIdx = this.polygons.length - 1
        },
        /** chọn polygon để chỉnh */
        setActive(idx) {
            this.activeIdx = this.activeIdx === idx ? null : idx
        },

        async doDelete() {
            const { isConfirmed } = await this.confirmDelete()
            if (isConfirmed) {
                try {
                    const res = await this.$services.delete(
                        `/streamConfigs/${this.streamConfigId}`
                    )
                    this.showSuccessToast(res.data.errorCode)
                    this.streamConfigId = null
                    const savedId = getStorage('streamConfigId')
                    if (String(savedId) == String(this.streamConfigId)) {
                        setStorage('streamConfigId', '', 1) // hoặc clearStorage() nếu bạn muốn
                    }
                    this.streamConfigId = null
                    this.loadStreamConfig()
                } catch (error) {
                    this.showErrorToast(error)
                }
            }
        },
        async confirmDelete() {
            return await this.$swal.fire({
                title: this.$t('common.confirmation.delete.title'),
                text: this.$t('common.confirmation.delete.message'),
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: this.$t('Message.Agree'),
                cancelButtonText: this.$t('Message.Exit'),
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-outline-danger ml-1',
                },
                buttonsStyling: false,
            })
        },
        // Mở rộng/thu gọn khu vực
        toggleArea(areaId) {
            if (this.expandedAreas.includes(areaId)) {
                this.expandedAreas = this.expandedAreas.filter(
                    (id) => id !== areaId
                )
            } else {
                this.expandedAreas.push(areaId)
            }
        },
        getStreamConfig() {},
        onClickMenu(text, data, code) {
            switch (code) {
                //Chọn cam
                case 'SCR':
                    this.canvasSelected(data)
                    this.isSidebarActive = true
                    break
                //Vẽ
                case 'DRA':
                    this.getFrameSelecting(data)
                    break
                default:
                    break
            }
        },
        //Lấy Id cam đang chọn
        getInfoSelecting(data) {
            this.camIdSelecting = data.camId
            this.p_width = data.width
            this.p_height = data.height

            //get data polygon đã lưu
            this.$services
                .get(`/device/polygon/${this.camIdSelecting}`)
                .then((response) => {
                    if (response.data.data != null) {
                        // this.arrpoints = response.data.data.decimalPointArrays
                        this.polygons = JSON.parse(
                            response.data.data.pointArrays
                        )
                        this.polygonId = response.data.data.id
                    } else {
                        this.arrpoints = []
                        this.polygonId = 0
                    }
                })
        },
        //lấy frame từ màn hình live
        getFrameSelecting(data) {
            switch (data) {
                case 1:
                    this.$refs.player1.getFrame()
                    this.getInfoSelecting(this.$refs.player1)
                    break
                case 2:
                    this.$refs.player2.getFrame()
                    this.getInfoSelecting(this.$refs.player2)
                    break
                case 3:
                    this.$refs.player3.getFrame()
                    this.getInfoSelecting(this.$refs.player3)
                    break
                case 4:
                    this.$refs.player4.getFrame()
                    this.getInfoSelecting(this.$refs.player4)
                    break
                case 5:
                    this.$refs.player5.getFrame()
                    this.getInfoSelecting(this.$refs.player5)
                    break
                case 6:
                    this.$refs.player6.getFrame()
                    this.getInfoSelecting(this.$refs.player6)
                    break
                case 7:
                    this.$refs.player7.getFrame()
                    this.getInfoSelecting(this.$refs.player7)
                    break
                case 8:
                    this.$refs.player8.getFrame()
                    this.getInfoSelecting(this.$refs.player8)
                    break
                case 9:
                    this.$refs.player9.getFrame()
                    this.getInfoSelecting(this.$refs.player9)
                    break
                default:
                    break
            }
            this.$services
                .get(`/lookup/polygon-function/${this.camIdSelecting}`)
                .then((response) => {
                    const data = response.data.data?.value || '[]'
                    const functions = JSON.parse(data) || []
                    if (functions.length > 0) {
                        this.polygonMetaData = functions
                    } else {
                        this.polygonMetaData = []
                    }
                })
        },

        cancel() {
            this.isDrawing = false
            this.modalDraw = false
        },
        base64Img(e) {
            if (e != null) {
                this.showModalDraw()

                const image = new window.Image()
                image.src = e
                image.width = this.drawWidth
                image.height = this.drawHeight
                image.onload = () => {
                    // set image only when it is loaded
                    this.image = image
                }
            } else {
                alert('Vui lòng thử lại')
            }
        },
        startDrawing() {
            this.isDrawing = true
            let drawLayer = this.$refs.drawLayer.getStage()

            this.arrcicle = []

            this.polygon = new Konva.Line({
                points: this.arrpoints,
                stroke: '#ff0000',
                strokeWidth: 1,
                draggable: false,
                closed: true,
                dash: [],
            })
            drawLayer.add(this.polygon)

            const polygon2 = new Konva.Line({
                points: [200, 200, 250, 1000],
                stroke: '#ff0000',
                strokeWidth: 1,
                draggable: false,
                closed: true,
                dash: [],
            })
            drawLayer.add(this.polygon)
            drawLayer.add(polygon2)
            this.isPolygonAdded = true

            //Vẽ cicle
            if (this.arrpoints.length > 0) {
                for (
                    let index = 0;
                    index < this.arrpoints.length / 2;
                    index++
                ) {
                    let item = new Konva.Circle({
                        x: parseFloat(this.arrpoints[index * 2]),
                        y: parseFloat(this.arrpoints[index * 2 + 1]),
                        radius: 7,
                        fill: 'red',
                        stroke: 'black',
                        strokeWidth: 2,
                        draggable: true,
                    })

                    // add hover styling
                    item.on('mouseover', function () {
                        document.body.style.cursor = 'pointer'
                        this.strokeWidth(4)
                    })

                    item.on('mouseout', function () {
                        document.body.style.cursor = 'default'
                        this.strokeWidth(2)
                    })

                    //Add mảng cicles
                    this.arrcicle.push(item)

                    var vm = this
                    item.on('dragmove', function () {
                        vm.polygon.points(vm.circlesToPoints(vm.arrcicle))
                        vm.arrpoints = vm.circlesToPoints(vm.arrcicle)
                    })

                    drawLayer.add(item)
                }
            }
        },
        drawingClick() {
            if (this.isDrawing) {
                let drawLayer = this.$refs.drawLayer.getStage()

                const mousePos = this.$refs.stage.getNode().getPointerPosition()

                let item = new Konva.Circle({
                    x: mousePos.x,
                    y: mousePos.y,
                    radius: 7,
                    fill: 'red',
                    stroke: 'black',
                    strokeWidth: 2,
                    draggable: true,
                })

                // add hover styling
                item.on('mouseover', function () {
                    document.body.style.cursor = 'pointer'
                    this.strokeWidth(4)
                })

                item.on('mouseout', function () {
                    document.body.style.cursor = 'default'
                    this.strokeWidth(2)
                })

                //Add mảng points
                this.arrpoints.push(mousePos.x)
                this.arrpoints.push(mousePos.y)

                //Add mảng cicles
                this.arrcicle.push(item)

                //update polygon
                if (this.isPolygonAdded && this.arrcicle.length > 0) {
                    this.polygon.points(this.circlesToPoints(this.arrcicle))
                }

                var vm = this
                item.on('dragmove', function () {
                    vm.polygon.points(vm.circlesToPoints(vm.arrcicle))
                    vm.arrpoints = vm.circlesToPoints(vm.arrcicle)
                })

                drawLayer.add(item)
            }
        },

        circlesToPoints(circles) {
            return circles
                .map((circle) => [circle.attrs.x, circle.attrs.y])
                .reduce((prev, current) => prev.concat(current))
        },

        //lưu vẽ vùng nhận diện
        savePolygons() {
            const polygonsMetaData = {}
            this.$refs.polygon.forEach((item, index) => {
                const res = item.getMetaData()
                polygonsMetaData[index] = {
                    ...res,
                }
            })

            this.isDrawing = false

            let saveData = {
                id: this.polygonId,
                deviceId: this.camIdSelecting,
                decimalPointArrays: JSON.stringify(this.polygons),
                width: this.p_width,
                height: this.p_height,
                drawWidth: this.drawWidth,
                drawHeight: this.drawHeight,
                strType: 'polygon',
                polygonsMetaData: JSON.stringify(polygonsMetaData),
            }

            const onSaveSuccess = (response) => {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Live.Success.Monitor'),
                        icon: 'CheckIcon',
                        variant: 'success',
                    },
                })
                // Cập nhật zone overlay ngay sau khi lưu polygon
                this.refreshZoneOverlay(saveData.deviceId)
            }

            const onSaveError = (error) => {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(error.response.data.message)}`,
                    },
                })
            }

            if (saveData.id == 0) {
                this.$services
                    .post('/device/polygon', saveData)
                    .then(onSaveSuccess)
                    .catch(onSaveError)
            } else {
                this.$services
                    .put(`/device/polygon/${this.polygonId}`, saveData)
                    .then(onSaveSuccess)
                    .catch(onSaveError)
            }
        },
        /**
         * Refresh zone overlay cho camera vừa sửa polygon
         * Tìm player đang hiển thị camera đó rồi reload polygon từ API
         */
        refreshZoneOverlay(deviceId) {
            if (!deviceId) return
            for (let i = 1; i <= 9; i++) {
                const ref = this.$refs[`player${i}`]
                if (ref && ref.camId === deviceId) {
                    this.loadPolygonForCamera(deviceId, i)
                    break
                }
            }
        },

        /**
         * Khôi phục camId, cameraName, cameraCode, polygon cho mỗi player
         * sau khi đổi layout (v-if destroy/create lại component)
         * @param {number} monitorCount - số player đang hiển thị
         */
        restorePlayerSlots(monitorCount) {
            for (let i = 1; i <= monitorCount; i++) {
                const ref = this.$refs[`player${i}`]
                const slot = this.playerSlots[i - 1]
                if (!ref) continue

                if (slot && slot.camId) {
                    ref.camId = slot.camId
                    ref.cameraName = slot.cameraName
                    ref.cameraCode = slot.cameraCode
                    // Reload polygon cho player này
                    this.loadPolygonForCamera(slot.camId, i)
                } else {
                    // Slot trống → clear polygon nếu có
                    ref.clearPersistentZones?.()
                }
            }
        },

        showModalDraw() {
            this.modalDraw = true
            this.isDrawing = false
        },
        //lấy danh sách camera
        async loadCams() {
            try {
                const { data } = await this.$services.get('/device/getAllCam')
                this.listCams = data.data
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: 'Error',
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(error.response.data.message)}`,
                    },
                })
            }
        },
        selectCam(item) {
            // const videoSource =
            //     this.nodeMediaServer + item.compId + '/' + item.code + '.flv'
            // const videoSource = 'http://192.168.1.85:18889/GT1/whep'
            const videoSource = `${this.mediaMTX}/${item.code}/whep`
            console.log('videoSource', videoSource)
            let camIndex = this.camIndexSelecting

            // Nếu camIndexSelecting không hợp lệ, tìm vị trí trống
            let isManualSelection = true
            if (
                !camIndex ||
                typeof camIndex !== 'number' ||
                camIndex < 1 ||
                camIndex > 9
            ) {
                isManualSelection = false
                for (let i = 1; i <= 9; i++) {
                    const ref = this.$refs[`player${i}`]
                    if (!ref?.camId || ref.camId === 0 || isNaN(ref.camId)) {
                        camIndex = i
                        break
                    }
                }
            }

            // Nếu không tìm được slot thì thoát
            if (!camIndex) return

            // Gán video và thông tin vào player tương ứng
            this[`videoSource${camIndex}`] = videoSource
            const playerRef = this.$refs[`player${camIndex}`]
            if (playerRef) {
                playerRef.camId = item.id
                playerRef.cameraName = item.name
                playerRef.cameraCode = item.code
            }

            // Cập nhật cameraCodeMap
            this.$set(this.cameraCodeMap, item.code, camIndex)

            // Lưu thông tin camera cho slot này (persist qua layout change)
            this.$set(this.playerSlots, camIndex - 1, {
                camId: item.id,
                cameraName: item.name,
                cameraCode: item.code,
            })

            // Load polygon zone overlay cho camera này
            this.$nextTick(() => {
                this.loadPolygonForCamera(item.id, camIndex)
            })

            // Chỉ đóng sidebar nếu là thao tác chọn thủ công
            if (isManualSelection) {
                this.isSidebarActive = false
            }

            // Cập nhật danh sách selected camera
            const camIds = []
            for (let i = 1; i <= 9; i++) {
                const id = this.$refs[`player${i}`]?.camId
                if (typeof id === 'number' && !isNaN(id) && id !== 0) {
                    camIds.push(id)
                }
            }
            this.selectedCameras = [...camIds]
        },

        hideSidebar() {
            this.isSidebarActive = false
        },
        canvasSelected(index) {
            this.camIndexSelecting = index

            switch (index) {
                case 1:
                    this.isSelected_1 = true

                    this.isSelected_2 = false
                    this.isSelected_3 = false
                    this.isSelected_4 = false
                    this.isSelected_5 = false
                    this.isSelected_6 = false
                    this.isSelected_7 = false
                    this.isSelected_8 = false
                    this.isSelected_9 = false
                    break
                case 2:
                    this.isSelected_2 = true

                    this.isSelected_1 = false
                    this.isSelected_3 = false
                    this.isSelected_4 = false
                    this.isSelected_5 = false
                    this.isSelected_6 = false
                    this.isSelected_7 = false
                    this.isSelected_8 = false
                    this.isSelected_9 = false
                    break
                case 3:
                    this.isSelected_3 = true

                    this.isSelected_1 = false
                    this.isSelected_2 = false
                    this.isSelected_4 = false
                    this.isSelected_5 = false
                    this.isSelected_6 = false
                    this.isSelected_7 = false
                    this.isSelected_8 = false
                    this.isSelected_9 = false
                    break
                case 4:
                    this.isSelected_4 = true

                    this.isSelected_1 = false
                    this.isSelected_2 = false
                    this.isSelected_3 = false
                    this.isSelected_5 = false
                    this.isSelected_6 = false
                    this.isSelected_7 = false
                    this.isSelected_8 = false
                    this.isSelected_9 = false
                    break
                case 5:
                    this.isSelected_5 = true

                    this.isSelected_1 = false
                    this.isSelected_2 = false
                    this.isSelected_3 = false
                    this.isSelected_4 = false
                    this.isSelected_6 = false
                    this.isSelected_7 = false
                    this.isSelected_8 = false
                    this.isSelected_9 = false
                    break
                case 6:
                    this.isSelected_6 = true

                    this.isSelected_1 = false
                    this.isSelected_2 = false
                    this.isSelected_3 = false
                    this.isSelected_4 = false
                    this.isSelected_5 = false
                    this.isSelected_7 = false
                    this.isSelected_8 = false
                    this.isSelected_9 = false
                    break
                case 7:
                    this.isSelected_7 = true

                    this.isSelected_1 = false
                    this.isSelected_2 = false
                    this.isSelected_3 = false
                    this.isSelected_4 = false
                    this.isSelected_5 = false
                    this.isSelected_6 = false
                    this.isSelected_8 = false
                    this.isSelected_9 = false
                    break
                case 8:
                    this.isSelected_8 = true

                    this.isSelected_1 = false
                    this.isSelected_2 = false
                    this.isSelected_3 = false
                    this.isSelected_4 = false
                    this.isSelected_5 = false
                    this.isSelected_6 = false
                    this.isSelected_7 = false
                    this.isSelected_9 = false
                    break
                case 9:
                    this.isSelected_9 = true

                    this.isSelected_1 = false
                    this.isSelected_2 = false
                    this.isSelected_3 = false
                    this.isSelected_4 = false
                    this.isSelected_5 = false
                    this.isSelected_6 = false
                    this.isSelected_7 = false
                    this.isSelected_8 = false
                    break
            }
        },
        async changeMonitor(val) {
            this.camIndexSelecting = 0

            //Bỏ chọn màn hình
            this.isSelected_1 = false
            this.isSelected_2 = false
            this.isSelected_3 = false
            this.isSelected_4 = false
            this.isSelected_5 = false
            this.isSelected_6 = false
            this.isSelected_7 = false
            this.isSelected_8 = false
            this.isSelected_9 = false

            this.monitor = val
            // Chờ layout mới render xong (v-if tạo player mới)
            await this.$nextTick()

            // Khôi phục thông tin camera và polygon cho các player mới
            this.restorePlayerSlots(val)

            switch (val) {
                case 1:
                    //pause
                    this.$refs.player2?.stop()
                    this.$refs.player3?.stop()
                    this.$refs.player4?.stop()
                    this.$refs.player5?.stop()
                    this.$refs.player6?.stop()
                    this.$refs.player7?.stop()
                    this.$refs.player8?.stop()
                    this.$refs.player9?.stop()
                    break
                case 4:
                    //stop
                    this.$refs.player5?.stop()
                    this.$refs.player6?.stop()
                    this.$refs.player7?.stop()
                    this.$refs.player8?.stop()
                    this.$refs.player9?.stop()

                    //play
                    this.$refs.player2?.play()
                    this.$refs.player3?.play()
                    this.$refs.player4?.play()
                    break
                case 5:
                    //stop
                    this.$refs.player6?.stop()
                    this.$refs.player7?.stop()
                    this.$refs.player8?.stop()
                    this.$refs.player9?.stop()

                    //play
                    this.$refs.player2?.play()
                    this.$refs.player3?.play()
                    this.$refs.player4?.play()
                    this.$refs.player5?.play()
                    break
                case 6:
                    //stop
                    this.$refs.player7?.stop()
                    this.$refs.player8?.stop()
                    this.$refs.player9?.stop()

                    //play
                    this.$refs.player2?.play()
                    this.$refs.player3?.play()
                    this.$refs.player4?.play()
                    this.$refs.player5?.play()
                    this.$refs.player6?.play()
                    break
                case 9:
                    //play
                    this.$refs.player2?.play()
                    this.$refs.player3?.play()
                    this.$refs.player4?.play()
                    this.$refs.player5?.play()
                    this.$refs.player6?.play()
                    this.$refs.player7?.play()
                    this.$refs.player8?.play()
                    this.$refs.player9?.play()
                    break
                default:
                // code block
            }

            // await this.loadConfig()
        },
        async saveConfig(isUpdate) {
            this.$refs.rules.validate().then(async (success) => {
                if (success) {
                    try {
                        const accessToken = this.$services.getUserData()
                        const playersStreaming = this.getPlayersByMonitor(
                            this.monitor
                        )
                        var vm = this
                        // Update basic stream configuration
                        this.streamConfig = {
                            ...this.streamConfig,
                            userId: accessToken.userId,
                            configName:
                                this.streamConfig.configName ||
                                `Config_${this.monitor}_${accessToken.userId}`,
                            monitorCount: this.monitor,
                            isUpdate: isUpdate,
                        }
                        // Update or initialize stream details
                        this.updateStreamDetails(playersStreaming)
                        // // validate
                        if (this.isInvalidStreamConfig()) {
                            this.showErrorToast(
                                'Vui lòng chọn ít nhất một camera'
                            )
                            return
                        }
                        // Save configuration via API
                        const res = await this.$services.post(
                            `/streamConfigs`,
                            this.streamConfig
                        )
                        this.streamConfigId = null
                        this.loadStreamConfig()

                        this.streamConfig = {
                            userId: null,
                            configName: null,
                            monitorCount: null,
                            streamDetail: [
                                {
                                    id: null,
                                    deviceId: null,
                                },
                            ],
                        }
                        this.showSuccessToast(res.data.errorCode)
                        this.loadStreamConfig()
                    } catch (error) {
                        this.showErrorToast(error)
                    } finally {
                        // Close the modal regardless of success or failure
                        this.modalStreamConfig = false
                        this.modalAddStreamConfig = false
                    }
                }
            })
        },
        showSuccessToast(message) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(`Response.ErrorCode.${message}`),
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
                    text: error.errorCode
                        ? this.$t(`Response.ErrorCode.${error.errorCode}`)
                        : error,
                },
            })
        },
        isInvalidStreamConfig() {
            return this.streamConfig.streamDetail.every(
                (x) => x.deviceId == 0 || x.deviceId == null
            )
        },
        updateStreamDetails(playersStreaming) {
            const feasiblePlayStream = playersStreaming.filter(
                (x) => +x.camId !== 0 || x.camId
            )
            if (
                !this.streamConfig.streamDetail.length ||
                !this.streamConfig.streamDetail[0]?.id
            ) {
                // Initialize streamDetail when empty or invalid
                this.streamConfig.streamDetail = feasiblePlayStream.map(
                    (player) => ({
                        deviceId: player.camId,
                        camIdxSelected: player.videoIndex,
                    })
                )
            } else {
                // Update existing stream details or add new players
                const updatedDetails = []
                feasiblePlayStream.forEach((player, index) => {
                    const existingItem = this.streamConfig.streamDetail[index]
                    if (existingItem) {
                        // Update existing item
                        existingItem.deviceId = player.camId
                        existingItem.camIdxSelected = player.videoIndex
                        updatedDetails.push(existingItem)
                    } else {
                        // Add new player
                        updatedDetails.push({
                            deviceId: player.camId,
                            camIdxSelected: player.videoIndex,
                        })
                    }
                })
                this.streamConfig.streamDetail = updatedDetails
            }
        },
        async loadStreaming() {
            const streamConfigData = this.transformConfigData()
            // chờ tất cả <FlvPlayer> (đặc biệt 5..9) mount xong
            await this.$nextTick()

            this.updateStreamLinks(streamConfigData)

            // đảm bảo các player visible được play đúng layout
            this.changeMonitor(Number(this.monitor || 1))
        },
        transformConfigData() {
            const vm = this
            const streamConfigData = this.streamConfig.streamDetail.map(
                (item) => {
                    const cameraItem = vm.listCams.find(
                        (dv) => dv.id === item.deviceId
                    )
                    return {
                        deviceId: item.deviceId,
                        camIdxSelected: item.camIdxSelected,
                        cameraName: cameraItem?.name,
                        compId: cameraItem?.compId,
                        code: cameraItem?.code,
                    }
                }
            )
            return streamConfigData
        },
        async loadDevicePermissions() {
            try {
                const { data } = await this.$services.get(`/user-device`)
                this.devicePermissions = data.data
            } catch (err) {
                throw new Error(err)
            }
        },
        updateStreamLinks(streamConfigDetail) {
            const vm = this
            this.selectedCameras = streamConfigDetail.map((x) => x.deviceId)

            streamConfigDetail.forEach((detail) => {
                // const videoSource =
                //     vm.nodeMediaServer +
                //     detail.compId +
                //     '/' +
                //     detail.code +
                //     '.flv'
                const videoSource = `${vm.mediaMTX}/${detail.code}/whep`
                const idx = detail.camIdxSelected
                const playerRef = vm.$refs[`player${idx}`]

                if (playerRef) {
                    playerRef.camId = detail.deviceId
                    playerRef.cameraName = detail.cameraName
                    playerRef.cameraCode = detail.code || ''
                    vm[`videoSource${idx}`] = videoSource

                    // Lưu thông tin camera cho slot (persist qua layout change)
                    vm.$set(vm.playerSlots, idx - 1, {
                        camId: detail.deviceId,
                        cameraName: detail.cameraName,
                        cameraCode: detail.code || '',
                    })

                    // Cập nhật cameraCodeMap
                    if (detail.code) {
                        vm.$set(vm.cameraCodeMap, detail.code, idx)
                    }

                    // >>> FIX: auto play player sau khi đã có source
                    if (vm[`videoSource${idx}`]) {
                        playerRef.play?.()
                    }

                    // Load polygon đã lưu cho camera này và hiển thị lên player
                    vm.loadPolygonsForPlayer(detail.deviceId, playerRef)
                }
            })

            if (streamConfigDetail.length === 0) {
                this.changeMonitor(this.monitor)
            }
        },
        getPlayersByMonitor(monitor) {
            const players = []
            for (let i = 1; i <= monitor; i++) {
                const player = this.$refs[`player${i}`]
                if (player) {
                    players.push(player)
                }
            }
            return players
        },

        /**
         * Load polygon đã lưu từ API và hiển thị persistent trên player
         * Parse polygonAI → pointArrays (fallback) để lấy tọa độ normalized
         */
        loadPolygonsForPlayer(deviceId, playerRef) {
            if (!deviceId || !playerRef) return

            this.$services
                .get(`/device/polygon/${deviceId}`)
                .then((res) => {
                    if (!res.data?.data) {
                        playerRef.setPersistentZones([])
                        return
                    }

                    const polygonData = res.data.data
                    let zones = []

                    // Ưu tiên 1: polygonAI (đã normalized 0-1)
                    if (polygonData.polygonAI) {
                        try {
                            const aiData =
                                typeof polygonData.polygonAI === 'string'
                                    ? JSON.parse(polygonData.polygonAI)
                                    : polygonData.polygonAI
                            const jsonPolygons = aiData.jsonPointPolygon || []

                            zones = jsonPolygons.map((poly, idx) => ({
                                zoneId: `polygon_${deviceId}_${idx}`,
                                zoneName: '',
                                points: (poly.pointPolygon || []).flatMap(
                                    (p) => [p.x, p.y]
                                ),
                                direction: null,
                                isTriggered: false,
                            }))
                        } catch (e) {
                            console.error(
                                '[POLYGON] Error parsing polygonAI:',
                                e
                            )
                        }
                    }

                    // Ưu tiên 2: pointArrays (pixel → normalized)
                    if (zones.length === 0 && polygonData.pointArrays) {
                        try {
                            const savedPolygons = JSON.parse(
                                polygonData.pointArrays
                            )
                            const drawW = polygonData.drawWidth || 960
                            const drawH = polygonData.drawHeight || 540

                            zones = savedPolygons.map((poly, idx) => ({
                                zoneId: `polygon_${deviceId}_${idx}`,
                                zoneName: '',
                                points: (poly.points || []).map((val, i) =>
                                    i % 2 === 0 ? val / drawW : val / drawH
                                ),
                                direction: null,
                                isTriggered: false,
                            }))
                        } catch (e) {
                            console.error(
                                '[POLYGON] Error parsing pointArrays:',
                                e
                            )
                        }
                    }

                    console.log(
                        `[POLYGON] Loaded ${zones.length} polygons for device ${deviceId}`
                    )
                    playerRef.setPersistentZones(zones)
                })
                .catch((err) => {
                    console.warn(
                        `[POLYGON] No polygon data for device ${deviceId}:`,
                        err.message
                    )
                    playerRef.setPersistentZones([])
                })
        },
        rechangeOptions(dataList) {
            return dataList?.map((item) => ({
                ...item,
                label: this.$t(item.text),
            }))
        },
        async loadStreamConfig() {
            try {
                const { data } = await this.$services.get(
                    `/streamConfigs/select-items`
                )

                this.listStreamConfig = data.data
            } catch (error) {}
        },
    },
}
</script>
<style lang="scss">
@import '@core/scss/vue/libs/vue-context.scss';

/* Hiệu ứng trượt ngang */
.slide-enter-active,
.slide-leave-active {
    transition: all 1s ease;
}

.slide-enter {
    opacity: 0;
    transform: translateX(100%);
    /* Trượt từ bên phải vào */
}

.slide-leave-to {
    opacity: 0;
    transform: translateX(100%);
    /* Trượt ra bên phải */
}

@keyframes flashBackground {
    0% {
        background-color: #ffeaa7;
    } /* vàng nhạt */
    50% {
        background-color: #fff;
    }
    100% {
        background-color: #ffeaa7;
    }
}

.flash-item {
    animation: flashBackground 1s ease-in-out infinite;
}

.player-box {
    border: 1px solid #404656;
}

.player-box-selected {
    border: 1px solid #db4213;
}

.modal-lg {
    max-width: 999px;
}

.hover-card {
    transition: all 0.2s ease-in-out;
}

.hover-card:hover {
    transform: translateY(-3px) scale(1.02);
    box-shadow: 0 6px 18px rgba(0, 0, 0, 0.15) !important;
    cursor: pointer;
}

.sidebar-scroll {
    max-height: 60vh;
    /* chiều cao cố định, bạn chỉnh tùy ý */
    overflow-y: auto;
    /* bật cuộn dọc */
    overflow-x: hidden;
    /* ẩn cuộn ngang nếu có */
}

/* Tùy chọn: style thanh cuộn đẹp hơn */
.sidebar-scroll::-webkit-scrollbar {
    width: 6px;
}

.sidebar-scroll::-webkit-scrollbar-thumb {
    background-color: rgba(0, 0, 0, 0.2);
    border-radius: 3px;
}

.sidebar-scroll::-webkit-scrollbar-thumb:hover {
    background-color: rgba(0, 0, 0, 0.4);
}
</style>
