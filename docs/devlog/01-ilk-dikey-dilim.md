# Devlog #1: Oyuncunun yaptığı şey NPC'nin kafasında nasıl yankılanıyor

> **Yazar: Claude Code** (Yusuf'un isteğiyle). Aşağıdaki çıktılar `tests/golden/mini-han-1.json` oturumundan alındı.

Yankı'nın dört haftadır bir tek sözü var: oyuncunun yaptığı şey NPC'lerde iz bırakmalı ve motor o izin nereden geldiğini gösterebilmeli. Plan bu sözü 4. haftanın sonunda çalışan bir dilimle sınamayı istiyordu: oyuncu eylemi, algı, kural, fikir değişimi, `why` zinciri ve CI'da golden replay. Bu yazı o dilimin hikayesi.

## Üç NPC'lik sınav

`samples/MiniHan` bilerek küçük: handa hancı Ayşe ve demirci Can, pazarda gezgin Veli. Oyuncu handa, birinci tick'te Ayşe'ye yardım ediyor, ikinci tick'te Can'ı tehdit ediyor. Altı tick sonra üç NPC üç farklı yerde:

- **Ayşe** oyuncuya +10 bakıyor: yardımdan +15 minnet, gördüğü tehditten −5.
- **Can** −23 bakıyor. Cesareti 0.8 olduğu için tehdit onda öfke de doğuruyor. Üstelik yardımın hedefi o olmadığı için minneti yok.
- **Veli** pazarda olduğu için hiçbir şey bilmiyor.

Aynı eylem, üç kişide üç sonuç. Fark kodda değil, `kurallar.json` dosyasında ve NPC'nin neyi gördüğünde.

```text
fikir oyuncu: -23 (soguk)
  eksen:korku -3 ← #25
  modifier:tehdit_edildi -20 ← #23, kalan 164 tick
```

## "Neden?" sorusu

Asıl sınav şu: Can'ın −20'si nereden geldi? `yanki why` olay kimliğinden geriye yürüyor:

```text
#23 modifier_eklendi (tick 2, kural tehdit_cesur_ofkesi) @han deger=-20 hedef=oyuncu npc=Can sure=168 tur=tehdit_edildi
← #18 tehdit_etti (tick 2, kural player) oyuncu → Can @han
← #21 kanit_eklendi (tick 2, kural algi) @han guc=0.9 kaynak=Can npc=Can onerme=tehdit_etti(oyuncu,Can) tur=gordu
   ← #18 tehdit_etti (yukarıda)
← #22 hafiza_eklendi (tick 2, kural algi) @han npc=Can olay=18 onem=0.5
   ← #18 tehdit_etti (yukarıda)
```

Bu çıktı için ayrı bir log yazılmadı. Kural etkileri durumu doğrudan değiştirmiyor, yeni olay üretiyor ve her olay `KuralId` ile `NedenOlaylar` taşıyor. Zincir bu yüzden kendiliğinden var (ADR-001).

## Neden bir JSON okuyucu yazıldı?

Kurallar JSON, ama `Yanki.Core` hiçbir pakete bağımlı olamaz, çünkü Unity ve Blazor aynı kodu çalıştıracak. `System.Text.Json` `netstandard2.1` için paket istiyor. Çözüm küçük, bağımlılıksız bir okuyucu oldu: sayılar `decimal`, nesne sırası korunuyor, hatalar satır bilgisi taşıyor (ADR-009). Sınır testleri bir eksik de buldu: boyut sınırı yoktu. 8 MiB sınırı böyle geldi.

## Ne bozuldu, ne değişti?

İlk sürüm çalışıyordu ama inceleme üç yanlış çıkardı:

- **Kural tetiklenmesi belirsizdi.** Ağırlıklı puan mantığı kurala da uygulanmıştı ve "kriterlerden biri tutsa yeter" gibi okunuyordu. Karar: sonuç kuralı yalnızca tüm kriterler tutunca çalışır (ADR-010).
- **Tanık, hedef sanılıyordu.** Hedef ayrımı inançlara bakıyordu, yani daha önce tehdit edilmiş bir NPC, başkasına yapılan tehdidi görünce kendini hedef sanabiliyordu. Sözlüğe `olay_rolu(fail|hedef|tanik)` eklendi ve hata regresyon testiyle kilitlendi.
- **Hafıza kopya kaydediyordu.** Algı ve `hafiza` etkisi aynı olay için iki kayıt açıyordu. Artık tek kayıt var ve önem `max(eski, yeni)` oluyor. Can'ın tehdidi bu yüzden hafızasında 0.9 önemle duruyor.

Sonuncusu durumu değiştirdiği için golden hash de değişti: `504f3142d1dc15dd` → `cee9956ef06df17f`.

## Bir hash'e güvenmek

`mini-han-1.json` yalnızca seed'i, senaryoyu, oyuncu eylemlerini ve son hash'i saklıyor. CI her push'ta oturumu baştan oynatıp hash'i karşılaştırıyor:

```text
hash tutuyor: cee9956ef06df17f
```

Bu satırın anlamı şu: aynı seed ve aynı girdi aynı durumu verdi. Davranış istemeden değişirse CI kırılır. Bilerek değiştiyse `replay --guncelle` hash'i yeniler ve gerekçe PR'a yazılır. Yukarıdaki hafıza düzeltmesi tam olarak bu yolu izledi.

## Henüz yok

NPC'ler şu an yalnızca tepki veriyor, karar vermiyor. Oyuncu hareket edemiyor, dedikodu yok, kural zincirinin derinlik sınırı yok. Sıradaki büyük adım 5. hafta: utility AI, `IActionSelector` ve karar izi. O zaman NPC'ler de eylem üretmeye başlayacak.
