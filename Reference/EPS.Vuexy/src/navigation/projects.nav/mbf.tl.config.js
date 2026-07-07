import { liveHorizontalNav } from '../horizontal/live'
import { categoriesHorizontalNav } from '../horizontal/categories'
import { eventHorizontalNav } from '../horizontal/event'
import { systemHorizontalNav } from '../horizontal/systems'

import { liveVerticalNav } from '../vertical/live'
import { categoriesVerticalNav } from '../vertical/categories'
import { eventVerticalNav } from '../vertical/event'
import { systemVerticalNav } from '../vertical/systems'

export default {
    nav: {
        vertical: [
            ...liveVerticalNav({
                liveTLMBF: true,
                attendanceMonitor: true,
            }),
            ...eventVerticalNav({
                allEvents: true,
                faceEvent: true,
                fireEvent: true,
                peopleCountEvent: true,
                virtualFenceEvent: true,
            }),
            ...categoriesVerticalNav({
                areas: true,
                devices: true,
                employees: true,
                departments: true,
                groups: true
            }),
            ...systemVerticalNav(),
        ],
        horizontal: [
            ...liveHorizontalNav({
                liveTLMBF: true,
                attendanceMonitor: true,
            }),
            ...categoriesHorizontalNav({
                areas: true,
                devices: true,
                employees: true,
                departments: true,
                groups: true
            }),
            ...eventHorizontalNav({
                allEvents: true,
                faceEvent: true,
                fireEvent: true,
                peopleCountEvent: true,
                virtualFenceEvent: true,
            }),
            ...systemHorizontalNav(),
        ],
    },
}
