using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Yanki.Core.Events;
using Yanki.Core.Json;
using Yanki.Core.World;

namespace Yanki.Core.Decision
{
    /// <summary>Kısa izdeki top-N havuz adayı: <c>eylem|hedef</c> anahtarı ve puanı (puanlamayan seçicide <c>null</c>).</summary>
    public sealed class KisaHavuzAdayi
    {
        /// <summary><c>eylem|hedef</c>.</summary>
        public string Anahtar { get; }

        /// <summary>Son puan; puanlamayan seçicide <c>null</c>.</summary>
        public decimal? Puan { get; }

        /// <summary>Kurar.</summary>
        public KisaHavuzAdayi(string anahtar, decimal? puan)
        {
            Anahtar = anahtar ?? throw new ArgumentNullException(nameof(anahtar));
            Puan = puan;
        }
    }

    /// <summary>
    /// Günlükteki karar izi (K3, ADR-011): <c>karar_verildi</c> olayının <c>iz</c> yükü. Seçilen, top-N havuzu ve puanları, zar,
    /// kaynak olaylar ve kaynaksız girdiler ile aday/elenen sayılarını taşır; bütün adayları ve puan bileşenlerini taşımaz.
    /// Geçmiş bir kararın TAM izi <see cref="KararIziYenidenUretici"/> ile replay üzerinden üretilir ve bu kısa izle doğrulanır.
    /// </summary>
    public sealed class KisaIz
    {
        /// <summary>Kısa iz biçim etiketi (<c>surum</c> alanı).</summary>
        public const string SurumEtiketi = "kisa";

        /// <summary>Karar veren NPC.</summary>
        public string Npc { get; }

        /// <summary>Kararın tick'i.</summary>
        public long Tick { get; }

        /// <summary>Havuz büyüklüğü ayarı.</summary>
        public int EnIyiN { get; }

        /// <summary>Havuz eşiği ayarı.</summary>
        public decimal Esik { get; }

        /// <summary>Puanlanan (ön koşulları tutan) aday sayısı.</summary>
        public int PuanlananSayisi { get; }

        /// <summary>Elenen aday sayısı.</summary>
        public int ElenenSayisi { get; }

        /// <summary>Top-N havuzu, seçim sırasıyla.</summary>
        public IReadOnlyList<KisaHavuzAdayi> Havuz { get; }

        /// <summary>Atılan zar; atılmadıysa <c>null</c>.</summary>
        public decimal? Zar { get; }

        /// <summary>Seçilen eylemin kimliği; seçim yoksa boş.</summary>
        public string SecilenEylem { get; }

        /// <summary>Seçilen hedef; hedefsiz ya da seçim yoksa boş.</summary>
        public string SecilenHedef { get; }

        /// <summary>Seçilen eylemin çeliştiği kişilik özellikleri.</summary>
        public IReadOnlyList<string> CelisenOzellikler { get; }

        /// <summary>Karara katkı veren zihin olaylarının kimlikleri.</summary>
        public IReadOnlyList<long> KaynakOlaylar { get; }

        /// <summary>Kaynağı bulunamayan girdiler.</summary>
        public IReadOnlyList<string> Kaynaksiz { get; }

        /// <summary>Okunan kanonik JSON metni.</summary>
        public string Metin { get; }

        private KisaIz(
            string metin, string npc, long tick, int n, decimal esik, int puanlanan, int elenen, IList<KisaHavuzAdayi> havuz, decimal? zar,
            string secilenEylem, string secilenHedef, IList<string> celisen, IList<long> kaynaklar, IList<string> kaynaksiz)
        {
            Metin = metin;
            Npc = npc;
            Tick = tick;
            EnIyiN = n;
            Esik = esik;
            PuanlananSayisi = puanlanan;
            ElenenSayisi = elenen;
            Havuz = new ReadOnlyCollection<KisaHavuzAdayi>(havuz);
            Zar = zar;
            SecilenEylem = secilenEylem;
            SecilenHedef = secilenHedef;
            CelisenOzellikler = new ReadOnlyCollection<string>(celisen);
            KaynakOlaylar = new ReadOnlyCollection<long>(kaynaklar);
            Kaynaksiz = new ReadOnlyCollection<string>(kaynaksiz);
        }

        /// <summary><c>karar_verildi</c> olayının kısa izini okur.</summary>
        /// <exception cref="ArgumentException">Olay karar olayı değilse.</exception>
        /// <exception cref="FormatException">İz yükü kısa iz biçiminde değilse ya da bozuksa.</exception>
        public static KisaIz Oku(Olay karar)
        {
            if (karar == null)
            {
                throw new ArgumentNullException(nameof(karar));
            }

            if (karar.Tur != YerlesikTurler.KararVerildi)
            {
                throw new ArgumentException($"Olay #{karar.Id} bir karar_verildi olayı değil ({karar.Tur}).", nameof(karar));
            }

            try
            {
                return Metinden(karar.Deger("iz"));
            }
            catch (InvalidOperationException e)
            {
                throw new FormatException($"Olay #{karar.Id}: iz yükü okunamadı: {e.Message}", e);
            }
        }

