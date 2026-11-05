# Diyalog katmanı (8. hafta): taslak not

> **Taslak:** Claude Code yazdı; Yusuf kendi cümleleriyle yeniden yazacak (CLAUDE.md). Karar gerekçeleri: [ADR-014](../adr/0014-diyalog-katmani.md).

## Bu modül ne yapıyor?

`Yanki.Dialogue`, simülasyonun yayınladığı olaylardan **söylenen satırlar** üretir. Bir olay (ör. Ayşe Can'a bir önerme anlattı) bir **niyet** olur (konuşan, dinleyen, niyet adı, konu, ton, fikir bandı);
niyetten, şablon dosyasındaki şablonlardan biri **kriter diliyle** seçilir; seçilen şablonun yuvaları (`#dinleyen#`, `#konu#`, `#hafiza.zaman#`) doldurulur ve satır oluşur. Her satırın "neden bu şablon?" sorusunun
cevabı vardır (`yanki diyalog … --neden <satır>`): tutan ve tutmayan kriterler, puanlar, tekrar kuralı, hafıza göndermesi.

## Neden böyle?

- **Olaydan niyet:** Karar modülü, anlatımın doğru mu yalan mı olduğunu bilir; niyet ondan türeseydi yalanı ele verirdi (ADR-012). Olay ve konuşanın zihni yeterlidir.
- **Ayrı şablon dosyası:** Metin düzeltmesi simülasyon durumunu ve eski kayıtları bozmasın. `sablon_hash` metnin, `diyalog_hash` çıkan satırların kanıtıdır.
- **Hash korunur:** Diyalog çekirdeğe yazmaz; açık/kapalı fark dört golden'ın hash'ini değiştirmez (test + CsCheck değişmezi).
- **Gramer elle:** Türkçe ek motoru küçük ve tablodur; kültüre bağlı `ToUpper` kullanılmaz. Bilinmeyen durumda sessiz yanlış cümle yerine hata verir (yumuşak gövde, zamir, ek sırası).
- **Sıra etkisi görünür:** Aynı iki eylem farklı sırada, konuşanın modifier'ları farklı olduğu için farklı satır seçer (SiraEtkisiSoylenti).

## Parçalar

| Dosya | Görev |
|---|---|
| `NiyetUretici.cs` | olay → niyet (kaynak oyuncu/npc, dinleyensiz, ton) |
| `Sablon/SablonYukleyici.cs` | JSON → doğrulanmış `SablonDosyasi`; tüm hatalar birlikte, `dosya:satir:` biçiminde |
| `DiyalogMotoru.cs` | abonelik, seçim, tekrar, hafıza, followup, hash |
| `Gramer/*` | `#kural#`, `#olgu.ek#`, Türkçe ekler, zaman ifadesi, önerme metni (zamirler) |
| `Renderer.cs` | `IDialogueRenderer` + varsayılan şablon renderer'ı |

## Bilinen sınırlar

Yalnızca Türkçe gramer; fiil çekimi yok (özne varyantları elle yazılır); ton eşikleri yer tutucudur (11. hafta); gerçek Kasaba Hanı şablon kümesi 9. haftada.
