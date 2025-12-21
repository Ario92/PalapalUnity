# Palapal Issue Console (com.palapalco.issue-console)

Lightweight, optional Issue Console for capturing and handling project-specific issues inside the Unity Editor.

Namespaces: `Palapal.IssueConsole.Runtime` (runtime API) and `Palapal.IssueConsole.Editor` (editor UI).

Features
- Capture issues reported programmatically via `IssueConsoleAPI.Report(...)` or using `IssueLogger.ReportIssue(...)`.
- Capture Unity logs intentionally marked with the `[Issue]` prefix.
- Editor window with multi-select, per-entry `Resolve` and dynamic action buttons.
- API to register custom IIssueAction implementations (e.g. auto-fixers) without forcing other packages to depend on this package.

Additional features
- Search box and filters (Errors/Warnings/Info)
- Grouping by Source or Type
- Persisted resolved state (saved to EditorPrefs)
- Example auto-fix action that renames assets to PascalCase when `assetPath` metadata is provided.

How to use
- Open: Window → Issue Console
- Programmatically report an issue (from runtime code):

```csharp
Palapal.IssueConsole.Runtime.IssueLogger.ReportIssue("My problem description", UnityEngine.LogType.Warning, source: "mypackage");
```

Or log to Unity console (for quick tests):

```csharp
Debug.Log("[Issue] Something is broken: PascalCase violation");
```

Register actions (from any code that references this assembly):

```csharp
public class MyAction : Palapal.IssueConsole.Runtime.IIssueAction { ... }
Palapal.IssueConsole.Runtime.IssueConsoleAPI.RegisterAction(new MyAction());
```

Notes
- This package is editor-focused (`Editor` assembly) and provides a runtime API assembly that other packages can optionally reference. Other packages are not required to depend on it.

Publishing to openUPM
- Ensure this package folder is committed to a public GitHub repository.
- Tag a release that matches `version` in `package.json` (e.g., `v0.1.0`).
- Register or publish via openUPM (https://openupm.com) following their GitHub-based release workflow.

Notes on namespaces and usage
- Runtime API namespace: `Palapal.IssueConsole.Runtime`
- Editor UI namespace: `Palapal.IssueConsole.Editor`
