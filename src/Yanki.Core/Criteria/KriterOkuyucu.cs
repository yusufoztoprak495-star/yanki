using System;
using System.Collections.Generic;
using Yanki.Core.Json;

namespace Yanki.Core.Criteria
{
    /// <summary>
    /// Kriteri JSON nesnesinden okur (docs/criteria-language.md): <c>alan</c>, <c>op</c>, <c>deger</c>, isteğe bağlı
    /// <c>agirlik</c> (varsayılan 1) ve <c>zorunlu</c> (varsayılan false). İkinci bir kriter yolu değildir; ayrıştırma ve
    /// doğrulama <see cref="Kriter.Olustur"/>'a devredilir. Kural, diyalog şablonu ve eylem ön koşulu aynı okuyucuyu kullanır.
    /// </summary>
    public static class KriterOkuyucu
    {
        /// <summary>Kriter nesnesinde izin verilen anahtarlar.</summary>
        private static readonly string[] Anahtarlar = { "alan", "op", "deger", "agirlik", "zorunlu" };

        /// <summary>Operatör yazımından <see cref="KriterOperatoru"/>'na; bilinmiyorsa <c>false</c>.</summary>
        public static bool OperatorCoz(string metin, out KriterOperatoru op)
        {
            switch (metin)
            {
                case "<": op = KriterOperatoru.Kucuk; return true;
                case "<=": op = KriterOperatoru.KucukEsit; return true;
                case ">": op = KriterOperatoru.Buyuk; return true;
                case ">=": op = KriterOperatoru.BuyukEsit; return true;
                case "==": op = KriterOperatoru.Esit; return true;
                case "!=": op = KriterOperatoru.EsitDegil; return true;
                case "aralik": op = KriterOperatoru.Aralik; return true;
                default: op = KriterOperatoru.Esit; return false;
            }
        }

        /// <summary>
        /// Bir <c>alan</c> ifadesini ayrıştırır ve sözlüğe göre doğrular. Kriterler ve eylem ölçütleri aynı yolu kullanır (ikinci sorgu yolu yoktur).
        /// </summary>
        /// <exception cref="KriterHatasi">İfade ya da çağrı geçersizse.</exception>
        public static SorguCagrisi AlanOku(string alan)
        {
            var cagri = IfadeAyristirici.Ayristir(alan);
            SorguSozlugu.Dogrula(cagri);
            return cagri;
        }

        /// <summary>Kriter nesnesini okur ve doğrular.</summary>
        /// <exception cref="KriterHatasi">Şema, ifade, operatör ya da değer geçersizse. Mesajda kaynak konumu yoktur; çağıran <see cref="JsonDegeri.Satir"/> ile ekler.</exception>
        public static Kriter Oku(JsonDegeri nesne)
        {
            if (nesne == null)
            {
                throw new ArgumentNullException(nameof(nesne));
            }

            if (nesne.Tur != JsonTuru.Nesne)
            {
                throw new KriterHatasi("Kriter bir JSON nesnesi olmalı");
            }

            foreach (var kv in nesne.Ozellikler)
            {
                if (Array.IndexOf(Anahtarlar, kv.Key) < 0)
                {
                    throw new KriterHatasi($"Kriterde bilinmeyen alan \"{kv.Key}\" (alan, op, deger, agirlik, zorunlu)");
                }
            }

            var alan = Gerekli(nesne, "alan", JsonTuru.Metin);
            var opMetni = Gerekli(nesne, "op", JsonTuru.Metin).MetinDeger;
            if (!OperatorCoz(opMetni, out var op))
            {
                throw new KriterHatasi($"Bilinmeyen operatör \"{opMetni}\" (<, <=, >, >=, ==, !=, aralik)");
            }

            var deger = DegerOku(Gerekli(nesne, "deger", null));

            var agirlik = 1m;
            var agirlikDugumu = nesne.Al("agirlik");
            if (agirlikDugumu != null)
            {
                if (agirlikDugumu.Tur != JsonTuru.Sayi)
                {
                    throw new KriterHatasi("\"agirlik\" sayı olmalı");
                }

                agirlik = agirlikDugumu.SayiDeger;
            }

            var zorunlu = false;
            var zorunluDugumu = nesne.Al("zorunlu");
            if (zorunluDugumu != null)
            {
                if (zorunluDugumu.Tur != JsonTuru.Mantiksal)
                {
                    throw new KriterHatasi("\"zorunlu\" true ya da false olmalı");
                }

                zorunlu = zorunluDugumu.MantiksalDeger;
            }

            return Kriter.Olustur(alan.MetinDeger, op, deger, agirlik, zorunlu);
        }

        private static JsonDegeri Gerekli(JsonDegeri nesne, string anahtar, JsonTuru? tur)
        {
            var d = nesne.Al(anahtar) ?? throw new KriterHatasi($"Kriterde \"{anahtar}\" alanı gerekli");
            if (tur.HasValue && d.Tur != tur.Value)
            {
                throw new KriterHatasi($"Kriterde \"{anahtar}\" {(tur.Value == JsonTuru.Metin ? "metin" : "sayı")} olmalı");
            }

            return d;
        }

        private static SorguDegeri DegerOku(JsonDegeri d)
        {
            switch (d.Tur)
            {
                case JsonTuru.Sayi:
                    return SorguDegeri.Sayi(d.SayiDeger);
                case JsonTuru.Mantiksal:
                    return SorguDegeri.Mantiksal(d.MantiksalDeger);
                case JsonTuru.Metin:
                    return SorguDegeri.Metin(d.MetinDeger);
                case JsonTuru.Dizi:
                    if (d.Elemanlar.Count != 2 || d.Elemanlar[0].Tur != JsonTuru.Sayi || d.Elemanlar[1].Tur != JsonTuru.Sayi)
                    {
                        throw new KriterHatasi("Aralık değeri iki sayıdan oluşan bir dizi olmalı: [alt, ust]");
                    }

                    try
                    {
                        return SorguDegeri.Aralik(d.Elemanlar[0].SayiDeger, d.Elemanlar[1].SayiDeger);
                    }
                    catch (ArgumentException e)
                    {
                        throw new KriterHatasi(e.Message);
                    }

                default:
                    throw new KriterHatasi("\"deger\" sayı, true/false, metin ya da [alt, ust] olmalı");
            }
        }
    }
}
