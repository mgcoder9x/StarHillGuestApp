<template>
    <input-suggestion
        v-model="inputValue"
        :suggestions="filteredSuggestions"
        :field-props="{...fieldProps, disabled}"
        @input="updateSuggestions"
        @focus="updateSuggestions"
        @keydown.space="handleSpaceInput"
        @keydown.backspace="handleBackspace"
        @select="selectSuggestion"
    />
</template>

<script>
import { lookupService } from '@/services'
import helper from '@/utils/utils'
import NotiDataHelper from '@/utils/operator'
import InputSuggestion from './InputSuggestion.vue'

export default {
    components: { InputSuggestion },
    props: {
        eventType: { type: String, required: true },
        data: { type: Array, default: () => [] },
        fieldProps: { type: Object, default: () => ({}) },
        disabled: { type: Boolean, default: false },
        // Cho phép gõ tham số có tiền tố @
        allowAtParam: { type: Boolean, default: true },
    },
    data() {
        return {
            propertiesList: [],
            operatorList: NotiDataHelper.operators.map((x) => x.id),
            conditionList: NotiDataHelper.conditions.map((x) => x.id),
            inputValue: '',
            suggestions: [],
            filteredSuggestions: [],
            activeSuggestionIndex: -1,
            suggestionType: 'property',
        }
    },
    watch: {
        eventType() {
            this.getProperties()
        },
    },
    created() {
        this.getProperties()
        if (this.data && this.data.length > 0 && this.data[0]) {
            this.inputValue = helper.generateConditionString(this.data[0])
        }
    },
    methods: {
        /* ===== Helpers cho @ & NULL ===== */
        _stripAt(token) {
            if (!this.allowAtParam) return token || ''
            return (token || '').replace(/^@/, '')
        },
        _normalizeNull(expr) {
            return (expr || '')
                .replace(/\s*!=\s*null\b/gi, ' IS NOT NULL')
                .replace(/\s*=\s*null\b/gi, ' IS NULL')
        },
        // Chuẩn hoá để PARSE: chuẩn hoá NULL + bỏ @ (chỉ cho parser)
        _normalizeForParse(expr) {
            let s = this._normalizeNull(expr)
            if (this.allowAtParam) s = s.replace(/@([A-Za-z_]\w*)/g, '$1')
            return s
        },

        /* ===== API dùng bởi form cha ===== */
        async getProperties() {
            this.propertiesList.length = 0
            const data = await lookupService.getPropertiesClass(this.eventType)
            if (data && data.length > 0) {
                this.propertiesList = data.map((x) => x.value)
            }
        },

        // ✅ Luôn resolve – không reject nữa để không chặn nút “Ghi lại”
        getQuery() {
            return new Promise((resolve) => {
                const result = this.getDataSafe()
                resolve({ data: result })
            })
        },

        // Thử parse; nếu lỗi → fallback object { expression: '...' }
        getDataSafe() {
            if (!this.inputValue) return null

            const raw = this.inputValue
            const normalizedForParse = this._normalizeForParse(raw)
            const normalizedForStore = this._normalizeNull(raw) // giữ nguyên @ cho người dùng

            try {
                // Thử dùng parser cũ (an toàn vì đã normalize)
                const parsed = helper.parseCondition(normalizedForParse)
                return parsed
            } catch (e) {
                // Fallback: trả về biểu thức dạng chuỗi để backend lưu/diễn giải
                return { expression: normalizedForStore }
            }
        },

        // Kiểm tra nhẹ (ngoặc cân bằng). Không fail để tránh chặn submit.
        validateQuery() {
            const s = this._normalizeForParse(this.inputValue || '')
            if (!s.trim()) return true
            let bal = 0
            for (const ch of s) {
                if (ch === '(') bal++
                else if (ch === ')') bal--
                if (bal < 0) return true // đừng chặn, để backend/QA xử lý
            }
            return true
        },

        /* ===== Suggestion (hỗ trợ @) ===== */
        updateSuggestions() {
            const tokens = this.inputValue.trim().split(/\s+/)
            const lastToken = tokens[tokens.length - 1] || ''
            const beforeLastToken =
                tokens.length > 1 ? tokens[tokens.length - 2] || '' : lastToken

            this.suggestionType = this.determineSuggestionType(
                lastToken,
                beforeLastToken
            )

            this.suggestions = this.getSuggestions(
                lastToken,
                this.suggestionType
            )
            this.filteredSuggestions = this.filterSuggestions(
                lastToken,
                this.suggestionType
            )
        },

        determineSuggestionType(lastToken, beforeLastToken) {
            const lastNorm = this._stripAt(lastToken)

            if (
                this.inputValue.trim() === '' ||
                this.inputValue.trim().endsWith('(')
            ) {
                return 'property'
            }

            const isLastTokenProperty = this.isInList(
                lastNorm,
                this.propertiesList
            )
            const isCondition = this.isInList(
                this._stripAt(beforeLastToken),
                this.operatorList
            )

            if (this.isInList(lastNorm, this.conditionList)) return 'property'
            if (isLastTokenProperty) return 'operator'
            if (this.startsWithAny(lastNorm, this.propertiesList))
                return 'property'
            if (this.startsWithAny(lastNorm, this.operatorList))
                return 'operator'
            if (this.isInList(lastNorm, this.operatorList)) return 'value'
            if (/^@?\w+$/.test(lastToken) && isCondition) return 'condition'
            if (lastToken.endsWith(')')) return 'condition'
            return null
        },

        getSuggestions(token, suggestionType) {
            const suggestionMap = {
                property: this.propertiesList,
                operator: this.operatorList,
                value: this.conditionList,
                condition: this.conditionList,
            }
            return suggestionMap[suggestionType] || []
        },

        filterSuggestions(token, suggestionType) {
            const t = this._stripAt(token)
            if (!t) return this.getSuggestionsByType(suggestionType)

            const filterLogic = {
                property: (list) => this.filterListByToken(list, t),
                operator: (list) => this.filterListByToken(list, t),
                value: (list) => this.filterListByToken(list, t),
                condition: (list) =>
                    list.filter((c) =>
                        c.toLowerCase().includes(t.toLowerCase())
                    ),
            }

            const list = this.getSuggestionsByType(suggestionType)
            const out = filterLogic[suggestionType]
                ? filterLogic[suggestionType](list)
                : list
            return out && out.length ? out : list
        },

        getSuggestionsByType(type) {
            const suggestionMap = {
                property: this.propertiesList,
                operator: this.operatorList,
                value: this.conditionList,
                condition: this.conditionList,
            }
            return suggestionMap[type] || []
        },

        isInList(token, list) {
            const t = this._stripAt(token)
            return list.some(
                (item) => t.toLowerCase() === item.toString().toLowerCase()
            )
        },

        startsWithAny(token, list) {
            const t = this._stripAt(token)
            return list.some((item) =>
                item.toString().toLowerCase().startsWith(t.toLowerCase())
            )
        },

        filterListByToken(list, token) {
            const t = this._stripAt(token)
            return list.filter((item) =>
                item.toString().toLowerCase().startsWith(t.toLowerCase())
            )
        },

        /* ===== Select & key handlers ===== */
        selectSuggestion(suggestion = null, index = -1) {
            if (suggestion instanceof Event) suggestion = null
            if (!suggestion)
                suggestion = this.filteredSuggestions[index] || null
            if (!suggestion) {
                this.suggestions = []
                this.filteredSuggestions = []
                return
            }

            const cursorPosition = this.inputValue.length
            const tokens = this.inputValue.slice(0, cursorPosition).split(/\s+/)
            const lastToken = tokens[tokens.length - 1]

            // Nếu đang gõ @xxx thì giữ @
            if (lastToken) {
                const hadAt = /^@/.test(lastToken)
                tokens[tokens.length - 1] = hadAt
                    ? '@' + suggestion
                    : suggestion
            } else {
                tokens.push(suggestion)
            }

            this.inputValue = tokens.join(' ') + ' '
            this.suggestions = []
            this.filteredSuggestions = []
            this.updateSuggestions()
        },

        handleSpaceInput() {
            if (this.suggestionType === 'condition') {
                this.inputValue += ' '
                this.updateSuggestions()
            }
        },
        handleBackspace() {
            if (this.inputValue.trim().endsWith(' ')) {
                this.updateSuggestions()
            }
        },
    },
}
</script>

<style scoped>
.input-suggestion {
    position: relative;
    width: 100%;
    max-width: 600px;
}
.input-suggestion input {
    width: 100%;
    padding: 10px;
    font-size: 16px;
    border: 1px solid #ddd;
    border-radius: 4px;
    outline: none;
}
.suggestions {
    list-style: none;
    border: 1px solid #ddd;
    border-radius: 4px;
    max-height: 150px;
    overflow-y: auto;
    position: absolute;
    background: white;
    z-index: 10;
    width: 100%;
    padding: 0;
    margin: 5px 0 0 0;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}
.suggestions li {
    padding: 10px;
    cursor: pointer;
}
.suggestions li.active {
    background: #007bff;
    color: white;
}
</style>
