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
                liveView: true,
            }),
            ...eventVerticalNav(),
            ...categoriesVerticalNav(),
            ...tosSyncInfoVerticalNav(),
            ...reportVerticalNav(),
            ...systemVerticalNav(),
            ...issueVerticalNav(),
        ],
        horizontal: [
            ...dashboardHorizontalNav(),
            ...liveHorizontalNav({
                live: true,
                liveView: true,
            }),
            ...eventHorizontalNav(),
            ...categoriesHorizontalNav(),
            ...tosSyncInfoHorizontalNav(),
            ...reportHorizontalNav(),
            ...systemHorizontalNav(),
            ...issueHorizontalNav(),
        ],
    },
}
