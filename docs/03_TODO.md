# CİNLİ MAHZEN — Görev Listesi (TODO)

> Sürüm: **v2.1 — 2026-10-15** (`01_GDD_Oyun.md` v2.1 ve `02_GDD_Teknik.md` v2.1 ile senkronize)
> **Nasıl kullanılır (Claude Code için):**
> 1. Sıradaki görev = ilk `[ ]` olan ve bağımlılıkları `[x]` olan görev. Kullanıcı başka bir görev söylerse o.
> 2. Görevi alınca `[~]`, kabul kriterlerinin **hepsi** sağlanınca `[x]`. Commit: `<GörevID>: <kısa açıklama>`.
> 3. Kriterde "MCP" yazıyorsa Unity MCP ile play mode'da doğrula; `CinliMahzen/Debug/Dump State To Console` çıktısı kanıttır.
> 4. "🖐 elle" işaretli adımlar kullanıcının Unity'de elle yapacağı işlerdir — Claude Code hazırlığı yapar, kullanıcıya ne yapılacağını adım adım söyler.
>
> Boyut: **S** ≈ tek oturum, **M** ≈ 2-3 oturum, **L** ≈ 4+ oturum.

---

## Kilometre Taşları

| MS | Ad | Çıkış kriteri |
|---|---|---|
| **M0** | Kurulum | ✅ Proje, Core kontratları, bootstrap, input, debug iskeleti |
| **M0.5** | Pivot (v1 → v2.1) | Kod v2.1 kontratlarına uyarlandı, KayKit import edildi, derleme + testler yeşil |
| **M1** | Hareket & Possession | Hotseat'te insan yürür, cinler uçar (Ruh Çarpışması dahil), kötü cin eşyaya/Anne Terliğine girip fırlatır, insan bayılır ve Kolonya ile ayılır; dinamik süre; graybox ev |
| **M2** | Bulmaca Çekirdeği | 1 bulmaca (Portreler) uçtan uca: Ruh Perdesi ile referans okuma, 1.5 sn toplu sabotaj + 15 sn cooldown, fener koruması, onay, ödül; RoundSetupPlanner; aşama geçidi (+90 sn) |
| **M3** | Tam Oynanış (offline) | 4 iletişim odaklı bulmaca, bahçe (Kutsanmış Yağ) → 2 aşama → mahzen (3 katmanlı kazı + Ruh Siperi); ışık + Eski Radyo mekaniği; iyi cin yetenekleri; 4 raund + kademeli puan; HUD'lar |
| **M4** | Cila | KayKit ile ev, menüler, raund raporu + yeni unvanlar, ses (radyo türküsü, terlik sesi), VFX, denge |
| **M5** | Online (PUN 2) | 4 istemci tam maç |
| **M6** | Playtest | 3 gerçek oturum, denge |

---

## M0 — Kurulum ✅

