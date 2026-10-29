# Değişiklik günlüğü

Biçim [Keep a Changelog](https://keepachangelog.com/tr/1.1.0/), sürümleme [SemVer](https://semver.org/lang/tr/).

## [Yayınlanmadı]

### Eklendi
- 1. hafta iskeleti: `Yanki.Core` (modül klasörleri), `Yanki.Dialogue`, `Yanki.Cli` (`--version`), test projeleri, `Yanki.Benchmarks`.
- `netstandard2.1;net10.0` çoklu hedef, `Core` ve `Dialogue` için C# 9.0, merkezi paket yönetimi, deterministik derleme.
- GitHub Actions CI: derleme, test, trx artifact'ı, coverage ve Codecov yüklemesi.
- Mimari ve iskelet testleri (çekirdek yalnızca çerçeveye bağlı, her iki hedefte).
- ADR-001..006 (durum: önerildi), `CLAUDE.md`, `docs/ai-usage.md`, mimari ve kriter dili belgeleri, README iskeleti.
- 2. hafta: `Yanki.Core.Events` (`Olay`, `OlayGunlugu`: yalnızca sona ekleme, Id/Tick/Sira/nedensellik doğrulaması, `Neden` zinciri).
- 2. hafta: `Yanki.Core.World` (`Dunya`, `Gercek`, `GercekDeseni`; tek değerli `!` gerçekler, yönsüz mekan grafiği, dünya olaylarından türeyen durum, ASCII kimlikler).
- 2. hafta: anahtarlı RNG (`AnahtarliRng`: FNV-1a + SplitMix64), `Hash64`, `KanonikYazici` (`decimal`, `ToEven`, 6 basamak) ve `Dunya.DurumHash()`.
- 2. hafta: `Yanki.Core.Criteria` (ayrıştırıcı, 15 fonksiyonluk sözlük ve doğrulayıcı, `Kriter`, `ISorguCozucu`, `DunyaSorguCozucusu` (`gercek`, `ayni_mekanda`), `Secici.EnIyiSec` ve eşleşme izi). Kriterler C# ile kurulur; JSON okuma yok.
- 2. hafta testleri: determinizm, altın sabit (hash ve RNG, bağımsız hesapla doğrulanmış), yasaklı API taraması (`System.Random`, `DateTime.Now`, `double`/`float`, sırasız koleksiyonlar vb.), sözlük-docs eşitleme testi.
- ADR-007: simülasyonda `decimal` (durum: önerildi). `docs/notes/` altında `olay-kaydi.md`, `dunya.md`, `kriter-dili.md` taslakları.
- 3. hafta: `Yanki.Core.Mind` (`Zihin`, `ZihinDeposu`, `Kisilik`, ilişki eksenleri ve süreli fikir modifier'ları, `FikirDokumu`, tek duygulu ruh hali, stres, kanıtlı inançlar ve çelişki kuralı, hafıza, `Sonumleme` saf sönme fonksiyonları, `ZihinAyarlari`, altı zihin olayı, birleşik `DurumHashu`).
- 3. hafta: `Yanki.Core.Perception` (`DogrudanAlgi`: `acik`/`sessiz`/`gizli` görünürlük, `gordu` kanıtı ve hafıza olayı üretimi).
- 3. hafta: `ZihinSorguCozucusu`: sözlükteki 15 fonksiyonun 13'ü gerçek veriye bağlandı (`yalan_soyledi` 6., `yay` 7. hafta).
- ADR-008: zihin olaylardan türetilir; süreli değerler sorgu anında hesaplanır (durum: önerildi). `docs/notes/zihin.md`, `docs/notes/algi.md` taslakları. `docs/DURUM.md` proje durum dosyası (her PR'da güncellenir).
