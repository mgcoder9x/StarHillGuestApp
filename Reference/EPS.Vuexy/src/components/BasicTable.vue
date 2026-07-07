<template>
    <div class="bt-root">
        <vue-good-table
            mode="remote"
            :is-loading.sync="isLoading"
            :columns="[
                { label: $t('Bgrid.Index'), field: 'stt' },
                ...filteredColumns,
            ]"
            :rows="rows"
            :rtl="direction"
            :row-style-class="rowStyleClassFn"
            :highlight-row="highlightRow"
            :line-numbers="false"
            :total-rows="total"
            :sort-options="{ enabled: false }"
            :storage-name="storageName"
            :select-options="{
                enabled: showCheckBox,
                disableSelectInfo: true, // disable the select info panel on top
                selectable: false
            }"
            :pagination-options="{
                enabled: true,
                mode: 'pages',
                perPageDropdownEnabled: false,
                dropdownAllowAll: false,
            }"
            @on-selected-rows-change="onSelectedRowsChange"
            @on-search="onSearch"
        >
            <template #loadingContent>
                <b-spinner
                    :variant="'primary'"
                    style="width: 2rem; height: 2rem"
                />
            </template>

            <template v-slot:table-row="{ row }">
                <td>{{ row.stt }}</td>
                <td v-for="column in filteredColumns" :key="column.field">
                    {{ row[column.field] }}
                </td>
            </template>

            <template #pagination-bottom>
                <b-row class="mt-25">
                    <b-col
                        md="6"
                        class="my-2 my-md-auto d-flex align-items-center"
                    >
                        <button
                            class="btn btn-icon btn-hover-linear-secondary btn-label-secondary p-25 mr-50"
                            type="button"
                            @click="openSettings"
                        >
                            <Icon
                                icon="line-md:cog-filled-loop"
                                class="sm-icon"
                            />
                        </button>
                        {{ $t('Bgrid.Sentence.ShowResultFrom') }}
                        <b class="mx-50">{{ firstItem }}</b>
                        {{ $t('Bgrid.Sentence.To') }}
                        <b class="mx-50">{{ lastItem }}</b>
                        {{ $t('Bgrid.Sentence.Intotal') }}
                        <b class="mx-50">{{ total }}</b>
                        {{ $t('Bgrid.Sentence.Record') }}
                    </b-col>

                    <b-col
                        md="6"
                        class="d-flex justify-content-center align-items-center justify-content-md-end ml-auto"
                    >
                        <b-form inline class="inline">
                            <b-form-select
                                v-model="perPage"
                                :options="options.pageOptions"
                                class="py-25 paging mr-50"
                            />
                            <b-pagination
                                v-if="total > 0"
                                v-model="page"
                                align="right"
                                :total-rows="total"
                                :per-page="perPage"
                                class="my-0"
                                pills
                            />
                        </b-form>
                    </b-col>
                </b-row>
            </template>

            <template v-for="(_, slot) of $scopedSlots" v-slot:[slot]="scope">
                <slot :name="slot" v-bind="scope" />
            </template>
        </vue-good-table>

        <!-- Nút Tìm kiếm (được "dock" vào thanh hành động) -->
        <div
            v-if="showSearchButton"
            ref="searchPortal"
            class="bt-search-portal"
            :class="{
                'bt-right-flex': dockMode === 'right',
                'bt-right-float': dockMode === 'right-float',
                'bt-next-in-row': dockMode === 'next',
            }"
            :style="{ display: dockSucceeded ? 'inline-flex' : 'none' }"
        >
            <b-button
                v-b-tooltip.hover
                :title="$t('common.button.search')"
                variant="primary"
                class="btn-square-icon"
                @click="handleSearchClick"
            >
                <Icon icon="material-symbols:refresh-rounded" class="sm-icon" />
            </b-button>
        </div>

        <!-- Fallback nổi nếu không tìm được chỗ dock -->
        <div
            v-if="showSearchButton && !dockSucceeded"
            class="bt-fallback-float"
        >
            <b-button
                v-b-tooltip.hover
                :title="$t('common.button.search')"
                variant="primary"
                class="btn-square-icon"
                @click="handleSearchClick"
            >
                <Icon icon="material-symbols:refresh-rounded" class="sm-icon" />
            </b-button>
        </div>

        <!-- Modal cấu hình cột -->
        <b-modal v-model="mdConfig" :title="$t('Bgrid.Columns')" size="lg">
            <div class="d-flex" style="flex-flow: row wrap">
                <div
                    v-for="(column, index) in lstColumns"
                    :key="index"
                    style="flex-basis: 50%; max-width: 100%; margin: 10px 0"
                >
                    <b-row style="margin: 0 0 5px 0px">{{
                        $t(column.label)
                    }}</b-row>
                    <label class="switch">
                        <input
                            :id="'column-' + index"
                            v-model="column.showColumn"
                            type="checkbox"
                        />
                        <span
                            class="slider round"
                            data-checked="✓"
                            data-unchecked="✕"
                            :for="'column-' + index"
                        ></span>
                    </label>
                </div>
            </div>
            <template #modal-footer>
                <button class="btn btn-secondary" @click="mdConfig = false">
                    {{ $t('common.button.cancel') }}
                </button>
                <button class="btn btn-primary" @click="applySettings">
                    {{ $t('common.button.save') }}
                </button>
            </template>
        </b-modal>
    </div>
