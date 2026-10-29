<!-- Claude Code'a verilen görev metni, birebir. Hazırlandığı yer: claude.ai. Tarih: 29 Ekim 2026. -->

# Görev: Yankı — 1. hafta (Kurulum ve iskelet)

## 0. Önce ortamı kontrol et
Şu an `C:\Projeler` klasöründesin. Windows ve PowerShell kullanıyorum.
1. `dotnet --list-sdks` çalıştır. 10.x yoksa kurmam için gereken komutu söyle (örnek: `winget install Microsoft.DotNet.SDK.10`) ve bekle.
2. `git --version` ve `gh --version` çalıştır. Eksik olan varsa söyle.
3. `C:\Projeler\yanki` klasörü zaten varsa dokunma. Durup bana sor.
4. Repoyu `C:\Projeler\yanki` olarak klonla: `git clone https://github.com/yusufoztoprak495-star/yanki`
5. Plan PDF'i burada: `C:\Projeler\Yanki Plan v2.pdf`. Bu dosyayı repodaki `docs\plan\` klasörüne kopyala ve tamamını oradan oku.

## Bağlam
Ben Yusuf. "Yankı" adında, oyun motorundan bağımsız, açık kaynak bir C# NPC ve sonuç motoru yapıyorum. Bu, GitHub portfolyomun ana projesi. Süre 12 hafta (2 Kas 2026 – 24 Oca 2027), haftada 12–15 saat. Kodu sen yazıyorsun, tasarım kararları bende.

Tek kaynak: `docs/plan/Yanki Plan v2.pdf` ("Yankı Proje Planı v2, 28 Ekim 2026"). Bu görev planın SADECE 1. haftası. 2. haftaya ait hiçbir şeyi yazma: olay tipleri, RNG, durum hash'i, kriter ayrıştırıcı vb.

## Repo
Repoda daha önce oluşturulmuş bir iskelet var. Mevcut her dosyayı aşağıdaki listeye göre denetle. Uymayanı düzelt, eksik olanı ekle. Listede olmayan bir dosyayı silmeden önce bana sor.

## Değişmez kararlar (sorgulamadan uygula)
1. NPC kararları kural + utility AI ile verilir. LLM karar vermez. LLM ileride yalnızca opsiyonel bir `IDialogueRenderer` eklentisi olabilir.
2. Çekirdek olay kaynaklı (event-sourced). Her olay `NedenOlaylar[]` ve `KuralId` taşıyacak (kodu 2. haftada).
3. Determinizm: simülasyon kodunda `System.Random`, `DateTime.Now`, `Guid.NewGuid` ve sırasız `Dictionary`/`HashSet` gezintisi yasak. Tek, anahtarlı bir RNG olacak (2. hafta).
4. Sonuç kuralları, diyalog şablonları ve eylem ön koşulları için tek kriter dili: Valve tarzı ağırlıklı kriterler, JSON.
5. `Yanki.Core` ve `Yanki.Dialogue`:
   - `netstandard2.1;net10.0` hedefler,
   - hiçbir NuGet paketine bağımlı değildir,
   - `LangVersion` 9.0'dır (Unity sınırı).
6. Demo Blazor WebAssembly ile yapılacak (10. hafta). Lisans MIT. Ölçek 10–50 derin NPC.
7. Python'a ya da başka bir dile geçme. Çekirdeğe ağ, dosya sistemi ya da veritabanı erişimi ekleme.

## 1. haftanın teslimleri

### A. .NET çözümü
- `Yanki.slnx`.
- `global.json`: SDK 10.0.100, `rollForward: latestFeature`.
- `Directory.Build.props`: Nullable, TreatWarningsAsErrors, Deterministic, CI'da ContinuousIntegrationBuild.
- `Directory.Packages.props`: merkezi paket yönetimi.
- `src/Yanki.Core`: `netstandard2.1;net10.0`, LangVersion 9.0, bağımlılıksız.
  - Modül klasörleri: Events, World, Mind, Perception, Criteria, Rules, Decision, Brakes, Simulation.
  - Her klasörde, modülün sorumluluğunu ve hangi haftada yazılacağını 2–3 satırla anlatan bir `README.md` olsun (plan bölüm 5 ve 12).
  - `netstandard2.1` için `IsExternalInit` polyfill.
- `src/Yanki.Dialogue`: aynı hedefler, Core'a referans, yer tutucu.
- `src/Yanki.Cli`: net10.0, assembly adı `yanki`. Şimdilik sadece `--version`.
- Bu hafta OLUŞTURMA: `src/Yanki.Demo.Web` (10. hafta).
- `tests/Yanki.Core.Tests` ve `tests/Yanki.Scenarios.Tests`: xUnit, net10.0.
- `tests/golden/`: ne işe yarayacağını anlatan bir README.
- `tests/Yanki.Benchmarks`: boş BenchmarkDotNet konsol projesi. Benchmark yok, test koşusuna dahil değil.
- `samples/KasabaHani/` ve `samples/Quickstart/`: her birinde yer tutucu README.
- **Mimari testleri (önce test, TDD):**
  - Core'un referans verdiği assembly'ler yalnızca `System*`, `netstandard` ve `mscorlib` olabilir.
  - Core hiçbir `Yanki.*` assembly'sine referans vermez.
  - DİKKAT: "Dialogue, Core'a referans veriyor mu" diye metadata testi YAZMA. Dialogue, Core'dan tip kullanmadıkça derleyici bu referansı DLL'e yazmaz ve test yanlışlıkla kırılır.

