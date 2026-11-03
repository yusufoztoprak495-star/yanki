# Not: Stres ve kriz

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır. Karar kaydı: ADR-012.

## Ne yapıyor?

Kişiliğe aykırı eylem **mümkün ama pahalıdır** (plan 5.4, CK3 / Dwarf Fortress): eylem tanımı hangi kişilik özellikleriyle çeliştiğini (`celisen_ozellikler`) ve bir maliyet tabanını (`stres_tabani`) bildirir.
Seçilince `stres += stres_tabani × Σ(çelişen özelliklerin kişilik değeri)` olur; stres eşiği geçince kişiliğe uygun bir **kriz olayı** çıkar, stres düşer ve kriz yeni bir olay zinciri başlatır.

## Stres

- Yazım adım 6.3'te `stres_degisti` olayıyla; `KuralId: eylem:<id>`, nedeni eylemin dış olayı (hareket eyleminde `geldi`, konuşmada ilk `anlatti`). Eylem seçilmediyse stres değişmez; çelişki/taban yoksa olay yazılmaz (hiç stres eklenmeyen senaryoda hash değişmez).
- Örnek: `yalan_soyle` (`stres_tabani: 15`, `celisen_ozellikler: ["durustluk"]`), dürüstlüğü 0.8 olan NPC → +12.
- Stres 0–100, yavaş söner (`StresSonumuTickBasina` 0.2/tick); sönüm sorgu anında hesaplanır, yazılan durum yoktur (ADR-008).
- Karar izi seçilenin `celisen_ozellikler`'ini ve uygulanacak `stres_artisi`'nı gösterir (yalnızca varsa); `yanki query` güncel stresi gösterir.
- `celisen_ozellikler` var ama `stres_tabani` yoksa doğrulayıcı reddeder.

## Kriz

- **Eşikler 50 ve 80** (`seviye` 1 = alt eşik, 2 = üst eşik; doğrulayıcı başka değeri reddeder), **kriz sonrası düşüş 30 ve 60** (`StresAyarlari`, denge alanı). Değişmez: her seviyede kriz sonrası stres en alt eşiğin altında kalır; yoksa her tick yeniden kriz çıkar.
  Üst seviye dışındaki her `i` için `esik[i+1] − dusus[i] ≤ esik[0]`, en üst seviye için `100 − dusus[son] < esik[0]` (50/30 ve 80/60 geçer, 50/20 ve 80/20 reddedilir). `Simulasyon.Olustur` aykırı ayarı reddeder.
- **Tespit adım 4'te, tek kural:** sönmüş güncel `stres(t) ≥ esik` ise o NPC için en üst uygun seviyede kriz seçilir (iki eşik birden aşıldıysa üstteki). Geçiş durumu tutulmaz; tick başına kriz için olay yazılmaz. Senaryo yüksek stresle başlarsa (ör. oturum yükleyen bir oyun) kriz ilk tick'te çıkar.
  Tam eşikte (`stres = 50`) kriz çıkar; eşiğin altında çıkmaz; sönümle eşiğin altına düşen stres kriz üretmez.
- Krizdeki NPC **o tick eylem seçmez** (`karar_verildi` yazılmaz). Kriz olayı seçilmiş eylem gibi adım 6 yolundan geçer: olay 6.1'de, algı 6.2'de adım 5 başındaki konum görüntüsüyle, satır etkileri ve stres düşüşü 6.3'te; sonuç kuralları sıradaki tick'te. Ayrı kuyruk yok.
- `KuralId: kriz:<id>`; `NedenOlaylar`: NPC'nin en yeni 5 stres ARTIŞ olayı (günlükte bulunamayan neden uydurulmaz). `why` krizden stres artışlarına, eylemlere (örn. `anlatti`) ve karar izine iner. Stres düşüşü olayı da `kriz:<id>`, nedeni kriz olayı.

## Kriz tablosu

Senaryo `kriz_dosyalari` ile JSON bağlar (yoksa kriz üretilmez, stres yine çalışır). Satır: `id`, `seviye`, `olay_turu`, `hedef`, `kriterler`, `etkiler`.

- **Hedef:** `yok`; `yalan_kurbani` (EN SON yalan söylenen kişi, aynı mekandaysa); `en_yakin` (aynı mekandaki, oyuncu dahil, en yüksek fikirli kişi); `en_soguk` (en düşük fikirli kişi). Eşitlikte kimlik ordinal. Hedefi aynı mekanda olmayan satır elenir.
- **Seçim:** kriterler ADR-010 gibi hepsi tutmalı; tutan satırlar arasında en özgül (ağırlık toplamı yüksek) kazanır, eşitlikte `AnahtarliRng.Icin(seed, tick, npc, "kriz")` (mevcut `Secici`). Kişilik farkı satır kriterleriyle yazılır: dürüst NPC `itiraf`, kinci NPC `patla`.
- **Yedek satır:** her seviye için kriterli olmayan bir satır zorunludur (doğrulayıcı reddeder). Yedeğin hedefi `yok` olmalıdır (uyarı); hedefli yedek elenirse hiç kriz çıkmaz ve NPC normal karar verir.
- Etkiler (`hafiza`, `modifier`, `eksen`, `duygu`, `gercek`) mevcut kural etkisi biçimidir; boş olabilir (kriz olayı ve stres düşüşü zaten etkidir). `eylem.hedef` yalnızca hedefli satırlarda geçerlidir.
- Hata biçimi: `dosya:satir: kriz "id": mesaj`.

## Karar (Yusuf, 6. hafta)

- **Karar (Yusuf):** eşikler 50 ve 80; düşüşler 30 ve 60; doğrulayıcı formülü `esik[i+1] − dusus[i] ≤ esik[0]` ve `100 − dusus[son] < esik[0]`; tespit `stres ≥ eşik` (geçiş durumu yok); `en_soguk` hedefi eklendi (kinci NPC'nin patlaması için); `en_yakin` ve `en_soguk` adaylarına oyuncu dahil.
- **Karar (Yusuf):** yalanın kanıt çelişkisinden otomatik yakalanması bu hafta yok; yalnızca itiraf krizi ve yazarın kuralları.

## Açık

- Itiraf kurbanın inancını düzeltmez; kurban yalanı yalnızca yazarın kurallarıyla (ör. `itiraf_etti` olayına `modifier` ve `duygu`) öğrenir.
- Denge sayıları (50/80, 30/60, taban 15) yer tutucudur; 11. hafta denge turunda ayarlanır.
- Kişilik ağırlıkları değişmez; stres yalnızca kriz seçimini ve davranış maliyetini etkiler, puan formülüne girmez.
