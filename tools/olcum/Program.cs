using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.World;

namespace Yanki.Olcum
{
    /// <summary>
    /// Karar ölçüm düzeneği. Fikstür SAHTE içeriktir (Kasaba Hanı değil): 5 eylem, 3 kural, sabit seed'li kişilikler, 168 tick.
    /// <para>
    /// Bu bir STRES fikstürüdür: her NPC her tick bir eylem seçer (hedefsiz <c>dinlen</c> eylemi her zaman aday olduğu için hiçbir NPC "boşta" kalmaz),
    /// yani karar adımı ve olay yazımı olabildiğince yoğundur. Gerçek bir senaryoda NPC'ler çoğu tick'te bir şey yapmayabilir.
    /// </para>
    /// Kullanım: <c>dotnet run --project tools/olcum -c Release [-- --npc 3,10,50 --tekrar 3]</c>.
    /// </summary>
    internal static class Program
    {
        private const int TickSayisi = 168;

        // --gercekci: ayrı, gerçekçi 50 NPC fikstürü (GercekciFikstur.cs); stres fikstürü değişmez.
        private static bool _gercekci;

        private static void Girdi(Simulasyon sim, int tick)
        {
            if (_gercekci)
            {
                GercekciFikstur.Girdi(sim, tick);
            }
        }

        private static void Kos(Simulasyon sim, int tickSayisi)
        {
            if (!_gercekci)
            {
                sim.Tick(tickSayisi);
                return;
            }

            for (var t = 1; t <= tickSayisi; t++)
            {
                GercekciFikstur.Girdi(sim, t);
                sim.Tick(1);
            }
        }

        private static readonly string[] Mekanlar = { "han", "pazar", "degirmen", "kilise", "mahalle" };

        private const string Eylemler = @"{ ""eylemler"": [
  { ""id"": ""selam"", ""olay_turu"": ""selam_oldu"", ""hedef"": ""yok"", ""taban"": 1,
    ""on_kosullar"": [ { ""alan"": ""hafiza_var(selam_oldu, ben, 24)"", ""op"": ""=="", ""deger"": false } ],
    ""olcutler"": [ { ""alan"": ""kisilik(sosyallik)"", ""egri"": { ""tur"": ""lojistik"", ""k"": 6, ""m"": 0.4 } } ],
    ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""sosyallik"" } ] },
    ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.5 } ] },
  { ""id"": ""yardim"", ""olay_turu"": ""yardim_oldu"", ""hedef"": ""npc"", ""taban"": 1.2,
    ""on_kosullar"": [ { ""alan"": ""hafiza_var(yardim_oldu, ben, 12)"", ""op"": ""=="", ""deger"": false } ],
    ""olcutler"": [ { ""alan"": ""kisilik(merhamet)"" }, { ""alan"": ""fikir(eylem.hedef)"", ""min"": -100, ""max"": 100 } ],
    ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""merhamet"" } ] },
    ""fikir_carpani"": { ""yon"": ""artan"" },
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""eylem.hedef"", ""eksen"": ""sevgi"", ""delta"": 1 } ] },
  { ""id"": ""tehdit"", ""olay_turu"": ""tehdit_oldu"", ""hedef"": ""npc"", ""taban"": 0.6,
    ""olcutler"": [ { ""alan"": ""kisilik(kin_tutma)"" }, { ""alan"": ""kisilik(cesaret)"", ""egri"": { ""tur"": ""ustel"", ""k"": 2 } } ],
    ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""cesaret"" } ] },
    ""celisen_ozellikler"": [ ""merhamet"" ], ""stres_tabani"": 10,
    ""etkiler"": [ { ""tur"": ""duygu"", ""duygu"": ""ofke"", ""siddet"": 0.3, ""hedef"": ""eylem.hedef"" } ] },
  { ""id"": ""git"", ""hedef"": ""komsu_mekan"", ""taban"": 0.8,
    ""olcutler"": [ { ""alan"": ""kisilik(cesaret)"" } ],
    ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""sosyallik"" } ] } },
  { ""id"": ""dinlen"", ""olay_turu"": ""dinlendi"", ""hedef"": ""yok"", ""taban"": 0.5,
    ""ruh_hali_carpani"": { ""duygular"": [ { ""duygu"": ""ofke"", ""ters"": true } ] },
    ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.2 } ] }
] }";

        // Dedikodulu ölçüm (6. hafta): aynı 5 eylem + anlat/yalan_soyle. Konu havuzu yalnızca borclu(*,*,*); sahte içerik (Kasaba Hanı değil).
        private const string DedikoduEylemleri = @",
  { ""id"": ""anlat"", ""olay_turu"": ""anlatti"", ""hedef"": ""npc"", ""taban"": 3, ""aktarim"": ""dogru"",
    ""konu_desenleri"": [ ""borclu(*,*,*)"" ],
    ""olcutler"": [ { ""alan"": ""kisilik(sosyallik)"", ""egri"": { ""tur"": ""lojistik"", ""k"": 6, ""m"": 0.4 } }, { ""alan"": ""konu_puani(eylem.hedef)"" } ],
    ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""sosyallik"" } ] } },
  { ""id"": ""yalan_soyle"", ""olay_turu"": ""anlatti"", ""hedef"": ""npc"", ""taban"": 0.3, ""aktarim"": ""yalan"",
    ""yalan_onermeleri"": [ ""degil(borclu(N00,N01,500))"", ""degil(borclu(N02,N03,500))"" ],
    ""celisen_ozellikler"": [ ""durustluk"" ], ""stres_tabani"": 15,
    ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""durustluk"", ""ters"": true } ] } }
