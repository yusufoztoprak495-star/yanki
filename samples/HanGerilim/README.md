# HanGerilim: 6. haftanın dedikodu, yalan, stres ve kriz dilimi

Üç şeyi tek senaryoda gösterir: **(1)** oyuncunun bir söylentisi Ayşe'den Can'a yayılır ve `why` ilk anlatana iner (plan 7); **(2)** dürüst Hatice kişiye özel bir nedenle iki farklı yalan söyler, stresi birikir, kişiliğine uygun **itiraf** krizi çıkar; **(3)** hakarete uğrayan kinci Selim iki kez tehdit eder, stresi 80'i aşar ve **patlar**.
**Kasaba Hanı değildir** (o, 9. hafta). İçeriği Yusuf seçti (6. hafta seçenek 3, "Han'da gerilim"). `MiniHan` ve `MiniKarar` değişmedi.

## Varlıklar

Mekanlar: `han` — `pazar`. Oyuncu handadır.

| Varlık   | Mekan | Rol                                            | Kişilik (yer tutucu)                                      |
| -------- | ----- | ---------------------------------------------- | --------------------------------------------------------- |
| `Ayse`   | han   | hancı, dedikoducu                              | sosyallik 0.9, merhamet 0.6, kin_tutma 0.3                |
| `Can`    | han   | Mehmet'in kardeşi                              | durustluk 0.7, kin_tutma 0.6, cesaret 0.6                 |
| `Hatice` | han   | dürüst, merhametli; Mehmet'in sırrını saklıyor | durustluk 0.9, merhamet 0.8, kin_tutma 0.2, sosyallik 0.6 |
| `Kemal`  | han   | seyirci                                        | hepsi 0.4–0.6                                             |
| `Selim`  | han   | kinci, merhametli                              | kin_tutma 0.9, merhamet 0.8, cesaret 0.7                  |
| `Mehmet` | pazar | borçlu; hiç hareket etmez, konuşmaz            | sosyallik 0.3                                             |

**Kişilik değerleri, aşağıdaki taban/ölçüt sayıları, `stres_tabani` değerleri, kriz eşikleri (50/80) ve düşüşleri (30/60) yer tutucudur;** 11. hafta denge turunda ayarlanacak.

## Eylemler (`eylemler.json`)

| Eylem         | Hedef  | Tür / olay                  | Taban | Ön koşul ve ölçüt                                                                                                                                                                                                                                                                                                  |
| ------------- | ------ | --------------------------- | ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `anlat`       | NPC    | `aktarim: dogru`, `anlatti` | 2     | `konu_desenleri`: `borclu(*,*,*)`, `kardes(*,*)`; ölçütler `sosyallik` (lojistik) ve `konu_puani(eylem.hedef)`; kişilik çarpanı sosyallik                                                                                                                                                                          |
| `yalan_soyle` | NPC    | `aktarim: yalan`, `anlatti` | 1     | **ön koşul `inanc(sir_sakliyor(ben, Mehmet)) >= 0.5`** (aşağıda), `hafiza_var(itiraf_etti \| sakinlesti \| kacti, ben, 24) == false`; kişilik çarpanı dürüstlük TERS; `celisen_ozellikler: [durustluk]`, `stres_tabani: 30`; yalan önermeleri: `degil(borclu(Mehmet,Ayse,500))`, `degil(borclu(Selim,Hatice,200))` |
| `tehdit_et`   | oyuncu | `tehdit_etti`               | 3     | ön koşul `fikir(eylem.hedef) < -10`, `hafiza_var(patladi \| sakinlesti \| itiraf_etti \| kacti, ben, 24) == false`; ölçüt `kin_tutma`; `celisen_ozellikler: [merhamet]`, `stres_tabani: 60`; etki: öfke 0.5                                                                                                        |
| `dinlen`      | yok    | `dinlendi`                  | 0.3   | yok (bekleme eylemi)                                                                                                                                                                                                                                                                                               |

- **Yalanın kişiye özel nedeni:** `sir_sakliyor(Hatice,Mehmet)` ("Hatice, Mehmet'in sırrını saklıyor") önermesini yalnızca Hatice bilir: oyuncu tick 1'de yalnızca ona anlatır ve `anlat` bu yüklemi konu yapmaz (kimseye yayılmaz). `yalan_soyle`'nin ön koşulu bu inancı ister; yani yalan ancak Hatice için aday olur. Kişilik yalnızca puanı ve stres maliyetini belirler: dürüstlük ne kadar yüksekse yalan puanı o kadar düşük, stres o kadar yüksektir (30 × 0.9 = 27 / yalan).
- **Kriz sonrası bekleme (7. hafta cooldown'ına kadar):** yeni özellik yok, mevcut `hafiza_var`: kriz olayı (`itiraf_etti`, `sakinlesti`, `patladi`, `kacti`) 24 tick boyunca ilgili eylemi engeller. Bu, `docs/notes/karar.md`'deki tekrar sınırı kısıtlarını taşır (200 tick sonra unutma, eylem türüne göre).

## Kurallar (`kurallar.json`)

| Kural              | `ne_zaman`    | Koşul                                                                   | Etki                                                                                  |
| ------------------ | ------------- | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| `aile_iftirasi`    | `anlatti`     | `olay_konusu(borclu(Mehmet,*,*))` ve `inanc(kardes(Mehmet, ben)) > 0.3` | `oyuncu` hedefli `aileme_iftira` modifier'ı −22, 18 tick (yalnızca Mehmet'in kardeşi) |
| `yalan_yakalandi`  | `itiraf_etti` | `olay_rolu(hedef)`                                                      | `olay.fail` için `yalanini_yakaladim` −30 (30 tick), güven −10, öfke 0.6              |
| `hakarete_kirgin`  | `hakaret_et`  | `olay_rolu(hedef)`                                                      | `oyuncu`ya güven −60                                                                  |
| `patlamadan_korku` | `patladi`     | `olay_rolu(tanik)`                                                      | `olay.fail`e korku +10                                                                |

