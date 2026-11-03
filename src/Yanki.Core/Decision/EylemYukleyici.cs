using System;
using System.Collections.Generic;
using Yanki.Core.Criteria;
using Yanki.Core.Json;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.World;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Eylem dosyasını (JSON metni) okur ve doğrular (ADR-011). Çekirdek dosya sistemine dokunmaz; metni ve kaynak adını çağıran verir.
    /// Kriterler, alan ifadeleri ve etkiler mevcut kriter/kural okuyucularıyla okunur (ikinci kriter ya da etki yolu yoktur).
    /// Tüm hatalar toplanır; her biri <c>dosya:satir: eylem "id": mesaj</c> biçimindedir.
    /// <para>
    /// Eylem bağlamında reddedilenler: <c>olay_rolu</c>, <c>olay.fail</c>, <c>olay.hedef</c>, <c>konusan</c>, <c>dinleyen</c>; NPC bilgisini
    /// aşan <c>gercek(...)</c>; henüz bağlı olmayan <c>yay</c> ve <c>yalan_soyledi</c>; hedefsiz eylemde <c>eylem.hedef</c>;
    /// <c>komsu_mekan</c> eyleminde kişi bekleyen sorgu ve etkiler.
    /// </para>
    /// </summary>
    public static class EylemYukleyici
    {
        /// <summary>Taban için izin verilen en büyük değer.</summary>
        public const decimal EnBuyukTaban = 1000m;

        private static readonly string[] EylemAnahtarlari =
        {
            "id", "olay_turu", "hedef", "taban", "on_kosullar", "olcutler", "kisilik_carpani", "fikir_carpani",
            "ruh_hali_carpani", "celisen_ozellikler", "etkiler", "aktarim", "konu_desenleri", "yalan_onermeleri", "gorunurluk", "stres_tabani", "cooldown", "kapsam", "en_fazla", "ihlal_bayraklari",
        };

        private static readonly string[] OlcutAnahtarlari = { "alan", "min", "max", "egri" };

        internal static readonly RolKumesi HedefsizRoller = new RolKumesi(
            r => r == Rol.Ben || r == Rol.Oyuncu, "hedefsiz eylemlerde", "ben, oyuncu");

        internal static readonly RolKumesi HedefliRoller = new RolKumesi(
            r => r == Rol.Ben || r == Rol.Oyuncu || r == Rol.EylemHedef, "eylemlerde", "ben, oyuncu, eylem.hedef");

        /// <summary>Eylem dosyasını okur ve doğrular.</summary>
        /// <param name="json">Dosyanın metni.</param>
        /// <param name="kaynakAdi">Hata mesajlarında görünen dosya adı.</param>
        public static EylemYuklemeSonucu Yukle(string json, string kaynakAdi)
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
                hatalar.Add(new KuralHatasi(kaynakAdi, e.Satir, string.Empty, "JSON okunamadı: " + e.Message, "eylem"));
                return new EylemYuklemeSonucu(new EylemTanimi[0], hatalar);
            }

            var dizi = kok.Tur == JsonTuru.Nesne ? kok.Al("eylemler") : null;
            if (dizi == null)
            {
                hatalar.Add(new KuralHatasi(kaynakAdi, kok.Satir, string.Empty, "Kök nesnede \"eylemler\" dizisi gerekli", "eylem"));
                return new EylemYuklemeSonucu(new EylemTanimi[0], hatalar);
            }

            if (dizi.Tur != JsonTuru.Dizi)
            {
                hatalar.Add(new KuralHatasi(kaynakAdi, dizi.Satir, string.Empty, "\"eylemler\" bir dizi olmalı", "eylem"));
                return new EylemYuklemeSonucu(new EylemTanimi[0], hatalar);
            }

            foreach (var kv in kok.Ozellikler)
            {
                if (kv.Key != "eylemler")
                {
                    hatalar.Add(new KuralHatasi(kaynakAdi, kv.Value.Satir, string.Empty, $"Kök nesnede bilinmeyen alan \"{kv.Key}\" (yalnızca \"eylemler\")", "eylem"));
                }
            }

            var eylemler = new List<EylemTanimi>();
            var kimlikler = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var oge in dizi.Elemanlar)
            {
                var eylem = EylemOku(oge, kaynakAdi, hatalar, uyarilar);
                if (eylem == null)
                {
                    continue;
                }

                if (!kimlikler.Add(eylem.Id))
                {
                    hatalar.Add(new KuralHatasi(kaynakAdi, oge.Satir, eylem.Id, $"Eylem kimliği tekrar ediyor: \"{eylem.Id}\"", "eylem"));
                    continue;
                }

                eylemler.Add(eylem);
            }

            if (hatalar.Count > 0)
            {
                return new EylemYuklemeSonucu(new EylemTanimi[0], hatalar, uyarilar);
            }

            eylemler.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return new EylemYuklemeSonucu(eylemler, hatalar, uyarilar);
        }

        private static EylemTanimi? EylemOku(JsonDegeri oge, string kaynak, List<KuralHatasi> hatalar, List<KuralHatasi> uyarilar)
        {
            if (oge.Tur != JsonTuru.Nesne)
            {
                hatalar.Add(new KuralHatasi(kaynak, oge.Satir, string.Empty, "Eylem bir JSON nesnesi olmalı", "eylem"));
                return null;
            }

            var once = hatalar.Count;
            var kimlik = string.Empty;
            var idDugumu = oge.Al("id");
            if (idDugumu == null || idDugumu.Tur != JsonTuru.Metin)
            {
                hatalar.Add(new KuralHatasi(kaynak, idDugumu?.Satir ?? oge.Satir, string.Empty, "Eylemde metin olarak \"id\" alanı gerekli", "eylem"));
            }
            else
            {
                kimlik = idDugumu.MetinDeger;
                if (!Kimlik.GecerliKimlikMi(kimlik))
                {
                    hatalar.Add(new KuralHatasi(kaynak, idDugumu.Satir, kimlik, $"\"id\" geçersiz kimlik: \"{kimlik}\" (ASCII harf/rakam/_, harf ya da _ ile başlar)", "eylem"));
                }
            }

            void Hata(int satir, string mesaj) => hatalar.Add(new KuralHatasi(kaynak, satir, kimlik, mesaj, "eylem"));

            foreach (var kv in oge.Ozellikler)
            {
                if (Array.IndexOf(EylemAnahtarlari, kv.Key) < 0)
                {
                    Hata(kv.Value.Satir, $"Eylemde bilinmeyen alan \"{kv.Key}\" ({string.Join(", ", EylemAnahtarlari)})");
                }
            }

            // hedef türü (rol kümesini belirler)
            var hedef = EylemHedefTuru.Yok;
            var hedefGecerli = false;
            var hedefDugumu = oge.Al("hedef");
            if (hedefDugumu == null || hedefDugumu.Tur != JsonTuru.Metin)
            {
                Hata(hedefDugumu?.Satir ?? oge.Satir, $"Eylemde metin olarak \"hedef\" alanı gerekli ({EylemHedefTurleri.GecerliAdlar})");
            }
            else if (!EylemHedefTurleri.TryCoz(hedefDugumu.MetinDeger, out hedef))
            {
                Hata(hedefDugumu.Satir, $"\"hedef\" bilinmeyen tür \"{hedefDugumu.MetinDeger}\" ({EylemHedefTurleri.GecerliAdlar})");
            }
            else
            {
                hedefGecerli = true;
            }

            // aktarım (dedikodu) türü
            var aktarim = AktarimTuru.Yok;
            var aktarimDugumu = oge.Al("aktarim");
            if (aktarimDugumu != null)
            {
                if (aktarimDugumu.Tur != JsonTuru.Metin || !AktarimTurleri.TryCoz(aktarimDugumu.MetinDeger, out aktarim))
                {
                    Hata(aktarimDugumu.Satir, $"\"aktarim\" geçersiz: dogru ya da yalan olmalı ({AktarimTurleri.GecerliAdlar})");
                    aktarim = AktarimTuru.Yok;
                    aktarimDugumu = null;
                }
                else if (hedefGecerli && hedef != EylemHedefTuru.Npc && hedef != EylemHedefTuru.Oyuncu)
                {
                    Hata(aktarimDugumu.Satir, "aktarim eyleminin \"hedef\" alanı npc ya da oyuncu olmalı (aynı mekandaki dinleyen)");
                }
            }

            var konusma = aktarimDugumu != null;
            var hareket = hedefGecerli && hedef == EylemHedefTuru.KomsuMekan;
            var kume = !hedefGecerli ? RolKumesi.Hepsi : hedef == EylemHedefTuru.Yok ? HedefsizRoller : HedefliRoller;

            // taban
            var taban = 1m;
            var tabanDugumu = oge.Al("taban");
            if (tabanDugumu == null || tabanDugumu.Tur != JsonTuru.Sayi)
            {
                Hata(tabanDugumu?.Satir ?? oge.Satir, "Eylemde sayı olarak \"taban\" alanı gerekli");
            }
            else if (tabanDugumu.SayiDeger <= 0m || tabanDugumu.SayiDeger > EnBuyukTaban)
            {
                Hata(tabanDugumu.Satir, $"\"taban\" 0'dan büyük ve en çok {EnBuyukTaban.ToString(System.Globalization.CultureInfo.InvariantCulture)} olmalı");
            }
            else
            {
                taban = tabanDugumu.SayiDeger;
            }

            // olay türü
            var olayTuru = string.Empty;
            var olayDugumu = oge.Al("olay_turu");
            if (hareket)
            {
                if (olayDugumu != null)
                {
                    Hata(olayDugumu.Satir, "komsu_mekan (hareket) eyleminde \"olay_turu\" yazılmaz: çıktısı yerleşik ayrildi ve geldi olaylarıdır");
                }
            }
            else if (olayDugumu == null || olayDugumu.Tur != JsonTuru.Metin)
            {
                Hata(olayDugumu?.Satir ?? oge.Satir, "Eylemde metin olarak \"olay_turu\" alanı gerekli (komsu_mekan hareket eylemi hariç)");
            }
            else if (konusma)
            {
                if (olayDugumu.MetinDeger != YerlesikTurler.Anlatti)
                {
                    Hata(
                        olayDugumu.Satir,
                        $"aktarim eyleminin \"olay_turu\" değeri \"anlatti\" olmak zorundadır (\"{olayDugumu.MetinDeger}\" olursa dışarıdan yalan ile doğru ayırt edilir)");
                }
                else
                {
                    olayTuru = olayDugumu.MetinDeger;
                }
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

            var roller = new SortedSet<Rol>();

            // ön koşullar
            var kriterler = new List<Kriter>();
            var kosulDugumu = oge.Al("on_kosullar");
            if (kosulDugumu != null)
            {
                if (kosulDugumu.Tur != JsonTuru.Dizi)
                {
                    Hata(kosulDugumu.Satir, "\"on_kosullar\" bir dizi olmalı");
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

                        var onek = $"kriter #{i + 1}: ";
                        KuralYukleyici.KriterRolleriniDenetle(kriter, kd.Satir, i + 1, roller, Hata, kume);
                        AlanBaglamDenetle(kriter.Alan, hedef, hedefGecerli, kd.Satir, onek, Hata, aktarim);

                        if (kd.Al("agirlik") != null)
                        {
                            uyarilar.Add(new KuralHatasi(
                                kaynak, kd.Al("agirlik")!.Satir, kimlik,
                                $"kriter #{i + 1}: eylem ön koşulları yalnızca tüm kriterleri tutunca geçer; \"agirlik\" sonucu etkilemez (ADR-010), kaldırılabilir", "eylem"));
                        }

                        var zorunlu = kd.Al("zorunlu");
                        if (zorunlu != null && zorunlu.Tur == JsonTuru.Mantiksal && !zorunlu.MantiksalDeger)
                        {
                            uyarilar.Add(new KuralHatasi(
                                kaynak, zorunlu.Satir, kimlik,
                                $"kriter #{i + 1}: \"zorunlu\": false anlamsız, tüm kriterler zaten zorunludur (ADR-010); alan kaldırılabilir", "eylem"));
                        }

                        kriterler.Add(kriter);
                    }
                }
            }

            // ölçütler
            var olcutler = new List<Olcut>();
            var olcutDugumu = oge.Al("olcutler");
            if (olcutDugumu != null)
            {
                if (olcutDugumu.Tur != JsonTuru.Dizi)
                {
                    Hata(olcutDugumu.Satir, "\"olcutler\" bir dizi olmalı");
                }
                else
                {
                    for (var i = 0; i < olcutDugumu.Elemanlar.Count; i++)
                    {
                        var olcut = OlcutOku(olcutDugumu.Elemanlar[i], i + 1, roller, kume, hedef, hedefGecerli, Hata, aktarim);
                        if (olcut != null)
                        {
                            olcutler.Add(olcut);
                        }
                    }
                }
            }

            // çarpanlar
            KisilikCarpaniTanimi? kisilikCarpani = null;
            var kd2 = oge.Al("kisilik_carpani");
            if (kd2 != null)
            {
                kisilikCarpani = KisilikCarpaniOku(kd2, Hata);
            }

            FikirCarpaniTanimi? fikirCarpani = null;
            var fd = oge.Al("fikir_carpani");
            if (fd != null)
            {
                if (hedefGecerli && hedef != EylemHedefTuru.Npc && hedef != EylemHedefTuru.Oyuncu)
                {
                    Hata(fd.Satir, $"\"fikir_carpani\" yalnızca npc ya da oyuncu hedefli eylemde tanımlanabilir (bu eylem: {EylemHedefTurleri.Ad(hedef)})");
                }
                else
                {
                    fikirCarpani = FikirCarpaniOku(fd, Hata);
                }
            }

            RuhHaliCarpaniTanimi? ruhHali = null;
            var rd = oge.Al("ruh_hali_carpani");
            if (rd != null)
            {
                ruhHali = RuhHaliCarpaniOku(rd, Hata);
            }

            // çelişen özellikler
            var celisen = new List<string>();
            var cd = oge.Al("celisen_ozellikler");
            if (cd != null)
            {
                if (cd.Tur != JsonTuru.Dizi)
                {
                    Hata(cd.Satir, "\"celisen_ozellikler\" bir dizi olmalı");
                }
                else
                {
                    var gorulen = new SortedSet<string>(StringComparer.Ordinal);
                    foreach (var o in cd.Elemanlar)
                    {
                        if (o.Tur != JsonTuru.Metin)
                        {
                            Hata(o.Satir, "\"celisen_ozellikler\" öğesi metin olmalı");
                        }
                        else if (!KisilikOzelligiMi(o.MetinDeger))
                        {
                            Hata(o.Satir, OzellikHatasi(o.MetinDeger));
                        }
                        else if (!gorulen.Add(o.MetinDeger))
                        {
                            Hata(o.Satir, $"\"celisen_ozellikler\" içinde özellik tekrar ediyor: \"{o.MetinDeger}\"");
                        }
                        else
                        {
                            celisen.Add(o.MetinDeger);
                        }
                    }
                }
            }

            var konuDesenleri = new List<OnermeDeseni>();
            var yalanOnermeleri = new List<string>();
            var gorunurluk = string.Empty;
            AktarimAlanlariniOku(oge, aktarim, konusma, konuDesenleri, yalanOnermeleri, ref gorunurluk, Hata, uyarilar, kaynak, kimlik);

            // stres tabanı
            decimal? stresTabani = null;
            var stresDugumu = oge.Al("stres_tabani");
            if (stresDugumu != null)
            {
                if (stresDugumu.Tur != JsonTuru.Sayi || stresDugumu.SayiDeger <= 0m || stresDugumu.SayiDeger > 100m)
                {
                    Hata(stresDugumu.Satir, "\"stres_tabani\" 0'dan büyük, en çok 100 olan bir sayı olmalı");
                }
                else
                {
                    stresTabani = stresDugumu.SayiDeger;
                    if (celisen.Count == 0)
                    {
                        uyarilar.Add(new KuralHatasi(
                            kaynak, stresDugumu.Satir, kimlik, "\"stres_tabani\" yazılı ama \"celisen_ozellikler\" yok: stres hiç artmaz", "eylem"));
                    }
                }
            }

            if (celisen.Count > 0 && stresDugumu == null)
            {
                Hata(cd!.Satir, "\"celisen_ozellikler\" var ama \"stres_tabani\" yok: çelişen eylemin stres maliyeti yazılmalı");
            }

            // etkiler
            var etkiler = new List<KuralEtkisi>();
            var etkiDugumu = oge.Al("etkiler");
            if (etkiDugumu == null)
            {
                if (!hareket && !konusma)
                {
                    Hata(oge.Satir, "Eylemin etkisi yok: \"etkiler\" alanı gerekli (etkisi boş eylem reddedilir; yalnızca komsu_mekan hareketi etkisiz olabilir)");
                }
            }
            else if (etkiDugumu.Tur != JsonTuru.Dizi)
            {
                Hata(etkiDugumu.Satir, "\"etkiler\" bir dizi olmalı");
            }
            else if (etkiDugumu.Elemanlar.Count == 0)
            {
                if (!hareket && !konusma)
                {
                    Hata(etkiDugumu.Satir, "Eylemin etkisi yok: \"etkiler\" boş (etkisi boş eylem reddedilir; yalnızca komsu_mekan hareketi etkisiz olabilir)");
                }
            }
            else
            {
                for (var i = 0; i < etkiDugumu.Elemanlar.Count; i++)
                {
                    var etki = KuralYukleyici.EtkiOku(etkiDugumu.Elemanlar[i], i + 1, roller, Hata, kume);
                    if (etki == null)
                    {
                        continue;
                    }

                    if (hareket && etki.Hedef == "eylem.hedef"
                        && (etki.Tur == EtkiTuru.Modifier || etki.Tur == EtkiTuru.Eksen || etki.Tur == EtkiTuru.Duygu))
                    {
                        Hata(etkiDugumu.Elemanlar[i].Satir, $"Etki #{i + 1}: 'eylem.hedef' komsu_mekan eyleminde bir mekandır; {EtkiAdi(etki.Tur)} etkisinin hedefi kişi bekler");
                        continue;
                    }

                    etkiler.Add(etki);
                }
            }

            if (hatalar.Count > once || kimlik.Length == 0)
            {
                return null;
            }

            var ihlaller = Brakes.IhlalBayragi.OkuDizi(
                oge, "ihlal_bayraklari", hedef == EylemHedefTuru.Yok ? new[] { "ben" } : new[] { "ben", "eylem.hedef" }, Hata);
            foreach (var ib in ihlaller)
            {
                if (ib.Rolu.HasValue)
                {
                    roller.Add(ib.Rolu.Value);
                }
            }

            if (hatalar.Count > once)
            {
                return null;
            }

            return new EylemTanimi(kimlik, olayTuru, hedef, taban, kriterler, olcutler, kisilikCarpani, fikirCarpani, ruhHali, celisen, etkiler, oge.Satir, aktarim, konuDesenleri, yalanOnermeleri, gorunurluk, stresTabani, Brakes.TetikSiniri.Oku(oge, true, Hata), ihlaller);
        }

        // aktarim, konu_desenleri, yalan_onermeleri, gorunurluk alanları ve aktarımsız eylemde yasakları.
        private static void AktarimAlanlariniOku(
            JsonDegeri oge, AktarimTuru aktarim, bool konusma, List<OnermeDeseni> desenler, List<string> yalanlar, ref string gorunurluk,
            Action<int, string> hata, List<KuralHatasi> uyarilar, string kaynak, string kimlik)
        {
            var konuDugumu = oge.Al("konu_desenleri");
            var yalanDugumu = oge.Al("yalan_onermeleri");
            var gorunurDugumu = oge.Al("gorunurluk");

            if (!konusma)
            {
                foreach (var (ad, d) in new[] { ("konu_desenleri", konuDugumu), ("yalan_onermeleri", yalanDugumu), ("gorunurluk", gorunurDugumu) })
                {
                    if (d != null)
                    {
                        hata(d.Satir, $"\"{ad}\" yalnızca \"aktarim\" alanı olan eylemde yazılır");
                    }
                }

                return;
            }

            if (gorunurDugumu != null)
            {
                if (gorunurDugumu.Tur != JsonTuru.Metin || (gorunurDugumu.MetinDeger != "sessiz" && gorunurDugumu.MetinDeger != "acik"))
                {
                    hata(gorunurDugumu.Satir, "\"gorunurluk\" sessiz ya da acik olmalı (konuşmada gizli anlamsızdır; varsayılan sessiz)");
                }
                else
                {
                    gorunurluk = gorunurDugumu.MetinDeger;
                }
            }

            if (aktarim == AktarimTuru.Dogru)
            {
                if (yalanDugumu != null)
                {
                    hata(yalanDugumu.Satir, "\"yalan_onermeleri\" yalnızca aktarim: yalan eyleminde yazılır");
                }

                if (konuDugumu == null)
                {
                    hata(oge.Satir, "aktarim: dogru eyleminde \"konu_desenleri\" zorunlu (anlatılabilecek önerme desenleri, örn. \"borclu(*,*,*)\")");
                    return;
                }

                if (konuDugumu.Tur != JsonTuru.Dizi || konuDugumu.Elemanlar.Count == 0)
                {
                    hata(konuDugumu.Satir, "\"konu_desenleri\" boş olmayan bir dizi olmalı");
                    return;
                }

                foreach (var e in konuDugumu.Elemanlar)
                {
                    if (e.Tur != JsonTuru.Metin)
                    {
                        hata(e.Satir, "\"konu_desenleri\" öğesi metin olmalı");
                        continue;
                    }

                    var metin = e.MetinDeger.Trim();
                    var bos = metin.StartsWith("degil(", StringComparison.Ordinal) && metin.EndsWith(")", StringComparison.Ordinal)
                        ? metin.Substring(6, metin.Length - 7).Trim() : metin;
                    if (bos.StartsWith("*", StringComparison.Ordinal))
                    {
                        hata(e.Satir, $"\"konu_desenleri\" joker yüklem kabul etmez (yüklemi açık yaz): \"{e.MetinDeger}\"");
                        continue;
                    }

                    try
                    {
                        var desen = OnermeDeseni.Ayristir(metin);
                        desenler.Add(desen);
                        if (bos.StartsWith("anlatti(", StringComparison.Ordinal))
                        {
                            uyarilar.Add(new KuralHatasi(
                                kaynak, e.Satir, kimlik, "\"konu_desenleri\" içindeki anlatti önermeleri hiçbir zaman konu olamaz (desen etkisiz)", "eylem"));
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        hata(e.Satir, $"\"konu_desenleri\" geçersiz desen: {ex.Message}");
                    }
                }

                return;
            }

            // yalan
            if (konuDugumu != null)
            {
                hata(konuDugumu.Satir, "\"konu_desenleri\" yalnızca aktarim: dogru eyleminde yazılır (yalan \"yalan_onermeleri\" kullanır)");
            }

            if (yalanDugumu == null)
            {
                hata(oge.Satir, "aktarim: yalan eyleminde \"yalan_onermeleri\" zorunlu (örn. \"degil(borclu(Mehmet,{eylem.hedef},500))\")");
                return;
            }

            if (yalanDugumu.Tur != JsonTuru.Dizi || yalanDugumu.Elemanlar.Count == 0)
            {
                hata(yalanDugumu.Satir, "\"yalan_onermeleri\" boş olmayan bir dizi olmalı");
                return;
            }

            foreach (var e in yalanDugumu.Elemanlar)
            {
                if (e.Tur != JsonTuru.Metin || e.MetinDeger.Trim().Length == 0)
                {
                    hata(e.Satir, "\"yalan_onermeleri\" öğesi boş olmayan bir metin olmalı");
                    continue;
                }

                // Şablonlar örnek kimliklerle açılıp önerme olarak ayrıştırılır; tanınmayan { } şablonu reddedilir.
                var ornek = e.MetinDeger.Replace("{ben}", "Ornek1").Replace("{eylem.hedef}", "Ornek2");
                if (ornek.IndexOf('{') >= 0 || ornek.IndexOf('}') >= 0)
                {
                    hata(e.Satir, $"\"yalan_onermeleri\" bilinmeyen şablon (yalnızca {{ben}} ve {{eylem.hedef}}): \"{e.MetinDeger}\"");
                    continue;
                }

                try
                {
                    Yanki.Core.Mind.Onerme.Ayristir(ornek);
                    yalanlar.Add(e.MetinDeger.Trim());
                }
                catch (ArgumentException ex)
                {
                    hata(e.Satir, $"\"yalan_onermeleri\" geçersiz önerme \"{e.MetinDeger}\": {ex.Message}");
                }
            }
        }

        // ---- kriter/ölçüt alanı: eylem bağlamı denetimleri ----

        internal static void AlanBaglamDenetle(
            SorguCagrisi alan, EylemHedefTuru hedef, bool hedefGecerli, int satir, string onek, Action<int, string> hata, AktarimTuru aktarim)
        {
            if (alan.Ad == "olay_konusu")
            {
                hata(satir, onek + "'olay_konusu' sonuç kuralı bağlamıdır (anlatılan olay); eylemlerde tanımsız");
            }

            if (alan.Ad == "konu_puani" && aktarim != AktarimTuru.Dogru)
            {
                hata(satir, onek + "'konu_puani' yalnızca aktarim: dogru eylemlerinde geçerlidir (konu havuzu eyleme özel desenlerden gelir)");
            }

            if (alan.Ad == "olay_rolu")
            {
                hata(satir, onek + "'olay_rolu' sonuç kuralı bağlamıdır (olay.fail/hedef/tanık); eylemlerde tanımsız");
            }

            if (alan.Ad == "gercek")
            {
                hata(satir, onek + "'gercek' nesnel dünya sorgusudur, NPC'nin bilgisini aşar; eylemlerde kullanılamaz (NPC'nin inancı için 'inanc(...)' kullan)");
            }

            if (hedefGecerli && hedef == EylemHedefTuru.KomsuMekan)
            {
                foreach (var arg in alan.Argumanlar)
                {
                    if (arg is RolArgumani r && r.Rol == Rol.EylemHedef)
                    {
                        hata(satir, onek + $"'{alan.Ad}' kişi bekler; komsu_mekan hedefi bir mekandır ('eylem.hedef' yalnızca önerme içinde kullanılabilir)");
                    }
                }
            }
        }

        private static string EtkiAdi(EtkiTuru tur) =>
            tur == EtkiTuru.Modifier ? "modifier" : tur == EtkiTuru.Eksen ? "eksen" : "duygu";

        // ---- ölçüt ----

        private static Olcut? OlcutOku(
            JsonDegeri d, int sira, SortedSet<Rol> roller, RolKumesi kume, EylemHedefTuru hedef, bool hedefGecerli, Action<int, string> hata,
            AktarimTuru aktarim)
        {
            var onek = $"ölçüt #{sira}: ";
            if (d.Tur != JsonTuru.Nesne)
            {
                hata(d.Satir, onek + "ölçüt bir JSON nesnesi olmalı");
                return null;
            }

            var tamam = true;
            foreach (var kv in d.Ozellikler)
            {
                if (Array.IndexOf(OlcutAnahtarlari, kv.Key) < 0)
                {
                    hata(kv.Value.Satir, onek + $"bilinmeyen alan \"{kv.Key}\" ({string.Join(", ", OlcutAnahtarlari)})");
                    tamam = false;
                }
            }

            SorguCagrisi? alan = null;
            var alanDugumu = d.Al("alan");
            if (alanDugumu == null || alanDugumu.Tur != JsonTuru.Metin)
            {
                hata(alanDugumu?.Satir ?? d.Satir, onek + "metin olarak \"alan\" alanı gerekli");
                tamam = false;
            }
            else
            {
                try
                {
                    alan = KriterOkuyucu.AlanOku(alanDugumu.MetinDeger);
                }
                catch (KriterHatasi e)
                {
                    hata(alanDugumu.Satir, onek + e.Message);
                    tamam = false;
                }
            }

            if (alan != null)
            {
                var once = tamam;
                var sayac = 0;
                void Say(int satir, string mesaj)
                {
                    sayac++;
                    hata(satir, mesaj);
                }

                KuralYukleyici.AlanRolleriniDenetle(alan, alanDugumu!.Satir, onek, roller, Say, kume);
                AlanBaglamDenetle(alan, hedef, hedefGecerli, alanDugumu.Satir, onek, Say, aktarim);
                var donus = SorguSozlugu.Bul(alan.Ad)!.Donus;
                if (donus != DonusTuru.Sayi && donus != DonusTuru.Mantiksal)
                {
                    Say(alanDugumu.Satir, onek + $"ölçüt sayısal ya da doğru/yanlış sonuç vermeli; '{alan.Ad}' {(donus == DonusTuru.Bant ? "bant" : "varlık kimliği")} döndürür");
                }

                if (sayac > 0)
                {
                    tamam = false;
                }

                _ = once;
            }

            var min = 0m;
            var max = 1m;
            var minDugumu = d.Al("min");
            var maxDugumu = d.Al("max");
            if (minDugumu != null)
            {
                if (minDugumu.Tur != JsonTuru.Sayi)
                {
                    hata(minDugumu.Satir, onek + "\"min\" sayı olmalı");
                    tamam = false;
                }
                else
                {
                    min = minDugumu.SayiDeger;
                }
            }

            if (maxDugumu != null)
            {
                if (maxDugumu.Tur != JsonTuru.Sayi)
                {
                    hata(maxDugumu.Satir, onek + "\"max\" sayı olmalı");
                    tamam = false;
                }
                else
                {
                    max = maxDugumu.SayiDeger;
                }
            }

            if (tamam && max <= min)
            {
                hata(d.Satir, onek + "min < max olmalı");
                tamam = false;
            }

            var egri = EgriTanimi.Dogrusal();
            var egriDugumu = d.Al("egri");
            if (egriDugumu != null)
            {
                var e = EgriOku(egriDugumu, onek, hata);
                if (e == null)
                {
                    tamam = false;
                }
                else
                {
                    egri = e;
                }
            }

            return tamam && alan != null ? new Olcut(alan, min, max, egri, d.Satir) : null;
        }

        private static EgriTanimi? EgriOku(JsonDegeri d, string onek, Action<int, string> hata)
        {
            if (d.Tur != JsonTuru.Nesne)
            {
                hata(d.Satir, onek + "\"egri\" bir JSON nesnesi olmalı");
                return null;
            }

            var turDugumu = d.Al("tur");
            if (turDugumu == null || turDugumu.Tur != JsonTuru.Metin)
            {
                hata(turDugumu?.Satir ?? d.Satir, onek + "\"egri\" için metin olarak \"tur\" alanı gerekli (dogrusal, ustel, lojistik, basamak)");
                return null;
            }

            if (!EgriTurleri.TryCoz(turDugumu.MetinDeger, out var tur))
            {
                hata(turDugumu.Satir, onek + $"bilinmeyen eğri \"{turDugumu.MetinDeger}\" (dogrusal, ustel, lojistik, basamak)");
                return null;
            }

            string[] izinli;
            switch (tur)
            {
                case EgriTuru.Ustel: izinli = new[] { "tur", "k" }; break;
                case EgriTuru.Lojistik: izinli = new[] { "tur", "k", "m" }; break;
                case EgriTuru.Basamak: izinli = new[] { "tur", "esik" }; break;
                default: izinli = new[] { "tur" }; break;
            }

            var tamam = true;
            foreach (var kv in d.Ozellikler)
            {
                if (Array.IndexOf(izinli, kv.Key) < 0)
                {
                    hata(kv.Value.Satir, onek + $"\"{turDugumu.MetinDeger}\" eğrisinde bilinmeyen alan \"{kv.Key}\" ({string.Join(", ", izinli)})");
                    tamam = false;
                }
            }

            if (!tamam)
            {
                return null;
            }

            switch (tur)
            {
                case EgriTuru.Ustel:
                {
                    var k = d.Al("k");
                    if (k == null || k.Tur != JsonTuru.Sayi || k.SayiDeger != decimal.Truncate(k.SayiDeger)
                        || k.SayiDeger < EgriTanimi.UstelEnKucukK || k.SayiDeger > EgriTanimi.UstelEnBuyukK)
                    {
                        hata(k?.Satir ?? d.Satir, onek + "ustel eğride \"k\" 1..8 aralığında tam sayı olmalı");
                        return null;
                    }

                    return EgriTanimi.Ustel((int)k.SayiDeger);
                }

                case EgriTuru.Lojistik:
                {
                    var k = d.Al("k");
                    var m = d.Al("m");
                    var ok = true;
                    if (k == null || k.Tur != JsonTuru.Sayi || k.SayiDeger <= 0m || k.SayiDeger > EgriTanimi.LojistikEnBuyukK)
                    {
                        hata(k?.Satir ?? d.Satir, onek + "lojistik eğride \"k\" 0'dan büyük ve en çok 100 olan bir sayı olmalı");
                        ok = false;
                    }

                    if (m == null)
                    {
                        hata(d.Satir, onek + "lojistik eğri için \"m\" alanı gerekli (0..1 orta nokta)");
                        ok = false;
                    }
                    else if (m.Tur != JsonTuru.Sayi || m.SayiDeger < 0m || m.SayiDeger > 1m)
                    {
                        hata(m.Satir, onek + "lojistik eğride \"m\" 0..1 aralığında bir sayı olmalı");
                        ok = false;
                    }

                    return ok ? EgriTanimi.Lojistik(k!.SayiDeger, m!.SayiDeger) : null;
                }

                case EgriTuru.Basamak:
                {
                    var e = d.Al("esik");
                    if (e == null || e.Tur != JsonTuru.Sayi || e.SayiDeger < 0m || e.SayiDeger > 1m)
                    {
                        hata(e?.Satir ?? d.Satir, onek + "basamak eğrisinde \"esik\" 0..1 aralığında bir sayı olmalı");
                        return null;
                    }

                    return EgriTanimi.Basamak(e.SayiDeger);
                }

                default:
                    return EgriTanimi.Dogrusal();
            }
        }

        // ---- çarpan tanımları ----

        private static bool KisilikOzelligiMi(string ad)
        {
            foreach (var o in Kisilik.Ozellikler)
            {
                if (string.Equals(o, ad, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string OzellikHatasi(string ad) =>
            $"bilinmeyen kişilik özelliği \"{ad}\" ({string.Join(", ", Kisilik.Ozellikler)})";

        private static KisilikCarpaniTanimi? KisilikCarpaniOku(JsonDegeri d, Action<int, string> hata)
        {
            const string onek = "kisilik_carpani: ";
            var ogeler = AgirlikListesi(d, "ozellikler", "ozellik", onek, hata, out var tamam);
            if (ogeler == null)
            {
                return null;
            }

            var sonuc = new List<OzellikAgirligi>();
            var gorulen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var o in ogeler)
            {
                if (!KisilikOzelligiMi(o.Ad))
                {
                    hata(o.Satir, onek + OzellikHatasi(o.Ad));
                    tamam = false;
                }
                else if (!gorulen.Add(o.Ad))
                {
                    hata(o.Satir, onek + $"özellik tekrar ediyor: \"{o.Ad}\"");
                    tamam = false;
                }
                else
                {
                    sonuc.Add(new OzellikAgirligi(o.Ad, o.Agirlik, o.Ters));
                }
            }

            return tamam ? new KisilikCarpaniTanimi(sonuc) : null;
        }

        private static RuhHaliCarpaniTanimi? RuhHaliCarpaniOku(JsonDegeri d, Action<int, string> hata)
        {
            const string onek = "ruh_hali_carpani: ";
            var ogeler = AgirlikListesi(d, "duygular", "duygu", onek, hata, out var tamam);
            if (ogeler == null)
            {
                return null;
            }

            var sonuc = new List<DuyguAgirligi>();
            var gorulen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var o in ogeler)
            {
                if (!Kimlik.GecerliKimlikMi(o.Ad))
                {
                    hata(o.Satir, onek + $"\"duygu\" geçersiz kimlik: \"{o.Ad}\"");
                    tamam = false;
                }
                else if (!gorulen.Add(o.Ad))
                {
                    hata(o.Satir, onek + $"duygu tekrar ediyor: \"{o.Ad}\"");
                    tamam = false;
                }
                else
                {
                    sonuc.Add(new DuyguAgirligi(o.Ad, o.Agirlik, o.Ters));
                }
            }

            return tamam ? new RuhHaliCarpaniTanimi(sonuc) : null;
        }

        private static FikirCarpaniTanimi? FikirCarpaniOku(JsonDegeri d, Action<int, string> hata)
        {
            const string onek = "fikir_carpani: ";
            if (d.Tur != JsonTuru.Nesne)
            {
                hata(d.Satir, onek + "bir JSON nesnesi olmalı");
                return null;
            }

            var tamam = true;
            foreach (var kv in d.Ozellikler)
            {
                if (kv.Key != "yon")
                {
                    hata(kv.Value.Satir, onek + $"bilinmeyen alan \"{kv.Key}\" (yon)");
                    tamam = false;
                }
            }

            var yon = d.Al("yon");
            if (yon == null || yon.Tur != JsonTuru.Metin)
            {
                hata(yon?.Satir ?? d.Satir, onek + "metin olarak \"yon\" alanı gerekli (artan, azalan)");
                return null;
            }

            if (!FikirYonleri.TryCoz(yon.MetinDeger, out var yonDegeri))
            {
                hata(yon.Satir, onek + $"\"yon\" bilinmeyen: \"{yon.MetinDeger}\" (artan, azalan)");
                return null;
            }

            return tamam ? new FikirCarpaniTanimi(yonDegeri) : null;
        }

        private sealed class AgirlikOgesi
        {
            public AgirlikOgesi(string ad, decimal agirlik, bool ters, int satir)
            {
                Ad = ad;
                Agirlik = agirlik;
                Ters = ters;
                Satir = satir;
            }

            public string Ad { get; }

            public decimal Agirlik { get; }

            public bool Ters { get; }

            public int Satir { get; }
        }

        // { "<liste>": [ { "<adAnahtari>": "...", "agirlik": n, "ters": b } ] } biçimini okur (kişilik ve ruh hali çarpanı ortak).
        private static List<AgirlikOgesi>? AgirlikListesi(JsonDegeri d, string listeAnahtari, string adAnahtari, string onek, Action<int, string> hata, out bool tamam)
        {
            tamam = true;
            if (d.Tur != JsonTuru.Nesne)
            {
                hata(d.Satir, onek + "bir JSON nesnesi olmalı");
                return null;
            }

            foreach (var kv in d.Ozellikler)
            {
                if (kv.Key != listeAnahtari)
                {
                    hata(kv.Value.Satir, onek + $"bilinmeyen alan \"{kv.Key}\" ({listeAnahtari})");
                    tamam = false;
                }
            }

            var liste = d.Al(listeAnahtari);
            if (liste == null || liste.Tur != JsonTuru.Dizi)
            {
                hata(liste?.Satir ?? d.Satir, onek + $"\"{listeAnahtari}\" dizisi gerekli");
                return null;
            }

            if (liste.Elemanlar.Count == 0)
            {
                hata(liste.Satir, onek + $"\"{listeAnahtari}\" en az bir öğe içermeli (çarpan istenmiyorsa tanımı hiç yazma)");
                return null;
            }

            var sonuc = new List<AgirlikOgesi>();
            foreach (var e in liste.Elemanlar)
            {
                if (e.Tur != JsonTuru.Nesne)
                {
                    hata(e.Satir, onek + "öğe bir JSON nesnesi olmalı");
                    tamam = false;
                    continue;
                }

                var izinli = new[] { adAnahtari, "agirlik", "ters" };
                var ogeTamam = true;
                foreach (var kv in e.Ozellikler)
                {
                    if (Array.IndexOf(izinli, kv.Key) < 0)
                    {
                        hata(kv.Value.Satir, onek + $"öğede bilinmeyen alan \"{kv.Key}\" ({string.Join(", ", izinli)})");
                        ogeTamam = false;
                    }
                }

                var ad = e.Al(adAnahtari);
                if (ad == null || ad.Tur != JsonTuru.Metin)
                {
                    hata(ad?.Satir ?? e.Satir, onek + $"öğede metin olarak \"{adAnahtari}\" alanı gerekli");
                    tamam = false;
                    continue;
                }

                var agirlik = 1m;
                var agirlikDugumu = e.Al("agirlik");
                if (agirlikDugumu != null)
                {
                    if (agirlikDugumu.Tur != JsonTuru.Sayi || agirlikDugumu.SayiDeger <= 0m)
                    {
                        hata(agirlikDugumu.Satir, onek + "\"agirlik\" 0'dan büyük bir sayı olmalı");
                        ogeTamam = false;
                    }
                    else
                    {
                        agirlik = agirlikDugumu.SayiDeger;
                    }
                }

                var ters = false;
                var tersDugumu = e.Al("ters");
                if (tersDugumu != null)
                {
                    if (tersDugumu.Tur != JsonTuru.Mantiksal)
                    {
                        hata(tersDugumu.Satir, onek + "\"ters\" true ya da false olmalı");
                        ogeTamam = false;
                    }
                    else
                    {
                        ters = tersDugumu.MantiksalDeger;
                    }
                }

                if (!ogeTamam)
                {
                    tamam = false;
                    continue;
                }

                sonuc.Add(new AgirlikOgesi(ad.MetinDeger, agirlik, ters, ad.Satir));
            }

            return sonuc;
        }
    }
}
