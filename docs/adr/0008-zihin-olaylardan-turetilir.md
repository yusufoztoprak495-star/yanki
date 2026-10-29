# ADR-0008: Zihin olaylardan türetilir; süreli değerler sorgu anında tick'ten hesaplanır

**Durum:** Önerildi
**Tarih:** 2026-10-29
**Kabul eden:** —
**Kabul tarihi:** —

## Bağlam

Zihin (plan 5.4) zamanla değişen değerler taşır: fikir modifier'ları süresi dolunca düşer, duygu şiddeti söner, stres yavaşça düşer,
kanıtlar zayıflar, önemsiz anılar silikleşir. ADR-001 durumun olay günlüğünden türetilmesini şart koşuyor, durum hash'i de
(plan 5.1) her tick sonunda alınıyor. Soru: bu zamana bağlı değişimleri nasıl temsil edeceğiz?

Etkili kuvvetler: (1) Aynı seed + aynı girdi = aynı hash (ADR-001, ADR-007). (2) 10–50 NPC ölçeği, performans baskısı düşük.
(3) "Neden?" zinciri: bir modifier'ın düşmesi bile açıklanabilir olmalı. (4) 4. haftada tick döngüsü yazılacak; zihin bu döngüye
bağımlı olmadan test edilebilmeli.

## Karar

Zihnin durumu yalnızca **zihin olaylarıyla** ilerler (`modifier_eklendi`, `eksen_degisti`, `duygu_degisti`, `stres_degisti`,
`kanit_eklendi`, `hafiza_eklendi`); her biri `NedenOlaylar` ve `KuralId` taşır. Zamana bağlı değerler **saklanmaz, sorgu anında
`(durum, simdikiTick) → değer` saf fonksiyonlarıyla hesaplanır** (`Sonumleme`). Durum, ham değeri ve başlangıç tick'ini tutar
(ör. stres: değer + ölçüldüğü tick); sönmüş değer hash'e girmez. Böylece sorgu tick'i hash'i etkilemez.

## Değerlendirilen seçenekler

### Seçenek A: Tick başına sönme olayı

Her tick'te her NPC için "modifier düştü", "stres azaldı", "duygu söndü" gibi olaylar günlüğe yazılır.

| Boyut             | Değerlendirme                                                                                             |
| ----------------- | --------------------------------------------------------------------------------------------------------- |
| Karmaşıklık       | Orta: her sönme için olay tipi, tick döngüsüne sıkı bağlılık                                              |
| Maliyet           | Günlük hızla şişer: 50 NPC × tick başına birkaç olay; bir haftalık oyun (≈168 tick) için on binlerce olay |
| Ölçeklenebilirlik | Zayıf: günlük boyutu NPC sayısı × tick ile büyür; replay yavaşlar                                         |
| Bilinirlik        | Yüksek (klasik "update" mantığı)                                                                          |

**Artıları:** Her değişim günlükte açıkça görünür; sönmenin kendisi de kural tetikleyebilir ("öfken yatıştı" olayı).
**Eksileri:** Günlük gürültüyle dolar; golden replay dosyaları şişer; "neden?" zincirleri anlamsız sönme olaylarıyla uzar;
tick döngüsü olmadan zihni test etmek zorlaşır.

### Seçenek B: Sorgu anında hesap (seçilen)

Sönmeyi tanımlayan formül ve oranlar `ZihinAyarlari`'nda; değer `Sonumleme` saf fonksiyonlarıyla her sorguda hesaplanır.

| Boyut             | Değerlendirme                                                                  |
| ----------------- | ------------------------------------------------------------------------------ |
| Karmaşıklık       | Düşük–orta: saf fonksiyonlar, tek yerde                                        |
| Maliyet           | Sorgu başına küçük hesap (modifier/kanıt sayısı kadar); 10–50 NPC için önemsiz |
| Ölçeklenebilirlik | İyi: günlük yalnızca gerçek olaylarla büyür                                    |
| Bilinirlik        | Orta (CK3 modifier süreleri benzer)                                            |

**Artıları:** Günlük küçük kalır; zihin tick döngüsünden bağımsız test edilir; hash sorgu tick'inden etkilenmez;
geri sarma ucuz (tick'i değiştirip sorgula).
**Eksileri:** "Eşik aşıldı" tipi tetikleyiciler (ör. stres 50'yi geçti) artık olay üretmez; 4. haftadaki tick döngüsü eşik geçişini
önceki ve sonraki tick değerini karşılaştırarak kendisi bulup olay üretmeli. Sönme formülü değişirse eski replay'ler farklı sonuç
verebilir (günlük biçim sürümüyle yönetilir).

## Ödünleşimler

B günlüğü sade tutar ama "değer eşiği geçti" bilgisi örtük kalır; kriz olayları (6. hafta) bu geçişi tick döngüsünde açıkça bir
olaya çevirecek. Doğrusal sönme (değer − oran × geçen tick) decimal ile birebir ve platformdan bağımsız hesaplanır;
üstel sönme `Math.Pow` ister ve ADR-007'yi zorlar. Doğrusal sönmenin ödünleşimi: uzun vadede "hafif kalıntı" yok, değer sıfıra
tam iner.

## Gerekçe (Yusuf)

## Sonuçlar

- Kolaylaşan: zihin tek başına test edilir; günlük ve golden dosyalar küçük kalır; `FikirDokumu` her tick için yeniden üretilir.
- Zorlaşan: eşik geçişi olayları tick döngüsünde elle üretilmeli (4–6. hafta); sönme formülü bir sözleşmedir, değişirse replay uyumu düşünülmeli.
- Yeniden bakılacak: Doğrusal yerine üstel/yarı ömür sönmesi gerekirse (yeni ADR); tick'in oyun içi süreye eşlemesi (1 tick = 1 saat mi?).

## Yapılacaklar

1. [x] 3. hafta: `Sonumleme` saf fonksiyonları, `ZihinAyarlari`, altı zihin olayı.
2. [ ] 4. hafta: tick döngüsü sönme adımını sorgu tick'iyle bağlar; eşik geçişi olay üretimi.
3. [ ] 6. hafta: stres 50/80 eşiği geçişinde kriz olayı.
