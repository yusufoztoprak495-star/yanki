# ADR-0009: Çekirdekte bağımlılıksız JSON okuyucu

**Durum:** Önerildi
**Tarih:** 2026-11-02
**Kabul eden:** —
**Kabul tarihi:** —

## Bağlam

Kurallar, senaryolar, oturum (golden replay) dosyaları ve — ADR-003 uyarınca — kriterler JSON'dur. Kural motoru ve doğrulayıcı `Yanki.Core` içinde olmalı
(ADR-001, ADR-004; Unity ve Blazor aynı kodu kullanacak). Ama `Yanki.Core` `netstandard2.1;net10.0` hedefler ve hiçbir NuGet paketine bağımlı olamaz
(CLAUDE.md değişmez karar 5). `System.Text.Json` `net10.0`'da çerçevenin parçasıdır, `netstandard2.1` için paket gerektirir.
Ek olarak sayılar `decimal` olmalı (ADR-007) ve doğrulayıcı hataları satır numarası taşımalı.

## Karar

`Yanki.Core.Json` altında küçük, bağımlılıksız bir JSON okuyucu/yazıcı yazıldı: `JsonAyristirici` (katı RFC 8259; sayılar `decimal`, nesne özellikleri dosya sırasıyla,
tekrar eden anahtar hata, derinlik sınırı 64, satır/sütun bilgili `JsonHatasi`), `JsonDegeri` (değişmez ağaç, her düğümde kaynak konumu), `JsonYazici` (kanonik çıktı: 2 boşluk girinti, sabit sayı biçimi).
Çekirdek dosya sistemine dokunmaz; metni CLI okur ve çekirdeğe verir.

## Değerlendirilen seçenekler

### Seçenek A: Çekirdekte küçük özel JSON okuyucu (seçildi)

| Boyut             | Değerlendirme                                               |
| ----------------- | ----------------------------------------------------------- |
| Karmaşıklık       | Orta (~600 satır kod + testler)                             |
| Maliyet           | Bir kez yazılır, bakım azdır (JSON sabit bir biçim)         |
| Ölçeklenebilirlik | Dosyalar küçük (kurallar, senaryo); performans kritik değil |
| Bilinirlik        | Standart kütüphane değil, ama biçim standart JSON           |

**Artıları:** Her hedefte ve Unity'de aynı davranış; `decimal` sayılar kayıpsız; satır numarası doğrulayıcı hatalarına doğal akar; paket yok.
**Eksileri:** Sahip olunan ek kod; büyük/özel JSON özellikleri (yorum, NaN vb.) bilerek desteklenmiyor.

### Seçenek B: JSON yalnızca CLI'da (System.Text.Json), çekirdek C# modeli alır

**Artıları:** Çekirdek sade kalır. **Eksileri:** Unity/Blazor kullanıcısı JSON'u kendi okumak zorunda; "kod değiştirmeden genişler" ve doğrulayıcının satır bilgisi CLI'ya bağımlı olur; 7. haftadaki kayıt/yükleme aynı sorunla karşılaşır.

### Seçenek C: `System.Text.Json` paketini Core'a eklemek

**Artıları:** Hazır, bakımlı. **Eksileri:** Değişmez karar 5'i (NuGet bağımlılığı yok) ve ADR-004'ü bozar; `decimal` ve sıra koruma için yine sarmalayıcı gerekir.

## Ödünleşimler

Kendi okuyucumuzun bakım yükü, ADR-004'ün "sıfır bağımlılık" taahhüdü ve her hedefte aynı davranış karşılığında kabul edildi.

## Gerekçe (Yusuf)

> Bu gerekçeyi Claude Code, Yusuf'un isteğiyle plandan, görev metinlerinden ve PR kararlarından derledi. Yusuf onaylayana kadar ADR "Önerildi" kalır.

Unity ve Blazor aynı kuralları aynı şekilde okumalı, ADR-004 ise paket yasaklıyor. JSON'u CLI'ya bırakmak bu kullanıcıları kendi başına bırakır ve satır bilgili doğrulama hatalarını CLI'ya bağlar. Biçim sabit ve dosyalar küçük olduğu için özel okuyucunun bakım yükü düşük. Karşılığında her hedefte aynı davranış, kayıpsız `decimal` ve `dosya:satır` hataları geliyor. Yorum desteği gibi eklentiler bilerek dışarıda.

## Sonuçlar

- Kolaylaşan: Kural, senaryo, oturum dosyaları çekirdekte doğrulanır; Unity'de ek paket gerekmez; hatalar `dosya:satır` taşır.
- Zorlaşan: JSON biçimi hatalarının bakımı bizde.
- Yeniden bakılacak: JSON'a yorum/komut satırı özellikleri gerekirse (yazar deneyimi); büyük dosyalarda performans.

## Yapılacaklar

1. [x] `Yanki.Core.Json` ve testleri (4. hafta).
2. [ ] Yusuf gerekçeyi yazınca durum "Kabul edildi" yapılır.
