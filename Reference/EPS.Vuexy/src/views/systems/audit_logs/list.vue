<template>
    <div>
        <validation-observer ref="rules">
            <!-- Filters -->
            <b-card no-body>
                <b-card-body>
                    <b-form @submit.prevent="search">
                        <b-row>
                            <!-- FromDate (validate theo mẫu chung) -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'System.AuditLogs.FormSearch.DateTimeFrom'
                                        )
                                    "
                                    label-for="h-searchForm-dateFrom"
                                    label-cols-md="3"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        :rules="
                                            `fromDate:` +
                                            searchForm.filterDateTo
                                        "
                                        name="FromDate"
                                    >
                                        <date-picker
                                            id="h-searchForm-dateFrom"
                                            v-model="searchForm.filterDateFrom"
                                            type="datetime"
                                            :locale="currentLocale"
                                            format="DD-MM-YYYY HH:mm:ss"
                                            value-type="YYYY-MM-DD HH:mm:ss"
                                            style="width: 100%"
                                            @change="search"
                                        />
                                        <small class="text-danger">{{
                                            errors[0]
                                        }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>

                            <!-- Username -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'System.AuditLogs.FormSearch.Username'
                                        )
                                    "
                                    label-for="h-searchForm-username"
                                    label-cols-md="3"
                                >
                                    <b-form-input
                                        id="h-searchForm-username"
                                        v-model.trim="searchForm.filterUsername"
                                        type="text"
                                        @input="onUsernameInput"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- EventType -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'System.AuditLogs.FormSearch.EventType'
                                        )
                                    "
                                    label-for="h-searchForm-eventType"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        id="h-searchForm-eventType"
                                        v-model="searchForm.filterEventType"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        :options="eventTypeOptions"
                                        label="text"
                                        :reduce="(o) => o.id"
                                        :clearable="true"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <b-row>
                            <!-- ToDate -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'System.AuditLogs.FormSearch.DateTimeTo'
                                        )
                                    "
                                    label-for="h-searchForm-dateTo"
                                    label-cols-md="3"
                                >
                                    <date-picker
                                        id="h-searchForm-dateTo"
                                        v-model="searchForm.filterDateTo"
                                        type="datetime"
                                        :locale="currentLocale"
                                        format="DD-MM-YYYY HH:mm:ss"
                                        value-type="YYYY-MM-DD HH:mm:ss"
                                        style="width: 100%"
                                        @change="search"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- IP Address -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'System.AuditLogs.FormSearch.IpAddress'
                                        )
                                    "
                                    label-for="h-searchForm-ip"
                                    label-cols-md="3"
                                >
                                    <b-form-input
                                        id="h-searchForm-ip"
                                        v-model.trim="
                                            searchForm.filterIpAddress
                                        "
                                        type="text"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <!-- Hidden submit để Enter -->
                        <button
                            type="submit"
                            style="
                                position: absolute;
                                width: 1px;
                                height: 1px;
                                overflow: hidden;
                                clip: rect(0, 0, 0, 0);
                            "
                        >
                            {{ $t('common.button.search') }}
                        </button>
                    </b-form>
                </b-card-body>
            </b-card>
        </validation-observer>

        <b-card title="">
            <!-- table -->
            <BasicTable
                ref="auditLogTable"
                :columns="columns"
                data-url="/audit-logs"
                :search-form="searchForm"
                storage-name="auditLogTable"
            >
                <template #table-row="{ column, row }">
                    <!-- Column: EventType -->
                    <span v-if="column.field === 'eventType'">
                        {{ $t(`System.AuditLogs.EventType.${row.eventType}`) }}
                    </span>

                    <!-- Column: Action -->
                    <span v-else-if="column.field === 'action'">
                        <div class="center-icon text-nowrap">
                            <b-button
                                v-if="authorize(['ViewAuditLogs'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('common.button.detail')"
                                @click="openJson(row)"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                        </div>
                    </span>
                </template>
            </BasicTable>
        </b-card>

        <!-- Modal JSON (gọn) -->
        <b-modal
            v-model="jsonModal"
            title="Audit Data"
            centered
            size="lg"
            scrollable
            hide-footer
            dialog-class="audit-modal"
        >
            <div class="json-container">
                <pre class="json-viewer mb-0"><code>{{ jsonText }}</code></pre>
            </div>
        </b-modal>
    </div>
</template>

<script>
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'
import moment from 'moment'
import { ValidationObserver, ValidationProvider, extend } from 'vee-validate'

