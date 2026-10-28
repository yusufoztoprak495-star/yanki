# ADR-0005: Tarayıcı demosu ve hata ayıklama ekranı için Blazor WebAssembly

**Durum:** Kabul edildi
**Tarih:** 2026-10-29
**Karar veren:** Yusuf

## Bağlam
İşe alımcılar bir repoya ortalama 30 saniye bakıyor. "Oyunu olmayan motor" yarım iş gibi algılanıyor. Linke tıklayınca
kurulumsuz açılan bir demo en büyük farkı yaratır. Demo aynı zamanda motorun içini göstermeli: NPC zihni, "neden?"
zinciri, tick'e geri sarma.

## Karar
Demo ve hata ayıklama ekranı **tek bir Blazor WebAssembly uygulaması** olarak yazılır ve GitHub Pages'te yayınlanır
(10. hafta). Motor tarayıcıda doğrudan çalışır; sunucu yok.

## Değerlendirilen seçenekler

### Seçenek A: Blazor WebAssembly
| Boyut | Değerlendirme |
|---|---|
| Karmaşıklık | Orta |
| Maliyet | Ücretsiz barındırma (GitHub Pages) |
| Ölçeklenebilirlik | İstemci tarafı, sınırsız |
| Bilinirlik | Saf C#, ek dil yok |

**Artıları:** Motorla aynı dil; demo ve debugger tek uygulama; kurulum yok.
**Eksileri:** İlk yükleme birkaç MB; oyun hissi için görsel cila sınırlı.

### Seçenek B: Unity WebGL
**Artıları:** Gerçek oyun motoru, Unity sinyali.
**Eksileri:** Unity öğrenme yükü 12 haftaya sığmıyor; debugger ayrı yazılmalı.

### Seçenek C: Godot C#
**Eksileri:** Godot 4 C# projeleri web'e export edilemiyor.

## Ödünleşim analizi
A, 12 haftalık takvimde motoru göstermenin en ucuz yolu. Unity örneği v0.2 sonrasına ertelendi.

## Sonuçlar
- Kolaylaşan: Repodaki README'den tek tıkla demo; mülakatta canlı gösterim.
- Zorlaşan: Çekirdeğin WASM'da çalışması (çoklu iş parçacığı yok, dosya sistemi yok) — ADR-0004 ile zaten uyumlu.
- Yeniden bakılacak: Küçük Unity örnek projesi (v0.2 sonrası).

## Yapılacaklar
1. [ ] 10. hafta: `Yanki.Demo` projesi, Pages yayını.
