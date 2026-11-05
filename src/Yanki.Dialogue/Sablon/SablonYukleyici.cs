using System;
using System.Collections.Generic;
using Yanki.Core.Criteria;
using Yanki.Core.Json;
using Yanki.Core.Mind;
using Yanki.Core.World;
using Yanki.Dialogue.Gramer;

namespace Yanki.Dialogue
{
    /// <summary>Şablon dosyası yükleme sonucu: ya <see cref="Dosya"/> ya da dosya:satir biçimli, bütün hatalar birlikte listelenmiş <see cref="Hatalar"/>.</summary>
    public sealed class SablonYuklemeSonucu
    {
        internal SablonYuklemeSonucu(SablonDosyasi? dosya, IReadOnlyList<string> hatalar, IReadOnlyList<string>? uyarilar = null)
        {
            Dosya = dosya;
            Hatalar = hatalar;
            Uyarilar = uyarilar ?? new string[0];
        }

        /// <summary>Hata yoksa yüklenen dosya.</summary>
        public SablonDosyasi? Dosya { get; }

        /// <summary><c>dosya:satir: sablon "id": mesaj</c> biçiminde hatalar; boşsa yükleme başarılıdır.</summary>
        public IReadOnlyList<string> Hatalar { get; }

        /// <summary>
        /// <c>dosya:satir: sablon "id": mesaj</c> biçiminde uyarılar (yükleme yine başarılı olabilir). Şu an tek uyarı: anlatma/anlatim_cevabi şablonunda belirli bir önermeyi okuyan
        /// <c>inanc</c>/<c>kaynak</c> kriteri var ama <c>konu_deseni</c> yok (şablon her konuda aday olur, metni yanlış konuya çıkabilir).
        /// </summary>
        public IReadOnlyList<string> Uyarilar { get; }

        /// <summary>Hata yoksa doğru.</summary>
        public bool Basarili => Hatalar.Count == 0 && Dosya != null;
    }

    /// <summary>
    /// Şablon dosyası yükleyici ve doğrulayıcı v0 (8. hafta; <c>yanki validate</c> tam sürümü 9. haftada bu kodu kullanabilir). Eşleme, adlar, <c>onerme_metinleri</c>, gramer ve şablonlar
    /// tek yükleyiciden okunur. Kriterler Core'daki <see cref="KriterOkuyucu"/> ile okunur (ikinci kriter yolu yok). Hatalar dosya:satır gösterir ve hepsi birlikte döner.
    /// </summary>
    public static class SablonYukleyici
    {
        // Şablon bağlamında bağlanabilen olgular (metin etiketleri). `konu` ve `hafiza.*` düz metindir (ek almaz).
        private static readonly string[] AdOlgulari = { "konusan", "dinleyen", "oyuncu", "hafiza.fail" };
        private static readonly string[] MetinOlgulari = { "konu", "hafiza.zaman" };

        // Şablon bağlamında reddedilen sorgular (C kararı + Yusuf'un düzeltmeleri).
        private static readonly string[] Reddedilen = { "olay_rolu", "olay_konusu", "konu_puani", "gercek", "yalan_soyledi" };

        // anlatti niyetine eşlenen şablonlarda ek olarak reddedilenler: doğru ve yalan anlatımı ayırt ettirir (yalan = konuşan buna inanmıyor; gören kişinin hafızası vardır).
        private static readonly string[] AnlattiReddedilen = { "inanc", "kaynak", "hafiza_var" };

        // Eşlenemeyen iç olay türleri: zihin/karar/fren/yay/dünya olayları ve aktarım kaydı (yalan bilgisini taşır).
        private static bool IcOlay(string tur) =>
            Yanki.Core.Mind.ZihinOlaylari.ZihinOlayiMi(tur) || tur == "karar_verildi" || tur == Yanki.Core.Brakes.FrenOlayi.Tur
            || tur == Yanki.Core.Brakes.YayDegiskenleri.OlayTuru || tur == Dunya.GercekEklendi || tur == Dunya.GercekKaldirildi;

        /// <summary>Şablon dosyasının metnini yükler ve doğrular.</summary>
        public static SablonYuklemeSonucu Yukle(string json, string dosyaAdi)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var hatalar = new List<string>();
            var hash = HashHesapla(json);

            var uyarilar = new List<string>();

            string Bicim(int satir, string tur, string kimlik, string mesaj) =>
                $"{dosyaAdi}:{satir}: " + (tur.Length == 0 ? string.Empty : tur + (kimlik.Length == 0 ? string.Empty : " \"" + kimlik + "\"") + ": ") + mesaj;

            void Hata(int satir, string tur, string kimlik, string mesaj) => hatalar.Add(Bicim(satir, tur, kimlik, mesaj));

            void Uyari(int satir, string tur, string kimlik, string mesaj) => uyarilar.Add(Bicim(satir, tur, kimlik, mesaj));

            JsonDegeri kok;
            try
            {
                kok = JsonAyristirici.Ayristir(json);
            }
            catch (JsonHatasi e)
            {
                Hata(e.Satir, string.Empty, string.Empty, "JSON okunamadı: " + e.Message);
                return new SablonYuklemeSonucu(null, hatalar);
            }

            if (kok.Tur != JsonTuru.Nesne)
            {
                Hata(kok.Satir, string.Empty, string.Empty, "şablon dosyası bir JSON nesnesi olmalı");
                return new SablonYuklemeSonucu(null, hatalar);
            }

            foreach (var kv in kok.Ozellikler)
            {
                if (Array.IndexOf(new[] { "dil", "adlar", "onerme_metinleri", "esleme", "gramer", "sablonlar" }, kv.Key) < 0)
                {
                    Hata(kv.Value.Satir, "dosya", string.Empty, $"bilinmeyen alan \"{kv.Key}\" (dil, adlar, onerme_metinleri, esleme, gramer, sablonlar)");
                }
            }

            var dil = DilOku(kok, Hata);
            var adlar = AdlariOku(kok.Al("adlar"), Hata);
            var onermeMetinleri = OnermeMetinleriniOku(kok.Al("onerme_metinleri"), dil, Hata);
            var esleme = EslemeyiOku(kok.Al("esleme"), Hata);
            var gramer = GramerOku(kok.Al("gramer"), Hata, out var gramerSatirlari);
            var sablonlar = SablonlariOku(kok.Al("sablonlar"), Hata, Uyari, dil, gramer, onermeMetinleri, esleme);

            GramerAnalizi(gramer, gramerSatirlari, dil, Hata);
            CaprazDenetimler(sablonlar, esleme, Hata);

            if (hatalar.Count > 0)
            {
                return new SablonYuklemeSonucu(null, hatalar);
            }

            return new SablonYuklemeSonucu(new SablonDosyasi(dosyaAdi, dil, hash, adlar, onermeMetinleri, esleme, gramer, sablonlar), hatalar, uyarilar);
        }

