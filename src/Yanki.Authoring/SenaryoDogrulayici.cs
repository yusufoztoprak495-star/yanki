using System;
using System.Collections.Generic;
using System.Globalization;
using Yanki.Core.Decision;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Dialogue;

namespace Yanki.Authoring
{
    /// <summary>
    /// <c>yanki validate</c> mantığı (9. hafta, V kararı): tek giriş, mevcut yükleyicilerin hepsini çağırır
    /// (<see cref="SenaryoYukleyici"/>, <see cref="EylemYukleyici"/>, <see cref="KrizYukleyici"/>, <see cref="KuralYukleyici"/>, <see cref="SablonYukleyici"/>),
    /// ikinci bir ayrıştırma yolu açmaz. Hata ve uyarıların hepsi birlikte döner. Dosya sistemine dokunmaz: metin ve dosya adı alır.
    /// </summary>
    public static class SenaryoDogrulayici
    {
        /// <summary>Senaryoyu, bağlı dosyaları, isteğe bağlı şablon ve menüyü doğrular.</summary>
        public static DogrulamaSonucu Dogrula(DogrulamaGirdisi girdi) => Calistir(girdi, out _);

        /// <summary>
        /// Doğrular ve hata yoksa yüklenmiş nesneleri de verir (coverage aynı yükleme yolunu kullanır; ikinci yükleme yolu yok).
        /// Hata varsa <paramref name="yuklenen"/> <c>null</c>'dır.
        /// </summary>
        public static DogrulamaSonucu Yukle(DogrulamaGirdisi girdi, out YuklenenSenaryo? yuklenen) => Calistir(girdi, out yuklenen);

        private static DogrulamaSonucu Calistir(DogrulamaGirdisi girdi, out YuklenenSenaryo? yuklenen)
        {
            yuklenen = null;
            if (girdi == null)
            {
                throw new ArgumentNullException(nameof(girdi));
            }

            var b = new List<Bulgu>();
            var senaryoSonucu = SenaryoYukleyici.Yukle(girdi.SenaryoMetni, girdi.SenaryoAdi);
            foreach (var h in senaryoSonucu.Hatalar)
            {
                b.Add(Cevir(h, hata: true, "senaryo"));
            }

            if (!senaryoSonucu.Basarili)
            {
                // Senaryo okunamadıysa bağlı dosyalar ve analizler anlamsız; yine de şablon yükleyicisi kendi hatalarını verir.
                SablonYukle(girdi, null, b);
                return new DogrulamaSonucu(b);
            }

            var senaryo = senaryoSonucu.Senaryo!;
            var dosyalar = new SortedDictionary<string, DosyaGirdisi>(StringComparer.Ordinal);
            foreach (var d in girdi.Dosyalar)
            {
                dosyalar[d.Ad] = d;
            }

            var eylemler = new List<EylemTanimi>();
            var eylemDosyalari = new List<KeyValuePair<string, IReadOnlyList<EylemTanimi>>>();
            foreach (var ad in senaryo.EylemDosyalari)
            {
                if (!Bul(girdi, dosyalar, ad, "eylem", b, out var dosya))
                {
                    continue;
                }

                var sonuc = EylemYukleyici.Yukle(dosya.Metin, dosya.GorunenAd);
                Ekle(b, sonuc.Hatalar, sonuc.Uyarilar, "eylem");
                if (sonuc.Basarili)
                {
                    eylemler.AddRange(sonuc.Eylemler);
                    eylemDosyalari.Add(new KeyValuePair<string, IReadOnlyList<EylemTanimi>>(dosya.GorunenAd, sonuc.Eylemler));
                }
            }

            EylemKatalogu? katalog = null;
            try
            {
                katalog = new EylemKatalogu(eylemler);
            }
            catch (ArgumentException e)
            {
                b.Add(new Bulgu(girdi.SenaryoAdi, 1, true, "eylem", string.Empty, e.Message));
            }

            var krizler = new List<KrizSatiri>();
            var krizSeviye = new StresAyarlari().Esikler.Count;
            var krizBasarili = true;
            foreach (var ad in senaryo.KrizDosyalari)
            {
                if (!Bul(girdi, dosyalar, ad, "kriz", b, out var dosya))
                {
                    krizBasarili = false;
                    continue;
                }

                var sonuc = KrizYukleyici.Yukle(dosya.Metin, dosya.GorunenAd, krizSeviye, yedekDenetle: false);
                Ekle(b, sonuc.Hatalar, sonuc.Uyarilar, "kriz");
                if (sonuc.Basarili)
                {
                    krizler.AddRange(sonuc.Satirlar);
                }
                else
                {
                    krizBasarili = false;
                }
            }

            if (krizBasarili && krizler.Count > 0)
            {
                foreach (var h in KrizYukleyici.YedekHatalari(krizler, krizSeviye, girdi.SenaryoAdi))
                {
                    b.Add(Cevir(h, hata: true, "kriz"));
                }
            }

            var kuralTurleri = new List<string>(senaryo.OlayTurleri);
            if (katalog != null)
            {
                kuralTurleri.AddRange(katalog.OlayTurleri);
            }

            foreach (var k in krizler)
            {
                kuralTurleri.Add(k.OlayTuru);
            }

            var kurallar = new List<KuralTanimi>();
            var kuralBasarili = true;
            foreach (var ad in senaryo.KuralDosyalari)
            {
                if (!Bul(girdi, dosyalar, ad, "kural", b, out var dosya))
                {
                    kuralBasarili = false;
                    continue;
                }

                var sonuc = KuralYukleyici.Yukle(dosya.Metin, dosya.GorunenAd, kuralTurleri);
                Ekle(b, sonuc.Hatalar, sonuc.Uyarilar, "kural");
                if (sonuc.Basarili)
                {
                    kurallar.AddRange(sonuc.Kurallar);
                }
                else
                {
                    kuralBasarili = false;
                }
            }

            var hepsiYuklendi = katalog != null && krizBasarili && kuralBasarili && !HataVar(b);
            Simulasyon? sim = null;
            if (hepsiYuklendi)
            {
                try
                {
                    sim = Simulasyon.Olustur(senaryo, kurallar, 1UL, eylemler: katalog!.Eylemler, krizler: krizler);
                }
                catch (ArgumentException e)
                {
                    b.Add(new Bulgu(girdi.SenaryoAdi, 1, true, "senaryo", string.Empty, e.Message));
                }
                catch (InvalidOperationException e)
                {
                    b.Add(new Bulgu(girdi.SenaryoAdi, 1, true, "senaryo", string.Empty, e.Message));
                }
            }

            var sablon = SablonYukle(girdi, senaryo, b);

            if (hepsiYuklendi)
            {
                var bilgi = new SenaryoBilgisi(girdi, senaryo, eylemler, krizler, kurallar);
                KisilikKullanimi.Denetle(bilgi, sablon, girdi, b);
                KorumaliDenetimi.Denetle(bilgi, girdi, b);
                if (sablon != null)
                {
                    SablonUlasilabilirligi.Denetle(bilgi, sablon, girdi.Sablon!.GorunenAd, b);
                }
            }

            if (girdi.Menu != null)
            {
                MenuDenetimi.Denetle(girdi, senaryo, kurallar, katalog, krizler, sim != null, b);
            }

            var genel = new DogrulamaSonucu(b);
            if (sim != null && genel.HataSayisi == 0)
            {
                yuklenen = new YuklenenSenaryo(senaryo, kurallar, katalog!.Eylemler, krizler, sablon);
            }

            return genel;
        }

