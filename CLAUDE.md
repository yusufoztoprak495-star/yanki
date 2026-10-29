# CLAUDE.md — Yankı

Bu dosya, bu repoda çalışan AI kod ajanları (Claude Code vb.) içindir. Önce bunu oku.

## Proje

Yankı: motordan bağımsız, açık kaynak C# NPC ve sonuç motoru. Oyuncu kararları NPC'lerde ve dünyada yankılanır;
motor her sonucun nedenini kanıtlayabilir. Sahibi ve tasarımcısı Yusuf. 12 haftalık plan, 2 Kasım 2026 – 24 Ocak 2027.
Tek kaynak: `docs/plan/Yanki Plan v2.pdf`. Plan ile bir talimat çelişirse dur ve Yusuf'a sor.

## Değişmez kararlar (ADR'siz değiştirme)

1. **NPC kararları kural + utility AI ile alınır. LLM karar vermez.** LLM ileride yalnızca opsiyonel bir
   `IDialogueRenderer` eklentisi olabilir. (ADR-002)
2. **Olay kaynaklı çekirdek.** Durum, olay günlüğünden türetilir. Her olay `NedenOlaylar[]` ve `KuralId` taşır. (ADR-001)
3. **Determinizm.** Simülasyon kodunda `System.Random`, `DateTime.Now`, `Guid.NewGuid` ve sırasız `Dictionary`/`HashSet`
   gezintisi yasak. Tek, anahtarlı bir RNG kullanılır (2. hafta). Aynı seed + aynı girdi = aynı durum hash'i.
4. **Tek kriter dili** (Valve tarzı ağırlıklı kriterler, JSON): sonuç kuralları, diyalog şablonları ve eylem
   ön koşulları için. (ADR-003)
5. **`Yanki.Core` ve `Yanki.Dialogue`:** `netstandard2.1;net10.0` hedefler, hiçbir NuGet paketine bağımlı değildir,
   `LangVersion` 9.0'dır (Unity sınırı). (ADR-004)
6. Demo Blazor WebAssembly ile yapılır (10. hafta). Lisans MIT. (ADR-005, ADR-006)
7. Ölçek: 10–50 derin NPC. Binlerce sığ NPC için optimize etme.
8. Python'a ya da başka bir dile geçme. Çekirdeğe ağ, dosya sistemi ya da veritabanı erişimi ekleme.

## Komutlar

```bash
dotnet build Yanki.slnx -c Release                      # iki hedef de derlenir, 0 uyarı beklenir
dotnet test Yanki.slnx -c Release
dotnet run --project src/Yanki.Cli -- --version
```

## Çalışma kuralları

- **Önce test (TDD).** Yeni davranış için önce kırmızı test, sonra kod.
- İş **PR üzerinden** ilerler; `main`'e doğrudan commit yok. Merge'i Yusuf yapar.
- **Küçük, anlamlı commit'ler.** Mesajlar Türkçe olabilir; ilk satır ≤ 72 karakter.
- AI'nin yazdığı commit'lerin sonuna `Co-Authored-By` satırı eklenir.
- Uyarılar hata sayılır (`TreatWarningsAsErrors`). Uyarıyı bastırma, düzelt.
- Paket sürümleri yalnızca `Directory.Packages.props` içinde. Planda olmayan paket ekleme (önce sor).
- **Her PR'da `docs/DURUM.md` güncellenir. PR bunsuz açılmaz.**
- **Karar değişirse yeni ADR** yaz (`docs/adr/`, şablon: `0000-sablon.md`). Eski ADR'yi silme, durumunu "Yerini aldı" yap.
  ADR'lerdeki "Gerekçe (Yusuf)" bölümünü Yusuf yazar; sen doldurma.
  ADR'ler Yusuf gerekçeyi yazana kadar "Önerildi" kalır; kararlar yine de bağlayıcıdır.
- **`docs/prompts/` altına yalnızca Yusuf'un verdiği görev metinleri birebir kaydedilir. Claude Code görev metni yazmaz veya tamamlamaz.**
- **Her modül bittiğinde** `docs/notes/` altına "bu modül ne yapıyor, neden böyle" taslak notu bırak; Yusuf kendi
  cümleleriyle yeniden yazar.
- Kod yorumları ve dokümanlar Türkçe; tip ve üye adları İngilizce, alan terimleri gerektiğinde Türkçe kalabilir
  (`NedenOlaylar`, `KuralId`).

## Yusuf'un tasarım alanı

Şunlarda **karar verme; seçenekleri sun, Yusuf seçsin:**

- kriter dili sözlüğü (hangi sorgu fonksiyonları var),
- kişilik eksenleri,
- denge sayıları,
- Kasaba Hanı içeriği (NPC'ler, eylemler, kurallar, şablon metinleri),
- test senaryoları (neyin test edileceği).

## Yapma

- Çekirdeğe LLM çağrısı, ağ erişimi, dosya sistemi erişimi ya da veritabanı ekleme.
- Haftanın kapsamı dışındaki kodu yazma (plan bölüm 12'ye bak).
