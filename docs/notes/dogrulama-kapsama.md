# `yanki validate` ve `yanki coverage` (9. hafta taslak notu)

> Taslak (Claude Code). Yusuf kendi cümleleriyle yeniden yazar.

## Ne yapıyorlar

- **validate:** bir senaryo dizinindeki bütün dosyaları (senaryo, kural, eylem, kriz, şablon, oyuncu menüsü) mevcut yükleyicilerle okur ve hata/uyarıları `dosya:satir: tur "id": mesaj` biçiminde, dosya ve satır sırasıyla, hepsi birlikte listeler. Ek denetimler: kullanılmayan kişilik özelliği (eylem ölçütünde yok / diyalog kriterinde yok: iki ayrı uyarı), ulaşılamaz şablon (beş statik tanım), senaryoda hiçbir yerde geçmeyen korumalı kimlik, senaryoda olmayan `aktorler` kimliği, oyuncu menüsünün `Submit`'te geçerli olması.
- **coverage:** her oyuncu betiği × seed ve oyuncusuz koşu için senaryoyu `--tick` (varsayılan 168) tick koşar; olay günlüğünden ve diyalog satırlarından kural, eylem, kriz satırı, fren türü ve şablon başına sayım çıkarır. Plan 3 kriteri: her sonuç kuralı en az bir kez tetiklenir, hiçbiri tetiklenmelerin %30'undan fazlasını almaz (çıkış kodu 2 kırmızı).

## Neden böyle

- Doğrulama ve kapsama mantığı `Yanki.Authoring` derlemesindedir (metin + dosya adı alır, dosya okumaz): CLI dosyayı okuyup çağırır, 10. haftada Blazor demosu aynı kodu kullanabilir. Ayrı bir ayrıştırma yolu yoktur; mevcut yükleyiciler çağrılır.
- Sayım yalnızca günlükten türer: simülasyona sayaç ya da olay eklenmedi (olay biçimi ve hash'ler değişmez). Aynı girdi aynı raporu verir (CRLF/LF ve tr-TR kültüründe de).
- "Ulaşılamaz şablon" yalnızca statik tanımdır; koşuya bakan "hiç seçilmedi" denetimi coverage'ın işidir ve raporda üç gruba ayrılır (konsepti satır üretmedi / genel şablon, özgül kazandı / özgül şablon tutmadı).
- Betik eylemi dünya durumu yüzünden geçersizse (ör. hedef başka mekana gitmiş) oyuncu o eylemi yapamaz sayılır: atlanır ve raporda sayılır.

## Denge döngüsü

Yaz → `validate --uyari-hata` → `coverage --seeds 20` → ayarla. Kural başına kriz tutuyorsa bile tohum dengesine bak: Kasaba'da hırsızlık tohumu payı %59.9 iken kural başına en büyük pay %24.0 idi (11. hafta denge turu açık maddesi).
