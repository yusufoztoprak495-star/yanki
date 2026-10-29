using System;
using Yanki.Core.World;

namespace Yanki.Core.Criteria
{
    /// <summary>Kriterin karşılaştırma operatörü.</summary>
    public enum KriterOperatoru
    {
        /// <summary><c>&lt;</c></summary>
        Kucuk,

        /// <summary><c>&lt;=</c></summary>
        KucukEsit,

        /// <summary><c>&gt;</c></summary>
        Buyuk,

        /// <summary><c>&gt;=</c></summary>
        BuyukEsit,

        /// <summary><c>==</c></summary>
        Esit,

        /// <summary><c>!=</c></summary>
        EsitDegil,

        /// <summary><c>aralik [a, b]</c>: uçlar dahil.</summary>
        Aralik,
    }

    /// <summary>
    /// Ağırlıklı tek kriter (Valve tarzı, plan 5.9): bir sorgu çağrısı, operatör, değer, ağırlık ve zorunluluk.
    /// Kriterler C# ile kurulur; JSON okuma bu hafta yoktur.
    /// </summary>
    public sealed class Kriter
    {
        /// <summary>Ayrıştırılmış ve doğrulanmış sorgu çağrısı.</summary>
        public SorguCagrisi Alan { get; }

        /// <summary>Karşılaştırma operatörü.</summary>
        public KriterOperatoru Op { get; }

        /// <summary>Karşılaştırılacak değer.</summary>
        public SorguDegeri Deger { get; }

        /// <summary>Kriter tutarsa puana eklenen değer.</summary>
        public decimal Agirlik { get; }

        /// <summary>Tutmazsa adayı eleyen kriter mi.</summary>
        public bool Zorunlu { get; }

        private Kriter(SorguCagrisi alan, KriterOperatoru op, SorguDegeri deger, decimal agirlik, bool zorunlu)
        {
            Alan = alan;
            Op = op;
            Deger = deger;
            Agirlik = agirlik;
            Zorunlu = zorunlu;
        }

        /// <summary>
        /// Kriteri kurar ve doğrular: <paramref name="alan"/> ayrıştırılır ve sözlüğe göre denetlenir;
        /// operatör ve değer, fonksiyonun dönüş türüyle uyumlu olmalıdır.
        /// </summary>
        /// <exception cref="KriterHatasi">İfade, operatör ya da değer geçersizse.</exception>
        public static Kriter Olustur(string alan, KriterOperatoru op, SorguDegeri deger, decimal agirlik = 1m, bool zorunlu = false)
        {
            var cagri = IfadeAyristirici.Ayristir(alan);
            SorguSozlugu.Dogrula(cagri);
            var tanim = SorguSozlugu.Bul(cagri.Ad)!;
            TurDogrula(tanim, op, deger);
            return new Kriter(cagri, op, deger, agirlik, zorunlu);
        }

        /// <summary>Operatörün kriter dilindeki yazımı.</summary>
        public static string OperatorMetni(KriterOperatoru op)
        {
            switch (op)
            {
                case KriterOperatoru.Kucuk: return "<";
                case KriterOperatoru.KucukEsit: return "<=";
                case KriterOperatoru.Buyuk: return ">";
                case KriterOperatoru.BuyukEsit: return ">=";
                case KriterOperatoru.Esit: return "==";
                case KriterOperatoru.EsitDegil: return "!=";
                case KriterOperatoru.Aralik: return "aralik";
                default: throw new ArgumentOutOfRangeException(nameof(op), "Tanımsız operatör: " + (int)op);
            }
        }

        /// <summary>Okunur yazım: <c>fikir(dinleyen) &lt; -10</c>.</summary>
        public override string ToString() => Alan + " " + OperatorMetni(Op) + " " + Deger;

        private static string TurAdi(DonusTuru tur)
        {
            switch (tur)
            {
                case DonusTuru.Sayi: return "sayı";
                case DonusTuru.Mantiksal: return "mantıksal";
                case DonusTuru.Varlik: return "varlık";
                default: return "bant";
            }
        }

        private static void TurDogrula(FonksiyonTanimi tanim, KriterOperatoru op, SorguDegeri deger)
        {
            if (!Enum.IsDefined(typeof(KriterOperatoru), op))
            {
                throw new KriterHatasi($"Tanımsız operatör değeri: {(int)op}");
            }

            var opMetni = OperatorMetni(op);
            var tur = TurAdi(tanim.Donus);

            if (tanim.Donus == DonusTuru.Sayi)
            {
                if (op == KriterOperatoru.Aralik)
                {
                    if (deger.Tur != DegerTuru.Aralik)
                    {
                        throw new KriterHatasi($"'{tanim.Ad}' ({tur}) için '{opMetni}' operatörü bir aralık değeri ister, {deger.Tur} verildi");
                    }
                }
                else if (deger.Tur != DegerTuru.Sayi)
                {
                    throw new KriterHatasi($"'{tanim.Ad}' ({tur}) için '{opMetni}' operatörü bir sayı değeri ister, {deger.Tur} verildi");
                }

                return;
            }

            if (op != KriterOperatoru.Esit && op != KriterOperatoru.EsitDegil)
            {
                throw new KriterHatasi($"'{tanim.Ad}' {tur} değer döndürür; '{opMetni}' operatörü yalnızca sayı dönüşlerinde kullanılabilir (== ve != geçerli)");
            }

            if (tanim.Donus == DonusTuru.Mantiksal)
            {
                if (deger.Tur != DegerTuru.Mantiksal)
                {
                    throw new KriterHatasi($"'{tanim.Ad}' ({tur}) için değer true/false olmalı, {deger.Tur} verildi");
                }

                return;
            }

            if (deger.Tur != DegerTuru.Metin)
            {
                throw new KriterHatasi($"'{tanim.Ad}' ({tur}) için değer metin olmalı, {deger.Tur} verildi");
            }

            if (tanim.Donus == DonusTuru.Bant)
            {
                if (!Contains(SorguSozlugu.Bantlar, deger.MetinDegeri))
                {
                    throw new KriterHatasi($"'{deger.MetinDegeri}' geçerli bir bant değil (dusman, soguk, notr, yakin, dost)");
                }

                return;
            }

            if (!RolAdlari.TryCoz(deger.MetinDegeri, out _) && !Kimlik.GecerliKimlikMi(deger.MetinDegeri))
            {
                throw new KriterHatasi($"'{deger.MetinDegeri}' geçerli bir varlık kimliği ya da rol değil");
            }
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<string> liste, string deger)
        {
            foreach (var s in liste)
            {
                if (string.Equals(s, deger, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
