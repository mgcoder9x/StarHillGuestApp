import dashboard from '../routes/dashboard-smart-factory'
import systems from '../routes/systems'
import categories from '../routes/categories'
import event from '../routes/event'
import live from '../routes/live'
import notification from '../routes/notification'
import reports from '../routes/reports'

export default {
    name: 'meiko-route',
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
        ...notification,
        ...reports,
    ],
}
