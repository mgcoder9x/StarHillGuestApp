## Context

The Fire Event list view (`src/views/events/fireEvent/List.vue`) currently uses a mix of component types for filtering:

- Treeselect component for hierarchical area selection (with multiple)
- tree-select (alias for Treeselect) for device selection (with multiple)
- v-select for fire type (single selection)
- v-select for fire level (multiple selection)

This inconsistency creates:

- Different visual appearance and interaction patterns
- Maintenance overhead tracking two component libraries
- User confusion with varying filter behaviors

The existing Treeselect component (`@riophae/vue-treeselect`) is already imported and configured. Backend API endpoint `/fireEvents` needs to support array parameters for fireType filtering.

## Goals / Non-Goals

**Goals:**

- Standardize all filter controls to use Treeselect component
- Enable multiple selection for fire type filter (currently single)
- Maintain consistent limit display (3 items with "+n" overflow)
- Update export logic to handle multiple fire type selections
- Preserve existing functionality for area, device, and level filters

**Non-Goals:**

- Not modifying backend API structure (assumes it can handle array params)
- Not changing the data structure of lstFireType or level arrays
- Not affecting other event list views (FACE, EQUIPMENT, etc.) in this change
- Not introducing new dependencies or component libraries

## Decisions

### Decision 1: Use Treeselect for all filters (not create custom component)

**Rationale:** Treeselect is already imported, supports flat lists (not just trees), and provides consistent API with `multiple`, `limit`, and `limit-text` props. The area and device filters already use it successfully.

**Alternatives considered:**

- Create custom multi-select component → Rejected: Adds maintenance burden
- Use vue-multiselect library → Rejected: Introduces new dependency
- Keep v-select with custom styling → Rejected: Still inconsistent UX

### Decision 2: Change fireType from single to array value

**Rationale:** Enables users to filter by multiple fire types simultaneously (e.g., show both smoke AND fire events). Export logic already handles arrays for level, so pattern is established.

**Impact:** `searchForm.fireType` changes from `id | null` to `array | null`. Backend must handle array parameter.

**Alternatives considered:**

- Keep single selection → Rejected: Inconsistent with level filter behavior
- Use comma-separated string → Rejected: Backend expects array format

### Decision 3: Maintain existing SCSS styling from @core

**Rationale:** File already imports `@/assets/scss/_custom-tree-select.scss` which provides consistent styling for Treeselect. No additional styles needed.

### Decision 4: Update export logic inline (not extract utility)

**Rationale:** The `exportData()` method is specific to this view. The logic for handling multiple fireTypes is straightforward (map + join), similar to existing fireLevelName logic.

**Alternatives considered:**

- Create shared utility function → Rejected: Only used in one place currently
- Refactor entire export system → Out of scope for this change

## Risks / Trade-offs

**[Risk]** Backend API `/fireEvents` may not support array parameter for fireType  
→ **Mitigation:** Verify API documentation or test endpoint. If not supported, backend team needs to update parameter binding.

**[Risk]** Changing fireType from single to array may break saved filters in localStorage  
→ **Mitigation:** Clear `fireEventSearchForm` cache on first load after deployment, or add migration logic to convert single value to array.

**[Trade-off]** Treeselect component is heavier than v-select (includes tree navigation logic)  
→ **Acceptable:** Performance impact is negligible for small lists (~4 items), and consistency benefit outweighs bundle size.

**[Risk]** Users may be confused by multiple fire type selection if not needed  
→ **Mitigation:** Placeholder and label remain clear. Users can still select single item.

## Migration Plan

1. **Pre-deployment:** Verify backend `/fireEvents` endpoint accepts array for `fireType` parameter
2. **Deployment:** Standard frontend build and deploy process
3. **Post-deployment:** Monitor for errors related to filter searches
4. **Cache invalidation:** Consider clearing `fireEventSearchForm` localStorage key for all users (or increment cache version)

**Rollback strategy:** Revert component changes in List.vue. No database changes involved.

## Open Questions

1. ✅ Should fire type filter allow multiple selection? → **Yes, for consistency with level filter**
2. ❓ Does backend API support array parameter for fireType? → **Need to verify**
3. ✅ Should we apply this pattern to other event list views? → **Not in this change, assess later**
