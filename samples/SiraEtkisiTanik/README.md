# SiraEtkisiTanik: sıra etkisi 3c (tanık sırası)

Aynı iki oyuncu eylemi, farklı sırada: Zeki tehdidi yalnızca B'de görür. **Kasaba Hanı değildir.** İçeriği Yusuf seçti (7. hafta, seçenek 3c).
Test: `tests/Yanki.Scenarios.Tests/SiraEtkisiTests.cs`.

| Varlık | Mekan                 | Rol                                                                         |
| ------ | --------------------- | --------------------------------------------------------------------------- |
| `Ayse` | han                   |                                                                             |
| `Can`  | han                   | açık tehdidin hedefi                                                        |
| `Zeki` | pazar → han (kendisi) | `han_a_gel` eylemi (ön koşul: oyuncuyla aynı mekanda değil) ile han'a gelir |

Oyuncu han'da kalır. **Sıra A:** t1 Can'a açık tehdit (`tehdit_etti`), t5 Ayşe'ye yardım (`yardim_etti`). **Sıra B:** t1 Ayşe'ye yardım, t5 Can'a açık tehdit. 10 tick, seed 42.
Zeki'nin t5'ten önce han'a geldiği seed 42 için testle doğrulanır (`geldi` olayı t5'ten önce, son konumu han).

Kurallar: `tehdit_gordu` (tehdidi gören tanıkta `olay.fail` için `tehdit_gordu` −15), `yardim_minneti` (yardım edilende `minnet` +15).

**Beklenen fark:** Zeki A'da (t1'de hâlâ pazarda) tehdidi görmez: `tehdit_gordu` modifier'ı ve `gordu` kanıtı yok; B'de (t5'te han'da) görür. Ayşe ve Can iki sırada da görür.
`why`: Zeki'nin `tehdit_gordu` modifier'ı kendi algı olayına (`gordu` kanıtı) ve oyuncunun tehdidine iner.

**Yer tutucu sayılar:** kişilik değerleri, `han_a_gel` tabanı 2, modifier değerleri (−15, +15) ve süreleri (100 tick).
