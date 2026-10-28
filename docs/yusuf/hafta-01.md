# 1. hafta — Yusuf'un işleri

Bu iki iş kod değil, tasarım. Projenin "beyni" bunlar; mülakatta "neden böyle?" sorusunun cevabı buradan çıkar.
Doldurduktan sonra commit at ya da Claude'a gönder.

## 1. Kriter dili sözlüğü (yaklaşık 15 sorgu fonksiyonu)

Kurallar ve diyaloglar bu fonksiyonlarla koşul yazacak (ADR-0003). Her biri bir soru sorar ve sayı ya da doğru/yanlış döner.
İlk üçünü örnek olarak doldurdum, gerisi senin.

| # | Fonksiyon | Ne sorar | Döndürdüğü | Örnek kriter |
|---|---|---|---|---|
| 1 | `iliski(hedef).guven` | Bu NPC hedefe ne kadar güveniyor | sayı (−100..100) | `iliski(oyuncu).guven < -20` |
| 2 | `biliyor(gercek)` | Bu NPC bu gerçeği biliyor mu (kanıt listesinde var mı) | doğru/yanlış | `biliyor(cinayet_silahi)` |
| 3 | `kisilik.acgozluluk` | Kişilik ekseni değeri | sayı (0..100) | `kisilik.acgozluluk > 70` |
| 4 | | | | |
| 5 | | | | |
| 6 | | | | |
| 7 | | | | |
| 8 | | | | |
| 9 | | | | |
| 10 | | | | |
| 11 | | | | |
| 12 | | | | |
| 13 | | | | |
| 14 | | | | |
| 15 | | | | |

Düşünmen için ipuçları: ruh hali, stres, mekan (aynı odada mı?), zaman (kaç tick önce?), kimden duydu, kaç kişi biliyor,
yalan söylendi mi, bir eylemi en son ne zaman yaptı.

## 2. Kasaba Hanı: 8 NPC'nin tek satırlık tanımı

Her NPC için: kim, ne istiyor, neyi saklıyor. Kişilikleri birbirine çarpacak şekilde seç (en az bir açgözlü, bir
dedikoducu, bir dürüst-ama-inatçı olsun).

| # | İsim | Rol | Ne istiyor | Neyi saklıyor | Kişilik (2–3 sıfat) |
|---|---|---|---|---|---|
| 1 | | Hancı | | | |
| 2 | | | | | |
| 3 | | | | | |
| 4 | | | | | |
| 5 | | | | | |
| 6 | | | | | |
| 7 | | | | | |
| 8 | | | | | |

## 3. Kontrol (5 dakika)

- [ ] `git clone` ile repoyu bilgisayarına çek, `dotnet test` çalıştır, yeşil gör.
- [ ] ADR'leri oku. Katılmadığın bir şey varsa şimdi söyle, 2. haftadan sonra pahalılaşır.
