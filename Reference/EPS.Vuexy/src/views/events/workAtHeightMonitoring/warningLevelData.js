export const ProtectiveEquipment = {
    200: 'ProtectiveEquipmentType.Enough',
    201: 'ProtectiveEquipmentType.Helmet',
    202: 'ProtectiveEquipmentType.FaceMask',
    203: 'ProtectiveEquipmentType.Gloves',
    204: 'ProtectiveEquipmentType.HelmetGloves',
    205: 'ProtectiveEquipmentType.HelmetMask',
    206: 'ProtectiveEquipmentType.MaskGloves',
    207: 'ProtectiveEquipmentType.MissingEverything',
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
