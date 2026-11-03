# Not: Tick döngüsü ve dış API

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## Ne yapıyor?

`Simulasyon` motorun dış yüzüdür: `Submit(PlayerAction)` eylemi bekletir, `Tick(n)` n adım ilerletir, `Neden(olayId)` zinciri verir, `SonHash` durum hash'ini verir, `OlayYayinlandi` tick'in olaylarını yayınlar. `Simulasyon.Olustur(senaryo, kurallar, seed, eylemler: ..., secici: ...)` başlangıç dünyasını tick 0'da `KuralId: senaryo` olaylarıyla yazar. NPC karar sorguları: `KararIzi(npc)` (son tam iz), `SonKararOlayi(npc)`, `GecmisKararKisaIzi(olayId)`, `TamIziIste`/`YakalananIz`.

Bir tick'te çalışan adımlar (plan 6):

| Adım | Ne olur                                                                                                                                                                                                                                                             |
| ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | Bekleyen oyuncu eylemleri `Submit` sırasıyla olay olur (`KuralId: player`). Oyuncu anlatımı (`anlatti`) da burada olay olur; `duydu` kanıtı adım 2'de, algıdan sonra yazılır. Oyuncu hareketi burada işlenir: `ayrildi`, `geldi` ve oyuncunun `konum!` gerçeği hemen yazılır; aynı tick'te hareketten sonra gelen eylem varış mekanında olur. NPC konumları değişmez. |
| 2    | Algı: zihin olayı olmayan olaylar için kanıt + hafıza üretilir. Önceki tick'in adım 6'sında algılanmış NPC olayları atlanır.                                                                                                                                        |
| 3    | Sonuç kuralları çalışır; çıktılar yazılır ve sıradaki tick'e devredilir.                                                                                                                                                                                            |
| 4    | Sönüm: ADR-008 gereği yazılacak durum yok, değerler sorgu anında hesaplanır. **Kriz tespiti** (6. hafta): sönmüş güncel `stres ≥ eşik` ise o NPC için en üst uygun seviyede kriz satırı seçilir; krizdeki NPC bu tick eylem seçmez. Kriz için tick başına olay yazılmaz.                                                                                                                                                                                        |
| 5    | NPC karar adımı: her NPC için (kimlik sırasıyla) aday üretimi, puanlama, `IActionSelector`. **Hiçbir şey yazılmadan önce** bütün kararlar adım başındaki aynı durumdan hesaplanır.                                                                                  |
| 6.1  | Kriz olayları (`KuralId: kriz:<id>`), sonra `karar_verildi` + eylem olayı (hareket eyleminde `ayrildi` + `geldi`; konuşma eyleminde seçilen konu sayısı kadar `anlatti`) NPC kimlik sırasıyla yazılır.                                                                                                                                                                  |
| 6.2  | Bu dış olayların algısı yazılır; dünya hâlâ adım 5 başındaki konum görüntüsünü taşır.                                                                                                                                                                               |
| 6.3  | Eylem etkileri, konuşmada dinleyene `duydu` kanıtı + konuşana aktarım kaydı, çelişen eylem için stres artışı, kriz etkileri ve stres düşüşü, en son `konum!` değişimleri yazılır.                                                                                                                                                                                                              |
| 7    | Durum hash'i alınır, tick'in olayları yayınlanır.                                                                                                                                                                                                                   |

## Neden böyle?

- **Durum, günlüğün türevi.** Her olay eklenir eklenmez dünyaya ve zihne uygulanır. Durumu yeniden kurmak için günlüğü baştan uygulamak yeter.
- **Oturum = seed + senaryo + eylem günlüğü.** `OturumKaydi` bunları ve son hash'i JSON'a yazar. Oyuncu hareketi (`hareket`) de eylem günlüğüne girer; NPC kararları ve karar izleri dosyaya yazılmaz, replay'de yeniden hesaplanır. Golden replay ve 7. haftadaki kayıt/yükleme aynı biçimi kullanır.
- **Kaskad v0.** Kural çıktıları ve eylem etkileri aynı tick'te yeniden kural tetiklemez, sıradaki tick'e kalır. NPC eylem/hareket olayları da sonraki tick'te kurallara girer. Zincir sınırı 7. haftada.
- **Tetik kuyruğu `Olay.Id` sırasıyla kurulur.** Sıradaki tick'in kuyruğu: önceki tick'in kural çıktıları ve eylem etkileri + saklanan NPC olayları, kimliğe göre artan; sonra bu tick'in oyuncu olayları. Eylem etkileri (6.3), eylem olaylarından büyük kimlik alır; "devredilenler sonra, saklılar önce" gibi bir sıra yanlış olurdu.
- **Saklı algılayanlar.** Adım 6.2'nin ürettiği algı sonucundan türetilir (ayrı bir bilgi kaynağı yok), yalnızca sıradaki tick'in kural değerlendirmesinde kullanılıp atılır. Kurallar `Algilayanlar`'ı o anki konumdan yeniden hesaplamaz: aynı tick'te mekandan ayrılan tanık kural çıktısını alır, aynı tick'te gelen ve olayı görmeyen almaz.
- **Sıra sabit:** NPC kimliği (ordinal) → tetikleyici kimliği → kural kimliği → algılayan NPC. Zar `AnahtarliRng.Icin(Seed, tick, npcId, "karar")` ile her NPC için bağımsızdır.
- **Eylemler tick sınırında işlenir.** `Tick(1); Tick(4)` ile `Tick(5)` aynı durumu ve aynı günlüğü verir, testle korunuyor.
- **`konum!` olayları algılanmaz ve kural tetiklemez;** hareketin dış görünümü `ayrildi`/`geldi` olaylarıdır.

## 6. hafta: konuşma, stres ve kriz

- **Konuşma adım 5'te karar, 6'da yazım.** Konular ve aktarım güçleri bütün NPC'ler için aynı durum görüntüsünden hesaplanır (aday aşamasında bir kez); bir NPC'nin bu tick'te duyduğu, başka NPC'nin bu tick'teki kararını etkilemez (testle). `duydu` kanıtı 6.3'te yazılır, dinleyen onu bir sonraki tick'te kullanır.
- **Kriz de bu hattan geçer**, ayrı kuyruk yoktur: adım 4'te seçilir, 6.1'de olay, 6.2'de algı (adım 5 başı konum görüntüsü), 6.3'te etkiler ve stres düşüşü, sonuç kuralları sıradaki tick'te. Krizdeki NPC `karar_verildi` yazmaz.
- **Zihin olayları kural tetiklemez:** `aktarim_kaydedildi`, `stres_degisti`, `kanit_eklendi` kuyruğa girmez; `anlatti` ve kriz olayları dış olaydır ve sıradaki tick'te kurallara girer (`olay_konusu` ile konu bazında).
- Aynı tick'te iki NPC aynı dinleyene aynı kökü yazarsa sonuç NPC kimlik sırasına bağlıdır, girdi sırasına değil (testle).

## Yusuf'un kararları

- **Karar (Yusuf):** 1 tick = 1 oyun saati (`zihin.md`). Çekirdek bunu varsaymaz, yalnızca sönme oranları buna göre seçildi.
- **Karar (Yusuf, 5. hafta):** Adım 6 sırası (olaylar → algı → etkiler ve `konum!`), eş zamanlı algı, hareketin iki dış olay üretmesi, oyuncu hareketinin `Submit` anında komşuluk denetimi ve adım 1'de hemen işlenmesi, tick başına tek hareket. Ayrıntı: ADR-011.
- Oyuncu hareketi 4. haftadan 5. haftaya kalmıştı; tamamlandı.
