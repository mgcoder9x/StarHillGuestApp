<template>
    <div class="navbar-container main-menu-content">
        <horizontal-nav-menu-items :items="navMenuItems" />
    </div>
</template>

<script>
/* eslint-disable */
import navMenuItems from '@/navigation/horizontal'
import useJwt from '@/auth/jwt/useJwt'
import HorizontalNavMenuItems from './components/horizontal-nav-menu-items/HorizontalNavMenuItems.vue'

export default {
    components: {
        HorizontalNavMenuItems,
    },
    computed: {
        /* eslint-disable */
        navMenuItems() {
            let dynamicNav = useJwt.getNavbar()
            const dashboardNav = this.getDashboardNav()
            if (dashboardNav) {
                this.addUniqueRoutes(dashboardNav, dynamicNav)
            }
            let result = navMenuItems
            let privileges = useJwt.getUserData().privileges

            RemoveUnauthorizedItems(result)

            function RemoveUnauthorizedItems(items) {
                if (items && items.length > 0) {
                    for (let i = items.length - 1; i >= 0; i--) {
                        let item = items[i]

                        let requiresPrivileges = item.requiresPrivileges || []
                        if (item.children && item.children.length > 0) {
                            requiresPrivileges = requiresPrivileges.concat(
                                item.children
                                    .map((x) => x.requiresPrivileges)
                                    .reduce((x, y) => (x || []).concat(y || []))
                            )
                        }

                        if (
                            requiresPrivileges &&
                            requiresPrivileges.length > 0 &&
                            privileges.filter((value) =>
                                requiresPrivileges.includes(value)
                            ).length == 0
                        ) {
                            removeElement(items, item)
                        } else {
                            RemoveUnauthorizedItems(item.children)
                        }
                    }
                }
            }

            function removeElement(array, elem) {
                let index = array.indexOf(elem)
                if (index > -1) {
                    array.splice(index, 1)
                }
            }

            return result
        },
    },
    methods: {
        getDynamicRoutes(routes) {
            return routes.map((route) => ({
                title: route.title,
                icon: route.icon,
                requiresPrivileges: route.requiresPrivileges,
                route: route.name,
            }))
        },

        getDashboardNav() {
            const index = navMenuItems.findIndex(
                (nav) => nav.title === 'Dashboards'
            )
            return index !== -1 ? navMenuItems[index] : null
        },

        addUniqueRoutes(dashboardNav, dynamicRoutes) {
            const existingRoutes = new Set(
                dashboardNav.children.map((child) => child.route)
            )
            const uniqueRoutes = dynamicRoutes.filter(
                (route) => !existingRoutes.has(route.route)
            )

            dashboardNav.children.push(...uniqueRoutes)
        },
    },
}
</script>

<style lang="scss">
@import '~@core/scss/base/core/menu/menu-types/horizontal-menu.scss';
</style>
