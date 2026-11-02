# MiniHan: 4. haftanın 3 NPC'lik ince dikey dilimi

Motoru uçtan uca sınamak için küçük bir senaryo: oyuncu eylemi → algı → kural → fikir değişimi → `why` zinciri. **Kasaba Hanı değildir** (o, 9. hafta); dedikodu ve NPC kararı yoktur, akış oyuncu eylemini aynı mekanda görenler üzerinden çalışır.
İçeriği Yusuf seçti (seçenek A + hedef/tanık düzeltmesi).

| Varlık          | Mekan | Not                                |
| --------------- | ----- | ---------------------------------- |
| `oyuncu`        | han   |                                    |
| `Ayse` (hancı)  | han   | cesaret 0.3                        |
| `Can` (demirci) | han   | cesaret 0.8                        |
| `Veli` (gezgin) | pazar | cesaret 0.5; han ile `pazar` bağlı |

Eylem türleri: `yardim_etti`, `tehdit_etti`. Kişilik özelliklerinden `cesaret` dışındakiler (açgözlülük, dürüstlük, sosyallik, kin tutma, merhamet) **0.5 yer tutucudur**; 11. hafta denge turunda ayarlanacak.

## Kurallar (`kurallar.json`)

Hedef/tanık ayrımı `olay_rolu(hedef)` / `olay_rolu(tanik)` ile yapılır (4. haftada Yusuf'un kararıyla sözlüğe eklendi). Bir kural yalnızca tüm kriterleri tutunca çalışır (ADR-010).

| Kural                 | Ne zaman      | Kim için                                    | Etki                                                |
| --------------------- | ------------- | ------------------------------------------- | --------------------------------------------------- |
| `yardim_minneti`      | `yardim_etti` | yalnızca olayın hedefi                      | `olay.fail`'e `minnet` +15 (336 tick)               |
| `tehdit_korkusu`      | `tehdit_etti` | yalnızca olayın hedefi                      | `korku` ekseni +15, `korku` duygusu 0.6, hafıza 0.9 |
| `tehdit_cesur_ofkesi` | `tehdit_etti` | yalnızca hedef ve `kisilik(cesaret) >= 0.7` | `tehdit_edildi` −20 (168 tick), `ofke` duygusu 0.5  |
| `tehdit_tanigi`       | `tehdit_etti` | hedef olmayan tanıklar                      | `olay.fail`'e `tehdit_gordu` −5 (168 tick)          |

## Akış ve beklenen sonuç

`t1 yardim_etti → Ayse`, `t2 tehdit_etti → Can`, toplam 6 tick, seed 42:

- **Ayse:** minnet +15 ve tehdit_gordu −5 → oyuncu hakkında fikir +10.
- **Can:** korku ekseni +15, tehdit_edildi −20, öfke duygusu (korku 0.6 baskın kalır) → fikir −23. Yardımın hedefi olmadığı için minnet yok.
- **Veli:** pazarda, hiçbir şey görmez → değişmez.

```bash
dotnet run --project src/Yanki.Cli -- run samples/MiniHan/scenario.json --seed 42 --eylem yardim_etti:Ayse --tick 1 --eylem tehdit_etti:Can --tick 5 --kaydet tests/golden/mini-han-1.json
dotnet run --project src/Yanki.Cli -- query tests/golden/mini-han-1.json Can
dotnet run --project src/Yanki.Cli -- why tests/golden/mini-han-1.json 23    # Can'ın tehdit_edildi modifier'ı → algı → tehdit_etti
```

Golden replay: [`tests/golden/mini-han-1.json`](../../tests/golden/mini-han-1.json). Uçtan uca test: `tests/Yanki.Scenarios.Tests/MiniHanTests.cs`.
