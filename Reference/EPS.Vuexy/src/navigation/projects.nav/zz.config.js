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
            ...dashboardVerticalNav({
                fireworkDashboard: true
            }),
            ...liveVerticalNav({
                live: true,
            }),
            ...eventVerticalNav({
                allEvents: true,
                fireworkEvent: true
            }),
            ...categoriesVerticalNav({
                areas: true,
                devices: true,
                departments: true,
                contractors: true,
                groups: true,
                machines: true,
            }),
            // ...reportVerticalNav({}),
            ...systemVerticalNav(),
        ],
        horizontal: [
            ...dashboardHorizontalNav({
                fireworkDashboard: true
            }),
            ...liveHorizontalNav({
                live: true,
            }),
            ...eventHorizontalNav({
                allEvents: true,
                fireworkEvent: true
            }),
            ...categoriesHorizontalNav({
                areas: true,
                devices: true,
                departments: true,
                contractors: true,
                groups: true,
                machines: true,
            }),
            // ...reportHorizontalNav({}),
            ...systemHorizontalNav(),
        ],
    },
}
