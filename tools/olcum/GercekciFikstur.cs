using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Yanki.Core.Decision;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.World;

namespace Yanki.Olcum
{
    /// <summary>
    /// GERÇEKÇİ 50 NPC fikstürü (7. hafta ölçüm eki). Mevcut stres fikstürü DEĞİŞMEDİ; bu ayrı bir moddur (<c>--gercekci</c>) ve sonuçlar ayrı etiketle yazılır.
    /// <para>
    /// Varsayımlar (sahte içerik, Kasaba Hanı değil): 8 mekan ve küçük bir bağlantı grafiği (han merkez); NPC'ler mekanlara eşit olmayan dağılır (han'da ve pazarda çok, orman/rıhtımda az);
    /// 9 eylem ve 12 kural senaryo düzeyinde makul sayıdadır; her tick eylem seçmeye zorlayan "her zaman aday" bir eylem YOKTUR (<c>dinlen</c> yalnızca stresliyken aday),
    /// yani NPC'lerin çoğu çoğu tick'te boşta kalır; <c>selam</c>, <c>git</c> ve <c>tehdit</c> için cooldown (7. hafta frenleri) kullanılır; dedikodu açıktır
    /// (oyuncu t1'de han'daki dört NPC'ye dört borç söylentisi anlatır, gerisini NPC'ler yayar, ara sıra yalan ve kriz); oyuncu hafta boyunca altı kez müdahale eder.
    /// </para>
    /// </summary>
    internal static class GercekciFikstur
    {
        private static readonly string[] Mekanlar = { "han", "pazar", "degirmen", "kilise", "mahalle", "ciftlik", "rihtim", "orman" };

        // Bağlantılar (han merkez): han–pazar, han–kilise, han–mahalle, pazar–degirmen, pazar–rihtim, mahalle–ciftlik, ciftlik–orman.
        private static readonly string[][] Baglantilar =
        {
            new[] { "han", "pazar" }, new[] { "han", "kilise" }, new[] { "han", "mahalle" }, new[] { "pazar", "degirmen" },
            new[] { "pazar", "rihtim" }, new[] { "mahalle", "ciftlik" }, new[] { "ciftlik", "orman" },
        };

        // Mekan ağırlıkları (toplam 20): han 6, pazar 4, mahalle 3, kilise 2, degirmen 2, ciftlik 1, rihtim 1, orman 1.
        private static readonly int[] Agirlik = { 6, 4, 2, 2, 3, 1, 1, 1 };

