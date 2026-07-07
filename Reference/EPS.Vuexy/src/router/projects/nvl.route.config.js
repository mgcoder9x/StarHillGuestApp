import apps from '../routes/apps'
import dashboard from '../routes/dashboard'
import uiElements from '../routes/ui-elements/index'
import chartsMaps from '../routes/charts-maps'
import formsTable from '../routes/forms-tables'
import others from '../routes/others'
import systems from '../routes/systems'
import categories from '../routes/categories'
import event from '../routes/event'
import live from '../routes/live'
import notification from '../routes/notification'
import reports from '../routes/reports'
import issue from '../routes/issue'
import study from '../routes/study'
import tosSyncInfo from '../routes/tosSyncInfo'

export default {
    name: 'dashboard-map-dashboard-events',
    enabledRoutes: [
        ...dashboard,
        ...systems,
        ...categories,
        ...issue,
        ...event,
        ...live,
        ...reports,
        ...notification,
        ...apps,
        ...chartsMaps,
        ...formsTable,
        ...uiElements,
        ...others,
        ...tosSyncInfo,
        ...study,
    ],
}
