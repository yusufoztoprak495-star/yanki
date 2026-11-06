# Kasaba Hanı (demo senaryosu)

> **TASLAK** (Claude Code, 9. hafta). Yusuf seçer ve düzeltir; kadro: seçenek A "Yorgun han", alacaklı Rıza. Tüm kişilik değerleri, taban sayıları, eşikler, süreler ve şablon metinleri **yer tutucudur** ve 11. hafta denge turunda ayarlanır. JSON'da yorum yazılamadığı için taslak notu burada ve `npcs.md` başındadır.

8 NPC'lik küçük bir han. Motorun "her karar yankılanır" iddiasını gösteren örnek senaryo. 12. haftadaki hızlı başlangıç bu senaryoyu kullanır.

- **Kadro:** [`npcs.md`](npcs.md) (Yusuf'un tasarım tablosu).
- **Dosyalar:** `scenario.json`, `eylemler.json`, `kurallar.json`, `krizler.json`, `sablonlar.json` (şablonlar ayrı dosya, ADR-014), `oyuncu_eylemleri.json` (oyuncu menüsü, senaryo hash'ine girmez), `oynanislar/*.json` (coverage ve ölçüm betikleri).
- **Doğrulama:** `dotnet run --project src/Yanki.Cli -- validate samples/KasabaHani --uyari-hata` (hata ve uyarı vermez). **Kapsama:** `... coverage samples/KasabaHani --seeds 20`.

## İçerik bütçesi (plan 10)

|                 | Bütçe           | Bu senaryo                   |
| --------------- | --------------- | ---------------------------- |
| NPC             | 8 (en fazla 10) | 8                            |
| Eylem           | ~15             | 16 (menü eylemleri sayılmaz) |
| Sonuç kuralı    | 25–35           | 29                           |
| Şablon          | 60–80           | 80                           |
| Yay değişkeni   | 2               | 2                            |
| Korumalı varlık | 1–2             | 2                            |
| Gerilim tohumu  | 3               | 3                            |

Kriz tablosu: 4 satır (her seviyeye bir yedek). Oyuncu menüsü: 9 girdi (en fazla 10).

## Dünya

Mekanlar (7): `han`, `pazar`, `tarla`, `dukkan`, `muhtarlik`, `ahir`, `depo`. Bağlantılar: han–pazar, pazar–tarla, pazar–dukkan, pazar–muhtarlik, han–ahir, han–depo.

Başlangıç gerçekleri: `borclu(Mehmet,Riza,500)`, `kardes(Mehmet,Can)`, `sahip!(muhur,Osman)`, `yeri(muhur,depo)`, `yeri(kasa,han)`. Eşya mekaniği yeni mekanik gerektirmedi: eşya yalnızca gerçeklerde geçen bir kimliktir (`esya` varlığı tanımlanamaz; korumalı kimlikler için `validate` uyarı verir). **Başlangıç gerçekleri NPC zihnine inanç yazmaz** (aşağıda "Bilinen sınırlar").

## Üç gerilim tohumu ve plan 7 sahnesi

| Tohum             | Ne olur                                                                                                                                                                | Oyuncunun etkisi                           |
| ----------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------ |
| **Borç** (plan 7) | Mehmet'in Rıza'ya 500 lira borcu var. Ayşe borcu yalnızca oyuncudan duyar (başlangıçta inancı yoktur), Can'a anlatır; Can'da `aileme_iftira(oyuncu)` oluşur            | `borc_anlat`, `borc_ode`, `ele_ver_iste`   |
| **Kayıp eşya**    | Osman'ın mührü kayıp (aslında Fatma bulup `depo`ya kaldırmış: `yeri(muhur,depo)`)                                                                                      | `muhur_yeri_anlat` (Osman'a) ya da `sakla` |
| **Hırsızlık**     | Ayşe `para_say`ar, açgözlü ve cesur Kerem Ayşe'nin kesesini çalar (`kese_calindi`); `kasa` korumalıdır (kasaya el atma denemesi `korumali` freni olarak günlüğe düşer) | `kerem_suclu_anlat`, `sakla`               |

Plan 7 sahnesi Kasaba'da kendiliğinden çıkar (`KasabaHaniTests`): oyuncu Ayşe'ye borcu anlatır (tick 1), Ayşe Can'a anlatır, Can'da `aileme_iftira(oyuncu)` (−22, 432 tick) oluşur. Can'ın satırı: Durum 1 (tick 1) "Hoş geldin yabancı, bir ihtiyacın var mı?"; Durum 2 (tick 6) "Ağabeyim hakkında ne dediğini Ayşe'den duydum." Durum 2b: oyuncu Can'dan Mehmet'i ele vermesini isterse (`ele_ver_iste`) Can'ın `kardesi_ele_ver` eylemi merhametine aykırıdır, stresi artar ve kriz eşiğinde `itiraf` krizi olur.

## Tohum → kural tablosu

| Tohum                 | Kurallar                                                                                                                                                                                                                                                             |
| --------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Borç / plan 7         | `aile_iftirasi`, `borc_duyuldu`, `borc_sikintisi_goruldu`, `borc_talebi_hedef`, `borc_talebi_tanik`, `borc_odendi_alacakli`, `borc_odendi_tanik`, `borc_odendi_kardes`, `ele_verme_istendi`, `kardes_ele_verildi_hedef`, `kardes_ele_verildi_tanik`, `itiraf_alindi` |
| Kayıp eşya            | `muhur_soruldu_hedef`, `muhur_soran_supheli`, `muhur_yeri_ogrenildi`                                                                                                                                                                                                 |
| Hırsızlık             | `kese_goruldu`, `kese_calindi_magdur`, `kese_calindi_tanik`, `kese_haberi`, `suclanan_kirgin`, `suclama_tanik`, `saklayana_supheli`, `tehdit_korkusu`, `tehdit_tanik`                                                                                                |
| Yaylar (tüm tohumlar) | `toplanti_sakinlestirdi`, `misafirperverlik`, `yardima_minnet`, `hakarete_kirgin`, `hakaret_tanik`                                                                                                                                                                   |

Son satırdaki beş kural doğrudan bir tohuma değil, oyuncu eylemi ailelerine ve yaylara (`kasaba_gerginligi`, `han_itibari`) hizmet eder; Yusuf karar versin.

## Yay değişkenleri ve korumalı varlıklar

| Ad                  | Aralık             | Yazan kurallar                                                                                                                                                                                                                                     | Okuyan                                       |
| ------------------- | ------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------- |
| `kasaba_gerginligi` | 0–10               | `aile_iftirasi`, `borc_talebi_hedef`, `ele_verme_istendi`, `hakarete_kirgin`, `tehdit_korkusu`, `kese_calindi_*`, `suclanan_kirgin` (artırır); `borc_odendi_alacakli`, `muhur_yeri_ogrenildi`, `itiraf_alindi`, `toplanti_sakinlestirdi` (azaltır) | eylem `toplanti_cagir` (Osman; ön koşul ≥ 4) |
| `han_itibari`       | 0–10 (başlangıç 3) | `yardima_minnet`, `borc_odendi_alacakli`, `muhur_yeri_ogrenildi`, `misafirperverlik` (artırır); `hakarete_kirgin` (azaltır)                                                                                                                        | eylem `misafir_agirla` (Rıza; ön koşul ≥ 4)  |

Korumalı: `Riza` (`mekandan_ayrilamaz`: hancı han'dan ayrılamaz), `kasa` (`calinamaz`: eylem `kasa_acmak` her seferinde aday filtresinde elenir, yalnızca `korumali` freni olarak görünür; "neden olmadı?" sorusunun yanıtıdır).

## Oyuncu menüsü ve betikler

Menü `oyuncu_eylemleri.json` (9 girdi): konuş (`selamla`), bilgi ver (`borc_anlat`, `muhur_yeri_anlat`, `kerem_suclu_anlat`), sakla (`sakla`), iyilik (`yardim_et`, `borc_ode`), kötülük (`hakaret_et`, `ele_ver_iste`). `validate` her girdinin `Submit`'te geçerli olduğunu denetler.

Betikler (`oynanislar/`, **taslak**; coverage ve ölçümde kullanılır, ayrıca oyuncusuz koşu):

| Betik               | Ne yapar                                                                                                     |
| ------------------- | ------------------------------------------------------------------------------------------------------------ |
| `plan7_borc`        | Plan 7: Can'a selam (söylentiden önce), Ayşe'ye borcu anlat, Can'a selam (sonra), Can'dan ele vermesini iste |
| `iyilik_muhur`      | Rıza'ya borç ödemesi, Fatma'ya yardım, Osman'a mührün yerini söyle                                           |
| `kotuluk_saklama`   | Kerem ve Can'a hakaret, Can'dan ele vermesini iste, Rıza'dan bir şey sakla, Fatma'ya hakaret                 |
| `hirsizlik_suclama` | Rıza ve Ayşe'ye Kerem'in kese çaldığını anlat, Fatma'dan bir şey sakla                                       |

## Bilinen sınırlar (Yusuf'un kararı bekleyen)

- **Başlangıç inancı yok.** Senaryo `gercekler`i dünyaya yazar ama NPC zihnine kanıt yazmaz; bu yüzden "Rıza ve Mehmet borcu bilir" kural içi `gercek(...)` ile ifade edilir ve NPC kökenli _borç_ söylentisi oluşamaz (NPC kökenli söylenti hırsızlıktan çıkar: `kese_haberi`, `kok(..., oyuncu) == false`). Gerçek başlangıç bilgisi için yeni bir mekanik gerekir.
- **Eylemi NPC'ye bağlama yok.** Kişilik ön koşulları hedefi daraltır; kişilik değerleri değişirse bağlama bozulabilir (11. hafta denge turunda dikkat).
- **`kasa_acmak` hiç seçilmez** (tasarım gereği: yalnızca fren olayı üretir).
- Bazı genel şablonlar hiç seçilmez (özgül şablon kazanır); `coverage` raporunda listelenir.
