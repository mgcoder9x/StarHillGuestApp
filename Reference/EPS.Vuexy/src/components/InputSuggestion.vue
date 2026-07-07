<template>
    <div class="input-suggestion">
        <b-form-textarea
            v-model="localValue"
            autocomplete="off"
            type="text"
            v-bind="fieldProps"
            style="resize: none"
            :disabled="fieldProps.disabled"
            @keydown.down.prevent="navigateSuggestion('down')"
            @keydown.up.prevent="navigateSuggestion('up')"
            @keydown.enter.prevent="selectSuggestion"
            @input="$emit('input', localValue)"
            @focus="
                () => {
                    $emit('focus', localValue)
                    isShowSuggest = true
                }
            "
            @blur="handleBlurInput"
            @keydown.space="$emit('keydown:space', $event)"
            @keydown.backspace="$emit('keydown:backspace', $event)"
        />
        <ul v-show="isShowSuggest" class="suggestions">
            <li
                v-for="(suggestion, index) in suggestions"
                ref="suggestions"
                :key="index"
                :class="{ active: index === activeSuggestionIndex }"
                @mousedown.prevent="selectSuggestion(suggestion)"
                @change="selectSuggestion(suggestion)"
            >
                {{ suggestion }}
            </li>
        </ul>
    </div>
</template>

<script>
export default {
    props: {
        value: {
            type: [String, Number],
            default: '',
        },
        suggestions: {
            type: Array,
            default: () => [],
        },
        fieldProps: {
            type: Object,
            default: () => ({}),
        },
    },
    data() {
        return {
            localValue: this.value,
            activeSuggestionIndex: -1,
            isShowSuggest: false,
        }
    },
    watch: {
        value(newValue) {
            this.localValue = newValue
        },
    },
    methods: {
        handleBlurInput() {
            setTimeout(() => {
                this.isShowSuggest = false
            }, 100)
        },
        navigateSuggestion(direction) {
            if (direction === 'down') {
                this.activeSuggestionIndex =
                    (this.activeSuggestionIndex + 1) % this.suggestions.length
            } else if (direction === 'up') {
                this.activeSuggestionIndex =
                    (this.activeSuggestionIndex - 1 + this.suggestions.length) %
                    this.suggestions.length
            }
            this.scrollToActiveSuggestion()
        },
        selectSuggestion(suggestion) {
            debugger
            this.$emit('select', suggestion, this.activeSuggestionIndex)
            this.activeSuggestionIndex = -1
        },
        scrollToActiveSuggestion() {
            this.$nextTick(() => {
                const { suggestions } = this.$refs
                const activeItem = suggestions[this.activeSuggestionIndex]
                if (activeItem) {
                    activeItem.scrollIntoView({
                        behavior: 'smooth',
                        block: 'nearest',
                    })
                }
            })
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

.input-suggestion textarea {
    width: 100%;
    padding: 10px;
    font-size: 16px;
    border: 1px solid #ddd;
    border-radius: 4px;
    outline: none;
}
.input-suggestion textarea:focus {
    border: 1px solid #045da5;
}
.dark-layout {
    .suggestions {
        --bg-color: #161d31;
        background-color: var(--bg-color);
    }
}
.suggestions {
    display: block;
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
