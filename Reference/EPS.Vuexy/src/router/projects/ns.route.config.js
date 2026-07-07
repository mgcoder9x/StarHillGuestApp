import dashboard from '../routes/dashboard-ns'
import systems from '../routes/systems'
import categories from '../routes/categories'
import event from '../routes/event'
import live from '../routes/live'
import notification from '../routes/notification'
import reports from '../routes/reports'
import tosSyncInfo from '../routes/tosSyncInfo'

export default {
    name: 'dashboard-events',
    enabledRoutes: [
        ...dashboard,
        ...systems,
        ...categories,
        ...event,
        ...live,
        // ...apps,
        // ...chartsMaps,
        // ...formsTable,
        // ...uiElements,
        // ...others,
        ...tosSyncInfo,
        ...notification,
        ...reports,
    ],
}
