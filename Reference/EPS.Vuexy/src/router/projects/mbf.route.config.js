import systems from '../routes/systems'
import categories from '../routes/categories'
import event from '../routes/event'
import live from '../routes/live'
import notification from '../routes/notification'

export default {
    name: 'live',
    enabledRoutes: [
        ...systems,
        ...categories,
        ...event,
        ...live,
        ...notification,
    ],
}
