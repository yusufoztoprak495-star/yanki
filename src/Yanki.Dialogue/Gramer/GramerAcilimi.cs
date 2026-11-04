using System;
using System.Collections.Generic;
using Yanki.Core.World;

namespace Yanki.Dialogue.Gramer
{
    /// <summary>Metin açılımında bağlanan bir olgu: ya bir ad (ek alabilir) ya da düz metin (ek almaz).</summary>
    internal sealed class MetinOlgusu
    {
        private MetinOlgusu(string deger, bool ad, bool ozelAd)
        {
            Deger = deger;
            AdMi = ad;
            OzelAd = ozelAd;
        }

        public string Deger { get; }

        /// <summary>Ek alabilen ad mı (kişi adı, cins isim).</summary>
        public bool AdMi { get; }

        /// <summary>Özel ad ise ekten önce kesme işareti konur (Ayşe'ye); cins isimde konmaz (hancıya).</summary>
        public bool OzelAd { get; }

        public static MetinOlgusu Ad(string deger, bool ozelAd) => new MetinOlgusu(deger, true, ozelAd);

        public static MetinOlgusu Metin(string deger) => new MetinOlgusu(deger, false, false);
    }

    /// <summary>Gramer kuralları: kural adı → seçenek metinleri (her biri kural ve olgu etiketleri içerebilir). Kural adlarında nokta yoktur.</summary>
    internal sealed class GramerTablosu
    {
        public const int EnFazlaSeviye = 3;

        private readonly SortedDictionary<string, List<string>> _kurallar = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Adlar => _kurallar.Keys;

        public bool Var(string kural) => _kurallar.ContainsKey(kural);

        public void Ekle(string kural, IEnumerable<string> secenekler)
        {
            _kurallar[kural] = new List<string>(secenekler);
        }

        public IReadOnlyList<string> Secenekler(string kural) => _kurallar[kural];

        /// <summary>
        /// Kök kurallardan erişilen kuralları denetler: döngü, 3 seviyeden fazla iç içe açılım ve tanımsız sembol. <paramref name="olgular"/> bağlanabilen olgu adlarıdır
        /// (konusan, dinleyen, hafiza.zaman ...). Hata metinleri dosya/satır içermez; çağıran ekler.
        /// </summary>
        public IReadOnlyList<string> Analiz(IEnumerable<string> kokler, ICollection<string>? olgular = null)
        {
            var hatalar = new List<string>();
            var derinlik = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var yol = new List<string>();
            foreach (var kok in kokler)
            {
                if (_kurallar.ContainsKey(kok))
                {
                    Gez(kok, olgular, derinlik, yol, hatalar);
                }
            }

            return hatalar;
        }

        private int Gez(string kural, ICollection<string>? olgular, SortedDictionary<string, int> derinlik, List<string> yol, List<string> hatalar)
        {
            if (yol.Contains(kural))
            {
                var bas = yol.IndexOf(kural);
                var dongu = new List<string>(yol.GetRange(bas, yol.Count - bas)) { kural };
                var metin = "döngü: " + string.Join(" → ", dongu);
                if (!hatalar.Contains(metin))
                {
                    hatalar.Add(metin);
                }

                return 1;
            }

            if (derinlik.TryGetValue(kural, out var onceki))
            {
                return onceki;
            }

            yol.Add(kural);
            var en = 0;
            foreach (var secenek in _kurallar[kural])
            {
                IReadOnlyList<Etiket> etiketler;
                try
                {
                    etiketler = GramerAcilimi.Etiketler(secenek);
                }
                catch (FormatException e)
                {
                    hatalar.Add($"kural \"{kural}\": {e.Message}");
                    continue;
                }

                foreach (var etiket in etiketler)
                {
                    var cozum = GramerAcilimi.Cozumle(etiket.Ham, olgular, this);
                    if (cozum.Tur == EtiketTuru.Tanimsiz)
                    {
                        hatalar.Add($"kural \"{kural}\": tanımsız sembol \"#{etiket.Ham}#\"");
                    }
                    else if (cozum.Tur == EtiketTuru.Kural)
                    {
                        en = Math.Max(en, Gez(cozum.Ad, olgular, derinlik, yol, hatalar));
                    }
                }
            }

            yol.RemoveAt(yol.Count - 1);
            var d = 1 + en;
            derinlik[kural] = d;
            if (d > EnFazlaSeviye)
            {
                hatalar.Add($"kural \"{kural}\" {d} seviye iç içe açılıyor (en fazla {EnFazlaSeviye} seviye)");
            }

            return d;
        }
    }

    internal enum EtiketTuru
    {
        Tanimsiz,
        Olgu,
        Kural,
    }

    internal sealed class Etiket
    {
        public Etiket(string ham, int baslangic, int uzunluk)
        {
            Ham = ham;
            Baslangic = baslangic;
            Uzunluk = uzunluk;
        }

        public string Ham { get; }

        public int Baslangic { get; }

        public int Uzunluk { get; }
    }

    internal sealed class EtiketCozumu
    {
        public EtiketCozumu(EtiketTuru tur, string ad, IReadOnlyList<string> ekler)
        {
            Tur = tur;
            Ad = ad;
            Ekler = ekler;
        }

        public EtiketTuru Tur { get; }

        public string Ad { get; }

        public IReadOnlyList<string> Ekler { get; }
    }

