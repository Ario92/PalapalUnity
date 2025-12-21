using System;
using System.Collections.Generic;
using UnityEngine;

namespace Palapal.IssueConsole.Runtime
{
    // Actions that can be registered and shown in the editor UI
    public interface IIssueAction
    {
        string Name { get; }
        bool CanHandle(IEnumerable<IssueConsoleEntry> entries);
        void Execute(IEnumerable<IssueConsoleEntry> entries);
    }

    public static class IssueConsoleAPI
    {
        public static event Action<IssueConsoleEntry> OnEntryReported;
        public static event Action<IEnumerable<IssueConsoleEntry>> OnEntriesResolved;
        public static event Action<IIssueAction> OnActionRegistered;

        private static readonly List<IIssueAction> _registeredActions = new List<IIssueAction>();

        public static IReadOnlyList<IIssueAction> RegisteredActions => _registeredActions.AsReadOnly();

        // Other packages can call this to add issues to the Issue Console
        public static void Report(string message, string stackTrace = null, LogType type = LogType.Warning, string source = null, Dictionary<string, string> metadata = null)
        {
            var entry = new IssueConsoleEntry
            {
                message = message,
                stackTrace = stackTrace,
                type = type,
                source = source,
                metadata = metadata ?? new Dictionary<string, string>()
            };

            OnEntryReported?.Invoke(entry);
        }

        public static void Resolve(IEnumerable<IssueConsoleEntry> entries)
        {
            OnEntriesResolved?.Invoke(entries);
        }

        public static void RegisterAction(IIssueAction action)
        {
            if (action == null) return;
            _registeredActions.Add(action);
            OnActionRegistered?.Invoke(action);
        }
    }
}
