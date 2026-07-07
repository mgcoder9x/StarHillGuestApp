## Why

The current Fire Event list view uses standard v-select components for filter fields like fire type and level. To improve consistency with the hierarchical area and device selectors, we need to standardize on tree-select components with multiple selection support, providing a uniform UX across all filter controls.

## What Changes

- Replace v-select components with tree-select components in Fire Event list filters
- Enable multiple selection on the fire type filter (currently single-select)
- Maintain multiple selection on fire level filter (already supports multiple)
- Ensure consistent styling and behavior across all filter controls
- Update export functionality to handle multiple fire type selections
- Update translation keys and display logic for multiple selections

## Capabilities

### New Capabilities
- `tree-select-filter`: Standardized tree-select component usage for all filter fields with consistent configuration, styling, and multiple selection support

### Modified Capabilities
<!-- No existing capabilities are being modified at the requirements level -->

## Impact

**Code Changes:**
- `src/views/events/fireEvent/List.vue`: Replace v-select with tree-select for fireType and fireLevel filters
- Export logic: Update `exportData()` method to handle multiple fire type selections (currently expects single value)
- Search form structure: fireType field will change from single value to array

**UI/UX:**
- Users can now select multiple fire types in the filter
- Consistent tree-select appearance across all filters (area, device, fireType, fireLevel)
- Filter chips display selected items with "+n" overflow indicator (limit: 3)

**Data Flow:**
- Search form `fireType` field changes from single ID to array of IDs
- Backend API endpoint `/fireEvents` must support array parameter for fireType filter
- Excel export headers need to handle comma-separated fire type names

**Dependencies:**
- Already using `@riophae/vue-treeselect` package (no new dependencies)
- No impact on other event type list views unless similar pattern adopted
