# tools/olcum

Karar ölçüm düzeneği (5. hafta, 8. haftada genişletildi). NPC kararlarının olay günlüğünü ve tick süresini ne kadar büyüttüğünü ölçer.

- **Çözümde değil:** `Yanki.slnx` bu projeyi içermez; `dotnet build Yanki.slnx`, CI ve `dotnet test` bunu derlemez ya da çalıştırmaz.
- **Benchmark değil:** BenchmarkDotNet'in ısınma ve istatistik titizliği yok. Sonuçlar yalnızca karar girdisidir (neyi değiştireceğimizi seçmek için); yayınlanacak benchmark tablosu **11. haftada** `tests/Yanki.Benchmarks` ile üretilir (plan bölüm 12).
- **Kriter:** v0.1 hız kriteri 8 NPC ortalama ≤ 5 ms'dir (9. haftaya kadar gerçekçi fikstürde, sonra gerçek Kasaba Hanı senaryosunda). 50 NPC v0.2 hedefidir; ölçülen değerler yayımlanır (ADR-015).
- **Fikstürler (her sonuç etiketiyle yazılır):**
  - **STRES** (varsayılan, `--dedikodu` ile dedikodulu): sahte içerik, 5 eylem, 3 kural; hedefsiz `dinlen` her zaman aday olduğu için her NPC her tick bir eylem seçer.
  - **GERÇEKÇİ** (`--gercekci`): 8 mekan, 9 eylem, 12 kural, dedikodu açık, NPC'ler çoğu tick boşta.
  - **BÜTÇE** (`--butce`): gerçekçi fikstür + toplam 15 eylem ve 30 kural (plan bütçesine yakın hacim; içerik sahte, Kasaba Hanı değil).
- **Zaman dökümü yok:** aşama aşama süre ölçümü için gereken geçici kod repoya alınmaz; yerelde yapılır ve commit edilmez ya da `dotnet-trace` kullanılır (repoya paket girmez).
- **Tek sayı yeterli değildir:** süreçler arasında çift mod görüldü (bazı süreçler 5–6 ms, çoğu 10–14 ms; ADR-015). Karar vermek için dönüşümlü sırayla en az 20'şer ayrı süreç ve bir A/A kolu kullanın; ortanca, en düşük, en yüksek ve p10–p90 yazın. Güç modu, şarj durumu ve açık uygulamaları kaydedin.

```bash
dotnet run --project tools/olcum -c Release                         # 3, 10 ve 50 NPC; 168 tick (stres fikstürü)
dotnet run --project tools/olcum -c Release -- --npc 10 --tekrar 3
dotnet run --project tools/olcum -c Release -- --gercekci --npc 8   # gerçekçi fikstür
dotnet run --project tools/olcum -c Release -- --butce --npc 8      # bütçe varyantı
dotnet run --project tools/olcum -c Release -- --gercekci --npc 8 --ozet   # süreç başına tek satır: OZET;npc;tekrar;ortanca;en_dusuk;en_yuksek;p95_tick;en_yavas_tick;koşular|cpu:...
dotnet run --project tools/olcum -c Release -- --gercekci --npc 8 --oynat  # OturumOynatici ile gerçek 168 tick baştan oynatma süresi
```

Çıktı Markdown'dır (`--ozet` ve `--oynat` hariç); `docs/notes/karar.md` ve `docs/notes/frenler.md` içindeki tablolar bu çıktıdan alınmıştır.
