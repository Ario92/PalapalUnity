using System;
using System.Collections.Generic;
using UnityEngine;

namespace Palapal.IssueConsole.Runtime
{
    [Serializable]
    public class IssueConsoleEntry
    {
        public string id;
        public string message;
        public string stackTrace;
        public LogType type;
        public string source; // optional source/package identifier
        public bool resolved;
        public DateTime timestamp;
        public DateTime lastSeen;
        public int occurrences;
        public Dictionary<string, string> metadata;

        public IssueConsoleEntry()
        {
            id = Guid.NewGuid().ToString();
            timestamp = DateTime.UtcNow;
            lastSeen = timestamp;
            occurrences = 1;
            metadata = new Dictionary<string, string>();
        }
    }
}
