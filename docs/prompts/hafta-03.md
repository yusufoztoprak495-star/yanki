<!-- Claude Code'a verilen görev metni, birebir. Hazırlandığı yer: claude.ai. Tarih: 29 Ekim 2026. -->

# Görev: Yankı — 3. hafta (Zihin ve doğrudan algı)

## Bağlam
Plan: docs/plan/Yanki Plan v2.pdf. Bu görev SADECE 3. hafta.
Önce şunları oku: planın 5.1, 5.3 (yalnızca doğrudan algı), 5.4, 5.9, 6 ve 9. bölümleri, CLAUDE.md, docs/criteria-language.md, ADR-001..007 ve docs/notes/.
`main`'i çek, `hafta-03` dalı aç.

Bu hafta YAZMA:
- kural motoru, tick döngüsü (4. hafta)
- karar/utility AI, eylem tanımları (5. hafta)
- "anlat" eylemi, duydu kanıtı aktarımı, konu sınırı, yalan kaydı, kriz olayları (6. hafta)
- frenler, korumalı bayraklar, yay değişkenleri (7. hafta)
- hedefler

## Değişmez kurallar (2. haftadakiyle aynı)
- Her şey `Yanki.Core` içinde, LangVersion 9.0, NuGet paketi yok, iki hedefte de derlenir.
- Determinizm: `System.Random`, `DateTime.Now`, `Guid.NewGuid`, `GetHashCode`/`HashCode` yasak. Sırasız `Dictionary`/`HashSet` gezintisi yasak (ordinal sıralı). `float`/`double` yasak, sayılar `decimal`. Tek RNG `AnahtarliRng`.
- Zihin durumu da olaylardan türetilir (event-sourced). Her değişiklik bir olaydır, `NedenOlaylar` ve `KuralId` taşır.
- `Olay` tipine yeni alan EKLEME.
- TDD: her parça için önce kırmızı test.
- Kod tanımlayıcıları ve JSON anahtarları ASCII.

## Teslimler

