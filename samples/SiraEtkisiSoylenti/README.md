# SiraEtkisiSoylenti: sıra etkisi 1a (plan 7, Durum 1 / Durum 2)

Oyuncunun Can'a yardım etmesi ile söylentiyi Ayşe'ye anlatması AYNI iki eylemdir; yalnızca sırası değişir. **Kasaba Hanı değildir.** İçeriği Yusuf seçti
(7. hafta, seçenek 1a). Test: `tests/Yanki.Scenarios.Tests/SiraEtkisiTests.cs` (değişmezler senaryodan önce yazıldı).

| Varlık  | Mekan | Rol                                   |
| ------- | ----- | ------------------------------------- |
| `Ayse`  | han   | dedikoducu (sosyallik 0.9)            |
| `Can`   | han   | Mehmet'in kardeşi (söylentiyi dinler) |
| `Kemal` | han   | seyirci                               |

**Sıra A:** t1 yardım (`yardim_etti` → Can), t4 söylenti (Ayşe'ye `borclu(Mehmet,Ayse,500)` ve `kardes(Mehmet,Can)`).
**Sıra B:** t1 söylenti, t4 yardım. 14 tick, seed 42. Ayşe `anlat` eylemiyle (utility) söylentiyi Can'a iletir; bu seed'de söylentiden bir tick sonra iftira modifier'ı yazılır.

Kurallar: `aile_iftirasi` (Can'da oyuncuya `aileme_iftira` −22; `en_fazla: 1`, yoksa söylenti geri dolanınca tekrar yazılırdı), `yardim_minneti` (yardım edilen Can,
iftira modifier'ı yoksa `minnet` +15) ve `yardim_supheli` (iftira varsa `supheli` −10).

**Beklenen fark:** A'da Can'da `minnet` (+15) ve sonra `aileme_iftira`; B'de `supheli` (−10) ve `aileme_iftira`, `minnet` yok. Can'ın oyuncu hakkındaki fikri A'da B'den yüksek.
`why`: `minnet`/`supheli` yardımın oyuncu eylemine, `aileme_iftira` Ayşe'nin anlatımından oyuncunun anlatımına iner.

**Yer tutucu sayılar** (11. hafta denge turunda ayarlanacak): kişilik değerleri, `anlat` tabanı 2, modifier değerleri (+15, −10, −22) ve süreleri (100 tick).
