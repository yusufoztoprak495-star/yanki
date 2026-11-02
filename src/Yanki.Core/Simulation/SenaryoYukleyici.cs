using System;
using System.Collections.Generic;
using Yanki.Core.Json;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.World;

namespace Yanki.Core.Simulation
{
    /// <summary>
    /// Senaryo dosyasını (JSON metni) okur ve doğrular. Çekirdek dosya sistemine dokunmaz; metni ve kaynak adını çağıran verir.
    /// Biçim: <c>surum</c> (1), <c>ad</c>, <c>mekanlar</c>, <c>baglantilar</c> (çiftler), <c>oyuncu</c> (<c>id</c>, <c>konum</c>),
    /// <c>npcler</c> (<c>id</c>, <c>konum</c>, <c>kisilik</c>), <c>gercekler</c>, <c>olay_turleri</c>, <c>kural_dosyalari</c>.
    /// </summary>
    public static class SenaryoYukleyici
    {
        /// <summary>Desteklenen senaryo biçim sürümü.</summary>
        public const int Surum = 1;

        private static readonly string[] KokAnahtarlari =
        {
            "surum", "ad", "mekanlar", "baglantilar", "oyuncu", "npcler", "gercekler", "olay_turleri", "kural_dosyalari",
        };

        /// <summary>Senaryo metnini okur ve doğrular. Tüm hatalar toplanır.</summary>
        public static SenaryoYuklemeSonucu Yukle(string json, string kaynakAdi)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var hatalar = new List<KuralHatasi>();
            void Hata(int satir, string mesaj) => hatalar.Add(new KuralHatasi(kaynakAdi, satir, string.Empty, mesaj));

            JsonDegeri kok;
            try
            {
                kok = JsonAyristirici.Ayristir(json);
            }
            catch (JsonHatasi e)
            {
                Hata(e.Satir, "JSON okunamadı: " + e.Message);
                return new SenaryoYuklemeSonucu(null, hatalar);
            }

            if (kok.Tur != JsonTuru.Nesne)
            {
                Hata(kok.Satir, "Senaryo bir JSON nesnesi olmalı");
                return new SenaryoYuklemeSonucu(null, hatalar);
            }

            foreach (var kv in kok.Ozellikler)
            {
                if (Array.IndexOf(KokAnahtarlari, kv.Key) < 0)
                {
                    Hata(kv.Value.Satir, $"Senaryoda bilinmeyen alan \"{kv.Key}\"");
                }
            }

            var surum = kok.Al("surum");
            if (surum == null || surum.Tur != JsonTuru.Sayi || surum.SayiDeger != Surum)
            {
                Hata(surum?.Satir ?? kok.Satir, $"\"surum\" {Surum} olmalı");
            }

            var ad = kok.Al("ad");
            if (ad != null && ad.Tur != JsonTuru.Metin)
            {
                Hata(ad.Satir, "\"ad\" metin olmalı");
            }

            var mekanlar = new List<string>();
            var mekanKume = new SortedSet<string>(StringComparer.Ordinal);
            var tumKimlikler = new SortedSet<string>(StringComparer.Ordinal);
            var mekanDizisi = Dizi(kok, "mekanlar", true, Hata);
            if (mekanDizisi != null)
            {
                foreach (var m in mekanDizisi.Elemanlar)
                {
                    var id = KimlikOku(m, "mekan", Hata);
                    if (id == null)
                    {
                        continue;
                    }

                    if (!tumKimlikler.Add(id))
                    {
                        Hata(m.Satir, $"Kimlik tekrar ediyor: \"{id}\"");
                        continue;
                    }

                    mekanlar.Add(id);
                    mekanKume.Add(id);
                }
            }

            var baglantilar = new List<KeyValuePair<string, string>>();
            var baglantiDizisi = Dizi(kok, "baglantilar", false, Hata);
            if (baglantiDizisi != null)
            {
                foreach (var b in baglantiDizisi.Elemanlar)
                {
                    if (b.Tur != JsonTuru.Dizi || b.Elemanlar.Count != 2 || b.Elemanlar[0].Tur != JsonTuru.Metin || b.Elemanlar[1].Tur != JsonTuru.Metin)
                    {
                        Hata(b.Satir, "Bağlantı iki mekan kimliğinden oluşan bir dizi olmalı: [\"a\", \"b\"]");
                        continue;
                    }

                    var a = b.Elemanlar[0].MetinDeger;
                    var c = b.Elemanlar[1].MetinDeger;
                    if (!mekanKume.Contains(a) || !mekanKume.Contains(c))
                    {
                        Hata(b.Satir, $"Bağlantıdaki mekan tanımlı değil: \"{(mekanKume.Contains(a) ? c : a)}\"");
                        continue;
                    }

                    if (string.Equals(a, c, StringComparison.Ordinal))
                    {
                        Hata(b.Satir, $"Mekan kendisine bağlanamaz: \"{a}\"");
                        continue;
                    }

                    baglantilar.Add(new KeyValuePair<string, string>(a, c));
                }
            }

