# ADR-0004: Hedef framework: `netstandard2.1` + `net10.0`, bağımlılıksız çekirdek

**Durum:** Önerildi
**Tarih:** 2026-10-29

## Bağlam
Kütüphane hem Unity'de (Türk mobil oyun stüdyolarının ana motoru) hem güncel .NET'te (CLI, Blazor demo, testler)
çalışmalı. Unity yalnızca `netstandard2.1` derlemelerini kabul ediyor. .NET 8 ve 9'un desteği 10 Kasım 2026'da
bitiyor; güncel LTS .NET 10.

## Karar
- `Yanki.Core` ve `Yanki.Dialogue`: `netstandard2.1;net10.0` çoklu hedef.
- `Yanki.Cli`, testler ve demo: yalnızca `net10.0`.
- **Çekirdek hiçbir NuGet paketine bağımlı değildir.** Bu bir mimari testle CI'da korunur.
- `netstandard2.1`'de eksik olan dil destek tipleri (ör. `IsExternalInit`) çekirdek içinde polyfill olarak tutulur.
- SDK sürümü `global.json` ile sabitlenir.

## Değerlendirilen seçenekler

### Seçenek A: Çoklu hedef `netstandard2.1;net10.0`
| Boyut | Değerlendirme |
|---|---|
| Karmaşıklık | Düşük-orta |
| Maliyet | Bazı modern API'ler `#if` gerektirir |
| Ölçeklenebilirlik | — |
| Bilinirlik | Yaygın kütüphane pratiği |

**Artıları:** Unity + modern .NET; tek kod tabanı.
**Eksileri:** `netstandard2.1`'de `Span` dışındaki bazı yeni API'ler yok; `#if` disiplini lazım.

### Seçenek B: Yalnızca `net10.0`
**Artıları:** En yeni API'ler, sade.
**Eksileri:** Unity dışarıda kalır; motordan bağımsızlık iddiası zayıflar.

### Seçenek C: Yalnızca `netstandard2.1`
**Artıları:** En geniş uyum.
**Eksileri:** Modern çalışma zamanının performans ve analiz kazançları kaybolur.

## Ödünleşimler
A, küçük bir `#if` maliyetiyle hem Unity hem modern .NET'i kapsıyor.

## Gerekçe (Yusuf)

## Sonuçlar
- Kolaylaşan: Unity'ye paket olarak girmek (OpenUPM, v0.2 sonrası), NuGet yayını.
- Zorlaşan: Yeni API kullanırken her iki hedefi de derlemek; CI ikisini de derler.
- Yeniden bakılacak: Unity CoreCLR'a geçtiğinde `netstandard2.1` hedefi bırakılabilir.

## Yapılacaklar
1. [x] Çoklu hedefli projeler, C# 9.0 ve `global.json` (1. hafta).
2. [x] "Çekirdek yalnızca çerçeveye bağlı" mimari testleri, iki hedefte (1. hafta).
