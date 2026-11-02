# ADR-0010: Kural yalnızca tüm kriterleri tutunca çalışır

**Durum:** Önerildi
**Tarih:** 2026-11-02
**Kabul eden:** —
**Kabul tarihi:** —

## Bağlam

ADR-003 kriterlere ağırlık ve `zorunlu` bayrağı verir; puan, tutan kriterlerin ağırlık toplamıdır. Bu, birden fazla adayın yarıştığı yerler (eylem seçimi, diyalog şablonu) için doğrudur.
Sonuç kuralında ise tek bir "aday" vardır ve soru "bu kural çalışsın mı?"dır. 4. haftanın ilk sürümü `zorunlu` elenmemiş ve puan > 0 koşulunu kullanıyordu;
bu, "kriterlerden biri tutunca çalış" gibi okunabilen belirsiz bir davranıştı.

## Karar

Bir sonuç kuralı **yalnızca tüm kriterleri tutunca** çalışır. Kriteri olmayan kural, her tetikleyici olayda (olayı algılayan her NPC için) çalışır.
`agirlik` bu kullanımda sonucu etkilemez; doğrulayıcı `agirlik` kullanan kural için **uyarı** verir (hata değil; kural yüklenir). `zorunlu` da anlamsızdır (tüm kriterler zaten zorunludur) ve yok sayılır; `"zorunlu": false` yazan kriter için doğrulayıcı ayrıca **uyarı** verir ("tüm kriterler zaten zorunludur"), kural yüklenmeye devam eder. `"zorunlu": true` uyarı vermez.
Ağırlıklı puanlama eylem seçimi ve diyalog şablonu için aynen kalır (5. ve 8. hafta).

## Değerlendirilen seçenekler

### Seçenek A: Tüm kriterler tutmalı (seçildi)

| Boyut             | Değerlendirme                                      |
| ----------------- | -------------------------------------------------- |
| Karmaşıklık       | Düşük                                              |
| Maliyet           | Küçük kod değişikliği                              |
| Ölçeklenebilirlik | Kural sayısı arttıkça davranış öngörülebilir kalır |
| Bilinirlik        | Çoğu kural motorunda bilinen "ve" anlamı           |

**Artıları:** Kural yazarı için açık ("bu koşulların hepsi"); `why` çıktısında neden çalıştığı belli. **Eksileri:** "Ya da" ifadesi için ayrı kural yazmak gerekir.

### Seçenek B: Zorunlular tutmalı ve puan > 0 (ilk sürüm)

**Artıları:** ADR-003 modeline yakın. **Eksileri:** Ağırlıklar ve `zorunlu` yazarın beklentisini çoğu zaman yanıltır; "biri tutsa yeter" sürprizi.

### Seçenek C: Kuralda eşik puanı

**Artıları:** Esnek. **Eksileri:** Yeni bir denge sayısı ve alan; sonuç kuralı için gereksiz karmaşıklık.

## Ödünleşimler

"Ya da" ve kısmi eşleşme ifade gücü, öngörülebilirlik ve basitlik karşılığında bırakıldı.

## Gerekçe (Yusuf)

> Bu gerekçeyi Claude Code, Yusuf'un isteğiyle plandan, görev metinlerinden ve PR kararlarından derledi. Yusuf onaylayana kadar ADR "Önerildi" kalır.

PR #7 incelemesinde sorun çıktı: "zorunlu elenmedi ve puan > 0" koşulu, kural yazarının "hepsi tutmalı" beklentisiyle çelişiyordu. Sonuç kuralında yarışan aday yok, tek soru var: çalışsın mı? Bu yüzden Yusuf "tüm kriterler tutmalı" kararını verdi. Ağırlıklı puan, adayların yarıştığı eylem seçimi ve diyalogda kalıyor. Yerine "ya da" için ayrı kural yazmak gerekiyor; okunabilirlik ve test edilebilirlik bu bedele değiyor.

## Sonuçlar

- Kolaylaşan: Kuralın neden çalıştığını okumak ve test etmek.
- Zorlaşan: Alternatif koşullar için birden fazla kural.
- Yeniden bakılacak: Diyalog şablonlarında ve eylem ön koşullarında aynı yorumun uygun olup olmadığı (orada ağırlıklı puan kalıyor).

## Yapılacaklar

1. [x] `KuralMotoru` ve doğrulayıcı uyarıları (`agirlik`, `zorunlu: false`) (4. hafta).
2. [ ] Yusuf gerekçeyi yazınca durum "Kabul edildi" yapılır.