</template>

<script>
import { VueGoodTable } from 'vue-good-table'
import { BSpinner } from 'bootstrap-vue'
import store from '@/store/index'
/* eslint-disable */

export default {
    name: 'BasicTable',
    components: { VueGoodTable, BSpinner },
    props: {
        columns: { type: Array, required: true },
        dataUrl: { type: String, required: true },
        searchForm: {},
        sortBy: { type: String },
        highlightRow: { type: Boolean },
        storageName: { type: String },
        initialPage: { type: Number, default: 1 },
        initialItemsPerPage: { type: Number, default: 10 },

        showSearchButton: { type: Boolean, default: true },
        showCheckBox: { type: Boolean, default: false },
        beforeSearch: { type: Function }, // optional
    },
    data() {
        return {
            mdConfig: false,
            lstColumns: [],
            page: this.initialPage,
            perPage: this.initialItemsPerPage,
            total: 0,
            isLoading: false,
            rows: [],
            selectedRows: [],
            options: {
                pageOptions: [10, 20, 50, 100],
                sortBy: '',
                sortDesc: false,
                sortDirection: 'asc',
            },
            dir: false,

            _observer: null,
            _retry: 0,
            _retryTimer: null,

            dockSucceeded: false,
            // 'right' | 'right-float' | 'next' | 'none'
            dockMode: 'none',
        }
    },
    mounted() {
        this.lstColumns = this.columns.map((col) => ({
            ...col,
            visible: col.visible === undefined ? true : col.visible,
            showColumn: col.visible === undefined ? true : col.visible,
        }))
        if (this.storageName != null) {
            const storage = JSON.parse(localStorage.getItem(this.storageName))
            if (storage != null) this.lstColumns = storage
        }
        this.$nextTick(() => {
            this.attachPortal()
            this._observer = new MutationObserver(() => this.attachPortal(true))
            this._observer.observe(document.body, {
                childList: true,
                subtree: true,
            })
            this.retryAttach()
        })
    },
    beforeDestroy() {
        if (this._observer) this._observer.disconnect()
        if (this._retryTimer) clearTimeout(this._retryTimer)
    },
    computed: {
        firstItem() {
            return (this.page - 1) * this.perPage + 1
        },
        lastItem() {
            return Math.min(this.total, this.page * this.perPage)
        },
        direction() {
            this.dir = !!store.state.appConfig.isRTL
            return this.dir
        },
        // filteredColumns() {
        //     return this.lstColumns
        //         .filter((c) => c.visible)
        //         .map((c) => ({ ...c, label: this.$t(c.label) }))
        // },
        filteredColumns() {
            const cols = Array.isArray(this.lstColumns) ? this.lstColumns : []
            return cols
                .filter((c) => c.visible)
                .map((c) => ({ ...c, label: this.$t(c.label) }))
        },
    },
    watch: {
        page() {
            this.changePagination()
        },
        perPage() {
            this.changePagination()
        },
        showSearchButton() {
            this.$nextTick(this.attachPortal)
        },
    },
    methods: {
        isRowSelectable(row) {
            return false
        },
        onSelectedRowsChange(selection) {
            // Nếu muốn lưu lại
            this.selectedRows = selection.selectedRows
        },
        openSettings() {
            this.mdConfig = true
        },
        applySettings() {
            this.lstColumns.forEach((c) => (c.visible = c.showColumn))
            if (this.storageName)
                localStorage.setItem(
                    this.storageName,
                    JSON.stringify(this.lstColumns)
                )
            this.mdConfig = false
        },
        rowStyleClassFn(row) {
            return !row.read && this.highlightRow ? 'rowhighlight' : ''
        },
        getFormData() {
            const pagination = {
                page: this.page,
                itemsPerPage: this.perPage,
                sortBy: this.sortBy,
            }
            return (
                new URLSearchParams(pagination).toString() +
                '&' +
                new URLSearchParams(this.searchForm).toString()
            )
        },
        onSearch() {
            this.loadItems()
        },
        async loadItems() {
            if (!this.page || !this.perPage) return
            this.isLoading = true
            const formData = this.getFormData()
            try {
                const response = await this.$services.get(
                    this.dataUrl + '?' + formData
                )
                const { currentPage, pageSize, totalRows, data } =
                    response.data.data
                this.rows = data.map((item, index) => ({
                    ...item,
                    stt: (currentPage - 1) * pageSize + index + 1,
                }))
                this.total = totalRows
            } catch (error) {
                console.log('error', error)
            } finally {
                this.isLoading = false
            }
        },
        refresh() {
            if (this.page === 1) this.changePagination()
            this.page = 1
        },
        changePagination() {
            this.loadItems()
            this.$emit('pagination', {
                page: this.page,
                itemsPerPage: this.perPage,
            })
        },
        async handleSearchClick() {
            if (typeof this.beforeSearch === 'function') {
                try {
                    const ok = await this.beforeSearch()
                    if (!ok) return
                } catch {
                    return
                }
            }
            this.refresh()
            this.$emit('searched')
        },

        // ===== Dock portal =====
        retryAttach() {
            if (this._retry > 20) return
            this._retryTimer = setTimeout(() => {
                this._retry++
                this.attachPortal(true)
                this.retryAttach()
            }, 120)
        },

        attachPortal(fromObserver = false) {
            const portal = this.$refs.searchPortal
            if (!portal || !this.showSearchButton) return

            // 1) Thử 2 case cũ trước
            const res = this.findActionRowContainer()
            if (res && res.el) {
                const target = res.el

                if (portal.parentElement !== target) {
                    try {
                        target.appendChild(portal)
                    } catch {}
                }
                portal.style.display = 'inline-flex'

                if (res.mode === 'right') {
                    const cs = window.getComputedStyle(target)
                    this.dockMode = cs.display.includes('flex')
                        ? 'right'
                        : 'right-float'
                } else {
                    this.dockMode = res.mode // 'next'
                }
                this.dockSucceeded = true
                return
            }

            // 2) Không tìm thấy (bảng hiếm) → bật fallback nổi
            portal.style.display = 'none'
            this.dockMode = 'none'
            this.dockSucceeded = false
        },

        /* CHỌN CONTAINER + MODE
       - Case A: Export + Refresh → { el, mode:'right' }
       - Case B: chỉ Export       → { el, mode:'next' }
       - Case C: KHÔNG Export/Refresh (có/không có Thêm) → { el, mode:'right' }
    */
        findActionRowContainer() {
            // card gần nhất
            let card = this.$el.closest('.card, .b-card, .vx-card')
            if (!card) card = this.$el.closest('[class*="card"]')
            if (!card) card = document

            const norm = (s) =>
                (s || '').replace(/\s+/g, ' ').trim().toLowerCase()
            const matchAny = (el, labels) =>
                labels.some((l) => norm(el.textContent).includes(norm(l)))

            const exportLabels = [
                'xuất excel',
                'export excel',
                'export',
                'xuất file',
                'export file',
                'xuất báo cáo',
                'in',
                'report',
                'báo cáo', // Thêm label cho nút In/Report để coi như Export
            ]
            const refreshLabels = ['làm mới', 'refresh', 'tải lại']
            const addLabels = [
                'thêm mới',
                'thêm',
                'create',
                'add new',
                'tạo mới',
                'new',
            ]

            const btns = Array.from(
                card.querySelectorAll('button, .btn, [role="button"], a.btn')
            )
            const expBtns = btns.filter((b) => matchAny(b, exportLabels)) // Bây giờ Export + In match chung
            const refBtns = btns.filter((b) => matchAny(b, refreshLabels))
            const addBtns = btns.filter((b) => matchAny(b, addLabels))

            const ancestors = (el) => {
                const arr = []
                let cur = el
                while (cur && cur !== document.body) {
                    arr.push(cur)
                    cur = cur.parentElement
                }
                return arr
            }
            const lca = (a, b) => {
                const setB = new Set(ancestors(b))
                return ancestors(a).find((n) => setB.has(n)) || null
            }

            // Case A: Có Export/In + Refresh → dock mép phải (giữ nguyên)
            if (expBtns.length && refBtns.length) {
                const el = lca(expBtns[0], refBtns[0])
                if (el) return { el, mode: 'right' }
            }

            // Case B: Chỉ Export/In (không Refresh) → dock MÉP PHẢI của container chung (thay vì 'next')
            if (expBtns.length && !refBtns.length) {
                // Tìm container lớn nhất chứa Add + Export/In (thường là div d-flex)
                let el = addBtns.length
                    ? lca(addBtns[0], expBtns[0])
                    : expBtns[0].parentElement
                el = this._getUsableContainer(el || card)
                if (el) return { el, mode: 'right' } // Luôn 'right' để nút Search ở cuối
            }

            // Case C: Không Export/In/Refresh → dock mép phải của Add (giữ nguyên)
            if (!expBtns.length && !refBtns.length) {
                let el = addBtns.length ? addBtns[0].parentElement : null
                el = this._getUsableContainer(el || card)
                return el ? { el, mode: 'right' } : null
            }

            return null
        },

        // Tìm container "block/flex" hợp lý để float/right mà KHÔNG sửa style của nó
        _getUsableContainer(start) {
            if (!start) return null
            let el = start
            while (el && el !== document.body) {
                const cs = window.getComputedStyle(el)
                const disp = cs.display || ''
                const isUsable =
                    disp.includes('flex') ||
                    disp === 'block' ||
                    disp === 'grid' ||
                    el.classList.contains('btn-group') ||
                    el.classList.contains('b-button-group')

                if (isUsable) return el
                el = el.parentElement
            }
            return start
        },
    },
}
</script>

