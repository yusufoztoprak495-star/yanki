# Proje durumu

Tek bakışta: neredeyiz, ne bitti, Yusuf'tan ne bekleniyor. **Her PR'da güncellenir** (CLAUDE.md); PR bunsuz açılmaz.

## Son güncelleme

- **Tarih:** 2026-10-29
- **PR:** hafta-03 (3. hafta: Zihin ve doğrudan algı), PR numarası açılınca buraya yazılır.

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

### 3. hafta: zihin ve doğrudan algı (bu PR)
Ayrıntı PR açıklamasında; bu hafta bitince aşağıdaki özet güncellenir.

## Ertelenenler (Yusuf'un işi)

- ADR-001..007 "Gerekçe (Yusuf)" bölümleri (ADR-008 de eklenecek).
- `docs/notes/` taslaklarını kendi cümleleriyle yeniden yazmak.
- `CODECOV_TOKEN` secret'ı.
- Kasaba Hanı NPC'leri: 4. haftada 3, 9. haftada 8.

## Açık kararlar (Yusuf'u bekleyen)

- 3. haftanın açık noktaları PR açıklamasında ve `docs/notes/zihin.md`, `docs/notes/algi.md` sonlarında.

## Sıradaki hafta

- **4. hafta:** sonuç kuralları, kural motoru (kriter diliyle), doğrulayıcı v0, tick döngüsü v0, minimal CLI, 3 NPC'lik mini senaryo, golden replay #1.
