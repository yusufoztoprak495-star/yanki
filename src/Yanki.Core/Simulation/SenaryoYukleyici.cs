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
    /// <c>npcler</c> (<c>id</c>, <c>konum</c>, <c>kisilik</c>), <c>gercekler</c>, <c>olay_turleri</c>, <c>kural_dosyalari</c>, <c>eylem_dosyalari</c> ve <c>kriz_dosyalari</c> (isteğe bağlı).
    /// </summary>
    public static class SenaryoYukleyici
    {
        /// <summary>Desteklenen senaryo biçim sürümü.</summary>
        public const int Surum = 1;

        private static readonly string[] KokAnahtarlari =
        {
            "surum", "ad", "mekanlar", "baglantilar", "oyuncu", "npcler", "gercekler", "olay_turleri", "kural_dosyalari", "eylem_dosyalari", "kriz_dosyalari", "frenler", "korumali", "oyuncu_ihlalleri", "yay_degiskenleri",
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

                    if (yerlesik.Contains(id) || Decision.YerlesikTurler.AyrilmisMi(id))
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

            var eylemDosyalari = new List<string>();
            var eylemDizisi = Dizi(kok, "eylem_dosyalari", false, Hata);
            if (eylemDizisi != null)
            {
                foreach (var d in eylemDizisi.Elemanlar)
                {
                    if (d.Tur != JsonTuru.Metin || d.MetinDeger.Length == 0)
                    {
                        Hata(d.Satir, "Eylem dosyası adı boş olmayan metin olmalı");
                    }
                    else
                    {
                        eylemDosyalari.Add(d.MetinDeger);
                    }
                }
            }

            var krizDosyalari = new List<string>();
            var krizDizisi = Dizi(kok, "kriz_dosyalari", false, Hata);
            if (krizDizisi != null)
            {
                foreach (var d in krizDizisi.Elemanlar)
                {
                    if (d.Tur != JsonTuru.Metin || d.MetinDeger.Length == 0)
                    {
                        Hata(d.Satir, "Kriz dosyası adı boş olmayan metin olmalı");
                    }
                    else
                    {
                        krizDosyalari.Add(d.MetinDeger);
                    }
                }
            }

            if (hatalar.Count > 0)
            {
                return new SenaryoYuklemeSonucu(null, hatalar);
            }

            Brakes.GenelFrenler? genelFrenler = null;
            var frenDugumu = kok.Al("frenler");
            if (frenDugumu != null)
            {
                if (frenDugumu.Tur != JsonTuru.Nesne)
                {
                    Hata(frenDugumu.Satir, "\"frenler\" bir nesne olmalı (tick_basina_en_fazla_sonuc)");
                }
                else
                {
                    int? m = null;
                    foreach (var kv in frenDugumu.Ozellikler)
                    {
                        if (kv.Key != "tick_basina_en_fazla_sonuc")
                        {
                            Hata(kv.Value.Satir, $"\"frenler\" içinde bilinmeyen alan \"{kv.Key}\" (tick_basina_en_fazla_sonuc)");
                        }
                        else if (kv.Value.Tur != JsonTuru.Sayi || kv.Value.SayiDeger < 1m || kv.Value.SayiDeger != decimal.Truncate(kv.Value.SayiDeger) || kv.Value.SayiDeger > 1000000m)
                        {
                            Hata(kv.Value.Satir, "\"tick_basina_en_fazla_sonuc\" 1 ile 1000000 arasında bir tam sayı olmalı");
                        }
                        else
                        {
                            m = (int)kv.Value.SayiDeger;
                        }
                    }

                    genelFrenler = m == null ? null : new Brakes.GenelFrenler(m);
                }
            }

            var korumaGirdisi = new List<KeyValuePair<string, IEnumerable<string>>>();
            var korumaDugumu = kok.Al("korumali");
            if (korumaDugumu != null)
            {
                if (korumaDugumu.Tur != JsonTuru.Nesne)
                {
                    Hata(korumaDugumu.Satir, "\"korumali\" bir nesne olmalı: { \"Varlik\": [\"oldurulemez\", ...] }");
                }
                else
                {
                    foreach (var kv in korumaDugumu.Ozellikler)
                    {
                        if (!Kimlik.GecerliKimlikMi(kv.Key))
                        {
                            Hata(kv.Value.Satir, $"korumali: geçersiz varlık kimliği \"{kv.Key}\"");
                            continue;
                        }

                        var bayraklar = new List<string>();
                        if (kv.Value.Tur != JsonTuru.Dizi || kv.Value.Elemanlar.Count == 0)
                        {
                            Hata(kv.Value.Satir, $"korumali \"{kv.Key}\": boş olmayan bayrak dizisi bekleniyor (oldurulemez, calinamaz, mekandan_ayrilamaz)");
                            continue;
                        }

                        foreach (var b in kv.Value.Elemanlar)
                        {
                            if (b.Tur != JsonTuru.Metin || !Brakes.KorumaBayraklari.Gecerli(b.MetinDeger))
                            {
                                Hata(b.Satir, $"korumali \"{kv.Key}\": bilinmeyen bayrak (oldurulemez, calinamaz, mekandan_ayrilamaz)");
                            }
                            else
                            {
                                bayraklar.Add(b.MetinDeger);
                            }
                        }

                        korumaGirdisi.Add(new KeyValuePair<string, IEnumerable<string>>(kv.Key, bayraklar));
                    }
                }
            }

            var oyuncuIhlalleri = new List<KeyValuePair<string, IReadOnlyList<Brakes.IhlalBayragi>>>();
            var oiDugumu = kok.Al("oyuncu_ihlalleri");
            if (oiDugumu != null)
            {
                if (oiDugumu.Tur != JsonTuru.Nesne)
                {
                    Hata(oiDugumu.Satir, "\"oyuncu_ihlalleri\" bir nesne olmalı: { \"eylem_turu\": [ { \"bayrak\", \"varlik\" } ] }");
                }
                else
                {
                    foreach (var kv in oiDugumu.Ozellikler)
                    {
                        var sarma = JsonDegeri.Nesne(("oyuncu_ihlalleri." + kv.Key, kv.Value));
                        oyuncuIhlalleri.Add(new KeyValuePair<string, IReadOnlyList<Brakes.IhlalBayragi>>(
                            kv.Key, Brakes.IhlalBayragi.OkuDizi(sarma, "oyuncu_ihlalleri." + kv.Key, new[] { "ben", "eylem.hedef" }, Hata)));
                    }
                }
            }

            var yayTanimlari = new List<Brakes.YayTanimi>();
            var yayDugumu = kok.Al("yay_degiskenleri");
            if (yayDugumu != null)
            {
                if (yayDugumu.Tur != JsonTuru.Nesne)
                {
                    Hata(yayDugumu.Satir, "\"yay_degiskenleri\" bir nesne olmalı: { \"ad\": { \"baslangic\", \"min\", \"max\" } }");
                }
                else
                {
                    foreach (var kv in yayDugumu.Ozellikler)
                    {
                        var d = kv.Value;
                        if (!Kimlik.GecerliKimlikMi(kv.Key))
                        {
                            Hata(d.Satir, $"yay_degiskenleri: geçersiz değişken adı \"{kv.Key}\"");
                            continue;
                        }

                        decimal? Say(string anahtar)
                        {
                            var n = d.Tur == JsonTuru.Nesne ? d.Al(anahtar) : null;
                            if (n == null || n.Tur != JsonTuru.Sayi)
                            {
                                Hata(n?.Satir ?? d.Satir, $"yay_degiskenleri \"{kv.Key}\": \"{anahtar}\" sayı olmalı");
                                return null;
                            }

                            return n.SayiDeger;
                        }

                        var b = Say("baslangic");
                        var mn = Say("min");
                        var mx = Say("max");
                        if (d.Tur == JsonTuru.Nesne)
                        {
                            foreach (var alan in d.Ozellikler)
                            {
                                if (alan.Key != "baslangic" && alan.Key != "min" && alan.Key != "max")
                                {
                                    Hata(alan.Value.Satir, $"yay_degiskenleri \"{kv.Key}\": bilinmeyen alan \"{alan.Key}\" (baslangic, min, max)");
                                }
                            }
                        }

                        if (b.HasValue && mn.HasValue && mx.HasValue)
                        {
                            if (mn.Value > mx.Value || b.Value < mn.Value || b.Value > mx.Value)
                            {
                                Hata(d.Satir, $"yay_degiskenleri \"{kv.Key}\": min ≤ baslangic ≤ max olmalı");
                            }
                            else
                            {
                                yayTanimlari.Add(new Brakes.YayTanimi(kv.Key, b.Value, mn.Value, mx.Value));
                            }
                        }
                    }
                }
            }

            return new SenaryoYuklemeSonucu(
                new SenaryoTanimi(ad?.MetinDeger ?? string.Empty, mekanlar, baglantilar, oyuncuId, oyuncuKonumu, npcler, gercekler, turler, dosyalar, eylemDosyalari, krizDosyalari, genelFrenler, new Brakes.Korumalilik(korumaGirdisi), oyuncuIhlalleri, yayTanimlari),
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
