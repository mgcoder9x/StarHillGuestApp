<template>
    <div class="condition-builder">
        <div class="input-container">
            <input
                v-model="conditionInput"
                class="condition-input"
                placeholder="Nhập điều kiện"
                type="text"
                @input="onInputChange"
                @focus="handleFocusInput"
                @keydown.enter.prevent="onEnter"
                @keydown.down.prevent="onArrowDown"
                @keydown.up.prevent="onArrowUp"
            />
            <!-- @blur="handleBlurInput($event)" -->
        </div>

        <div v-if="suggestions.properties.length > 0" class="suggestions">
            <h4>Gợi ý thuộc tính:</h4>
            <ul>
                <li
                    v-for="(property, index) in suggestions.properties"
                    :key="'property-' + index"
                    :class="{
                        'is-active': index === activeSuggestionIndex,
                        'suggestion-item': true,
                    }"
                    @click="applySuggestion(property)"
                    @mousedown.prevent="selectSuggestion(suggestion)"
                >
                    {{ property }}
                </li>
            </ul>
        </div>

        <div v-if="suggestions.operators.length > 0" class="suggestions">
            <h4>Gợi ý toán tử:</h4>
            <ul>
                <li
                    v-for="(operator, index) in suggestions.operators"
                    :key="'operator-' + index"
                    class="suggestion-item"
                    @click="applySuggestion(operator)"
                >
                    {{ operator }}
                </li>
            </ul>
        </div>

        <div v-if="suggestions.values.length > 0" class="suggestions">
            <h4>Gợi ý giá trị:</h4>
            <ul>
                <li
                    v-for="(value, index) in suggestions.values"
                    :key="'value-' + index"
                    class="suggestion-item"
                    @click="applySuggestion(value)"
                >
                    {{ value }}
                </li>
            </ul>
        </div>

        <div v-if="suggestions.conditions.length > 0" class="suggestions">
            <h4>Gợi ý điều kiện nối tiếp:</h4>
            <ul>
                <li
                    v-for="(condition, index) in suggestions.conditions"
                    :key="'condition-' + index"
                    class="suggestion-item"
                    @click="applySuggestion(condition)"
                >
                    {{ condition }}
                </li>
            </ul>
        </div>
    </div>
</template>

