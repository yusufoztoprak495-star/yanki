using System;
using System.Collections.Generic;
using Yanki.Core.Criteria;
using Yanki.Core.Decision;
using Yanki.Core.Json;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Dialogue;

namespace Yanki.Authoring
{
    /// <summary>
    /// Plan 8 / plan 10: her kişilik özelliği en az bir eylem ölçütünde ve en az bir diyalog kriterinde kullanılmalı; mekaniği olmayan süs özellik olmaz.
    /// Eylem tarafı: <c>on_kosullar</c>, <c>olcutler</c> ve <c>kisilik_carpani</c>. Diyalog tarafı yalnızca şablon dosyası verilmişse denetlenir.
    /// İki ayrı uyarı: "eylem ölçütünde yok", "diyalog kriterinde yok".
    /// </summary>
    internal static class KisilikKullanimi
    {
        public static void Denetle(SenaryoBilgisi bilgi, SablonDosyasi? sablon, DogrulamaGirdisi girdi, List<Bulgu> bulgular)
        {
            var eylemde = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var e in bilgi.Eylemler)
            {
                foreach (var k in e.OnKosullar)
                {
                    KisilikTopla(k.Alan, eylemde);
                }

                foreach (var o in e.Olcutler)
                {
                    KisilikTopla(o.Alan, eylemde);
                }

                if (e.KisilikCarpani != null)
                {
                    foreach (var o in e.KisilikCarpani.Ozellikler)
                    {
                        eylemde.Add(o.Ozellik);
                    }
                }
            }

            var diyalogda = new SortedSet<string>(StringComparer.Ordinal);
            var sablonDosyasiVar = girdi.Sablon != null;
            if (sablon != null)
            {
                foreach (var s in sablon.Sablonlar)
                {
                    foreach (var k in s.Kriterler)
                    {
                        KisilikTopla(k.Alan, diyalogda);
                    }
                }
            }

            var satir = SenaryoDogrulayici.SatirBul(girdi.SenaryoMetni, "\"npcler\"");
            foreach (var ozellik in Kisilik.Ozellikler)
            {
                if (!eylemde.Contains(ozellik))
                {
                    bulgular.Add(new Bulgu(girdi.SenaryoAdi, satir, false, "kisilik", ozellik, "kişilik özelliği hiçbir eylem ölçütünde (on_kosullar, olcutler, kisilik_carpani) kullanılmıyor"));
                }

                // Şablon dosyası hiç verilmediyse ya da yüklenemediyse diyalog tarafı bilinemez; uyarı yalnızca yüklenmiş dosyada çıkar.
                if (sablonDosyasiVar && sablon != null && !diyalogda.Contains(ozellik))
                {
                    bulgular.Add(new Bulgu(girdi.Sablon!.GorunenAd, 1, false, "kisilik", ozellik, "kişilik özelliği hiçbir diyalog kriterinde kullanılmıyor"));
                }
            }
        }

