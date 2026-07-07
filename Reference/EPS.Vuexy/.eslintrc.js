module.exports = {
    root: true,
    env: {
        node: true,
        browser: true,
        es2021: true,
    },
    extends: [
        'plugin:vue/recommended',
        '@vue/airbnb',
        'plugin:prettier/recommended',
    ],
    parserOptions: {
        parser: 'babel-eslint',
        ecmaVersion: 12,
        sourceType: 'module',
    },
    rules: {
        'prettier/prettier': [
            'error',
            {
                singleQuote: true,
                semi: false,
                trailingComma: 'es5',
                tabWidth: 4,
                endOfLine: 'auto',
            },
        ],
        'no-console': process.env.NODE_ENV === 'production' ? 'error' : 'off',
        'no-debugger': process.env.NODE_ENV === 'production' ? 'error' : 'off',
        semi: ['error', 'never'],
        'max-len': 'off',
        'linebreak-style': 'off',
        camelcase: [
            'error',
            {
                properties: 'never',
                ignoreDestructuring: true,
                ignoreImports: true,
            },
        ],
        'arrow-parens': ['error', 'always'],
        'vue/multiline-html-element-content-newline': 'off',
        'eol-last': 'off',
        'no-multiple-empty-lines': ['error', { max: 1, maxEOF: 0 }],
        'comma-dangle': [
            'error',
            {
                arrays: 'only-multiline',
                objects: 'only-multiline',
                imports: 'only-multiline',
                exports: 'only-multiline',
                functions: 'never',
            },
        ],
        'no-plusplus': 'off',
    },
}
