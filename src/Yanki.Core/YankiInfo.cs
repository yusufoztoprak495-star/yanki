namespace Yanki.Core
{
    /// <summary>
    /// Kütüphane hakkında temel bilgiler. Modüller (Events, World, Criteria 2. haftada; Mind, Perception,
    /// Decision, Rules sonraki haftalarda) kendi ad alanlarında yer alır.
    /// </summary>
    public static class YankiInfo
    {
        /// <summary>Kütüphanenin adı.</summary>
        public const string Name = "Yankı";

        /// <summary>
        /// Olay günlüğü biçiminin sürümü. Kayıtlı bir günlüğün hangi kurallarla
        /// yeniden oynatılacağını belirler; biçim değişirse artırılır (ADR-001).
        /// </summary>
        public const int EventLogFormatVersion = 1;
    }
}