        private const string Eylemler = @"{ ""eylemler"": [
  { ""id"": ""selam"", ""olay_turu"": ""selam_oldu"", ""hedef"": ""yok"", ""taban"": 0.6, ""cooldown"": 24, ""kapsam"": ""npc"",
    ""olcutler"": [ { ""alan"": ""kisilik(sosyallik)"", ""egri"": { ""tur"": ""lojistik"", ""k"": 6, ""m"": 0.4 } } ],
    ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""sosyallik"" } ] }, ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.4 } ] },
  { ""id"": ""yardim"", ""olay_turu"": ""yardim_oldu"", ""hedef"": ""npc"", ""taban"": 1.0, ""cooldown"": 12, ""kapsam"": ""hedef"",
    ""on_kosullar"": [ { ""alan"": ""fikir(eylem.hedef)"", ""op"": "">="", ""deger"": 0 } ],
    ""olcutler"": [ { ""alan"": ""kisilik(merhamet)"" } ], ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""merhamet"" } ] },
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""eylem.hedef"", ""eksen"": ""sevgi"", ""delta"": 1 } ] },
  { ""id"": ""tehdit"", ""olay_turu"": ""tehdit_oldu"", ""hedef"": ""npc"", ""taban"": 1.5, ""cooldown"": 24, ""kapsam"": ""hedef"",
    ""on_kosullar"": [ { ""alan"": ""fikir(eylem.hedef)"", ""op"": ""<"", ""deger"": -10 } ],
    ""olcutler"": [ { ""alan"": ""kisilik(kin_tutma)"" } ], ""celisen_ozellikler"": [ ""merhamet"" ], ""stres_tabani"": 20,
    ""etkiler"": [ { ""tur"": ""duygu"", ""duygu"": ""ofke"", ""siddet"": 0.4, ""hedef"": ""eylem.hedef"" } ] },
  { ""id"": ""git"", ""hedef"": ""komsu_mekan"", ""taban"": 0.5, ""cooldown"": 6, ""kapsam"": ""npc"",
    ""olcutler"": [ { ""alan"": ""kisilik(cesaret)"" } ], ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""sosyallik"" } ] } },
  { ""id"": ""anlat"", ""olay_turu"": ""anlatti"", ""hedef"": ""npc"", ""taban"": 2, ""aktarim"": ""dogru"", ""konu_desenleri"": [ ""borclu(*,*,*)"" ],
    ""olcutler"": [ { ""alan"": ""kisilik(sosyallik)"", ""egri"": { ""tur"": ""lojistik"", ""k"": 6, ""m"": 0.4 } }, { ""alan"": ""konu_puani(eylem.hedef)"" } ],
    ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""sosyallik"" } ] } },
  { ""id"": ""yalan_soyle"", ""olay_turu"": ""anlatti"", ""hedef"": ""npc"", ""taban"": 0.8, ""aktarim"": ""yalan"",
    ""yalan_onermeleri"": [ ""degil(borclu(N06,N00,500))"", ""degil(borclu(N07,N01,500))"" ], ""celisen_ozellikler"": [ ""durustluk"" ], ""stres_tabani"": 15,
    ""on_kosullar"": [ { ""alan"": ""hafiza_var(itiraf_etti, ben, 24)"", ""op"": ""=="", ""deger"": false } ],
    ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""durustluk"", ""ters"": true } ] } },
  { ""id"": ""calis"", ""olay_turu"": ""calisti"", ""hedef"": ""yok"", ""taban"": 0.4, ""cooldown"": 8, ""kapsam"": ""npc"", ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.2 } ] },
  { ""id"": ""sohbet"", ""olay_turu"": ""sohbet_oldu"", ""hedef"": ""npc"", ""taban"": 0.5, ""cooldown"": 10, ""kapsam"": ""hedef"",
    ""olcutler"": [ { ""alan"": ""kisilik(sosyallik)"" } ], ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""eylem.hedef"", ""eksen"": ""guven"", ""delta"": 1 } ] },
  { ""id"": ""dinlen"", ""olay_turu"": ""dinlendi"", ""hedef"": ""yok"", ""taban"": 0.5,
    ""on_kosullar"": [ { ""alan"": ""stres()"", ""op"": "">"", ""deger"": 5 } ], ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.2 } ] }
] }";

        private const string Kurallar = @"{ ""kurallar"": [
  { ""id"": ""yardim_minneti"", ""ne_zaman"": ""yardim_oldu"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""minnet"", ""deger"": 10, ""sure"": 336 } ] },
  { ""id"": ""tehdit_korkusu"", ""ne_zaman"": ""tehdit_oldu"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 24,
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""korku"", ""delta"": 8 } ] },
  { ""id"": ""tehdit_gordu"", ""ne_zaman"": ""tehdit_oldu"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""en_fazla"": 2,
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""tehdit_gordu"", ""deger"": -5, ""sure"": 168 } ] },
  { ""id"": ""selam_tanigi"", ""ne_zaman"": ""selam_oldu"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 48,
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""selamlandi"", ""deger"": 2, ""sure"": 48 } ] },
  { ""id"": ""sohbet_guveni"", ""ne_zaman"": ""sohbet_oldu"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""sevgi"", ""delta"": 1 } ] },
  { ""id"": ""soylenti_kaygisi"", ""ne_zaman"": ""anlatti"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ], ""en_fazla"": 1,
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""soylenti"", ""deger"": -3, ""sure"": 168 } ] },
  { ""id"": ""yalan_yakalandi"", ""ne_zaman"": ""itiraf_etti"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""yalanini_yakaladim"", ""deger"": -30, ""sure"": 720 } ] },
  { ""id"": ""patlamadan_korku"", ""ne_zaman"": ""patladi"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""korku"", ""delta"": 10 } ] },
  { ""id"": ""kactigi_gorulur"", ""ne_zaman"": ""kacti"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""en_fazla"": 1,
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""guven"", ""delta"": -2 } ] },
  { ""id"": ""oyuncu_yardimi"", ""ne_zaman"": ""yardim_etti"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""minnet"", ""deger"": 15, ""sure"": 336 } ] },
  { ""id"": ""oyuncu_tehdidi"", ""ne_zaman"": ""tehdit_etti"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""guven"", ""delta"": -20 } ] },
  { ""id"": ""calisma_huzuru"", ""ne_zaman"": ""calisti"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 72,
    ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.2 } ] }
] }";

        // --butce (8. hafta): plan bütçesine yakın HACIM varyantı (≈ 15 eylem, 30 kural); içerik sahte, Kasaba Hanı değil. Yalnızca eylem/kural sayısı büyür.
        public static bool Butce { get; set; }

        private const string ButceEylemleri = @",
  { ""id"": ""hediye"", ""olay_turu"": ""hediye_verildi"", ""hedef"": ""npc"", ""taban"": 0.7, ""cooldown"": 20, ""kapsam"": ""hedef"",
    ""on_kosullar"": [ { ""alan"": ""fikir(eylem.hedef)"", ""op"": "">="", ""deger"": 0 } ],
    ""olcutler"": [ { ""alan"": ""kisilik(merhamet)"" } ], ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""eylem.hedef"", ""eksen"": ""sevgi"", ""delta"": 2 } ] },
  { ""id"": ""pazarlik"", ""olay_turu"": ""pazarlik_edildi"", ""hedef"": ""npc"", ""taban"": 0.6, ""cooldown"": 12, ""kapsam"": ""hedef"",
    ""olcutler"": [ { ""alan"": ""kisilik(acgozluluk)"" } ], ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""acgozluluk"" } ] },
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""eylem.hedef"", ""eksen"": ""guven"", ""delta"": -1 } ] },
  { ""id"": ""dua_et"", ""olay_turu"": ""dua_edildi"", ""hedef"": ""yok"", ""taban"": 0.3, ""cooldown"": 30, ""kapsam"": ""npc"", ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.2 } ] },
  { ""id"": ""uyar"", ""olay_turu"": ""uyarildi"", ""hedef"": ""npc"", ""taban"": 0.5, ""cooldown"": 16, ""kapsam"": ""hedef"",
    ""olcutler"": [ { ""alan"": ""kisilik(cesaret)"" } ], ""etkiler"": [ { ""tur"": ""duygu"", ""duygu"": ""korku"", ""siddet"": 0.2, ""hedef"": ""eylem.hedef"" } ] },
  { ""id"": ""oyun_oyna"", ""olay_turu"": ""oyun_oynandi"", ""hedef"": ""npc"", ""taban"": 0.4, ""cooldown"": 14, ""kapsam"": ""hedef"",
    ""olcutler"": [ { ""alan"": ""kisilik(sosyallik)"" } ], ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""eylem.hedef"", ""eksen"": ""sevgi"", ""delta"": 1 } ] },
  { ""id"": ""gozle"", ""olay_turu"": ""gozlendi"", ""hedef"": ""yok"", ""taban"": 0.3, ""cooldown"": 18, ""kapsam"": ""npc"", ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.3 } ] }
