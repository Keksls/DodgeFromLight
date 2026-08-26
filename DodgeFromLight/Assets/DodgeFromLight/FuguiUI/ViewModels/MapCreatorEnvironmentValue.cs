namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Describes one bounded numeric environment control.
    /// </summary>
    public sealed class MapCreatorEnvironmentValue
    {
        public string Id { get; }
        public string Label { get; }
        public string Section { get; }
        public float Value { get; }
        public float Minimum { get; }
        public float Maximum { get; }
        public string Format { get; }

        /// <summary>
        /// Creates one immutable numeric environment control.
        /// </summary>
        public MapCreatorEnvironmentValue(
            string id,
            string label,
            string section,
            float value,
            float minimum,
            float maximum,
            string format)
        {
            // Bounds and formatting travel with the value so the presenter contains no environment rules.
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
            Section = section ?? string.Empty;
            Value = value;
            Minimum = minimum;
            Maximum = maximum;
            Format = string.IsNullOrWhiteSpace(format)
                ? "0.00"
                : format;
        }
    }
}
