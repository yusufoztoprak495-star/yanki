# Decision

Karar: eylem tanımları, aday üretimi, puanlama, `IActionSelector`/`UtilitySelector` ve karar izi (plan 5.5, [ADR-011](../../../docs/adr/0011-karar-modeli.md)).

**Durum:** 5. hafta teslimi tamam.

## Tipler

| Tip | Ne yapar |
|---|---|
| `EylemYukleyici`, `EylemTanimi`, `Olcut`, `EylemKatalogu` | Eylem JSON'unu okur ve doğrular (`dosya:satir: eylem "id": mesaj`); kriterler ve etkiler mevcut okuyucularla okunur. Katalog dosyalar arası birleşik, kimliğe göre sıralı, kimlikler tekil. |
| `Egriler`, `EgriTanimi`, `DecimalMat` | `dogrusal`, `ustel`, `lojistik`, `basamak`; çıktı 0..1'e kırpılır ve 6 ondalığa yuvarlanır. `e^y` elle yazılmış decimal seri (`y` ∈ [−30, 30]). |
| `CarpanHesaplayici`, `CarpanSiniri` | Kişilik/fikir/ruh hali çarpanı: parçalı doğrusal, nötr 1.0; sınırlar 0.25–2.0, 0.6–1.5, 0.8–1.25. |
| `AdayUretici`, `Aday`, `ElenenAday` | Aday = (eylem, hedef). Tek nokta; ön koşullar hepsi tutmalı; hedefe bağlı olmayan koşullar eylem başına bir kez hesaplanır. |
| `Puanlayici`, `AdayPuani`, `OlcutKatkisi` | `taban × Π(eğri) × kişilik × fikir × ruh hali`. |
| `IActionSelector`, `UtilitySelector`, `SecimAyarlari`, `SecimSonucu` | Varsayılan: en yüksek puanın %70'ine ulaşan ilk 3 aday arasından puan ağırlıklı seçim (`N=1` argmax). Zar `AnahtarliRng.Icin(seed, tick, npc, "karar")`. |
| `KararIzi`, `AdayIzi` | Tam iz (adaylar, elenenler ve nedeni, ölçüt katkıları, üç çarpan, havuz, zar, seçilen, kaynaklar). |
| `KisaIz`, `KararIziYazici`, `KararOlayi` | Günlükteki `karar_verildi` olayı KISA iz taşır; `KararIziYazici.Yaz` tam, `YazKisa` kısa kanonik JSON üretir. |
| `KararIziYenidenUretici`, `KararIziOzeti` | Geçmiş kararın tam izini oturumu replay ile oynatarak üretir ve kısa izle doğrular (`KararIziUyusmazligiException`); okunur satırlar. |
| `YerlesikTurler` | Çekirdeğe ayrılmış adlar: `ayrildi`, `geldi`, `karar_verildi`, `hareket`. |

Yazılmayanlar (sonraki haftalar): cooldown, tetiklenme ve zincir sınırları, fren olayları, korumalı bayraklar (aday üretimine takılacak), stres (`celisen_ozellikler` 6. haftada), GOAP/BT adaptörleri, hedef zihin katmanı (v0.2 sonrası).

Not: [`docs/notes/karar.md`](../../../docs/notes/karar.md). Eylem yazımı: [`docs/authoring-guide.md`](../../../docs/authoring-guide.md).