";

        private const string ButceKurallari = @",
  { ""id"": ""hediye_hedef"", ""ne_zaman"": ""hediye_verildi"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ], ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""hediye_aldim"", ""deger"": 6, ""sure"": 168 } ] },
  { ""id"": ""hediye_tanik"", ""ne_zaman"": ""hediye_verildi"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 24, ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""comertlik"", ""deger"": 2, ""sure"": 72 } ] },
  { ""id"": ""pazarlik_hedef"", ""ne_zaman"": ""pazarlik_edildi"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ], ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""pazarlik_ettim"", ""deger"": -3, ""sure"": 72 } ] },
  { ""id"": ""pazarlik_tanik"", ""ne_zaman"": ""pazarlik_edildi"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 24, ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.2 } ] },
  { ""id"": ""dua_tanigi"", ""ne_zaman"": ""dua_edildi"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 48, ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""dindar"", ""deger"": 1, ""sure"": 168 } ] },
  { ""id"": ""dua_hafizasi"", ""ne_zaman"": ""dua_edildi"", ""kriterler"": [ { ""alan"": ""kisilik(sosyallik)"", ""op"": "">"", ""deger"": 0.5 }, { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 48, ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.1 } ] },
  { ""id"": ""uyari_hedef"", ""ne_zaman"": ""uyarildi"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ], ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""korku"", ""delta"": 2 } ] },
  { ""id"": ""uyari_tanik"", ""ne_zaman"": ""uyarildi"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 24, ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""uyarici"", ""deger"": -1, ""sure"": 48 } ] },
  { ""id"": ""oyun_hedef"", ""ne_zaman"": ""oyun_oynandi"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ], ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""sevgi"", ""delta"": 1 } ] },
  { ""id"": ""oyun_tanik"", ""ne_zaman"": ""oyun_oynandi"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 24, ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.1 } ] },
  { ""id"": ""gozle_tanigi"", ""ne_zaman"": ""gozlendi"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 48, ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""gozetliyor"", ""deger"": -2, ""sure"": 72 } ] },
  { ""id"": ""gozle_hafizasi"", ""ne_zaman"": ""gozlendi"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true }, { ""alan"": ""kisilik(cesaret)"", ""op"": ""<"", ""deger"": 0.5 } ], ""cooldown"": 48, ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.3 } ] },
  { ""id"": ""tehdit_edildim"", ""ne_zaman"": ""tehdit_oldu"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true }, { ""alan"": ""kisilik(cesaret)"", ""op"": ""<"", ""deger"": 0.6 } ], ""cooldown"": 24, ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""tehdit_edildim"", ""deger"": -8, ""sure"": 168 } ] },
  { ""id"": ""iyilik_gorulur"", ""ne_zaman"": ""yardim_oldu"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 24, ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""iyilik_gordum"", ""deger"": 3, ""sure"": 72 } ] },
  { ""id"": ""soylenti_tanigi"", ""ne_zaman"": ""anlatti"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 12, ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.1 } ] },
  { ""id"": ""sohbet_tanigi"", ""ne_zaman"": ""sohbet_oldu"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 24, ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""sohbet_gordum"", ""deger"": 1, ""sure"": 24 } ] },
  { ""id"": ""selam_hedef"", ""ne_zaman"": ""selam_oldu"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 12, ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""selam_aldim"", ""deger"": 2, ""sure"": 24 } ] },
  { ""id"": ""itiraf_gorulur"", ""ne_zaman"": ""itiraf_etti"", ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ], ""cooldown"": 24, ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""itiraf_gordum"", ""deger"": -3, ""sure"": 168 } ] }
";

        private const string Krizler = @"{ ""krizler"": [
  { ""id"": ""itiraf"", ""seviye"": 1, ""olay_turu"": ""itiraf_etti"", ""hedef"": ""yalan_kurbani"",
    ""kriterler"": [ { ""alan"": ""kisilik(durustluk)"", ""op"": "">="", ""deger"": 0.6 } ] },
  { ""id"": ""sakinles"", ""seviye"": 1, ""olay_turu"": ""sakinlesti"", ""hedef"": ""yok"" },
  { ""id"": ""patla"", ""seviye"": 2, ""olay_turu"": ""patladi"", ""hedef"": ""en_soguk"", ""kriterler"": [ { ""alan"": ""kisilik(kin_tutma)"", ""op"": "">="", ""deger"": 0.6 } ] },
  { ""id"": ""kac"", ""seviye"": 2, ""olay_turu"": ""kacti"", ""hedef"": ""yok"" }
] }";

        public static string SenaryoJson(int npcSayisi)
        {
            var toplamAgirlik = Agirlik.Sum();
            var sb = new StringBuilder();
            sb.Append("{ \"surum\": 1, \"ad\": \"gercekci\", \"mekanlar\": [");
            sb.Append(string.Join(", ", Mekanlar.Select(m => "\"" + m + "\"")));
            sb.Append("], \"baglantilar\": [");
            sb.Append(string.Join(", ", Baglantilar.Select(b => "[\"" + b[0] + "\", \"" + b[1] + "\"]")));
            sb.Append("], \"oyuncu\": { \"id\": \"oyuncu\", \"konum\": \"han\" }, \"npcler\": [");
            var parcalar = new List<string>();
            for (var i = 0; i < npcSayisi; i++)
            {
                // Mekan: i'nin ağırlıklı dağılıma göre yeri (deterministik, kimlik sırasından bağımsız bir döngü).
                var dilim = (i * toplamAgirlik) / npcSayisi;
                var mekan = 0;
                for (var kalan = dilim; mekan < Agirlik.Length - 1 && kalan >= Agirlik[mekan]; mekan++)
                {
                    kalan -= Agirlik[mekan];
                }

                var rng = AnahtarliRng.Icin(4321, 0, "gercekci", i.ToString(CultureInfo.InvariantCulture));
                string D() => Math.Round(rng.SonrakiDecimal01(), 2).ToString(CultureInfo.InvariantCulture);
                parcalar.Add(
                    $"{{ \"id\": \"N{i:D2}\", \"konum\": \"{Mekanlar[mekan]}\", \"kisilik\": {{ \"acgozluluk\": {D()}, \"durustluk\": {D()}, "
                    + $"\"cesaret\": {D()}, \"sosyallik\": {D()}, \"kin_tutma\": {D()}, \"merhamet\": {D()} }} }}");
            }

            sb.Append(string.Join(", ", parcalar));
            sb.Append("], \"gercekler\": [], \"olay_turleri\": [\"yardim_etti\", \"tehdit_etti\"], \"kural_dosyalari\": [] }");
            return sb.ToString();
        }

        public static Simulasyon Kur(int npcSayisi, ulong seed)
        {
            var senaryo = SenaryoYukleyici.Yukle(SenaryoJson(npcSayisi), "gercekci.json");
            if (!senaryo.Basarili || senaryo.Senaryo == null)
            {
                throw new InvalidOperationException(string.Join("\n", senaryo.Hatalar));
            }

            var eylemler = EylemYukleyici.Yukle(Butce ? Eylemler.Substring(0, Eylemler.LastIndexOf(']')) + ButceEylemleri + "] }" : Eylemler, "eylemler.json");
            if (!eylemler.Basarili)
            {
                throw new InvalidOperationException(string.Join("\n", eylemler.Hatalar));
            }

            var kriz = KrizYukleyici.Yukle(Krizler, "krizler.json");
            if (!kriz.Basarili)
            {
                throw new InvalidOperationException(string.Join("\n", kriz.Hatalar));
            }

            var turler = new List<string>(senaryo.Senaryo.OlayTurleri);
            turler.AddRange(new EylemKatalogu(eylemler.Eylemler).OlayTurleri);
            foreach (var k in kriz.Satirlar)
            {
                turler.Add(k.OlayTuru);
            }

            var kurallar = KuralYukleyici.Yukle(Butce ? Kurallar.Substring(0, Kurallar.LastIndexOf(']')) + ButceKurallari + "] }" : Kurallar, "kurallar.json", turler);
            if (!kurallar.Basarili)
            {
                throw new InvalidOperationException(string.Join("\n", kurallar.Hatalar));
            }

            return Simulasyon.Olustur(senaryo.Senaryo, kurallar.Kurallar, seed, eylemler: eylemler.Eylemler, krizler: kriz.Satirlar);
        }

        /// <summary>Oyuncunun müdahaleleri: t1'de dört borç söylentisi (han'daki ilk dört NPC'ye); sonra altı kez yardım/tehdit (han'daki NPC'lere).</summary>
        public static void Girdi(Simulasyon sim, int tick)
        {
            var handa = sim.Zihinler.Kimlikler.Where(n => sim.Dunya.TekDeger("konum!", n) == "han").ToList();
            if (handa.Count == 0)
            {
                return;
            }

            if (tick == 1)
            {
                for (var i = 0; i < Math.Min(4, handa.Count); i++)
                {
                    sim.Submit(new PlayerAction(
                        "anlatti", handa[i],
                        new[] { new KeyValuePair<string, string>("onerme", "borclu(N" + (i + 6).ToString("D2", CultureInfo.InvariantCulture) + ",N" + i.ToString("D2", CultureInfo.InvariantCulture) + ",500)") }));
                }
            }
            else if (tick == 20 || tick == 60 || tick == 100)
            {
                sim.Submit(new PlayerAction("yardim_etti", handa[tick % handa.Count]));
            }
            else if (tick == 40 || tick == 80 || tick == 120)
            {
                sim.Submit(new PlayerAction("tehdit_etti", handa[tick % handa.Count]));
            }
        }
    }
}
