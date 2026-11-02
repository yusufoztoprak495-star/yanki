using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Yanki.Core.Events;
using Yanki.Core.Json;
using Yanki.Core.World;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// <c>karar_verildi</c> olayının izini okunur satırlara çevirir (<c>yanki why</c>, <c>yanki query</c>): adaylar, puan bileşenleri
    /// (taban × ölçütler × kişilik × fikir × ruh hali), elenenler ve nedeni, top-N havuzu, zar, seçilen, çelişen özellikler, kaynak olaylar
    /// ve kaynağı bulunamayan girdiler. Kaynak olayın <c>iz</c> yükündeki kanonik JSON'dur; bellekteki nesneye bağlı değildir.
    /// </summary>
    public static class KararIziOzeti
    {
        /// <summary>
        /// Satırları döndürür (girinti çağıranın işidir). Olay karar olayı değilse boş liste; yük okunamazsa tek satırlık uyarı.
        /// </summary>
        public static IReadOnlyList<string> Satirlar(Olay karar)
        {
            if (karar == null)
            {
                throw new ArgumentNullException(nameof(karar));
            }

            var satirlar = new List<string>();
            if (karar.Tur != YerlesikTurler.KararVerildi)
            {
                return new ReadOnlyCollection<string>(satirlar);
            }

            try
            {
                Yaz(JsonAyristirici.Ayristir(karar.Deger("iz")), satirlar);
            }
            catch (Exception e) when (e is JsonHatasi || e is InvalidOperationException || e is FormatException || e is NullReferenceException)
            {
                satirlar.Clear();
                satirlar.Add("karar izi okunamadı (iz yükü geçersiz)");
            }

            return new ReadOnlyCollection<string>(satirlar);
        }

        private static void Yaz(JsonDegeri iz, List<string> satirlar)
        {
            var ayarlar = Al(iz, "ayarlar");
            satirlar.Add(
                $"karar izi: {Al(iz, "npc").MetinDeger}, tick {Say(Al(iz, "tick"))} (ilk {Say(Al(ayarlar, "en_iyi_n"))} aday, eşik {Say(Al(ayarlar, "esik"))})");

            var adaylar = Al(iz, "adaylar").Elemanlar;
            var secilen = Al(iz, "secilen");
            var zar = Al(iz, "zar");
            if (secilen.Tur == JsonTuru.Bos)
            {
                satirlar.Add("  seçilen: yok (puanı sıfırdan büyük aday yok)");
            }
            else
            {
                satirlar.Add($"  seçilen: {Ad(Al(secilen, "eylem").MetinDeger, Al(secilen, "hedef").MetinDeger)}"
                    + (zar.Tur == JsonTuru.Sayi ? $" (zar {Say(zar)})" : string.Empty));
            }

            var havuz = new List<string>();
            foreach (var h in Al(iz, "havuz").Elemanlar)
            {
                havuz.Add(AdayMetni(adaylar, h.MetinDeger));
            }

            if (havuz.Count > 0)
            {
                satirlar.Add("  havuz: " + string.Join(", ", havuz));
            }

            foreach (var a in adaylar)
            {
                satirlar.Add("  " + Aday(a));
            }

            if (secilen.Tur != JsonTuru.Bos)
            {
                var celisen = new List<string>();
                foreach (var c in Al(secilen, "celisen_ozellikler").Elemanlar)
                {
                    celisen.Add(c.MetinDeger);
                }

                if (celisen.Count > 0)
                {
                    satirlar.Add("  çelişen özellikler: " + string.Join(", ", celisen));
                }
            }

            var kaynaklar = new List<string>();
            foreach (var k in Al(iz, "kaynak_olaylar").Elemanlar)
            {
                kaynaklar.Add("#" + Say(k));
            }

            if (kaynaklar.Count > 0)
            {
                satirlar.Add("  kaynak olaylar: " + string.Join(", ", kaynaklar));
            }

            foreach (var k in Al(iz, "kaynaksiz").Elemanlar)
            {
                satirlar.Add("  kaynaksız girdi: " + k.MetinDeger);
            }
        }

        private static string Aday(JsonDegeri a)
        {
            var ad = Ad(Al(a, "eylem").MetinDeger, Al(a, "hedef").MetinDeger);
            var durum = Al(a, "durum").MetinDeger;
            if (durum == "elendi")
            {
                return $"aday {ad}: elendi — {Al(a, "neden").MetinDeger}";
            }

            var sb = new StringBuilder();
            sb.Append("aday ").Append(ad).Append(": ");
            if (durum == "puanlandi")
            {
                sb.Append("puan ").Append(Say(Al(a, "puan"))).Append(" = taban ").Append(Say(Al(a, "taban")));
                var olcutler = Al(a, "olcutler").Elemanlar;
                if (olcutler.Count > 0)
                {
                    sb.Append(" × ölçütler [");
                    for (var i = 0; i < olcutler.Count; i++)
                    {
                        var o = olcutler[i];
                        if (i > 0)
                        {
                            sb.Append("; ");
                        }

                        sb.Append(Al(o, "alan").MetinDeger).Append(": girdi ").Append(Say(Al(o, "girdi")))
                            .Append(" → normal ").Append(Say(Al(o, "normal")))
                            .Append(" → ").Append(Egri(Al(o, "egri")))
                            .Append(" çıktı ").Append(Say(Al(o, "cikti")));
                    }

                    sb.Append(']');
                }

                sb.Append(" × kişilik ").Append(Say(Al(a, "kisilik")))
                    .Append(" × fikir ").Append(Say(Al(a, "fikir")))
                    .Append(" × ruh hali ").Append(Say(Al(a, "ruh_hali")))
                    .Append("; ");
            }

            sb.Append("sıra ").Append(Say(Al(a, "sira")));
            if (Al(a, "havuz").MantiksalDeger)
            {
                sb.Append(", havuzda");
            }

            return sb.ToString();
        }

        private static string Egri(JsonDegeri e)
        {
            var tur = Al(e, "tur").MetinDeger;
            switch (tur)
            {
                case "ustel": return $"ustel(k={Say(Al(e, "k"))})";
                case "lojistik": return $"lojistik(k={Say(Al(e, "k"))}, m={Say(Al(e, "m"))})";
                case "basamak": return $"basamak(esik={Say(Al(e, "esik"))})";
                default: return tur;
            }
        }

        // "eylem → hedef" ya da hedefsizse "eylem".
        private static string Ad(string eylem, string hedef) => hedef.Length == 0 ? eylem : eylem + " → " + hedef;

        // Havuz anahtarı "eylem|hedef"; yanına puanı yazar.
        private static string AdayMetni(IReadOnlyList<JsonDegeri> adaylar, string anahtar)
        {
            foreach (var a in adaylar)
            {
                var eylem = Al(a, "eylem").MetinDeger;
                var hedef = Al(a, "hedef").MetinDeger;
                if (string.Equals(eylem + "|" + hedef, anahtar, StringComparison.Ordinal))
                {
                    var puan = a.Al("puan");
                    return Ad(eylem, hedef) + (puan != null ? " (" + Say(puan) + ")" : string.Empty);
                }
            }

            return anahtar;
        }

        private static JsonDegeri Al(JsonDegeri nesne, string anahtar) =>
            nesne.Al(anahtar) ?? throw new FormatException($"Karar izinde \"{anahtar}\" yok.");

        private static string Say(JsonDegeri d) => KanonikYazici.Sayi(d.SayiDeger).ToString(CultureInfo.InvariantCulture);
    }
}
