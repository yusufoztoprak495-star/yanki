using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Criteria;
using Yanki.Core.Decision;
using Yanki.Core.Json;
using Yanki.Core.Rules;
using Yanki.Core.World;

namespace Yanki.Core.Simulation
{
    /// <summary>Kriz satırının hedefi: krizin yöneldiği kişi nasıl bulunur.</summary>
    public enum KrizHedefTuru
    {
        /// <summary>Hedefsiz.</summary>
        Yok,

        /// <summary>Konuşanın EN SON yalan söylediği kişi (aktarım kaydından); aynı mekanda değilse satır elenir.</summary>
        YalanKurbani,

        /// <summary>Aynı mekandaki (oyuncu dahil) en yüksek fikirli kişi; eşitlikte kimlik ordinal.</summary>
        EnYakin,

        /// <summary>Aynı mekandaki (oyuncu dahil) en düşük fikirli kişi; eşitlikte kimlik ordinal.</summary>
        EnSoguk,
    }

    /// <summary><see cref="KrizHedefTuru"/> ile JSON adları arasında çeviri.</summary>
    public static class KrizHedefTurleri
    {
        /// <summary>Geçerli adlar.</summary>
        public const string GecerliAdlar = "yok, yalan_kurbani, en_yakin, en_soguk";

        /// <summary>Adı türe çevirir; bilinmiyorsa <c>false</c>.</summary>
        public static bool TryCoz(string ad, out KrizHedefTuru tur)
        {
            switch (ad)
            {
                case "yok": tur = KrizHedefTuru.Yok; return true;
                case "yalan_kurbani": tur = KrizHedefTuru.YalanKurbani; return true;
                case "en_yakin": tur = KrizHedefTuru.EnYakin; return true;
                case "en_soguk": tur = KrizHedefTuru.EnSoguk; return true;
                default: tur = KrizHedefTuru.Yok; return false;
            }
        }
    }

    /// <summary>
    /// Kriz tablosu satırı (plan 5.4, ADR-012): belirli bir kriz seviyesinde, kişiliğe uygun krizi tanımlar. Kriterler ADR-010 gibi
    /// "hepsi tutmalı"dır; tutan satırlar arasında en özgül (ağırlık toplamı yüksek) kazanır, eşitlikte anahtarlı RNG seçer.
    /// Kriterli olmayan satır o seviyenin yedeğidir.
    /// </summary>
    public sealed class KrizSatiri
    {
        /// <summary>Krizin <c>KuralId</c> öneki: <c>kriz:&lt;id&gt;</c>.</summary>
        public const string KuralIdOneki = "kriz:";

        /// <summary>Kimlik.</summary>
        public string Id { get; }

        /// <summary>Seviye (1 tabanlı): <c>StresAyarlari.Esikler[Seviye - 1]</c> eşiğinin krizi.</summary>
        public int Seviye { get; }

        /// <summary>Krizin dışarı görünen olayının türü.</summary>
        public string OlayTuru { get; }

        /// <summary>Hedef türü.</summary>
        public KrizHedefTuru Hedef { get; }

        /// <summary>Kriterler (hepsi tutmalı).</summary>
        public IReadOnlyList<Kriter> Kriterler { get; }

        /// <summary>Etkiler (kural etkisi biçimi); boş olabilir, krizin kendisi ve stres düşüşü etkidir.</summary>
        public IReadOnlyList<KuralEtkisi> Etkiler { get; }

        /// <summary>Kaynak dosyadaki 1 tabanlı satır; bilinmiyorsa 0.</summary>
        public int Satir { get; }

        /// <summary>Olay ve stres düşüşü olaylarının <c>KuralId</c>'si.</summary>
        public string KuralId => KuralIdOneki + Id;

        /// <summary>Kriterli olmayan yedek satır mı.</summary>
        public bool Yedek => Kriterler.Count == 0;

        /// <summary>Satır kurar.</summary>
        /// <exception cref="ArgumentException">Kimlik/olay türü boşsa ya da seviye 1'den küçükse.</exception>
        public KrizSatiri(
            string id, int seviye, string olayTuru, KrizHedefTuru hedef, IEnumerable<Kriter> kriterler, IEnumerable<KuralEtkisi> etkiler, int satir = 0)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Kriz satırı kimliği boş olamaz.", nameof(id));
            }

            if (seviye < 1)
            {
                throw new ArgumentException("Kriz seviyesi 1'den küçük olamaz.", nameof(seviye));
            }

            if (string.IsNullOrEmpty(olayTuru))
            {
                throw new ArgumentException("Kriz olay türü boş olamaz.", nameof(olayTuru));
            }

