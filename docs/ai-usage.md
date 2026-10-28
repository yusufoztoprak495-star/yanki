# AI kullanımı

Bu projede AI kod araçlarını açıkça ve bilinçli olarak kullanıyorum. Bu sayfa iş bölümünü şeffaf tutmak için var.

## Kim ne yapıyor

| Alan | Yusuf | AI araçları (Claude, Claude Code) |
|---|---|---|
| Problem tanımı ve hedefler | ✅ | Araştırma ve karşılaştırma desteği |
| Mimari kararlar (ADR'ler) | ✅ Karar | Seçeneklerin ve ödünleşimlerin taslağı |
| Kriter dili sözlüğü, kişilik eksenleri | ✅ | Tutarlılık kontrolü |
| Demo senaryosu içeriği (Kasaba Hanı) | ✅ Ağırlıklı | Şablon ve doğrulama |
| Test senaryoları (neyi test edeceğiz) | ✅ | Test kodunun gövdesi |
| Uygulama kodu, CI, araçlar | İnceleme ve onay | ✅ Yazım |
| Dokümantasyon | Son hal, modül notları | İlk taslak |

## Kurallar

- Birleştirilen her değişikliği okudum ve anlattığımda anlayabileceğim düzeyde anlıyorum.
- Her modül için `docs/notes/` altında kendi cümlelerimle bir not var.
- NPC kararları kural ve utility AI ile veriliyor; motorun içinde LLM karar vermiyor ([ADR-002](adr/0002-utility-ai-ve-iactionselector.md)).
- AI ile yazılmış commit'lerde `Co-Authored-By` satırı bulunur.

## Araçlar

- Claude (sohbet): araştırma, plan, ADR taslakları
- Claude Code: uygulama, test, refactor
