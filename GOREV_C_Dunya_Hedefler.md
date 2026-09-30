# 🏚️ GÖREV DOKÜMANI — AJAN C: Dünya, Hedefler, Bulmacalar & Ses

> **Kim:** Üçüncü arkadaş + Claude Code (kendi bilgisayarında, repo klonu ile).
> **Rolün:** Oyunun **geçtiği yeri ve hedefini** yaparsın: KayKit asset'lerinin importu, prosedürel level generator'ı (`DungeonGenerator`), haritaya eşya/bulmaca yerleştiren populator, mühür bulmacaları, hazine ve kaçış, ışık, ses ve raund sonu ekranları.
> **Önemli:** Prosedürel level generator'ı (`DungeonGenerator`) **sen yazarsın ve kalıcıdır**: M0'da hızlı bir v1 (C0.4, A/B bloklanmasın), M3'te kalite & çeşitlilik v2 (C3.4). Bu, diğer görevlerinin (populator, hedefler, bulmacalar, ışık, ses, otopsi) **yanında** bir iştir, yerine değil. A ve B yalnızca §8 kontratına bağımlı.
> **Detaylı referans:** `docs/01_GDD_Oyun.md` §5, §6, §11 · `docs/02_GDD_Teknik.md` §7, §8 · `docs/04_Asset_Eslestirme.md` · `docs/03_TODO.md`.

---

## 1. Claude Code'a İlk Mesaj (kopyala-yapıştır)

```
Sen Cinli Mahzen projesinde AJAN C'sin (Dünya, Level, Hedefler, Bulmacalar, Ses, Otopsi).
Proje kökündeki CLAUDE.md'yi, GOREV_C_Dunya_Hedefler.md'yi ve docs/03_TODO.md'yi oku.
Sonra sıradaki [ ] görevimi başlat. Her görevde kabul kriterlerini Unity MCP ile doğrula
(Sandbox_C.unity sahnesinde), bitince commit + push yap ve bana kısa rapor ver.
```

---

## 2. Makine Kurulumu (bir kez)
1. Ajan A "repo hazır" deyince: `git clone https://github.com/Thawne1455/Cinli-Mahzen.git`.
2. Unity Hub → **aynı sürüm: 6000.3.18f1** → projeyi aç.
3. Unity MCP'yi bağla ve doğrula.
4. KayKit paketi repo ile gelir: `KayKit_Dungeon_Pack_1.1_FREE/` (repo kökü, ham kaynak).
5. **C0.1'e hemen başlayabilirsin** (asset import için kontrat gerekmez). C0.3 için A'nın A0.5'ini bekle.

---

## 3. Sorumluluk Alanın

| Alan | Klasör |
|---|---|
| Level kontratı, DungeonGenerator, Populator, LevelService, ışık | `Scripts/World/` (CM.World) |
| Mühürler, bulmacalar, kaplar, vault, altın, çıkış | `Scripts/Objectives/` (CM.Objectives) |
| Ses sistemi | `Scripts/Audio/`, `Audio/` |
| Otopsi, maç sonu, hedef bildirimleri UI'ı | `Scripts/UI/Round/`, `Prefabs/UI/Round/` |
| KayKit modelleri & materyal | `Art/KayKit/`, `Art/Materials/` |
| Ortam & hedef prefabları | `Prefabs/Environment/`, `Prefabs/Props/`, `Prefabs/Objectives/` |
| Level ayarları & loot | `ScriptableObjects/Level/` |
| Test sahnesi | `Sandbox_C.unity` |
| Mesaj kodları | `MsgCode` **100–139** aralığı |
| Event'ler | `Scripts/Core/Events/` altında kendi dosyaların (`// Owner: C`) |
| Doküman | `docs/04_Asset_Eslestirme.md` (ölçü tablosu) |

**❌ Dokunmadığın yerler:** `Scripts/Core` (event'lerin hariç) · `Game.unity` · `ProjectSettings` · A ve B'nin klasörleri · **B'nin possessable prefabları** (kap bileşenini runtime'da `AddComponent` ile eklersin).

---

## 4. Görev Sırası

### 🟥 M0 — Kurulum