<style lang="scss">
@import '@core/scss/vue/libs/vue-good-table.scss';

.bt-root {
    position: relative;
}

/* Nút icon */
.btn-square-icon {
    width: 40px;
    height: 40px;
    padding: 0 !important;
    border-radius: 10px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
}

/* Case A/C khi container là flex → đẩy sang phải */
.bt-right-flex {
    margin-left: auto !important;
    margin-right: 0 !important;
    align-self: center !important;
}

/* Case A/C khi container không phải flex → float bên phải, không đụng style container */
.bt-right-float {
    float: right !important;
    margin-left: 12px;
}

/* Case B: đứng cạnh Export */
.bt-next-in-row {
    margin-left: 8px;
    align-self: center !important;
}

/* Đồng bộ chiều cao nút */
.bt-search-portal .btn-square-icon {
    height: 38px !important;
    width: 38px !important;
    margin-top: -12px; /* Đẩy nút lên trên để bằng với các nút khác */
}

/* Fallback nổi (hiếm khi dùng) */
.bt-fallback-float {
    position: absolute;
    top: -46px;
    right: 12px;
    z-index: 6;
}

/* Bảng */
.paging.custom-select {
    height: 2rem;
}
table.vgt-table th {
    font-weight: 500 !important;
    color: #000 !important;
}
table.vgt-table td {
    vertical-align: middle;
    color: #333 !important;
}
.rowhighlight span {
    color: red !important;
}
.paging.custom-select {
    height: 2rem;
}
table.vgt-table th {
    font-weight: 500 !important;
    color: #000000 !important;
}
table.vgt-table td {
    vertical-align: middle;
    color: #333333 !important;
}

