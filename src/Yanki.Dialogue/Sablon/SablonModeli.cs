using System;
using System.Collections.Generic;
using Yanki.Core.Criteria;
using Yanki.Core.Mind;
using Yanki.Dialogue.Gramer;

namespace Yanki.Dialogue
{
    /// <summary>
    /// Yüklenmiş şablon dosyası (8. hafta). Şablonlar senaryodan AYRI bir dosyadır: <c>senaryo_hash</c>'e girmez, kendi <see cref="SablonHash"/>'ini taşır (metin düzeltmesi eski
    /// kayıtları yüklenemez yapmaz). Çekirdek şablon dosyasını tanımaz; dosyayı CLI okur, burası metni ayrıştırır.
    /// </summary>
    public sealed class SablonDosyasi
    {
        internal SablonDosyasi(
            string dosyaAdi, string dil, string hash, SortedDictionary<string, AdKaydi> adlar, SortedDictionary<string, OnermeMetniKaydi> onermeMetinleri,
            EslemeTablosu esleme, GramerTablosu gramer, IReadOnlyList<Sablon> sablonlar)
        {
            DosyaAdi = dosyaAdi;
            Dil = dil;
            SablonHash = hash;
            Adlar = adlar;
            OnermeMetinleri = onermeMetinleri;
            Esleme = esleme;
            Gramer = gramer;
            Sablonlar = sablonlar;
        }

        /// <summary>Dosyanın adı (hata mesajlarında ve <c>--neden</c> çıktısında).</summary>
        public string DosyaAdi { get; }

        /// <summary>Dosya başına tek dil (<c>tr</c> ya da <c>en</c>). Bu hafta yalnızca Türkçe gramer yazılıdır.</summary>
        public string Dil { get; }

        /// <summary>Dosya metninin (CRLF/LF aynı) 16 karakterlik onaltılık hash'i.</summary>
        public string SablonHash { get; }

        /// <summary>Şablon sayısı.</summary>
        public int SablonSayisi => Sablonlar.Count;

        internal SortedDictionary<string, AdKaydi> Adlar { get; }

        internal SortedDictionary<string, OnermeMetniKaydi> OnermeMetinleri { get; }

        internal EslemeTablosu Esleme { get; }

        internal GramerTablosu Gramer { get; }

        internal IReadOnlyList<Sablon> Sablonlar { get; }
    }

    internal sealed class AdKaydi
    {
        public AdKaydi(string ad, bool ozelAd, bool ince, string? yumusak = null)
        {
            Ad = ad;
            OzelAd = ozelAd;
            Ince = ince;
            Yumusak = yumusak;
        }

        /// <summary>p, ç, t, k ile biten cins isimde ünlüyle başlayan ek için yazarın verdiği yumuşak gövde (borç → borc).</summary>
        public string? Yumusak { get; }

        public string Ad { get; }

        public bool OzelAd { get; }

        /// <summary>Yumuşak l (Kemal): yazar belirtir; ince ünlü eki alır.</summary>
        public bool Ince { get; }
    }

    internal sealed class OnermeParcasi
    {
        public OnermeParcasi(string sabit, int indeks, string? ek)
        {
            Sabit = sabit;
            Indeks = indeks;
            Ek = ek;
        }

        /// <summary>Düz metin parçası (<see cref="Indeks"/> -1 ise).</summary>
        public string Sabit { get; }

        /// <summary>Argüman yer tutucusunun sırası; düz metin parçasında -1.</summary>
        public int Indeks { get; }

        public string? Ek { get; }
    }

    internal sealed class OnermeMetniKaydi
    {
        public OnermeMetniKaydi(
            IReadOnlyList<OnermeParcasi> olumlu, IReadOnlyList<OnermeParcasi> olumsuz, IReadOnlyList<int> sayilar, IReadOnlyList<int> ozneler,
            SortedDictionary<string, OnermeVaryanti> varyantlar)
        {
            Olumlu = olumlu;
            Olumsuz = olumsuz;
            Sayilar = sayilar;
            Ozneler = ozneler;
            Varyantlar = varyantlar;
        }

        /// <summary>Özne olabilecek (konuşan ya da dinleyen olabilen) argüman sıraları: her biri için ben ve sen varyantı zorunludur.</summary>
        public IReadOnlyList<int> Ozneler { get; }

        /// <summary>Anahtar "ben:0", "sen:1" gibi (kişi:argüman sırası); eklemli yüklem elle yazılır, motor fiil çekimi yapmaz.</summary>
        public SortedDictionary<string, OnermeVaryanti> Varyantlar { get; }

        public IReadOnlyList<OnermeParcasi> Olumlu { get; }

        public IReadOnlyList<OnermeParcasi> Olumsuz { get; }

        /// <summary>Sayısal olduğu bildirilen argüman sıraları (bunlara ek getirilemez).</summary>
        public IReadOnlyList<int> Sayilar { get; }
    }

    internal sealed class EslemeKurali
    {
        public EslemeKurali(string olayTuru, string kaynak, string niyet, bool konusanFail, bool dinleyensiz, int satir)
        {
            Kaynak = kaynak;
            Dinleyensiz = dinleyensiz;
            OlayTuru = olayTuru;
            Niyet = niyet;
            KonusanFail = konusanFail;
            Satir = satir;
        }

