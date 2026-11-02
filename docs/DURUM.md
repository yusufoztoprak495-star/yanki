# Proje durumu

Tek bakışta: neredeyiz, ne bitti, Yusuf'tan ne bekleniyor. **Her PR'da güncellenir** (CLAUDE.md); PR bunsuz açılmaz.

## Son güncelleme

- **Tarih:** 2026-11-02
- **PR:** [PR #8](https://github.com/yusufoztoprak495-star/yanki/pull/8) (ADR gerekçeleri, modül notları ve Devlog #1, Yusuf'un incelemesini bekliyor)

## Biten haftalar

### 1. hafta: kurulum ve iskelet ([PR #1](https://github.com/yusufoztoprak495-star/yanki/pull/1), [PR #2](https://github.com/yusufoztoprak495-star/yanki/pull/2))
- .NET çözümü (`netstandard2.1;net10.0`), CI ve Codecov adımı.
- README iskeleti, `CLAUDE.md`, `docs/ai-usage.md`.
- ADR-001..006.
- Kriter dili sözlüğü v0.1 (15 fonksiyon).

### 2. hafta: olay kaydı, dünya, kriter dili ([PR #3](https://github.com/yusufoztoprak495-star/yanki/pull/3))
- Olay kaydı (`NedenOlaylar`, `KuralId`) ve dünya (gerçekler, mekan grafiği).
- Anahtarlı RNG ve dünya durum hash'i.
- Kriter ayrıştırıcı, doğrulayıcı ve seçici (eşitlikte özgül olan kazanır, sonra RNG).
- ADR-007 (`decimal`), `tools/golden_ref.py`, 318 test.

### 3. hafta: zihin ve doğrudan algı ([PR #4](https://github.com/yusufoztoprak495-star/yanki/pull/4))
- Zihin: kişilik, 3 eksen + süreli fikir modifier'ları (`FikirDokumu`), tek duygulu ruh hali, stres, kanıtlı inançlar (çelişki kuralı, `KimdenDuydu`), hafıza.
- Saf sönme fonksiyonları (`Sonumleme`, `ZihinAyarlari`) ve altı zihin olayı; zihin durum hash'e giriyor, dünya altın hash'i değişmedi.
- Doğrudan algı: `acik`/`sessiz`/`gizli`, `gordu` kanıtı + hafıza olayı (`KuralId: algi`).
- `ZihinSorguCozucusu`: kriter dili sözlüğünün 13/15 fonksiyonu gerçek veriye bağlı (`yalan_soyledi` 6., `yay` 7. hafta).
- ADR-008, `docs/notes/zihin.md`, `docs/notes/algi.md`; 508 test (iki hedefte derleniyor, 0 uyarı).

### 4. hafta: sonuç kuralları ve ince dikey dilim ([PR #7](https://github.com/yusufoztoprak495-star/yanki/pull/7))
- **JSON okuyucu/yazıcı** (`Yanki.Core.Json`, bağımlılıksız, sayılar `decimal`, satır/sütun bilgili hata); ADR-009. `KriterOkuyucu` kriterleri JSON'dan okur (ikinci kriter yolu yok).
- **Kural motoru** (`Rules`): `ne_zaman` + kriterler (hepsi tutmalı, ADR-010) → etkiler; v0 etkileri modifier, eksen, ruh hali, hafıza, dünya gerçeği; etkiler yeni olay (`KuralId` + `NedenOlaylar`). Doğrulayıcı v0 `dosya:satır: kural "id": mesaj` verir, `agirlik` için uyarır. Sözlüğe `olay_rolu` eklendi (16 fonksiyon).
- **Tick döngüsü v0 ve dış API** (`Simulation`): `Submit(PlayerAction)`, `Tick(n)`, sorgular, `Neden(olayId)`; adımlar 1, 2, 3, 4, 7. Kaskad v0: kural çıktıları sıradaki tick'e kalır.
- **CLI:** `yanki run | query | why | replay` (`replay --guncelle` golden hash'i tek komutla günceller).
- **Mini senaryo `samples/MiniHan`** (Yusuf'un seçimi: seçenek A + hedef/tanık düzeltmesi) ve **golden replay #1** (`tests/golden/mini-han-1.json`); CI her push'ta tekrar oynatır.
- Uçtan uca test: oyuncu eylemi fikri değiştirir, `why` zinciri modifier'dan oyuncu eylemine iner. Notlar: `docs/notes/kural-motoru.md`, `docs/notes/tick-dongusu.md`; Devlog #1 `docs/devlog/01-ilk-dikey-dilim.md`.
- Testler: 649 (Core) + 24 (senaryo/CLI/golden) = 673; iki hedef derleniyor, 0 uyarı. Golden hash değişti (hafıza tekilleştirme): `504f3142d1dc15dd` → `cee9956ef06df17f`.

## Ertelenenler (Yusuf'un işi)

- [ ] **ADR-001..010 "Gerekçe (Yusuf)" onayı:** gerekçeleri Yusuf'un isteğiyle Claude Code yazdı. Yusuf onaylayınca durumlar "Kabul edildi" yapılır.
- [x] **`docs/notes/` notları:** yedisi Yusuf'un isteğiyle Claude Code tarafından yeniden yazıldı. Açık kalan ve onaylanmamış kararlar notlarda ayrı bölümde.
- [x] **Devlog #1:** `docs/devlog/01-ilk-dikey-dilim.md` (Claude Code yazdı, çıktılar golden oturumdan).
- [x] **`CODECOV_TOKEN` secret'ı:** GitHub repo ayarlarına eklendi.
- [x] **Mini senaryo (3 NPC):** `samples/MiniHan` (4. hafta). Kişilik özelliklerinden `cesaret` dışındakiler 0.5 yer tutucu; 11. hafta denge turunda ayarlanacak.
- [ ] **Kasaba Hanı NPC'leri:** 9. haftada 8.

## Açık kararlar (Yusuf'u bekleyen)

- 3. haftanın büyük kararları PR #4 incelemesinde verildi (eksen ağırlıkları, sönme oranları, hafıza eşikleri, birikme, çelişki eşitliği, duygu eşitliği, algı değerleri); notlarda **Karar (Yusuf)** olarak işli.
- İkinci inceleme turunda kalan küçük noktalar da karara bağlandı ve koda işlendi: `inanc(degil(...))`, tekrar eden kanıtın tazelenmesi, failın her zaman algılaması,
  `FikirDokumu` kırpma satırı; `gordu` kaynağı ve bilinmeyen görünürlük hatası olduğu gibi kaldı.
- `oyuncu` artık ayrı bir varlık türü (`VarlikTuru.Oyuncu`), eşya değil.
- 3. hafta için Yusuf'u bekleyen açık karar kalmadı (yalnızca ADR-008 "Gerekçe (Yusuf)" yazımı).

**4. hafta: karara bağlananlar (Yusuf, PR #7 incelemesi):**
- [x] **Hedef/tanık ayrımı:** sözlüğe `olay_rolu(fail|hedef|tanik)` eklendi (16. fonksiyon); `inanc(...)` yöntemi kaldırıldı. Regresyon: daha önce tehdit edilmiş NPC, başkasına yapılan tehdidi görünce tanıktır.
- [x] **Kural tetiklenmesi:** kural yalnızca tüm kriterleri tutunca çalışır; kriteri olmayan kural her tetikleyici olayda çalışır; `agirlik` etkilemez, doğrulayıcı uyarır (ADR-010, gerekçe boş).
- [x] **Algı kapsamı (KAPANDI):** algı yalnızca dış (zihin olmayan) olayları alır; algı olayları (`KuralId: algi`) kural tetiklemez.
- [x] **Hafıza:** aynı NPC ve aynı olay için tek kayıt; `hafiza` etkisi kayıt varsa önemi `max(eski, yeni)` yapar, yoksa yeni kayıt açar; her değişiklik yine `hafiza_eklendi` olayıyla kaydedilir.
- [x] **Tanımsız rol:** mevcut davranış kalır (kural o olay için sessizce atlanır). **7. hafta notu:** bu durum için fren olayı yazılmalı (atlanan kural günlükte iz bırakmalı).
- [x] **MiniHan:** `olay_rolu` kullanıyor. `cesaret` dışındaki kişilik değerleri 0.5 kalıyor; **11. hafta denge turunda ayarlanacak.**

**PR #8 (Yusuf):** `RepoStructureTests` içindeki "ADR-007 gerekçesi boş olmalı" kontrolü kaldırıldı; durum kontrolleri kalıyor. `CLAUDE.md` kuralı henüz değişmedi.

**4. hafta: ikinci inceleme turunda kapatılanlar (Yusuf):**
- [x] **Tek durum görüntüsü:** bir adımdaki bütün kurallar adım başındaki durumu görür; aynı adımdaki başka kuralın çıktısı görünmez. Kalıyor.
- [x] **Gerçek etkisi tekrarsız yazılır:** durumu değiştirmeyen etki üretilmez, aynı adımda aynı gerçek tek kez yazılır.
- [x] **Oyuncu hareketi 5. haftaya kaldı** (v0'da oyuncunun mekanı senaryodan gelir ve değişmez).
- [x] **1 tick = 1 saat** `docs/notes/zihin.md` madde 5'te **Karar (Yusuf)** olarak kayıtlı.
- Açık kalan tek ayrıntı: `hafiza` etkisi her zaman tetikleyici olayı hatırlatır (başka olay seçme yok); etki hedefi senaryoda yoksa `Simulasyon.Olustur` reddeder.
- Doğrulayıcı uyarıları: `agirlik` ve `zorunlu: false` uyarı verir (ADR-010). JSON okuyucuya 8 MiB boyut sınırı eklendi (sınır testleri sırasında eksik bulundu).

## Sıradaki hafta

- **5. hafta:** karar/utility AI, `IActionSelector`, eylem JSON'ları, NPC'lerin kendi eylemleri, karar izi (`yanki why` tam sürümü). Tick döngüsünün 5. ve 6. adımları `Simulasyon.TekTick` içinde işaretli yerlere takılır.
