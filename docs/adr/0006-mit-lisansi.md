# ADR-0006: MIT lisansı

**Durum:** Kabul edildi
**Tarih:** 2026-10-29
**Karar veren:** Yusuf

## Bağlam
Proje portfolyo amaçlı ve "Love/Hate'in açık, motordan bağımsız alternatifi" olarak konumlanıyor. Stüdyoların
(ticari oyunlar dahil) rahatça deneyebilmesi isteniyor.

## Karar
Kod **MIT** lisansıyla yayınlanır. Örnek içerik (Kasaba Hanı) de aynı lisansla gelir.

## Değerlendirilen seçenekler

| Lisans | Artı | Eksi |
|---|---|---|
| **MIT** | En yaygın, Unity/NuGet ekosisteminde sürtünmesiz, ticari kullanıma açık | Türev çalışmaların açık kalmasını şart koşmaz |
| Apache-2.0 | Patent maddesi | Daha uzun, küçük proje için fazla |
| GPL/LGPL | Türevler açık kalır | Oyun stüdyoları genelde uzak durur; Unity'de LGPL pratikte zor |

## Ödünleşim analizi
Amaç benimsenme ve görünürlük; korumacı lisans bunun önüne geçer.

## Sonuçlar
- Kolaylaşan: NuGet/OpenUPM yayını, stüdyoların denemesi.
- Zorlaşan: Yok denecek kadar az.

## Yapılacaklar
1. [x] `LICENSE` dosyası ve `PackageLicenseExpression`.
