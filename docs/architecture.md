# Mimari

> Taslak. Modüller eklendikçe her bölüm doldurulacak.

## Tick döngüsü (hedef)

```mermaid
sequenceDiagram
    participant O as Oyuncu
    participant G as Olay günlüğü
    participant A as Algı
    participant Z as Zihin
    participant K as Karar (IActionSelector)
    participant R as Sonuç kuralları
    O->>G: Eylem olayı
    G->>A: Görünür olaylar
    A->>Z: Gözlem → inanç (kanıt listesi), hafıza
    Z->>K: Kişilik, ilişki, ruh hali, stres
    K->>G: Seçilen eylem (+ karar izi)
    G->>R: Yeni olaylar
    R->>G: Sonuç olayları (KuralId + NedenOlaylar)
```

## Modüller (`Yanki.Core`)

| Modül | Sorumluluk | Hafta |
|---|---|---|
| Events | Olay tipleri, günlük, durum hash'i | 2 |
| World | Gerçekler, mekan grafiği, anahtarlı RNG | 2 |
| Criteria | Kriter dili ayrıştırıcı ve puanlayıcı | 2 |
| Mind | Kişilik, 3 eksenli ilişki + fikir modifier'ları, ruh hali, hafıza, inançlar, stres | 3, 6 |
| Perception | Görünürlük, doğrudan algı, dedikodu ile aktarım | 3, 6 |
| Rules | Sonuç kuralları, frenler, korumalı bayraklar | 4, 7 |
| Decision | `IActionSelector`, `UtilitySelector`, karar izi | 5 |
| Simulation | Tick döngüsü, kayıt/yükleme | 4, 7 |

## Değişmezler

- Determinizm: aynı seed + aynı girdi = aynı durum hash'i.
- Her olay nedenini taşır.
- Çekirdek dış pakete, ağa, dosya sistemine bağımlı değildir.

Kararların gerekçeleri: [`adr/`](adr/).
