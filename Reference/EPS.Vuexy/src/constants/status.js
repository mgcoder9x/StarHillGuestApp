const CARGO_STATUS = {
    EMPTY: 0,
    HALF: 1,
    FULL: 2,
}
const REGISTER_STATUS = {
    REGISTERED: true,
    UNREGISTERED: false,
}

const cargoStatus = [
    { key: CARGO_STATUS.EMPTY, value: 'cargoStatus.empty' },
    { key: CARGO_STATUS.HALF, value: 'cargoStatus.half' },
    { key: CARGO_STATUS.FULL, value: 'cargoStatus.full' },
]

const registerStatus = [
    { id: REGISTER_STATUS.REGISTERED, i18nKey: 'registerStatus.registed' },
    { id: REGISTER_STATUS.UNREGISTERED, i18nKey: 'registerStatus.unregisted' },
]
export { CARGO_STATUS, cargoStatus, REGISTER_STATUS, registerStatus }
