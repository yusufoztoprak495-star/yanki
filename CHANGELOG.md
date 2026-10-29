# Değişiklik günlüğü

Biçim [Keep a Changelog](https://keepachangelog.com/tr/1.1.0/), sürümleme [SemVer](https://semver.org/lang/tr/).

## [Yayınlanmadı]

### Eklendi
- 1. hafta iskeleti: `Yanki.Core` (modül klasörleri), `Yanki.Dialogue`, `Yanki.Cli` (`--version`), test projeleri, `Yanki.Benchmarks`.
- `netstandard2.1;net10.0` çoklu hedef, `Core` ve `Dialogue` için C# 9.0, merkezi paket yönetimi, deterministik derleme.
- GitHub Actions CI: derleme, test, trx artifact'ı, coverage ve Codecov yüklemesi.
- Mimari ve iskelet testleri (çekirdek yalnızca çerçeveye bağlı, her iki hedefte).
- ADR-001..006 (durum: önerildi), `CLAUDE.md`, `docs/ai-usage.md`, mimari ve kriter dili belgeleri, README iskeleti.
