# Not: Olay kaydı (Events)

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## Ne yapıyor?

Oyundaki her değişiklik sırayla, silinmeyen bir günlüğe yazılır. Bir kayıt (`Olay`) şunları söyler: ne oldu (`Tur`), kim yaptı (`Fail`), kime ve nerede (`Hedef`, `Mekan`), ayrıntılar (`Yuk`), buna hangi olaylar yol açtı (`NedenOlaylar`) ve hangi kural üretti (`KuralId`).

Günlük her eklemede üç şeyi denetler: kimlikler ardışık mı, zaman geriye gidiyor mu, neden gösterilen olay gerçekten günlükte mi?

## Neden böyle?

- **Tek doğruluk kaynağı günlük** (ADR-001). Durum günlükten türediği için "bu neden oldu?" sorusu zinciri geriye izleyerek cevaplanır. Aynı günlük aynı durumu verir, golden replay ve geri sarma buna dayanır.
- **Kayıt değişmez.** `Yuk` ve `NedenOlaylar` kurulurken kopyalanıp sıralanır. Dışarıdan değiştirilemez ve aynı olay hep aynı biçimde yazılır.
- **Hata eklendiği anda patlar.** Yanlış bir neden sessizce geçseydi `why` sonradan yanlış cevap verirdi ve kaynağını bulmak zorlaşırdı.
- **`Neden()` kimliği azalan sırayla döner.** Nedenler her zaman daha küçük kimlikli olduğu için bu sıra zaten "yeniden eskiye" demektir, ek kural gerekmez.

## Claude'un verdiği, Yusuf'un henüz ayrıca onaylamadığı kararlar

- `Sira` her tick'te 0'dan başlar ve birer artar.
- `NedenOlaylar` artan sıralanır, tekrar hata verir. `Yuk` anahtara göre ordinal sıralanır, tekrar eden anahtar hata verir.
- `Neden()` olayın kendisini değil, yalnızca atalarını döndürür.

## Reddedilen yollar

- Değiştirilebilir durum + ayrı log: başlaması kolay ama ikisi zamanla ayrışır (ADR-001, Seçenek B).
- `Id`'yi günlüğün ataması: daha az hata riski var ama olayı günlükten bağımsız kurup doğrulamayı zorlaştırır.

## Açık

- `Neden()` her çağrıda yeniden hesaplar. 50 NPC ve bir haftalık oyun için sorun değil, 11. hafta benchmark'ı bakacak.