";

        private const string DedikoduKrizleri = @"{ ""krizler"": [
  { ""id"": ""itiraf"", ""seviye"": 1, ""olay_turu"": ""itiraf_etti"", ""hedef"": ""yalan_kurbani"",
    ""kriterler"": [ { ""alan"": ""kisilik(durustluk)"", ""op"": "">="", ""deger"": 0.6 } ] },
  { ""id"": ""sakinles"", ""seviye"": 1, ""olay_turu"": ""sakinlesti"", ""hedef"": ""yok"" },
  { ""id"": ""patla"", ""seviye"": 2, ""olay_turu"": ""patladi"", ""hedef"": ""en_soguk"" },
  { ""id"": ""kac"", ""seviye"": 2, ""olay_turu"": ""kacti"", ""hedef"": ""yok"" }
] }";

        private const string Kurallar = @"{ ""kurallar"": [
  { ""id"": ""yardim_minneti"", ""ne_zaman"": ""yardim_oldu"",
    ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""minnet"", ""deger"": 10, ""sure"": 168 } ] },
  { ""id"": ""tehdit_korkusu"", ""ne_zaman"": ""tehdit_oldu"",
    ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""korku"", ""delta"": 8 }, { ""tur"": ""duygu"", ""duygu"": ""korku"", ""siddet"": 0.5, ""hedef"": ""olay.fail"" } ] },
  { ""id"": ""selam_tanigi"", ""ne_zaman"": ""selam_oldu"",
    ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""selamlandi"", ""deger"": 2, ""sure"": 48 } ] }
] }";

        private static string SenaryoJson(int npcSayisi)
        {
            var mekanSayisi = npcSayisi <= 3 ? 2 : npcSayisi <= 10 ? 3 : 5;
            var sb = new StringBuilder();
            sb.Append("{ \"surum\": 1, \"ad\": \"olcum\", \"mekanlar\": [");
            sb.Append(string.Join(", ", Mekanlar.Take(mekanSayisi).Select(m => "\"" + m + "\"")));
            sb.Append("], \"baglantilar\": [");
            sb.Append(string.Join(", ", Mekanlar.Skip(1).Take(mekanSayisi - 1).Select(m => "[\"han\", \"" + m + "\"]")));
            sb.Append("], \"oyuncu\": { \"id\": \"oyuncu\", \"konum\": \"han\" }, \"npcler\": [");
            var parcalar = new List<string>();
            for (var i = 0; i < npcSayisi; i++)
            {
                var rng = AnahtarliRng.Icin(1234, 0, "olcum", i.ToString(CultureInfo.InvariantCulture));
                string D() => Math.Round(rng.SonrakiDecimal01(), 2).ToString(CultureInfo.InvariantCulture);
                parcalar.Add(
                    $"{{ \"id\": \"N{i:D2}\", \"konum\": \"{Mekanlar[i % mekanSayisi]}\", \"kisilik\": {{ \"acgozluluk\": {D()}, \"durustluk\": {D()}, "
                    + $"\"cesaret\": {D()}, \"sosyallik\": {D()}, \"kin_tutma\": {D()}, \"merhamet\": {D()} }} }}");
            }

            sb.Append(string.Join(", ", parcalar));
            sb.Append("], \"gercekler\": [], \"olay_turleri\": [\"yardim_etti\"], \"kural_dosyalari\": [] }");
            return sb.ToString();
        }

        private static Simulasyon Kur(int npcSayisi, ulong seed, bool dedikodu = false)
        {
            if (_gercekci)
            {
                return GercekciFikstur.Kur(npcSayisi, seed);
            }

            var senaryo = SenaryoYukleyici.Yukle(SenaryoJson(npcSayisi), "olcum.json");
            if (!senaryo.Basarili || senaryo.Senaryo == null)
            {
                throw new InvalidOperationException(string.Join("\n", senaryo.Hatalar));
            }

            var eylemler = EylemYukleyici.Yukle(dedikodu ? Eylemler.Substring(0, Eylemler.LastIndexOf(']')) + DedikoduEylemleri + "] }" : Eylemler, "eylemler.json");
            if (!eylemler.Basarili)
            {
                throw new InvalidOperationException(string.Join("\n", eylemler.Hatalar));
            }

            var krizler = new List<KrizSatiri>();
            if (dedikodu)
            {
                var kriz = KrizYukleyici.Yukle(DedikoduKrizleri, "krizler.json");
                if (!kriz.Basarili)
                {
                    throw new InvalidOperationException(string.Join("\n", kriz.Hatalar));
                }

                krizler.AddRange(kriz.Satirlar);
            }

            var turler = new List<string>(senaryo.Senaryo.OlayTurleri);
            turler.AddRange(new EylemKatalogu(eylemler.Eylemler).OlayTurleri);
            var kurallar = KuralYukleyici.Yukle(Kurallar, "kurallar.json", turler);
            if (!kurallar.Basarili)
            {
                throw new InvalidOperationException(string.Join("\n", kurallar.Hatalar));
            }

            var sim = Simulasyon.Olustur(senaryo.Senaryo, kurallar.Kurallar, seed, eylemler: eylemler.Eylemler, krizler: krizler);
            if (dedikodu)
            {
                // Oyuncu han'daki ilk NPC'lere birkaç farklı borç söylentisi anlatır (tick 1); gerisini NPC'ler yayar.
                for (var i = 0; i < Math.Min(4, npcSayisi); i++)
                {
                    var hedef = "N" + (i * (npcSayisi >= 5 ? 5 : 1)).ToString("D2", CultureInfo.InvariantCulture);
                    if (sim.Dunya.TekDeger("konum!", hedef) == "han")
                    {
                        sim.Submit(new PlayerAction(
                            "anlatti", hedef,
                            new[] { new KeyValuePair<string, string>("onerme", "borclu(N" + (i + 6).ToString("D2", CultureInfo.InvariantCulture) + ",N" + i.ToString("D2", CultureInfo.InvariantCulture) + ",500)") }));
                    }
                }
            }

            return sim;
        }

        // Olayın kanonik metin dökümünün UTF-8 bayt sayısı (günlük boyutunun yaklaşık ölçüsü).
        private static long Boyut(Olay o)
        {
            var sb = new StringBuilder();
            sb.Append(o.Id).Append('|').Append(o.Tick).Append('|').Append(o.Sira).Append('|').Append(o.Tur).Append('|').Append(o.Fail).Append('|')
                .Append(o.Hedef).Append('|').Append(o.Mekan).Append('|');
            foreach (var kv in o.Yuk)
            {
                sb.Append(kv.Key).Append('=').Append(kv.Value).Append(';');
            }

            sb.Append('|').Append(string.Join(",", o.NedenOlaylar)).Append('|').Append(o.KuralId).Append('\n');
            return Encoding.UTF8.GetByteCount(sb.ToString());
        }

        private static void Olc(int npcSayisi, int tekrar, bool dedikodu)
        {
            Kur(npcSayisi, 1, dedikodu).Tick(10); // ısınma: JIT etkisini ölçümden çıkarır

            var sureler = new List<double>();
            Simulasyon? son = null;
            for (var i = 0; i < tekrar; i++)
            {
                var sim = Kur(npcSayisi, 42, dedikodu);
                var sw = Stopwatch.StartNew();
                Kos(sim, TickSayisi);
                sw.Stop();
                sureler.Add(sw.Elapsed.TotalMilliseconds);
                son = sim;
            }

            sureler.Sort();
            var ortanca = sureler[sureler.Count / 2];
            var olaylar = son!.Gunluk.Olaylar;

            long toplam = 0;
            long karar = 0;
            var tur = new SortedDictionary<string, KeyValuePair<int, long>>(StringComparer.Ordinal);
            foreach (var o in olaylar)
            {
                var b = Boyut(o);
                toplam += b;
                if (o.Tur == YerlesikTurler.KararVerildi)
                {
                    karar += b;
                }

                var anahtar = o.KuralId == "algi" ? o.Tur + "(algi)" : o.Tur;
                tur.TryGetValue(anahtar, out var onceki);
                tur[anahtar] = new KeyValuePair<int, long>(onceki.Key + 1, onceki.Value + b);
            }

            var kararSayisi = olaylar.Count(o => o.Tur == YerlesikTurler.KararVerildi);
            var oturum = Encoding.UTF8.GetByteCount(OturumKaydi.Yaz(OturumKaydi.Uret(son, "olcum.json")));
            var ic = CultureInfo.InvariantCulture;

            Console.WriteLine(_gercekci
                ? $"### {npcSayisi} NPC, {TickSayisi} tick (GERÇEKÇİ fikstür: 8 mekan, 9 eylem, 12 kural, dedikodu açık, NPC'ler çoğu tick boşta)"
                : $"### {npcSayisi} NPC, {TickSayisi} tick ({(dedikodu ? "dedikodulu" : "dedikodusuz")} stres fikstürü: her NPC her tick eylem seçer)");
            Console.WriteLine();
            Console.WriteLine("| ölçü | değer |");
            Console.WriteLine("|---|---|");
            Console.WriteLine($"| tick başına ortalama süre | {(ortanca / TickSayisi).ToString("F3", ic)} ms (toplam {ortanca.ToString("F1", ic)} ms, {tekrar} çalıştırmanın ortancası) |");
            Console.WriteLine($"| olay sayısı | {olaylar.Count} (karar_verildi {kararSayisi}) |");
            var kuralKimlikleri = new SortedSet<string>(son.Kurallar.Select(k => k.Id), StringComparer.Ordinal);
            var kuralCikti = new SortedDictionary<long, int>();
            foreach (var o in olaylar)
            {
                if (kuralKimlikleri.Contains(o.KuralId))
                {
                    kuralCikti.TryGetValue(o.Tick, out var n);
                    kuralCikti[o.Tick] = n + 1;
                }
            }

            var enYuksek = kuralCikti.Count == 0 ? new KeyValuePair<long, int>(0, 0) : kuralCikti.OrderByDescending(k => k.Value).ThenBy(k => k.Key).First();
            Console.WriteLine($"| tick başına en yüksek kural çıktısı (M için) | {enYuksek.Value} (tick {enYuksek.Key}); fren olayı {olaylar.Count(o => o.Tur == "fren_uygulandi")} |");
            Console.WriteLine($"| günlük boyutu | {(toplam / 1024.0).ToString("F1", ic)} KiB |");
            Console.WriteLine($"| karar_verildi boyutu | {(karar / 1024.0).ToString("F1", ic)} KiB = %{(100.0 * karar / toplam).ToString("F1", ic)} |");
            Console.WriteLine($"| karar_verildi bayt/olay | {(kararSayisi == 0 ? 0 : karar / kararSayisi)} |");
            if (dedikodu)
            {
                var kanit = olaylar.Where(o => o.Tur == "kanit_eklendi").ToList();
                var duydu = kanit.Count(o => o.Deger("tur") == "duydu");
                var onermeler = new SortedSet<string>(kanit.Where(o => o.Deger("tur") == "duydu").Select(o => o.Deger("onerme")), StringComparer.Ordinal);
                Console.WriteLine($"| kanıt sayısı | {kanit.Count} (gordu {kanit.Count - duydu}, duydu {duydu}); duydu ile yayılan {onermeler.Count} farklı önerme |");
                Console.WriteLine($"| anlatım | anlatti {olaylar.Count(o => o.Tur == "anlatti")}, aktarim_kaydedildi {olaylar.Count(o => o.Tur == "aktarim_kaydedildi")}, yalan kaydı {olaylar.Count(o => o.Tur == "aktarim_kaydedildi" && o.Deger("yalan") == "1")} |");
                Console.WriteLine($"| stres/kriz | stres_degisti {olaylar.Count(o => o.Tur == "stres_degisti")}, kriz olayı {olaylar.Count(o => o.KuralId.StartsWith("kriz:", StringComparison.Ordinal) && !Yanki.Core.Mind.ZihinOlaylari.ZihinOlayiMi(o.Tur))} |");
                var enCok = 0;
                foreach (var id in son.Zihinler.Kimlikler)
                {
                    enCok = Math.Max(enCok, son.Zihinler[id].Onermeler.Count);
                }

                Console.WriteLine($"| NPC başına en çok önerme | {enCok} |");
            }

            // Hash maliyetinin tick içindeki payı: aynı koşuyu tick tick sür ve her tick sonunda önbelleksiz tam durum hash'ini ayrıca zamanla.
            {
                var hizalama = Kur(npcSayisi, 42, dedikodu);
                double tamToplam = 0;
                var sw2 = new Stopwatch();
                for (var i = 0; i < TickSayisi; i++)
                {
                    Girdi(hizalama, i + 1);
                    hizalama.Tick(1);
                    sw2.Restart();
                    var tamMetin = new StringBuilder("ZIHIN|1\n");
                    foreach (var kimlik in hizalama.Zihinler.Kimlikler)
                    {
                        tamMetin.Append(hizalama.Zihinler[kimlik].KanonikMetinTam());
                    }

                    Yanki.Core.World.Hash64.Hesapla(tamMetin.ToString());
                    sw2.Stop();
                    tamToplam += sw2.Elapsed.TotalMilliseconds;
                }

                Console.WriteLine($"| önbelleksiz tam zihin hash'i (tick başına ortalama) | {(tamToplam / TickSayisi).ToString("F3", ic)} ms = tick süresinin %{(100.0 * (tamToplam / TickSayisi) / (ortanca / TickSayisi)).ToString("F1", ic)} |");
            }

            var hSw = Stopwatch.StartNew();
            for (var i = 0; i < 20; i++)
            {
                son.DurumHash();
            }

            var tamHash = hSw.Elapsed.TotalMilliseconds / 20;
            hSw.Restart();
            for (var i = 0; i < 20; i++)
            {
                son.Dunya.DurumHash();
            }

            var dunyaHash = hSw.Elapsed.TotalMilliseconds / 20;
            hSw.Restart();
            for (var i = 0; i < 20; i++)
            {
                son.Zihinler.KanonikMetin();
            }

            var zihinHash = hSw.Elapsed.TotalMilliseconds / 20;
            Console.WriteLine($"| durum hash (son durumda tek çağrı) | {tamHash.ToString("F3", ic)} ms (dünya {dunyaHash.ToString("F3", ic)}, zihinler {zihinHash.ToString("F3", ic)}); tick başına bir çağrı = tick süresinin %{(100.0 * tamHash / (ortanca / TickSayisi)).ToString("F1", ic)} |");
            Console.WriteLine($"| son durum hash'i | {son.DurumHash()} |");
            Console.WriteLine($"| oturum dosyası | {oturum} bayt (NPC kararları ve izler dosyaya yazılmaz) |");
            Console.WriteLine();
            Console.WriteLine("En büyük olay türleri:");
            Console.WriteLine();
            foreach (var kv in tur.OrderByDescending(k => k.Value.Value).Take(5))
            {
                Console.WriteLine($"- `{kv.Key}`: {kv.Value.Key} olay, {(kv.Value.Value / 1024.0).ToString("F1", ic)} KiB (%{(100.0 * kv.Value.Value / toplam).ToString("F1", ic)})");
            }

            Console.WriteLine();
        }

        public static int Main(string[] args)
        {
            var npcler = new[] { 3, 10, 50 };
            int? tekrar = null;
            var dedikodu = false;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--npc" && i + 1 < args.Length)
                {
                    npcler = args[++i].Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                }
                else if (args[i] == "--dedikodu")
                {
                    dedikodu = true;
                }
                else if (args[i] == "--gercekci")
                {
                    _gercekci = true;
                    dedikodu = true;
                }
                else if (args[i] == "--tekrar" && i + 1 < args.Length)
                {
                    tekrar = int.Parse(args[++i], CultureInfo.InvariantCulture);
                }
                else
                {
                    Console.Error.WriteLine("Kullanım: olcum [--npc 3,10,50] [--tekrar N] [--dedikodu | --gercekci]");
                    return 1;
                }
            }

            Console.WriteLine($".NET {Environment.Version}, işlemci sayısı {Environment.ProcessorCount}");
            Console.WriteLine();
            foreach (var n in npcler)
            {
                Olc(n, tekrar ?? (n >= 50 ? 1 : 5), dedikodu);
            }

            return 0;
        }
    }
}
