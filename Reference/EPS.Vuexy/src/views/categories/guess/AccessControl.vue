<template>
    <div class="access-section">
        <b-card-header class="section-header">
        <h5 class="section-title text-primary">
            {{ $t('categories.employees.common.form.roles.title') }}
        </h5>
        </b-card-header>

        <b-form @submit.prevent>
            <!-- Header -->
            <b-row class="form-header mb-2">
                <b-col md="4" class="font-weight-bold">
                    {{ $t('categories.employees.common.form.roles.area') }}
                </b-col>
                <b-col md="3" class="font-weight-bold">
                    {{ $t('categories.employees.common.form.roles.timeFrom') }}
                </b-col>
                <b-col md="3" class="font-weight-bold">
                    {{ $t('categories.employees.common.form.roles.timeTo') }}
                </b-col>
                <b-col md="2" class="text-right font-weight-bold">
                    {{ $t('categories.employees.common.form.roles.action') }}
                </b-col>
            </b-row>

            <!-- Dòng dữ liệu -->
            <transition-group name="fade" tag="div">
                <div
                    v-for="(item, idx) in form.items"
                    :key="idx"
                    class="data-row mb-2 p-2 rounded shadow-sm"
                >
                    <b-row>
                        <!-- Khu vực -->
                        <b-col md="4">
                            <tree-select
                                v-model="item.areaId"
                                :options="areaOptions"
                                label="text"
                                :reduce="(area) => area.id"
                                :disabled="disabled"
                                :append-to-body="true"
                            />
                        </b-col>

                        <!-- Thời gian bắt đầu -->
                        <b-col md="3">
                            <date-picker
                                v-model="item.startTime"
                                type="datetime"
                                format="DD-MM-YYYY HH:mm:ss"
                                value-type="YYYY-MM-DD HH:mm:ss"
                                style="width: 100%"
                                :disabled="disabled"
                                input-class="form-control"
                                :class="{ 'is-invalid': !isTimeInRange(item.startTime, checkIn, checkOut) && item.startTime }"
                            />
                            <small
                                v-if="item.startTime && !isTimeInRange(item.startTime, checkIn, checkOut)"
                                class="text-danger d-block mt-1"
                            >
                                <i class="fas fa-exclamation-circle"></i>
                                {{ checkOut ? $t('categories.employees.common.form.validation.accessTimeInRange') : $t('categories.employees.common.form.validation.accessTimeAfterCheckIn') }}
                            </small>
                        </b-col>

                        <!-- Thời gian kết thúc -->
                        <b-col md="3">
                            <date-picker
                                v-model="item.endTime"
                                type="datetime"
                                format="DD-MM-YYYY HH:mm:ss"
                                value-type="YYYY-MM-DD HH:mm:ss"
                                style="width: 100%"
                                :disabled="disabled"
                                input-class="form-control"
                                :class="{ 'is-invalid': !isTimeInRange(item.endTime, checkIn, checkOut) && item.endTime }"
                            />
                            <!-- Cảnh báo -->
                            <small
                                v-if="
                                    item.startTime &&
                                    item.endTime &&
                                    item.endTime < item.startTime
                                "
                                class="text-danger d-block mt-1"
                            >
                                <i class="fas fa-exclamation-circle"></i>
                                {{
                                    this.$t(
                                        'categories.employees.common.form.validation.endTimeGreaterThanStartTime'
                                    )
                                }}
                            </small>
                            <small
                                v-if="item.endTime && !isTimeInRange(item.endTime, checkIn, checkOut)"
                                class="text-danger d-block mt-1"
                            >
                                <i class="fas fa-exclamation-circle"></i>
                                {{ checkOut ? $t('categories.employees.common.form.validation.accessTimeInRange') : $t('categories.employees.common.form.validation.accessTimeAfterCheckIn') }}
                            </small>
                        </b-col>

                        <!-- Nút hành động -->
                        <b-col md="2" class="text-right">
                            <b-button
                                v-if="form.items.length > 1"
                                size="sm"
                                variant="outline-danger"
                                class="btn-icon"
                                :disabled="disabled"
                                @click="remove(idx)"
                            >
                                <!-- <i class="fas fa-minus"></i> -->
                                <Icon icon="fa7-solid:minus" class="xs-icon" />
                            </b-button>

                            <b-button
                                size="sm"
                                class="btn-icon ml-2"
                                variant="primary"
                                :disabled="!canAdd(item) || disabled"
                                @click="add"
                            >
                                <!-- <i class="fas fa-plus"></i> -->
                                <Icon icon="mdi:plus-thick" class="xs-icon" />
                            </b-button>
                        </b-col>
                    </b-row>
                </div>
            </transition-group>
        </b-form>
    </div>
</template>

<script>
import TreeHelper from '@/utils/treeHelper'

