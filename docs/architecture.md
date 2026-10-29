# Mimari

> Taslak. Modüller eklendikçe her bölüm doldurulacak (plan bölüm 5 ve 6).

## Modüller

Motor sekiz modülden ve bunların ortak kullandığı kriter dilinden oluşur. Modüller yalnızca tanımlı arayüzlerle konuşur.

| Modül | Klasör | Sorumluluk | Hafta |
|---|---|---|---|
| Olay Kaydı | `Events` | Değişmez günlük, nedensellik (`NedenOlaylar`, `KuralId`), durum hash'i | 2 |
| Dünya | `World` | NPC'ler, mekan grafiği, eşyalar, gerçekler, korumalı varlıklar, anahtarlı RNG | 2 |
| Algı | `Perception` | Görünürlük, doğrudan algı, dedikodu, top-n konu sınırı | 3, 6 |
| Zihin | `Mind` | Kişilik, ilişki, fikir modifier'ları, ruh hali, stres, hafıza, inançlar | 3, 6 |
| Sonuç Kuralları | `Rules` | `ne_zaman <olay> + kriterler → etkiler` | 4, 7 |
| Karar | `Decision` | `IActionSelector`, `UtilitySelector`, karar izi | 5 |
| Frenler | `Brakes` | Cooldown, tetiklenme ve zincir sınırı, korumalı bayraklar, yay değişkenleri | 7 |
| Simülasyon / Dış API | `Simulation` | Tick döngüsü, `Submit`, `Tick`, sorgular, kayıt/yükleme | 4, 7 |
| Kriter dili (ortak) | `Criteria` | Ağırlıklı kriterler: sonuç kuralları, diyalog, eylem ön koşulları | 2 |

Diyalog (`Yanki.Dialogue`) ayrı bir pakettir: şablon seçimi, mini gramer, `IDialogueRenderer` (8. hafta).

## Tick döngüsü

```mermaid
flowchart TD
    S1["1. Bekleyen oyuncu eylemleri olay olarak eklenir (KuralId: player)"]
    S2["2. Algı: kim neyi algıladı, inançlara kanıt eklenir"]
    S3["3. Sonuç kuralları çalışır (cooldown, sınır, korumalar); zihin değişiklikleri yeni olay"]
    S4["4. Süresi dolan modifier'lar düşer; ruh hali ve stres söner; hafıza silikleşir; eşikte kriz"]
    S5["5. Her NPC için sabit sırayla, anahtarlı RNG ile IActionSelector eylem seçer"]
    S6["6. Seçilen eylemler olay olarak kayda düşer"]
    S7["7. Durum hash'i alınır; olaylar dışarı yayınlanır"]
    D{"Zincir derinliği D'yi aştı mı?"}
    NEXT["Kalan etkiler bir sonraki tick'e ertelenir"]
    S1 --> S2 --> S3 --> S4 --> S5 --> S6 --> D
    D -- "hayır, yeni olay var" --> S2
    D -- "hayır, yeni olay yok" --> S7
    D -- evet --> NEXT --> S7
```

## Değişmezler

- Determinizm: aynı seed + aynı girdi = aynı durum hash'i.
- Her olay nedenini taşır (`NedenOlaylar`, `KuralId`).
- Çekirdek dış pakete, ağa, dosya sistemine bağımlı değildir.

Kararların gerekçeleri: [`adr/`](adr/).
