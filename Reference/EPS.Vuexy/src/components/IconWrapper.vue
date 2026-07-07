<script>
/**
 * IconWrapper — thin shim around @iconify/vue2 <Icon>.
 *
 * Why this exists:
 *   @iconify/vue2 v2.x reads and re-spreads `this.$attrs` / `this.$listeners`
 *   inside its render() function. When @vue/composition-api is installed it
 *   wraps those two properties with reactive getters/setters, making them
 *   effectively read-only from Vue 2's perspective. Iconify's internal spread
 *   triggers Vue's "X is readonly" warning.
 *
 *   The fix: intercept at the boundary with `inheritAttrs: false` and forward
 *   plain (non-reactive) copies of $attrs and $listeners into the real Icon
 *   component so it never touches the reactive proxies.
 */
import { Icon } from '@iconify/vue2'

export default {
    name: 'IconWrapper',
    inheritAttrs: false,
    render(h) {
        return h(Icon, {
            // Spread into plain objects → no reactive proxy, no readonly warning
            attrs: { ...this.$attrs },
            on: { ...this.$listeners },
            // Forward static / dynamic class and inline styles
            class: this.$vnode.data.staticClass,
            staticClass: this.$vnode.data.staticClass,
            style: this.$vnode.data.style,
            staticStyle: this.$vnode.data.staticStyle,
        })
    },
}
</script>
