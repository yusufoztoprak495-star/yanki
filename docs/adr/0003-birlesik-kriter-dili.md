# ADR-0003: Sonuç kuralları ve diyalog için tek kriter dili

**Durum:** Önerildi
**Tarih:** 2026-10-29
**Kabul eden:** —
**Kabul tarihi:** —

## Bağlam
İki yerde "şu koşullar sağlanıyorsa" demek gerekiyor: sonuç kuralları (oyuncu hancıya yalan söyledi ve hancı bunu
öğrendi → güven düşer) ve diyalog şablonu seçimi (NPC kızgın ve oyuncuyu tanıyor → şu satırı söyle). İki ayrı dil
iki ayrıştırıcı, iki doğrulayıcı ve iki kapsama aracı demek. Benzer sistemlerin hepsinde en büyük maliyet içerik yazımı.

## Karar
Valve'ın Left 4 Dead / Dynamic Dialog sisteminden esinlenen **ağırlıklı kriter dili**:
- Bir kural ya da şablon = kriter listesi + (kural için) etkiler / (şablon için) metin.
- Kriter = `sorgu karşılaştırma değer` (ör. `iliski(oyuncu).guven < -20`, `biliyor(yalan_17)`).
- Birden fazla kural/şablon eşleşirse **en çok kriteri sağlayan** (en özgül) kazanır; eşitlikte anahtarlı RNG.
- Sorgu fonksiyonları sabit ve küçük bir sözlükten gelir (ilk liste ~15 fonksiyon, Yusuf yazar).
- Dosya biçimi JSON; tek ayrıştırıcı, tek doğrulayıcı (`yanki validate`), tek kapsama aracı (`yanki coverage`).

## Değerlendirilen seçenekler

### Seçenek A: Tek kriter dili (Valve tarzı)
| Boyut | Değerlendirme |
|---|---|
| Karmaşıklık | Orta |
| Maliyet | Tek uygulama |
| Ölçeklenebilirlik | Binlerce kuralda bile basit eşleme yeterli |
| Bilinirlik | GDC konuşmalarıyla iyi belgelenmiş |

**Artıları:** Tek araç zinciri; "hiç tetiklenmeyen kural" tespiti iki taraf için birden çalışır.
**Eksileri:** Çok karmaşık mantık (döngüler, değişken atama) ifade edilemez — bilerek.

### Seçenek B: Gömülü betik dili (Lua, C# script)
**Artıları:** Sınırsız ifade gücü.
**Eksileri:** Doğrulanamaz, kapsaması ölçülemez, determinizm garanti edilemez, Unity/WASM'da ek yük.

### Seçenek C: Kurallar ve diyalog için ayrı DSL'ler
**Artıları:** Her biri kendi işine özel.
**Eksileri:** İki kat iş, iki kat hata.

## Ödünleşimler
Kısıtlı bir dil, içerik yazımını doğrulanabilir ve ölçülebilir kılıyor; bu, "seçimler gerçekten önemli mi?"
sorusunu kanıtlamanın ön şartı.

## Gerekçe (Yusuf)

## Sonuçlar
- Kolaylaşan: Doğrulama, kapsama raporu, karar izinde "hangi kriterler sağlandı" gösterimi.
- Zorlaşan: Dilin dışında kalan nadir durumlar için C# eklenti noktası gerekecek.
- Yeniden bakılacak: Yarn Spinner köprüsü (v0.2 sonrası, ayrı paket).

## Yapılacaklar
1. [ ] 1. hafta (Yusuf): sorgu fonksiyonu sözlüğünün ilk listesi — `docs/yusuf/hafta-01.md`.
2. [ ] 2. hafta: kriter ayrıştırıcı ve puanlayıcı.
3. [ ] 4. hafta: kural motoru ve doğrulayıcı v0.
