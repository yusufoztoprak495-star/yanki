# ADR-0001: Olay kaynaklı (event-sourced) çekirdek

**Durum:** Önerildi
**Tarih:** 2026-10-29

## Bağlam
Yankı'nın ana iddiası "her karar yankılanır ve motor nedenini gösterebilir". Bunun için üç şey gerekiyor:
bir sonucun hangi olaydan ve hangi kuraldan doğduğunu izlemek, aynı oyunu birebir yeniden oynatmak (hata ayıklama,
golden test, tek seçim farkı raporu) ve tick'e geri sarmak (tarayıcı demosu). Ölçek 10–50 derin NPC olduğu için
performans baskısı düşük, açıklanabilirlik baskısı yüksek.

## Karar
Dünya durumu, sıralı ve değişmez bir **olay günlüğünden** türetilir. Her olay şunları taşır:
`Tick`, sıra numarası, tip, yük, `NedenOlaylar` (bu olayı doğuran olayların kimlikleri) ve varsa `KuralId`.
Durum = günlüğün baştan uygulanması. Günlük biçiminin bir sürüm numarası vardır (`YankiInfo.EventLogFormatVersion`).

## Değerlendirilen seçenekler

### Seçenek A: Olay kaynaklı çekirdek
| Boyut | Değerlendirme |
|---|---|
| Karmaşıklık | Orta |
| Maliyet | Uzun oturumlarda günlük büyür; snapshot v0.2'ye ertelendi |
| Ölçeklenebilirlik | 10–50 NPC için yeterli |
| Bilinirlik | Yusuf için yeni; mülakatta güçlü bir konu |

**Artıları:** Nedensellik zinciri bedava gelir; replay, geri sarma ve golden test doğal; kayıt/yükleme = günlüğü saklamak.
**Eksileri:** Her durum değişikliği bir olay tipi ister; yanlış tasarlanan olay tipi sonradan pahalıya patlar.

### Seçenek B: Değiştirilebilir durum + ayrı log
| Boyut | Değerlendirme |
|---|---|
| Karmaşıklık | Düşük (başta) |
| Maliyet | Log ile durum zamanla ayrışır |
| Ölçeklenebilirlik | Yüksek |
| Bilinirlik | Yüksek |

**Artıları:** Başlaması kolay, alışılmış.
**Eksileri:** "Neden?" sorusu ancak log disiplinle tutulursa cevaplanır; replay garanti değil.

## Ödünleşimler
B ilk iki haftayı hızlandırır ama projenin farkını (kanıtlanabilir nedensellik, deterministik replay) zayıflatır.
A biraz daha fazla tören getirir; ölçek küçük olduğu için performans maliyeti önemsiz.

## Gerekçe (Yusuf)

## Sonuçlar
- Kolaylaşan: `yanki why`, `yanki diff`, golden replay testleri, demodaki geri sarma.
- Zorlaşan: Olay tiplerini baştan dikkatli tasarlamak; günlük biçimi değişince sürüm yükseltmek.
- Yeniden bakılacak: Uzun oturumlar için snapshot (v0.2 sonrası).

## Yapılacaklar
1. [ ] 2. hafta: temel olay tipleri, `NedenOlaylar`, `KuralId`, durum hash'i.
2. [ ] 4. hafta: ilk golden replay testi CI'da.
