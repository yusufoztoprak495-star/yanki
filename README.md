# Yankı

[![CI](https://github.com/yusufoztoprak495-star/yanki/actions/workflows/ci.yml/badge.svg)](https://github.com/yusufoztoprak495-star/yanki/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/yusufoztoprak495-star/yanki/graph/badge.svg)](https://codecov.io/gh/yusufoztoprak495-star/yanki)
[![Lisans: MIT](https://img.shields.io/badge/lisans-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-net10.0%20%7C%20netstandard2.1-512BD4)](docs/adr/0004-hedef-framework.md)
![NuGet](https://img.shields.io/badge/NuGet-yak%C4%B1nda-lightgrey)
![Demo](https://img.shields.io/badge/%E2%96%B6%20Demoyu%20oyna-yak%C4%B1nda-lightgrey)

**Anlatı oyunları için deterministik, tekrar oynatılabilir NPC hafızası ve sonuç sistemi; Unity, Godot ve sunucuda çalışan açık bir C# kütüphanesi.**

> 🚧 **Erken geliştirme aşamasında.** 12 haftalık planın 7. haftası: olay kaydı, zihin ve doğrudan algı, sonuç kuralları, tick döngüsü, utility AI ile NPC'lerin kendi eylemleri ve hareketi, karar izi, **dedikodu (`duydu` kanıtı aktarımı, konu sınırı), yalan, stres ve kişiliğe uygun kriz** (`yanki why` söylentiyi ilk anlatana, krizi eyleme kadar indirir), CLI (`run`, `query`, `why`, `replay`, `devam`), **frenler (cooldown, tetiklenme ve zincir sınırları, korumalı bayraklar, yay değişkenleri; her fren günlükte iz bırakır ve "bu neden olmadı?" cevaplanır)**, kayıt/yükleme (sürüm 2; `replay --guncelle | --hash-ekle | --senaryo-hash-yenile`) ve dört golden replay.

**Hız (v0.1 kriteri, `tools/olcum`, benchmark değil):** 8 NPC'lik senaryoda tick başına ortalama ≤ 5 ms. Gerçekçi fikstürde ölçülen: **2.9 ms** (8 NPC); sahte plan-bütçesi varyantında (15 eylem, 30 kural) 5.8–8.0 ms, yani gerçek Kasaba Hanı ile 9. haftada yeniden ölçülecek bir **risk**. **50 NPC v0.2 hedefidir**; bugünkü ölçülen değer ≈ 330 ms/tick (gerçekçi fikstür). 168 tick baştan oynatma: 8 NPC 0.7 sn, 50 NPC ≈ 59 sn. Koşullar ve dağılım: [ADR-015](docs/adr/0015-5ms-kriteri.md). BenchmarkDotNet tablosu 11. haftada.

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
