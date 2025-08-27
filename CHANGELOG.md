# Changelog

All notable changes to this project will be documented in this file.

## [1.7.5.0] - 2025-08-27
### Added
- Count in evironment list.
- In Query per Schema tab, allow multiple environment query for SCAH only. The behavior of tab is different from others since to avoid confusion when using it.
- Can now give multiple results in Query tab as long as it is separated by GO.
- Tab in query has been removed upon opening the application. Ctrl+Shift+T to add a new tab.

### Fixed
- Removed button in Environment list. Automatically loads the tenant when selecting environment.
- Adjust width of result in query.

## [1.7.4.0] - 2025-08-12
### Added
- Context menu in query text box for saving queries.
- Context menu in query text box for inserting saved queries.
- Search functionality in the environment list.
- Width resizing capability in the environment tab.
- Change search functionality in Feature Toggle Tab.

### Fixed
- Renamed Query Multiple to Query per Schema.
- The tenant selected in the Environment tab will remain consistent across all tabs except for the Query tab. Additionally, the selected inner tab inside the Query tab will change accordingly.
- Removed search button in Feature Toggle Tab.

## [1.7.3.0] - 2025-08-10
### Added
- Manual execute trigger of failed changesets in Check Database section.
- Manual execute trigger of selected changeset in Check Database section.

### Fixed
- Verify button - now disabled after verification. this will be enabled again once get changeset is clicked.

## [1.7.2.0] - 2025-08-08
### Added
- Viewing of changeset in Database-> Check Database.

### Fixed
- Changelog viewing

## [1.7.1.0] - 2025-07-31
### Added
- Change location of help menu from right to left.

### Fixed
- Bug in grid of feature toggle when saving.