<script>
export default {
    data() {
        return {
            conditionInput: '', // Chuỗi đầu vào
            suggestions: {
                properties: [],
                operators: [],
                conditions: [],
                values: [],
            }, // Gợi ý hiện tại
            allProperties: ['name', 'age', 'date', 'status'], // Danh sách thuộc tính
            allOperators: ['=', '!=', '>', '<', '>=', '<='], // Danh sách toán tử
            allConditions: ['AND', 'OR'], // Danh sách điều kiện nối
            activeSuggestionIndex: -1,
            successProperty: false,
            successOperator: false,
            sucecssValue: false,
            successCondition: false,
            isTypingProperty: false,
            isTypingOperator: false,
            isTypingValue: false,
            isTypingCondition: false,
        }
    },
    methods: {
        handleFocusInput() {
            this.onInputChange()
        },
        handleBlurInput(e) {
            e.preventDefault()
            this.clearSuggest()
        },
        clearSuggest() {
            this.suggestions = {
                properties: [],
                operators: [],
                conditions: [],
                values: [],
            }
        },
        onInputChange() {
            const input = this.conditionInput.trimStart() // Remove extra spaces
            const parts = input.split(/\s+/g) // Split the input into parts
            const lastWord = parts[parts.length - 1]
            this.clearSuggest()
            const checkFirstCharactor = parts.length === 1 && lastWord !== '('
            if (lastWord === '(') {
                this.isTypingProperty = true
            } else if (checkFirstCharactor) {
                this.isTypingProperty = true
            } else if (this.isAfterCondition(parts)) {
                this.isTypingProperty = true
            }
            if (this.isTypingProperty) {
                this.suggestions.properties = this.allProperties.filter(
                    (property) =>
                        property
                            .toLowerCase()
                            .startsWith(lastWord.toLowerCase())
                )
            }
        },
        // opInputChange() {
        //     const input = this.conditionInput.trim() // Remove extra spaces
        //     const parts = input.split(/\s+/) // Split the input into parts
        //     const lastWord = parts[parts.length - 1] // Get the last word
        //     const isEndingWithSpace = this.conditionInput.endsWith(' ') // Check if input ends with a space
        //     const openParenthesesCount = (
        //         this.conditionInput.match(/\(/g) || []
        //     ).length // Count open parentheses
        //     const closeParenthesesCount = (
        //         this.conditionInput.match(/\)/g) || []
        //     ).length // Count close parentheses
        //     const isInsideGroup = openParenthesesCount > closeParenthesesCount // Check if inside a group

        //     this.clearSuggest() // Clear old suggestions

        //     if (isInsideGroup && lastWord === '(') {
        //         debugger
        //         // Suggest properties after an open parenthesis
        //         this.suggestions.properties = this.allProperties
        //     } else if (parts.length === 1 || this.isAfterCondition(parts)) {
        //         // Suggest properties at the beginning or after a condition
        //         this.suggestions.properties = this.allProperties.filter(
        //             (property) =>
        //                 property
        //                     .toLowerCase()
        //                     .startsWith(lastWord.toLowerCase())
        //         )
        //     } else if (this.isAfterProperty(parts)) {
        //         // Suggest operators immediately after a property
        //         this.suggestions.operators = this.allOperators.filter(
        //             (operator) => operator.startsWith(lastWord)
        //         )
        //     } else if (this.isAfterOperator(parts)) {
        //         // Suggest values immediately after an operator
        //         if (isEndingWithSpace) {
        //             this.suggestions.conditions = this.allConditions // Suggest connecting conditions if value is complete
        //         } else {
        //             this.suggestions.values = this.getValueSuggestions(lastWord)
        //         }
        //     } else if (this.isAfterValue(parts)) {
        //         // Suggest connecting conditions (AND, OR) or closing parenthesis
        //         this.suggestions.conditions = this.allConditions.filter(
        //             (condition) =>
        //                 condition
        //                     .toUpperCase()
        //                     .startsWith(lastWord.toUpperCase())
        //         )
        //         if (isInsideGroup) {
        //             this.suggestions.conditions.push(')') // Add closing parenthesis as a suggestion
        //         }
        //     } else if (lastWord === ')') {
        //         // Suggest conditions (AND, OR) after a closing parenthesis
        //         this.suggestions.conditions = this.allConditions.filter(
        //             (condition) =>
        //                 condition
        //                     .toUpperCase()
        //                     .startsWith(lastWord.toUpperCase())
        //         )
        //     }
        // },
        // onInputChange() {
        //     const input = this.conditionInput.trim() // Remove extra spaces
        //     const isEndingWithSpace = input.endsWith(' ') // Check if input ends with space
        //     const parts = input.split(/\s+/) // Split the input into parts
        //     const lastWord = parts[parts.length - 1] // Get the last word
        //     const openParenthesesCount = (input.match(/\(/g) || []).length // Count open parentheses
        //     const closeParenthesesCount = (input.match(/\)/g) || []).length // Count close parentheses
        //     const isInsideGroup = openParenthesesCount > closeParenthesesCount // Check if inside a group

        //     // Clear previous suggestions
        //     this.clearSuggest()

        //     // If input is empty or starts a new condition
        //     if (!input || (isEndingWithSpace && !lastWord)) {
        //         this.suggestions.properties = this.allProperties // Suggest properties
        //     }
        //     // If input ends with an open parenthesis
        //     else if (
        //         lastWord === '(' ||
        //         (isEndingWithSpace && parts[parts.length - 2] === '(')
        //     ) {
        //         this.suggestions.properties = this.allProperties // Suggest properties
        //     }
        //     // If input ends with a property
        //     else if (this.allProperties.includes(lastWord)) {
        //         this.suggestions.operators = this.allOperators // Suggest operators
        //     }
        //     // If input ends with an operator
        //     else if (this.allOperators.includes(lastWord)) {
        //         if (isEndingWithSpace) {
        //             this.suggestions.values = this.getValueSuggestions('') // Suggest all values
        //         } else {
        //             this.suggestions.values = this.getValueSuggestions(lastWord) // Suggest values matching lastWord
        //         }
        //     }
        //     // If input ends with a value
        //     else if (this.getValueSuggestions(lastWord).length > 0) {
        //         if (isEndingWithSpace) {
        //             this.suggestions.conditions = [...this.allConditions] // Suggest conditions (AND, OR)
        //             if (isInsideGroup) {
        //                 this.suggestions.conditions.push(')') // Add closing parenthesis if inside a group
        //             }
        //         }
        //     }
        //     // If input ends with a closing parenthesis
        //     else if (lastWord === ')') {
        //         this.suggestions.conditions = this.allConditions // Suggest conditions (AND, OR)
        //     }
        //     // Default to suggesting properties
        //     else {
        //         this.suggestions.properties = this.allProperties.filter(
        //             (property) =>
        //                 property
        //                     .toLowerCase()
        //                     .startsWith(lastWord.toLowerCase())
        //         )
        //     }
        // },

        applySuggestion(suggestion) {
            const input = this.conditionInput.trim()
            const parts = input.split(/\s+/) // Tách input thành các phần
            // const lastSpace = this.conditionInput.endsWith(' ') ? '' : ' '
            parts.pop() // Phần cuối cùng đang nhập
            parts.push(suggestion)
            this.conditionInput = `${parts.join(' ')} `
            // this.suggestions = [] // Reset gợi ý
            this.clearSuggest()
        },
        isAfterCondition(parts) {
            // Kiểm tra xem gợi ý tiếp theo có phải sau điều kiện nối không
            const lastCondition = parts[parts.length - 2]?.toUpperCase()
            return this.allConditions.includes(lastCondition)
        },
        isAfterProperty(parts) {
            // Kiểm tra gợi ý tiếp theo sau thuộc tính
            return this.allProperties.includes(
                parts[parts.length - 2]?.toLowerCase()
            )
        },
        isAfterOperator(parts) {
            // Kiểm tra gợi ý tiếp theo sau toán tử
            const operator = parts[parts.length - 2]
            return this.allOperators.includes(operator)
        },
        isAfterValue(parts) {
            // Kiểm tra gợi ý tiếp theo sau giá trị
            return (
                parts.length >= 3 && this.isValueValid(parts[parts.length - 2])
            )
        },
        isValueValid(value) {
            // Kiểm tra giá trị hợp lệ (số, chuỗi)
            return (
                // eslint-disable-next-line no-restricted-globals
                !isNaN(Number(value)) || // Giá trị số
                (value.startsWith("'") && value.endsWith("'")) // Chuỗi hợp lệ
            )
        },
        getValueSuggestions(lastInput) {
            // Gợi ý giá trị dựa trên đầu vào
            // eslint-disable-next-line no-restricted-globals
            if (!isNaN(lastInput)) {
                return [lastInput] // Gợi ý số
                // eslint-disable-next-line no-else-return
            } else if (lastInput.startsWith("'") && lastInput.endsWith("'")) {
                return [lastInput] // Gợi ý chuỗi
            } else {
                return ["'Value'"] // Gợi ý mặc định
            }
        },
        onEnter() {
            if (this.activeSuggestionIndex >= 0) {
                this.selectSuggestion(
                    this.suggestions.properties[this.activeSuggestionIndex]
                )
            }
        },
        selectSuggestion(suggestion) {
            const words = this.conditionInput.split(' ')
            words.pop()
            this.conditionInput = `${words.join(' ')} ${suggestion} `
            // this.suggestions = []
        },
        onArrowDown() {
            debugger
            if (
                this.activeSuggestionIndex <
                this.suggestions.properties.length - 1
            ) {
                // eslint-disable-next-line no-plusplus
                this.activeSuggestionIndex++
            }
        },
        onArrowUp() {
            if (this.activeSuggestionIndex > 0) {
                // eslint-disable-next-line no-plusplus
                this.activeSuggestionIndex--
            }
        },
    },
}
</script>

