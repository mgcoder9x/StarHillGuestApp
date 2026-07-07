export const ProtectiveEquipment = {
    200: 'ProtectiveEquipmentType.Enough',
    201: 'ProtectiveEquipmentType.MissingHelmet',
    202: 'ProtectiveEquipmentType.MissingFaceMask',
    203: 'ProtectiveEquipmentType.MissingGloves',
    204: 'ProtectiveEquipmentType.EHelmet',
    205: 'ProtectiveEquipmentType.EMask',
    206: 'ProtectiveEquipmentType.EGloves',
    207: 'ProtectiveEquipmentType.EBoots',
    208: 'ProtectiveEquipmentType.MissingBoots',
    10: 'ProtectiveEquipmentType.ConveyorBeltOverflow',
    12: 'ProtectiveEquipmentType.ConveyorBeltTear',
    13: 'ProtectiveEquipmentType.ConveyorBeltMisalignment',
    14: 'ProtectiveEquipmentType.RawMaterialSize',

    Safe: 200,
}
export const CarWarningType = {
    301: 'CarEvent.Common.WarningType.None',
    302: 'CarEvent.Common.WarningType.Suspect',
    303: 'CarEvent.Common.WarningType.Wooden',
    304: 'CarEvent.Common.WarningType.Iron',
    305: 'CarEvent.Common.WarningType.Other',
    // 8001: 'Đủ điều kiện',
    // 8002: 'Không đủ điều kiện',
}
export const ProtectiveEquipmentEvent = {
    314: 'ProtectiveEquipmentType.Smoke',
    313: 'ProtectiveEquipmentType.Fire',
}
export const listSafe = [
    {
        id: true,
        text: 'ProtectiveEquipmentType.Safe',
        value: true,
    },
    {
        id: false,
        text: 'ProtectiveEquipmentType.Unsafe',
        value: false,
    },
]
export const listProtectiveEquipment = Object.keys(ProtectiveEquipment)
    .filter((x) => typeof ProtectiveEquipment[x] === 'string')
    .map((key) => {
        const value = ProtectiveEquipment[key]
        return {
            id: key,
            text: value,
            value: key,
        }
    })