        public string OlayTuru { get; }

        /// <summary>Olayın kaynağı: <c>oyuncu</c>, <c>npc</c> ya da boş (her ikisi).</summary>
        public string Kaynak { get; }

        /// <summary>Niyet dinleyensiz mi: satır mekanı taşır, kimin gördüğünü demo filtreler.</summary>
        public bool Dinleyensiz { get; }

        /// <summary>Niyet (concept) adı.</summary>
        public string Niyet { get; }

        /// <summary>Konuşan olayın failı mı (değilse hedefi).</summary>
        public bool KonusanFail { get; }

        public int Satir { get; }
    }

    internal enum TekrarTuru
    {
        Yok,
        Tekrarsiz,
        BirKez,
        Bekleme,
    }

    internal sealed class TekrarKurali
    {
        public static readonly TekrarKurali Yok = new TekrarKurali(TekrarTuru.Yok, 0);

        public TekrarKurali(TekrarTuru tur, long bekleme)
        {
            Tur = tur;
            Bekleme = bekleme;
        }

        public TekrarTuru Tur { get; }

        /// <summary><c>tekrar_bekleme</c> tick sayısı: <c>tick − son ≥ N</c> ise serbest.</summary>
        public long Bekleme { get; }
    }

    internal sealed class SonraTanimi
    {
        public SonraTanimi(string hedef, string concept)
        {
            Hedef = hedef;
            Concept = concept;
        }

        /// <summary><c>dinleyen</c> ya da sabit bir NPC kimliği.</summary>
        public string Hedef { get; }

        public string Concept { get; }
    }

    internal sealed class HafizaBildirimi
    {
        public HafizaBildirimi(string tur, string fail, long sonNTick)
        {
            Tur = tur;
            Fail = fail;
            SonNTick = sonNTick;
        }

        public string Tur { get; }

        /// <summary>Rol (<c>oyuncu</c>, <c>dinleyen</c>, <c>konusan</c>, <c>ben</c>) ya da sabit kimlik.</summary>
        public string Fail { get; }

        public long SonNTick { get; }
    }

    internal sealed class Sablon
    {
        public Sablon(
            string id, string concept, bool genel, IReadOnlyList<Kriter> kriterler, string metin, OnermeDeseni? konuDeseni, string grup,
            TekrarKurali tekrar, SonraTanimi? sonra, HafizaBildirimi? hafiza, int satir)
        {
            Id = id;
            Concept = concept;
            Genel = genel;
            Kriterler = kriterler;
            Metin = metin;
            KonuDeseni = konuDeseni;
            Grup = grup;
            Tekrar = tekrar;
            Sonra = sonra;
            Hafiza = hafiza;
            Satir = satir;
        }

        public string Id { get; }

        public string Concept { get; }

        public bool Genel { get; }

        public IReadOnlyList<Kriter> Kriterler { get; }

        public string Metin { get; }

        public OnermeDeseni? KonuDeseni { get; }

        /// <summary>Tekrar grubu (varsayılan: concept).</summary>
        public string Grup { get; }

        public TekrarKurali Tekrar { get; }

        public SonraTanimi? Sonra { get; }

        public HafizaBildirimi? Hafiza { get; }

        public int Satir { get; }

        /// <summary>Metin <c>#hafiza.zaman#</c> ya da <c>#hafiza.fail#</c> kullanıyor mu.</summary>
        public bool HafizaYuvasiKullaniyor
        {
            get
            {
                foreach (var e in GramerAcilimi.Etiketler(Metin))
                {
                    if (e.Ham.StartsWith("hafiza.", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

namespace Yanki.Dialogue
{
    internal sealed class OnermeVaryanti
    {
        public OnermeVaryanti(IReadOnlyList<OnermeParcasi> olumlu, IReadOnlyList<OnermeParcasi> olumsuz)
        {
            Olumlu = olumlu;
            Olumsuz = olumsuz;
        }

        public IReadOnlyList<OnermeParcasi> Olumlu { get; }

        public IReadOnlyList<OnermeParcasi> Olumsuz { get; }
    }

    /// <summary>
    /// Olay türü → niyet eşlemeleri (DURAK 2 kararı 6: kaynağa göre ayrılır, oyuncu | npc). Önce kaynağı eşleşen kural, yoksa kaynaksız kural kullanılır.
    /// </summary>
    internal sealed class EslemeTablosu
    {
        private readonly List<EslemeKurali> _kurallar = new List<EslemeKurali>();

        public IReadOnlyList<EslemeKurali> Hepsi => _kurallar;

        public void Ekle(EslemeKurali k) => _kurallar.Add(k);

        public EslemeKurali? Bul(string olayTuru, string kaynak)
        {
            EslemeKurali? genel = null;
            foreach (var k in _kurallar)
            {
                if (k.OlayTuru != olayTuru)
                {
                    continue;
                }

                if (k.Kaynak == kaynak)
                {
                    return k;
                }

                if (k.Kaynak.Length == 0)
                {
                    genel = k;
                }
            }

            return genel;
        }
    }
}