<style>
.condition-builder {
    max-width: 600px;
    margin: 20px auto;
    padding: 20px;
    border: 1px solid #ccc;
    border-radius: 5px;
    background-color: #f9f9f9;
}

.input-container {
    margin-bottom: 15px;
    display: flex;
    gap: 10px;
}

.condition-input {
    width: 80%;
    padding: 8px;
    font-size: 16px;
    border: 1px solid #ccc;
    border-radius: 5px;
}

.add-condition-btn {
    padding: 8px 16px;
    font-size: 16px;
    background-color: #4caf50;
    color: white;
    border: none;
    border-radius: 5px;
    cursor: pointer;
}

.add-condition-btn:hover {
    background-color: #45a049;
}

.suggestions {
    margin-top: 10px;
}

.suggestion-item {
    padding: 8px;
    margin: 5px 0;
    background-color: #e9e9e9;
    cursor: pointer;
    border-radius: 3px;
    transition: background-color 0.2s;
}

.suggestion-item:hover {
    background-color: #d4d4d4;
}

.final-condition {
    margin-top: 20px;
    padding: 10px;
    border: 1px solid #4caf50;
    background-color: #e7f7e7;
    border-radius: 5px;
}

.conditions-list {
    margin-top: 20px;
}

.conditions-list ul {
    list-style-type: none;
    list-style: none;
    padding: 0;
}

.conditions-list li {
    list-style: none;
    background-color: #f0f0f0;
    padding: 8px;
    margin-bottom: 5px;
    border-radius: 5px;
}
.is-active {
    background-color: #f0f0f0;
}
</style>
