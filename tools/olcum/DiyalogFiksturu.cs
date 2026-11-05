using System;
using System.Globalization;
using System.Text;
using Yanki.Dialogue;

namespace Yanki.Olcum
{
    /// <summary>
    /// <c>--diyalog</c> ölçüm modu için SAHTE şablon dosyası (8. hafta, DURAK 3 ölçümü). İçerik değil HACİM ölçer: gerçekçi fikstürdeki olay türlerini eşler, birkaç şablonda kriter,
    /// hafıza göndermesi ve followup bulunur. Metinler Yusuf'un yazacağı içerik DEĞİLDİR.
    /// </summary>
    internal static class DiyalogFiksturu
    {
        private static readonly string[] Adlar = { "Ayşe", "Can", "Hatice", "Selim", "Mehmet", "Ali", "Kemal", "Zeynep", "Murat", "Fatma" };

        public static SablonDosyasi Uret(int npcSayisi)
        {
            var sb = new StringBuilder();
            sb.Append("{ \"dil\": \"tr\", \"adlar\": { \"oyuncu\": { \"ad\": \"yabancı\", \"cins\": true }");
            for (var i = 0; i < npcSayisi; i++)
            {
                var kimlik = "N" + i.ToString("D2", CultureInfo.InvariantCulture);
                sb.Append(", \"").Append(kimlik).Append("\": \"").Append(Adlar[i % Adlar.Length]).Append('"');
            }

            sb.Append(" }, ");
            sb.Append(@"""onerme_metinleri"": { ""borclu"": { ""metin"": ""{0}, {1.yonelme} {2} lira borçlu"", ""olumsuz"": ""{0}, {1.yonelme} {2} lira borçlu değil"", ""sayilar"": [2] } },
 ""esleme"": {
  ""anlatti"": { ""niyet"": ""anlatma"", ""konusan"": ""fail"" },
  ""yardim_oldu"": { ""niyet"": ""tesekkur"", ""konusan"": ""hedef"" }, ""yardim_etti"": { ""niyet"": ""tesekkur"", ""konusan"": ""hedef"" },
  ""tehdit_oldu"": { ""niyet"": ""tehdit"", ""konusan"": ""hedef"" }, ""tehdit_etti"": { ""niyet"": ""tehdit"", ""konusan"": ""hedef"" },
  ""sohbet_oldu"": { ""niyet"": ""sohbet"", ""konusan"": ""hedef"" },
  ""itiraf_etti"": { ""niyet"": ""itiraf"", ""konusan"": ""fail"" }, ""patladi"": { ""niyet"": ""patlama"", ""konusan"": ""fail"" }
 },
 ""gramer"": { ""giris"": [""Dinle"", ""Bak"", ""Şunu duydum""] },
 ""sablonlar"": [
  { ""id"": ""anlat_genel"", ""concept"": ""anlatma"", ""genel"": true, ""sonra"": { ""hedef"": ""dinleyen"", ""concept"": ""cevap"" }, ""metin"": ""#giris# #dinleyen#: #konu#."" },
  { ""id"": ""anlat_gergin"", ""concept"": ""anlatma"", ""kriterler"": [ { ""alan"": ""stres()"", ""op"": "">="", ""deger"": 5, ""zorunlu"": true, ""agirlik"": 3 } ], ""metin"": ""Gergin: #konu#."" },
  { ""id"": ""tesekkur_genel"", ""concept"": ""tesekkur"", ""genel"": true, ""metin"": ""Sağ ol #dinleyen#."" },
  { ""id"": ""tesekkur_minnetli"", ""concept"": ""tesekkur"", ""kriterler"": [ { ""alan"": ""modifier_var(dinleyen, minnet)"", ""op"": ""=="", ""deger"": true, ""zorunlu"": true } ], ""tekrar"": { ""tekrar_bekleme"": 12 }, ""metin"": ""Unutmam #dinleyen.yonelme#."" },
  { ""id"": ""tehdit_genel"", ""concept"": ""tehdit"", ""genel"": true, ""sonra"": { ""hedef"": ""dinleyen"", ""concept"": ""cevap"" }, ""metin"": ""Beni tehdit etme #dinleyen#."" },
  { ""id"": ""tehdit_gecmis"", ""concept"": ""tehdit"", ""kriterler"": [ { ""alan"": ""hafiza_var(tehdit_oldu, dinleyen, 48)"", ""op"": ""=="", ""deger"": true, ""zorunlu"": true } ],
    ""hafiza"": { ""tur"": ""tehdit_oldu"", ""fail"": ""dinleyen"", ""son_n_tick"": 48 }, ""metin"": ""#hafiza.zaman# da böyle yapmıştın #dinleyen#."" },
  { ""id"": ""sohbet_genel"", ""concept"": ""sohbet"", ""genel"": true, ""metin"": ""Güzel sohbet #dinleyen#."" },
  { ""id"": ""itiraf_genel"", ""concept"": ""itiraf"", ""genel"": true, ""metin"": ""İtiraf ediyorum #dinleyen.yonelme#."" },
  { ""id"": ""patlama_genel"", ""concept"": ""patlama"", ""genel"": true, ""metin"": ""Yeter artık!"" },
  { ""id"": ""cevap_genel"", ""concept"": ""cevap"", ""genel"": true, ""metin"": ""Haklısın."" },
  { ""id"": ""cevap_kinli"", ""concept"": ""cevap"", ""kriterler"": [ { ""alan"": ""kisilik(kin_tutma)"", ""op"": "">="", ""deger"": 0.6, ""zorunlu"": true } ], ""metin"": ""Bunu unutmam."" }
 ] }");

            var sonuc = SablonYukleyici.Yukle(sb.ToString(), "olcum-sablon.json");
            if (!sonuc.Basarili)
            {
                throw new InvalidOperationException(string.Join("\n", sonuc.Hatalar));
            }

            return sonuc.Dosya!;
        }
    }
}
