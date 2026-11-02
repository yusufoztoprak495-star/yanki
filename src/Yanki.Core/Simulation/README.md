# Simulation

Simülasyon: tick döngüsü ve dış API (`Submit`, `Tick`, sorgular, `Neden`) ile oturum kaydı/oynatma (5.8, 6).

**Durum:** 4. hafta teslimi tamam (tick adımları 1, 2, 3, 4, 7). Karar ve eylem adımları (5, 6) 5.–6. haftada, tam kayıt/yükleme 7. haftada.

## Tipler

| Tip | Ne yapar |
|---|---|
| `Simulasyon` | `Olustur(senaryo, kurallar, seed)`, `Submit(PlayerAction)`, `Tick(n)`, `Neden(olayId)`, `DurumHash()`, `SonHash`, `OlayYayinlandi`; sorgular için `Dunya`, `Zihinler`, `Gunluk`, `EylemGunlugu`. |
| `SenaryoTanimi`, `SenaryoYukleyici` | Başlangıç dünyası (mekanlar, oyuncu, NPC'ler, gerçekler, oyuncu eylem türleri, kural dosyası adları) ve JSON okuyucusu/doğrulayıcısı. |
| `PlayerAction`, `EylemKaydi` | Oyuncu eylemi (tür, hedef, yük) ve işlendiği tick. |
| `OturumKaydi`, `OturumOynatici` | seed + senaryo + eylem günlüğü + son hash (JSON); baştan oynatıp hash'i doğrular. Golden replay ve 7. hafta kayıt/yükleme aynı biçimi kullanır. |
| `NedenYazici` | Nedensellik zincirini okunur satırlara çevirir (`yanki why`). |

Not: [`docs/notes/tick-dongusu.md`](../../../docs/notes/tick-dongusu.md).
