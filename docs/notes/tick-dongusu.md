# Not: Tick döngüsü v0 ve dış API

> **Taslak (Claude Code).** Yusuf bunu kendi cümleleriyle yeniden yazacak. Aşağıdakiler "ne yaptım, neden" iskeletidir.

## Bu modül ne yapıyor?

`Simulasyon` (plan 5.8): `Submit(PlayerAction)` oyuncu eylemini bekletir, `Tick(n)` n adım ilerletir, `Neden(olayId)` nedensellik zincirini verir,
`DurumHash()` / `SonHash` durum hash'ini, `OlayYayinlandi` her tick'in olaylarını dışarı yayınlar. Durum sorguları `Dunya`, `Zihinler`, `Gunluk` üzerinden yapılır.
`Simulasyon.Olustur(senaryo, kurallar, seed)` başlangıç dünyasını tick 0'da `KuralId: senaryo` olaylarıyla yazar (varlıklar, mekan bağlantıları, `konum!` ve başlangıç gerçekleri).

Bir tick (plan 6) — **bu hafta yalnızca şu adımlar**:

| Adım | Ne olur                                                                                                                      |
| ---- | ---------------------------------------------------------------------------------------------------------------------------- |
| 1    | Bekleyen oyuncu eylemleri olay olur (`KuralId: player`, fail = oyuncu, mekan = oyuncunun `konum!`'u).                        |
| 2    | Algı: zihin olayı olmayan olaylar için `DogrudanAlgi.Uret` (kanıt + hafıza, `KuralId: algi`).                                |
| 3    | Sonuç kuralları (`KuralMotoru`); çıktı olaylar günlüğe yazılır ve **sıradaki tick'e devredilir**.                            |
| 4    | Sönüm ve süre dolumu: ADR-008 gereği süreli değerler sorgu anında hesaplanır, yazılacak durum yoktur. Stres krizi üretilmez. |
| 7    | Durum hash'i alınır (`SonHash`), tick'in olayları sırayla `OlayYayinlandi`'ya bildirilir.                                    |

5. (karar/eylem seçimi) ve 6. (seçilen eylemlerin olaya dönüşüp geri beslenmesi) adımlar yazılmadı; `TekTick` içinde yerleri yorumla işaretli. Devredilen olay kuyruğu (`_devredilenler`) 6. adımın geri besleme noktası olarak kullanılabilir.

## Neden böyle?

- **Durum = olay günlüğünün türevi.** Her olay eklenir eklenmez dünyaya/zihne uygulanır (`Simulasyon.Ekle`), yani durumu yeniden kurmak için günlüğü baştan uygulamak yeter (7. hafta kayıt/yüklemesinin temeli).
- **Oturum = seed + senaryo + eylem günlüğü.** `OturumKaydi` bu üçünü ve son hash'i JSON'a yazar; `OturumOynatici` her tick için o tick'in eylemlerini gönderip tick'i ilerletir. Golden replay ve (7. haftada) kayıt/yükleme aynı biçimi kullanır.
- **Kaskad (v0):** kuralların ürettiği olaylar aynı tick'te yeniden kural tetiklemez, bir sonraki tick'in başında algı ve kurallardan geçer. Zincir derinliği sınırı yoktur (7. hafta); yalnızca `Tick(n)` sonlandırır.
- **Deterministik:** sıra sabit (tetikleyici Id'si → kural kimliği ordinal → NPC ordinal). Seed bu hafta kullanılmıyor (RNG çekmeyen bir akış); karar adımı (5. hafta) `AnahtarliRng.Icin(Seed, tick, ...)` ile kullanacak.

## 1 tick = 1 saat

`docs/notes/zihin.md` madde 5'te **Karar (Yusuf)** olarak kayıtlı (oranlar 1 tick = 1 saat varsayımıyla seçildi); ADR-008 "yeniden bakılacak" listesinde de var. Çekirdek bunu varsaymaz, yalnızca sönme oranları böyle seçilmiştir. Yeni bir karar gerekmedi.

## Açık noktalar

1. **Oyuncu hareketi yok.** Oyuncunun mekanı senaryodan gelir, v0'da değişmez (konum değişimi oyuncu eylemi değil, gelecekteki bir dünya olayı).
2. **Eylemler tick sınırında işlenir:** `Submit` ile `Tick` arasında beklerler; `Tick(n)` ilk adımda hepsini işler.
3. **`Tick(n)` ortasında eylem gönderilemez.** Parçalı ilerletme (`Tick(1); Tick(4)`) ile tek seferde ilerletme aynı durumu verir (testle korunuyor).
