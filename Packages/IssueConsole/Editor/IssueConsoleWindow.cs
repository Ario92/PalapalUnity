#if UNITY_EDITOR
using Palapal.IssueConsole.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Palapal.IssueConsole.Editor
{
    public class IssueConsoleWindow : EditorWindow
    {
        private List<IssueConsoleEntry> _entries = new List<IssueConsoleEntry>();
        private Vector2 _scroll;
        private HashSet<string> _selectedIds = new HashSet<string>();
        private bool _showResolved = false;
        private List<IIssueAction> _actions = new List<IIssueAction>();
        private string _search = "";
        private bool _filterErrors = true;
        private bool _filterWarnings = true;
        private bool _filterInfo = true;
        private enum GroupBy { None, Source, Type }
        private GroupBy _groupBy = GroupBy.None;
        private Dictionary<string, bool> _groupFoldouts = new Dictionary<string, bool>();
        private const string ResolvedIdsPrefKey = "IssueConsole_ResolvedIds_v1";
        private HashSet<string> _persistedResolvedIds = new HashSet<string>();

        [MenuItem("Window/Issue Console")]
        public static void Open()
        {
            GetWindow<IssueConsoleWindow>("Issue Console");
        }

        private void OnEnable()
        {
            IssueConsoleAPI.OnEntryReported += OnEntryReported;
            IssueConsoleAPI.OnActionRegistered += OnActionRegistered;
            IssueConsoleAPI.OnEntriesResolved += OnEntriesResolved;
            Application.logMessageReceived += OnUnityLog;

            foreach (var a in IssueConsoleAPI.RegisteredActions)
                _actions.Add(a);
            LoadResolvedState();
            // mark any already-captured entries as resolved if present in persisted state
            foreach (var e in _entries)
            {
                if (_persistedResolvedIds.Contains(e.id)) e.resolved = true;
            }
        }

        private void OnDisable()
        {
            IssueConsoleAPI.OnEntryReported -= OnEntryReported;
            IssueConsoleAPI.OnActionRegistered -= OnActionRegistered;
            IssueConsoleAPI.OnEntriesResolved -= OnEntriesResolved;
            Application.logMessageReceived -= OnUnityLog;
        }

        private void OnActionRegistered(IIssueAction action)
        {
            if (!_actions.Contains(action)) _actions.Add(action);
            Repaint();
        }

        private void OnEntriesResolved(IEnumerable<IssueConsoleEntry> entries)
        {
            var ids = new HashSet<string>(entries.Select(e => e.id));
            foreach (var e in _entries)
            {
                if (ids.Contains(e.id)) e.resolved = true;
            }
            Repaint();
        }

        private void OnEntryReported(IssueConsoleEntry entry)
        {
            // Try to collapse with existing identical entry (message + source + assetPath)
            var assetPath = entry.metadata != null && entry.metadata.ContainsKey("assetPath") ? entry.metadata["assetPath"] : null;
            var existing = _entries.FirstOrDefault(e => e.message == entry.message && e.source == entry.source && ((e.metadata != null && e.metadata.ContainsKey("assetPath") && e.metadata["assetPath"] == assetPath) || (string.IsNullOrEmpty(assetPath) && e.stackTrace == entry.stackTrace)) && !e.resolved);
            if (existing != null)
            {
                existing.occurrences += 1;
                existing.lastSeen = DateTime.UtcNow;
                // move to top
                _entries.Remove(existing);
                _entries.Insert(0, existing);
            }
            else
            {
                _entries.Insert(0, entry);
                // if this entry was persisted as resolved, mark it
                if (_persistedResolvedIds.Contains(entry.id))
                    entry.resolved = true;
            }

            Repaint();
        }

        // Only capture logs intentionally marked with [Issue]
        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(condition)) return;
            const string marker = "[Issue]";
            if (condition.StartsWith(marker))
            {
                var message = condition.Substring(marker.Length).Trim();
                var entry = new IssueConsoleEntry
                {
                    message = message,
                    stackTrace = stackTrace,
                    type = type,
                    source = "unity-log"
                };
                OnEntryReported(entry);
            }
        }

        private string ParseFirstAssetPath(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var m = System.Text.RegularExpressions.Regex.Match(text, @"(Assets/[A-Za-z0-9_\-./]+)");
            return m.Success ? m.Groups[1].Value : null;
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSearchAndFilters();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var filtered = _entries.Where(e => (_showResolved || !e.resolved) && MatchesFilters(e)).ToList();

            if (_groupBy == GroupBy.None)
            {
                foreach (var e in filtered)
                    DrawEntryRow(e);
            }
            else
            {
                var groups = _groupBy == GroupBy.Source
                    ? filtered.GroupBy(e => string.IsNullOrEmpty(e.source) ? "(no source)" : e.source)
                    : filtered.GroupBy(e => e.type.ToString());

                foreach (var g in groups)
                {
                    string key = g.Key;
                    if (!_groupFoldouts.ContainsKey(key)) _groupFoldouts[key] = true;
                    _groupFoldouts[key] = EditorGUILayout.Foldout(_groupFoldouts[key], $"{key} ({g.Count()})");
                    if (_groupFoldouts[key])
                    {
                        EditorGUI.indentLevel++;
                        foreach (var e in g) DrawEntryRow(e);
                        EditorGUI.indentLevel--;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private bool MatchesFilters(IssueConsoleEntry e)
        {
            if (e.type == LogType.Error || e.type == LogType.Exception || e.type == LogType.Assert)
                if (!_filterErrors) return false;
            if (e.type == LogType.Warning)
                if (!_filterWarnings) return false;
            if (e.type == LogType.Log)
                if (!_filterInfo) return false;

            if (!string.IsNullOrEmpty(_search))
            {
                var s = _search.ToLowerInvariant();
                if (!(e.message != null && e.message.ToLowerInvariant().Contains(s)) &&
                    !(e.stackTrace != null && e.stackTrace.ToLowerInvariant().Contains(s)) &&
                    !(e.source != null && e.source.ToLowerInvariant().Contains(s)))
                    return false;
            }

            return true;
        }

        private void DrawSearchAndFilters()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("IssueSearchField");
            _search = EditorGUILayout.TextField(_search, GUILayout.MinWidth(200));
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                _search = "";
                GUI.FocusControl(null);
                Repaint();
            }

            _filterErrors = GUILayout.Toggle(_filterErrors, "Errors", EditorStyles.miniButton);
            _filterWarnings = GUILayout.Toggle(_filterWarnings, "Warnings", EditorStyles.miniButton);
            _filterInfo = GUILayout.Toggle(_filterInfo, "Info", EditorStyles.miniButton);

            GUILayout.FlexibleSpace();

            _groupBy = (GroupBy)EditorGUILayout.Popup((int)_groupBy, new string[] { "None", "Source", "Type" }, GUILayout.Width(120));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEntryRow(IssueConsoleEntry e)
        {
            EditorGUILayout.BeginHorizontal();
            bool selected = _selectedIds.Contains(e.id);
            bool newSel = EditorGUILayout.Toggle(selected, GUILayout.Width(18));
            if (newSel != selected)
            {
                if (newSel) _selectedIds.Add(e.id); else _selectedIds.Remove(e.id);
            }

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label) { richText = true };
            if (e.resolved) labelStyle.normal.textColor = Color.gray;

            Texture icon = EditorGUIUtility.IconContent(GetIconName(e.type)).image;
            GUILayout.Label(icon, GUILayout.Width(18), GUILayout.Height(18));

            EditorGUILayout.BeginVertical();
            // Make message clickable: ping/select referenced asset if available
            if (GUILayout.Button(e.message, labelStyle))
            {
                TryOpenReference(e);
            }
            if (!string.IsNullOrEmpty(e.stackTrace))
                EditorGUILayout.LabelField(e.stackTrace, new GUIStyle(EditorStyles.miniLabel) { richText = true });
            if (!string.IsNullOrEmpty(e.source))
                EditorGUILayout.LabelField($"Source: {e.source}", new GUIStyle(EditorStyles.miniLabel) { richText = true });
            if (e.occurrences > 1)
                EditorGUILayout.LabelField($"Occurrences: {e.occurrences}", new GUIStyle(EditorStyles.miniLabel) { richText = true }, GUILayout.Width(110));
            EditorGUILayout.LabelField($"First: {e.timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss}", new GUIStyle(EditorStyles.miniLabel) { richText = true }, GUILayout.Width(200));
            EditorGUILayout.LabelField($"Last: {e.lastSeen.ToLocalTime():yyyy-MM-dd HH:mm:ss}", new GUIStyle(EditorStyles.miniLabel) { richText = true }, GUILayout.Width(200));
            EditorGUILayout.EndVertical();

            if (!e.resolved)
            {
                if (GUILayout.Button("Resolve", GUILayout.Width(70)))
                {
                    ResolveEntries(new[] { e });
                }
            }
            else
            {
                GUILayout.Label("Resolved", GUILayout.Width(70));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void TryOpenReference(IssueConsoleEntry e)
        {
            // Try metadata first
            string assetPath = null;
            if (e.metadata != null && e.metadata.TryGetValue("assetPath", out assetPath))
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                    return;
                }
            }

            // Try parse from stack trace
            var path = ParseFirstAssetPath(e.stackTrace ?? e.message);
            if (!string.IsNullOrEmpty(path))
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(path);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                    return;
                }
            }

            // fallback: if no asset path found, try to open script (if stackTrace contains file:line)
            Debug.Log("Issue Console: Could not find referenced asset to open.");
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Resolve Selected", EditorStyles.toolbarButton))
            {
                var entries = _entries.Where(e => _selectedIds.Contains(e.id) && !e.resolved).ToList();
                ResolveEntries(entries);
            }

            if (GUILayout.Button("Prune Resolved", EditorStyles.toolbarButton))
            {
                _entries.RemoveAll(e => e.resolved);
                _selectedIds.RemoveWhere(id => !_entries.Any(e => e.id == id));
            }
            if (GUILayout.Button("Prune All", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to clear all entries?", "Yes", "No"))
                {
                    _entries.Clear();
                    _selectedIds.Clear();
                }
            }

            _showResolved = GUILayout.Toggle(_showResolved, "Show Resolved", EditorStyles.toolbarButton);

            if (GUILayout.Button("Clear Resolved State", EditorStyles.toolbarButton))
            {
                // unmark resolved state for entries that were persisted
                foreach (var e in _entries)
                {
                    if (_persistedResolvedIds.Contains(e.id)) e.resolved = false;
                }
                _persistedResolvedIds.Clear();
                SaveResolvedState();
            }

            GUILayout.FlexibleSpace();

            // Dynamic actions
            var selectedEntries = _entries.Where(e => _selectedIds.Contains(e.id)).ToList();
            foreach (var action in _actions)
            {
                bool can = action.CanHandle(selectedEntries);
                EditorGUI.BeginDisabledGroup(!can || selectedEntries.Count == 0);
                if (GUILayout.Button(action.Name, EditorStyles.toolbarButton))
                {
                    action.Execute(selectedEntries);
                }
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ResolveEntries(IEnumerable<IssueConsoleEntry> entries)
        {
            var arr = entries.ToArray();
            foreach (var e in arr) e.resolved = true;
            IssueConsoleAPI.Resolve(arr);
            foreach (var e in arr) _persistedResolvedIds.Add(e.id);
            SaveResolvedState();
            Repaint();
        }

        private string GetIconName(LogType t)
        {
            switch (t)
            {
                case LogType.Error: return "console.erroricon";
                case LogType.Assert: return "console.erroricon";
                case LogType.Exception: return "console.erroricon";
                case LogType.Warning: return "console.warnicon";
                default: return "console.infoicon";
            }
        }

        private void SaveResolvedState()
        {
            try
            {
                var wrapper = new ResolvedWrapper { ids = _persistedResolvedIds.ToList() };
                string json = JsonUtility.ToJson(wrapper);
                EditorPrefs.SetString(ResolvedIdsPrefKey, json);
            }
            catch { }
        }

        private void LoadResolvedState()
        {
            try
            {
                string json = EditorPrefs.GetString(ResolvedIdsPrefKey, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var wrapper = JsonUtility.FromJson<ResolvedWrapper>(json);
                    if (wrapper != null && wrapper.ids != null)
                        _persistedResolvedIds = new HashSet<string>(wrapper.ids);
                }
            }
            catch { }
        }

        [Serializable]
        private class ResolvedWrapper { public List<string> ids; }
    }
}
#endif