        private static bool HataVar(List<Bulgu> b)
        {
            foreach (var x in b)
            {
                if (x.Hata)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Bul(
            DogrulamaGirdisi girdi, SortedDictionary<string, DosyaGirdisi> dosyalar, string ad, string tur, List<Bulgu> b, out DosyaGirdisi dosya)
        {
            if (dosyalar.TryGetValue(ad, out dosya!))
            {
                return true;
            }

            b.Add(new Bulgu(girdi.SenaryoAdi, SatirBul(girdi.SenaryoMetni, "\"" + ad + "\""), true, "senaryo", string.Empty, tur + " dosyası bulunamadı: " + ad));
            return false;
        }

        private static void Ekle(List<Bulgu> b, IEnumerable<KuralHatasi> hatalar, IEnumerable<KuralHatasi> uyarilar, string varsayilanTur)
        {
            foreach (var h in hatalar)
            {
                b.Add(Cevir(h, hata: true, varsayilanTur));
            }

            foreach (var u in uyarilar)
            {
                b.Add(Cevir(u, hata: false, varsayilanTur));
            }
        }

        private static Bulgu Cevir(KuralHatasi h, bool hata, string varsayilanTur) =>
            new Bulgu(h.Kaynak, h.Satir, hata, h.KuralId.Length > 0 ? h.Etiket : varsayilanTur, h.KuralId, h.Mesaj);

        private static SablonDosyasi? SablonYukle(DogrulamaGirdisi girdi, SenaryoTanimi? senaryo, List<Bulgu> b)
        {
            if (girdi.Sablon == null)
            {
                return null;
            }

            var sonuc = SablonYukleyici.Yukle(girdi.Sablon.Metin, girdi.Sablon.GorunenAd);
            foreach (var h in sonuc.Hatalar)
            {
                b.Add(SablonBulgusu(h, hata: true));
            }

            foreach (var u in sonuc.Uyarilar)
            {
                b.Add(SablonBulgusu(u, hata: false));
            }

            return sonuc.Basarili ? sonuc.Dosya : null;
        }

        // SablonYukleyici hata/uyarıyı zaten biçimlenmiş verir: dosya:satir: tur "id": mesaj. Dosya adı ':' içerse bile sondan çözülür.
        private static Bulgu SablonBulgusu(string metin, bool hata)
        {
            var i = 0;
            while (true)
            {
                var j = metin.IndexOf(':', i);
                if (j < 0 || j + 1 >= metin.Length)
                {
                    return new Bulgu(string.Empty, 0, hata, "sablon", string.Empty, metin);
                }

                var k = j + 1;
                while (k < metin.Length && metin[k] >= '0' && metin[k] <= '9')
                {
                    k++;
                }

                if (k > j + 1 && k + 1 < metin.Length && metin[k] == ':' && metin[k + 1] == ' ')
                {
                    var dosya = metin.Substring(0, j);
                    var satir = int.Parse(metin.Substring(j + 1, k - j - 1), CultureInfo.InvariantCulture);
                    var kalan = metin.Substring(k + 2);
                    var tur = "sablon";
                    var kimlik = string.Empty;
                    var q1 = kalan.IndexOf(" \"", StringComparison.Ordinal);
                    var q2 = q1 < 0 ? -1 : kalan.IndexOf("\": ", q1 + 2, StringComparison.Ordinal);
                    if (q1 > 0 && q2 > q1 && kalan.IndexOf(' ') == q1)
                    {
                        tur = kalan.Substring(0, q1);
                        kimlik = kalan.Substring(q1 + 2, q2 - q1 - 2);
                        kalan = kalan.Substring(q2 + 3);
                    }

                    if (kalan.StartsWith("uyarı: ", StringComparison.Ordinal))
                    {
                        kalan = kalan.Substring(7);
                    }

                    return new Bulgu(dosya, satir, hata, tur, kimlik, kalan);
                }

                i = j + 1;
            }
        }

        /// <summary>Metinde <paramref name="parca"/>'nın ilk geçtiği satır (1 tabanlı); yoksa 1.</summary>
        internal static int SatirBul(string metin, string parca)
        {
            var i = metin.IndexOf(parca, StringComparison.Ordinal);
            if (i < 0)
            {
                return 1;
            }

            var satir = 1;
            for (var k = 0; k < i; k++)
            {
                if (metin[k] == '\n')
                {
                    satir++;
                }
            }

            return satir;
        }
    }

    /// <summary>Doğrulama analizlerinin paylaştığı yüklenmiş senaryo bilgisi.</summary>
    internal sealed class SenaryoBilgisi
    {
        public DogrulamaGirdisi Girdi { get; }

        public SenaryoTanimi Senaryo { get; }

        public IReadOnlyList<EylemTanimi> Eylemler { get; }

        public IReadOnlyList<KrizSatiri> Krizler { get; }

        public IReadOnlyList<KuralTanimi> Kurallar { get; }

        public SenaryoBilgisi(DogrulamaGirdisi girdi, SenaryoTanimi senaryo, IReadOnlyList<EylemTanimi> eylemler, IReadOnlyList<KrizSatiri> krizler, IReadOnlyList<KuralTanimi> kurallar)
        {
            Girdi = girdi;
            Senaryo = senaryo;
            Eylemler = eylemler;
            Krizler = krizler;
            Kurallar = kurallar;
        }

        /// <summary>Senaryodaki kişi ve mekan kimlikleri (NPC, oyuncu, mekan) ve gerçeklerde geçen her kimlik, ordinal sıralı.</summary>
        public SortedSet<string> Kimlikler()
        {
            var s = new SortedSet<string>(StringComparer.Ordinal) { Senaryo.OyuncuId };
            foreach (var n in Senaryo.Npcler)
            {
                s.Add(n.Id);
            }

            foreach (var m in Senaryo.Mekanlar)
            {
                s.Add(m);
            }

            foreach (var g in Senaryo.Gercekler)
            {
                foreach (var a in Argumanlar(g))
                {
                    s.Add(a);
                }
            }

            return s;
        }

        /// <summary>Önerme metninin (<c>yuklem(a, b)</c> / <c>degil(...)</c>) argümanları.</summary>
        public static List<string> Argumanlar(string onerme)
        {
            var sonuc = new List<string>();
            var ac = onerme.LastIndexOf('(');
            var kapa = onerme.IndexOf(')');
            if (ac < 0 || kapa < ac)
            {
                return sonuc;
            }

            foreach (var parca in onerme.Substring(ac + 1, kapa - ac - 1).Split(','))
            {
                var p = parca.Trim();
                if (p.Length > 0 && p != "*")
                {
                    sonuc.Add(p);
                }
            }

            return sonuc;
        }
    }
}
