# Not: Olay kaydı (Events)

> **Taslak (Claude Code).** Yusuf bunu kendi cümleleriyle yeniden yazacak. Aşağıdakiler "ne yaptım, neden" iskeletidir.

## Bu modül ne yapıyor?

Oyundaki her değişikliği sırayla, silinmeyen bir günlüğe yazıyor. Her kayıt (`Olay`) şunu söylüyor: ne oldu (`Tur`), kim yaptı
(`Fail`), kime/nereye (`Hedef`, `Mekan`), ayrıntılar (`Yuk`), **hangi olaylar buna yol açtı** (`NedenOlaylar`) ve hangi kural
üretti (`KuralId`). Günlük, eklenen her olayı denetliyor: kimlikler ardışık, zaman geriye gitmiyor, neden olarak gösterilen
olay zaten günlükte.

## Neden böyle?

- **Günlük tek doğruluk kaynağı** (ADR-001). Dünya durumu günlükten türetilir; böylece "bu neden oldu?" sorusu nedensellik
  zincirini geriye izleyerek cevaplanır ve aynı günlük yeniden oynatılınca aynı durum çıkar (golden replay, geri sarma).
- **Değişmez kayıt, kopyalanan koleksiyonlar.** `Yuk` ve `NedenOlaylar` kurulurken kopyalanıp sıralanıyor; dışarıdan
  değiştirilemiyor. Sıralı olması, aynı olayın hep aynı serileştirilmesini sağlıyor (determinizm).
- **Katı doğrulama.** Yanlış bir neden (günlükte olmayan olay) ya da geriye giden tick sessizce geçse, `why` zinciri sonradan
  yanlış sonuç verirdi. Hatayı eklendiği anda patlatmak, hatayı kaynağında bulmayı kolaylaştırıyor.
- **`Neden()` Id azalan sırada döner.** Nedenler her zaman daha küçük Id'li olduğu için bu sıra "yeniden eskiye" geçerli bir
  sıralama ve ek bir sıralama kuralı gerektirmiyor.

## Benim (Claude'un) verdiğim kararlar — Yusuf onaylasın

- `Sira` her tick'te 0'dan başlar ve tam birer artar ("artar"dan daha sıkı).
- `NedenOlaylar` artan sıralanır, tekrar hata verir; `Yuk` anahtara göre ordinal sıralanır, tekrar eden anahtar hata verir.
- `Neden()` olayın kendisini döndürmez; yalnızca atalarını döndürür. Ağaç yapısı (hangi neden hangisine bağlı) gerekirse ayrı bir
  fonksiyon olarak `why` aracıyla birlikte eklenir.

## Alternatifler

- Değiştirilebilir durum + ayrı log (ADR-001, Seçenek B): başlaması kolay, ama log ile durum zamanla ayrışır.
- `Id`'yi günlüğün kendisinin atamasına bırakmak: daha az hata riski, ama olayların günlükten bağımsız kurulup doğrulanmasını
  zorlaştırır; şimdilik dışarıdan verilip doğrulanıyor.

## Emin olmadığım / Yusuf'un bakması gereken noktalar

- `Tur` değerleri (olay türü kataloğu) henüz yok; sonuç kuralları (4. hafta) gelince sabitlenecek.
- Büyük günlüklerde `Neden()` her çağrıda yeniden hesaplar; 50 NPC / bir hafta için sorun değil, benchmark (11. hafta) bakacak.
