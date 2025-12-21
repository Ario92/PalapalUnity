# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/) and this project adheres to [Semantic Versioning](https://semver.org/).


## [0.1.1] - 2025-12-21
### Added
- Included `LICENSE.md` (MIT) in package and added corresponding `.meta` entry.
- Added `changelogUrl`, `documentationUrl` and `licensesUrl` to `package.json` for better package metadata.

### Changed
- Bumped package version from `0.1.0` → `0.1.1`.

### Fixed
- Issue Console editor window fixes and robustness improvements:
  - Properly handle and persist "resolved" state for entries (load/save persisted resolved ids).
  - Fix entry collapse logic and occurrence tracking when identical issues are reported.
  - Improve Unity log capture to only record logs intentionally marked with the `[Issue]` prefix.
  - Better parsing of asset paths from stack traces and attempts to ping/select referenced assets.
  - Fix grouping/foldout handling and UI layout issues in the window.
  - Ensure dynamic action registration and toolbar actions behave correctly.
  - Implement missing helper methods for resolving entries and icon resolution.


## [Unreleased]
- No additional changes.
