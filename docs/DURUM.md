# Proje durumu

Tek bakışta: neredeyiz, ne bitti, Yusuf'tan ne bekleniyor. **Her PR'da güncellenir** (CLAUDE.md); PR bunsuz açılmaz.

## Son güncelleme

- **Tarih:** 2026-11-02
- **PR:** [PR #7](https://github.com/yusufoztoprak495-star/yanki/pull/7) (4. hafta: sonuç kuralları ve ince dikey dilim, merge bekliyor)

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

### 4. hafta: sonuç kuralları ve ince dikey dilim (PR #7, merge bekliyor)
- **JSON okuyucu/yazıcı** (`Yanki.Core.Json`, bağımlılıksız, sayılar `decimal`, satır/sütun bilgili hata); ADR-009. `KriterOkuyucu` kriterleri JSON'dan okur (ikinci kriter yolu yok).
- **Kural motoru** (`Rules`): `ne_zaman` + kriterler → etkiler; v0 etkileri modifier, eksen, ruh hali, hafıza, dünya gerçeği; etkiler yeni olay (`KuralId` + `NedenOlaylar`). Doğrulayıcı v0 `dosya:satır: kural "id": mesaj` verir.
- **Tick döngüsü v0 ve dış API** (`Simulation`): `Submit(PlayerAction)`, `Tick(n)`, sorgular, `Neden(olayId)`; adımlar 1, 2, 3, 4, 7. Kaskad v0: kural çıktıları sıradaki tick'e kalır.
- **CLI:** `yanki run | query | why | replay` (`replay --guncelle` golden hash'i tek komutla günceller).
- **Mini senaryo `samples/MiniHan`** (Yusuf'un seçimi: seçenek A + hedef/tanık düzeltmesi) ve **golden replay #1** (`tests/golden/mini-han-1.json`); CI her push'ta tekrar oynatır.
- Uçtan uca test: oyuncu eylemi fikri değiştirir, `why` zinciri modifier'dan oyuncu eylemine iner. Notlar: `docs/notes/kural-motoru.md`, `docs/notes/tick-dongusu.md`; devlog iskeleti `docs/devlog/01-taslak.md`.
- Testler: 625 (Core) + 24 (senaryo/CLI/golden); iki hedef derleniyor, 0 uyarı.

## Ertelenenler (Yusuf'un işi)

- [ ] **ADR-001..009 "Gerekçe (Yusuf)" bölümleri:** dokuz ADR'nin hepsinde boş (ADR-009: çekirdekte JSON okuyucu).
- [ ] **`docs/notes/` taslaklarını kendi cümlelerimle yeniden yazmak:** `dunya.md`, `olay-kaydi.md`, `kriter-dili.md`, `zihin.md`, `algi.md`, `kural-motoru.md`, `tick-dongusu.md`.
- [ ] **Devlog #1'i yazmak:** `docs/devlog/01-taslak.md` yalnızca başlık ve teknik gerçek içerir.
- [x] **`CODECOV_TOKEN` secret'ı:** GitHub repo ayarlarına eklendi.
- [x] **Mini senaryo (3 NPC):** `samples/MiniHan` (4. hafta). Kişilik özelliklerinden `cesaret` dışındakiler 0.5 yer tutucu; Yusuf değiştirebilir.
- [ ] **Kasaba Hanı NPC'leri:** 9. haftada 8.

## Açık kararlar (Yusuf'u bekleyen)

- 3. haftanın büyük kararları PR #4 incelemesinde verildi (eksen ağırlıkları, sönme oranları, hafıza eşikleri, birikme, çelişki eşitliği, duygu eşitliği, algı değerleri); notlarda **Karar (Yusuf)** olarak işli.
- İkinci inceleme turunda kalan küçük noktalar da karara bağlandı ve koda işlendi: `inanc(degil(...))`, tekrar eden kanıtın tazelenmesi, failın her zaman algılaması,
  `FikirDokumu` kırpma satırı; `gordu` kaynağı ve bilinmeyen görünürlük hatası olduğu gibi kaldı.
- `oyuncu` artık ayrı bir varlık türü (`VarlikTuru.Oyuncu`), eşya değil.
- 3. hafta için Yusuf'u bekleyen açık karar kalmadı (yalnızca ADR-008 "Gerekçe (Yusuf)" yazımı).

**4. hafta (Claude'un varsayılan seçtikleri; Yusuf'un onayı/kararı bekleniyor, ayrıntı `docs/notes/kural-motoru.md` ve `tick-dongusu.md`):**
- **Kural ne zaman tetiklenir?** Hiçbir `zorunlu` kriter elenmemiş **ve** toplam puan > 0 (tutan en az bir kriter); kriteri olmayan kural olay türü tutunca tetiklenir. Alternatifler: tüm kriterler tutmalı, ya da kuralda eşik puanı.
- **Olayda tanımsız rol** (ör. hedefsiz eylemde `olay.hedef`): kural o olay için sessizce atlanır.
- **Hedef/tanık ayrımı** sözlüğe fonksiyon eklenmeden `inanc(<tür>(olay.fail, ben)) >= 0.5` ile yapıldı (algı önermesi `tur(fail, hedef)` biçimine bağlı). İstenirse `ben_hedef_mi()` gibi bir fonksiyon sözlük değişikliği olarak onayına sunulur.
- **Algı hangi olayları alır:** zihin olayı olmayan (dış) olaylar; zihin olayını yalnızca sahibi kural tetiklemek için "algılar". Algı olayları kural tetiklemez.
- **Tek durum görüntüsü:** bir adımdaki bütün kurallar adım başındaki durumu görür; aynı adımdaki başka kuralın çıktısı görünmez.
- **Gerçek etkisi** durumu değiştirmiyorsa üretilmez ve aynı adımda aynı gerçek tek kez yazılır; `hafiza` etkisi her zaman tetikleyici olayı hatırlatır.
- **Aynı olay için çift hafıza kaydı:** algı (önem 0.5) ve `hafiza` etkisi (ör. 0.9) ayrı kayıtlardır; `query` ikisini de gösterir.
- **Oyuncu hareketi yok** (oyuncunun mekanı senaryodan gelir, v0'da değişmez).
- **1 tick = 1 saat** zaten `docs/notes/zihin.md` madde 5'te **Karar (Yusuf)** olarak kayıtlı; yeni karar gerekmedi.
- **Kişilik özellikleri** MiniHan'da `cesaret` dışında 0.5 yer tutucu.

## Sıradaki hafta

- **5. hafta:** karar/utility AI, `IActionSelector`, eylem JSON'ları, NPC'lerin kendi eylemleri, karar izi (`yanki why` tam sürümü). Tick döngüsünün 5. ve 6. adımları `Simulasyon.TekTick` içinde işaretli yerlere takılır.
