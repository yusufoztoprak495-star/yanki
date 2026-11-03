# Not: Frenler (7. hafta, TASLAK)

> **Yazar: Claude Code** (Yusuf'un isteğiyle). Bu not 7. hafta boyunca doldurulur; T16'da tamamlanır. **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## HanGerilim: `hafiza_var(...)` ön koşulları cooldown'a çevrilmeli mi? (T11)

**Karar (Yusuf):** Hayır. `samples/HanGerilim` eylemlerinin `hafiza_var(itiraf_etti | sakinlesti | patladi | kacti, ben, 24)` ön koşulları değişmez; cooldown bu koşulların yerini tutmaz.
Golden #3 (`han-gerilim-1.json`) bu karardan etkilenmez.

**Neden:** İki mekanizma farklı şeyi ifade eder. `hafiza_var(kriz, ben, 24)` krizden SONRA 24 tick eylemi engeller (toparlanma penceresi). Cooldown eylemin KENDİ son tetiklemesinden sayar (tekrar sınırı).
Aynı oyuncu eylemleri, seed 42, 24 tick ile üç varyant koşuldu (`eylemler.json` bir kopyada değiştirildi, `yalan_soyle` ve `tehdit_et`):

|                                          | bugün (`hafiza_var`) | `hafiza_var` yerine `cooldown: 24` | `hafiza_var` yerine `cooldown: 3` |
| ---------------------------------------- | -------------------- | ---------------------------------- | --------------------------------- |
| Hatice yalanı                            | 2                    | 1                                  | 7                                 |
| `itiraf_etti` / `sakinlesti` / `patladi` | 1 / 1 / 1            | **0 / 0 / 0**                      | 5 / 8 / 3                         |
| Tehdit (Can / Selim)                     | 2 / 2                | 1 / 1                              | 8 / 5                             |
| Fren olayı                               | 0                    | 6                                  | 32                                |
| `son_hash`                               | `d98b50f39fc358dd`   | `47d73a8cc99b4a9f`                 | `69669ea2cb4bd8ba`                |

- `cooldown: 24`: eylemler tek seferlik olur, stres hiçbir zaman kriz eşiğine ulaşmaz, HanGerilim'in ana değişmezleri (iki yalan, bir itiraf, Selim'in patlaması) bozulur.
- `cooldown: 3`: kriz tekrar tekrar çıkar; krizden sonraki toparlanma modellenmez.
- Ön koşulların yanına `cooldown` eklemek de (ör. `tehdit_et`'e `cooldown: 2`) Can'ın t2 ve t3'teki iki tehdidini bire indirir ve hash'i değiştirir.

Cooldown'ın doğru kullanımı `samples/FrenliHan`'da gösterilir (kural tekrarı). Krizden sonra bekleme `hafiza_var` ile kalır.
