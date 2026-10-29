# Proje durumu

Tek bakışta: neredeyiz, ne bitti, Yusuf'tan ne bekleniyor. **Her PR'da güncellenir** (CLAUDE.md); PR bunsuz açılmaz.

## Son güncelleme

- **Tarih:** 2026-10-29
- **PR:** [PR #4](https://github.com/yusufoztoprak495-star/yanki/pull/4) (3. hafta: Zihin ve doğrudan algı)

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

### 3. hafta: zihin ve doğrudan algı (PR #4, merge bekliyor)
- Zihin: kişilik, 3 eksen + süreli fikir modifier'ları (`FikirDokumu`), tek duygulu ruh hali, stres, kanıtlı inançlar (çelişki kuralı, `KimdenDuydu`), hafıza.
- Saf sönme fonksiyonları (`Sonumleme`, `ZihinAyarlari`) ve altı zihin olayı; zihin durum hash'e giriyor, dünya altın hash'i değişmedi.
- Doğrudan algı: `acik`/`sessiz`/`gizli`, `gordu` kanıtı + hafıza olayı (`KuralId: algi`).
- `ZihinSorguCozucusu`: kriter dili sözlüğünün 13/15 fonksiyonu gerçek veriye bağlı (`yalan_soyledi` 6., `yay` 7. hafta).
- ADR-008, `docs/notes/zihin.md`, `docs/notes/algi.md`; 475 test (iki hedefte derleniyor, 0 uyarı).

## Ertelenenler (Yusuf'un işi)

- ADR-001..008 "Gerekçe (Yusuf)" bölümleri.
- `docs/notes/` taslaklarını (dunya, olay-kaydi, kriter-dili, zihin, algi) kendi cümleleriyle yeniden yazmak.
- `CODECOV_TOKEN` secret'ı.
- Kasaba Hanı NPC'leri: 4. haftada 3, 9. haftada 8.

## Açık kararlar (Yusuf'u bekleyen)

- 3. haftanın açık noktaları (eksen ağırlıkları, sönme oranları, hafıza eşikleri, çelişkide eşitlik kuralı, algı gücü/önemi vb.):
  `docs/notes/zihin.md` ve `docs/notes/algi.md` sonlarındaki "Açık noktalar" bölümleri; özeti PR #4 açıklamasında.

## Sıradaki hafta

- **4. hafta:** sonuç kuralları, kural motoru (kriter diliyle), doğrulayıcı v0, tick döngüsü v0, minimal CLI, 3 NPC'lik mini senaryo, golden replay #1.
