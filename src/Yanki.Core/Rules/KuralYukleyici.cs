using System;
using System.Collections.Generic;
using System.Globalization;
using Yanki.Core.Criteria;
using Yanki.Core.Events;
using Yanki.Core.Json;
using Yanki.Core.Mind;
using Yanki.Core.World;

namespace Yanki.Core.Rules
{
    /// <summary>
    /// Kural dosyasını (JSON metni) okur ve doğrular (doğrulayıcı v0). Çekirdek dosya sistemine dokunmaz; metni ve kaynak adını çağıran verir.
    /// Reddedilenler: şema hatası ve bilinmeyen alan, geçersiz kriter (sözdizimi, fonksiyon, operatör/değer türü), bilinmeyen
    /// olay türü, etkisi boş kural, kural bağlamında çözülemeyen roller (<c>konusan</c>, <c>dinleyen</c>), henüz bağlı olmayan
    /// fonksiyonlar, tekrar eden kimlik. Tüm hatalar toplanır; her biri kural kimliğini ve satırı taşır.
    /// </summary>
    public static class KuralYukleyici
    {
        private static readonly string[] KuralAnahtarlari = { "id", "ne_zaman", "kriterler", "etkiler" };

        // Sözlükte olup çözücüsü henüz bağlı olmayan fonksiyonlar (ZihinSorguCozucusu): çalışma anında patlamasınlar diye baştan reddedilir.
        private static readonly string[] BagliOlmayanFonksiyonlar = { "yalan_soyledi", "yay" };

        /// <summary>
        /// Çekirdeğin bildiği olay türleri: dünya ve zihin olayları. Oyuncu eylem türlerini senaryo tanımlar ve
        /// <see cref="Yukle"/>'ye <c>ekOlayTurleri</c> olarak verilir.
        /// </summary>
        public static IReadOnlyList<string> YerlesikOlayTurleri { get; } = new List<string>
        {
            Dunya.VarlikEklendi, Dunya.GercekEklendi, Dunya.GercekKaldirildi, Dunya.MekanBaglandi,
            ZihinOlaylari.ModifierEklendiTuru, ZihinOlaylari.EksenDegistiTuru, ZihinOlaylari.DuyguDegistiTuru,
            ZihinOlaylari.StresDegistiTuru, ZihinOlaylari.KanitEklendiTuru, ZihinOlaylari.HafizaEklendiTuru,
        }.AsReadOnly();

        /// <summary>Kural bağlamında çözülebilen roller.</summary>
        internal static bool KuralRoluMu(Rol rol) =>
            rol == Rol.Ben || rol == Rol.Oyuncu || rol == Rol.OlayFail || rol == Rol.OlayHedef;

        /// <summary>Kural dosyasını okur ve doğrular.</summary>
        /// <param name="json">Dosyanın metni.</param>
        /// <param name="kaynakAdi">Hata mesajlarında görünen dosya adı.</param>
        /// <param name="ekOlayTurleri">Senaryonun tanımladığı oyuncu eylemi türleri; <c>ne_zaman</c> için geçerlidir.</param>
        public static KuralYuklemeSonucu Yukle(string json, string kaynakAdi, IEnumerable<string>? ekOlayTurleri = null)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var hatalar = new List<KuralHatasi>();
            var bilinen = new SortedSet<string>(YerlesikOlayTurleri, StringComparer.Ordinal);
            if (ekOlayTurleri != null)
            {
                foreach (var t in ekOlayTurleri)
                {
                    bilinen.Add(t);
                }
            }

            JsonDegeri kok;
            try
            {
                kok = JsonAyristirici.Ayristir(json);
            }
            catch (JsonHatasi e)
            {
                hatalar.Add(new KuralHatasi(kaynakAdi, e.Satir, string.Empty, "JSON okunamadı: " + e.Message));
                return new KuralYuklemeSonucu(new KuralTanimi[0], hatalar);
            }

            var dizi = kok.Tur == JsonTuru.Nesne ? kok.Al("kurallar") : null;
            if (dizi == null)
            {
                hatalar.Add(new KuralHatasi(kaynakAdi, kok.Satir, string.Empty, "Kök nesnede \"kurallar\" dizisi gerekli"));
                return new KuralYuklemeSonucu(new KuralTanimi[0], hatalar);
            }

            if (dizi.Tur != JsonTuru.Dizi)
            {
                hatalar.Add(new KuralHatasi(kaynakAdi, dizi.Satir, string.Empty, "\"kurallar\" bir dizi olmalı"));
                return new KuralYuklemeSonucu(new KuralTanimi[0], hatalar);
            }

