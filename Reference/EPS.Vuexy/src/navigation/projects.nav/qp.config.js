import { liveHorizontalNav } from '../horizontal/live'
import { categoriesHorizontalNav } from '../horizontal/categories'
import { eventHorizontalNav } from '../horizontal/event'
import { systemHorizontalNav } from '../horizontal/systems'
import { reportHorizontalNav } from '../horizontal/reports'
import { dashboardHorizontalNav } from '../horizontal/dashboard'
// import { tosSyncInfoHorizontalNav } from '../horizontal/tosSyncInfo'
// import { issueHorizontalNav } from '../horizontal/issue'

import { liveVerticalNav } from '../vertical/live'
import { categoriesVerticalNav } from '../vertical/categories'
import { eventVerticalNav } from '../vertical/event'
import { systemVerticalNav } from '../vertical/systems'
import { reportVerticalNav } from '../vertical/reports'
import { dashboardVerticalNav } from '../vertical/dashboard'
// import { tosSyncInfoVerticalNav } from '../vertical/tosSyncInfo'
// import { issueVerticalNav } from '../vertical/issue'

export default {
    nav: {
        vertical: [
            ...dashboardVerticalNav({
                mapDashboard: true,
                eventsDashboard: true,
            }),
            ...liveVerticalNav({
                live: true,
                liveView: true,
            }),
            ...eventVerticalNav({
                allEvents: true,
                vehicleEvent: true,
                fireEvent: true,
                virtualFenceEvent: true,
                faceEvent: true,
                vehicleSessionsEvent: true,
                trafficViolationEvent: true,
            }),
            ...categoriesVerticalNav({
                areas: true,
                devices: true,
                departments: true,
                groups: true,
                employees: true,
                lane: true,
                vehicleCards: true,
                rfidConfigs: true,
                controllers: true,
                vehicles: true,
            }),
            // ...tosSyncInfoVerticalNav(),
            ...reportVerticalNav({
                // CountSupervision: true,
                ViolatePersonReport: true,
                FireEvent: true,
                CountVehicleReport: true,
                VehicleInPlantReport: true,
            }),
            ...systemVerticalNav({
                NotificationTemplates: true,
                Users: true,
                Roles: true,
                Companies: true,
                AuditLogs: true,
            }),
            // ...issueVerticalNav(),
        ],
        horizontal: [
            ...dashboardHorizontalNav({
                mapDashboard: true,
                eventsDashboard: true,
            }),
            ...liveHorizontalNav({
                live: true,
                liveView: true,
            }),
            ...eventHorizontalNav({
                allEvents: true,
                vehicleEvent: true,
                fireEvent: true,
                virtualFenceEvent: true,
                faceEvent: true,
                vehicleSessionsEvent: true,
                trafficViolationEvent: true,
            }),
            ...categoriesHorizontalNav({
                areas: true,
                devices: true,
                departments: true,
                groups: true,
                employees: true,
                lane: true,
                vehicleCards: true,
                rfidConfigs: true,
                controllers: true,
                vehicles: true,
            }),
            // ...tosSyncInfoHorizontalNav(),
            ...reportHorizontalNav({
                // CountSupervision: true,
                ViolatePersonReport: true,
                FireEvent: true,
                CountVehicleReport: true,
                VehicleInPlantReport: true,
            }),
            ...systemHorizontalNav({
                NotificationTemplates: true,
                Users: true,
                Roles: true,
                Companies: true,
                AuditLogs: true,
            }),
            // ...issueHorizontalNav(),
        ],
    },
}
