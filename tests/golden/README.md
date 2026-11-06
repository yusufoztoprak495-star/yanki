# Golden replay oturumları

Bu klasör, determinizmin kanıtıdır. Her golden oturum (`*.json`, biçim: `OturumKaydi`) şunları saklar:

- **seed**
- **senaryo** (kayıt dosyasına göre göreli yol; hangi başlangıç dünyası)
- **eylem günlüğü** (oyuncu eylemleri, hangi tick'te işlendikleriyle)
- **beklenen son durum hash'i** (`son_hash`)

CI (`.github/workflows/ci.yml`, "Golden replay" adımı) ve `dotnet test` (`GoldenReplayTests`) bu oturumları baştan oynatır ve son hash'in
kayıttakiyle birebir tuttuğunu doğrular. Hash tutmuyorsa bir yerde determinizm bozulmuştur (ya da davranış bilerek değişmiştir).

## Komutlar

```bash
# Bir oturumu oynat ve hash'i doğrula (tutmazsa çıkış kodu 3):
dotnet run --project src/Yanki.Cli -c Release -- replay tests/golden/<dosya>.json

# Hash BİLEREK değiştiyse tek komutla güncelle (dosyadaki son_hash yeniden yazılır):
dotnet run --project src/Yanki.Cli -c Release -- replay tests/golden/<dosya>.json --guncelle

# Yalnızca senaryo dosyası değişti, davranış (son_hash ve gunluk_hash) aynı (yorum, boşluk): sadece senaryo_hash yenilenir.
# Davranış değişmişse reddeder (kod 3 / 5); o durumda --guncelle kullan.
dotnet run --project src/Yanki.Cli -c Release -- replay tests/golden/<dosya>.json --senaryo-hash-yenile

# Hepsini güncelle:
for f in tests/golden/*.json; do dotnet run --project src/Yanki.Cli -c Release -- replay "$f" --guncelle; done

# Yeni golden oturum kaydet (seed ve eylemler komut satırından, sırayla):
dotnet run --project src/Yanki.Cli -c Release -- run samples/<senaryo>/scenario.json --seed 42 --eylem tur:hedef --tick 3 --kaydet tests/golden/<ad>.json
```

## Hash güncelleme kuralı

Hash güncellemesi **PR'da gerekçe ister**: PR açıklamasına hangi golden dosyanın, hangi davranış değişikliği yüzünden ve neden bilerek değiştiğini yaz
(eski hash → yeni hash). Gerekçesiz bir hash değişikliği determinizm kaybı sayılır.

Hedef: 3–5 oturum; biri Blazor WebAssembly çıktısıyla karşılaştırılır (plan bölüm 9).

**Durum:** `mini-han-1.json` (4. hafta, oyuncu eylemleri) ve `mini-karar-1.json` (5. hafta, NPC kararları + oyuncu hareketi, seed 42, 24 tick; NPC kararları dosyaya yazılmaz, tekrar hesaplanır). `han-gerilim-1.json` (6. hafta, #3: söylenti zinciri + yalan/stres/itiraf + hakaret/tehdit/patlama, seed 42, 24 tick, `13e6ccbefed06409` (7. hafta: kaynak bilgisi düzeltmesi ve `aile_iftirasi` `en_fazla: 1` + süre düzeltmesi sonrası; önceki hash'ler `690f2f4ee9d3d385`, `d98b50f39fc358dd`); oyuncu anlatımları ve hakaret eylem günlüğündedir, NPC kararları tekrar hesaplanır). Yenileri ilerleyen haftalarda.
`kasaba-hani-1.json` (9. hafta, #5: `samples/KasabaHani`, seed 42, 48 tick; betik `oynanislar/plan7_borc.json`: Can'a selam, Ayşe'ye borç anlatımı, Can'a selam, ele verdirme isteği, Can'a selam; `son_hash` `993d83a17817026c`, `gunluk_hash` `7c118a6c64ca7ff1`; betikte dünya durumu yüzünden atlanan eylem yok, `KasabaHaniTests` ile kilitli).
`frenli-han-1.json` (7. hafta, #4: `samples/FrenliHan`, seed 42, 24 tick; cooldown, `en_fazla`, `yay_siniri` ve `korumali` fren olayları; oyuncunun beş tehdidi eylem günlüğündedir, NPC kararları tekrar hesaplanır). Üç eski golden sürüm 2'ye geçti (`gunluk_hash`, `senaryo_hash`, `bekleyen_eylemler`); `son_hash` değerleri değişmedi.

## Diyalog golden'ları (8. hafta)

`tests/golden/diyalog/*.golden.json` ayrı biçimdedir (dört ana golden'a alan eklenmedi): `kayit` (oturum yolu), `sablon` (şablon dosyası yolu), `diyalog_hash`, `sablon_hash`. Kayıt `replay` ile aynı oynatıcıdan geçer
(`son_hash`/`gunluk_hash` kırılırsa çıkış kodu 3/5), ardından `diyalog_hash` (çıkış 3) ve `sablon_hash` (çıkış 7) denetlenir.

```bash
dotnet run --project src/Yanki.Cli -c Release -- diyalog-replay tests/golden/diyalog/<dosya>.golden.json            # doğrula
dotnet run --project src/Yanki.Cli -c Release -- diyalog-replay tests/golden/diyalog/<dosya>.golden.json --guncelle # bilerek değiştiyse (gerekçe PR'da)
```

Mevcut: `kasaba-hani-diyalog-1` (9. hafta, Kasaba Hanı şablonu, `kasaba-hani-1.json` kaydı; `diyalog_hash` `aade456097c3531f`, `sablon_hash` `e29fda815545a828`: `selam_muhur_hafiza` ve `tehdit_gecmis` düzeltmeleriyle yalnızca `sablon_hash` değişti (`6207707f5dfdd641` → `91a3f51384690b6e` → `e29fda815545a828`), `diyalog_hash` aynı), `han-gerilim-diyalog-1` (HanGerilim şablonu, `han-gerilim-1.json` kaydı), `soylenti-a` / `soylenti-b` (SiraEtkisiSoylenti, aynı iki oyuncu eylemi farklı sırada; kayıtlar `*.kayit.json`).
Şablon metni değişince `sablon_hash` kırılır; bu yalnızca metin düzeltmesidir, simülasyon hash'leri değişmez.