            var oyuncuId = string.Empty;
            var oyuncuKonumu = string.Empty;
            var oyuncu = kok.Al("oyuncu");
            if (oyuncu == null)
            {
                Hata(kok.Satir, "\"oyuncu\" nesnesi gerekli (id, konum)");
            }
            else if (oyuncu.Tur != JsonTuru.Nesne)
            {
                Hata(oyuncu.Satir, "\"oyuncu\" bir nesne olmalı (id, konum)");
            }
            else
            {
                AnahtarDenetle(oyuncu, new[] { "id", "konum" }, "oyuncu", Hata);
                oyuncuId = KimlikAlan(oyuncu, "id", "oyuncu", Hata) ?? string.Empty;
                if (oyuncuId.Length > 0 && !tumKimlikler.Add(oyuncuId))
                {
                    Hata(oyuncu.Satir, $"Kimlik tekrar ediyor: \"{oyuncuId}\"");
                }

                oyuncuKonumu = KonumAlan(oyuncu, mekanKume, "oyuncu", Hata) ?? string.Empty;
            }

            var npcler = new List<NpcTanimi>();
            var npcDizisi = Dizi(kok, "npcler", true, Hata);
            if (npcDizisi != null)
            {
                foreach (var n in npcDizisi.Elemanlar)
                {
                    var npc = NpcOku(n, mekanKume, tumKimlikler, Hata);
                    if (npc != null)
                    {
                        npcler.Add(npc);
                    }
                }
            }

            var gercekler = new List<string>();
            var gercekDizisi = Dizi(kok, "gercekler", false, Hata);
            if (gercekDizisi != null)
            {
                foreach (var g in gercekDizisi.Elemanlar)
                {
                    if (g.Tur != JsonTuru.Metin)
                    {
                        Hata(g.Satir, "Gerçek metin olmalı");
                        continue;
                    }

                    try
                    {
                        gercekler.Add(Gercek.Ayristir(g.MetinDeger).ToString());
                    }
                    catch (ArgumentException e)
                    {
                        Hata(g.Satir, $"Geçersiz başlangıç gerçeği \"{g.MetinDeger}\": {e.Message}");
                    }
                }
            }

            var turler = new List<string>();
            var turDizisi = Dizi(kok, "olay_turleri", true, Hata);
            if (turDizisi != null)
            {
                var yerlesik = new SortedSet<string>(KuralYukleyici.YerlesikOlayTurleri, StringComparer.Ordinal);
                var gorulen = new SortedSet<string>(StringComparer.Ordinal);
                foreach (var t in turDizisi.Elemanlar)
                {
                    var id = KimlikOku(t, "olay türü", Hata);
                    if (id == null)
                    {
                        continue;
                    }

                    if (yerlesik.Contains(id))
                    {
                        Hata(t.Satir, $"Olay türü çekirdek olayıyla çakışıyor: \"{id}\"");
                    }
                    else if (!gorulen.Add(id))
                    {
                        Hata(t.Satir, $"Olay türü tekrar ediyor: \"{id}\"");
                    }
                    else
                    {
                        turler.Add(id);
                    }
                }
            }

            var dosyalar = new List<string>();
            var dosyaDizisi = Dizi(kok, "kural_dosyalari", false, Hata);
            if (dosyaDizisi != null)
            {
                foreach (var d in dosyaDizisi.Elemanlar)
                {
                    if (d.Tur != JsonTuru.Metin || d.MetinDeger.Length == 0)
                    {
                        Hata(d.Satir, "Kural dosyası adı boş olmayan metin olmalı");
                    }
                    else
                    {
                        dosyalar.Add(d.MetinDeger);
                    }
                }
            }

            if (hatalar.Count > 0)
            {
                return new SenaryoYuklemeSonucu(null, hatalar);
            }

            return new SenaryoYuklemeSonucu(
                new SenaryoTanimi(ad?.MetinDeger ?? string.Empty, mekanlar, baglantilar, oyuncuId, oyuncuKonumu, npcler, gercekler, turler, dosyalar),
                hatalar);
        }

