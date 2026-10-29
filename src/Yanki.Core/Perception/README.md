# Perception

Algı: görünürlük seviyelerine göre doğrudan algı (plan 5.3). Dedikodu, top-n konu sınırı ve aktarımda kanıt gücü düşüşü 6. haftada gelecek.

**Durum:** 3. hafta teslimi tamam (yalnızca doğrudan algı).

## Tipler

| Tip | Ne yapar |
|---|---|
| `DogrudanAlgi.Algilayanlar(dunya, olay)` | Görünürlüğe (`acik` / `sessiz` / `gizli`, yükte `gorunurluk`) göre olayı algılayan NPC'ler, ordinal sıralı. |
| `DogrudanAlgi.Uret(dunya, olay, gunluk)` | Her algılayan için `kanit_eklendi` (`gordu`, güç 0.9) ve `hafiza_eklendi` olayı üretir; `KuralId: algi`, `NedenOlaylar: [olay]`. Günlüğe eklemez. |
| `DogrudanAlgi.OnermeUret(olay)` | `gercek_eklendi` → gerçek; `gercek_kaldirildi` → `degil(gercek)`; diğer → `tur(fail, hedef)`. |
| `AlgiAyarlari` | `GorduKanitGucu` (0.9) ve `HafizaOnemi` (0.5). |

Taslak not ve açık noktalar: [`docs/notes/algi.md`](../../../docs/notes/algi.md).
