using System;
using System.Collections.Generic;
using Yanki.Core.Events;
using Yanki.Core.Json;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Karar izini kanonik, kompakt JSON'a çevirir. Alan sırası ve sayı biçimi sabittir (aynı iz her zaman aynı metni verir);
    /// metin <c>karar_verildi</c> olayının <c>iz</c> yükünde taşınır.
    /// <para>
    /// Şema: <c>npc, tick, ayarlar{en_iyi_n, esik}, adaylar[], havuz[], zar, secilen, kaynak_olaylar[], kaynaksiz[]</c>. Aday durumu
    /// <c>elendi</c> (<c>neden</c>), <c>puanlandi</c> (<c>taban, olcutler[{alan, girdi, normal, egri, cikti}], kisilik, fikir, ruh_hali, puan</c>)
    /// ya da <c>puansiz</c> (puanlamayan seçiciler); ikisinde de <c>sira</c> ve <c>havuz</c> (top-N'de mi) vardır.
    /// </para>
    /// </summary>
    public static class KararIziYazici
    {
        /// <summary>İzi kompakt kanonik JSON olarak yazar.</summary>
        public static string Yaz(KararIzi iz) => JsonYazici.YazKisa(Agac(iz ?? throw new ArgumentNullException(nameof(iz))));

        /// <summary>
        /// İzi KISA kanonik JSON'a çevirir (günlükteki <c>karar_verildi</c> yükü, K3): seçilen, top-N havuzu ve puanları, zar, aday/elenen
        /// sayıları, kaynak olaylar ve kaynaksız girdiler. Bütün adaylar ve puan bileşenleri yazılmaz (aday sayısıyla büyümez); tam iz replay ile
        /// yeniden üretilir. Şema: <c>surum:"kisa", npc, tick, ayarlar, puanlanan, elenen, havuz[{aday, puan}], zar, secilen, kaynak_olaylar, kaynaksiz</c>.
        /// </summary>
        public static string YazKisa(KararIzi iz)
        {
            if (iz == null)
            {
                throw new ArgumentNullException(nameof(iz));
            }

            var puanlanan = 0;
            var elenen = 0;
            var puanlar = new SortedDictionary<string, decimal?>(StringComparer.Ordinal);
            foreach (var a in iz.Adaylar)
            {
                if (a.Elendi)
                {
                    elenen++;
                }
                else
                {
                    puanlanan++;
                    puanlar[a.Aday.Anahtar] = a.Puan?.Puan;
                }
            }

            var havuz = new List<JsonDegeri>(iz.Havuz.Count);
            foreach (var h in iz.Havuz)
            {
                puanlar.TryGetValue(h, out var puan);
                havuz.Add(JsonDegeri.Nesne(("aday", JsonDegeri.Metin(h)), ("puan", puan.HasValue ? JsonDegeri.Sayi(puan.Value) : JsonDegeri.Bos())));
            }

            var kaynaklar = new List<JsonDegeri>(iz.KaynakOlaylar.Count);
            foreach (var k in iz.KaynakOlaylar)
            {
                kaynaklar.Add(JsonDegeri.Sayi(k));
            }

            var kaynaksiz = new List<JsonDegeri>(iz.Kaynaksiz.Count);
            foreach (var k in iz.Kaynaksiz)
            {
                kaynaksiz.Add(JsonDegeri.Metin(k));
            }

            return JsonYazici.YazKisa(JsonDegeri.Nesne(
                ("surum", JsonDegeri.Metin(KisaIz.SurumEtiketi)),
                ("npc", JsonDegeri.Metin(iz.Npc)),
                ("tick", JsonDegeri.Sayi(iz.Tick)),
                ("ayarlar", JsonDegeri.Nesne(("en_iyi_n", JsonDegeri.Sayi(iz.Ayarlar.EnIyiN)), ("esik", JsonDegeri.Sayi(iz.Ayarlar.Esik)))),
                ("puanlanan", JsonDegeri.Sayi(puanlanan)),
                ("elenen", JsonDegeri.Sayi(elenen)),
                ("havuz", JsonDegeri.Dizi(havuz.ToArray())),
                ("zar", iz.Zar.HasValue ? JsonDegeri.Sayi(iz.Zar.Value) : JsonDegeri.Bos()),
                ("secilen", Secilen(iz)),
                ("kaynak_olaylar", JsonDegeri.Dizi(kaynaklar.ToArray())),
                ("kaynaksiz", JsonDegeri.Dizi(kaynaksiz.ToArray()))));
        }

        private static JsonDegeri Agac(KararIzi iz)
        {
            var adaylar = new List<JsonDegeri>(iz.Adaylar.Count);
            foreach (var a in iz.Adaylar)
            {
                adaylar.Add(Aday(a));
            }

            var havuz = new List<JsonDegeri>(iz.Havuz.Count);
            foreach (var h in iz.Havuz)
            {
                havuz.Add(JsonDegeri.Metin(h));
            }

            var kaynaklar = new List<JsonDegeri>(iz.KaynakOlaylar.Count);
            foreach (var k in iz.KaynakOlaylar)
            {
                kaynaklar.Add(JsonDegeri.Sayi(k));
            }

            var kaynaksiz = new List<JsonDegeri>(iz.Kaynaksiz.Count);
            foreach (var k in iz.Kaynaksiz)
            {
                kaynaksiz.Add(JsonDegeri.Metin(k));
            }

            return JsonDegeri.Nesne(
                ("npc", JsonDegeri.Metin(iz.Npc)),
                ("tick", JsonDegeri.Sayi(iz.Tick)),
                ("ayarlar", JsonDegeri.Nesne(("en_iyi_n", JsonDegeri.Sayi(iz.Ayarlar.EnIyiN)), ("esik", JsonDegeri.Sayi(iz.Ayarlar.Esik)))),
                ("adaylar", JsonDegeri.Dizi(adaylar.ToArray())),
                ("havuz", JsonDegeri.Dizi(havuz.ToArray())),
                ("zar", iz.Zar.HasValue ? JsonDegeri.Sayi(iz.Zar.Value) : JsonDegeri.Bos()),
                ("secilen", Secilen(iz)),
                ("kaynak_olaylar", JsonDegeri.Dizi(kaynaklar.ToArray())),
                ("kaynaksiz", JsonDegeri.Dizi(kaynaksiz.ToArray())));
        }

        private static JsonDegeri Secilen(KararIzi iz)
        {
            if (iz.Secilen == null)
            {
                return JsonDegeri.Bos();
            }

            var celisen = new List<JsonDegeri>(iz.CelisenOzellikler.Count);
            foreach (var c in iz.CelisenOzellikler)
            {
                celisen.Add(JsonDegeri.Metin(c));
            }

            return JsonDegeri.Nesne(
                ("eylem", JsonDegeri.Metin(iz.Secilen.Eylem.Id)),
                ("hedef", JsonDegeri.Metin(iz.Secilen.Hedef)),
                ("celisen_ozellikler", JsonDegeri.Dizi(celisen.ToArray())));
        }

        private static JsonDegeri Aday(AdayIzi a)
        {
            var ozellikler = new List<(string, JsonDegeri)>
            {
                ("eylem", JsonDegeri.Metin(a.Aday.Eylem.Id)),
                ("hedef", JsonDegeri.Metin(a.Aday.Hedef)),
            };

            if (a.Elendi)
            {
                ozellikler.Add(("durum", JsonDegeri.Metin("elendi")));
                ozellikler.Add(("neden", JsonDegeri.Metin(a.Neden)));
            }
            else if (a.Puan == null)
            {
                ozellikler.Add(("durum", JsonDegeri.Metin("puansiz")));
                ozellikler.Add(("sira", JsonDegeri.Sayi(a.Sira)));
                ozellikler.Add(("havuz", JsonDegeri.Mantiksal(a.HavuzdaMi)));
            }
            else
            {
                var olcutler = new List<JsonDegeri>(a.Puan.Olcutler.Count);
                foreach (var o in a.Puan.Olcutler)
                {
                    olcutler.Add(JsonDegeri.Nesne(
                        ("alan", JsonDegeri.Metin(o.Olcut.Alan.ToString())),
                        ("girdi", JsonDegeri.Sayi(o.Girdi)),
                        ("normal", JsonDegeri.Sayi(o.Normal)),
                        ("egri", Egri(o.Olcut.Egri)),
                        ("cikti", JsonDegeri.Sayi(o.Cikti))));
                }

                ozellikler.Add(("durum", JsonDegeri.Metin("puanlandi")));
                ozellikler.Add(("taban", JsonDegeri.Sayi(a.Puan.Taban)));
                ozellikler.Add(("olcutler", JsonDegeri.Dizi(olcutler.ToArray())));
                ozellikler.Add(("kisilik", JsonDegeri.Sayi(a.Puan.KisilikCarpani)));
                ozellikler.Add(("fikir", JsonDegeri.Sayi(a.Puan.FikirCarpani)));
                ozellikler.Add(("ruh_hali", JsonDegeri.Sayi(a.Puan.RuhHaliCarpani)));
                ozellikler.Add(("puan", JsonDegeri.Sayi(a.Puan.Puan)));
                ozellikler.Add(("sira", JsonDegeri.Sayi(a.Sira)));
                ozellikler.Add(("havuz", JsonDegeri.Mantiksal(a.HavuzdaMi)));
            }

            return JsonDegeri.Nesne(ozellikler.ToArray());
        }

        private static JsonDegeri Egri(EgriTanimi e)
        {
            var ad = JsonDegeri.Metin(EgriTurleri.Ad(e.Tur));
            switch (e.Tur)
            {
                case EgriTuru.Ustel:
                    return JsonDegeri.Nesne(("tur", ad), ("k", JsonDegeri.Sayi(e.K)));
                case EgriTuru.Lojistik:
                    return JsonDegeri.Nesne(("tur", ad), ("k", JsonDegeri.Sayi(e.K)), ("m", JsonDegeri.Sayi(e.M)));
                case EgriTuru.Basamak:
                    return JsonDegeri.Nesne(("tur", ad), ("esik", JsonDegeri.Sayi(e.M)));
                default:
                    return JsonDegeri.Nesne(("tur", ad));
            }
        }
    }

    /// <summary>
    /// <c>karar_verildi</c> olayı: KISA karar izini <c>iz</c> yükünde kanonik JSON olarak taşır (K3; tam iz replay ile yeniden üretilir). <c>KuralId</c> <c>karar</c>, fail karar veren NPC,
    /// <c>NedenOlaylar</c> izin zihin kaynaklarıdır. Bu olay algılanmaz, kural tetiklemez ve durum hash'ine girmez.
    /// Yükte ayrıca hızlı okuma için <c>secilen_eylem</c> ve <c>secilen_hedef</c> bulunur (seçim yoksa boş).
    /// </summary>
    public static class KararOlayi
    {
        /// <summary>Olayın <c>KuralId</c>'si.</summary>
        public const string KuralKimligi = "karar";

        /// <summary>Karar olayını kurar; günlüğe eklemek çağıranın işidir.</summary>
        public static Olay Yap(long id, long tick, int sira, KararIzi iz, string mekan)
        {
            if (iz == null)
            {
                throw new ArgumentNullException(nameof(iz));
            }

            return new Olay(
                id, tick, sira, YerlesikTurler.KararVerildi, iz.Npc, string.Empty, mekan ?? string.Empty,
                new[]
                {
                    new KeyValuePair<string, string>("iz", KararIziYazici.YazKisa(iz)),
                    new KeyValuePair<string, string>("secilen_eylem", iz.Secilen != null ? iz.Secilen.Eylem.Id : string.Empty),
                    new KeyValuePair<string, string>("secilen_hedef", iz.Secilen != null ? iz.Secilen.Hedef : string.Empty),
                },
                iz.KaynakOlaylar,
                KuralKimligi);
        }
    }
}