        private static void KisilikTopla(SorguCagrisi cagri, SortedSet<string> kume)
        {
            if (cagri.Ad == "kisilik" && cagri.Argumanlar.Count == 1 && cagri.Argumanlar[0] is TanimlayiciArgumani t)
            {
                kume.Add(t.Ad);
            }
        }
    }

    /// <summary>7. hafta Minor 8: <c>korumali</c> anahtarı senaryoda hiçbir NPC'de, mekanda, oyuncuda ya da başlangıç gerçeğinde geçmiyorsa uyarı (yazım hatası hiçbir şeyi korumaz).</summary>
    internal static class KorumaliDenetimi
    {
        public static void Denetle(SenaryoBilgisi bilgi, DogrulamaGirdisi girdi, List<Bulgu> bulgular)
        {
            var kimlikler = bilgi.Kimlikler();
            var satir = SenaryoDogrulayici.SatirBul(girdi.SenaryoMetni, "\"korumali\"");
            foreach (var v in bilgi.Senaryo.Korumali.Varliklar)
            {
                if (!kimlikler.Contains(v))
                {
                    bulgular.Add(new Bulgu(
                        girdi.SenaryoAdi, satir, false, "korumali", v,
                        "korumalı kimlik senaryoda hiçbir NPC'de, mekanda, oyuncuda ya da başlangıç gerçeğinde geçmiyor (yazım hatası olabilir)"));
                }
            }
        }
    }

    /// <summary>
    /// "Hiç ulaşılamayacak şablon" (plan 8), yalnızca STATİK tanım (koşuya bakan "hiç seçilmedi" denetimi coverage'ındır):
    /// (1) concept hiçbir eşlemede ve ulaşılabilir bir şablonun followup'ında yok; (2) <c>konu_deseni</c> senaryodaki hiçbir eylemin
    /// <c>konu_desenleri</c>/<c>yalan_onermeleri</c> ile ve menüdeki önermelerle örtüşemez; (3) kriteri senaryoda olmayan sabit kimlik okuyor;
    /// (4) <c>sonra</c> hedefi senaryoda olmayan kimlik; (5) yalnızca ulaşılamaz bir şablonun followup'ı olarak var.
    /// </summary>
    internal static class SablonUlasilabilirligi
    {
        // Kişi bekleyen argümanın indeksi (sözlük tanımından): fikir(0), eksen(0), modifier_var(0), ayni_mekanda(0), yalan_soyledi(0), kaynak(1), kok(1), hafiza_var(1).
        private static int KisiIndeksi(string ad)
        {
            switch (ad)
            {
                case "fikir":
                case "eksen":
                case "modifier_var":
                case "ayni_mekanda":
                case "yalan_soyledi":
                    return 0;
                case "kaynak":
                case "kok":
                case "hafiza_var":
                    return 1;
                default:
                    return -1;
            }
        }

        public static void Denetle(SenaryoBilgisi bilgi, SablonDosyasi dosya, string gorunenAd, List<Bulgu> bulgular)
        {
            var kimlikler = bilgi.Kimlikler();
            var npcler = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var n in bilgi.Senaryo.Npcler)
            {
                npcler.Add(n.Id);
            }

            var eslemeNiyetleri = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var e in dosya.Esleme.Hepsi)
            {
                eslemeNiyetleri.Add(e.Niyet);
            }

            var konuHavuzu = new List<string>();
            foreach (var e in bilgi.Eylemler)
            {
                foreach (var d in e.KonuDesenleri)
                {
                    konuHavuzu.Add(d.ToString());
                }

                foreach (var y in e.YalanOnermeleri)
                {
                    konuHavuzu.Add(y);
                }
            }

            konuHavuzu.AddRange(MenuDenetimi.MenuOnermeleri(bilgi.Girdi.Menu));

            // Her şablon için şablonun KENDİSİNDEKİ engel (concept dışında): konu deseni, sabit kimlik, followup hedefi.
            var kendiNedeni = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var s in dosya.Sablonlar)
            {
                var neden = KendiNedeni(s, kimlikler, npcler, konuHavuzu);
                if (neden != null)
                {
                    kendiNedeni[s.Id] = neden;
                }
            }

            // Ulaşılabilir concept'ler: eşlemedeki niyetler + ulaşılabilir (kendi engeli olmayan, concept'i ulaşılabilir) şablonların followup concept'i.
            var ulasilabilir = new SortedSet<string>(eslemeNiyetleri, StringComparer.Ordinal);
            var degisti = true;
            while (degisti)
            {
                degisti = false;
                foreach (var s in dosya.Sablonlar)
                {
                    if (s.Sonra != null && ulasilabilir.Contains(s.Concept) && !kendiNedeni.ContainsKey(s.Id) && ulasilabilir.Add(s.Sonra.Concept))
                    {
                        degisti = true;
                    }
                }
            }

            foreach (var s in dosya.Sablonlar)
            {
                string? mesaj = null;
                if (!ulasilabilir.Contains(s.Concept))
                {
                    mesaj = eslemeNiyetleri.Contains(s.Concept) ? null : "concept \"" + s.Concept + "\" hiçbir eşlemede ve ulaşılabilir bir şablonun followup'ında yok";
                }
                else if (kendiNedeni.TryGetValue(s.Id, out var neden))
                {
                    mesaj = neden;
                }

                if (mesaj != null)
                {
                    bulgular.Add(new Bulgu(gorunenAd, s.Satir, false, "sablon", s.Id, "şablon hiç ulaşılamaz: " + mesaj));
                }
            }
        }

        private static string? KendiNedeni(Sablon s, SortedSet<string> kimlikler, SortedSet<string> npcler, List<string> konuHavuzu)
        {
            if (s.KonuDeseni != null && !HavuzdaOrtusen(s.KonuDeseni.ToString(), konuHavuzu))
            {
                return "konu_deseni \"" + s.KonuDeseni + "\" senaryodaki hiçbir eylemin konu_desenleri/yalan_onermeleri ve menü önermeleriyle örtüşmez";
            }

            foreach (var k in s.Kriterler)
            {
                var idx = KisiIndeksi(k.Alan.Ad);
                if (idx >= 0 && idx < k.Alan.Argumanlar.Count && k.Alan.Argumanlar[idx] is TanimlayiciArgumani t && !kimlikler.Contains(t.Ad))
                {
                    return "kriter senaryoda olmayan \"" + t.Ad + "\" kimliğini okuyor";
                }
            }

            if (s.Sonra != null && s.Sonra.Hedef != "dinleyen" && !npcler.Contains(s.Sonra.Hedef))
            {
                return "sonra hedefi \"" + s.Sonra.Hedef + "\" senaryoda yok";
            }

            return null;
        }

        private static bool HavuzdaOrtusen(string desen, List<string> havuz)
        {
            foreach (var h in havuz)
            {
                if (Ortusur(desen, h))
                {
                    return true;
                }
            }

            return false;
        }

        // İki desen (joker '*' her argümanda) aynı yüklem, aynı olumsuzluk ve aynı sayıda argümanla, her konumda eşit ya da biri joker ise örtüşür.
        internal static bool Ortusur(string a, string b)
        {
            if (!Coz(a, out var olumsuzA, out var yuklemA, out var argA) || !Coz(b, out var olumsuzB, out var yuklemB, out var argB))
            {
                return true;   // ayrıştırılamayan havuz girdisi yüzünden yanlış uyarı verme
            }

            if (olumsuzA != olumsuzB || !string.Equals(yuklemA, yuklemB, StringComparison.Ordinal) || argA.Length != argB.Length)
            {
                return false;
            }

            for (var i = 0; i < argA.Length; i++)
            {
                if (argA[i] != "*" && argB[i] != "*" && !string.Equals(argA[i], argB[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Coz(string metin, out bool olumsuz, out string yuklem, out string[] arg)
        {
            olumsuz = false;
            yuklem = string.Empty;
            arg = new string[0];
            var m = metin.Trim();
            if (m.StartsWith("degil(", StringComparison.Ordinal) && m.EndsWith(")", StringComparison.Ordinal))
            {
                olumsuz = true;
                m = m.Substring(6, m.Length - 7);
            }

            var ac = m.IndexOf('(');
            if (ac <= 0 || !m.EndsWith(")", StringComparison.Ordinal))
            {
                return false;
            }

            yuklem = m.Substring(0, ac);
            var ic = m.Substring(ac + 1, m.Length - ac - 2);
            var parcalar = ic.Length == 0 ? new string[0] : ic.Split(',');
            arg = new string[parcalar.Length];
            for (var i = 0; i < parcalar.Length; i++)
            {
                arg[i] = parcalar[i].Trim();
            }

            return true;
        }
    }

    /// <summary>
    /// Oyuncu eylem menüsü (<c>oyuncu_eylemleri.json</c>, M kararı): çekirdek şeması ve senaryo_hash değişmez; her girdinin <c>Submit</c>'te geçerli olduğu
    /// gerçek <see cref="Simulasyon.Submit"/> çağrısıyla denetlenir. Menü 10 girdiyi geçemez; beş ailenin eksiği uyarıdır.
    /// </summary>
    internal static class MenuDenetimi
    {
        public const int EnFazlaGirdi = 10;

        private static readonly string[] Aileler = { "konus", "bilgi_ver", "sakla", "iyilik", "kotuluk" };

        /// <summary>Menüdeki <c>yuk.onerme</c> değerleri (okunamayan menü için boş).</summary>
        public static List<string> MenuOnermeleri(DosyaGirdisi? menu)
        {
            var sonuc = new List<string>();
            if (menu == null)
            {
                return sonuc;
            }

            try
            {
                var girdiler = JsonAyristirici.Ayristir(menu.Metin).Al("menu");
                if (girdiler == null || girdiler.Tur != JsonTuru.Dizi)
                {
                    return sonuc;
                }

                foreach (var g in girdiler.Elemanlar)
                {
                    var onerme = g.Tur == JsonTuru.Nesne ? g.Al("yuk")?.Al("onerme") : null;
                    if (onerme != null && onerme.Tur == JsonTuru.Metin)
                    {
                        sonuc.Add(onerme.MetinDeger);
                    }
                }
            }
            catch (JsonHatasi)
            {
            }

            return sonuc;
        }

        public static void Denetle(
            DogrulamaGirdisi girdi, SenaryoTanimi senaryo, IReadOnlyList<KuralTanimi> kurallar, EylemKatalogu? katalog, IReadOnlyList<KrizSatiri> krizler,
            bool simulasyonKurulabilir, List<Bulgu> bulgular)
        {
            var dosya = girdi.Menu!;
            void Hata(int satir, string kimlik, string mesaj) => bulgular.Add(new Bulgu(dosya.GorunenAd, satir, true, "menu", kimlik, mesaj));

            JsonDegeri kok;
            try
            {
                kok = JsonAyristirici.Ayristir(dosya.Metin);
            }
            catch (JsonHatasi e)
            {
                Hata(1, string.Empty, e.Message);
                return;
            }

            var menu = kok.Tur == JsonTuru.Nesne ? kok.Al("menu") : null;
            if (menu == null || menu.Tur != JsonTuru.Dizi)
            {
                Hata(kok.Satir, string.Empty, "\"menu\" bir dizi olmalı: { \"surum\": 1, \"menu\": [ { id, aile, tur, hedef_turu, ... } ] }");
                return;
            }

            if (menu.Elemanlar.Count > EnFazlaGirdi)
            {
                Hata(menu.Satir, string.Empty, "menü " + EnFazlaGirdi.ToString(System.Globalization.CultureInfo.InvariantCulture) + " girdiyi geçemez (" + menu.Elemanlar.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " var)");
            }

            var gorulenAileler = new SortedSet<string>(StringComparer.Ordinal);
            var kimlikler = new SortedSet<string>(StringComparer.Ordinal);
            var gecerliTurler = new SortedSet<string>(senaryo.OlayTurleri, StringComparer.Ordinal) { YerlesikTurler.Anlatti, YerlesikTurler.OyuncuHareketi };
            foreach (var g in menu.Elemanlar)
            {
                var id = g.Tur == JsonTuru.Nesne && g.Al("id")?.Tur == JsonTuru.Metin ? g.Al("id")!.MetinDeger : string.Empty;
                if (g.Tur != JsonTuru.Nesne)
                {
                    Hata(g.Satir, id, "menü girdisi bir nesne olmalı");
                    continue;
                }

                var dogru = true;
                if (id.Length == 0)
                {
                    Hata(g.Satir, id, "\"id\" gerekli");
                    dogru = false;
                }
                else if (!kimlikler.Add(id))
                {
                    Hata(g.Satir, id, "menü kimliği tekrar ediyor");
                    dogru = false;
                }

                foreach (var o in g.Ozellikler)
                {
                    if (Array.IndexOf(new[] { "id", "aile", "tur", "hedef_turu", "hedef", "yuk", "metin" }, o.Key) < 0)
                    {
                        Hata(o.Value.Satir, id, "bilinmeyen alan \"" + o.Key + "\" (id, aile, tur, hedef_turu, hedef, yuk, metin)");
                        dogru = false;
                    }
                }

                var aile = Metin(g, "aile");
                if (Array.IndexOf(Aileler, aile) < 0)
                {
                    Hata(g.Satir, id, "\"aile\" " + string.Join(", ", Aileler) + " değerlerinden biri olmalı");
                    dogru = false;
                }
                else
                {
                    gorulenAileler.Add(aile);
                }

                var tur = Metin(g, "tur");
                if (!gecerliTurler.Contains(tur))
                {
                    Hata(g.Satir, id, "\"tur\" \"" + tur + "\" senaryoda tanımlı değil (olay_turleri, anlatti, hareket)");
                    dogru = false;
                }

                var hedefTuru = Metin(g, "hedef_turu");
                if (hedefTuru != "yok" && hedefTuru != "npc" && hedefTuru != "mekan")
                {
                    Hata(g.Satir, id, "\"hedef_turu\" yok, npc ya da mekan olmalı");
                    dogru = false;
                }

                if (Metin(g, "metin").Length == 0)
                {
                    Hata(g.Satir, id, "\"metin\" (görünen metin) gerekli");
                    dogru = false;
                }

                var yuk = new List<KeyValuePair<string, string>>();
                var yukDugumu = g.Al("yuk");
                if (yukDugumu != null)
                {
                    if (yukDugumu.Tur != JsonTuru.Nesne)
                    {
                        Hata(yukDugumu.Satir, id, "\"yuk\" bir nesne olmalı");
                        dogru = false;
                    }
                    else
                    {
                        foreach (var o in yukDugumu.Ozellikler)
                        {
                            if (o.Value.Tur != JsonTuru.Metin)
                            {
                                Hata(o.Value.Satir, id, "yuk \"" + o.Key + "\" bir metin olmalı");
                                dogru = false;
                                continue;
                            }

                            yuk.Add(new KeyValuePair<string, string>(o.Key, o.Value.MetinDeger));
                            if (o.Key == "onerme")
                            {
                                try
                                {
                                    Onerme.Ayristir(o.Value.MetinDeger);
                                }
                                catch (ArgumentException e)
                                {
                                    Hata(o.Value.Satir, id, "yuk önermesi geçersiz: " + e.Message);
                                    dogru = false;
                                }
                            }
                        }
                    }
                }

                var hedef = Metin(g, "hedef");
                if (hedef.Length > 0 && hedefTuru == "yok")
                {
                    Hata(g.Satir, id, "hedef_turu \"yok\" iken \"hedef\" yazılamaz");
                    dogru = false;
                }

                if (dogru && simulasyonKurulabilir)
                {
                    SubmitDene(girdi, senaryo, kurallar, katalog, krizler, g.Satir, id, tur, hedefTuru, hedef, yuk, dosya.GorunenAd, bulgular);
                }
            }

            foreach (var aile in Aileler)
            {
                if (!gorulenAileler.Contains(aile))
                {
                    bulgular.Add(new Bulgu(dosya.GorunenAd, menu.Satir, false, "menu", aile, "menüde \"" + aile + "\" ailesinden eylem yok (plan 10: konuşur, bilgi verir ya da saklar, iyilik ya da kötülük yapar)"));
                }
            }
        }

        private static string Metin(JsonDegeri nesne, string anahtar)
        {
            var d = nesne.Al(anahtar);
            return d != null && d.Tur == JsonTuru.Metin ? d.MetinDeger : string.Empty;
        }

        private static void SubmitDene(
            DogrulamaGirdisi girdi, SenaryoTanimi senaryo, IReadOnlyList<KuralTanimi> kurallar, EylemKatalogu? katalog, IReadOnlyList<KrizSatiri> krizler,
            int satir, string id, string tur, string hedefTuru, string hedef, List<KeyValuePair<string, string>> yuk, string menuAdi, List<Bulgu> bulgular)
        {
            var deneme = hedef;
            if (deneme.Length == 0)
            {
                if (hedefTuru == "npc" && senaryo.Npcler.Count > 0)
                {
                    deneme = senaryo.Npcler[0].Id;
                }
                else if (hedefTuru == "mekan")
                {
                    foreach (var bag in senaryo.Baglantilar)
                    {
                        if (bag.Key == senaryo.OyuncuKonumu)
                        {
                            deneme = bag.Value;
                            break;
                        }

                        if (bag.Value == senaryo.OyuncuKonumu)
                        {
                            deneme = bag.Key;
                            break;
                        }
                    }
                }
            }

            try
            {
                var sim = Simulasyon.Olustur(senaryo, kurallar, 1UL, eylemler: katalog?.Eylemler, krizler: krizler);
                sim.Submit(new PlayerAction(tur, deneme, yuk));
            }
            catch (ArgumentException e)
            {
                bulgular.Add(new Bulgu(menuAdi, satir, true, "menu", id, "Submit geçersiz sayar: " + e.Message));
            }
            catch (InvalidOperationException e)
            {
                bulgular.Add(new Bulgu(menuAdi, satir, true, "menu", id, "Submit geçersiz sayar: " + e.Message));
            }
        }
    }
}
