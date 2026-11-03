# FrenliHan: 7. haftanın frenli senaryosu (F1, golden replay #4)

Cooldown, `en_fazla`, korumalı varlık ve yay değişkeni tek senaryoda. **Kasaba Hanı değildir** (o 9. hafta). İçeriği Yusuf seçti (7. hafta, seçenek F1).
Test: `tests/Yanki.Scenarios.Tests/FrenliHanTests.cs` (değişmezler senaryodan önce yazıldı). Golden: `tests/golden/frenli-han-1.json` (seed 42, 24 tick).

| Varlık    | Mekan | Rol                                                                                     |
| --------- | ----- | --------------------------------------------------------------------------------------- |
| `Hanci`   | han   | **korumalı: `mekandan_ayrilamaz`**; sosyal (0.9)                                        |
| `Ciftci`  | han   | oyuncunun tehdit ettiği kişi                                                            |
| `Yabanci` | han   | açgözlü (0.8); `hirsizlik` eylemi **`kese` (korumalı: `calinamaz`)** varlığını bildirir |

`oldurulemez` kullanılmaz; onu deneyen eylem yoktur. Yay: `kasaba_gerginligi` 0..5, başlangıç 0.

**Eylemler (`eylemler.json`):** `git` (komsu_mekan, taban 0.25, ön koşul sosyallik ≥ 0.7: Hancı ve Yabancı için aday; Hancı için `mekandan_ayrilamaz` ihlali, aday elenir ve fren yazılır),
`hirsizlik` (taban 0.5, ön koşul açgözlülük ≥ 0.7, `ihlal_bayraklari: calinamaz → kese`; yalnızca Yabancı için ön koşullar tutar, bu yüzden `korumali` freni yazılır), `dinlen` (0.3).

**Kurallar (`kurallar.json`)**, hepsi `tehdit_etti` olayında ve yalnızca hedef Çiftçi için (`olay_rolu(hedef)`):

- `tehdit_gerginligi`: yay `kasaba_gerginligi` +2, `kapsam: kural`, `en_fazla: 3`.
- `tehdit_korkusu`: Çiftçi'de oyuncu için `tehdit_korkusu` −12 (40 tick), `cooldown: 12`, `kapsam: npc`.
- `gergin_ofke`: `yay(kasaba_gerginligi) >= 4` iken Çiftçi'de `gergin_ofke` −10 (40 tick); gerginlik düşükken aynı tehdit bu etkiyi vermez.

**Oyuncu eylem günlüğü:** t2, t4, t8, t12, t16'da `tehdit_etti → Ciftci`.

| Tick | Olay                                                                                                                          |
| ---- | ----------------------------------------------------------------------------------------------------------------------------- |
| 1    | Hancı'nın `git` adayı `korumali`; Yabancı'nın `hirsizlik` adayı `korumali` (`kese`). Her biri 24 tick boyunca tek fren olayı. |
| 2    | Gerginlik 0 → 2; `tehdit_korkusu` çalışır. `gergin_ofke` yok (gerginlik 0).                                                   |
| 4    | Gerginlik 2 → 4; `tehdit_korkusu` pencerede: `cooldown` freni. `gergin_ofke` yok (adım başında gerginlik 2).                  |
| 8    | Gerginlik 4 + 2 = 6 → 5'e kırpılır: `yay_siniri`. `cooldown` freni. `gergin_ofke` çalışır (adım başında 4).                   |
| 12   | `tehdit_gerginligi` `en_fazla` doldu: `en_fazla` freni. `cooldown` freni (12 − 2 = 10 < 12). `gergin_ofke` çalışır.           |
| 16   | `en_fazla` freni. Pencere doldu (16 − 2 = 14 ≥ 12): `tehdit_korkusu` yeniden çalışır. `gergin_ofke` çalışır.                  |

**Yer tutucu sayılar** (11. hafta denge turunda ayarlanacak): kişilik değerleri, eylem tabanları (0.25, 0.5, 0.3), yay aralığı 0..5, delta +2, `en_fazla` 3, `cooldown` 12,
eşik `>= 4`, modifier değerleri (−12, −10) ve süreleri (40 tick), tehdit tick'leri.
