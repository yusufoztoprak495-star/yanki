# Golden replay oturumları

Bu klasör, determinizmin kanıtıdır. Her golden oturum şunları saklar:

- **seed**
- **senaryo** (hangi başlangıç dünyası)
- **eylem günlüğü** (oyuncu eylemleri, tick sırasıyla)
- **beklenen son durum hash'i**

CI bu oturumları baştan oynatır ve son hash'in kayıttakiyle birebir tuttuğunu doğrular. Hash tutmuyorsa bir yerde
determinizm bozulmuştur (ya da davranış bilerek değişmiştir).

Hash bilerek değiştiğinde güncelleme tek komutla yapılır ve gerekçesi PR açıklamasına yazılır.

Hedef: 3–5 oturum; biri Blazor WebAssembly çıktısıyla karşılaştırılır (plan bölüm 9).

**Durum:** 🚧 İlk golden replay 4. haftada eklenecek. Şimdilik boş.
