namespace DodgeFromLight.Infrastructure.Maps
{
    public sealed class LegacyMapImportIssue
    {
        public LegacyMapImportIssueSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public string SourceLabel { get; }

        /// <summary>
        /// Describes one warning or error discovered while importing legacy data.
        /// </summary>
        public LegacyMapImportIssue(
            LegacyMapImportIssueSeverity severity,
            string code,
            string message,
            string sourceLabel = "")
        {
            // Store machine-readable and user-readable diagnostics together.
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            SourceLabel = sourceLabel ?? string.Empty;
        }
    }
}