**Sınır (Yusuf'un kararı):** `aile_iftirasi` modifier'ının hedefi `oyuncu` olarak SABİT yazılmıştır, çünkü bu senaryoda söylentinin tek kökü oyuncudur. Kuralın söylentinin kökünü okuyan bir sorgusu yoktur (19. fonksiyon eklenmedi); NPC kökenli bir borç söylentisi de aynı kuralı oyuncuya karşı tetiklerdi. Kasaba Hanı'nda NPC kökenli söylentiler için kökü okuyan sorgu gerekip gerekmediği 9. hafta açık kararıdır (`docs/DURUM.md`).

## Kriz tablosu (`krizler.json`)

Seviye 1 = alt eşik (50), seviye 2 = üst eşik (80). `itiraf` (seviye 1, dürüstlük ≥ 0.7, hedef: en son yalan söylenen kişi), `sakinles` (seviye 1, yedek), `patla` (seviye 2, kin_tutma ≥ 0.7, hedef: en soğuk kişi), `kac` (seviye 2, yedek).

## Akış (seed 42, 24 tick)

Oyuncu eylem günlüğü: tick 1'de beş anlatım (Ayşe'ye `borclu(Mehmet,Ayse,500)` ve `kardes(Mehmet,Can)`; Hatice'ye `borclu(Mehmet,Ayse,500)`, `borclu(Selim,Hatice,200)` ve `sir_sakliyor(Hatice,Mehmet)`); tick 10'da `hakaret_et → Selim`.

| Tick | Olay                                                                                                                              |
| ---- | --------------------------------------------------------------------------------------------------------------------------------- |
| 1    | NPC'ler birbirine söylenti yayar (Ayşe → Can: kardeşlik ve borç; güç 0.7 → 0.49).                                                 |
| 2    | Can'da `aileme_iftira` (oyuncuya −22). Can oyuncuya soğuduğu için tehdit eder (tick 2 ve 3).                                      |
| 4    | Can'ın stresi 60'a ulaştı: `sakinlesti` krizi (yedek satır; yalan kurbanı olmadığından itiraf elenir). 24 tick tehdit yok.        |
| 4    | Hatice, Ayşe'ye ilk yalanı söyler (`degil(borclu(Mehmet,Ayse,500))`), stres +27.                                                  |
| 6    | Hatice, Ayşe'ye ikinci (farklı) yalanı söyler (`degil(borclu(Selim,Hatice,200))`), stres 54.                                      |
| 7    | **`itiraf_etti`** (Hatice → Ayşe, en son kurban). Stres 54 → 24. 24 tick yalan yok.                                               |
| 8    | Ayşe'de `yalanini_yakaladim` (Hatice için); `why` bunu itiraftan stres artışlarına, `anlatti` yalanlarına ve karar izine indirir. |
| 10   | Oyuncu Selim'e hakaret eder; Selim oyuncuya güvenini kaybeder, tick 10 ve 11'de oyuncuyu tehdit eder (stres 48 + 48).             |
| 12   | **`patladi`** (Selim → oyuncu: en soğuk kişi). Stres düşer, tanıklarda `patlamadan_korku`. 24 tick tehdit yok.                    |

## Beklenen sonuç

Değişmezler (testle, `tests/Yanki.Scenarios.Tests/HanGerilimTests.cs`; koşudan ÖNCE yazıldı): I1 Ayşe → Can söylentisi ve yalnızca Can'da `aileme_iftira`, `why` oyuncunun anlatımına iner; I2 Hatice tam 2 farklı yalan, 1 itiraf, sonra yalan yok, itiraf kurbanında `yalanini_yakaladim`, yalan yalnızca Hatice'de; I3 Selim 2 tehdit, 1 patlama, sonra tehdit yok; I4 Can en fazla 2 tehdit, krizden sonra tehdit yok; I5 24 tickte tam 3 kriz (`itiraf`, `patla`, `sakinles`); I6 aynı seed aynı hash.
Seed 42 son durum hash'i `690f2f4ee9d3d385` (golden #3: `tests/golden/han-gerilim-1.json`). Seed 7 de aynı sayıları verir (2 yalan, 1 itiraf, Can 2 / Selim 2 tehdit, 1 patlama, 1 sakinleşme); yalnızca yalanların kurbanı ve hash'i (`4bf5ab4e4786f4de`) farklıdır.

## Bilinen sınırlar

- Tekrar sınırı `hafiza_var` ile (24 tick); gerçek cooldown 7. haftada. 7. hafta cooldown'ı gelince bu ön koşullar yeniden değerlendirilecek; golden #3 değişirse Yusuf'un onayı alınır.
- Kriz sonrası stres ~24 iken tek güçlü eylem yeniden krize yetebilir (24 + 27 ≥ 50); düşüş ve `stres_tabani` dengesi 11. hafta denge turunda ölçülecek. Burada `hafiza_var` bekleme süresi bunu 24 tick engeller.
- Can'ın iki tehdidi söylentinin yan etkisidir (soğuma → tehdit); senaryo bunu bilerek gösterir.
