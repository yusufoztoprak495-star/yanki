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

# Hepsini güncelle:
for f in tests/golden/*.json; do dotnet run --project src/Yanki.Cli -c Release -- replay "$f" --guncelle; done

# Yeni golden oturum kaydet (seed ve eylemler komut satırından, sırayla):
dotnet run --project src/Yanki.Cli -c Release -- run samples/<senaryo>/scenario.json --seed 42 --eylem tur:hedef --tick 3 --kaydet tests/golden/<ad>.json
```

## Hash güncelleme kuralı

Hash güncellemesi **PR'da gerekçe ister**: PR açıklamasına hangi golden dosyanın, hangi davranış değişikliği yüzünden ve neden bilerek değiştiğini yaz
(eski hash → yeni hash). Gerekçesiz bir hash değişikliği determinizm kaybı sayılır.

Hedef: 3–5 oturum; biri Blazor WebAssembly çıktısıyla karşılaştırılır (plan bölüm 9).

**Durum:** 4. haftada ilk golden replay eklendi. Yenileri ilerleyen haftalarda.
