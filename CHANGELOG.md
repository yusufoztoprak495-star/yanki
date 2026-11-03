# Değişiklik günlüğü

Biçim [Keep a Changelog](https://keepachangelog.com/tr/1.1.0/), sürümleme [SemVer](https://semver.org/lang/tr/).

## [Yayınlanmadı]

### Eklendi
- ADR-001..010 gerekçeleri, yeniden yazılmış modül notları ve Devlog #1 (Claude Code yazdı; ADR'ler Yusuf'un onayına kadar "Önerildi").
- 1. hafta iskeleti: `Yanki.Core` (modül klasörleri), `Yanki.Dialogue`, `Yanki.Cli` (`--version`), test projeleri, `Yanki.Benchmarks`.
- `netstandard2.1;net10.0` çoklu hedef, `Core` ve `Dialogue` için C# 9.0, merkezi paket yönetimi, deterministik derleme.
- GitHub Actions CI: derleme, test, trx artifact'ı, coverage ve Codecov yüklemesi.
- Mimari ve iskelet testleri (çekirdek yalnızca çerçeveye bağlı, her iki hedefte).
- ADR-001..006 (durum: önerildi), `CLAUDE.md`, `docs/ai-usage.md`, mimari ve kriter dili belgeleri, README iskeleti.
- 2. hafta: `Yanki.Core.Events` (`Olay`, `OlayGunlugu`: yalnızca sona ekleme, Id/Tick/Sira/nedensellik doğrulaması, `Neden` zinciri).
- 2. hafta: `Yanki.Core.World` (`Dunya`, `Gercek`, `GercekDeseni`; tek değerli `!` gerçekler, yönsüz mekan grafiği, dünya olaylarından türeyen durum, ASCII kimlikler).
- 2. hafta: anahtarlı RNG (`AnahtarliRng`: FNV-1a + SplitMix64), `Hash64`, `KanonikYazici` (`decimal`, `ToEven`, 6 basamak) ve `Dunya.DurumHash()`.
- 2. hafta: `Yanki.Core.Criteria` (ayrıştırıcı, 15 fonksiyonluk sözlük ve doğrulayıcı, `Kriter`, `ISorguCozucu`, `DunyaSorguCozucusu` (`gercek`, `ayni_mekanda`), `Secici.EnIyiSec` ve eşleşme izi). Kriterler C# ile kurulur; JSON okuma yok.
- 2. hafta testleri: determinizm, altın sabit (hash ve RNG, bağımsız hesapla doğrulanmış), yasaklı API taraması (`System.Random`, `DateTime.Now`, `double`/`float`, sırasız koleksiyonlar vb.), sözlük-docs eşitleme testi.
- ADR-007: simülasyonda `decimal` (durum: önerildi). `docs/notes/` altında `olay-kaydi.md`, `dunya.md`, `kriter-dili.md` taslakları.
- 3. hafta: `Yanki.Core.Mind` (`Zihin`, `ZihinDeposu`, `Kisilik`, ilişki eksenleri ve süreli fikir modifier'ları, `FikirDokumu`, tek duygulu ruh hali, stres, kanıtlı inançlar ve çelişki kuralı, hafıza, `Sonumleme` saf sönme fonksiyonları, `ZihinAyarlari`, altı zihin olayı, birleşik `DurumHashu`).
- 3. hafta: `Yanki.Core.Perception` (`DogrudanAlgi`: `acik`/`sessiz`/`gizli` görünürlük, `gordu` kanıtı ve hafıza olayı üretimi).
- 3. hafta: `ZihinSorguCozucusu`: sözlükteki 15 fonksiyonun 13'ü gerçek veriye bağlandı (`yalan_soyledi` 6., `yay` 7. hafta).
- 3. hafta: `VarlikTuru.Oyuncu` (`oyuncu` varlık türü; eşya değil). `inanc`/`kaynak` içinde `degil(...)`, tekrar eden kanıtın tazelenmesi, `FikirDokumu` kırpma satırı.
- ADR-008: zihin olaylardan türetilir; süreli değerler sorgu anında hesaplanır (durum: önerildi). `docs/notes/zihin.md`, `docs/notes/algi.md` taslakları. `docs/DURUM.md` proje durum dosyası (her PR'da güncellenir).
- 5. hafta: `Yanki.Core.Decision` — eylem tanımları (JSON, `EylemYukleyici` doğrulayıcı, `dosya:satir: eylem "id": mesaj`), `eylem.hedef` rolü (yalnızca eylem bağlamı), aday üretimi (`AdayUretici`), puanlama (`taban × Π(eğri) × kişilik × fikir × ruh hali`), decimal eğriler (`dogrusal`, `ustel`, `lojistik`, `basamak`; elle yazılmış `e^y`), `IActionSelector` ve `UtilitySelector` (ilk 3, %70, anahtarlı RNG), karar izi.
- 5. hafta: tick adımları 5 ve 6 (tek durum görüntüsü, eş zamanlı algı), NPC ve oyuncu hareketi (`ayrildi`, `geldi`, ayrılmış `hareket` eylemi), `karar_verildi` olayı (günlükte kısa iz; tam iz replay ile yeniden üretilir, `KararIziYenidenUretici`).
- 5. hafta: `yanki why` karar izine ve puan bileşenlerine iner, `yanki query` son kararı gösterir, `yanki run --eylem hareket:<mekan>`; CLI çıkış kodu 4 (iz uyuşmazlığı).
- 5. hafta: `samples/MiniKarar` senaryosu ve golden replay #2 (`tests/golden/mini-karar-1.json`); `tools/olcum` karar ölçüm düzeneği (çözüm ve CI dışı).
- 6. hafta: dedikodu (`anlatti`, `duydu` kanıtı aktarımı, kök ve yankı freni, `KonuHesabi` konu puanı/konu sınırı, aktarım kaydı `aktarim_kaydedildi`, `yalan_soyledi`), yalan gizliliği (doğru/yalan dışarıdan ayırt edilemez), oyuncu anlatımı (`PlayerAction("anlatti", ...)`), sözlük 18 fonksiyon (`konu_puani`, `olay_konusu`).
- 6. hafta: stres (`stres_tabani`, seçilen çelişen eylem `stres_degisti` yazar) ve kriz (`StresAyarlari`, `KrizYukleyici`/`KrizSatiri`, `kriz_dosyalari`, adım 4 tespit, adım 6 yolu); `yanki run --eylem "anlatti:...:onerme=..."`, `query` kök/aktarım, `why` krizden karar izine.
- 6. hafta: `samples/HanGerilim` senaryosu ve golden replay #3 (`tests/golden/han-gerilim-1.json`); `tools/olcum --dedikodu`; konuşan başına tick içi önbellek (50 NPC dedikodulu 3105.6 → 523.1 ms/tick); `.gitattributes` (LF) ve satır sonu testleri.
- ADR-012 (dedikodu, yalan, stres, kriz; durum: önerildi), `docs/notes/dedikodu.md`, `docs/notes/stres.md`; ADR-011 ve ADR-012 gerekçeleri (Yusuf'un devriyle Claude Code yazdı).
- ADR-011 (karar modeli, durum: önerildi), `docs/notes/karar.md`, `docs/authoring-guide.md` eylem yazımı ve `dinlen` önerisi, `docs/criteria-language.md` eylem bağlamı.

### Değişti
- 5. hafta: `Zihin.HafizaVar` olay kimliğine göre sıralı dizinle erken durur; hedefe bağlı olmayan eylem ön koşulları eylem başına bir kez hesaplanır (davranış ve durum hash'i değişmedi).
- 5. hafta: `RolAdlari.Ad` ve `SorguBaglami` tanımsız rolde sessizce `olay.hedef` dönmez; yasaklı API testi `Math.Pow/Exp/Log/Sqrt/Trig` kullanımını da yakalar.
- 5. hafta: `ayrildi` ve `geldi` yerleşik olay türleridir; `ayrildi`/`geldi` ve hedefi mekan olan olaylarda kural, kişi olmayan hedefe modifier/eksen/duygu yazmaz.
