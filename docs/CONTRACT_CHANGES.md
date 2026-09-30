# Kontrat Değişiklik Kaydı

> `02_GDD_Teknik.md` §2 (layer'lar), §4 (Core kontratları), §8 (Level kontratı) ve `MsgCode` başka ajanın aralığındaki değişiklikler buraya **önce** yazılır.
> Diğer ajanlar oturum başında bu dosyayı okur.

| Tarih | Ajan | Değişiklik | Etkilenen ajanlar | Neden | Durum |
|---|---|---|---|---|---|
| 2026-09-30 | — | İlk sürüm (GDD v1.0) | — | — | ✅ |
| 2026-09-30 | B | `GameBalanceConfig`'e `NetRangeTolerance = 0.5` (m) alanı: otoritenin menzil doğrulamalarında (possession, etkileşim, kovma) istemciye tanınan pay | A (config, Interactor), B | Teknik §4.5 ve §6.2'deki "+0.5 tolerans" sihirli sayı olmasın. B şimdilik `PossessionRules.RangeTolerance` ile dışarıdan alıyor | 🟡 önerildi |
| 2026-09-30 | B | **Bilgi (değişiklik değil):** B0.1 kodu §4.1/§4.5 imzalarına birebir dayanıyor: `CinliMahzen.Core` içinde `PlayerId(byte)` ctor + `PlayerId.None`, `NetId(int)` ctor + `.Value`, `Role`, `DamageFlags` (None0 Knockdown1 Grab2 Lethal4 IgnoreInvuln8), `StatusType` (None, Knockdown, Grabbed, Drunk, Darkness, Slowed, Stunned). `PD_*.asset`'ler bu enum'ların **int değerlerini** saklıyor → sıra değişirse asset'ler bozulur | A | A0.5 yazılırken sözleşmeden sapma olursa B'ye haber verilsin | ℹ️ bilgi |
| 2026-09-30 | — (proje sahibi) | **Level generator kararı:** Harici generator gelmeyecek; `DungeonGenerator` C'nin kalıcı görevi (C0.4 = v1, C3.4 = v2 kalite & çeşitlilik). §8 başlığı/§8.6 güncellendi, `World/External/` ve `GeneratorType = External` kaldırıldı, §8.5 fallback → `LevelGenSettings.FallbackSeed`. Menü: `Generate Map (Random Seed)` + `Batch Report (100 seeds)`. §8.1–8.4 arayüzleri **değişmedi** | C (A, B: bilgi) | Proje sahibinin kararı | ✅ |
