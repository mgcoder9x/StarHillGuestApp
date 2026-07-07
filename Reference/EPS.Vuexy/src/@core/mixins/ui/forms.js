import useJwt from '@/auth/jwt/useJwt'
// We haven't added icon's computed property because it makes this mixin coupled with UI
export const togglePasswordVisibility = {
    data() {
        return {
            passwordFieldType: 'password',
        }
    },
    methods: {
        togglePasswordVisibility() {
            this.passwordFieldType =
                this.passwordFieldType === 'password' ? 'text' : 'password'
        },
    },
}

export const authorizationMixin = {
    data() {
        return {
            // eslint-disable-next-line
            currentPrivileges: useJwt.getUserData().privileges,
        }
    },
    methods: {
        authorize(requiredPrivileges) {
            return this.currentPrivileges.some(
                (r) => requiredPrivileges.indexOf(r) >= 0
            )
        },
    },
}

export const _ = null
