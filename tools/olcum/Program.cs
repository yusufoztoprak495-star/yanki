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
    ""celisen_ozellikler"": [ ""merhamet"" ],
    ""etkiler"": [ { ""tur"": ""duygu"", ""duygu"": ""ofke"", ""siddet"": 0.3, ""hedef"": ""eylem.hedef"" } ] },
  { ""id"": ""git"", ""hedef"": ""komsu_mekan"", ""taban"": 0.8,
    ""olcutler"": [ { ""alan"": ""kisilik(cesaret)"" } ],
    ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""sosyallik"" } ] } },
  { ""id"": ""dinlen"", ""olay_turu"": ""dinlendi"", ""hedef"": ""yok"", ""taban"": 0.5,
    ""ruh_hali_carpani"": { ""duygular"": [ { ""duygu"": ""ofke"", ""ters"": true } ] },
    ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.2 } ] }
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

        private static Simulasyon Kur(int npcSayisi, ulong seed)
        {
            var senaryo = SenaryoYukleyici.Yukle(SenaryoJson(npcSayisi), "olcum.json");
            if (!senaryo.Basarili || senaryo.Senaryo == null)
            {
                throw new InvalidOperationException(string.Join("\n", senaryo.Hatalar));
            }

            var eylemler = EylemYukleyici.Yukle(Eylemler, "eylemler.json");
            if (!eylemler.Basarili)
            {
                throw new InvalidOperationException(string.Join("\n", eylemler.Hatalar));
            }

            var turler = new List<string>(senaryo.Senaryo.OlayTurleri);
            turler.AddRange(new EylemKatalogu(eylemler.Eylemler).OlayTurleri);
            var kurallar = KuralYukleyici.Yukle(Kurallar, "kurallar.json", turler);
            if (!kurallar.Basarili)
            {
                throw new InvalidOperationException(string.Join("\n", kurallar.Hatalar));
            }

            return Simulasyon.Olustur(senaryo.Senaryo, kurallar.Kurallar, seed, eylemler: eylemler.Eylemler);
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

        private static void Olc(int npcSayisi, int tekrar)
        {
            Kur(npcSayisi, 1).Tick(10); // ısınma: JIT etkisini ölçümden çıkarır

            var sureler = new List<double>();
            Simulasyon? son = null;
            for (var i = 0; i < tekrar; i++)
            {
                var sim = Kur(npcSayisi, 42);
                var sw = Stopwatch.StartNew();
                sim.Tick(TickSayisi);
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

            Console.WriteLine($"### {npcSayisi} NPC, {TickSayisi} tick (stres fikstürü: her NPC her tick eylem seçer)");
            Console.WriteLine();
            Console.WriteLine("| ölçü | değer |");
            Console.WriteLine("|---|---|");
            Console.WriteLine($"| tick başına ortalama süre | {(ortanca / TickSayisi).ToString("F3", ic)} ms (toplam {ortanca.ToString("F1", ic)} ms, {tekrar} çalıştırmanın ortancası) |");
            Console.WriteLine($"| olay sayısı | {olaylar.Count} (karar_verildi {kararSayisi}) |");
            Console.WriteLine($"| günlük boyutu | {(toplam / 1024.0).ToString("F1", ic)} KiB |");
            Console.WriteLine($"| karar_verildi boyutu | {(karar / 1024.0).ToString("F1", ic)} KiB = %{(100.0 * karar / toplam).ToString("F1", ic)} |");
            Console.WriteLine($"| karar_verildi bayt/olay | {(kararSayisi == 0 ? 0 : karar / kararSayisi)} |");
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
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--npc" && i + 1 < args.Length)
                {
                    npcler = args[++i].Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                }
                else if (args[i] == "--tekrar" && i + 1 < args.Length)
                {
                    tekrar = int.Parse(args[++i], CultureInfo.InvariantCulture);
                }
                else
                {
                    Console.Error.WriteLine("Kullanım: olcum [--npc 3,10,50] [--tekrar N]");
                    return 1;
                }
            }

            Console.WriteLine($".NET {Environment.Version}, işlemci sayısı {Environment.ProcessorCount}");
            Console.WriteLine();
            foreach (var n in npcler)
            {
                Olc(n, tekrar ?? (n >= 50 ? 1 : 5));
            }

            return 0;
        }
    }
}