            foreach (var kv in kok.Ozellikler)
            {
                if (kv.Key != "kurallar")
                {
                    hatalar.Add(new KuralHatasi(kaynakAdi, kv.Value.Satir, string.Empty, $"Kök nesnede bilinmeyen alan \"{kv.Key}\" (yalnızca \"kurallar\")"));
                }
            }

            var kurallar = new List<KuralTanimi>();
            var kimlikler = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var oge in dizi.Elemanlar)
            {
                var kural = KuralOku(oge, kaynakAdi, bilinen, hatalar);
                if (kural == null)
                {
                    continue;
                }

                if (!kimlikler.Add(kural.Id))
                {
                    hatalar.Add(new KuralHatasi(kaynakAdi, oge.Satir, kural.Id, $"Kural kimliği tekrar ediyor: \"{kural.Id}\""));
                    continue;
                }

                kurallar.Add(kural);
            }

            if (hatalar.Count > 0)
            {
                return new KuralYuklemeSonucu(new KuralTanimi[0], hatalar);
            }

            kurallar.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return new KuralYuklemeSonucu(kurallar, hatalar);
        }

        private static KuralTanimi? KuralOku(JsonDegeri oge, string kaynak, SortedSet<string> bilinen, List<KuralHatasi> hatalar)
        {
            if (oge.Tur != JsonTuru.Nesne)
            {
                hatalar.Add(new KuralHatasi(kaynak, oge.Satir, string.Empty, "Kural bir JSON nesnesi olmalı"));
                return null;
            }

            var once = hatalar.Count;
            var kimlik = string.Empty;
            var idDugumu = oge.Al("id");
            if (idDugumu == null || idDugumu.Tur != JsonTuru.Metin)
            {
                hatalar.Add(new KuralHatasi(kaynak, oge.Satir, string.Empty, "Kuralda metin olarak \"id\" alanı gerekli"));
            }
            else if (!Kimlik.GecerliKimlikMi(idDugumu.MetinDeger))
            {
                hatalar.Add(new KuralHatasi(kaynak, idDugumu.Satir, idDugumu.MetinDeger, $"\"id\" geçersiz kimlik: \"{idDugumu.MetinDeger}\" (ASCII harf/rakam/_, harf ya da _ ile başlar)"));
                kimlik = idDugumu.MetinDeger;
            }
            else
            {
                kimlik = idDugumu.MetinDeger;
            }

            void Hata(int satir, string mesaj) => hatalar.Add(new KuralHatasi(kaynak, satir, kimlik, mesaj));

            foreach (var kv in oge.Ozellikler)
            {
                if (Array.IndexOf(KuralAnahtarlari, kv.Key) < 0)
                {
                    Hata(kv.Value.Satir, $"Kuralda bilinmeyen alan \"{kv.Key}\" (id, ne_zaman, kriterler, etkiler)");
                }
            }

            var neZaman = string.Empty;
            var nzDugumu = oge.Al("ne_zaman");
            if (nzDugumu == null || nzDugumu.Tur != JsonTuru.Metin)
            {
                Hata(oge.Satir, "Kuralda metin olarak \"ne_zaman\" alanı gerekli");
            }
            else if (!bilinen.Contains(nzDugumu.MetinDeger))
            {
                Hata(nzDugumu.Satir, $"Bilinmeyen olay türü: \"{nzDugumu.MetinDeger}\" (çekirdek olayları ya da senaryonun tanımladığı eylem türleri)");
            }
            else
            {
                neZaman = nzDugumu.MetinDeger;
            }

            var roller = new SortedSet<Rol>();

            var kriterler = new List<Kriter>();
            var kriterDugumu = oge.Al("kriterler");
            if (kriterDugumu != null)
            {
                if (kriterDugumu.Tur != JsonTuru.Dizi)
                {
                    Hata(kriterDugumu.Satir, "\"kriterler\" bir dizi olmalı");
                }
                else
                {
                    for (var i = 0; i < kriterDugumu.Elemanlar.Count; i++)
                    {
                        var kd = kriterDugumu.Elemanlar[i];
                        var kriter = KriterOku(kd, i + 1, Hata);
                        if (kriter == null)
                        {
                            continue;
                        }

                        KriterRolleriniDenetle(kriter, kd.Satir, i + 1, roller, Hata);
                        kriterler.Add(kriter);
                    }
                }
            }

            var etkiler = new List<KuralEtkisi>();
            var etkiDugumu = oge.Al("etkiler");
            if (etkiDugumu == null)
            {
                Hata(oge.Satir, "Kuralın etkisi yok: \"etkiler\" alanı gerekli (etkisi boş kural reddedilir)");
            }
            else if (etkiDugumu.Tur != JsonTuru.Dizi)
            {
                Hata(etkiDugumu.Satir, "\"etkiler\" bir dizi olmalı");
            }
            else if (etkiDugumu.Elemanlar.Count == 0)
            {
                Hata(etkiDugumu.Satir, "Kuralın etkisi yok: \"etkiler\" boş (etkisi boş kural reddedilir)");
            }
            else
            {
                for (var i = 0; i < etkiDugumu.Elemanlar.Count; i++)
                {
                    var etki = EtkiOku(etkiDugumu.Elemanlar[i], i + 1, roller, Hata);
                    if (etki != null)
                    {
                        etkiler.Add(etki);
                    }
                }
            }

            if (hatalar.Count > once || etkiler.Count == 0)
            {
                return null;
            }

            return new KuralTanimi(kimlik, neZaman, kriterler, etkiler, roller, oge.Satir);
        }

        private static Kriter? KriterOku(JsonDegeri kd, int sira, Action<int, string> hata)
        {
            try
            {
                return KriterOkuyucu.Oku(kd);
            }
            catch (KriterHatasi e)
            {
                hata(kd.Satir, $"kriter #{sira}: {e.Message}");
                return null;
            }
        }

        private static void KriterRolleriniDenetle(Kriter kriter, int satir, int sira, SortedSet<Rol> roller, Action<int, string> hata)
        {
            foreach (var bagli in BagliOlmayanFonksiyonlar)
            {
                if (string.Equals(kriter.Alan.Ad, bagli, StringComparison.Ordinal))
                {
                    hata(satir, $"kriter #{sira}: '{bagli}' fonksiyonu henüz bağlı değil (yalan_soyledi 6. hafta, yay 7. hafta)");
                }
            }

            var bulunan = new List<Rol>();
            RolleriTopla(kriter.Alan, bulunan);
            foreach (var rol in bulunan)
            {
                if (KuralRoluMu(rol))
                {
                    roller.Add(rol);
                }
                else
                {
                    hata(satir, $"kriter #{sira}: '{RolAdlari.Ad(rol)}' rolü sonuç kurallarında tanımsız (ben, oyuncu, olay.fail, olay.hedef)");
                }
            }
        }

        private static void RolleriTopla(SorguCagrisi cagri, List<Rol> roller)
        {
            foreach (var a in cagri.Argumanlar)
            {
                if (a is RolArgumani r)
                {
                    roller.Add(r.Rol);
                }
                else if (a is OnermeArgumani o)
                {
                    RolleriTopla(o.Cagri, roller);
                }
            }
        }

        private static KuralEtkisi? EtkiOku(JsonDegeri d, int sira, SortedSet<Rol> roller, Action<int, string> hata)
        {
            var onek = $"Etki #{sira}: ";
            if (d.Tur != JsonTuru.Nesne)
            {
                hata(d.Satir, onek + "etki bir JSON nesnesi olmalı");
                return null;
            }

            var turDugumu = d.Al("tur");
            if (turDugumu == null || turDugumu.Tur != JsonTuru.Metin)
            {
                hata(d.Satir, onek + "metin olarak \"tur\" alanı gerekli (modifier, eksen, duygu, hafiza, gercek)");
                return null;
            }

            string[] izinli;
            switch (turDugumu.MetinDeger)
            {
                case "modifier": izinli = new[] { "tur", "hedef", "modifier", "deger", "sure" }; break;
                case "eksen": izinli = new[] { "tur", "hedef", "eksen", "delta" }; break;
                case "duygu": izinli = new[] { "tur", "duygu", "siddet", "hedef" }; break;
                case "hafiza": izinli = new[] { "tur", "onem" }; break;
                case "gercek": izinli = new[] { "tur", "islem", "gercek" }; break;
                default:
                    hata(turDugumu.Satir, onek + $"bilinmeyen etki türü \"{turDugumu.MetinDeger}\" (modifier, eksen, duygu, hafiza, gercek)");
                    return null;
            }

            var tamam = true;
            foreach (var kv in d.Ozellikler)
            {
                if (Array.IndexOf(izinli, kv.Key) < 0)
                {
                    hata(kv.Value.Satir, onek + $"\"{turDugumu.MetinDeger}\" etkisinde bilinmeyen alan \"{kv.Key}\" ({string.Join(", ", izinli)})");
                    tamam = false;
                }
            }

            if (!tamam)
            {
                return null;
            }

            switch (turDugumu.MetinDeger)
            {
                case "modifier":
                {
                    var hedef = Hedef(d, "hedef", true, onek, roller, hata);
                    var ad = Metin(d, "modifier", onek, hata);
                    var deger = Sayi(d, "deger", onek, hata);
                    var sure = Sayi(d, "sure", onek, hata);
                    if (ad != null && !Kimlik.GecerliKimlikMi(ad))
                    {
                        hata(d.Al("modifier")!.Satir, onek + $"modifier adı geçersiz kimlik: \"{ad}\"");
                        return null;
                    }

                    if (sure.HasValue && (sure.Value <= 0m || sure.Value != decimal.Truncate(sure.Value)))
                    {
                        hata(d.Al("sure")!.Satir, onek + "\"sure\" pozitif tam sayı (tick) olmalı");
                        return null;
                    }

                    return hedef == null || ad == null || !deger.HasValue || !sure.HasValue
                        ? null
                        : KuralEtkisi.Modifier(hedef, ad, deger.Value, (long)sure.Value, d.Satir);
                }

                case "eksen":
                {
                    var hedef = Hedef(d, "hedef", true, onek, roller, hata);
                    var ad = Metin(d, "eksen", onek, hata);
                    var delta = Sayi(d, "delta", onek, hata);
                    if (ad != null && ad != Eksenler.Guven && ad != Eksenler.Sevgi && ad != Eksenler.Korku)
                    {
                        hata(d.Al("eksen")!.Satir, onek + $"bilinmeyen eksen \"{ad}\" (guven, sevgi, korku)");
                        return null;
                    }

                    return hedef == null || ad == null || !delta.HasValue ? null : KuralEtkisi.Eksen(hedef, ad, delta.Value, d.Satir);
                }

                case "duygu":
                {
                    var ad = Metin(d, "duygu", onek, hata);
                    var siddet = Sayi(d, "siddet", onek, hata);
                    var hedef = Hedef(d, "hedef", false, onek, roller, hata);
                    if (ad != null && !Kimlik.GecerliKimlikMi(ad))
                    {
                        hata(d.Al("duygu")!.Satir, onek + $"duygu türü geçersiz kimlik: \"{ad}\"");
                        return null;
                    }

                    if (siddet.HasValue && (siddet.Value < 0m || siddet.Value > 1m))
                    {
                        hata(d.Al("siddet")!.Satir, onek + "\"siddet\" 0..1 aralığında olmalı");
                        return null;
                    }

                    return hedef == null || ad == null || !siddet.HasValue ? null : KuralEtkisi.Duygu(ad, siddet.Value, hedef, d.Satir);
                }

                case "hafiza":
                {
                    var onem = Sayi(d, "onem", onek, hata);
                    if (onem.HasValue && (onem.Value < 0m || onem.Value > 1m))
                    {
                        hata(d.Al("onem")!.Satir, onek + "\"onem\" 0..1 aralığında olmalı");
                        return null;
                    }

                    return onem.HasValue ? KuralEtkisi.Hafiza(onem.Value, d.Satir) : null;
                }

                default:
                {
                    var islem = Metin(d, "islem", onek, hata);
                    var gercek = Metin(d, "gercek", onek, hata);
                    if (islem != null && islem != "ekle" && islem != "kaldir")
                    {
                        hata(d.Al("islem")!.Satir, onek + $"bilinmeyen islem \"{islem}\" (ekle, kaldir)");
                        return null;
                    }

                    if (gercek == null || islem == null)
                    {
                        return null;
                    }

                    var sablonRolleri = new List<Rol>();
                    var hataMesaji = GercekSablonu.Denetle(gercek, sablonRolleri);
                    if (hataMesaji != null)
                    {
                        hata(d.Al("gercek")!.Satir, onek + hataMesaji);
                        return null;
                    }

                    foreach (var r in sablonRolleri)
                    {
                        roller.Add(r);
                    }

                    return KuralEtkisi.Gercek(gercek, islem == "kaldir", d.Satir);
                }
            }
        }

        // Rol adı ya da varlık kimliği; zorunluysa eksikliği hatadır. Boş (isteğe bağlı) hedef için "" döner.
        private static string? Hedef(JsonDegeri d, string anahtar, bool zorunlu, string onek, SortedSet<Rol> roller, Action<int, string> hata)
        {
            var dugum = d.Al(anahtar);
            if (dugum == null)
            {
                if (zorunlu)
                {
                    hata(d.Satir, onek + $"\"{anahtar}\" alanı gerekli");
                    return null;
                }

                return string.Empty;
            }

            if (dugum.Tur != JsonTuru.Metin)
            {
                hata(dugum.Satir, onek + $"\"{anahtar}\" metin olmalı");
                return null;
            }

            var deger = dugum.MetinDeger;
            if (RolAdlari.TryCoz(deger, out var rol))
            {
                if (!KuralRoluMu(rol))
                {
                    hata(dugum.Satir, onek + $"'{deger}' rolü sonuç kurallarında tanımsız (ben, oyuncu, olay.fail, olay.hedef)");
                    return null;
                }

                roller.Add(rol);
                return deger;
            }

            if (!Kimlik.GecerliKimlikMi(deger))
            {
                hata(dugum.Satir, onek + $"\"{anahtar}\" bir rol ya da geçerli varlık kimliği değil: \"{deger}\"");
                return null;
            }

            return deger;
        }

        private static string? Metin(JsonDegeri d, string anahtar, string onek, Action<int, string> hata)
        {
            var dugum = d.Al(anahtar);
            if (dugum == null || dugum.Tur != JsonTuru.Metin)
            {
                hata(dugum?.Satir ?? d.Satir, onek + $"metin olarak \"{anahtar}\" alanı gerekli");
                return null;
            }

            return dugum.MetinDeger;
        }

        private static decimal? Sayi(JsonDegeri d, string anahtar, string onek, Action<int, string> hata)
        {
            var dugum = d.Al(anahtar);
            if (dugum == null || dugum.Tur != JsonTuru.Sayi)
            {
                hata(dugum?.Satir ?? d.Satir, onek + $"sayı olarak \"{anahtar}\" alanı gerekli");
                return null;
            }

            return dugum.SayiDeger;
        }
    }

    /// <summary>
    /// Gerçek etkisinin metin şablonu: <c>{ben}</c>, <c>{oyuncu}</c>, <c>{olay.fail}</c>, <c>{olay.hedef}</c> yer tutucuları
    /// çalışma anında varlık kimliğiyle doldurulur; sonuç <see cref="Gercek.Ayristir"/> ile ayrıştırılır.
    /// </summary>
    internal static class GercekSablonu
    {
        /// <summary>Şablonu denetler; geçerliyse <c>null</c> döner ve kullanılan rolleri <paramref name="roller"/>'e ekler.</summary>
        public static string? Denetle(string sablon, List<Rol> roller)
        {
            string doldurulmus;
            try
            {
                doldurulmus = Doldur(sablon, rol =>
                {
                    roller.Add(rol);
                    return "x_" + ((int)rol).ToString(CultureInfo.InvariantCulture);
                });
            }
            catch (FormatException e)
            {
                return e.Message;
            }

            try
            {
                Gercek.Ayristir(doldurulmus);
            }
            catch (ArgumentException e)
            {
                return $"\"gercek\" geçersiz: {e.Message}";
            }

            return null;
        }

        /// <summary>Yer tutucuları verilen çözücüyle doldurur.</summary>
        /// <exception cref="FormatException">Yer tutucu bilinmiyorsa ya da kapanmıyorsa.</exception>
        public static string Doldur(string sablon, Func<Rol, string> coz)
        {
            var sb = new System.Text.StringBuilder();
            var i = 0;
            while (i < sablon.Length)
            {
                var c = sablon[i];
                if (c != '{')
                {
                    sb.Append(c);
                    i++;
                    continue;
                }

                var son = sablon.IndexOf('}', i);
                if (son < 0)
                {
                    throw new FormatException($"\"gercek\" şablonunda '{{' kapanmıyor: \"{sablon}\"");
                }

                var ad = sablon.Substring(i + 1, son - i - 1);
                if (!RolAdlari.TryCoz(ad, out var rol) || !KuralYukleyici.KuralRoluMu(rol))
                {
                    throw new FormatException($"\"gercek\" şablonunda bilinmeyen yer tutucu {{{ad}}} (ben, oyuncu, olay.fail, olay.hedef)");
                }

                sb.Append(coz(rol));
                i = son + 1;
            }

            return sb.ToString();
        }
    }
}