| # | ID | Görev | Kabul |
|---|---|---|---|
| 1 | C0.1 | **KayKit import:** repo kökündeki `KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/` → `Art/KayKit/Models/`, texture → `Art/KayKit/Textures/`, tek materyal `M_KayKit_Dungeon` (URP Lit). `License.txt` da kopyalanır. **Ölçü raporu:** duvar, zemin, kapı, merdiven, raf, fıçı, sandık, sandalye bounds'ları → `04_Asset_Eslestirme.md §1` | Pembe model yok, **grid hücre boyutu kesinleşti** (beklenti 4 m) |
| 2 | C0.2 | **Prefablar:** Environment (duvar çeşitleri, zemin, kapı açıklığı, `wall_gated`, sütun, merdiven — `Environment` layer collider'lı, tutarlı pivot) + Props (collider'lı, possessable bileşeni **yok**) | Sandbox_C'de elle kurulmuş 3×3 oda, boşluk yok (screenshot) |
| — | 📣 | **B'ye haber ver: "Prop prefabları hazır."** B1.3 bunu bekliyor | |
| 3 | C0.3 | **Level kontratı** (Teknik §8.1–8.5): `ILevelGenerator`, `LevelLayout`, `RoomInfo`, tüm marker bileşenleri (gizmo'lu), `LevelValidator`, `LevelHash` | Validator testleri yeşil |
| — | 📣 | **A'ya haber ver: "Marker'lar hazır."** A1.5 (PawnSpawner) bunu kullanıyor | |
| 4 | C0.4 | **DungeonGenerator v1** (Teknik §7.2, kalıcı kod): 7×7 grid, 8-12 oda, MST + %20 ekstra koridor (döngü), `wall_doorway` ve `wall_gated`, tüm marker kuralları. Editör menüleri | Aynı seed → aynı hash · 50 seed → hepsi Validator'dan geçiyor · üstten screenshot |

### 🟧 M1 — Çekirdek Döngü

| # | ID | Görev | Kabul (MCP) |
|---|---|---|---|
| 5 | C1.1 | **LevelService:** raund başında temizle → üret → doğrula → NavMesh → populate → `LevelBuiltEvt`. `ILevelInfo` gerçek uygulaması | Raund değişince eski haritadan kalıntı yok |
| 6 | C1.2 | **LevelPopulator:** soket kategorilerine göre possessable (B'nin prefabları, Teknik §8.3 ağırlıkları), kaplar, altın, çıkış. Deterministik NetId (`1000 + sıra`) | Aynı seed → aynı NetId ↔ prefab eşlemesi (test) |
| 7 | C1.3 | **SearchableContainer** (`IInteractable`, runtime AddComponent) + `LootTable` SO. Önce `IInteractInterceptor` sorulur (mimik). Aranan kap görsel olarak boşalır | İnsan kabı arar → `ContainerSearched` |
| 8 | C1.4 | **Geçici hedef:** altın sandığı + taşıma + `ExitZone` → SeekersWin | İnsan altını çıkışa taşır → raund biter |
| 9 | C1.5 | **LightService** + oda ışıkları + karanlık atmosfer (ambient, fog, meşale point light'ları) | Screenshot: karanlık, turuncu meşaleler |

### 🟨 M2 — Tam Oynanış

| # | ID | Görev | Kabul (MCP) |
|---|---|---|---|
| 10 | C2.1 | **ObjectiveState** (`IObjectiveInfo`), fazlar (Keşif → Hazine → Kaçış), kaptan mühür parçası + iyi cine doğru kabın ruh parıltısı (SpiritOnly, 6 m) | F2'de parıltı var, F1'de yok |
| 11 | C2.2 | **Rün bulmacası:** 4 rün taşı + başka odada ruh ipucu duvarı (sadece iyi cin görür). Yanlış basış → sıfırlama + büyük gürültü | Doğru sıra → parça |
| 12 | C2.3 | **Hayalet izleri + kazı noktası:** NavMesh yolu boyunca ayak izleri (SpiritOnly), 3 sn kazma | İzler sadece F2'de |
| 13 | C2.4 | **Vault:** `wall_gated` 3 parça ile açılır, `chest_gold`, taşıma kuralları, hasar alınca sandık düşer, `GoldStateEvt` | Tam akış: 3 parça → kapı → altın → çıkış |
| 14 | C2.5 | Hedef bildirim banner'ları (role göre farklı metin) | |
| 15 | C2.6 | Süre dolumu → "Bekçi geldi" + son 60 sn kırmızı sayaç | |

### 🟩 M3 — Cila

| ID | Görev |
|---|---|
| C3.1 | **Otopsi Raporu** ekranı (ölüm sebebi, katil, raund istatistikleri, komik metinler) — A'nın StatsService'ini kullanır |
| C3.2 | **Maç Sonu** ekranı + unvanlar ("Mobilya Katili", "Tekmeci Dayı"...) |
| C3.3 | **AudioService** + `SfxLibrary` + tüm event'lere ses hook'u (placeholder ses; telgraf sesleri 3D ve öncelikli) |
| **C3.4** | ⭐ **DungeonGenerator v2 — kalite & çeşitlilik:** şekilli odalar / oda şablonları, graf mesafesiyle rol atama, oynanış sezgileri, `Batch Report (100 seeds)` → 100/100 geçer (Teknik §7.2 v2, §8.6) |
| C3.5 | Post-MVP eşya soketleri (Masa, Diken, Tabak) — prefab/aksiyon B'de |

### 🟦 M4 — Online (EN SON)

| ID | Görev |
|---|---|
| C4.1 | Seed senkronu + her istemcinin `LevelHash`'ini otoriteyle karşılaştırma (uyuşmazlık → hata ekranı) |
| C4.2 | Hedefler, ışıklar ve otopsinin online senkronu |

---

## 5. Beklediklerin → Beklerken Ne Yaparsın

| Kimden | Ne | Beklerken |
|---|---|---|
| A | **A0.5 Core kontratları** | C0.1 ve C0.2 (asset işleri kontrat gerektirmez) |
| A | A1.3 Interactor | Container mantığını düz C# + test olarak yaz |
| A | A1.6 Match (RoundSetup) | LevelService'i editör menüsüyle test et |
| A | A2.5 Stats/Score | Otopsi UI'ını sahte verilerle tasarla |
| B | B1.3 Possessable prefabları | Populator'ı düz prop prefablarıyla test et, sonra B'ninkilerle değiştir |
| B | B2.1 `IInteractInterceptor` | Stub interceptor (hep false) |

## 6. Teslim Ettiklerin → Kim Bekliyor

| Sen teslim edersin | Bekleyen | Neden |
|---|---|---|
| C0.1–C0.2 modeller/prefablar | B | Possessable prefabları |
| **C0.3 marker'lar** | A | Spawn noktaları |
| C1.1 LevelService / `ILevelInfo` | A, B | Maç akışı, cin uçuş sınırı |
| C1.5 `ILightService` | B | Meşale söndürme |
| C2.4 `GoldStateEvt` | A, B | İnsanın yavaşlaması, Öfke modu |
| **§8 Level kontratı** | A, B | Generator'dan bağımsız çalışmaları için (sadece `LevelLayout` / marker'lar) |

---

## 7. Dikkat Et
- **Determinizm her şeydir.** Online'da her oyuncu haritayı aynı seed'den kendisi üretir. `UnityEngine.Random` yok, sadece `GameRandom(seed)`. `Dictionary` / `HashSet` sırasına güvenme, listeleri sırala. Her üretim değişikliğinden sonra "aynı seed → aynı hash" testi.
- **Generator ile populator ayrı.** Generator sadece duvar, zemin ve marker üretir. Eşya, bulmaca, altın hep populator'ın işi. Generator'ı geliştirirken populator'a dokunmak gerekmez (kontrat sabit kaldıkça).
- Haritanın tasarım kuralları: `01_GDD_Oyun.md §11`. En az 1 döngü koridor, çıkış vault'tan ≥ 35 m, rün ipucu ile rün taşları farklı odalarda.
- Performans: tek gölgeli ışık insanın feneri olmalı. Meşale ışıkları gölgesiz.
- Harita üretimi + populate < 1.5 sn sürmeli.
