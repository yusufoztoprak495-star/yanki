# CLAUDE.md — Yankı

Bu dosya, bu repoda çalışan AI kod ajanları (Claude Code vb.) içindir. Önce bunu oku.

## Proje

Yankı: motordan bağımsız, açık kaynak C# NPC ve sonuç motoru. Oyuncu kararları NPC'lerde ve dünyada yankılanır;
motor her sonucun nedenini kanıtlayabilir. Sahibi ve tasarımcısı Yusuf. 12 haftalık plan, 2 Kasım 2026 – 24 Ocak 2027.

## Değişmez kararlar (ADR'siz değiştirme)

1. **NPC kararları kural + utility AI ile alınır. LLM karar vermez.** LLM yalnızca opsiyonel bir `IDialogueRenderer`
   eklentisi olarak metin üretebilir. (ADR-002)
2. **Olay kaynaklı çekirdek.** Durum, olay günlüğünden türetilir. Her olay `NedenOlaylar` ve (varsa) `KuralId` taşır. (ADR-001)
3. **Determinizm.** Aynı seed + aynı girdi = aynı durum hash'i. `System.Random`, `DateTime.Now`, `Guid.NewGuid`,
   sırası tanımsız koleksiyon iterasyonu (`Dictionary`/`HashSet` üzerinde sıralamasız `foreach`) ve `float`/`double`
   karşılaştırmasına dayalı dallanma çekirdekte yasak. Anahtarlı RNG kullan.
4. **Tek kriter dili** hem sonuç kuralları hem diyalog seçimi için. (ADR-003)
5. **`Yanki.Core` dış pakete bağımlı olmaz** ve `netstandard2.1;net10.0` hedefler. (ADR-004)
6. Demo Blazor WebAssembly ile yapılır (10. hafta). (ADR-005)
7. Ölçek: 10–50 derin NPC. Binlerce sığ NPC için optimize etme.

## Komutlar

```bash
dotnet build Yanki.slnx -c Release
dotnet test Yanki.slnx -c Release
dotnet run --project src/Yanki.Cli -- --version
```

## Çalışma kuralları

- **Önce test (TDD).** Yeni davranış için önce kırmızı test, sonra kod.
- Uyarılar hata sayılır (`TreatWarningsAsErrors`). Uyarıyı bastırma, düzelt.
- Paket sürümleri yalnızca `Directory.Packages.props` içinde.
- Küçük, anlamlı commit'ler. Mesajlar Türkçe olabilir; ilk satır ≤ 72 karakter.
- Mimari bir karar değişiyorsa `docs/adr/` altına yeni ADR yaz (şablon: `docs/adr/0000-sablon.md`). Eski ADR'yi silme,
  durumunu "Superseded" yap.
- Her modül bittiğinde Yusuf için `docs/notes/` altına kısa bir "bu modül ne yapıyor, neden böyle" notu taslağı bırak;
  Yusuf kendi cümleleriyle yeniden yazar.
- Kod yorumları ve dokümanlar Türkçe; tip ve üye adları İngilizce (C# alışkanlığı), alan terimleri gerektiğinde Türkçe
  kalabilir (`NedenOlaylar`, `KuralId`).
- Yusuf'un tasarım alanı: kriter dili sözlüğü, kişilik eksenleri, Kasaba Hanı içeriği, test senaryoları. Bunlarda
  karar verme; seçenekleri sun, o seçsin.

## Yapma

- Çekirdeğe LLM çağrısı, ağ erişimi, dosya sistemi erişimi ya da veritabanı ekleme.
- Python'a ya da başka bir dile geçme.
- Planda olmayan büyük bağımlılık ekleme (önce sor).
