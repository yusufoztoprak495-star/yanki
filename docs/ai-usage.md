# AI kullanımı

Bu projede AI kod aracını (Claude Code) açıkça ve bilinçli olarak kullanıyorum. Bu sayfa iş bölümünü şeffaf tutmak için var.

## Rol dağılımı

| Alan | Yusuf | Claude Code |
|---|---|---|
| Sistem tasarımı ve kurallar | Karar verir, onaylar | Seçenek ve taslak önerir, alternatifleri listeler |
| Kişilik / ilişki / stres modeli, denge sayıları | Belirler, coverage ve diff araçlarıyla deneyerek ayarlar | Ayar araçlarını yazar |
| Kriter dili sözlüğü (hangi sorgu fonksiyonları var) | Belirler | Ayrıştırıcı, doğrulayıcı ve testleri yazar |
| Demo senaryosu, NPC'ler, şablon metinleri (Türkçe ve İngilizce) | Yazar | Biçim, doğrulama, eksik varyant raporu |
| Kod | Okur, anlar, PR olarak inceler | Yazar |
| Testler | Senaryoları, değişmezleri ve golden oturumları tanımlar | Test kodunu yazar |
| Blazor demo arayüzü | Ekran akışını ve neyin gösterileceğini belirler | Yazar |
| Playtest | Düzenler, gözlemler, notları yazar | Sonuç tablosunu biçimlendirir |
| Dokümantasyon ve devlog | Kararları verir, metinleri inceler, README tanıtım metni | Biçim, taslak, API dokümanı; ADR-001..010 gerekçeleri, 1–4. hafta modül notları ve Devlog #1 |

## Kurallar

- Birleştirilen her değişikliği okuyorum ve anlatabilecek düzeyde anlıyorum.
- Her modül için `docs/notes/` altında bir not var. 1–4. hafta notlarını Claude Code yazdı; **Karar (Yusuf)** satırları benim kararlarım.
- NPC kararları kural ve utility AI ile veriliyor; motorun içinde LLM karar vermiyor ([ADR-002](adr/0002-utility-ai-ve-iactionselector.md)).
- İş PR üzerinden ilerler; AI'nin yazdığı commit'ler `Co-Authored-By` satırı taşır.

## Karar devirleri

Bazı kararlar Yusuf'un devriyle AI tarafından verildi; Yusuf bunları kabul edip görev metnine aldı. Bunlar Yusuf'un doğrudan tasarımı gibi okunmamalıdır:

- **8. hafta (ADR-014 ve ADR-015):** A, E, F ve 5 ms kararları, Yusuf'un devriyle claude.ai'da Claude tarafından verildi; Yusuf bunları kabul edip görev metnine aldı. Bu iki ADR de Yusuf onaylayana kadar "Önerildi" kalır.

## Görev metinleri

Her haftanın Claude Code'a verilen görev metni [`docs/prompts/`](prompts/) altında duruyor. Metinleri Yusuf hazırlar ve birebir kaydedilir; Claude Code görev metni yazmaz. Böylece hangi işin AI'ye nasıl tarif edildiği açıkça görülebiliyor.

## Çıktı nasıl doğrulanıyor

Testler (xUnit), golden replay hash'leri, benchmark'lar ve PR incelemesi.
