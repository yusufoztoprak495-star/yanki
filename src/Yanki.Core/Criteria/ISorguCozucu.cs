namespace Yanki.Core.Criteria
{
    /// <summary>
    /// Sorgu fonksiyonu çağrılarını gerçek değerlere çözer. Dünya tabanlı fonksiyonları
    /// <see cref="DunyaSorguCozucusu"/>, zihin tabanlı olanları (3. hafta) başka çözücüler sağlar.
    /// </summary>
    public interface ISorguCozucu
    {
        /// <summary>
        /// Doğrulanmış bir çağrıyı verilen bağlam için çözer. Döndürülen değerin türü, fonksiyonun
        /// <see cref="FonksiyonTanimi.Donus"/> türüyle uyumlu olmalıdır (sayı, mantıksal ya da varlık/bant için metin).
        /// </summary>
        SorguDegeri Coz(SorguCagrisi cagri, SorguBaglami baglam);
    }
}