// Rule: FromDate <= ToDate (đúng value-type 'YYYY-MM-DD HH:mm:ss')
// extend('fromDate', {
//     params: ['to'],
//     message: 'Thời gian từ ngày phải nhỏ hơn thời gian đến ngày',
//     validate(value, { to }) {
//         if (!value || !to) return true
//         const f = moment(value, 'YYYY-MM-DD HH:mm:ss', true)
//         const t = moment(to, 'YYYY-MM-DD HH:mm:ss', true)
//         if (!f.isValid() || !t.isValid()) return true
//         return f.isSameOrBefore(t)
//     },
// })

export default {
    mixins: [authorizationMixin],
    components: {
        ValidationObserver,
        ValidationProvider,
    },
    data() {
        return {
            jsonModal: false,
            jsonText: '',

            // dữ liệu cho EventType
            eventTypeKeys: [],
            eventTypeOptions: [],

            // form gửi lên API
            searchForm: {
                filterDateFrom: null,
                filterDateTo: null,
                filterUsername: null,
                filterEventType: null,
                filterIpAddress: null,
            },

            // bảng
            columns: [
                {
                    label: 'System.AuditLogs.Field.DateTime',
                    field: 'createdUtc',
                    formatFn(value) {
                        return moment(value).format('DD/MM/YY HH:mm:ss')
                    },
                },
                {
                    label: 'System.AuditLogs.Field.EventType',
                    field: 'eventType',
                },
                { label: 'System.AuditLogs.Field.Username', field: 'username' },
                {
                    label: 'System.AuditLogs.Field.IpAddress',
                    field: 'ipAddress',
                },
                {
                    label: 'System.AuditLogs.Field.DurationMs',
                    field: 'durationMs',
                },
                { label: 'System.AuditLogs.Field.Action', field: 'action' },
            ],
        }
    },

    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },

    async created() {
        const now = moment()
        this.searchForm.filterDateFrom = now
            .clone()
            .startOf('day')
            .format('YYYY-MM-DD HH:mm:ss')
        this.searchForm.filterDateTo = now
            .clone()
            .seconds(0)
            .format('YYYY-MM-DD HH:mm:ss')
        await this.loadEventTypes()
    },

    watch: {
        // đổi ngôn ngữ -> remap label options EventType
        '$i18n.locale'() {
            this.remapEventTypeOptions()
        },
    },

    methods: {
        // Validate theo mẫu chung trước khi refresh
        search() {
            this.$refs.rules.validate().then((ok) => {
                if (ok) this.$refs.auditLogTable.refresh()
            })
        },

        // Username chỉ cho A–Z a–z 0–9 và - _ @
        onUsernameInput() {
            const safe = (this.searchForm.filterUsername || '').replace(
                /[^A-Za-z0-9@_-]/g,
                ''
            )
            if (safe !== this.searchForm.filterUsername) {
                this.searchForm.filterUsername = safe
            }
            this.search()
        },

        async loadEventTypes() {
            const res = await this.$services.get('/audit-logs/event-types')
            this.eventTypeKeys = res.data?.data || []
            this.remapEventTypeOptions()
        },

        remapEventTypeOptions() {
            this.eventTypeOptions = this.eventTypeKeys.map((k) => ({
                id: k,
                text: this.$t(`System.AuditLogs.EventType.${k}`),
            }))
        },

        // JSON modal
        openJson(row) {
            this.jsonModal = true
            try {
                const raw = row?.data
                if (raw == null || raw === '') {
                    this.jsonText = '(no data)'
                    return
                }
                const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw
                this.jsonText = JSON.stringify(parsed, null, 2)
            } catch (e) {
                this.jsonText =
                    typeof row?.data === 'string' ? row.data : String(row?.data)
            }
        },
    },
}
</script>

<style lang="scss">
.audit-modal {
    max-width: 820px;
    width: 100%;
}
@media (max-width: 992px) {
    .audit-modal {
        max-width: calc(100% - 2rem);
        margin: 0.5rem auto;
    }
}
.json-container {
    max-height: 60vh;
    overflow: auto;
    padding: 0.75rem;
}
.json-viewer {
    background: #fff;
    color: #212529;
    border: 1px solid rgba(0, 0, 0, 0.1);
    border-radius: 0.25rem;
    padding: 0.75rem;
    font-size: 13px;
    line-height: 1.4;
    white-space: pre;
    overflow: auto;
}
</style>
