## Implementation Note

**Adapted for:** carEvent/List.vue device select component  
**Date:** 2026-03-06  
**Status:** Device select successfully converted to tree-select with consistent configuration

---

## 1. Template Component Replacement (Adapted for Device Select)

- [x] 1.1 Replace v-select component with Treeselect for device filter (carEvent/List.vue lines 163-178)
- [x] 1.2 Add `multiple` prop to device Treeselect component (`:multiple="true"`)
- [x] 1.3 Add `limit="3"` and `:limit-text="(count) => \`+${count}\`"` props to device Treeselect
- [x] 1.4 Add `value-consists-of="ALL"` prop to device Treeselect (`:value-consists-of="'ALL'"`)
- [x] 1.5 Add `placeholder` and `class="treeselect-nowrap"` to device Treeselect
- [ ] 1.6 Replace v-select component with Treeselect for fireLevel filter (lines ~58-78)
- [ ] 1.7 Ensure fireLevel Treeselect has consistent props: `limit="3"`, `limit-text`, `value-consists-of="ALL"`, `class="treeselect-nowrap"`
- [x] 1.8 Verify component uses `:dir` binding for RTL support from isRTL computed property

## 2. Data Model Updates

- [ ] 2.1 Update searchForm.fireType initialization to null (support array value) in data() method
- [ ] 2.2 Verify searchForm.fireLevel already supports array (no change needed)
- [ ] 2.3 Update refresh() method to reset fireType to null (clear array on refresh)
- [ ] 2.4 Ensure cached searchForm in localStorage handles fireType as array (check getStorage/setStorage compatibility)

## 3. Export Logic Updates

- [ ] 3.1 Update exportData() method to handle multiple fireType selections (around line ~492-500)
- [ ] 3.2 Replace single fireType lookup logic with array mapping similar to fireLevelName pattern
- [ ] 3.3 Use `map()` to get translated names for multiple fire types, then `join(', ')` for display
- [ ] 3.4 Update Excel header to show comma-separated fire type names or "All" if empty
- [ ] 3.5 Ensure export_fields_vi header translations remain correct for fireType column

## 4. Data Formatting in Export

- [ ] 4.1 Update exportData() data loop to set fireTypeName for each item using getDynamicName() helper
- [ ] 4.2 Verify getDynamicName() method works correctly with fireType ID from event data
- [ ] 4.3 Test Excel export with single fireType selection shows correct name
- [ ] 4.4 Test Excel export with multiple fireType selections shows correct comma-separated names
- [ ] 4.5 Test Excel export with no fireType selection shows "Tất cả" (All)

## 5. Component Import and Registration (Adapted for carEvent/List.vue)

- [x] 5.1 Import Treeselect component from '@riophae/vue-treeselect'
- [x] 5.2 Register Treeselect in components object
- [x] 5.3 Fix import order to place external imports before local imports

## 6. Testing and Validation

- [ ] 6.1 Test fire type filter with single selection - verify search works
- [ ] 6.2 Test fire type filter with multiple selections - verify search returns combined results
- [ ] 6.3 Test fire level filter still works with multiple selections
- [ ] 6.4 Test filter chips display correctly with limit of 3 items and "+n" overflow
- [ ] 6.5 Test refresh button clears all filters including fireType array
- [ ] 6.6 Test filter state persistence in localStorage after page refresh
- [ ] 6.7 Test RTL mode - verify Treeselect components display correctly in RTL direction
- [ ] 6.8 Test Excel export with various fireType filter combinations
- [ ] 6.9 Verify BasicTable correctly receives and processes searchForm with array fireType
- [ ] 6.10 Check browser console for any errors or warnings during filter operations
