# Simulation

Simülasyon: tick döngüsü ve dış API (`Submit`, `Tick`, sorgular, `Neden`) ile oturum kaydı/oynatma (5.8, 6).

**Durum:** 5. hafta teslimi tamam (tick adımları 1–7: NPC kararı ve olaya dönüşme, oyuncu ve NPC hareketi, eş zamanlı algı). Tam kayıt/yükleme 7. haftada.

## Tipler

| Tip | Ne yapar |
|---|---|
| `Simulasyon` | `Olustur(senaryo, kurallar, seed, eylemler: ..., secici: ...)`, `Submit(PlayerAction)` (oyuncu hareketi: `hareket`), `Tick(n)`, `Neden(olayId)`, `DurumHash()`, `SonHash`, `OlayYayinlandi`; sorgular için `Dunya`, `Zihinler`, `Gunluk`, `EylemGunlugu`, `Eylemler`; karar için `KararIzi(npc)`, `SonKararOlayi(npc)`, `GecmisKararKisaIzi(olayId)`, `TamIziIste`/`YakalananIz`/`TutulanTamIzSayisi`. |
| `SenaryoTanimi`, `SenaryoYukleyici` | Başlangıç dünyası (mekanlar, oyuncu, NPC'ler, gerçekler, oyuncu eylem türleri, kural dosyası adları) ve JSON okuyucusu/doğrulayıcısı. |
| `PlayerAction`, `EylemKaydi` | Oyuncu eylemi (tür, hedef, yük) ve işlendiği tick. |
| `OturumKaydi`, `OturumOynatici` | seed + senaryo + eylem günlüğü + son hash (JSON); baştan oynatıp hash'i doğrular (`TickeKadarOynat` bir tick'e kadar). NPC kararları ve izler dosyaya yazılmaz. Golden replay ve 7. hafta kayıt/yükleme aynı biçimi kullanır. |
| `NedenYazici` | Nedensellik zincirini okunur satırlara çevirir (`yanki why`); zincirdeki karar olaylarının altına karar izini ekler. |
| `AlgiHaritasi` (iç) | Adım 6.2'nin algı sonucu: olay → algılayan NPC → algı olayları; sıradaki tick'in kural değerlendirmesi için bir tick saklanır. |

Not: [`docs/notes/tick-dongusu.md`](../../../docs/notes/tick-dongusu.md).