        /// <summary>Kısa iz JSON metnini okur.</summary>
        /// <exception cref="FormatException">Metin kısa iz biçiminde değilse ya da bozuksa.</exception>
        public static KisaIz Metinden(string metin)
        {
            try
            {
                var k = JsonAyristirici.Ayristir(metin);
                if (k.Al("surum")?.MetinDeger != SurumEtiketi)
                {
                    throw new FormatException("Kısa iz bekleniyordu (surum: \"kisa\").");
                }

                var havuz = new List<KisaHavuzAdayi>();
                foreach (var h in Gerekli(k, "havuz").Elemanlar)
                {
                    var puan = Gerekli(h, "puan");
                    havuz.Add(new KisaHavuzAdayi(Gerekli(h, "aday").MetinDeger, puan.Tur == JsonTuru.Sayi ? puan.SayiDeger : (decimal?)null));
                }

                var secilen = Gerekli(k, "secilen");
                var celisen = new List<string>();
                var secilenEylem = string.Empty;
                var secilenHedef = string.Empty;
                if (secilen.Tur != JsonTuru.Bos)
                {
                    secilenEylem = Gerekli(secilen, "eylem").MetinDeger;
                    secilenHedef = Gerekli(secilen, "hedef").MetinDeger;
                    foreach (var c in Gerekli(secilen, "celisen_ozellikler").Elemanlar)
                    {
                        celisen.Add(c.MetinDeger);
                    }
                }

                var kaynaklar = new List<long>();
                foreach (var o in Gerekli(k, "kaynak_olaylar").Elemanlar)
                {
                    kaynaklar.Add((long)o.SayiDeger);
                }

                var kaynaksiz = new List<string>();
                foreach (var o in Gerekli(k, "kaynaksiz").Elemanlar)
                {
                    kaynaksiz.Add(o.MetinDeger);
                }

                var ayarlar = Gerekli(k, "ayarlar");
                var zar = Gerekli(k, "zar");
                return new KisaIz(
                    metin, Gerekli(k, "npc").MetinDeger, (long)Gerekli(k, "tick").SayiDeger, (int)Gerekli(ayarlar, "en_iyi_n").SayiDeger,
                    Gerekli(ayarlar, "esik").SayiDeger, (int)Gerekli(k, "puanlanan").SayiDeger, (int)Gerekli(k, "elenen").SayiDeger, havuz,
                    zar.Tur == JsonTuru.Sayi ? zar.SayiDeger : (decimal?)null, secilenEylem, secilenHedef, celisen, kaynaklar, kaynaksiz);
            }
            catch (JsonHatasi e)
            {
                throw new FormatException("Kısa iz JSON'u okunamadı: " + e.Message, e);
            }
        }

        /// <summary>
        /// Bu kısa izi, yeniden üretilen tam izin kısa haliyle karşılaştırır: seçilen, havuz, puanlar, zar, sayılar, kaynaklar.
        /// Uyuşuyorsa <c>null</c>, uyuşmuyorsa ilk farkı alan adıyla açıklayan metin döner.
        /// </summary>
        public string? Fark(KararIzi tam)
        {
            if (tam == null)
            {
                throw new ArgumentNullException(nameof(tam));
            }

            var yeniden = Metinden(KararIziYazici.YazKisa(tam));
            if (string.Equals(Metin, yeniden.Metin, StringComparison.Ordinal))
            {
                return null;
            }

            string? F(string alan, string log, string yenid) => string.Equals(log, yenid, StringComparison.Ordinal) ? null : $"{alan}: günlük \"{log}\", yeniden üretilen \"{yenid}\"";
            string S(decimal? d) => d.HasValue ? KanonikYazici.Sayi(d.Value) : "yok";
            string Liste(IEnumerable<string> l) => string.Join(",", l);
            var havuzMetni = new Func<KisaIz, string>(x =>
            {
                var parcalar = new List<string>();
                foreach (var h in x.Havuz)
                {
                    parcalar.Add(h.Anahtar + "=" + S(h.Puan));
                }

                return string.Join(",", parcalar);
            });
            var kaynakMetni = new Func<KisaIz, string>(x =>
            {
                var parcalar = new List<string>();
                foreach (var o in x.KaynakOlaylar)
                {
                    parcalar.Add(o.ToString(CultureInfo.InvariantCulture));
                }

                return string.Join(",", parcalar);
            });

            return F("npc", Npc, yeniden.Npc)
                ?? F("tick", Tick.ToString(CultureInfo.InvariantCulture), yeniden.Tick.ToString(CultureInfo.InvariantCulture))
                ?? F("ayarlar", EnIyiN.ToString(CultureInfo.InvariantCulture) + "/" + KanonikYazici.Sayi(Esik), yeniden.EnIyiN.ToString(CultureInfo.InvariantCulture) + "/" + KanonikYazici.Sayi(yeniden.Esik))
                ?? F("puanlanan/elenen", PuanlananSayisi + "/" + ElenenSayisi, yeniden.PuanlananSayisi + "/" + yeniden.ElenenSayisi)
                ?? F("havuz ve puanlar", havuzMetni(this), havuzMetni(yeniden))
                ?? F("zar", S(Zar), S(yeniden.Zar))
                ?? F("secilen", SecilenEylem + "|" + SecilenHedef, yeniden.SecilenEylem + "|" + yeniden.SecilenHedef)
                ?? F("celisen_ozellikler", Liste(CelisenOzellikler), Liste(yeniden.CelisenOzellikler))
                ?? F("kaynak_olaylar", kaynakMetni(this), kaynakMetni(yeniden))
                ?? F("kaynaksiz", Liste(Kaynaksiz), Liste(yeniden.Kaynaksiz))
                ?? "iz metni farklı (alanlar eşit görünüyor)";
        }

        private static JsonDegeri Gerekli(JsonDegeri nesne, string anahtar) =>
            nesne.Al(anahtar) ?? throw new FormatException($"Kısa izde \"{anahtar}\" yok.");
    }
}