| ✔ | ID | Görev |
|---|---|---|
| [x] | A0.1–A0.4 | Unity projesi, paketler, klasör/asmdef, layer/fizik/render feature |
| [x] | A0.5 | Core kontratları + stub'lar (v1) |
| [x] | A0.6 | OfflineNetBridge, EventBus, EntityRegistry, GameServices + testler |
| [x] | A0.7 | GameBalanceConfig + Loc + CMLog + GameRandom |
| [x] | A0.8 | Input Actions + LocalInputSource |
| [x] | A0.9 | Boot/MainMenu/Game sahneleri, auto-bootstrap, debug overlay |
| [x] | B0.1 | Possession veri modeli + durum makinesi + arbiter + enerji + testler (v1 kurallarıyla — P.4'te uyarlanır) |
| [~] | B0.2 | Spirit placeholder görseli (script var; Sandbox doğrulaması 1.6'da yapılır) |

---

## M0.5 — Pivot (v1 → v2.1)

| ✔ | ID | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|
| [x] | P.1 | Dokümanları v2.1'e yaz (GDD, Teknik, TODO, CLAUDE.md, README), ajan dosyalarını kaldır | — | S |
| [x] | P.2 | KayKit import + prefablar (C dalından al) | — | S |
| [x] | P.3 | Core kontratları v2.1 (config, event, bridge, MsgCode, stub, ItemDefinition) | P.1 | M |
| [x] | P.4 | Possession v2.1 (ışık kuralı, iyi cin, PD_* özel eşyalar) + Input v2.1 | P.3 | M |
| [x] | P.5 | Eski dalları temizle + Sandbox sahnesi | P.2 | S |

### P.2 — KayKit import
- `origin/agent-c/m0-assets-scene` dalından **sadece** şunları `main`'e al (`git checkout <dal> -- <yol>`): `Assets/_Project/Art/KayKit/**`, `Art/Materials/M_KayKit_Dungeon.mat`, `Prefabs/Environment/Env_*`, `Prefabs/Props/Prop_*` (+ .meta'lar), `docs/04_Asset_Eslestirme.md` ölçü tablosu.
- **Almayacakların:** `Scripts/World/**` (generator, planner, level kontratı), `Tests/EditMode/C/**`, `Editor/C/**`, `LevelPrefabSet`. Prefablar bu script'lere referans veriyorsa referansı kaldır.
- **Kabul (MCP):** Derleme temiz, konsolda missing script yok, birkaç prefab sahneye konup screenshot'ta pembe değil.

### P.3 — Core kontratları v2.1
- `02_GDD_Teknik.md §4` ve `§14` tablosundaki Core satırları:
  - `GameBalanceConfig` v2.1 alanları (`BaseRoundDuration = 360`, `StageTimeBonus = 90`, `LanternSafeZoneRadius = 2.5`, `ScrambleHoldTime = 1.5`, `ScrambleEnergyCost = 25`, `ScramblePuzzleCooldown = 15`, `FocusReferenceCost = 2`, `SpiritBumpBlurTime = 2`, `SpiritBlockEnergyCost = 30`, `DigTotalTime = 18`, `DigCheckpoints = 3`, kademeli puan alanları).
  - Event'ler (`RoundTimeAddedEvt`, `BlessedOilCollectedEvt`, `PuzzleScrambledEvt`, `RadioStateChangedEvt`, `ReferenceFocusChangedEvt`, `SpiritBumpedEvt`, `SpiritBlockedEvt`, `HumanRevivedEvt{byCologne}` vb.).
  - Bridge arayüzleri (`IHumanStatusQuery`, `IRoomService.IsNoiseSuppressed`, `IObjectiveInfo` yeni alanları), `MsgCode` v2.1 listesi, `RoundEndReason`, `ObjectivePhase`, stub'lar.
  - `ItemDefinition` + görev eşyası asset'leri (`ID_HouseKey`, `ID_BlessedOil`, `ID_FuseKey`, `ID_Paint`, `ID_Shovel`, `ID_CellarKey`). *(Fener artık sabit ekipman olduğu için `ID_Lantern` yerine `ID_BlessedOil` kullanılır).*
- `StatusType` sonuna `CameraSpin180` ve `VisionBlurred` eklenir (mevcut int değerleri **değişmez**).
- **Kabul:** Derleme temiz, tüm EditMode testleri yeşil (silinen tiplere ait testler kaldırıldı/uyarlandı).

### P.4 — Possession & Input v2.1
- `PossessionArbiterCore`: tuz kuralı → ışık kuralı (`IRoomService.LightOf`, lamba istisnası), iyi cin (`AllowGoodJinn`), `PossessDenyReason.Lit`.
- `PossessableDefinition` alan ekleri (§6.3: `AllowGoodJinn`, `IsLamp`, `PossessableSize {Small, Slipper, Large, Shelf, Lamp, Radio, Cologne, QuestItem}`).
- `PD_*` v2.1 listesi: `PD_Bottle`, `PD_Candle`, `PD_Plate`, `PD_Slipper` (Anne Terliği), `PD_Stool`, `PD_Chair`, `PD_Barrel`, `PD_Chest`, `PD_Table`, `PD_Shelf`, `PD_Lamp`, `PD_Radio` (Eski Radyo), `PD_Cologne` (Limon Kolonyası), `PD_QuestItem` (eskileri sil).
- Input: `IHumanInput` 3 slot + Drop + `LanternPulsePressed`, `ISpiritInput` (`FocusHeld` Sağ Tık Ruh Perdesi, `Exorcise`, `LampLight`, `PossessAsGood`), `CMInput` map'leri GDD §11.
- **Testler:** ışık kuralı (karanlık ✔, aydınlık ✘, lamba aydınlıkta ✔), iyi cin izinli/izinsiz eşya (`PD_Cologne` iyi ve kötü cin için ✔).
- **Kabul:** Testler yeşil.

### P.5 — Temizlik
- Kullanıcıya sor, onay verirse: `agent-c/*` ve `claude/gracious-euler-dewg8h` uzak dallarını sil.
- `Sandbox.unity` (serbest test sahnesi, düz zemin + birkaç prop). `Sandbox_A` → `Sandbox` olarak yeniden adlandırıldı (2026-10-15).

---

## M1 — Hareket & Possession

| ✔ | ID | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|
| [x] | 1.1 | PawnBase + Human FPS controller + stamina + merdiven/rampa + `CameraSpin180` | P.3 | M |
| [x] | 1.2 | CameraRig (FPS / Orbit / Faint) + culling mask + `VisionBlurred` | 1.1 | M |
| [x] | 1.3 | PlayerRegistry + Hotseat (F1-F4) + PawnSpawner | 1.1 | M |
| [x] | 1.4 | Interactor (raycast, basılı tutma, ipucu event'i) | 1.1 | S |
| [x] | 1.5 | HumanHealth + bayılma + Kolonya ile anında ayılma + kaybetme (otoriter) | 1.1 | M |
| [x] | 1.6 | SpiritController (uçuş, noclip, sınır) + Ruh Çarpışması (`SpiritBump`) | 1.2 | M |
| [~] | 1.7 | Possession akışı (MonoBehaviour) + mesajlar + orbit kamera | P.4, 1.6 | L |
| [~] | 1.8 | Aksiyonlar: Throw, HomingSlipper, Hop, Lunge, Slide, Topple, PlayTune, ReviveSplash | 1.7, 1.5 | L |
| [x] | 1.9 | Graybox ev v0 (zemin kat + 1. kat + bahçe, primitive/KayKit, marker'sız) | P.2 | M |
| [x] | 1.10 | MatchStateMachine minimal (dinamik sayaç 360s + 90s bonus, Faint/Timeout bitişi) | 1.3, 1.5 | M |

### 1.1 — Human controller
- `02_GDD_Teknik.md §5.1–5.2`. `HumanMotorState` düz C# + test (Fainted/Interacting'de hareket yok).
- `StatusType.CameraSpin180` uygulandığında kameranın yaw açısını 0.15 sn içinde 180° döndürür.
- **Kabul (MCP):** Sandbox'ta yürü/koş/stamina, rampa çıkılıyor; Dump State'te stamina değişiyor.

### 1.2 — CameraRig
- API: `SetFps(head)`, `SetOrbit(target, dist)`, `SetFaint()`, `TriggerBlur(duration)`. Culling `LocalRoleChangedEvt` ile (§2).
- **Kabul:** Geçişler 0.3 sn; orbit duvara girmiyor; `SpiritBumpedEvt` ile 2 sn bulanıklık/sarsıntı çalışıyor.

### 1.3 — Hotseat
- 4 PlayerInfo, rotasyon (raund 0: P1 insan, P2 iyi, P3-P4 kötü). F1-F4 → input/kamera/HUD/culling.
- **Kabul (MCP):** `Switch To Player 1..4` ile 4 piyon arası geçiş.

### 1.4 — Interactor
- §5.4. `InteractPromptEvt{key, progress}`.
- **Kabul:** Test küp 1 sn basılı → otoritede `ExecuteAuthority` log.

### 1.5 — HumanHealth, bayılma ve Kolonya
- §5.3. Bayılma, görev eşyası düşürme, `FaintsToLose = 3`, `ReviveWithCologneAuthority()` ile 12 sn beklemeden anında tam canla ayılma (`HumanRevivedEvt{byCologne=true}`).
- **Testler:** dokunulmazlık, bayılma sayacı, bayılmışken hasar yok, kolonya ile anında ayılma, 3. bayılma → kaybetme event'i.
- **Kabul (MCP):** `Faint Human` → `Revive Human With Cologne` anında kaldırır; `Faint Human` ×3 → `JinnsWin(Faint)` log.

### 1.6 — SpiritController & Ruh Çarpışması
- §6.1. İyi/kötü aynı controller, farklı hız.
- Kötü cin `Shift` ile uçarken odaklanan (`IsFocusingReference`) İyi Cin'in 1.5 m yakınından geçerse `ReqSpiritBump` → `SpiritBumpedEvt` (6 sn cooldown).
- **Kabul (MCP):** F3 → duvardan geçerek uçuyor, sınır dışına çıkamıyor; odaklanan F2'ye çarptığında F2'nin odaklanması bozuluyor.

### 1.7 — Possession akışı
- `PossessionSystem`, `Possessable` (`INetEntity`, `IKickable`), `PossessionCameraBinder`, gerçek `IPossessionQuery`. Girme telgrafı (titreme + ses hook), çıkış, tekme sersemliği.
- **Kabul (MCP):** F3 → sandalyeye gir → TPS → çık → FPS. Aydınlık odada (stub `IRoomService` ile) girme reddi. Dump State'te possession görünüyor.

### 1.8 — Aksiyonlar (Özel Eşyalar Dahil)
- `PossessableActionRunner` + `IActionBehaviour`:
  - `ThrowAction` (yay çizen projectile)
  - `HomingSlipperAction` (**Anne Terliği**: `arc = 0` düz hızlı atış, 1 hasar + `CameraSpin180`, kırılmaz)
  - `HopMove`, `LungeAction`, `SlideAction` (hücre kaydırma), `ToppleAction`
  - `PlayTuneAction` (**Eski Radyo**: 12 sn boyunca odada `IsNoiseSuppressed = true`)
  - `ReviveSplashAction` (**Limon Kolonyası**: İyi Cin baygın insanın 3 m yakınında kırarsa anında ayıltır, `Spent` olur)
- **Kabul (MCP):** Şişe fırlat → 1 hasar; Anne Terliği fırlat → 1 hasar + insan 180° döner; raf devril → hasar + knockdown; baygın insana İyi Cin kolonya kırar → anında kalkar.

### 1.9 — Graybox ev v0
- Claude Code: KayKit `Env_*` prefablarıyla (4 m grid) **editör script'i** ile zemin kat + 1. kat + bahçe iskeleti üretir (`CinliMahzen/House/Build Graybox`) → `P_House` prefabı. Oda listesi GDD §9.
- 🖐 elle: kullanıcı oda oranlarını, kapıları, merdiveni elle düzeltir.
- **Kabul (MCP):** Üstten + içeriden screenshot; insan iki kat arasında yürüyebiliyor.

### 1.10 — Match minimal & Dinamik Süre
- §9. RoundSetup → Intro → Playing (`BaseRoundDuration = 360 sn`) → RoundEnd → RoundSetup.
- `AddStageTimeBonusAuthority()` ile kapı/geçit açıldıkça `StateEndTime += 90 sn` (`RoundTimeAddedEvt`).
- **Testler:** geçişler, dinamik süre ekleme, 3 bitiş koşulu.
- **Kabul (MCP):** Faint ×3 → RoundEnd → 8 sn → yeni raund.

### ✅ M1 Çıkış Testi
1. `Game.unity` Play → evde 4 piyon doğar, sayaç 360 sn'den başlar.
2. F3 → kötü cin → Anne Terliği ve şişelerle F1'deki insanı bayıltır; F2 kolonya ile ayıltır.
3. Faint ×3 → raund biter, yeni raund başlar. Konsolda Error yok.

---

## M2 — Bulmaca Çekirdeği

| ✔ | ID | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|
| [x] | 2.1 | `PuzzleState` (toplu sabotaj, cooldown, referans dönüş/tuzak) + `PuzzleDefinition` SO + testler | P.3 | S |
| [x] | 2.2 | Ev marker'ları (§8: `CologneSpawnSpot` dahil) + `HouseService.Index` + `HouseValidator` + `IRoomService` | 1.9 | M |
| [~] | 2.3 | `RoundSetupPlanner` + `RoundPlanValidator` + testler (500 seed) | 2.1, 2.2 | L |
| [~] | 2.4 | `PuzzleSystem` + mesajlar (Op / Toplu Sabotaj / Confirm / Completed) + round-trip testleri | 2.1, 1.4 | M |
| [~] | 2.5 | Bulmaca P1 **Portreler**: `PuzzleView`, `PuzzlePart`, `ConfirmLever`, `PuzzleReference` (Ruh Perdesi) | 2.4 | M |
| [~] | 2.6 | Kötü cin **Toplu Sabotaj** (`PuzzleScrambleTarget`, 1.5s basılı, 15s cd, fener koruması) | 2.5, 1.7 | M |
| [~] | 2.7 | `RoundSetupApplier` + `StageGate` (+90 sn bonus) + `ItemReward` + Inventory | 2.3, 2.5 | M |

### 2.1 — PuzzleState
- §7.1. **Testler:** Cycle/Set/Swap, `TryScrambleAll` (her zaman çözülmmemiş bir dizilim üretir), `ScrambleCooldownUntil`, kilitliyken op/sabotaj reddi, `ResetToSolution`, `IsSolved`.

### 2.2 — Ev marker'ları
- §8 marker bileşenleri (`RoomMarker`, `StageZone`, `PuzzleSlot`, `ReferenceSlot`, `StageGate`, `GateLock`, `LampMarker`, `KeyHideSpot`, `CologneSpawnSpot`, `DigSpot`, `ToolShedItemPoint`), `HouseService` (NetId deterministik), `HouseValidator` + menü.
- 🖐 elle / MCP: graybox eve marker'ları yerleştir (Claude Code MCP ile yerleştirir, kullanıcı kontrol eder).
- **Kabul:** `House/Validate` temiz.

### 2.3 — RoundSetupPlanner
- §7.5. **Testler:** aynı seed → aynı plan hash; 500 seed → validator temiz; Boya/Şalter Anahtarı gerektiren bulmaca hep ödülünden sonra veya paralelinde; referans ≠ bulmaca odası; `CologneSpawnSpot` seçimi geçerli.
- **Kabul:** `House/Plan Batch Report (500 seeds)` → %100 geçerli + tip dağılımı raporu.

### 2.4 — PuzzleSystem
- §7.3 akışı (`ReqPuzzleOp`, `ReqPuzzleScramble`, `ReqPuzzleConfirm`), `IObjectiveInfo` gerçek uygulaması.
- **Testler:** NetMsg round-trip; yanlış onay → hasar + cooldown; 15 sn sabotaj cooldown veya fener koruma alanındayken (`IsInLanternSafeZone`) sabotaj reddi.

### 2.5 — P1 Portreler bulmacası & Ruh Perdesi
- `P_Puzzle_Paintings`: 3 komik/ürkütücü portre (Bıyıklı Dede, Pörtlek Nine, Ters Eşek) × 4 açı (90°: Düz, Sağ, Sol, Ters).
- `P_PuzzleRef_Paintings` (`SpiritOnly`): Normalde silik/perdeli durur; yalnızca İyi Cin Sağ Tık (`FocusReference`, 2 enerji/sn) basılı tuttuğunda portre yönleri netleşir.
- **Kabul (MCP):** İnsan portreyi 90° çevirir → `PuzzleStateChanged`; çözülmüşken kol → Completed; F2 Sağ Tık basılı tutunca referans netleşiyor, F1'de hiç görünmüyor (screenshot ×2).

### 2.6 — Toplu Sabotaj (Anti-Spam)
- `PuzzleScrambleTarget`: Kötü cin bulmacaya bakıp `1.5 sn` E basılı tutar (`25 enerji`) → tüm parçalar tek seferde karışır, gıcırdar ve bulmaca `15 sn` sabotaj korumasına girer.
- Aydınlık odada veya insan güçlü fenerle `2.5 m` yakındayken sabotaj yapılamaz.
- **Kabul (MCP):** F3 → 1.5 sn E basılı → tüm portreler karışır, 25 enerji düşer, 15 sn tekrar sabote edilemez.

### 2.7 — Applier, geçit (+90 sn), ödül, envanter
- §7.6. Inventory (3 slot görev eşyası, otoriter), `ReqDropItem`, yerdeki eşya possessable.
- `StageGate` açıldığında `MatchController`'a `+90 sn` (`StageTimeBonus`) ekletir.
- **Kabul (MCP):** Raund başı plan uygulanıyor; aşamadaki 2 bulmaca tamam → geçit açılıyor + süreye 90 sn ekleniyor → ödül eşyası alınıyor; F8 → yeni seed ile farklı yerleşim.

### ✅ M2 Çıkış Testi
F8 ile 3 farklı seed: F3 1.5 sn basılı tutarak bulmacayı toplu sabote eder, F2 Sağ Tık ile Ruh Perdesini açıp portreleri okur, F1 çözer + onaylar → geçit açılır ve sayaca +90 sn eklenir.

---

## M3 — Tam Oynanış (offline)

| ✔ | ID | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|
| [~] | 3.1 | P2 **Heykelin Nazarlığı** (6 çevre objesi yönü + 3 asa eğimi) | 2.7 | M |
| [~] | 3.2 | P3 **Tılsımlı Şalter** (4 soket × 6 köy motifi + referansta 2 Çarpı `X` tuzağı, Şalter Anahtarı) | 2.7 | M |
| [~] | 3.3 | P4 **Mühür Taşları** (3×3 ızgara + 4 köşe sembolü + dönük referans, Boya gereksinimi) | 2.7 | M |
| [~] | 3.4 | Işık & Radyo sistemi: lambalar, düğmeler, `LightService`, aydınlanınca atılma, Patlat, Radyo ses bastırma | 2.2, 1.8 | M |
| [~] | 3.5 | İyi cin yetenekleri: Ruh Perdesi Odakla, Lamba Yak/Onar, İşaret, Kov, Eşyaya Gir (hasarsız + Kolonya) | 3.4, 1.7 | L |
| [~] | 3.6 | Bahçe aşaması: `KeyHideSpot`, ön kapı `GateLock` (+90 sn), alet kulübesi **Kutsanmış Yağ** | 2.7 | S |
| [~] | 3.7 | Mahzen: kapak, `DigSpot`, kürek, **3 katmanlı kazı (3×6 sn)**, Öfke, **Ruh Siperi (`SpiritBlock`)**, `TreasureDug` | 2.7 | M |
| [~] | 3.8 | Sabit Fener (Zayıf → Kutsanmış Yağ ile Güçlü Mod: 2.5 m sabotaj koruması + Sağ Tık Parlat) + Tekme + itme | 1.8, 2.7 | M |
| [~] | 3.9 | Görünürlük servisi (GDD §12 tablosu + odaklanan iyi cin parıltısı) + soğuk nefes | 1.7 | M |
| [ ] | 3.10 | Maç tam: 4 raund rotasyon, **Kademeli Puanlama (`ScoreService`)**, `MatchEnd` | 1.10, 3.7 | M |
| [ ] | 3.11 | HUD'lar: İnsan, İyi Cin, Kötü Cin (GDD v2.1 §13 ve Teknik §10) | 3.5, 3.10 | L |
| [ ] | 3.12 | Dummy İnsan botu (NavMesh gezinme) | 2.2 | S |

Her bulmaca görevi için kabul: tip planner'a eklendi, 500 seed raporu geçerli, MCP ile çözülme + toplu sabotaj + Ruh Perdesi referans screenshot'ı.

### ✅ M3 Çıkış Testi (tam raund, hotseat)
Bahçede Kutsanmış Yağ ve anahtar → ön kapı (+90 sn) → zemin kat 2 bulmaca (+90 sn) → 1. kat 2 bulmaca (+90 sn) → mahzen 3 katmanlı kazı (İyi Cin Ruh Siperi ile uçan eşyayı bloklar) → SeekersWin; ayrı raundda 3 bayılma → JinnsWin; süre bitimi → JinnsWin. 4 raund → Kademeli puanlar (`ScorePerPuzzle`, `ScorePerUnfinishedPuzzle` vb.) doğru hesaplanır.

---

## M4 — Cila

| ✔ | ID | Görev | Boyut |
|---|---|---|---|
| [ ] | 4.1 | Ev v1: KayKit ile 3 kat + çatı katı + mahzen + bahçe (Claude Code iskelet, 🖐 elle detay) | L |
| [ ] | 4.2 | Ana menü, ayarlar, duraklat, rol açıklama | M |
| [ ] | 4.3 | Raund raporu + maç sonu unvanları (`StatsService`, `TitleCalculator`: "Bozguncu", "Terlik Nişancısı", "Kolonyacı" dahil) | M |
| [ ] | 4.4 | Ses: telgraf, toplu sabotaj gıcırtısı, Anne Terliği "ŞRAK!", Eski Radyo oyun havası (ses bastırma), ortam, müzik | M |
| [ ] | 4.5 | VFX: cin görselleri, Ruh Perdesi odak parıltısı, Ruh Siperi çarpışma efekti, lamba patlaması | M |
| [ ] | 4.6 | Atmosfer: ay ışığı, sis, lamba ışıkları, performans (≥ 90 FPS) | M |
| [ ] | 4.7 | Çatı katı aşaması (`StagesActive = 3`) | S |
| [ ] | 4.8 | Denge araçları: overlay'de runtime config ayarı | S |

## M5 — Online (PUN 2)

| ✔ | ID | Görev | Boyut |
|---|---|---|---|
| [ ] | 5.1 | PUN 2 import + `PunConnection` + lobi (oda kodu) | M |
| [ ] | 5.2 | `PunNetBridge` + `PunPlayerRegistry` | M |
| [ ] | 5.3 | `PunPawnSync` + `PunPawnSpawner` | M |
| [ ] | 5.4 | Plan/ev hash kontrolü, ayrılma senaryoları | S |
| [ ] | 5.5 | Multiplayer Play Mode ile 4 istemci tam maç | M |

## M6 — Playtest & Denge

| ✔ | ID | Görev |
|---|---|---|
| [ ] | 6.1 | 3 playtest oturumu, notlar `docs/PLAYTEST.md` (dinamik süre, fener koruması ve 3 katmanlı mahzen kazısı odaklı) |
| [ ] | 6.2 | Denge değişiklikleri (config + GDD §14 aynı commit) |
| [ ] | 6.3 | P5 Kitaplar & Şömine, P6 Yapboz (isteğe bağlı) |