        internal static string HashHesapla(string json) =>
            Hash64.Hex(Hash64.Hesapla(json.Replace("\r\n", "\n").Replace("\r", "\n")));

        private static bool KimlikMi(string s)
        {
            if (string.IsNullOrEmpty(s) || !(Harf(s[0]) || s[0] == '_'))
            {
                return false;
            }

            foreach (var c in s)
            {
                if (!(Harf(c) || (c >= '0' && c <= '9') || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Harf(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        private static string DilOku(JsonDegeri kok, Action<int, string, string, string> hata)
        {
            var d = kok.Al("dil");
            if (d == null)
            {
                hata(kok.Satir, "dosya", string.Empty, "\"dil\" gerekli (dosya başına tek dil: tr ya da en)");
                return "tr";
            }

            if (d.Tur != JsonTuru.Metin || (d.MetinDeger != "tr" && d.MetinDeger != "en"))
            {
                hata(d.Satir, "dosya", string.Empty, "\"dil\" tr ya da en olmalı");
                return "tr";
            }

            return d.MetinDeger;
        }

        private static SortedDictionary<string, AdKaydi> AdlariOku(JsonDegeri? d, Action<int, string, string, string> hata)
        {
            var sonuc = new SortedDictionary<string, AdKaydi>(StringComparer.Ordinal);
            if (d == null)
            {
                return sonuc;
            }

            if (d.Tur != JsonTuru.Nesne)
            {
                hata(d.Satir, "adlar", string.Empty, "bir nesne olmalı (kimlik → görünen ad)");
                return sonuc;
            }

            foreach (var kv in d.Ozellikler)
            {
                var v = kv.Value;
                if (!KimlikMi(kv.Key))
                {
                    hata(v.Satir, "adlar", kv.Key, "kimlik ASCII olmalı (harf, rakam, alt çizgi); Türkçe karakter yalnızca görünen adda");
                    continue;
                }

                if (v.Tur == JsonTuru.Metin)
                {
                    if (v.MetinDeger.Trim().Length == 0)
                    {
                        hata(v.Satir, "adlar", kv.Key, "görünen ad boş");
                        continue;
                    }

                    sonuc[kv.Key] = new AdKaydi(v.MetinDeger, true, false);
                }
                else if (v.Tur == JsonTuru.Nesne)
                {
                    var ad = v.Al("ad");
                    var cins = v.Al("cins");
                    var ince = v.Al("ince");
                    var yumusak = v.Al("yumusak");
                    foreach (var o in v.Ozellikler)
                    {
                        if (o.Key != "ad" && o.Key != "cins" && o.Key != "ince" && o.Key != "yumusak")
                        {
                            hata(o.Value.Satir, "adlar", kv.Key, $"bilinmeyen alan \"{o.Key}\" (ad, cins, ince, yumusak)");
                        }
                    }

                    if (ad == null || ad.Tur != JsonTuru.Metin || ad.MetinDeger.Trim().Length == 0)
                    {
                        hata(v.Satir, "adlar", kv.Key, "\"ad\" gerekli (görünen ad)");
                        continue;
                    }

                    if (cins != null && cins.Tur != JsonTuru.Mantiksal)
                    {
                        hata(cins.Satir, "adlar", kv.Key, "\"cins\" true ya da false olmalı (cins isim: kesme işareti yok)");
                        continue;
                    }

                    if (ince != null && ince.Tur != JsonTuru.Mantiksal)
                    {
                        hata(ince.Satir, "adlar", kv.Key, "\"ince\" true ya da false olmalı (yumuşak l: Kemal gibi adlar ince ünlü eki alır)");
                        continue;
                    }

                    if (yumusak != null && (yumusak.Tur != JsonTuru.Metin || yumusak.MetinDeger.Trim().Length == 0))
                    {
                        hata(yumusak.Satir, "adlar", kv.Key, "\"yumusak\" metin olmalı (yumuşak gövde, ör. borç → borc)");
                        continue;
                    }

                    var cinsIsim = cins != null && cins.MantiksalDeger;
                    if (cinsIsim && TurkceEkler.YumusamaAdayi(ad.MetinDeger.Trim()) && yumusak == null)
                    {
                        hata(v.Satir, "adlar", kv.Key, $"\"{ad.MetinDeger}\" p, ç, t ya da k ile biten bir cins isim: yumuşak gövde zorunlu (\"yumusak\": \"...\", ör. borç → borc; yumuşamayan sözcükte gövde aynen yazılır). Yanlış ek sessizce üretilmez.");
                        continue;
                    }

                    sonuc[kv.Key] = new AdKaydi(ad.MetinDeger, !(cins != null && cins.MantiksalDeger), ince != null && ince.MantiksalDeger, yumusak == null ? null : yumusak.MetinDeger);
                }
                else
                {
                    hata(v.Satir, "adlar", kv.Key, "görünen ad metin ya da { \"ad\", \"cins\" } nesnesi olmalı");
                }
            }

            return sonuc;
        }

        private static SortedDictionary<string, OnermeMetniKaydi> OnermeMetinleriniOku(JsonDegeri? d, string dil, Action<int, string, string, string> hata)
        {
            var sonuc = new SortedDictionary<string, OnermeMetniKaydi>(StringComparer.Ordinal);
            if (d == null)
            {
                return sonuc;
            }

            if (d.Tur != JsonTuru.Nesne)
            {
                hata(d.Satir, "onerme_metinleri", string.Empty, "bir nesne olmalı (yüklem → cümle)");
                return sonuc;
            }

            foreach (var kv in d.Ozellikler)
            {
                var v = kv.Value;
                if (!KimlikMi(kv.Key))
                {
                    hata(v.Satir, "onerme_metni", kv.Key, "yüklem adı ASCII olmalı");
                    continue;
                }

                if (v.Tur != JsonTuru.Nesne)
                {
                    hata(v.Satir, "onerme_metni", kv.Key, "{ \"metin\", \"olumsuz\" } nesnesi olmalı");
                    continue;
                }

                foreach (var o in v.Ozellikler)
                {
                    if (o.Key != "metin" && o.Key != "olumsuz" && o.Key != "sayilar" && o.Key != "ozne" && o.Key != "ben" && o.Key != "sen")
                    {
                        hata(o.Value.Satir, "onerme_metni", kv.Key, $"bilinmeyen alan \"{o.Key}\" (metin, olumsuz, sayilar, ozne, ben, sen)");
                    }
                }

                var sayilar = new List<int>();
                var sayilarDugumu = v.Al("sayilar");
                if (sayilarDugumu != null)
                {
                    if (sayilarDugumu.Tur != JsonTuru.Dizi)
                    {
                        hata(sayilarDugumu.Satir, "onerme_metni", kv.Key, "\"sayilar\" tam sayı dizisi olmalı (sayısal argüman sıraları)");
                    }
                    else
                    {
                        foreach (var e in sayilarDugumu.Elemanlar)
                        {
                            if (e.Tur != JsonTuru.Sayi || e.SayiDeger < 0 || e.SayiDeger != decimal.Truncate(e.SayiDeger))
                            {
                                hata(e.Satir, "onerme_metni", kv.Key, "\"sayilar\" öğeleri 0 ya da büyük tam sayı olmalı");
                            }
                            else
                            {
                                sayilar.Add((int)e.SayiDeger);
                            }
                        }
                    }
                }

                var ozneler = OzneleriOku(v.Al("ozne"), kv.Key, hata);
                var varyantlar = new SortedDictionary<string, OnermeVaryanti>(StringComparer.Ordinal);
                var varyantTamam = true;
                foreach (var kisi in new[] { "ben", "sen" })
                {
                    varyantTamam &= VaryantlariOku(v.Al(kisi), kisi, kv.Key, ozneler, sayilar, dil, hata, varyantlar);
                }

                var okundu = varyantTamam;
                foreach (var p in ozneler)
                {
                    foreach (var kisi in new[] { "ben", "sen" })
                    {
                        if (!varyantlar.ContainsKey(kisi + ":" + p.ToString(System.Globalization.CultureInfo.InvariantCulture)) && okundu)
                        {
                            hata(v.Satir, "onerme_metni", kv.Key, $"ozne {p} için {kisi} varyantı yok: özne konuşan ya da dinleyen olabiliyorsa ben ve sen varyantı zorunlu (eklemli yüklem elle yazılır, motor fiil çekimi yapmaz)");
                        }
                    }
                }

                var olumlu = OnermeParcalari(v.Al("metin"), "metin", kv.Key, sayilar, dil, hata);
                var olumsuz = OnermeParcalari(v.Al("olumsuz"), "olumsuz", kv.Key, sayilar, dil, hata);
                if (olumlu != null && olumsuz != null)
                {
                    sonuc[kv.Key] = new OnermeMetniKaydi(olumlu, olumsuz, sayilar, ozneler, varyantlar);
                }
            }

            return sonuc;
        }

        private static List<int> OzneleriOku(JsonDegeri? d, string yuklem, Action<int, string, string, string> hata)
        {
            var sonuc = new List<int>();
            if (d == null)
            {
                return sonuc;
            }

            if (d.Tur != JsonTuru.Dizi)
            {
                hata(d.Satir, "onerme_metni", yuklem, "\"ozne\" tam sayı dizisi olmalı (özne olabilecek argüman sıraları)");
                return sonuc;
            }

            foreach (var e in d.Elemanlar)
            {
                if (e.Tur != JsonTuru.Sayi || e.SayiDeger < 0 || e.SayiDeger != decimal.Truncate(e.SayiDeger))
                {
                    hata(e.Satir, "onerme_metni", yuklem, "\"ozne\" öğeleri 0 ya da büyük tam sayı olmalı");
                }
                else
                {
                    sonuc.Add((int)e.SayiDeger);
                }
            }

            return sonuc;
        }

        // ben/sen varyantları: { "0": { "metin", "olumsuz" } }; anahtar özne sırası. Eklemli yüklem elle yazılır.
        private static bool VaryantlariOku(
            JsonDegeri? d, string kisi, string yuklem, List<int> ozneler, List<int> sayilar, string dil, Action<int, string, string, string> hata,
            SortedDictionary<string, OnermeVaryanti> sonuc)
        {
            if (d == null)
            {
                return true;
            }

            if (d.Tur != JsonTuru.Nesne)
            {
                hata(d.Satir, "onerme_metni", yuklem, $"\"{kisi}\" bir nesne olmalı (özne sırası → {{ \"metin\", \"olumsuz\" }})");
                return false;
            }

            var tamam = true;
            foreach (var kv in d.Ozellikler)
            {
                var v = kv.Value;
                if (!int.TryParse(kv.Key, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var sira) || !ozneler.Contains(sira))
                {
                    hata(v.Satir, "onerme_metni", yuklem, $"{kisi} varyantı {kv.Key}: ozne listesinde yok");
                    tamam = false;
                    continue;
                }

                if (v.Tur != JsonTuru.Nesne)
                {
                    hata(v.Satir, "onerme_metni", yuklem, $"{kisi} varyantı {kv.Key}: {{ \"metin\", \"olumsuz\" }} nesnesi olmalı");
                    tamam = false;
                    continue;
                }

                foreach (var o in v.Ozellikler)
                {
                    if (o.Key != "metin" && o.Key != "olumsuz")
                    {
                        hata(o.Value.Satir, "onerme_metni", yuklem, $"{kisi} varyantı {kv.Key}: bilinmeyen alan \"{o.Key}\" (metin, olumsuz)");
                        tamam = false;
                    }
                }

                if (v.Al("olumsuz") == null)
                {
                    hata(v.Satir, "onerme_metni", yuklem, $"{kisi} varyantı {kv.Key}: \"olumsuz\" gerekli (degil(...) için ayrı metin zorunludur)");
                    tamam = false;
                    continue;
                }

                var olumlu = OnermeParcalari(v.Al("metin"), "metin", yuklem, sayilar, dil, hata);
                var olumsuz = OnermeParcalari(v.Al("olumsuz"), "olumsuz", yuklem, sayilar, dil, hata);
                if (olumlu == null || olumsuz == null)
                {
                    tamam = false;
                    continue;
                }

                sonuc[kisi + ":" + kv.Key] = new OnermeVaryanti(olumlu, olumsuz);
            }

            return tamam;
        }

        private static IReadOnlyList<OnermeParcasi>? OnermeParcalari(
            JsonDegeri? d, string alan, string yuklem, List<int> sayilar, string dil, Action<int, string, string, string> hata)
        {
            if (d == null)
            {
                hata(0, "onerme_metni", yuklem, $"\"{alan}\" gerekli (degil(...) için ayrı metin zorunludur)");
                return null;
            }

            if (d.Tur != JsonTuru.Metin || d.MetinDeger.Trim().Length == 0)
            {
                hata(d.Satir, "onerme_metni", yuklem, $"\"{alan}\" boş olmayan metin olmalı");
                return null;
            }

            var metin = d.MetinDeger;
            var parcalar = new List<OnermeParcasi>();
            var i = 0;
            var sabit = new System.Text.StringBuilder();
            var tamam = true;
            while (i < metin.Length)
            {
                if (metin[i] != '{')
                {
                    sabit.Append(metin[i]);
                    i++;
                    continue;
                }

                var kapanis = metin.IndexOf('}', i + 1);
                if (kapanis < 0)
                {
                    hata(d.Satir, "onerme_metni", yuklem, $"\"{alan}\": kapanmamış '{{'");
                    return null;
                }

                if (sabit.Length > 0)
                {
                    parcalar.Add(new OnermeParcasi(sabit.ToString(), -1, null));
                    sabit.Clear();
                }

                var ic = metin.Substring(i + 1, kapanis - i - 1);
                var nokta = ic.IndexOf('.');
                var indeksMetni = nokta < 0 ? ic : ic.Substring(0, nokta);
                var ek = nokta < 0 ? null : ic.Substring(nokta + 1);
                if (!int.TryParse(indeksMetni, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var indeks))
                {
                    hata(d.Satir, "onerme_metni", yuklem, $"\"{alan}\": yer tutucu {{{ic}}} geçersiz ({{0}}, {{1}} ya da {{1.yonelme}} gibi olmalı)");
                    tamam = false;
                }
                else
                {
                    if (ek != null && Array.IndexOf(TurkceEkler.Ekler, ek) < 0)
                    {
                        hata(d.Satir, "onerme_metni", yuklem, $"\"{alan}\": bilinmeyen ek \"{ek}\" (yonelme, belirtme, bulunma, ayrilma, iyelik)");
                        tamam = false;
                    }
                    else if (ek != null && dil != "tr")
                    {
                        hata(d.Satir, "onerme_metni", yuklem, $"\"{alan}\": ek değiştiricileri bu hafta yalnızca Türkçe (dil: tr) dosyalarında kullanılır");
                        tamam = false;
                    }
                    else if (ek != null && sayilar.Contains(indeks))
                    {
                        hata(d.Satir, "onerme_metni", yuklem, $"\"{alan}\": {{{ic}}} sayısal argümana ek getirir (500'e gibi); ek okunuşa bağlıdır, bu hafta sayıya ek yok");
                        tamam = false;
                    }

                    parcalar.Add(new OnermeParcasi(string.Empty, indeks, ek));
                }

                i = kapanis + 1;
            }

            if (sabit.Length > 0)
            {
                parcalar.Add(new OnermeParcasi(sabit.ToString(), -1, null));
            }

            return tamam ? parcalar : null;
        }

        private static EslemeTablosu EslemeyiOku(JsonDegeri? d, Action<int, string, string, string> hata)
        {
            var tablo = new EslemeTablosu();
            if (d == null)
            {
                return tablo;
            }

            // İki biçim: nesne (olay türü → { niyet, konusan, kaynak?, dinleyensiz? }) ya da dizi ([{ olay_turu, kaynak?, niyet, konusan, dinleyensiz? }]). Kaynak: oyuncu | npc.
            var girdiler = new List<(string Tur, JsonDegeri Deger)>();
            if (d.Tur == JsonTuru.Nesne)
            {
                foreach (var kv in d.Ozellikler)
                {
                    girdiler.Add((kv.Key, kv.Value));
                }
            }
            else if (d.Tur == JsonTuru.Dizi)
            {
                foreach (var e in d.Elemanlar)
                {
                    var t = e.Tur == JsonTuru.Nesne ? e.Al("olay_turu") : null;
                    if (t == null || t.Tur != JsonTuru.Metin)
                    {
                        hata(e.Satir, "esleme", string.Empty, "dizi elemanı { \"olay_turu\", \"niyet\", \"konusan\" } nesnesi olmalı");
                        continue;
                    }

                    girdiler.Add((t.MetinDeger, e));
                }
            }
            else
            {
                hata(d.Satir, "esleme", string.Empty, "bir nesne ya da dizi olmalı (olay türü → { niyet, konusan, kaynak })");
                return tablo;
            }

            var gorulen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var (tur, v) in girdiler)
            {
                if (!KimlikMi(tur))
                {
                    hata(v.Satir, "esleme", tur, "olay türü ASCII olmalı");
                    continue;
                }

                if (IcOlay(tur))
                {
                    hata(v.Satir, "esleme", tur, "iç olay türü eşlenemez (zihin, karar, fren, yay, dünya olayları ve aktarım kaydı diyalog katmanına kapalıdır; yalan gizliliği)");
                    continue;
                }

                if (v.Tur != JsonTuru.Nesne)
                {
                    hata(v.Satir, "esleme", tur, "{ \"niyet\", \"konusan\" } nesnesi olmalı");
                    continue;
                }

                foreach (var o in v.Ozellikler)
                {
                    if (o.Key != "niyet" && o.Key != "konusan" && o.Key != "kaynak" && o.Key != "dinleyensiz" && o.Key != "olay_turu")
                    {
                        hata(o.Value.Satir, "esleme", tur, $"bilinmeyen alan \"{o.Key}\" (olay_turu, kaynak, niyet, konusan, dinleyensiz); eylem kimliği kullanılmaz, eşleme olay türüyle yapılır");
                    }
                }

                var niyet = v.Al("niyet");
                var konusan = v.Al("konusan");
                var kaynak = v.Al("kaynak");
                var dinleyensiz = v.Al("dinleyensiz");
                if (niyet == null || niyet.Tur != JsonTuru.Metin || !KimlikMi(niyet.MetinDeger))
                {
                    hata(v.Satir, "esleme", tur, "\"niyet\" (concept adı, ASCII) gerekli");
                    continue;
                }

                if (konusan == null || konusan.Tur != JsonTuru.Metin || (konusan.MetinDeger != "fail" && konusan.MetinDeger != "hedef"))
                {
                    hata(v.Satir, "esleme", tur, "\"konusan\" fail ya da hedef olmalı");
                    continue;
                }

                if (kaynak != null && (kaynak.Tur != JsonTuru.Metin || (kaynak.MetinDeger != "oyuncu" && kaynak.MetinDeger != "npc")))
                {
                    hata(kaynak.Satir, "esleme", tur, "\"kaynak\" oyuncu ya da npc olmalı (olayı yapanın kim olduğu)");
                    continue;
                }

                if (dinleyensiz != null && dinleyensiz.Tur != JsonTuru.Mantiksal)
                {
                    hata(dinleyensiz.Satir, "esleme", tur, "\"dinleyensiz\" true ya da false olmalı");
                    continue;
                }

                var kaynakMetni = kaynak == null ? string.Empty : kaynak.MetinDeger;
                if (!gorulen.Add(tur + "|" + kaynakMetni))
                {
                    hata(v.Satir, "esleme", tur, "aynı olay türü ve kaynak için iki kez eşleme var (" + (kaynakMetni.Length == 0 ? "kaynaksız" : "kaynak " + kaynakMetni) + ")");
                    continue;
                }

                tablo.Ekle(new EslemeKurali(tur, kaynakMetni, niyet.MetinDeger, konusan.MetinDeger == "fail", dinleyensiz != null && dinleyensiz.MantiksalDeger, v.Satir));
            }

            return tablo;
        }

        private static GramerTablosu GramerOku(JsonDegeri? d, Action<int, string, string, string> hata, out SortedDictionary<string, int> satirlar)
        {
            var tablo = new GramerTablosu();
            satirlar = new SortedDictionary<string, int>(StringComparer.Ordinal);
            if (d == null)
            {
                return tablo;
            }

            if (d.Tur != JsonTuru.Nesne)
            {
                hata(d.Satir, "gramer", string.Empty, "bir nesne olmalı (kural → seçenek dizisi)");
                return tablo;
            }

            foreach (var kv in d.Ozellikler)
            {
                var v = kv.Value;
                if (!KimlikMi(kv.Key))
                {
                    hata(v.Satir, "gramer", kv.Key, "kural adı ASCII olmalı (nokta ve Türkçe karakter yok)");
                    continue;
                }

                if (Array.IndexOf(AdOlgulari, kv.Key) >= 0 || Array.IndexOf(MetinOlgulari, kv.Key) >= 0)
                {
                    hata(v.Satir, "gramer", kv.Key, "kural adı ayrılmış bir olgu adıyla çakışıyor (konusan, dinleyen, oyuncu, konu)");
                    continue;
                }

                if (v.Tur != JsonTuru.Dizi || v.Elemanlar.Count == 0)
                {
                    hata(v.Satir, "gramer", kv.Key, "boş olmayan bir seçenek dizisi olmalı");
                    continue;
                }

                var secenekler = new List<string>();
                foreach (var e in v.Elemanlar)
                {
                    if (e.Tur != JsonTuru.Metin)
                    {
                        hata(e.Satir, "gramer", kv.Key, "seçenekler metin olmalı");
                    }
                    else
                    {
                        secenekler.Add(e.MetinDeger);
                    }
                }

                tablo.Ekle(kv.Key, secenekler);
                satirlar[kv.Key] = v.Satir;
            }

            return tablo;
        }

        private static List<string> OlguAdlari()
        {
            var l = new List<string>(AdOlgulari);
            l.AddRange(MetinOlgulari);
            return l;
        }

        private static void GramerAnalizi(GramerTablosu gramer, SortedDictionary<string, int> satirlar, string dil, Action<int, string, string, string> hata)
        {
            var olgular = OlguAdlari();
            var gorulen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var kural in gramer.Adlar)
            {
                var satir = satirlar.TryGetValue(kural, out var s) ? s : 0;
                foreach (var mesaj in gramer.Analiz(new[] { kural }, olgular))
                {
                    if (gorulen.Add(mesaj))
                    {
                        hata(satir, "gramer", kural, mesaj);
                    }
                }

                foreach (var secenek in gramer.Secenekler(kural))
                {
                    try
                    {
                        foreach (var e in GramerAcilimi.Etiketler(secenek))
                        {
                            EtiketEkDenetimi(e.Ham, olgular, gramer, dil, (mesaj) => hata(satir, "gramer", kural, mesaj));
                        }
                    }
                    catch (FormatException)
                    {
                        // etiket biçimi hatası Analiz'de raporlandı
                    }
                }
            }
        }

        // Etiketteki olgu ekini denetler: metin olgusuna, kurala ve bilinmeyen eke ek getirilemez; ek yalnızca Türkçe dosyalarda.
        private static void EtiketEkDenetimi(string ham, ICollection<string> olgular, GramerTablosu gramer, string dil, Action<string> hata)
        {
            var c = GramerAcilimi.Cozumle(ham, olgular, gramer);
            if (c.Ekler.Count == 0)
            {
                return;
            }

            var adOlgusu = c.Tur == EtiketTuru.Olgu && Array.IndexOf(MetinOlgulari, c.Ad) < 0;
            var mesaj = GramerAcilimi.EkHatasi(c.Ekler, c.Tur, adOlgusu, dil == "tr");
            if (mesaj != null)
            {
                hata($"#{ham}#: {mesaj}");
            }
        }

        private static List<Sablon> SablonlariOku(
            JsonDegeri? d, Action<int, string, string, string> hata, Action<int, string, string, string> uyari, string dil, GramerTablosu gramer,
            SortedDictionary<string, OnermeMetniKaydi> onermeMetinleri, EslemeTablosu esleme)
        {
            var sonuc = new List<Sablon>();
            if (d == null)
            {
                hata(1, "dosya", string.Empty, "\"sablonlar\" gerekli");
                return sonuc;
            }

            if (d.Tur != JsonTuru.Dizi)
            {
                hata(d.Satir, "sablonlar", string.Empty, "bir dizi olmalı");
                return sonuc;
            }

            // anlatti'ya eşlenen concept'ler (yalan gizliliği kuralları bunlara uygulanır) ve followup hedef concept'leri.
            var anlattiConceptleri = new SortedSet<string>(StringComparer.Ordinal);
            var anlattiTumConceptler = new SortedSet<string>(StringComparer.Ordinal);   // kaynağı ne olursa olsun anlatti'ya eşlenenler (konu_deseni uyarısı)
            foreach (var e in esleme.Hepsi)
            {
                if (e.OlayTuru == "anlatti")
                {
                    anlattiTumConceptler.Add(e.Niyet);
                }

                if (e.OlayTuru == "anlatti" && e.Kaynak != "oyuncu" && e.KonusanFail)
                {
                    anlattiConceptleri.Add(e.Niyet);
                }
            }

            var olgular = OlguAdlari();
            var idler = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var s in d.Elemanlar)
            {
                var sablon = SablonOku(s, hata, uyari, dil, gramer, onermeMetinleri, anlattiConceptleri, anlattiTumConceptler, olgular, idler);
                if (sablon != null)
                {
                    sonuc.Add(sablon);
                }
            }

            return sonuc;
        }

        private static Sablon? SablonOku(
            JsonDegeri s, Action<int, string, string, string> hata, Action<int, string, string, string> uyari, string dil, GramerTablosu gramer,
            SortedDictionary<string, OnermeMetniKaydi> onermeMetinleri, SortedSet<string> anlattiConceptleri, SortedSet<string> anlattiTumConceptler, List<string> olgular, SortedSet<string> idler)
        {
            if (s.Tur != JsonTuru.Nesne)
            {
                hata(s.Satir, "sablon", string.Empty, "şablon bir JSON nesnesi olmalı");
                return null;
            }

            var idDugumu = s.Al("id");
            var id = idDugumu != null && idDugumu.Tur == JsonTuru.Metin ? idDugumu.MetinDeger : string.Empty;
            var tamam = true;

            void H(int satir, string mesaj)
            {
                hata(satir, "sablon", id, mesaj);
                tamam = false;
            }

            foreach (var kv in s.Ozellikler)
            {
                if (Array.IndexOf(new[] { "id", "concept", "genel", "kriterler", "metin", "konu_deseni", "grup", "tekrar", "sonra", "hafiza" }, kv.Key) < 0)
                {
                    H(kv.Value.Satir, $"bilinmeyen alan \"{kv.Key}\" (id, concept, genel, kriterler, metin, konu_deseni, grup, tekrar, sonra, hafiza)");
                }
            }

            if (id.Length == 0 || !KimlikMi(id))
            {
                H(s.Satir, "\"id\" gerekli ve ASCII olmalı");
            }
            else if (!idler.Add(id))
            {
                H(s.Satir, "şablon kimliği tekrar ediyor");
            }

            var conceptDugumu = s.Al("concept");
            var concept = conceptDugumu != null && conceptDugumu.Tur == JsonTuru.Metin ? conceptDugumu.MetinDeger : string.Empty;
            if (!KimlikMi(concept))
            {
                H(s.Satir, "\"concept\" (niyet adı, ASCII) gerekli");
            }

            var genelDugumu = s.Al("genel");
            var genel = false;
            if (genelDugumu != null)
            {
                if (genelDugumu.Tur != JsonTuru.Mantiksal)
                {
                    H(genelDugumu.Satir, "\"genel\" true ya da false olmalı");
                }
                else
                {
                    genel = genelDugumu.MantiksalDeger;
                }
            }

            var metinDugumu = s.Al("metin");
            var metin = metinDugumu != null && metinDugumu.Tur == JsonTuru.Metin ? metinDugumu.MetinDeger : string.Empty;
            if (metin.Trim().Length == 0)
            {
                H(metinDugumu?.Satir ?? s.Satir, "\"metin\" gerekli");
            }

            var anlatti = anlattiConceptleri.Contains(concept);

            // kriterler
            var kriterler = new List<Kriter>();
            var kriterDugumu = s.Al("kriterler");
            if (kriterDugumu != null)
            {
                if (kriterDugumu.Tur != JsonTuru.Dizi)
                {
                    H(kriterDugumu.Satir, "\"kriterler\" bir dizi olmalı");
                }
                else
                {
                    for (var i = 0; i < kriterDugumu.Elemanlar.Count; i++)
                    {
                        var kd = kriterDugumu.Elemanlar[i];
                        try
                        {
                            var kriter = KriterOkuyucu.Oku(kd);
                            KriterBaglamDenetimi(kriter, i + 1, kd.Satir, anlatti, H);
                            kriterler.Add(kriter);
                        }
                        catch (KriterHatasi e)
                        {
                            H(kd.Satir, $"kriter #{i + 1}: {e.Message}");
                        }
                    }
                }
            }

            // genel / genel değil kuralları (C kararı)
            var tekrarDugumu = s.Al("tekrar");
            var tekrar = TekrarOku(tekrarDugumu, H);
            var konuDugumu = s.Al("konu_deseni");
            OnermeDeseni? konuDeseni = null;
            if (konuDugumu != null)
            {
                if (konuDugumu.Tur != JsonTuru.Metin)
                {
                    H(konuDugumu.Satir, "\"konu_deseni\" metin olmalı (borclu(*,*,*) gibi)");
                }
                else
                {
                    try
                    {
                        konuDeseni = OnermeDeseni.Ayristir(konuDugumu.MetinDeger);
                    }
                    catch (ArgumentException e)
                    {
                        H(konuDugumu.Satir, $"\"konu_deseni\" geçersiz: {e.Message}");
                    }
                }
            }

            var hafizaDugumu = s.Al("hafiza");
            var hafiza = HafizaOku(hafizaDugumu, H);
            var sonraDugumu = s.Al("sonra");
            var sonra = SonraOku(sonraDugumu, H);

            var grupDugumu = s.Al("grup");
            var grup = concept;
            if (grupDugumu != null)
            {
                if (grupDugumu.Tur != JsonTuru.Metin || !KimlikMi(grupDugumu.MetinDeger))
                {
                    H(grupDugumu.Satir, "\"grup\" ASCII bir ad olmalı");
                }
                else
                {
                    grup = grupDugumu.MetinDeger;
                }
            }

            if (genel)
            {
                foreach (var k in kriterler)
                {
                    if (k.Zorunlu)
                    {
                        H(s.Satir, "genel şablon zorunlu kriter taşıyamaz (her niyet için her zaman bir genel şablon bulunmalı)");
                        break;
                    }
                }

                if (tekrar.Tur != TekrarTuru.Yok)
                {
                    H(tekrarDugumu?.Satir ?? s.Satir, "genel şablon tekrar sınırı taşıyamaz (satırsız niyet kalmasın)");
                }

                if (hafiza != null)
                {
                    H(hafizaDugumu!.Satir, "genel şablon hafiza göndermesi taşıyamaz (hafıza yoksa şablon aday olamaz)");
                }

                if (konuDeseni != null)
                {
                    H(konuDugumu!.Satir, "genel şablon konu_deseni taşıyamaz (her konu için aday olmalı)");
                }
            }
            else
            {
                var zorunluVar = false;
                foreach (var k in kriterler)
                {
                    zorunluVar |= k.Zorunlu;
                }

                if (!zorunluVar)
                {
                    H(s.Satir, "genel olmayan şablonda en az bir zorunlu kriter olmalı (aksi halde kriteri tutmayan şablon 0 puanla genelle eşitlenip seçilebilir); genel şablon için \"genel\": true yazın");
                }
            }

            // hafıza göndermesi ↔ hafiza_var uyumu
            if (hafiza != null)
            {
                if (anlatti)
                {
                    H(hafizaDugumu!.Satir, "anlatti niyetine eşlenen şablonda hafiza alanı reddedilir: hafıza olayı doğru ve yalan anlatımı ayırır (gören kişinin hafızası vardır, yalancının yoktur)");
                }

                if (!HafizaUyumu(kriterler, hafiza))
                {
                    H(hafizaDugumu!.Satir, $"hafiza bildirimi ile zorunlu hafiza_var({hafiza.Tur}, {hafiza.Fail}, {hafiza.SonNTick}) kriteri uyuşmuyor (aynı tür, fail ve aralık gerekli; gönderme kriteri zorunlu olmalı)");
                }
            }

            // metin etiketleri
            if (metin.Length > 0)
            {
                MetinEtiketleriniDenetle(metin, hafiza != null, gramer, dil, olgular, H, metinDugumu?.Satir ?? s.Satir);
                if (konuDeseni != null && metin.Contains("#konu", StringComparison.Ordinal))
                {
                    var yuklem = YuklemAdi(konuDugumu!.MetinDeger);
                    if (!onermeMetinleri.ContainsKey(yuklem))
                    {
                        H(konuDugumu.Satir, $"tanımsız yüklem \"{yuklem}\": onerme_metinleri içinde cümlesi yok (metin #konu# kullanıyor)");
                    }
                }
            }

            if (!tamam)
            {
                return null;
            }

            // Uyarı: belirli bir önermeyi okuyan inanc/kaynak kriteri konu_deseni olmadan her konuda aday olur (cevap metni yanlış konuya çıkabilir).
            if (konuDeseni == null && anlattiTumConceptler.Contains(concept) && BelirliOnermeOkuyanKriter(kriterler))
            {
                uyari(s.Satir, "sablon", id, "belirli bir önermeyi okuyan inanc/kaynak kriteri var ama konu_deseni yok: şablon her konuda aday olur ve metni yanlış konuya çıkabilir (konu_deseni ekleyin)");
            }

            return new Sablon(id, concept, genel, kriterler, metin, konuDeseni, grup, tekrar, sonra, hafiza, s.Satir);
        }

        private static bool BelirliOnermeOkuyanKriter(List<Kriter> kriterler)
        {
            foreach (var k in kriterler)
            {
                if (k.Alan.Ad != "inanc" && k.Alan.Ad != "kaynak")
                {
                    continue;
                }

                foreach (var a in k.Alan.Argumanlar)
                {
                    if (a is OnermeArgumani)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string YuklemAdi(string desen)
        {
            var d = desen.Trim();
            if (d.StartsWith("degil(", StringComparison.Ordinal) && d.EndsWith(")", StringComparison.Ordinal))
            {
                d = d.Substring(6, d.Length - 7);
            }

            var p = d.IndexOf('(');
            return p < 0 ? d : d.Substring(0, p);
        }

        private static void MetinEtiketleriniDenetle(
            string metin, bool hafizaVar, GramerTablosu gramer, string dil, List<string> olgular, Action<int, string> h, int satir)
        {
            IReadOnlyList<Etiket> etiketler;
            try
            {
                etiketler = GramerAcilimi.Etiketler(metin);
            }
            catch (FormatException e)
            {
                h(satir, "metin: " + e.Message);
                return;
            }

            var kokler = new List<string>();
            foreach (var e in etiketler)
            {
                var c = GramerAcilimi.Cozumle(e.Ham, olgular, gramer);
                if (c.Tur == EtiketTuru.Tanimsiz)
                {
                    h(satir, $"metin: tanımsız sembol \"#{e.Ham}#\" (olgular: {string.Join(", ", olgular)}; ya da gramer kuralı)");
                    continue;
                }

                if (c.Tur == EtiketTuru.Olgu && c.Ad.StartsWith("hafiza.", StringComparison.Ordinal) && !hafizaVar)
                {
                    h(satir, $"metin: #{e.Ham}# kullanıyor ama şablonda hafiza bildirimi yok (yuva bağlanamaz)");
                }

                if (c.Tur == EtiketTuru.Kural)
                {
                    kokler.Add(c.Ad);
                }

                EtiketEkDenetimi(e.Ham, olgular, gramer, dil, (mesaj) => h(satir, "metin: " + mesaj));
            }

            foreach (var mesaj in gramer.Analiz(kokler, olgular))
            {
                h(satir, "metin: " + mesaj);
            }
        }

        private static TekrarKurali TekrarOku(JsonDegeri? d, Action<int, string> h)
        {
            if (d == null)
            {
                return TekrarKurali.Yok;
            }

            if (d.Tur == JsonTuru.Metin)
            {
                if (d.MetinDeger == "tekrarsiz")
                {
                    return new TekrarKurali(TekrarTuru.Tekrarsiz, 0);
                }

                if (d.MetinDeger == "bir_kez")
                {
                    return new TekrarKurali(TekrarTuru.BirKez, 0);
                }
            }
            else if (d.Tur == JsonTuru.Nesne && d.Ozellikler.Count == 1 && d.Ozellikler[0].Key == "tekrar_bekleme")
            {
                var n = d.Ozellikler[0].Value;
                if (n.Tur == JsonTuru.Sayi && n.SayiDeger >= 1 && n.SayiDeger == decimal.Truncate(n.SayiDeger))
                {
                    return new TekrarKurali(TekrarTuru.Bekleme, (long)n.SayiDeger);
                }

                h(n.Satir, "tekrar_bekleme 1 ya da büyük bir tam sayı (tick) olmalı");
                return TekrarKurali.Yok;
            }

            h(d.Satir, "\"tekrar\" \"tekrarsiz\", \"bir_kez\" ya da { \"tekrar_bekleme\": N } olmalı");
            return TekrarKurali.Yok;
        }

        private static HafizaBildirimi? HafizaOku(JsonDegeri? d, Action<int, string> h)
        {
            if (d == null)
            {
                return null;
            }

            if (d.Tur != JsonTuru.Nesne)
            {
                h(d.Satir, "\"hafiza\" { tur, fail, son_n_tick } nesnesi olmalı");
                return null;
            }

            foreach (var o in d.Ozellikler)
            {
                if (o.Key != "tur" && o.Key != "fail" && o.Key != "son_n_tick")
                {
                    h(o.Value.Satir, $"hafiza: bilinmeyen alan \"{o.Key}\" (tur, fail, son_n_tick)");
                }
            }

            var tur = d.Al("tur");
            var fail = d.Al("fail");
            var n = d.Al("son_n_tick");
            if (tur == null || tur.Tur != JsonTuru.Metin || !KimlikMi(tur.MetinDeger))
            {
                h(d.Satir, "hafiza: \"tur\" (olay türü, ASCII) gerekli");
                return null;
            }

            if (fail == null || fail.Tur != JsonTuru.Metin || !(KimlikMi(fail.MetinDeger) || fail.MetinDeger == "ben"))
            {
                h(d.Satir, "hafiza: \"fail\" (oyuncu, dinleyen, konusan, ben ya da sabit kimlik) gerekli");
                return null;
            }

            if (fail.MetinDeger.StartsWith("olay", StringComparison.Ordinal) || fail.MetinDeger.StartsWith("eylem", StringComparison.Ordinal))
            {
                h(fail.Satir, "hafiza: olay.* ve eylem.hedef rolleri şablon bağlamında yoktur");
                return null;
            }

            if (n == null || n.Tur != JsonTuru.Sayi || n.SayiDeger < 1 || n.SayiDeger != decimal.Truncate(n.SayiDeger))
            {
                h(d.Satir, "hafiza: \"son_n_tick\" 1 ya da büyük tam sayı olmalı");
                return null;
            }

            return new HafizaBildirimi(tur.MetinDeger, fail.MetinDeger, (long)n.SayiDeger);
        }

        private static SonraTanimi? SonraOku(JsonDegeri? d, Action<int, string> h)
        {
            if (d == null)
            {
                return null;
            }

            if (d.Tur != JsonTuru.Nesne)
            {
                h(d.Satir, "\"sonra\" { hedef, concept } nesnesi olmalı");
                return null;
            }

            foreach (var o in d.Ozellikler)
            {
                if (o.Key != "hedef" && o.Key != "concept")
                {
                    h(o.Value.Satir, $"sonra: bilinmeyen alan \"{o.Key}\" (hedef, concept)");
                }
            }

            var hedef = d.Al("hedef");
            var concept = d.Al("concept");
            if (hedef == null || hedef.Tur != JsonTuru.Metin || !KimlikMi(hedef.MetinDeger))
            {
                h(d.Satir, "sonra: \"hedef\" (dinleyen ya da sabit NPC kimliği) gerekli");
                return null;
            }

            if (concept == null || concept.Tur != JsonTuru.Metin || !KimlikMi(concept.MetinDeger))
            {
                h(d.Satir, "sonra: \"concept\" (niyet adı, ASCII) gerekli");
                return null;
            }

            return new SonraTanimi(hedef.MetinDeger, concept.MetinDeger);
        }

        private static void KriterBaglamDenetimi(Kriter kriter, int sira, int satir, bool anlatti, Action<int, string> h)
        {
            var roller = new List<Rol>();
            var fonksiyonlar = new List<string>();
            Topla(kriter.Alan, roller, fonksiyonlar);
            foreach (var ad in fonksiyonlar)
            {
                if (Array.IndexOf(Reddedilen, ad) >= 0)
                {
                    h(satir, $"kriter #{sira}: \"{ad}\" şablon bağlamında reddedilir" + (ad == "yalan_soyledi"
                        ? " (aktarım kaydını okur: yalancının sözü dışarıdan ayırt edilebilir olur)"
                        : ad == "gercek" ? " (NPC'nin bilemeyeceği dünya gerçeğini metne sızdırır)" : " (olay/eylem bağlamına ait; şablon konuşanın zihni üzerinden değerlendirilir)"));
                }

                if (anlatti && Array.IndexOf(AnlattiReddedilen, ad) >= 0)
                {
                    h(satir, $"kriter #{sira}: anlatti niyetine eşlenen şablonda \"{ad}\" reddedilir: konuşanın inancı/kaynağı/hafızası doğru ve yalan anlatımı ayırt ettirir (yalan gizliliği)");
                }
            }

            foreach (var rol in roller)
            {
                if (rol != Rol.Ben && rol != Rol.Oyuncu && rol != Rol.Konusan && rol != Rol.Dinleyen)
                {
                    h(satir, $"kriter #{sira}: \"{RolAdlari.Ad(rol)}\" rolü şablon bağlamında yok (geçerli roller: ben, konusan, dinleyen, oyuncu)");
                }
            }
        }

        private static void DinleyensizDenetimi(List<Sablon> sablonlar, EslemeTablosu esleme, Action<int, string, string, string> hata)
        {
            var dinleyensiz = new SortedSet<string>(StringComparer.Ordinal);
            var dinleyenli = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var e in esleme.Hepsi)
            {
                (e.Dinleyensiz ? dinleyensiz : dinleyenli).Add(e.Niyet);
            }

            foreach (var c in dinleyensiz)
            {
                if (dinleyenli.Contains(c))
                {
                    hata(1, "esleme", c, $"niyet \"{c}\" hem dinleyenli hem dinleyensiz olayla eşlenmiş: bir niyet ya dinleyenlidir ya dinleyensiz");
                }
            }

            foreach (var s in sablonlar)
            {
                if (!dinleyensiz.Contains(s.Concept))
                {
                    continue;
                }

                foreach (var k in s.Kriterler)
                {
                    var roller = new List<Rol>();
                    Topla(k.Alan, roller, new List<string>());
                    if (roller.Contains(Rol.Dinleyen))
                    {
                        hata(s.Satir, "sablon", s.Id, "dinleyensiz niyette \"dinleyen\" rolü kullanılamaz (dinleyen yok)");
                        break;
                    }
                }

                if (s.Metin.Contains("#dinleyen", StringComparison.Ordinal))
                {
                    hata(s.Satir, "sablon", s.Id, "dinleyensiz niyette #dinleyen# yuvası kullanılamaz (dinleyen yok)");
                }

                if (s.Sonra != null && s.Sonra.Hedef == "dinleyen")
                {
                    hata(s.Satir, "sonra", s.Id, "dinleyensiz niyette followup hedefi dinleyen olamaz (dinleyen yok)");
                }
            }
        }

        private static void Topla(SorguCagrisi c, List<Rol> roller, List<string> fonksiyonlar)
        {
            fonksiyonlar.Add(c.Ad);
            foreach (var a in c.Argumanlar)
            {
                if (a is RolArgumani r)
                {
                    roller.Add(r.Rol);
                }
                else if (a is OnermeArgumani o)
                {
                    foreach (var ia in o.Cagri.Argumanlar)
                    {
                        if (ia is RolArgumani ir)
                        {
                            roller.Add(ir.Rol);
                        }
                    }
                }
            }
        }

        private static bool HafizaUyumu(List<Kriter> kriterler, HafizaBildirimi hafiza)
        {
            foreach (var k in kriterler)
            {
                if (!k.Zorunlu || k.Alan.Ad != "hafiza_var" || k.Alan.Argumanlar.Count != 3 || k.Op != KriterOperatoru.Esit || !(k.Deger.Tur == DegerTuru.Mantiksal && k.Deger.MantiksalDegeri))
                {
                    continue;
                }

                var tur = (k.Alan.Argumanlar[0] as TanimlayiciArgumani)?.Ad;
                var fail = k.Alan.Argumanlar[1] is RolArgumani rol ? RolAdlari.Ad(rol.Rol) : (k.Alan.Argumanlar[1] as TanimlayiciArgumani)?.Ad;
                var n = k.Alan.Argumanlar[2] is SayiArgumani sayi ? sayi.Deger : -1m;
                if (tur == hafiza.Tur && fail == hafiza.Fail && n == hafiza.SonNTick)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CaprazDenetimler(List<Sablon> sablonlar, EslemeTablosu esleme, Action<int, string, string, string> hata)
        {
            DinleyensizDenetimi(sablonlar, esleme, hata);
            var genelVar = new SortedSet<string>(StringComparer.Ordinal);
            var conceptler = new SortedSet<string>(StringComparer.Ordinal);
            var followupConceptleri = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var s in sablonlar)
            {
                conceptler.Add(s.Concept);
                if (s.Genel)
                {
                    genelVar.Add(s.Concept);
                }

                if (s.Sonra != null)
                {
                    followupConceptleri[s.Sonra.Concept] = s.Satir;
                }
            }

            foreach (var e in esleme.Hepsi)
            {
                if (!genelVar.Contains(e.Niyet))
                {
                    hata(e.Satir, "esleme", e.OlayTuru, $"niyet \"{e.Niyet}\" için genel (\"genel\": true) şablon yok; her concept için tam bir genel şablon zorunlu");
                }
            }

            foreach (var kv in followupConceptleri)
            {
                if (!genelVar.Contains(kv.Key))
                {
                    hata(kv.Value, "sonra", kv.Key, $"followup concept'i \"{kv.Key}\" için genel şablon yok");
                }
            }

            foreach (var s in sablonlar)
            {
                if (s.Sonra != null && followupConceptleri.ContainsKey(s.Concept))
                {
                    hata(s.Satir, "sablon", s.Id, $"\"{s.Concept}\" bir followup hedefidir ve followup'ın \"sonra\" alanı yok sayılmaz: tek seviye (followup zinciri yok)");
                }
            }

            foreach (var c in conceptler)
            {
                if (!genelVar.Contains(c))
                {
                    var ilk = sablonlar.Find(x => x.Concept == c);
                    hata(ilk?.Satir ?? 1, "sablon", ilk?.Id ?? string.Empty, $"concept \"{c}\" için genel şablon yok (her concept için tam bir genel şablon zorunlu)");
                }
            }

            // aynı concept için ikiden fazla genel şablon yasak değildir; "tam bir" = en az bir.
        }
    }
}