.rowhighlight {
    span {
        color: red !important;
    }
}

/* The switch - the box around the slider */
.switch {
    position: relative;
    display: inline-block;
    width: 60px;
    height: 34px;
}

/* Hide default HTML checkbox */
.switch input {
    opacity: 0;
    width: 0;
    height: 0;
}

/* The slider */
.slider {
    position: absolute;
    cursor: pointer;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: #ccc;
    -webkit-transition: 0.4s;
    transition: 0.4s;
}

.slider:before {
    position: absolute;
    content: '';
    height: 26px;
    width: 26px;
    left: 4px;
    bottom: 4px;
    background-color: white;
    -webkit-transition: 0.4s;
    transition: 0.4s;
}

input:checked + .slider {
    background-color: #2196f3;
}

input:focus + .slider {
    box-shadow: 0 0 1px #2196f3;
}

input:checked + .slider:before {
    -webkit-transform: translateX(26px);
    -ms-transform: translateX(26px);
    transform: translateX(26px);
}

/* Rounded sliders */
.slider.round {
    border-radius: 34px;
}

.slider.round:before {
    border-radius: 50%;
}
.setting-btn {
    border: none !important;
    border-radius: 50% !important;
    padding: 10px !important;
    font-size: 1.4rem;
    &:hover {
        color: #2196f3 !important;
        color: #fff;
        box-shadow: none !important;
    }
}
</style>
