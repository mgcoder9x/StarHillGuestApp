<template>
    <input-suggestion
        v-model="inputValue"
        :suggestions="filteredSuggestions"
        :field-props="fieldProps"
        @input="updateSuggestions"
        @focus="updateSuggestions"
        @select="selectSuggestion"
    />
</template>

<script>
/* eslint-disable */
import InputSuggestion from '@/components/InputSuggestion.vue'
import { lookupService } from '@/services'

export default {
    components: { InputSuggestion },
    props: {
        value: {
            type: String,
            default: '',
        },
        fieldProps: {
            type: Object,
            default: () => ({}),
        },
        eventType: {
            type: String,
            default: () => null,
        },
    },
    data() {
        return {
            inputValue: this.value,
            suggestions: [],
            filteredSuggestions: [],
            showSuggestions: false,
        }
    },
    watch: {
        eventType() {
            this.getProperties()
        },
        value(newValue) {
            // Đồng bộ inputValue khi prop value thay đổi
            this.inputValue = newValue
        },
        inputValue(newValue) {
            // Emit sự kiện input khi inputValue thay đổi
            this.$emit('input', newValue)
        },
    },
    async created() {
        if (this.eventType) this.getProperties()
    },
    methods: {
        async getProperties() {
            const data = await lookupService.getPropertiesClass(this.eventType)
            if (data && data.length > 0) {
                this.suggestions = data.map((x) => x.value)
            }
        },
        updateSuggestions() {
            const input = this.inputValue.trim() // Loại bỏ khoảng trắng thừa

            // Kiểm tra nếu chuỗi trống hoặc kết thúc bằng '}'
            if (!input || input.endsWith('}')) {
                this.showSuggestions = false // Ẩn gợi ý khi chuỗi trống hoặc kết thúc bằng '}'
                this.filteredSuggestions = []
                return
            }

            // Kiểm tra nếu chuỗi chứa ký tự `{`
            if (input.includes('{')) {
                const parts = input.split('{') // Tách chuỗi tại `{`
                const prefix = parts[parts.length - 1].trim() // Lấy phần sau `{`, loại bỏ khoảng trắng

                // Hiển thị gợi ý liên tục khi có ký tự `{`
                this.filteredSuggestions = prefix
                    ? this.suggestions.filter((suggestion) =>
                          suggestion
                              .toLowerCase()
                              .startsWith(prefix.toLowerCase())
                      )
                    : this.suggestions // Nếu không có ký tự sau `{`, hiển thị tất cả gợi ý

                this.showSuggestions = true // Hiển thị gợi ý
            } else {
                // Nếu không có `{`, lọc gợi ý theo ký tự nhập vào
                this.filteredSuggestions = this.suggestions.filter(
                    (suggestion) =>
                        suggestion.toLowerCase().startsWith(input.toLowerCase())
                )

                this.showSuggestions = this.filteredSuggestions.length > 0 // Hiển thị gợi ý nếu có kết quả
            }
        },
        selectSuggestion(suggestion = null, index = -1) {
            debugger
            // Kiểm tra nếu `suggestion` là một đối tượng sự kiện, đặt lại giá trị
            if (suggestion instanceof Event) {
                suggestion = null
            }

            if (!suggestion) {
                suggestion = this.filteredSuggestions[index] || null
            }

            if (!suggestion) {
                this.filteredSuggestions = []
                return
            }

            const cursorPosition = this.inputValue.length // Vị trí con trỏ
            const tokens = this.inputValue.slice(0, cursorPosition).split(/\s+/) // Chia input thành các token
            const lastToken = tokens[tokens.length - 1] // Lấy token cuối cùng

            // Kiểm tra nếu token cuối cùng bắt đầu với dấu `{`
            if (lastToken.startsWith('{') && !lastToken.endsWith('}')) {
                // Nếu token bắt đầu với `{`, thay thế phần sau `{` với gợi ý
                tokens[tokens.length - 1] = `{${suggestion}` // Thay thế token cuối bằng gợi ý
            } else if (lastToken.endsWith('}')) {
                // Nếu token kết thúc bằng `}`, thay thế gợi ý vào trước dấu `}`
                tokens[tokens.length - 1] = `${suggestion}}` // Thay thế token cuối bằng gợi ý
            } else {
                // Nếu không có dấu `{`, chỉ thay thế token cuối cùng
                tokens[tokens.length - 1] = suggestion // Thay thế token cuối cùng bằng gợi ý
            }

            this.inputValue = tokens.join(' ') // Gán lại giá trị input
            this.filteredSuggestions = [] // Xóa danh sách gợi ý
            this.updateSuggestions() // Cập nhật gợi ý
            this.$emit('input', this.inputValue)
        },
    },
}
</script>

<style scoped lang="scss">
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
.input-suggestion input:focus {
    border: 1px solid #045da5;
    + .suggestions {
        display: block;
    }
}
.dark-layout {
    .suggestions {
        --bg-color: #161d31;
        background-color: var(--bg-color);
    }
}
.suggestions {
    display: none;

    list-style: none;
    border-radius: 4px;
    max-height: 150px;
    overflow-y: auto;
    position: absolute;
    background-color: #ffffff;
    z-index: 10;
    width: 100%;
    padding: 0;
    margin: 5px 0 0 0;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.suggestions li {
    padding: 10px;
    cursor: pointer;
    font-weight: bold;
}
.suggestions li:hover {
    background: #045da5;
    color: white;
}
.suggestions li.active {
    background: #045da5;
    color: white;
}
</style>