        private static NpcTanimi? NpcOku(JsonDegeri n, SortedSet<string> mekanlar, SortedSet<string> kimlikler, Action<int, string> hata)
        {
            if (n.Tur != JsonTuru.Nesne)
            {
                hata(n.Satir, "NPC bir nesne olmalı (id, konum, kisilik)");
                return null;
            }

            AnahtarDenetle(n, new[] { "id", "konum", "kisilik" }, "npc", hata);
            var id = KimlikAlan(n, "id", "npc", hata);
            if (id != null && !kimlikler.Add(id))
            {
                hata(n.Satir, $"Kimlik tekrar ediyor: \"{id}\"");
                id = null;
            }

            var konum = KonumAlan(n, mekanlar, "npc " + (id ?? "?"), hata);
            var kisilikDugumu = n.Al("kisilik");
            if (kisilikDugumu == null || kisilikDugumu.Tur != JsonTuru.Nesne)
            {
                hata(kisilikDugumu?.Satir ?? n.Satir, $"npc {id ?? "?"}: \"kisilik\" nesnesi gerekli");
                return null;
            }

            var degerler = new SortedDictionary<string, decimal>(StringComparer.Ordinal);
            var tamam = true;
            foreach (var kv in kisilikDugumu.Ozellikler)
            {
                if (Array.IndexOf(new List<string>(Kisilik.Ozellikler).ToArray(), kv.Key) < 0)
                {
                    hata(kv.Value.Satir, $"npc {id ?? "?"}: bilinmeyen kişilik özelliği \"{kv.Key}\"");
                    tamam = false;
                }
                else if (kv.Value.Tur != JsonTuru.Sayi)
                {
                    hata(kv.Value.Satir, $"npc {id ?? "?"}: \"{kv.Key}\" sayı olmalı");
                    tamam = false;
                }
                else if (kv.Value.SayiDeger < 0m || kv.Value.SayiDeger > 1m)
                {
                    hata(kv.Value.Satir, $"npc {id ?? "?"}: \"{kv.Key}\" 0..1 aralığında olmalı");
                    tamam = false;
                }
                else
                {
                    degerler[kv.Key] = kv.Value.SayiDeger;
                }
            }

            foreach (var ozellik in Kisilik.Ozellikler)
            {
                if (!degerler.ContainsKey(ozellik) && tamam)
                {
                    hata(kisilikDugumu.Satir, $"npc {id ?? "?"}: \"{ozellik}\" kişilik özelliği eksik");
                    tamam = false;
                }
            }

            if (id == null || konum == null || !tamam)
            {
                return null;
            }

            return new NpcTanimi(
                id,
                konum,
                new Kisilik(
                    degerler["acgozluluk"], degerler["durustluk"], degerler["cesaret"], degerler["sosyallik"], degerler["kin_tutma"], degerler["merhamet"]));
        }

        private static JsonDegeri? Dizi(JsonDegeri kok, string anahtar, bool zorunlu, Action<int, string> hata)
        {
            var d = kok.Al(anahtar);
            if (d == null)
            {
                if (zorunlu)
                {
                    hata(kok.Satir, $"\"{anahtar}\" dizisi gerekli");
                }

                return null;
            }

            if (d.Tur != JsonTuru.Dizi)
            {
                hata(d.Satir, $"\"{anahtar}\" bir dizi olmalı");
                return null;
            }

            return d;
        }

        private static string? KimlikOku(JsonDegeri d, string ne, Action<int, string> hata)
        {
            if (d.Tur != JsonTuru.Metin || !Kimlik.GecerliKimlikMi(d.MetinDeger))
            {
                var deger = d.Tur == JsonTuru.Metin ? d.MetinDeger : "(metin değil)";
                hata(d.Satir, $"Geçersiz {ne} kimliği: \"{deger}\" (ASCII harf/rakam/_, harf ya da _ ile başlar)");
                return null;
            }

            return d.MetinDeger;
        }

        private static string? KimlikAlan(JsonDegeri nesne, string anahtar, string ne, Action<int, string> hata)
        {
            var d = nesne.Al(anahtar);
            if (d == null)
            {
                hata(nesne.Satir, $"{ne}: \"{anahtar}\" alanı gerekli");
                return null;
            }

            return KimlikOku(d, ne, hata);
        }

        private static string? KonumAlan(JsonDegeri nesne, SortedSet<string> mekanlar, string ne, Action<int, string> hata)
        {
            var d = nesne.Al("konum");
            if (d == null || d.Tur != JsonTuru.Metin)
            {
                hata(d?.Satir ?? nesne.Satir, $"{ne}: metin olarak \"konum\" alanı gerekli");
                return null;
            }

            if (!mekanlar.Contains(d.MetinDeger))
            {
                hata(d.Satir, $"{ne}: konum tanımlı bir mekan değil: \"{d.MetinDeger}\"");
                return null;
            }

            return d.MetinDeger;
        }

        private static void AnahtarDenetle(JsonDegeri nesne, string[] izinli, string ne, Action<int, string> hata)
        {
            foreach (var kv in nesne.Ozellikler)
            {
                if (Array.IndexOf(izinli, kv.Key) < 0)
                {
                    hata(kv.Value.Satir, $"{ne}: bilinmeyen alan \"{kv.Key}\" ({string.Join(", ", izinli)})");
                }
            }
        }
    }
}
