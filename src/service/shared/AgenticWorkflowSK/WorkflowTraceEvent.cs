
namespace AgenticWorkflowSK
{
    public class WorkflowTraceEvent
    {
        private const string Prefix = "TRACE_";
        private const string Infix = "_To_";
        private const string IdInfix = "_Id_";
        private const string Suffix = "_Event";

        public static string CreateTraceEventName(string from, string to, string id)
        {
            return $"{Prefix}{from}{Infix}{to}{IdInfix}{id}{Suffix}";
        }

        public static bool IsTraceEvent(string eventName)
        {
            return eventName != null &&
                   eventName.StartsWith(Prefix) &&
                   eventName.EndsWith(Suffix);
        }
        public static (string from, string to, string id)? ParseFromToId(string eventName)
        {
            if (!IsTraceEvent(eventName))
                return null;

            // Expected format: TRACE_{from}_To_{to}_Id_{id}_Event

            int fromStart = Prefix.Length;
            int fromEnd = eventName.IndexOf(Infix, fromStart, StringComparison.Ordinal);
            if (fromEnd < 0) return null;

            int toStart = fromEnd + Infix.Length;
            int idInfixStart = eventName.IndexOf(IdInfix, toStart, StringComparison.Ordinal);
            if (idInfixStart < 0) return null;

            string from = eventName.Substring(fromStart, fromEnd - fromStart);
            string to = eventName.Substring(toStart, idInfixStart - toStart);

            int idStart = idInfixStart + IdInfix.Length;
            int idEnd = eventName.LastIndexOf(Suffix, StringComparison.Ordinal);
            if (idEnd < 0 || idEnd <= idStart) return null;

            string id = eventName.Substring(idStart, idEnd - idStart);

            return (from, to, id);
        }
    }
}
