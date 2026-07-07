import { liveHorizontalNav } from '../horizontal/live'
import { categoriesHorizontalNav } from '../horizontal/categories'
import { eventHorizontalNav } from '../horizontal/event'
import { systemHorizontalNav } from '../horizontal/systems'
import { reportHorizontalNav } from '../horizontal/reports'
import { dashboardHorizontalNav } from '../horizontal/dashboard'
import { issueHorizontalNav } from '../horizontal/issue'

import { liveVerticalNav } from '../vertical/live'
import { categoriesVerticalNav } from '../vertical/categories'
import { eventVerticalNav } from '../vertical/event'
import { systemVerticalNav } from '../vertical/systems'
import { reportVerticalNav } from '../vertical/reports'
import { dashboardVerticalNav } from '../vertical/dashboard'
import { issueVerticalNav } from '../vertical/issue'

export default {
    nav: {
        vertical: [
            ...dashboardVerticalNav(),
            ...liveVerticalNav({
                live: true,
            }),
            ...eventVerticalNav(),
            ...categoriesVerticalNav(),
            ...issueVerticalNav(),
            ...reportVerticalNav(),
            ...systemVerticalNav(),
        ],
        horizontal: [
            ...dashboardHorizontalNav(),
            ...liveHorizontalNav({
                live: true,
            }),
            ...eventHorizontalNav(),
            ...categoriesHorizontalNav(),
            ...issueHorizontalNav(),
            ...reportHorizontalNav(),
            ...systemHorizontalNav(),
        ],
    },
}
