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
import dashboardMeiko from '../routes/dashboard-smart-factory'
import dashboardHoaPhat from '../routes/dashboard-hp'
import dashboardNovaland from '../routes/dashboard-nvl'
import dashboardNghiSon from '../routes/dashboard-ns'
import eventType from '../routes/eventType'
import eventWarningLevel from '../routes/eventWarningLevel'
export default {
    name: 'live',
    enabledRoutes: [
        ...dashboard,
        ...dashboardMeiko,
        ...dashboardHoaPhat,
        ...dashboardNovaland,
        ...dashboardNghiSon,
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
        ...eventType,
        ...eventWarningLevel,
    ],
}
