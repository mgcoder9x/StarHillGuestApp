<template>
    <div>
        <b-card no-body>
            <b-card-header class="d-flex align-items-center justify-content-between flex-wrap" style="padding: 10px">
                <!-- Stream config -->

                <div class="d-flex align-items-center ml-auto">
                    <b-button variant="primary" @click="modalAddStreamConfig = true" size="sm" class="mr-1"
                        :title="$t('Title.Add')" v-if="!streamConfigId">
                        <Icon icon="material-symbols:add" style="font-size: 16px" />
                    </b-button>
                    <b-button variant="primary" @click="modalStreamConfig = true" size="sm" class="mr-1"
                        :title="$t('Title.Update')" v-if="streamConfigId">
                        <Icon icon="material-symbols:save-outline" style="font-size: 16px" />
                    </b-button>
                    <b-button variant="primary" @click="doDelete()" size="sm" class="mr-1" :title="$t('Title.Delete')"
                        v-if="streamConfigId">
                        <Icon icon="material-symbols:delete" style="font-size: 16px" />
                    </b-button>
                    <b-button variant="primary" @click="modalGreeting = true" size="sm" class="mr-1"
                        :title="$t('Sửa lời chào')">
                        <Icon icon="material-symbols:short-text" style="font-size: 16px" />
                    </b-button>
                    <b-button variant="primary" @click="modalChooseStreamConfig = true" size="sm" class="mr-1"
                        :title="$t('Chọn cấu hình')">
                        <Icon icon="material-symbols:settings" style="font-size: 16px" />
                    </b-button>
                    <b-button @click="isSidebarContent = !isSidebarContent" size="sm" class="mr-1"
                        :title="$t('Xem sự kiện')">
                        <Icon icon="material-symbols:visibility" style="font-size: 16px" />
                    </b-button>
                    <b-button @click="isSidebarActive = !isSidebarActive" size="sm" class="mr-1"
                        :title="$t('Title.ViewCamera')">
                        <Icon icon="material-symbols:camera" style="font-size: 16px" />
                    </b-button>
                    <!-- <b-col md="2"> -->
                    <b-dropdown variant="link" no-caret class="chart-dropdown" toggle-class="p-0" right>
                        <template #button-content>
                            <Icon icon="mingcute:more-2-line" class="md-icon text-body cursor-pointer" />
                        </template>
                        <b-dropdown-item href="#" @click="changeMonitor(1)">
                            <Icon icon="material-symbols:screenshot-monitor-outline-rounded" class="md-icon" />
                            <span class="align-text-bottom line-height-1">1</span>
                        </b-dropdown-item>
                        <b-dropdown-item href="#" @click="changeMonitor(4)">
                            <Icon icon="material-symbols:screenshot-monitor-outline-rounded" class="md-icon" />
                            <span class="align-text-bottom line-height-1">4</span>
                        </b-dropdown-item>
                        <b-dropdown-item href="#" @click="changeMonitor(5)">
                            <Icon icon="material-symbols:screenshot-monitor-outline-rounded" class="md-icon" />
                            <span class="align-text-bottom line-height-1">5</span>
                        </b-dropdown-item>
                        <b-dropdown-item href="#" @click="changeMonitor(6)">
                            <Icon icon="material-symbols:screenshot-monitor-outline-rounded" class="md-icon" />
                            <span class="align-text-bottom line-height-1">6</span>
                        </b-dropdown-item>
                        <b-dropdown-item href="#" @click="changeMonitor(9)">
                            <Icon icon="material-symbols:screenshot-monitor-outline-rounded" class="md-icon" />
                            <span class="align-text-bottom line-height-1">9</span>
                        </b-dropdown-item>
                    </b-dropdown>
                </div>
            </b-card-header>
            <b-card-body>
                <!-- context menu-->
                <vue-context ref="menu" v-slot="{ data }">
                    <li>
                        <b-link class="d-flex align-items-center" @click="
                            onClickMenu(
                                $event.target.innerText,
                                data,
                                'SCR'
                            )
                            ">
                            <feather-icon icon="MousePointerIcon" size="16" />
                            <span class="ml-75">{{
                                $t('Live.SelectCam')
                            }}</span>
                        </b-link>
                        <!-- <a href="#" @click.prevent="onClickMenu($event.target.innerText, data, 'SCR')">Chọn camera</a> -->
                    </li>
                    <li v-if="authorize(['IdentificationZone'])">
                        <b-link class="d-flex align-items-center" @click="
                            onClickMenu(
                                $event.target.innerText,
                                data,
                                'DRA'
                            )
                            ">
                            <feather-icon icon="SquareIcon" size="16" />
                            <span class="ml-75">{{
                                $t('Live.RecognitionArea')
                            }}</span>
                        </b-link>
                        <!-- <a href="#" @click.prevent="onClickMenu($event.target.innerText, data, 'DRA')">Vẽ vùng nhận
                            diện</a> -->
                    </li>
                </vue-context>
                <!-- context menu-->
                <b-row>

                    <b-col :md="isSidebarContent ? 9 : 12">

                        <div style="
                            padding-bottom: 3vh;
                            text-align: center;
                            font-family: 'Segoe UI', sans-serif;
                        ">
                            <div style="color: #007BFF; font-size: 25px">
                                {{ greeting.header1 }}
                            </div>
                            <div style="color: #007BFF; font-size: 30px; font-weight: bold">
                                {{ greeting.header2 }}
                            </div>
                        </div>
                        <b-col v-if="monitor === 1" cols="12">
                            <b-card-group class="mb-0">
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 1)
                                    ">
                                    <FlvPlayer ref="player1" :video-index="1" :source="videoSource1"
                                        @base64-img="base64Img">
                                        <canvas id="video1" class="card-img-top" :class="[
                                            isSelected_1
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(1)" />
                                    </FlvPlayer>
                                </b-card>
                            </b-card-group>
                        </b-col>
                        <b-col v-if="monitor === 4" cols="12">
                            <b-card-group class="mb-0">
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 1)
                                    ">
                                    <FlvPlayer ref="player1" :video-index="1" :source="videoSource1"
                                        @base64-img="base64Img">
                                        <canvas id="video1" class="card-img-top" :class="[
                                            isSelected_1
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(1)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 2)
                                    ">
                                    <FlvPlayer ref="player2" :video-index="2" :source="videoSource2"
                                        @base64-img="base64Img">
                                        <canvas id="video2" class="card-img-top" :class="[
                                            isSelected_2
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(2)" />
                                    </FlvPlayer>
                                </b-card>
                            </b-card-group>
                            <b-card-group class="mb-0">
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 3)
                                    ">
                                    <FlvPlayer ref="player3" :video-index="3" :source="videoSource3"
                                        @base64-img="base64Img">
                                        <canvas id="video3" class="card-img-top" :class="[
                                            isSelected_3
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(3)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 4)
                                    ">
                                    <FlvPlayer ref="player4" :video-index="4" :source="videoSource4"
                                        @base64-img="base64Img">
                                        <canvas id="video4" class="card-img-top" :class="[
                                            isSelected_4
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(4)" />
                                    </FlvPlayer>
                                </b-card>
                            </b-card-group>
                        </b-col>
                        <b-col v-if="monitor === 5" cols="12">
                            <b-card-group class="mb-0">
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 1)
                                    ">
                                    <FlvPlayer ref="player1" :video-index="1" :source="videoSource1"
                                        @base64-img="base64Img">
                                        <canvas id="video1" class="card-img-top" :class="[
                                            isSelected_1
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(1)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 2)
                                    ">
                                    <FlvPlayer ref="player2" :video-index="2" :source="videoSource2"
                                        @base64-img="base64Img">
                                        <canvas id="video2" class="card-img-top" :class="[
                                            isSelected_2
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(2)" />
                                    </FlvPlayer>
                                </b-card>
                            </b-card-group>
                            <b-card-group class="mb-0">
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 3)
                                    ">
                                    <FlvPlayer ref="player3" :video-index="3" :source="videoSource3"
                                        @base64-img="base64Img">
                                        <canvas id="video3" class="card-img-top" :class="[
                                            isSelected_3
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(3)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 4)
                                    ">
                                    <FlvPlayer ref="player4" :video-index="4" :source="videoSource4"
                                        @base64-img="base64Img">
                                        <canvas id="video4" class="card-img-top" :class="[
                                            isSelected_4
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(4)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 5)
                                    ">
                                    <FlvPlayer ref="player5" :video-index="5" :source="videoSource5"
                                        @base64-img="base64Img">
                                        <canvas id="video5" class="card-img-top" :class="[
                                            isSelected_5
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(5)" />
                                    </FlvPlayer>
                                </b-card>
                            </b-card-group>
                        </b-col>
                        <b-col v-if="monitor === 6" cols="12">
                            <b-card-group class="mb-0">
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 1)
                                    ">
                                    <FlvPlayer ref="player1" :video-index="1" :source="videoSource1"
                                        @base64-img="base64Img">
                                        <canvas id="video1" class="card-img-top" :class="[
                                            isSelected_1
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(1)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 2)
                                    ">
                                    <FlvPlayer ref="player2" :video-index="2" :source="videoSource2"
                                        @base64-img="base64Img">
                                        <canvas id="video2" class="card-img-top" :class="[
                                            isSelected_2
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(2)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 3)
                                    ">
                                    <FlvPlayer ref="player3" :video-index="3" :source="videoSource3"
                                        @base64-img="base64Img">
                                        <canvas id="video3" class="card-img-top" :class="[
                                            isSelected_3
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(3)" />
                                    </FlvPlayer>
                                </b-card>
                            </b-card-group>
                            <b-card-group class="mb-0">
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 4)
                                    ">
                                    <FlvPlayer ref="player4" :video-index="4" :source="videoSource4"
                                        @base64-img="base64Img">
                                        <canvas id="video4" class="card-img-top" :class="[
                                            isSelected_4
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(4)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 5)
                                    ">
                                    <FlvPlayer ref="player5" :video-index="5" :source="videoSource5"
                                        @base64-img="base64Img">
                                        <canvas id="video5" class="card-img-top" :class="[
                                            isSelected_5
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(5)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 6)
                                    ">
                                    <FlvPlayer ref="player6" :video-index="6" :source="videoSource6"
                                        @base64-img="base64Img">
                                        <canvas id="video6" class="card-img-top" :class="[
                                            isSelected_6
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(6)" />
                                    </FlvPlayer>
                                </b-card>
                            </b-card-group>
                        </b-col>
                        <b-col v-if="monitor === 9" cols="12">
                            <b-card-group class="mb-0">
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 1)
                                    ">
                                    <FlvPlayer ref="player1" :video-index="1" :source="videoSource1"
                                        @base64-img="base64Img">
                                        <canvas id="video1" class="card-img-top" :class="[
                                            isSelected_1
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(1)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 2)
                                    ">
                                    <FlvPlayer ref="player2" :video-index="2" :source="videoSource2"
                                        @base64-img="base64Img">
                                        <canvas id="video2" class="card-img-top" :class="[
                                            isSelected_2
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(2)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 3)
                                    ">
                                    <FlvPlayer ref="player3" :video-index="3" :source="videoSource3"
                                        @base64-img="base64Img">
                                        <canvas id="video3" class="card-img-top" :class="[
                                            isSelected_3
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(3)" />
                                    </FlvPlayer>
                                </b-card>
                            </b-card-group>
                            <b-card-group class="mb-0">
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 4)
                                    ">
                                    <FlvPlayer ref="player4" :video-index="4" :source="videoSource4"
                                        @base64-img="base64Img">
                                        <canvas id="video4" class="card-img-top" :class="[
                                            isSelected_4
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(4)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 5)
                                    ">
                                    <FlvPlayer ref="player5" :video-index="5" :source="videoSource5"
                                        @base64-img="base64Img">
                                        <canvas id="video5" class="card-img-top" :class="[
                                            isSelected_5
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(5)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 6)
                                    ">
                                    <FlvPlayer ref="player6" :video-index="6" :source="videoSource6"
                                        @base64-img="base64Img">
                                        <canvas id="video6" class="card-img-top" :class="[
                                            isSelected_6
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(6)" />
                                    </FlvPlayer>
                                </b-card>
                            </b-card-group>
                            <b-card-group class="mb-0">
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 7)
                                    ">
                                    <FlvPlayer ref="player7" :video-index="7" :source="videoSource7"
                                        @base64-img="base64Img">
                                        <canvas id="video7" class="card-img-top" :class="[
                                            isSelected_7
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(7)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 8)
                                    ">
                                    <FlvPlayer ref="player8" :video-index="8" :source="videoSource8"
                                        @base64-img="base64Img">
                                        <canvas id="video8" class="card-img-top" :class="[
                                            isSelected_8
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(8)" />
                                    </FlvPlayer>
                                </b-card>
                                <b-card img-top no-body @contextmenu.prevent="
                                    $refs.menu.open($event, 9)
                                    ">
                                    <FlvPlayer ref="player9" :video-index="9" :source="videoSource9"
                                        @base64-img="base64Img">
                                        <canvas id="video9" class="card-img-top" :class="[
                                            isSelected_9
                                                ? 'player-box-selected'
                                                : 'player-box',
                                        ]" @click="canvasSelected(9)" />
                                    </FlvPlayer>
                                </b-card>
                            </b-card-group>
                        </b-col>


                            <!-- Footer -->
                            <div style="padding-top: 5vh; text-align: center;">
                                <div
                                    style="display: flex; align-items: center; text-align: center;  font-style: italic; color: #007BFF; font-size: 17px; font-weight: 500;">
                                    <hr style="flex: 1; border: none; border-top: 1px solid #007BFF;" />
                                    <span style="padding: 0 10px;">{{ greeting.footer }}</span>
                                    <hr style="flex: 1; border: none; border-top: 1px solid #007BFF;" />
                                </div>
                            </div>
                    </b-col>
                    <b-col :md="isSidebarContent ? 3 : 0" v-if="isSidebarContent">
                        <div v-for="item in filteredEventList.slice(0, 7)" :key="item.eventId" 
                        :class="{ 'flash-item': item.isNew }"
                        :style="{
                            border: '1px solid #dcdcdc',
                            borderRadius: '10px',
                            padding: '10px',
                            display: 'flex',
                            alignItems: 'center',
                            gap: '10px',
                            marginBottom: '10px',
                            height: '12vh'
                        }">
                            <img :src="`${baseURL}${item.image}`" alt="avatar" style="
      width: 60px;
      height: 60px;
      object-fit: cover;
      border-radius: 50%;
    " />
                            <div style="flex: 1">
                                <div style="font-weight: 500; margin-bottom: 4px">
                                    <span style="font-size: 16px;">
                                        Đồng chí :
                                    </span>
                                    <span :style="{
                                        color:
                                            item.personId != '00000000-0000-0000-0000-000000000000'
                                                ? '#28c76f'
                                                : '#ff9f43',
                                        fontWeight: '600',
                                        fontSize: '16px'
                                    }">
                                        {{ item.userName }}
                                    </span>
                                </div>
                                <div style="font-size: 16px;">
                                    Chức vụ : {{ item.position ? item.position : 'Chưa xác định' }}
                                </div>
                                <!-- <div>Đơn vị : {{ item.areaName }}</div> -->
                                <div style="font-size: 16px;">Thời gian : {{ item.accessTimeStr }}</div>
                            </div>
                        </div>
                    </b-col>
                </b-row>
            </b-card-body>
        </b-card>

        <!-- Vẽ polygon -->
        <b-modal v-if="modalDraw" v-model="modalDraw" :title="$t('Live.Draw')" ok-title="Đóng" hide-header-close ok-only
            size="lg">
            <div>
                <v-stage ref="stage" :config="configKonva">
                    <v-layer ref="drawLayer" @click="layerClick">
                        <v-image :config="{ image: image }" />

                        <div v-if="isDrawing">
                            <PolygonEditor v-for="(poly, idx) in polygons" ref="polygon" :key="idx"
                                :points.sync="poly.points" :active="idx === activeIdx" @polygon-click="setActive(idx)"
                                @update:points="onPolygonUpdated(idx, $event)" />
                        </div>
                    </v-layer>
                </v-stage>
            </div>
            <template #modal-footer>
                <div class="d-flex justify-content-between w-100">
                    <!-- Bên trái -->
                    <div>
                        <b-button v-if="isDrawing" variant="danger" class="mr-1" @click="
                            polygons.splice(activeIdx, 1)
                        activeIdx = null
                            ">
                            {{ $t('Button.Delete') }}
                        </b-button>
                        <b-button v-if="isDrawing" variant="primary" class="mr-1" @click="
                            activeIdx = null
                        polygons.splice(0, polygons.length)
                            ">
                            {{ $t('Button.Refresh') }}
                        </b-button>
                    </div>

                    <!-- Bên phải -->
                    <div>
                        <b-button v-if="!isDrawing" variant="primary" class="mr-1" @click="isDrawing = true">
                            {{ $t('Button.Edit') }}
                        </b-button>
                        <b-button v-if="isDrawing" variant="primary" class="mr-1" @click="savePolygons">
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
        <b-sidebar id="sidebar-add-new-event" v-model="isSidebarActive" sidebar-class="sidebar-xl"
            :visible="isSidebarActive" bg-variant="white" shadow backdrop no-header right>
            <template>
                <!-- Header -->
                <div class="d-flex justify-content-between align-items-center content-sidebar-header px-2 py-1">
                    <h5 class="mb-0">Danh sách camera</h5>
                    <div>
                        <feather-icon class="ml-1 cursor-pointer" icon="XIcon" size="16" @click="hideSidebar" />
                    </div>
                </div>
                <b-list-group class="list-group-filters">
                    <!-- Lặp qua từng khu vực -->
                    <b-list-group-item v-for="area in groupedCams" :key="area.id" class="cursor-pointer p-0"
                        @click="toggleArea(area.id)">
                        <!-- Tiêu đề khu vực (có thể bấm để mở rộng/thu gọn) -->
                        <div style="margin-block: 5px; padding: 5px">
                            <feather-icon :icon="expandedAreas.includes(area.id)
                                ? 'ChevronDownIcon'
                                : 'ChevronRightIcon'
                                " size="18" class="mr-75" />
                            <span class="align-text-bottom line-height-1">{{
                                area.name
                            }}</span>
                        </div>
                        <!-- Danh sách camera trong khu vực, chỉ hiển thị khi khu vực được mở -->
                        <b-collapse :visible="expandedAreas.includes(area.id)">
                            <b-list-group>
                                <b-list-group-item v-for="cam in area.cameras" :key="cam.id + cam.code"
                                    class="cursor-pointer pl-4" :disabled="selectedCameras.includes(cam.id)"
                                    style="border-left: none; border-radius: 0" @click="selectCam(cam)">
                                    <feather-icon v-if="selectedCameras.includes(cam.id)" icon="CheckIcon" size="18"
                                        class="mr-75" />
                                    <feather-icon icon="VideoIcon" size="18" class="mr-75" />
                                    <span class="align-text-bottom line-height-1" :style="cam.status == 0
                                        ? 'color:red'
                                        : selectedCameras.includes(
                                            cam.id
                                        )
                                            ? 'color:green'
                                            : 'color:blue'
                                        ">{{ cam.name }}</span>
                                </b-list-group-item>
                            </b-list-group>
                        </b-collapse>
                    </b-list-group-item>
                </b-list-group>
            </template>
        </b-sidebar>

        <!-- Lưu cấu hình -->
        <b-modal v-model="modalStreamConfig" :title="$t('StreamConfig.Save')" ok-title="Đóng" hide-header-close ok-only
            size="lg">
            <div>
                <b-form-group :label="$t('StreamConfig.Name')" label-for="h-config-name" label-cols-md="4"
                    label-class="required" class="mb-50 mb-md-1">
                    <b-form-input id="h-area-code" v-model="streamConfig.configName"
                        :placeholder="$t('StreamConfig.Name')" />
                </b-form-group>
            </div>
            <template #modal-footer>
                <div class="w-100" style="text-align: right">
                    <b-button variant="primary" class="mr-1" @click="saveConfig(true)">
                        {{ $t('Button.Save') }}
                    </b-button>
                    <b-button @click="modalStreamConfig = false">
                        {{ $t('Button.Exit') }}
                    </b-button>
                </div>
            </template>
        </b-modal>

        <!-- Thêm mới cấu hình -->
        <b-modal v-model="modalAddStreamConfig" :title="$t('StreamConfig.Create')" ok-title="Đóng" hide-header-close
            ok-only size="lg">
            <div>
                <b-form-group :label="$t('StreamConfig.Name')" label-for="h-config-name" label-cols-md="4"
                    label-class="required" class="mb-50 mb-md-1">
                    <b-form-input id="h-area-code" v-model="streamConfig.configName"
                        :placeholder="$t('StreamConfig.Name')" />
                </b-form-group>
            </div>
            <template #modal-footer>
                <div class="w-100" style="text-align: right">
                    <b-button variant="primary" class="mr-1" @click="saveConfig(false)">
                        {{ $t('Button.Save') }}
                    </b-button>
                    <b-button @click="modalAddStreamConfig = false">
                        {{ $t('Button.Exit') }}
                    </b-button>
                </div>
            </template>
        </b-modal>

        <!-- Chọn cấu hình -->
        <b-modal v-model="modalChooseStreamConfig" :title="$t('Chọn cấu hình')" ok-title="Đóng" hide-header-close
            ok-only size="lg">
            <b-form-group :label="$t('FormGroupName.StreamConfig')" label-for="create-area-code" label-cols-md="5"
                label-class="required" :class="formGroupClass">
                <div class="d-flex align-items-center">
                    <v-select v-model="streamConfigId" :dir="$store.state.appConfig.isRTL ? 'rtl' : 'ltr'
                        " :options="rechangeOptions(listStreamConfig)" label="label"
                        :reduce="(streamConfig) => streamConfig.id" @input="getStreamConfig" class="flex-grow-1 mr-2" />
                </div>
            </b-form-group>
        </b-modal>

        <!-- Sửa lời chào -->
        <b-modal v-model="modalGreeting" :title="$t('Sửa lời chào')" ok-title="Đóng" hide-header-close
            ok-only size="lg">
            <div>
                <b-form-group :label="$t('Lời chào')" label-cols-md="4"
                    label-class="required" class="mb-50 mb-md-1">
                    <b-form-input v-model="greeting.header1"
                        :placeholder="$t('Lời chào')" />
                </b-form-group>
                <b-form-group :label="$t('Tên triển lãm')" label-cols-md="4"
                    label-class="required" class="mb-50 mb-md-1">
                    <b-form-input v-model="greeting.header2"
                        :placeholder="$t('Tên triển lãm')" />
                </b-form-group>
                <b-form-group :label="$t('Phần mềm')" label-cols-md="4"
                    label-class="required" class="mb-50 mb-md-1">
                    <b-form-input v-model="greeting.footer"
                        :placeholder="$t('Phần mềm')" />
                </b-form-group>
            </div>
            <template #modal-footer>
                <div class="w-100" style="text-align: right">
                    <b-button variant="primary" class="mr-1" @click="saveGreeting()">
                        {{ $t('Button.Save') }}
                    </b-button>
                    <b-button @click="modalGreeting = false">
                        {{ $t('Button.Exit') }}
                    </b-button>
                </div>
            </template>
        </b-modal>
    </div>
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
import { v4 as uuidv4 } from 'uuid'
export default {
    mixins: [authorizationMixin],
    components: {
        SelectCameraSidebar,
        VueContext,
        PolygonEditor,
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
            modalChooseStreamConfig: false, //Show modal chọn cấu hình
            modalGreeting:false, //Show modal sửa lời chào

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
            greeting:{
                header1: null,
                header2: null,
                footer: null
            }
        }
    },
    setup() {
        // App Name
        const nodeMediaServer = process.env.VUE_APP_NODE_MEDIA_SERVER

        return {
            nodeMediaServer,
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
        streamConfigId(newVal) {
            // Xóa tất cả luồng video
            for (let i = 1; i <= 9; i++) {
                this[`videoSource${i}`] = null
                if (this.$refs[`player${i}`]) {
                    this.$refs[`player${i}`].stop()
                    this.$refs[`player${i}`].camId = null
                    this.$refs[`player${i}`].fps = 0
                    this.$refs[`player${i}`].cameraName = null
                }
                this[`isSelected_${i}`] = false
            }
            this.$nextTick(() => {
                if (newVal) {
                    let currentStreamConfig = this.listStreamConfig.find(
                        (x) => x.id == newVal
                    )
                    if (currentStreamConfig) {
                        currentStreamConfig = {
                            id: currentStreamConfig.id,
                            userId: currentStreamConfig.userId,
                            monitorCount: currentStreamConfig.monitorCount,
                            configName: currentStreamConfig.text,
                            streamDetail: currentStreamConfig.streamDetail,
                        }
                    }
                    this.streamConfig = currentStreamConfig
                    this.monitor = this.streamConfig.monitorCount
                    this.loadStreaming()
                } else {
                    this.streamConfig = {
                        id: null,
                        userId: null,
                        configName: null,
                        monitorCount: null,
                        streamDetail: [],
                    }
                    this.selectedCameras = []
                    this.camIndexSelecting = 0
                    this.loadStreaming()
                }
                setStorage('streamConfigId', newVal, 120)
            })
        },
    },
    async created() {
        await this.loadDevicePermissions()
        await this.loadCams()
        await this.loadStreamConfig()
        this.loadEvent()
        const streamConfigId = getStorage('streamConfigId')
        this.streamConfigId = streamConfigId
        debugger
        const greeting = getStorage('greeting')
        if(greeting){
            this.greeting = greeting
        }

        await signalRService.connect('notificationHub')
        signalRService.on('NewEvent', (data) => {
            if (data) {
                const dataJson = JSON.parse(data)
                this.pushDataEvent(dataJson)
            }
        })
    },
    methods: {
        saveGreeting(){
            setStorage('greeting', this.greeting, 120)
            this.modalGreeting = false
        },
        pushDataEvent(event) {
            if (event.EventTypeId == 200) {
                if (event.FaceEvent.FaceFeature != null && event.FaceEvent.FaceFeature != '') {
                    var object = {
                        image: event.Image,
                        eventTypeId: event.EventTypeId,
                        userName: event.FaceEvent.Fullname || 'Chưa xác định',
                        position: event.FaceEvent.Position || 'Chưa xác định',
                        areaName: event.AreaName,
                        accessTimeStr: moment(event.AccessTime).format('DD/MM/YY HH:mm:ss'),
                        personId: event.FaceEvent.PersonId,
                        eventId: event.EventId,
                        isNew: true
                    }
                    this.filteredEventList.unshift(object);
                    // 👉 sau 3 giây thì bỏ trạng thái nhấp nháy
                    setTimeout(() => {
                        if (this.filteredEventList.length > 0) {
                            var a = this.filteredEventList.find(x=> x.eventId == event.EventId)
                            a.isNew = false
                        }
                    }, 3000);
                }
            }
        },
        async loadEvent() {
            const pagination = {
                page: 1,
                itemsPerPage: 10,
                sortBy: 'accessTime',
                sortDesc: true
            }
            const formData = `${new URLSearchParams(pagination).toString()}`
            const response = await this.$services.get(`/faceEvents?${formData}`)
            this.filteredEventList = response.data.data.data
        },
        onPolygonUpdated(idx, newPoints) {
            this.polygons[idx].points = newPoints
        },
        /** khi click layer trống → tạo polygon mới */
        layerClick(e) {
            const { x, y } = this.$refs.stage.getNode().getPointerPosition()
            if (this.activeIdx !== null) {
                if (
                    !pointInPolygon(x, y, this.polygons[this.activeIdx].points)
                ) {
                    this.$refs.polygon[this.activeIdx].addPoint(x, y)
                }
                return
            }

            function pointInPolygon(x, y, points) {
                let inside = false
                for (
                    let i = 0, j = points.length / 2 - 1;
                    i < points.length / 2;
                    j = i++
                ) {
                    const xi = points[i * 2],
                        yi = points[i * 2 + 1]
                    const xj = points[j * 2],
                        yj = points[j * 2 + 1]

                    const intersect =
                        yi > y !== yj > y &&
                        x < ((xj - xi) * (y - yi)) / (yj - yi) + xi

                    if (intersect) inside = !inside
                }
                return inside
            }

            const isInsideAny = this.polygons.some((p) =>
                pointInPolygon(x, y, p.points)
            )

            if (!isInsideAny) {
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
        getStreamConfig() {
            this.modalChooseStreamConfig = false
        },
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
            }

            if (saveData.id == 0) {
                this.$services
                    .post('/device/polygon', saveData)
                    .then((response) => {
                        this.$toast({
                            component: ToastificationContent,
                            position: 'top-right',
                            props: {
                                title: `Lưu vùng nhận diện thành công`,
                                icon: 'CheckIcon',
                                variant: 'success',
                            },
                        })
                    })
                    .catch((error) => {
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
                    })
            } else {
                this.$services
                    .put(`/device/polygon/${this.polygonId}`, saveData)
                    .then((response) => {
                        this.$toast({
                            component: ToastificationContent,
                            position: 'top-right',
                            props: {
                                title: `Lưu vùng nhận diện thành công`,
                                icon: 'CheckIcon',
                                variant: 'success',
                            },
                        })
                    })
                    .catch((error) => {
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
                    })
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
            debugger

            const videoSource =
                this.nodeMediaServer + item.compId + '/' + item.code + '.flv'

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
            }

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
            try {
                const accessToken = this.$services.getUserData()
                const playersStreaming = this.getPlayersByMonitor(this.monitor)
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
                    this.showErrorToast('Vui lòng chọn ít nhất một camera')
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
        },
        showSuccessToast(message) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    text: this.$t(`Response.ErrorCode.${message}`),
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
                    title: 'Error',
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
        loadStreaming() {
            const streamConfigData = this.transformConfigData()
            this.updateStreamLinks(streamConfigData)
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
                const videoSource =
                    vm.nodeMediaServer +
                    detail.compId +
                    '/' +
                    detail.code +
                    '.flv'
                // Dynamically update the corresponding player reference
                const playerRef = vm.$refs[`player${detail.camIdxSelected}`]
                if (playerRef) {
                    playerRef.camId = detail.deviceId
                    playerRef.cameraName = detail.cameraName
                    vm[`videoSource${detail.camIdxSelected}`] = videoSource
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
            } catch (error) { }
        },
    },
}
</script>
<style lang="scss">
@import '@core/scss/vue/libs/vue-context.scss';
@keyframes flashBackground {
  0% { background-color: #ffeaa7; }     /* vàng nhạt */
  50% { background-color: #fff; }
  100% { background-color: #ffeaa7; }
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
</style>
