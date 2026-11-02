# Devlog #1 (taslak iskeleti)

> **Yalnızca başlıklar ve teknik gerçekler.** Yazıyı Yusuf yazar. Aşağıdakiler kullanabileceği doğrulanmış olgulardır (kod/commit'lerden).

## Başlık önerileri (Yusuf seçer/değiştirir)

- "Oyuncunun yaptığı şey NPC'nin kafasında nasıl yankılanıyor: ilk dikey dilim"
- "Her sonucun bir nedeni var: `why` komutu"
- "Bir hash'e güvenmek: golden replay"

## Bölümler (iskelet)

1. Hedef: oyuncu eylemi → algı → kural → fikir değişimi → `why` zinciri → CI'da golden replay.
2. Kural motoru: JSON kurallar, kriter dili, etkiler yeni olay olarak kayda düşer.
3. Neden bir JSON okuyucu yazdım (paket yok kararı, ADR-009).
4. `why` çıktısı örneği (gerçek bir koşudan yapıştırılacak).
5. Golden replay: aynı seed + aynı girdi = aynı hash; CI'ın yaptığı.
6. Bu hafta yazmadıklarım / sıradaki hafta.

## Teknik gerçekler

- Çekirdek: `Yanki.Core`, `netstandard2.1;net10.0`, LangVersion 9.0, NuGet bağımlılığı yok.
- Yeni modüller: `Json` (okuyucu/yazıcı), `Rules` (kural motoru, doğrulayıcı v0), `Simulation` (tick döngüsü, oturum kaydı, `why` yazıcısı); CLI: `run`, `query`, `why`, `replay`.
- Tick adımları (plan 6): 1 oyuncu eylemleri, 2 algı, 3 sonuç kuralları, 4 sönüm/süre dolumu (sorgu anında hesaplanır, ADR-008), 7 durum hash'i ve olay yayını. 5–6 sonraki haftalarda.
- Kural etkileri v0: modifier, eksen, ruh hali, hafıza, dünya gerçeği. Hepsi yeni olay, hepsi `KuralId` + `NedenOlaylar` taşır.
- Kaskad v0: kuralın ürettiği olay aynı tick'te yeniden kural tetiklemez, sıradaki tick'e kalır.
- Oturum dosyası = seed + senaryo + oyuncu eylem günlüğü + son hash (JSON). Hash bilerek değişirse `yanki replay <dosya> --guncelle`.
- 1 tick = 1 oyun saati (`docs/notes/zihin.md`, Karar (Yusuf)).
