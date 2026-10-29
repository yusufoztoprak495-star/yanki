# ADR-0002: NPC kararları için utility AI ve takılabilir `IActionSelector`

**Durum:** Önerildi
**Tarih:** 2026-10-29

## Bağlam
NPC'ler doğal davranmalı ama kişiliklerine tutarlı kalmalı; aynı seed ile aynı kararı vermeli; her kararın gerekçesi
okunabilmeli. LLM ile karar vermek popüler ama determinizmi, maliyeti ve açıklanabilirliği bozuyor.
GOAP ve davranış ağaçları için olgun kütüphaneler zaten var; onlarla yarışmak projenin amacı değil.

## Karar
- Varsayılan karar verici **utility AI**: eylemler JSON'da tanımlanır, her eylem bir dizi eğri ve çarpanla puanlanır.
- NPC her zaman en yüksek puanlı eylemi değil, **en iyi N (varsayılan 3) eylem arasından ağırlıklı** seçim yapar
  (The Sims). Seçim anahtarlı RNG ile yapılır, determinizm korunur.
- Kişiliğe aykırı eylemler yasaklanmaz, **stres** olarak fiyatlandırılır; stres eşiği aşınca kişiliğe uygun kriz
  olayı doğar (CK3, Dwarf Fortress).
- Karar verici bir arayüzün arkasındadır: `IActionSelector`. `UtilitySelector` varsayılan uygulamadır;
  ileride GOAP/BT adaptörleri bu arayüze takılabilir.
- Her karar bir **karar izi** üretir (aday eylemler, puan bileşenleri, seçilen). `yanki why` bunu okur.
- **LLM karar vermez.** LLM yalnızca diyalog metni için opsiyonel bir eklenti olabilir (bkz. ADR-0003, `IDialogueRenderer`).

## Değerlendirilen seçenekler

### Seçenek A: Utility AI + `IActionSelector`
| Boyut | Değerlendirme |
|---|---|
| Karmaşıklık | Orta |
| Maliyet | Eğri ayarı (denge) zaman alır |
| Ölçeklenebilirlik | 50 NPC × onlarca eylem rahat |
| Bilinirlik | Oyun AI'sında yaygın (The Sims, Infinite Axis) |

**Artıları:** Deterministik, açıklanabilir, kişilik doğrudan çarpanlara bağlanır.
**Eksileri:** Kötü ayarlanmış eğriler tekdüze ya da kaotik davranış üretir; kapsama aracı şart.

### Seçenek B: LLM ile karar
**Artıları:** Çok doğal görünen davranış, az içerik yazımı.
**Eksileri:** Deterministik değil, test edilemez, çevrimdışı ve Unity'de pahalı, "neden?" sorusu cevapsız.

### Seçenek C: Davranış ağacı / GOAP
**Artıları:** Olgun, iyi bilinen.
**Eksileri:** Sosyal ve duygusal durumları tartmakta zayıf; mevcut kütüphanelerle yarışmak anlamsız.

## Ödünleşimler
A projenin üç iddiasını (tutarlılık, determinizm, açıklanabilirlik) aynı anda karşılayan tek seçenek. C'yi dışlamak
yerine arayüz arkasına alıyoruz.

## Gerekçe (Yusuf)

## Sonuçlar
- Kolaylaşan: Karar izi, sıra etkisi testleri, kişilik tutarlılığı.
- Zorlaşan: Eğri ve çarpan dengesi; içerik yazım yükü.
- Yeniden bakılacak: "Eylemi simüle et, sonucu puanla" (Versu tarzı, derinlik 1) v0.2 sonrası.

## Yapılacaklar
1. [ ] 5. hafta: `IActionSelector`, `UtilitySelector`, top-N seçim, karar izi.
2. [ ] 6. hafta: stres ve kriz olayları.
