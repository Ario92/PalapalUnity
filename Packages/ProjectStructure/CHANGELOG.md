# Changelog

All notable changes to this project will be documented in this file.

## [0.1.0] - 2025-12-21

### Added

- Tools → **Project Structure Validator & Creator** → **Validate All**: a new menu item that validates all configured folders and shows progress.
- Project window context menu **Validate Folder**: right-click an asset in Project and choose **Project Structure Validator & Creator/Validate Folder** to validate a folder; the menu is always visible and will show an explanatory dialog if selection is not a valid folder.
- Optional integration: validation warnings can be reported to the optional **Issue Console** package (if present) via reflection; this is a non-breaking, opt-in integration.

### Fixed

- Updated documentation to mention the new validator features.

## [Unreleased]
- No additional changes.

## [0.1.2] - 2025-01-XX

### Added
- Added "ConvertToPascalCase" context menu item in Assets menu for converting selected assets to PascalCase naming.
- Added Undo support for asset renaming operations.

### Changed
- Improved validation feedback by using Debug.Log instead of EditorUtility.DisplayDialog for better user experience.
- Enhanced folder validation menu with better error handling and progress reporting.
- Updated code formatting and organization for better maintainability.

## [0.1.1] - 2025-12-21

### Added

- Bumped package version from `0.1.0` → `0.1.1`.
- Added or refined package metadata fields (`documentationUrl`, `licensesUrl`, `changelogUrl`) and ensured the `files` list is correct in `package.json`.

### Changed

- Minor documentation and packaging improvements.
