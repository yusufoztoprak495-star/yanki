# tools/olcum

Karar ölçüm düzeneği (5. hafta). NPC kararlarının olay günlüğünü ve tick süresini ne kadar büyüttüğünü ölçer.

- **Çözümde değil:** `Yanki.slnx` bu projeyi içermez; `dotnet build Yanki.slnx`, CI ve `dotnet test` bunu derlemez ya da çalıştırmaz.
- **Benchmark değil:** BenchmarkDotNet'in ısınma ve istatistik titizliği yok. Sonuçlar yalnızca karar girdisidir (neyi değiştireceğimizi seçmek için); yayınlanacak benchmark tablosu 11. haftada `tests/Yanki.Benchmarks` ile üretilir.
- **Stres fikstürü:** sahte içerik (Kasaba Hanı değil), 5 eylem, 3 kural, sabit seed'li kişilikler. Hedefsiz `dinlen` eylemi her zaman aday olduğu için **her NPC her tick bir eylem seçer**; karar adımı ve olay yazımı olabildiğince yoğundur. Gerçek senaryoda NPC'ler çoğu tick'te bir şey yapmayabilir.
- **Zaman dökümü yok:** aşama aşama süre ölçümü için gereken geçici kod repoya alınmaz; yalnızca toplam tick süresi ölçülür.

```bash
dotnet run --project tools/olcum -c Release                     # 3, 10 ve 50 NPC; 168 tick
dotnet run --project tools/olcum -c Release -- --npc 10 --tekrar 3
```

Çıktı Markdown'dır; `docs/notes/karar.md` içindeki önce/sonra tablosu bu çıktıdan alınmıştır.
