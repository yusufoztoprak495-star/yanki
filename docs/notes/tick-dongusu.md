# Not: Tick döngüsü v0 ve dış API

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## Ne yapıyor?

`Simulasyon` motorun dış yüzüdür: `Submit(PlayerAction)` eylemi bekletir, `Tick(n)` n adım ilerletir, `Neden(olayId)` zinciri verir, `SonHash` durum hash'ini verir, `OlayYayinlandi` tick'in olaylarını yayınlar. `Simulasyon.Olustur(senaryo, kurallar, seed)` başlangıç dünyasını tick 0'da `KuralId: senaryo` olaylarıyla yazar.

v0'da çalışan adımlar:

| Adım | Ne olur |
| --- | --- |
| 1 | Bekleyen oyuncu eylemleri olay olur (`KuralId: player`). |
| 2 | Algı: zihin olayı olmayan olaylar için kanıt + hafıza üretilir. |
| 3 | Sonuç kuralları çalışır; çıktılar yazılır ve sıradaki tick'e devredilir. |
| 4 | Sönüm: ADR-008 gereği yazılacak durum yok, değerler sorgu anında hesaplanır. |
| 7 | Durum hash'i alınır, tick'in olayları yayınlanır. |

5. adım (karar) ve 6. adım (kararların olaya dönüşmesi) 5. haftada `TekTick` içinde işaretli yerlere takılacak.

## Neden böyle?

- **Durum, günlüğün türevi.** Her olay eklenir eklenmez dünyaya ve zihne uygulanır. Durumu yeniden kurmak için günlüğü baştan uygulamak yeter.
- **Oturum = seed + senaryo + eylem günlüğü.** `OturumKaydi` bunları ve son hash'i JSON'a yazar. Golden replay ve 7. haftadaki kayıt/yükleme aynı biçimi kullanır.
- **Kaskad v0.** Kural çıktıları aynı tick'te yeniden kural tetiklemez, sıradaki tick'e kalır. Zincir sınırı henüz yok, yalnızca `Tick(n)` sonlandırır.
- **Sıra sabit:** tetikleyici kimliği → kural kimliği → NPC. Seed henüz kullanılmıyor, karar adımı `AnahtarliRng.Icin(Seed, tick, ...)` ile kullanacak.
- **Eylemler tick sınırında işlenir.** `Tick(1); Tick(4)` ile tek seferde ilerletmek aynı durumu verir, testle korunuyor.

## Yusuf'un kararları

- **Karar (Yusuf):** Oyuncu hareketi 5. haftaya kaldı. v0'da oyuncunun mekanı senaryodan gelir ve değişmez.
- **Karar (Yusuf):** 1 tick = 1 oyun saati (`zihin.md`). Çekirdek bunu varsaymaz, yalnızca sönme oranları buna göre seçildi.
