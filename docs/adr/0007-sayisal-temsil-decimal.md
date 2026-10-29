# ADR-0007: Sayısal temsil: simülasyonda `decimal`

**Durum:** Önerildi
**Tarih:** 2026-10-29
**Kabul eden:** —
**Kabul tarihi:** —

## Bağlam
Durum hash'i ve golden replay (ADR-001), aynı seed ve aynı girdinin her platformda (net10.0, netstandard2.1/Unity,
Blazor WebAssembly) bit düzeyinde aynı durumu üretmesine dayanır. `double`/`float` işlemleri IEEE 754'e uysa da
platformlar arası bit düzeyinde aynı sonuç garanti edilmez: JIT'in FMA kullanımı, ara değerlerin genişletilmesi ve
kütüphane fonksiyonlarının (`Math.Pow` vb.) uygulamaları farklılaşabilir. Ölçek 10–50 derin NPC olduğu için
saf hız baskısı düşüktür.

## Karar
Simülasyon kodunda (`Yanki.Core`) sayısal değerler `decimal` olarak tutulur; `float` ve `double` kullanılmaz.
Hash'e giren her `decimal`, tek bir kanonik biçimle yazılır: `MidpointRounding.ToEven` ile 6 ondalık basamağa
yuvarlanır, `InvariantCulture` ile biçimlendirilir. Bir test `src/Yanki.Core` içinde `double`/`float` geçişini yasaklar.

## Değerlendirilen seçenekler

### Seçenek A: `double`
| Boyut | Değerlendirme |
|---|---|
| Karmaşıklık | Düşük |
| Maliyet | Platformlar arası hash tutarsızlığı riski; yuvarlama disiplini gerekir |
| Ölçeklenebilirlik | Yüksek |
| Bilinirlik | Çok yüksek |

**Artıları:** En hızlı, en bilinen.
**Eksileri:** Bit düzeyinde platform bağımsızlık garantisi yok; golden replay'in dayanağı zayıflar.

### Seçenek B: `decimal`
| Boyut | Değerlendirme |
|---|---|
| Karmaşıklık | Düşük |
| Maliyet | `double`'dan yavaş, ama 10–50 NPC ölçeğinde yeterli |
| Ölçeklenebilirlik | Orta |
| Bilinirlik | Yüksek |

**Artıları:** Ondalık gösterim tam, işlemleri yazılımsal ve deterministik, kanonik yazımı okunur.
**Eksileri:** Matematik fonksiyonları (üs, kök) yok; gerekirse elle yazılır. Bellek 16 bayt.

### Seçenek C: Sabit noktalı `int`/`long`
| Boyut | Değerlendirme |
|---|---|
| Karmaşıklık | Orta-yüksek |
| Maliyet | Ölçek yönetimi, taşma ve bölme dikkati |
| Ölçeklenebilirlik | Çok yüksek |
| Bilinirlik | Orta |

**Artıları:** Tam deterministik ve hızlı.
**Eksileri:** Her işlemde elle ölçekleme; okunabilirlik ve hata riski.

## Ödünleşimler
B, hız yerine deterministik ve okunur sayılar verir; bu proje için doğru takas.

## Gerekçe (Yusuf)

## Sonuçlar
- Kolaylaşan: Platformlar arası aynı hash; kanonik yazım; kriter değerlerinin okunabilirliği.
- Zorlaşan: Matematik fonksiyonları elle yazılır; `double` bekleyen dış API'lere sınırda çevrim gerekir.
- Yeniden bakılacak: Benchmark (11. hafta) `decimal`'i darboğaz gösterirse sabit noktalı `long`'a geçiş için yeni ADR.

## Yapılacaklar
1. [ ] `Yanki.Core` içinde `double`/`float` yasağını test ile denetle (2. hafta).
2. [ ] Kanonik `decimal` yazımını durum hash'inde uygula (2. hafta).
