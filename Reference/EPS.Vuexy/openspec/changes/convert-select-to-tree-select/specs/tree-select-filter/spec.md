## ADDED Requirements

### Requirement: Fire Type Filter with Multiple Selection

The fire event list view SHALL use Treeselect component for fire type filtering with multiple selection enabled.

#### Scenario: User selects single fire type

- **WHEN** user clicks the fire type filter dropdown
- **THEN** system displays Treeselect component with fire type options (smoke, fire)
- **AND** user can select one fire type
- **AND** selected value is stored as array in searchForm.fireType
- **AND** search is triggered automatically on selection

#### Scenario: User selects multiple fire types

- **WHEN** user clicks the fire type filter dropdown
- **THEN** system displays Treeselect component with fire type options
- **AND** user can select multiple fire types (e.g., both smoke and fire)
- **AND** selected values are stored as array in searchForm.fireType
- **AND** selected chips are displayed with limit of 3 items
- **AND** overflow is indicated with "+n" text
- **AND** search is triggered automatically on selection

#### Scenario: User clears fire type filter

- **WHEN** user clears all selected fire types
- **THEN** searchForm.fireType is set to null
- **AND** search is triggered automatically
- **AND** results show events of all fire types

### Requirement: Fire Level Filter Consistency

The fire event list view SHALL use Treeselect component for fire level filtering, maintaining existing multiple selection functionality.

#### Scenario: User selects multiple fire levels

- **WHEN** user clicks the fire level filter dropdown
- **THEN** system displays Treeselect component with level options (low, medium, high, critical)
- **AND** user can select multiple levels
- **AND** selected values are stored as array in searchForm.fireLevel
- **AND** selected chips are displayed with limit of 3 items
- **AND** overflow is indicated with "+n" text
- **AND** search is triggered automatically on selection

#### Scenario: Component styling matches area/device filters

- **WHEN** user views the fire level filter
- **THEN** component appearance is consistent with area and device Treeselect filters
- **AND** component uses treeselect-nowrap CSS class
- **AND** component respects RTL direction from app config

### Requirement: Excel Export with Multiple Fire Types

The system SHALL export fire event data with multiple fire type selections displayed correctly in the header.

#### Scenario: Export with single fire type selected

- **WHEN** user has selected one fire type (e.g., "Smoke")
- **AND** user clicks "Export Excel" button
- **THEN** system generates Excel file
- **AND** header row displays "Loại sự cố: Khói" (or translated fire type name)
- **AND** data rows contain events matching the selected fire type

#### Scenario: Export with multiple fire types selected

- **WHEN** user has selected multiple fire types (e.g., "Smoke" and "Fire")
- **AND** user clicks "Export Excel" button
- **THEN** system generates Excel file
- **AND** header row displays "Loại sự cố: Khói, Lửa" (comma-separated translated names)
- **AND** data rows contain events matching any of the selected fire types

#### Scenario: Export with no fire type selected

- **WHEN** user has not selected any fire type (filter is empty)
- **AND** user clicks "Export Excel" button
- **THEN** system generates Excel file
- **AND** header row displays "Loại sự cố: Tất cả" (All)
- **AND** data rows contain events of all fire types

### Requirement: Filter State Persistence

The system SHALL persist fire type filter selections across page refreshes using localStorage.

#### Scenario: Filter state is saved on search

- **WHEN** user selects fire type values and triggers search
- **THEN** system stores searchForm (including fireType array) in localStorage with key "fireEventSearchForm"
- **AND** cache expires after 120 minutes

#### Scenario: Filter state is restored on page load

- **WHEN** user navigates to fire event list page
- **AND** valid cached searchForm exists in localStorage
- **THEN** system restores fireType filter selections from cache
- **AND** fire type Treeselect component displays previously selected values
- **AND** search is triggered automatically with restored filters

#### Scenario: Refresh clears all filters

- **WHEN** user clicks "Refresh" button
- **THEN** system clears "fireEventSearchForm" from localStorage
- **AND** all filter fields (including fireType) are reset to null or default values
- **AND** dateFrom is reset to today at 00:00:00
- **AND** search is triggered with cleared filters

### Requirement: Component Configuration Consistency

All Treeselect filter components SHALL use consistent configuration properties for uniform behavior.

#### Scenario: Treeselect configuration for fire type

- **WHEN** fire type Treeselect component is rendered
- **THEN** component uses the following configuration:
    - `multiple`: true
    - `label`: "text"
    - `reduce`: maps to id property
    - `options`: translated fire type list
    - `placeholder`: empty string
    - `limit`: 3
    - `limit-text`: displays "+{count}" for overflow
    - `value-consists-of`: "ALL"
    - `dir`: respects RTL setting from $store.state.appConfig.isRTL
    - `@input`: triggers search() method

#### Scenario: Treeselect configuration for fire level

- **WHEN** fire level Treeselect component is rendered
- **THEN** component uses the same configuration properties as fire type filter
- **AND** `options`: translated level list (low, medium, high, critical)
- **AND** all other properties match fire type configuration
