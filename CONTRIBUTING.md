# Katkı

Yankı şu an tek kişilik bir portfolyo projesi; katkılar henüz 12 haftalık planın (`docs/plan/`) dışında kabul edilmiyor.
Yine de çalışma şekli şöyle:

- **İş PR üzerinden ilerler.** `main`'e doğrudan commit yok; commit'ler küçük ve anlamlı.
- **Önce test.** Yeni davranış için önce kırmızı test.
- **Derleme ve test:** `dotnet build Yanki.slnx -c Release` (0 uyarı) ve `dotnet test Yanki.slnx -c Release`.
- **Çekirdek kuralları:** `Yanki.Core` dış pakete bağımlı olmaz; `netstandard2.1;net10.0`, C# 9.0. Determinizm kuralları için `CLAUDE.md`.
- **Mimari karar değişirse** yeni bir ADR yaz (`docs/adr/0000-sablon.md`).

Lisans: MIT.
