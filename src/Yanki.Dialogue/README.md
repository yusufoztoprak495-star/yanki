# Yanki.Dialogue

Olaydan türeyen niyet, kriter diliyle şablon seçimi, tekrar kontrolü, hafıza göndermesi, Türkçe mini gramer ve `IDialogueRenderer`. `netstandard2.1;net10.0`, NuGet'siz, C# 9.

```csharp
var yukleme = SablonYukleyici.Yukle(File.ReadAllText("sablonlar.json"), "sablonlar.json");   // yukleme.Hatalar: dosya:satir: biçiminde
using var motor = DiyalogMotoru.Baglan(sim, yukleme.Dosya!);                                   // OlayYayinlandi'ya abone olur
sim.Tick(10);
foreach (var s in motor.Satirlar) Console.WriteLine($"{s.Konusan}: {s.Metin}");               // motor.DiyalogHash() golden için
```

Komutlar: `yanki diyalog <oturum.json> --sablon <sablon.json> [--neden N]`, `yanki diyalog-replay <golden.json> [--guncelle]`. Şablon yazımı: [`docs/authoring-guide.md`](../../docs/authoring-guide.md).
Kararlar: [ADR-014](../../docs/adr/0014-diyalog-katmani.md). Örnek şablonlar (taslak, Yusuf gözden geçirecek): `samples/HanGerilim/sablonlar.json`, `samples/SiraEtkisiSoylenti/sablonlar.json`.
Yalan gizliliği: niyet ve renderer girdisi `KuralId`, kök, `NedenOlaylar` ve karar izini taşımaz.