### B. CI: `.github/workflows/ci.yml`
- `ubuntu-24.04`, `actions/checkout@v5`, `actions/setup-dotnet@v5` (`global-json-file` ile).
- Adımlar: restore → build -c Release (iki hedef de derlenir) → test.
- trx sonuçlarını artifact olarak yükle.
- Test kırılırsa hata satırlarını `::error::` annotation'ı olarak bas.
- Coverage: `coverlet.collector` ile topla, Codecov'a yükle. `CODECOV_TOKEN` yoksa bu adım CI'ı kırmasın (`fail_ci_if_error: false`). Bana token'ı nasıl ekleyeceğimi anlat.
- Bu hafta OLUŞTURMA: `release.yml` ve `pages.yml`.

### C. Dokümanlar
- **LICENSE (MIT):** sahip adı için `git config user.name` değerini kullan.
- **README.md:** planın 13. bölümündeki 12 bölümlük iskelet, boş başlıklarla.
  - Dolu olacak kısımlar: rozetler (CI, coverage, lisans, .NET hedefleri; NuGet ve "▶ Demoyu oyna" için "yakında" rozeti) ve tek cümlelik konumlandırma.
  - Diğer başlıkların altına "🚧 X. haftada" notu.
- **CLAUDE.md:**
  - değişmez kararlar,
  - komutlar (build, test, cli),
  - çalışma kuralları: TDD, PR üzerinden ilerleme, küçük commit'ler, `Co-Authored-By` satırı, karar değişirse yeni ADR, her modül sonrası `docs/notes/` için taslak not,
  - Yusuf'un tasarım alanı (kriter sözlüğü, kişilik eksenleri, denge sayıları, Kasaba Hanı içeriği, test senaryoları): bunlarda karar verme, seçenek sun.
- **`docs/ai-usage.md`:** planın 11. bölümündeki rol dağılımı tablosu.
- **`docs/architecture.md`:** modül tablosu ve tick döngüsünün Mermaid taslağı (plan bölüm 5–6).
- **`docs/criteria-language.md`:** kriter dili biçimi (plan 5.9) ve 15 satırlık BOŞ sözlük tablosu. Sütunlar: fonksiyon, ne sorar, döndürdüğü, örnek kriter. Ben dolduracağım.
- **`samples/KasabaHani/npcs.md`:** 8 NPC için BOŞ tablo. Sütunlar: isim, rol, ne istiyor, neyi saklıyor, kişilik. Ben dolduracağım.
- **`docs/authoring-guide.md` ve `docs/evaluation.md`:** başlık ve "ilerleyen haftalarda" notu.
- **`docs/notes/` ve `docs/devlog/`:** `.gitkeep`.
- **CHANGELOG.md** (Keep a Changelog) ve kısa bir **CONTRIBUTING.md**.

### D. ADR-001..006 (`docs/adr/`, şablon `0000-sablon.md`)
- 001 olay kaynaklı çekirdek
- 002 utility AI + `IActionSelector`
- 003 tek kriter dili
- 004 hedef framework (`netstandard2.1;net10.0`, bağımlılıksız çekirdek, LangVersion 9.0)
- 005 Blazor WASM demo (Unity WebGL ve Godot C# neden reddedildi)
- 006 MIT lisansı

`engineering:architecture` skill'inin ADR formatını kullan: Durum, Tarih, Bağlam, Karar, Değerlendirilen seçenekler (tablo), Ödünleşimler, Sonuçlar, Yapılacaklar. Her ADR'de "Gerekçe (Yusuf)" başlığını BOŞ bırak, orayı ben kendi cümlelerimle yazacağım. Durum: "Önerildi".

## Çalışma şekli
1. Planı oku, sonra `superpowers:writing-plans` ile bu listeyi adım adım uygulama planına çevir. Planı bana göster ve ONAYIMI BEKLE.
2. Onaydan sonra `hafta-01` branch'inde adım adım uygula. Her mantıklı adımda küçük bir commit at.
3. Mimari testleri koddan önce yaz (`superpowers:test-driven-development`).
4. Bitirmeden önce `superpowers:verification-before-completion` uygula ve şu çıktıları göster:
   - `dotnet build Yanki.slnx -c Release` (iki hedef, 0 uyarı),
   - `dotnet test Yanki.slnx -c Release`,
   - `dotnet run --project src/Yanki.Cli -- --version`.
5. Push et ve PR aç (`gh` yoksa PR linkini ver). CI yeşil olunca haber ver. Merge'i ben yapacağım.
6. Son mesajında şunlar olsun:
   - ne yaptın,
   - mevcut iskelette neyi düzelttin,
   - benim yapmam gerekenler: ADR gerekçeleri, kriter sözlüğü, 8 NPC, Codecov token.

## Yapma
- 2. haftanın kodunu yazma.
- Demo.Web, release.yml ve pages.yml oluşturma.
- Planda olmayan bir paket ya da özellik ekleme. Gerekiyorsa önce sor.
- Plan ile bu prompt çelişirse dur ve bana sor.
