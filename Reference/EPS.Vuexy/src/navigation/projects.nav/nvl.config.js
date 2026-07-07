import { liveHorizontalNav } from '../horizontal/live'
import { categoriesHorizontalNav } from '../horizontal/categories'
import { eventHorizontalNav } from '../horizontal/event'
import { systemHorizontalNav } from '../horizontal/systems'
import { reportHorizontalNav } from '../horizontal/reports'
import { dashboardHorizontalNav } from '../horizontal/dashboard'
import { tosSyncInfoHorizontalNav } from '../horizontal/tosSyncInfo'
import { issueHorizontalNav } from '../horizontal/issue'

import { liveVerticalNav } from '../vertical/live'
import { categoriesVerticalNav } from '../vertical/categories'
import { eventVerticalNav } from '../vertical/event'
import { systemVerticalNav } from '../vertical/systems'
import { reportVerticalNav } from '../vertical/reports'
import { dashboardVerticalNav } from '../vertical/dashboard'
import { tosSyncInfoVerticalNav } from '../vertical/tosSyncInfo'
import { issueVerticalNav } from '../vertical/issue'

export default {
    nav: {
        vertical: [
            ...dashboardVerticalNav(),
            ...liveVerticalNav({
                live: true,
            }),
            ...eventVerticalNav({
                allEvents: true,
                carEvent: true,
                conveyorEvent: true,
                faceGateEvent: true,
                fireEvent: true,
                fireworkEvent: true,
                peopleCountEvent: true,
                peopleCountInOutEvent: true,
                protectiveEquipmentEvent: true,
                workAtHeightMonitoring: true,
                restrictedZonesEvent: true,
                trafficViolationEvent: true,
                vehicleEvent: true,
                virtualFenceEvent: true,
                waterEvent: true,
            }),
            ...categoriesVerticalNav(),
            ...tosSyncInfoVerticalNav(),
            ...reportVerticalNav({
                CountConveyor: true,
                CountSupervision: true,
                VehicleDetail: true,
                VehicleViolation: true,
                FireEvent: true,
                ReportVehicleWarning: true,
                PeopleTraffic: true,
                SafetyEquipmentViolationReport: true,
                SafetyBarrierStatisticsDetailReport: true,
                ContainerMonitoringReport: true,
            }),
            ...systemVerticalNav(),
            ...issueVerticalNav(),
        ],
        horizontal: [
            ...dashboardHorizontalNav(),
            ...liveHorizontalNav({
                live: true,
            }),
            ...eventHorizontalNav({
                allEvents: true,
                carEvent: true,
                conveyorEvent: true,
                faceGateEvent: true,
                fireEvent: true,
                fireworkEvent: true,
                peopleCountEvent: true,
                protectiveEquipmentEvent: true,
                workAtHeightMonitoring: true,
                peopleCountInOutEvent: true,
                restrictedZonesEvent: true,
                trafficViolationEvent: true,
                vehicleEvent: true,
                virtualFenceEvent: true,
                waterEvent: true,
            }),
            ...categoriesHorizontalNav(),
            ...tosSyncInfoHorizontalNav(),
            ...reportHorizontalNav({
                CountConveyor: true,
                CountSupervision: true,
                VehicleDetail: true,
                VehicleViolation: true,
                FireEvent: true,
                ReportVehicleWarning: true,
                PeopleTraffic: true,
                SafetyEquipmentViolationReport: true,
                SafetyBarrierStatisticsDetailReport: true,
                ContainerMonitoringReport: true,
            }),
            ...systemHorizontalNav(),
            ...issueHorizontalNav(),
        ],
    },
}