            Id = id;
            Seviye = seviye;
            OlayTuru = olayTuru;
            Hedef = hedef;
            Kriterler = new ReadOnlyCollection<Kriter>(new List<Kriter>(kriterler ?? throw new ArgumentNullException(nameof(kriterler))));
            Etkiler = new ReadOnlyCollection<KuralEtkisi>(new List<KuralEtkisi>(etkiler ?? throw new ArgumentNullException(nameof(etkiler))));
            Satir = satir;
        }
    }

    /// <summary>Kriz dosyasının yükleme sonucu: hata yoksa satırlar (kimliğe göre ordinal sıralı), varsa yalnızca hatalar.</summary>
    public sealed class KrizYuklemeSonucu
    {
        /// <summary>Yüklenen satırlar; hata varsa boş.</summary>
        public IReadOnlyList<KrizSatiri> Satirlar { get; }

        /// <summary>Hatalar, dosyadaki sırayla (<c>dosya:satir: kriz "id": mesaj</c>).</summary>
        public IReadOnlyList<KuralHatasi> Hatalar { get; }

        /// <summary>Uyarılar (yüklemeyi engellemez).</summary>
        public IReadOnlyList<KuralHatasi> Uyarilar { get; }

        /// <summary>Hata yok mu.</summary>
        public bool Basarili => Hatalar.Count == 0;

        /// <summary>Sonuç kurar.</summary>
        public KrizYuklemeSonucu(IEnumerable<KrizSatiri> satirlar, IEnumerable<KuralHatasi> hatalar, IEnumerable<KuralHatasi>? uyarilar = null)
        {
            Satirlar = new ReadOnlyCollection<KrizSatiri>(new List<KrizSatiri>(satirlar));
            Hatalar = new ReadOnlyCollection<KuralHatasi>(new List<KuralHatasi>(hatalar));
            Uyarilar = new ReadOnlyCollection<KuralHatasi>(new List<KuralHatasi>(uyarilar ?? new KuralHatasi[0]));
        }
    }

    /// <summary>
    /// Kriz dosyasını okur ve doğrular. Biçim: <c>{ "krizler": [ { "id", "seviye", "olay_turu", "hedef", "kriterler", "etkiler" } ] }</c>.
    /// Hata biçimi: <c>dosya:satir: kriz "id": mesaj</c>.
    /// </summary>
    public static class KrizYukleyici
    {
        private static readonly string[] SatirAnahtarlari = { "id", "seviye", "olay_turu", "hedef", "kriterler", "etkiler" };

        /// <summary>
        /// Kriz dosyasını okur. <paramref name="seviyeSayisi"/> geçerli seviye sayısıdır (<c>StresAyarlari.Esikler.Count</c>);
        /// <paramref name="yedekDenetle"/> true ise her seviye için kriterli olmayan yedek satırın varlığı bu dosyada denetlenir
        /// (birden çok dosya varsa false verip <see cref="YedekHatalari"/> birleşik listeyle çağrılır).
        /// </summary>
        public static KrizYuklemeSonucu Yukle(string json, string kaynakAdi, int seviyeSayisi = 2, bool yedekDenetle = true)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var hatalar = new List<KuralHatasi>();
            var uyarilar = new List<KuralHatasi>();
            JsonDegeri kok;
            try
            {
                kok = JsonAyristirici.Ayristir(json);
            }
            catch (JsonHatasi e)
            {
                hatalar.Add(new KuralHatasi(kaynakAdi, e.Satir, string.Empty, "JSON okunamadı: " + e.Message, "kriz"));
                return new KrizYuklemeSonucu(new KrizSatiri[0], hatalar);
            }

            var dizi = kok.Tur == JsonTuru.Nesne ? kok.Al("krizler") : null;
            if (dizi == null || dizi.Tur != JsonTuru.Dizi)
            {
                hatalar.Add(new KuralHatasi(kaynakAdi, kok.Satir, string.Empty, "Kök nesnede \"krizler\" dizisi gerekli", "kriz"));
                return new KrizYuklemeSonucu(new KrizSatiri[0], hatalar);
            }

            foreach (var kv in kok.Ozellikler)
            {
                if (kv.Key != "krizler")
                {
                    hatalar.Add(new KuralHatasi(kaynakAdi, kv.Value.Satir, string.Empty, $"Kök nesnede bilinmeyen alan \"{kv.Key}\" (yalnızca \"krizler\")", "kriz"));
                }
            }

            var satirlar = new List<KrizSatiri>();
            var kimlikler = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var oge in dizi.Elemanlar)
            {
                var satir = SatirOku(oge, kaynakAdi, seviyeSayisi, hatalar, uyarilar);
                if (satir == null)
                {
                    continue;
                }

                if (!kimlikler.Add(satir.Id))
                {
                    hatalar.Add(new KuralHatasi(kaynakAdi, oge.Satir, satir.Id, $"Kriz satırı kimliği tekrar ediyor: \"{satir.Id}\"", "kriz"));
                    continue;
                }

                satirlar.Add(satir);
            }

            if (yedekDenetle)
            {
                hatalar.AddRange(YedekHatalari(satirlar, seviyeSayisi, kaynakAdi, kok.Satir));
            }

            if (hatalar.Count > 0)
            {
                return new KrizYuklemeSonucu(new KrizSatiri[0], hatalar, uyarilar);
            }

            satirlar.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return new KrizYuklemeSonucu(satirlar, hatalar, uyarilar);
        }

        /// <summary>Her seviye (1..<paramref name="seviyeSayisi"/>) için kriterli olmayan bir yedek satır olmalı; eksik her seviye bir hatadır.</summary>
        public static IReadOnlyList<KuralHatasi> YedekHatalari(IEnumerable<KrizSatiri> satirlar, int seviyeSayisi, string kaynakAdi, int satirNo = 0)
        {
            var yedekli = new SortedSet<int>();
            foreach (var s in satirlar)
            {
                if (s.Yedek)
                {
                    yedekli.Add(s.Seviye);
                }
            }

            var hatalar = new List<KuralHatasi>();
            for (var seviye = 1; seviye <= seviyeSayisi; seviye++)
            {
                if (!yedekli.Contains(seviye))
                {
                    hatalar.Add(new KuralHatasi(
                        kaynakAdi, satirNo, string.Empty,
                        $"seviye {seviye} için kriterli olmayan bir yedek satır yok (her seviyenin kriterleri tutmayan NPC'ler için bir yedek krizi olmalı)", "kriz"));
                }
            }

            return hatalar;
        }

        private static KrizSatiri? SatirOku(JsonDegeri oge, string kaynak, int seviyeSayisi, List<KuralHatasi> hatalar, List<KuralHatasi> uyarilar)
        {
            if (oge.Tur != JsonTuru.Nesne)
            {
                hatalar.Add(new KuralHatasi(kaynak, oge.Satir, string.Empty, "Kriz satırı bir JSON nesnesi olmalı", "kriz"));
                return null;
            }

            var once = hatalar.Count;
            var kimlik = string.Empty;
            var idDugumu = oge.Al("id");
            if (idDugumu == null || idDugumu.Tur != JsonTuru.Metin)
            {
                hatalar.Add(new KuralHatasi(kaynak, idDugumu?.Satir ?? oge.Satir, string.Empty, "Kriz satırında metin olarak \"id\" alanı gerekli", "kriz"));
            }
            else
            {
                kimlik = idDugumu.MetinDeger;
                if (!Kimlik.GecerliKimlikMi(kimlik))
                {
                    hatalar.Add(new KuralHatasi(kaynak, idDugumu.Satir, kimlik, $"\"id\" geçersiz kimlik: \"{kimlik}\" (ASCII harf/rakam/_, harf ya da _ ile başlar)", "kriz"));
                }
            }

            void Hata(int satir, string mesaj) => hatalar.Add(new KuralHatasi(kaynak, satir, kimlik, mesaj, "kriz"));

            foreach (var kv in oge.Ozellikler)
            {
                if (Array.IndexOf(SatirAnahtarlari, kv.Key) < 0)
                {
                    Hata(kv.Value.Satir, $"Kriz satırında bilinmeyen alan \"{kv.Key}\" ({string.Join(", ", SatirAnahtarlari)})");
                }
            }

            // seviye
            var seviye = 0;
            var seviyeDugumu = oge.Al("seviye");
            if (seviyeDugumu == null || seviyeDugumu.Tur != JsonTuru.Sayi)
            {
                Hata(seviyeDugumu?.Satir ?? oge.Satir, "Kriz satırında sayı olarak \"seviye\" alanı gerekli");
            }
            else if (seviyeDugumu.SayiDeger != decimal.Truncate(seviyeDugumu.SayiDeger) || seviyeDugumu.SayiDeger < 1m || seviyeDugumu.SayiDeger > seviyeSayisi)
            {
                Hata(seviyeDugumu.Satir, $"\"seviye\" 1 ile {seviyeSayisi} arasında bir tam sayı olmalı (kriz eşiği sayısı kadar seviye var)");
            }
            else
            {
                seviye = (int)seviyeDugumu.SayiDeger;
            }

            // olay türü
            var olayTuru = string.Empty;
            var olayDugumu = oge.Al("olay_turu");
            if (olayDugumu == null || olayDugumu.Tur != JsonTuru.Metin)
            {
                Hata(olayDugumu?.Satir ?? oge.Satir, "Kriz satırında metin olarak \"olay_turu\" alanı gerekli");
            }
            else if (!Kimlik.GecerliKimlikMi(olayDugumu.MetinDeger))
            {
                Hata(olayDugumu.Satir, $"\"olay_turu\" geçersiz kimlik: \"{olayDugumu.MetinDeger}\" (ASCII harf/rakam/_, harf ya da _ ile başlar)");
            }
            else if (YerlesikTurler.AyrilmisMi(olayDugumu.MetinDeger))
            {
                Hata(olayDugumu.Satir, $"\"olay_turu\" yerleşik olay türü ya da ayrılmış ad olamaz: \"{olayDugumu.MetinDeger}\"");
            }
            else
            {
                olayTuru = olayDugumu.MetinDeger;
            }

            // hedef
            var hedef = KrizHedefTuru.Yok;
            var hedefGecerli = false;
            var hedefDugumu = oge.Al("hedef");
            if (hedefDugumu == null || hedefDugumu.Tur != JsonTuru.Metin)
            {
                Hata(hedefDugumu?.Satir ?? oge.Satir, $"Kriz satırında metin olarak \"hedef\" alanı gerekli ({KrizHedefTurleri.GecerliAdlar})");
            }
            else if (!KrizHedefTurleri.TryCoz(hedefDugumu.MetinDeger, out hedef))
            {
                Hata(hedefDugumu.Satir, $"\"hedef\" bilinmeyen tür \"{hedefDugumu.MetinDeger}\" ({KrizHedefTurleri.GecerliAdlar})");
            }
            else
            {
                hedefGecerli = true;
            }

            var kume = !hedefGecerli ? RolKumesi.Hepsi : hedef == KrizHedefTuru.Yok ? EylemYukleyici.HedefsizRoller : EylemYukleyici.HedefliRoller;
            var eylemHedefTuru = hedef == KrizHedefTuru.Yok ? EylemHedefTuru.Yok : EylemHedefTuru.Npc;
            var roller = new SortedSet<Rol>();

            // kriterler (ADR-010: hepsi tutmalı; her kriter zorunlu sayılır, ağırlık özgüllük sıralaması içindir)
            var kriterler = new List<Kriter>();
            var kosulDugumu = oge.Al("kriterler");
            if (kosulDugumu != null)
            {
                if (kosulDugumu.Tur != JsonTuru.Dizi)
                {
                    Hata(kosulDugumu.Satir, "\"kriterler\" bir dizi olmalı");
                }
                else
                {
                    for (var i = 0; i < kosulDugumu.Elemanlar.Count; i++)
                    {
                        var kd = kosulDugumu.Elemanlar[i];
                        Kriter kriter;
                        try
                        {
                            kriter = KriterOkuyucu.Oku(kd);
                        }
                        catch (KriterHatasi e)
                        {
                            Hata(kd.Satir, $"kriter #{i + 1}: {e.Message}");
                            continue;
                        }

                        KuralYukleyici.KriterRolleriniDenetle(kriter, kd.Satir, i + 1, roller, Hata, kume);
                        EylemYukleyici.AlanBaglamDenetle(kriter.Alan, eylemHedefTuru, hedefGecerli, kd.Satir, $"kriter #{i + 1}: ", Hata, AktarimTuru.Yok);
                        kriterler.Add(Kriter.Olustur(kriter.Alan.ToString(), kriter.Op, kriter.Deger, kriter.Agirlik, true));
                    }
                }
            }

            // etkiler (boş olabilir)
            var etkiler = new List<KuralEtkisi>();
            var etkiDugumu = oge.Al("etkiler");
            if (etkiDugumu != null)
            {
                if (etkiDugumu.Tur != JsonTuru.Dizi)
                {
                    Hata(etkiDugumu.Satir, "\"etkiler\" bir dizi olmalı");
                }
                else
                {
                    for (var i = 0; i < etkiDugumu.Elemanlar.Count; i++)
                    {
                        var etki = KuralYukleyici.EtkiOku(etkiDugumu.Elemanlar[i], i + 1, roller, Hata, kume);
                        if (etki != null)
                        {
                            etkiler.Add(etki);
                        }
                    }
                }
            }

            if (hatalar.Count > once || kimlik.Length == 0)
            {
                return null;
            }

            if (kriterler.Count == 0 && hedef != KrizHedefTuru.Yok)
            {
                uyarilar.Add(new KuralHatasi(
                    kaynak, oge.Satir, kimlik,
                    "yedek satırın hedefi \"yok\" olmalı: hedefi aynı mekanda olmayan satır elenir ve yedek de elenirse kriz çıkmaz", "kriz"));
            }

            return new KrizSatiri(kimlik, seviye, olayTuru, hedef, kriterler, etkiler, oge.Satir);
        }
    }
}