### 0. Proje durum dosyası (önce bunu yap)
- `docs/DURUM.md` oluştur. Bölümler:
  - Son güncelleme (tarih + PR)
  - Biten haftalar: her hafta için 3–5 madde, PR linkleri
  - Ertelenenler (Yusuf'un işi)
  - Açık kararlar (Yusuf'u bekleyen)
  - Sıradaki hafta
- İlk içerik:
  - 1. hafta (PR #1, #2): çözüm, CI + Codecov adımı, README iskeleti, CLAUDE.md, ADR-001..006, kriter sözlüğü v0.1 (15 fonksiyon).
  - 2. hafta (PR #3): olay kaydı (NedenOlaylar, KuralId), dünya (gerçekler, mekan grafiği), anahtarlı RNG, durum hash'i, kriter ayrıştırıcı/doğrulayıcı/seçici (eşitlikte özgül olan, sonra RNG), ADR-007 decimal, tools/golden_ref.py, 318 test.
  - Ertelenenler: ADR-001..007 "Gerekçe (Yusuf)" bölümleri; docs/notes taslaklarını Yusuf'un kendi cümleleriyle yeniden yazması; CODECOV_TOKEN secret'ı; Kasaba Hanı NPC'leri (4. haftada 3, 9. haftada 8).
- CLAUDE.md'ye kural ekle: "Her PR'da docs/DURUM.md güncellenir. PR bunsuz açılmaz."
- Bu hafta bitince DURUM.md'yi 3. hafta ile güncelle.

### 1. Mind modülü: kişilik (plan 5.4)
- Özellikler 0–1 decimal: `acgozluluk`, `durustluk`, `cesaret`, `sosyallik`, `kin_tutma`, `merhamet`.
- Senaryodan bir kez okunur, hiçbir olayla değişmez. Bunu test et.

### 2. İlişkiler
- Kişi başına 3 kalıcı eksen: `guven`, `sevgi`, `korku`. Aralık −100..100, sınır dışına taşmaz (kırp).
- Fikir modifier'ı alanları: tür, değer, başlangıç tick'i, süre (tick), neden olay kimliği.
  - Aynı türden kopyalar birikir. n'inci kopyanın etkisi `deger × 0.9^(n−1)`.
  - Tür başına birikme sınırı var (varsayılan 3, ayarlanabilir).
  - Süresi dolan modifier hesaba girmez.
- `Fikir(a, b, tick)` = eksenlerin ağırlıklı toplamı + aktif modifier'ların toplamı.
  - Eksen ağırlıklarını ayarlanabilir yap. Varsayılanı öner, açık noktalara yaz.
- Bantlar: `dusman < −40 ≤ soguk < −10 ≤ notr ≤ 10 < yakin ≤ 40 < dost`.
- `FikirDokumu(a, b, tick)`: her katkıyı neden olayıyla birlikte, sıralı döndürür. "Neden kızgın?" sorusunun cevabı bu.

### 3. Ruh hali ve stres
- Ruh hali, tek baskın duygudur: tür, şiddet (0–1), hedef (boş olabilir), neden olay.
  - Daha şiddetli yeni bir duygu gelirse baskın olur, eskisi `onceki` olarak tutulur.
  - Şiddet zamanla söner.
- Stres: 0–100 alanı, kırpılır, yavaş söner.
  - Bu hafta sadece alan, olay ve sönme var. Eylem bağlantısı ve kriz 6. haftada.

### 4. Kanıtlı inançlar
- İnanç = önerme + kanıt listesi. Kanıt alanları: tür (`gordu` / `duydu` / `cikarim`), kaynak, tick, güç (0–1).
- `Guven = 1 − Π(1 − guc)`, destekleyen kanıtlar üzerinden.
- Kanıt silinmez, gücü zamanla zayıflar.
- Çelişki iki durumda olur:
  - aynı anahtarlı tek değerli önermelerin (`!`) farklı değerleri,
  - `p` ile `degil(p)`.
  Çelişkide birikmiş kanıtı güçlü olan "inanılan" sayılır. Eşitlikte ne olacağını öner, açık noktalara yaz.
- `KimdenDuydu(npc, onerme)`: kanıtları sıralı döndürür.
- `*` joker önerme sorgularında her argümanda çalışır (2. hafta kararı).

### 5. Hafıza
- Kayıt alanları: olay kimliği + önem (0–1).
- Önem kalıcılık eşiğinin (varsayılan 0.7) üstündeyse kalıcıdır. Altındakiler zamanla silikleşir, eşik altına düşünce sorgulardan çıkar. Olay günlüğünden hiçbir şey silinmez.

### 6. Sönümleme
- Bütün sönmeleri saf fonksiyon olarak yaz: `(durum, simdikiTick) → deger`. Kapsam: modifier süresi, duygu şiddeti, stres, kanıt gücü, hafıza.
- Oranlar `ZihinAyarlari` içinde, varsayılanlarıyla. Hepsini açık noktalar listesine yaz.
- Tick döngüsüne bağlamak 4. haftanın işi.

### 7. Zihin olayları
- Olay türleri: `modifier_eklendi`, `eksen_degisti`, `duygu_degisti`, `stres_degisti`, `kanit_eklendi`, `hafiza_eklendi`.
- `Zihin.Uygula(olay)` ile durum ilerler, `Dunya.Uygula` ile aynı desende.
- Durum hash'ine zihin durumu da girer. Golden hash değişirse PR'da gerekçesini yaz.

### 8. Perception: doğrudan algı (plan 5.3, ilk madde)
- Görünürlük olayın `Yuk` alanında, `gorunurluk` anahtarıyla tutulur. Anahtar yoksa `acik`.
- Kimin algıladığı:
  - `acik`: olayın `Mekan`'ında bulunan bütün NPC'ler (`konum!` gerçeğine göre)
  - `sessiz`: yalnızca Fail ve Hedef
  - `gizli`: yalnızca Fail
- `DogrudanAlgi.Algilayanlar(dunya, olay)` sıralı NPC listesi döndürür.
- Her algılayan için iki olay üretilir:
  - `kanit_eklendi`, tür `gordu`, güç varsayılan 0.9, ayarlanabilir
  - `hafiza_eklendi`
  Her ikisinin `NedenOlaylar` alanında algılanan olay, `KuralId`'de `algi` yazar.
- Önerme şöyle türetilir:
  - `gercek_eklendi` → o gerçek
  - `gercek_kaldirildi` → `degil(gercek)`
  - diğer türler → `<tur>(fail, hedef)`

### 9. Kriter dili bağlantısı
- docs/criteria-language.md'deki fonksiyonlardan zihne dayananları (fikir, inanç, hafıza, ruh hali, stres vb.) gerçek veriye bağla. Her biri için test yaz.
- Sözlüğe yeni fonksiyon EKLEME. Gerekirse açık noktalara yaz.

### 10. Dokümanlar
- ADR-008: "Zihin olaylardan türetilir; süreli değerler sorgu anında tick'ten hesaplanır". Seçenekleri karşılaştır: tick başına sönme olayı / sorgu anında hesap. Durum "Önerildi", "Gerekçe (Yusuf)" boş.
- `docs/notes/zihin.md` ve `docs/notes/algi.md` taslakları. Bunlar sonradan Yusuf'un yeniden yazacağı taslaklar. Sonlarında "Açık noktalar" bölümü olsun: ağırlıklar, oranlar, eşikler, eşitlik kuralı.

## Bitiş
- Bütün testler iki hedefte yeşil, CI yeşil.
- Küçük, anlamlı commit'ler at. PR'ı aç: başlık "Hafta 3: Zihin ve doğrudan algı".
- PR açıklamasında şunlar olsun: teslim listesi, test sayısı, hash değiştiyse gerekçesi, ve benden karar bekleyen açık noktalar listesi.
- Bitince bana açık noktaları tek tek sor, merge etme.
