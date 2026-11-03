# SiraEtkisiKriz: sıra etkisi 2a (Hatice'nin krizi)

Aynı NPC'nin krizi, yalnızca oyuncunun iki eyleminin sırasından doğan farkla değişir. **Kasaba Hanı değildir.** İçeriği Yusuf seçti (7. hafta, seçenek 2a).
Test: `tests/Yanki.Scenarios.Tests/SiraEtkisiTests.cs`.

| Varlık   | Mekan | Rol                                               |
| -------- | ----- | ------------------------------------------------- |
| `Hatice` | han   | dürüst (0.9), merhametli (0.8), kinci değil (0.2) |
| `Ayse`   | han   | yalanın olası kurbanı                             |
| `Kemal`  | han   | yalanın olası kurbanı, seyirci                    |

Oyuncu eylemleri: **sır** = Hatice'ye `sir_sakliyor(Hatice,Mehmet)` ve `borclu(Mehmet,Ayse,500)` anlatımı (yalan durumunu açar: ön koşul sır inancı, çelişeceği borç inancı);
**baskı** = Hatice'ye `hakaret_et` (güven −60; Hatice oyuncuyu tehdit eder, `stres_tabani` 70 × merhamet 0.8 = +56 stres).
**Sıra A:** t1 sır, t4 baskı. **Sıra B:** t1 baskı, t4 sır. 14 tick, seed 42.

`yalan_soyle` bu senaryoda taban 4, `stres_tabani` 10 (+9 stres); `tehdit_et` `hafiza_var(kacti | itiraf_etti, ben, 24)` ve kendi tekrarı için 24 tick bekler.
Kriz tablosu: seviye 1 `itiraf` (kriterler: dürüstlük ≥ 0.7 ve **`yalan_soyledi(eylem.hedef)`**, hedef `yalan_kurbani`), yedek `kac_alt` (`kacti`); seviye 2 `patla` (kin_tutma ≥ 0.7) ve yedek `kac_ust`.

**Beklenen fark:** A'da Hatice önce yalan söyler (stres +9, ardından tehdit +56 = 65 ≥ 50) ve kriz **`itiraf_etti`** olur, hedefi yalan kurbanı; B'de tehdit yalandan önce
eşiği aşar (56 ≥ 50), yalan kurbanı olmadığı için `itiraf` elenir ve **`kacti`** (`kriz:kac_alt`) olur; 24 tick yalan yok. `why`: kriz her iki sırada da kendi stres artışlarına
(A: yalan + tehdit, B: yalnız tehdit), tehdit eylemine ve oyuncunun hakaretine iner.

**Yer tutucu sayılar:** kişilik değerleri, `stres_tabani` 10 ve 70, taban 4 ve 3, eşikler 50/80 ve düşüşler 30/60 (`StresAyarlari`), modifier değerleri.
