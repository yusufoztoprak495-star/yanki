# Not: Kayıt ve yükleme (7. hafta)

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## Ne yapıyor?

Oturum kaydı (`OturumKaydi`) seed + senaryo yolu + oyuncu eylem günlüğü + hash'lerdir; yükleme baştan oynatmadır (plan 5.1). `OturumOynatici.Oynat` TEK yükleme yoludur: simülasyonu kayıttaki eylemlerle `tick_sayisi`'na kadar oynatır, bekleyen eylemleri geri `Submit` eder, hash'leri karşılaştırır. Çekirdek dosya okumaz; CLI metin verir.

Sürüm 2 alanları (sürüm 1 okunur, denetim atlanır):
- `son_hash`: durum hash'i (değişmedi).
- `gunluk_hash`: bütün olay günlüğünün kanonik hash'i, `karar_verildi` ve fren olayları dahil. Seçici ya da karar izi değişince durum aynı kalsa bile replay'de yakalanır. Çıkış kodu 5.
- `senaryo_hash`: senaryo, kural, eylem ve kriz dosyalarının metinleri (CRLF/LF aynı). Tutmazsa yükleme `SenaryoDegistiHatasi` ile açık hata verir. Çıkış kodu 6.
- `bekleyen_eylemler`: `Submit` edilmiş ama `Tick` görmemiş eylemler. Yüklemede aynı sırayla geri `Submit` edilir, böylece "N tick koş, kaydet, yükle, M tick koş" = "N+M tick koş" (olay sırası, kimlikler, hash'ler birebir; testli ve CsCheck değişmezi).

CLI: `yanki devam <kayit.json> [--eylem ...] [--tick M] --kaydet <yeni.json>` baştan oynatır, doğrular, sürdürür ve YENİ kaydı yazar (eski dosya değişmez). Hash tutmazsa devam etmez. `replay --guncelle` ve `--hash-ekle` senaryo değişikliğini bilerek kabul eder; `--hash-ekle` yalnızca eksik hash'leri ekler ve `son_hash`e dokunmaz.

## Sınır

Özel ayarlar (`ZihinAyarlari`, `DedikoduAyarlari`, `StresAyarlari`, `FrenAyarlari`, `IActionSelector`) kayda girmez. v0.1'de yalnızca varsayılan ayarlar ve varsayılan seçici için garanti verilir; başka seçiciyle yüklenen kayıt `gunluk_hash` ile yakalanır.
Anlık görüntü (snapshot) v0.2'dir; yükleme süresi oynatma süresidir (`docs/notes/frenler.md` ölçümleri: gerçekçi 50 NPC fikstüründe 168 tick ≈ 2 dk, stres fikstüründe ≈ 31 sn).
