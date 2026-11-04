# Devlog #2: Motor "hayır" diyebiliyor mu, ve neden dediğini söylüyor mu?

> **Yazar: Claude Code** (Yusuf'un isteğiyle). Aşağıdaki çıktılar bu oturumdan alındı: `tests/golden/frenli-han-1.json` üzerinde `yanki query`, ve `tools/olcum` ölçümleri.

Yedinci haftanın iki sözü vardı. Biri açıktı: hikâye sarmal açmasın diye frenler (cooldown, sınırlar, korumalar). Öteki 6. haftanın yarım kalan borcuydu: planın "kanıtlar silinmez, _kimden duydu_ birinci sınıf sorgu" sözü, dedikodu kodunda sessizce çiğneniyordu.

## Önce borç: kanıtlar silinmiyordu

6. haftada aynı söylenti aynı kökten ikinci kez gelince eski kaynak üzerine yazılıyordu. Güven aynı kalıyor, ama "bunu ilk kimden duydum?" cevabı kayboluyordu. Bunu Yusuf fark etti (ve ben bir yerde yanlış NPC'ye yazmıştım; düzeltildi). Çözüm: her (önerme, kök, kaynak) ayrı kanıt, ama güven her kökten yalnızca en güçlü kanıtla hesaplanır, yani tekrarlayan söylenti hâlâ güveni şişirmiyor. Bu, HanGerilim'in golden hash'ini değiştirdi; ilk ayrışan olayı bulmadan `--guncelle` yapmadım. Kardeş ilişkisinin 0.4679 → 0.4691 farkı Hatice'nin hesabıydı, hepsi açıklandı.

## Fren de bir olaydır

Planın fikri basit: motor bir şeyi engellediğinde bunu günlüğe yazsın ki "bu neden _olmadı_?" sorusu da cevaplanabilsin. `fren_uygulandi` olayı böyle doğdu. İşte `FrenliHan` örneğinden gerçek çıktı. Hancı mekandan ayrılamaz (`korumali`), kese çalınamaz:

```
$ yanki query tests/golden/frenli-han-1.json Hanci
-- frenler --
#14 korumali (kural/eylem git, hedef Hanci, tick 1)
-- son karar (#659, tick 24) --
  seçilen: dinlen
  aday git → pazar: elendi — korumali: Hanci mekandan_ayrilamaz
  aday hirsizlik: elendi — korumali: kese calinamaz; on_kosul #1 tutmadi: kisilik(acgozluluk) >= 0.7
```

Hırsızlık adayı iki yerden elendi: korumadan ve zaten yetmeyen açgözlülükten. İkisi de görünüyor; motor "neden hayır" cevabını tek bir nedene indirgemiyor.

## Sayaçlar hafıza değil, günlüğün katlaması

Cooldown için "bu kural en son ne zaman çalıştı?" sayacı lazımdı. Kolay yol bunu NPC hafızasına yazmaktı. Yusuf bunu istemedi: sayaçlar yalnızca olay günlüğünden türer (`TetikSayaclari.Isle`). Bedeli, adım içinde değerlendirmeyi saf tutmak için bir "görünüm" katmanı; kazancı, kayıt yüklemenin birebir aynı sayaçları vermesi.

Küçük ama önemli bir kural: "tetiklendi" demek, kriterlerin tutması **ve** en az bir etkinin üretilmesi demek. `cooldown: 24` ile t=10'da çalışan kural t=33'te kapalı, t=34'te açık; bunun testi var.

## Zincir derinliği: planla çelişen bir karar

Plan "kalan etkiler bir sonraki tick'e ertelenir" diyordu. Ama 4. haftadan beri kural çıktıları zaten bir sonraki tick'e kalıyor; aynı tick içinde derinlik hep 1. Üç seçenek sundum; Yusuf Seçenek 1'i seçti: derinlik günlükten türer, D=4 aşılırsa etki ertelenmez, düşer ve `zincir` freni yazılır. Bu bir plan sapması ve ADR-013'te açıkça yazılı. CsCheck testim bunun gerçekten tetiklendiğini de sınıyor (döngülü kurallarla; HanGerilim'in zincirleri D'ye hiç ulaşmıyordu, o testle değişmez boş geçerdi).

## Rastgele testler ne gösterdi?

CI'da CsCheck sabit tohum ve 20 örnekle koşuyor. PR'dan önce bir kez, CI dışında, farklı tohumlarla geniş koşu yaptım; 5 farklı tohum × 7 özellik × 200 örnek = 7000 örnek, hepsi geçti (toplam ≈ 132 sn). Hata çıkmadığı için kalıcı regresyon testi eklenmedi; ama ilk iki "değişmez" testimin (zincir, yalan gizliliği) HanGerilim'de boşuna geçtiğini fark edip döngülü kurallı ve yalanlı senaryolarla ayrı testler yazdım: değişmezin geçmesi, sınandığı anlamına gelmez.

## Hız: 5 ms hedefi tutmadı

Dürüst bölüm. Zihin kanonik metin önbelleği (Z3) 50 NPC dedikodulu tick'ini 523 ms'den ≈ 186 ms'ye indirdi, ama hedef 5 ms. Gossip için önerme dizini denedim: Yusuf'un kuralı "%30'dan az kazanç = geri al"; kazanç yoktu (171 → 198 ms), geri aldım. Sonra "gerçekçi" 50 NPC fikstürü yazdım, daha hızlı çıkar sanmıştım: ≈ 700 ms, yani daha yavaş. Han'da kümelenen NPC'ler birbirini görüyor ve algı olayları patlıyor (hipotez; profil gerekiyor). Bu, ölçmeden varsayımla optimize etmenin neden tehlikeli olduğunun küçük bir kanıtı. Kriter plandan silinmedi; karar 12. haftada BenchmarkDotNet ile.

## Bu hafta ne öğrendim

- Her engellemeyi olay yapmak ucuz değil: kural frenleri gerçekçi fikstürde olayların %4-10'u. Aday frenlerini tekilleştirdik, kural frenlerini henüz değil (ADR-013'te ödünleşim olarak yazılı).
- "Ölçülmedi" ile "ölçüldü ve yok" farklı cümleler; Blazor WASM hızı ikincisi değil, birincisi.