export default {
    props: {
        disabled: { type: Boolean, default: false },
        checkIn: { type: String, default: null },   // YYYY-MM-DD HH:mm
        checkOut: { type: String, default: null },  // YYYY-MM-DD HH:mm
    },
    watch: {
        checkIn(newVal) {
            if (newVal && this.checkOut) {
                this.autoFillAccessTimes()
            }
        },
        checkOut(newVal) {
            if (newVal && this.checkIn) {
                this.autoFillAccessTimes()
            }
        },
    },
    data() {
        return {
            areaOptions: [],
            form: {
                items: [
                    {
                        areaId: null,
                        startTime: null,
                        endTime: null,
                    },
                ],
            },
        }
    },
    created() {
        this.loadArea()
    },
    methods: {
        getAccessControl() {
            return this.form.items
        },
        setAccessControl(accessControl) {
            this.form.items = accessControl
        },
        loadArea() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.areaOptions = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
        },
        autoFillAccessTimes() {
            if (!this.checkIn || !this.checkOut) return

            const checkIn = this.$moment(this.checkIn, 'YYYY-MM-DD HH:mm')
            const checkOut = this.$moment(this.checkOut, 'YYYY-MM-DD HH:mm')

            if (!checkIn.isValid() || !checkOut.isValid() || checkOut.isBefore(checkIn)) return

            this.form.items = this.form.items.map(item => ({
                ...item,
                startTime: item.startTime || checkIn.format('YYYY-MM-DD HH:mm:ss'),
                endTime: item.endTime || checkOut.format('YYYY-MM-DD HH:mm:ss'),
            }))
        },

        // Kiểm tra thời gian có nằm trong khoảng checkIn/checkOut không
        isTimeInRange(time, start, end) {
            if (!start || !time) return true
            const t = this.$moment(time, 'YYYY-MM-DD HH:mm:ss')
            const s = this.$moment(start, 'YYYY-MM-DD HH:mm')
            if (!t.isValid() || !s.isValid()) return true
            if (!end) {
                // Chỉ có checkIn: thời gian phải >= checkIn
                return t.isSameOrAfter(s)
            }
            const e = this.$moment(end, 'YYYY-MM-DD HH:mm')
            if (!e.isValid()) return true
            // Có cả checkIn và checkOut: thời gian phải nằm trong khoảng
            return t.isSameOrAfter(s) && t.isSameOrBefore(e)
        },
        canAdd(row) {
            return (
                !!row.areaId &&
                !!row.startTime &&
                !!row.endTime &&
                row.endTime >= row.startTime
            )
        },
        add() {
            this.form.items.push({
                areaId: null,
                startTime: null,
                endTime: null,
            })
        },
        remove(idx) {
            this.form.items.splice(idx, 1)
        },
        validateAccessTimes() {
            if (!this.checkIn) return true

            return this.form.items.every(item =>
                (!item.startTime || this.isTimeInRange(item.startTime, this.checkIn, this.checkOut)) &&
                (!item.endTime || this.isTimeInRange(item.endTime, this.checkIn, this.checkOut))
            )
        },
    },
}
</script>

<style scoped>
.access-section {
    background-color: #ffffff;
    border-radius: 10px;
    border: 1px solid #dee2e6;
    overflow: hidden;
    box-shadow: 0 3px 10px rgba(0, 0, 0, 0.05);
}

/* Header */
.section-header {
    background-color: #f8f9fa;
    border-bottom: 1px solid #dee2e6;
    padding: 0.75rem 1rem;
}
.section-title {
    font-weight: 600;
    font-size: 1.05rem;
    color: #0d6efd;
    margin: 0;
}

/* Giống table */
.table-container {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.92rem;
}

/* Header bảng */
.form-header {
    background-color: #f1f3f5;
    border-bottom: 2px solid #dee2e6;
    font-weight: 600;
    color: #495057;
    padding: 0.75rem 0.5rem;
    margin: 0;
}

/* Dòng dữ liệu */
.data-row {
    display: block;
    background-color: #fff;
    border-bottom: 1px solid #e9ecef;
    transition: background-color 0.2s ease;
}

.data-row:hover {
    background-color: #f8fbff;
}

/* Các ô trong dòng */
.data-row > .b-col {
    display: table-cell;
    vertical-align: middle;
    padding: 0.75rem;
    border-right: 1px solid #f1f3f5;
}

.data-row > .b-col:last-child {
    border-right: none;
}

/* Hàng cuối */
.data-row:last-child {
    border-bottom: none;
}

/* Nút hành động */
.btn-icon {
    width: 30px;
    height: 30px;
    border-radius: 50%;
    padding: 0;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s ease;
}
.btn-icon:hover {
    transform: scale(1.08);
}
.btn-icon:active {
    transform: scale(0.95);
}
.xs-icon {
    font-size: 0.9rem;
}

/* Cảnh báo */
.text-danger {
    font-size: 0.8rem;
    margin-top: 0.25rem;
}

/* Hiệu ứng thêm/xóa */
.fade-enter-active,
.fade-leave-active {
    transition: all 0.25s ease;
}
.fade-enter-from,
.fade-leave-to {
    opacity: 0;
    transform: translateY(-5px);
}
</style>