    /// <summary>
    /// Tracery benzeri küçük metin açıcı: bir kural etiketi gramer kuralını (seçenek anahtarlı RNG ile seçilir), bir olgu etiketi bağlı olguyu, olgu.ek etiketi olguya Türkçe ek
    /// getirir. Olgu adı nokta içerebilir (hafiza.zaman): etiketin en uzun öneki olgu adıdır, kalanı ektir. Rastgelelik yalnızca verilen <see cref="AnahtarliRng"/>'den gelir.
    /// </summary>
    internal static class GramerAcilimi
    {
        public static IReadOnlyList<Etiket> Etiketler(string metin)
        {
            var sonuc = new List<Etiket>();
            var i = 0;
            while (i < metin.Length)
            {
                if (metin[i] != '#')
                {
                    i++;
                    continue;
                }

                var kapanis = metin.IndexOf('#', i + 1);
                if (kapanis < 0)
                {
                    throw new FormatException($"kapanmamış '#' (konum {i + 1}): \"{metin}\"");
                }

                var ham = metin.Substring(i + 1, kapanis - i - 1);
                if (ham.Length == 0)
                {
                    throw new FormatException($"boş etiket '##' (konum {i + 1}): \"{metin}\"");
                }

                sonuc.Add(new Etiket(ham, i, kapanis - i + 1));
                i = kapanis + 1;
            }

            return sonuc;
        }

        public static EtiketCozumu Cozumle(string ham, ICollection<string>? olgular, GramerTablosu tablo)
        {
            var parcalar = ham.Split('.');
            if (olgular != null)
            {
                for (var k = parcalar.Length; k >= 1; k--)
                {
                    var ad = string.Join(".", parcalar, 0, k);
                    if (olgular.Contains(ad))
                    {
                        var ekler = new string[parcalar.Length - k];
                        Array.Copy(parcalar, k, ekler, 0, ekler.Length);
                        return new EtiketCozumu(EtiketTuru.Olgu, ad, ekler);
                    }
                }
            }

            if (tablo.Var(parcalar[0]))
            {
                var ekler = new string[parcalar.Length - 1];
                Array.Copy(parcalar, 1, ekler, 0, ekler.Length);
                return new EtiketCozumu(EtiketTuru.Kural, parcalar[0], ekler);
            }

            return new EtiketCozumu(EtiketTuru.Tanimsiz, ham, new string[0]);
        }

        public static string Ac(string metin, GramerTablosu tablo, IReadOnlyDictionary<string, MetinOlgusu> olgular, AnahtarliRng rng) =>
            Ac(metin, tablo, olgular, rng, 0);

        private static string Ac(string metin, GramerTablosu tablo, IReadOnlyDictionary<string, MetinOlgusu> olgular, AnahtarliRng rng, int seviye)
        {
            var etiketler = Etiketler(metin);
            if (etiketler.Count == 0)
            {
                return metin;
            }

            var adlar = new List<string>(olgular.Keys);
            var sb = new System.Text.StringBuilder();
            var son = 0;
            foreach (var e in etiketler)
            {
                sb.Append(metin, son, e.Baslangic - son);
                son = e.Baslangic + e.Uzunluk;
                var cozum = Cozumle(e.Ham, adlar, tablo);
                switch (cozum.Tur)
                {
                    case EtiketTuru.Olgu:
                        sb.Append(OlguMetni(e.Ham, olgular[cozum.Ad], cozum.Ekler));
                        break;
                    case EtiketTuru.Kural:
                        if (cozum.Ekler.Count > 0)
                        {
                            throw new InvalidOperationException($"#{e.Ham}#: gramer kuralına ek getirilmez (yalnızca ad olgularına: {string.Join(", ", TurkceEkler.Ekler)}).");
                        }

                        if (seviye >= GramerTablosu.EnFazlaSeviye)
                        {
                            throw new InvalidOperationException($"#{e.Ham}#: iç içe açılım {GramerTablosu.EnFazlaSeviye} seviyeyi aşıyor.");
                        }

                        var secenekler = tablo.Secenekler(cozum.Ad);
                        var secim = secenekler.Count > 1 ? rng.Aralik(0, secenekler.Count) : 0;
                        sb.Append(Ac(secenekler[secim], tablo, olgular, rng, seviye + 1));
                        break;
                    default:
                        throw new InvalidOperationException($"Tanımsız sembol \"#{e.Ham}#\".");
                }
            }

            sb.Append(metin, son, metin.Length - son);
            return sb.ToString();
        }

        private static string OlguMetni(string ham, MetinOlgusu olgu, IReadOnlyList<string> ekler)
        {
            if (ekler.Count == 0)
            {
                return olgu.Deger;
            }

            if (!olgu.AdMi)
            {
                throw new InvalidOperationException($"#{ham}#: metin olgusuna ek getirilmez (yalnızca ad olgularına).");
            }

            if (ekler.Count != 1 || Array.IndexOf(TurkceEkler.Ekler, ekler[0]) < 0)
            {
                throw new InvalidOperationException($"#{ham}#: bilinmeyen ya da birden çok ek (geçerli: {string.Join(", ", TurkceEkler.Ekler)}).");
            }

            try
            {
                return TurkceEkler.Uygula(olgu.Deger, olgu.OzelAd, ekler[0]);
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException($"#{ham}#: {e.Message}");
            }
        }
    }
}
