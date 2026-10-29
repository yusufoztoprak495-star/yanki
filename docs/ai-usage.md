# AI kullanımı

Bu projede AI kod aracını (Claude Code) açıkça ve bilinçli olarak kullanıyorum. Bu sayfa iş bölümünü şeffaf tutmak için var.

## Rol dağılımı

| Alan | Yusuf | Claude Code |
|---|---|---|
| Sistem tasarımı ve kurallar | Karar verir, onaylar, ADR'nin "neden" kısmını yazar | Seçenek ve taslak önerir, alternatifleri listeler |
| Kişilik / ilişki / stres modeli, denge sayıları | Belirler, coverage ve diff araçlarıyla deneyerek ayarlar | Ayar araçlarını yazar |
| Kriter dili sözlüğü (hangi sorgu fonksiyonları var) | Belirler | Ayrıştırıcı, doğrulayıcı ve testleri yazar |
| Demo senaryosu, NPC'ler, şablon metinleri (Türkçe ve İngilizce) | Yazar | Biçim, doğrulama, eksik varyant raporu |
| Kod | Okur, anlar, PR olarak inceler | Yazar |
| Testler | Senaryoları, değişmezleri ve golden oturumları tanımlar | Test kodunu yazar |
| Blazor demo arayüzü | Ekran akışını ve neyin gösterileceğini belirler | Yazar |
| Playtest | Düzenler, gözlemler, notları yazar | Sonuç tablosunu biçimlendirir |
| Dokümantasyon ve devlog | ADR gerekçeleri, devlog yazıları, README tanıtım metni | Biçim, taslak, API dokümanı |

## Kurallar

- Birleştirilen her değişikliği okuyorum ve anlatabilecek düzeyde anlıyorum.
- Her modül için `docs/notes/` altında kendi cümlelerimle bir not var.
- NPC kararları kural ve utility AI ile veriliyor; motorun içinde LLM karar vermiyor ([ADR-002](adr/0002-utility-ai-ve-iactionselector.md)).
- İş PR üzerinden ilerler; AI'nin yazdığı commit'ler `Co-Authored-By` satırı taşır.

## Çıktı nasıl doğrulanıyor

Testler (xUnit), golden replay hash'leri, benchmark'lar ve PR incelemesi.
