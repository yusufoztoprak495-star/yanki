using System;

namespace Yanki.Core.Mind
{
    /// <summary>
    /// Zihin hesaplarının ayarlanabilir sayıları ve varsayılanları (plan 5.4). Bütün değerler Yusuf'un denge alanıdır;
    /// varsayılanlar öneridir ve <c>docs/notes/zihin.md</c> "Açık noktalar" bölümünde listelidir.
    /// Sönme oranları tick başınadır ve doğrusaldır (<c>değer − oran × geçenTick</c>, alt sınır 0).
    /// </summary>
    public sealed class ZihinAyarlari
    {
        /// <summary>Fikir hesabında <c>guven</c> ekseninin ağırlığı.</summary>
        public decimal EksenAgirligiGuven { get; init; } = 0.4m;

        /// <summary>Fikir hesabında <c>sevgi</c> ekseninin ağırlığı.</summary>
        public decimal EksenAgirligiSevgi { get; init; } = 0.4m;

        /// <summary>Fikir hesabında <c>korku</c> ekseninin ağırlığı (korku fikri düşürür).</summary>
        public decimal EksenAgirligiKorku { get; init; } = -0.2m;

        /// <summary>Aynı türden en fazla kaç modifier kopyası fikre girer.</summary>
        public int ModifierBirikmeSiniri { get; init; } = 3;

        /// <summary>n'inci kopyanın etkisi <c>deger × ModifierKopyaCarpani^(n−1)</c>.</summary>
        public decimal ModifierKopyaCarpani { get; init; } = 0.9m;

        /// <summary>Duygu şiddetinin tick başına sönmesi (0..1 ölçeğinde).</summary>
        public decimal DuyguSonumuTickBasina { get; init; } = 0.05m;

        /// <summary>Stresin tick başına sönmesi (0..100 ölçeğinde); duygudan yavaştır.</summary>
        public decimal StresSonumuTickBasina { get; init; } = 0.2m;

        /// <summary>Kanıt gücünün tick başına zayıflaması (0..1 ölçeğinde).</summary>
        public decimal KanitSonumuTickBasina { get; init; } = 0.001m;

        /// <summary>Kalıcı olmayan hafızanın önem değerinin tick başına silikleşmesi.</summary>
        public decimal HafizaSonumuTickBasina { get; init; } = 0.002m;

        /// <summary>Önemi bu değerin üstünde olan hafıza kalıcıdır, silikleşmez.</summary>
        public decimal HafizaKaliciEsigi { get; init; } = 0.7m;

        /// <summary>Silikleşen hafızanın önemi bu değere ya da altına inince sorgulardan çıkar.</summary>
        public decimal HafizaUnutmaEsigi { get; init; } = 0.1m;

        /// <summary>Ayarların tutarlılığını denetler.</summary>
        /// <exception cref="ArgumentException">Sınır ya da oran geçersizse.</exception>
        public void Dogrula()
        {
            if (ModifierBirikmeSiniri < 1)
            {
                throw new ArgumentException("ModifierBirikmeSiniri en az 1 olmalı.");
            }

            if (ModifierKopyaCarpani <= 0m || ModifierKopyaCarpani > 1m)
            {
                throw new ArgumentException("ModifierKopyaCarpani (0, 1] aralığında olmalı.");
            }

            if (DuyguSonumuTickBasina < 0m || StresSonumuTickBasina < 0m || KanitSonumuTickBasina < 0m || HafizaSonumuTickBasina < 0m)
            {
                throw new ArgumentException("Sönme oranları negatif olamaz.");
            }

            if (HafizaKaliciEsigi < 0m || HafizaKaliciEsigi > 1m || HafizaUnutmaEsigi < 0m || HafizaUnutmaEsigi > 1m)
            {
                throw new ArgumentException("Hafıza eşikleri 0..1 aralığında olmalı.");
            }
        }
    }
}
