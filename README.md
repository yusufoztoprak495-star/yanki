# Yankı

[![CI](https://github.com/yusufoztoprak495-star/yanki/actions/workflows/ci.yml/badge.svg)](https://github.com/yusufoztoprak495-star/yanki/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/yusufoztoprak495-star/yanki/graph/badge.svg)](https://codecov.io/gh/yusufoztoprak495-star/yanki)
[![Lisans: MIT](https://img.shields.io/badge/lisans-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-net10.0%20%7C%20netstandard2.1-512BD4)](docs/adr/0004-hedef-framework.md)
![NuGet](https://img.shields.io/badge/NuGet-yak%C4%B1nda-lightgrey)
![Demo](https://img.shields.io/badge/%E2%96%B6%20Demoyu%20oyna-yak%C4%B1nda-lightgrey)

**Anlatı oyunları için deterministik, tekrar oynatılabilir NPC hafızası ve sonuç sistemi; Unity, Godot ve sunucuda çalışan açık bir C# kütüphanesi.**

> 🚧 **Erken geliştirme aşamasında.** 12 haftalık planın 1. haftası: iskelet, CI ve mimari kararlar.

## 1. Rozetler

Yukarıda. NuGet ve demo rozetleri v0.1.0 ile gerçek bağlantılara dönecek.

## 2. Hero GIF

🚧 10. haftada (demo hazır olunca): oyuncu bir şey söylüyor, panelde bir NPC'nin fikri değişiyor, "neden?" zinciri açılıyor.

## 3. Tek cümlelik tanım ve 5 madde

Deterministik · Motordan bağımsız · Açıklanabilir · Test edilmiş · Hızlı

🚧 12. haftada: her maddenin kanıtına bağlantı (golden replay'ler, CI, `why` zinciri, benchmark tablosu).

## 4. Demo ve video

🚧 10. haftada tarayıcı demosu (GitHub Pages), 12. haftada 60–90 sn'lik video.

## 5. Kurulum ve hızlı başlangıç

🚧 12. haftada: `dotnet add package Yanki.Core` ve 15 satırlık konsol örneği ([`samples/Quickstart`](samples/Quickstart/README.md)).

Şimdilik kaynaktan: [.NET 10 SDK](https://dotnet.microsoft.com/download) ile
`dotnet build Yanki.slnx -c Release`, `dotnet test Yanki.slnx -c Release`.

## 6. Temel kavramlar

🚧 3–7. haftalarda: zihin katmanları, kanıtlı inançlar, kriter dili, frenler (`<details>` blokları içinde).

## 7. Mimari

🚧 12. haftada Mermaid diyagramıyla tamamlanacak. Taslak: [`docs/architecture.md`](docs/architecture.md).

## 8. Seçimler önemli mi?

🚧 11. haftada: tek seçim farkı ayrışma raporu ve ablasyon tablosu.

## 9. Benchmark

🚧 11. haftada: 10 / 50 / 200 NPC için tick süresi ve bellek tahsisi (donanım ve .NET sürümüyle).

## 10. Benzer sistemlerle karşılaştırma

🚧 12. haftada: Love/Hate, Ensemble, Yarn/ink ile ilişki.

## 11. Bu proje nasıl geliştirildi

Tasarım kararları ve sistem kurgusu Yusuf'a, kodun büyük kısmı Claude Code'a ait. Ayrıntı: [`docs/ai-usage.md`](docs/ai-usage.md).
🚧 12. haftada ADR ve devlog bağlantılarıyla genişleyecek ([`docs/adr/`](docs/adr/), [`docs/devlog/`](docs/devlog/)).

## 12. Yol haritası, katkı, lisans

🚧 12. haftada yol haritası (v0.2 sonrası). Katkı: [`CONTRIBUTING.md`](CONTRIBUTING.md). Lisans: [MIT](LICENSE).
