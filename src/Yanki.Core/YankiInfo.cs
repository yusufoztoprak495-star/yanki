namespace Yanki.Core
{
    /// <summary>
    /// Kütüphane hakkında temel bilgiler. 1. hafta iskeletinin tek gerçek tipi;
    /// modüller (Events, World, Mind, Perception, Decision, Rules) 2. haftadan itibaren eklenir.
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
