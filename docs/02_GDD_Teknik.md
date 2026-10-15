# CİNLİ MAHZEN — Teknik Tasarım Dokümanı

> Sürüm: **v2.1 — 2026-10-15** (tek geliştirici + Claude Code; v2.1 mekanik, anti-spam, ruh etkileşimleri ve dinamik süre kurallarıyla senkronize)
> Oyun kuralları: `01_GDD_Oyun.md`. Görevler: `03_TODO.md`. Asset eşleştirme: `04_Asset_Eslestirme.md`.
> Kontratlar (§4, §8) değişirse bu doküman **aynı commit'te** güncellenir.

---

## 0. Özet Kararlar

| Konu | Karar |
|---|---|
| Motor | **Unity 6 — 6000.3.18f1**, URP, Input System |
| Geliştirme | **Tek geliştirici + Claude Code (Unity MCP ile).** Ajan/sahiplik sistemi yok. |
| Network | **Photon PUN 2 — en son fazda (M5).** O zamana kadar offline, ama kod ilk günden network'e hazır (§4.3 `INetBridge`). |
| Otorite | **Master Client otoriter**: can/bayılma, possession, enerji, bulmaca durumları, sabotaj kilitleri, ışıklar, envanter, dinamik süre, maç. Oyuncu hareketi sahip-otoriter. |
| Fizik | Oyun sonucunu etkileyen hiçbir şey Rigidbody'ye bağlı değil. Fırlatma/kaydırma kinematik + otorite hit-check (Ruh Siperi dahil). Rigidbody sadece kozmetik. |
| Dünya | **Elle kurulmuş ev** (`P_House` prefabı). Rastgelelik = **seed'li yerleşim** (`RoundSetupPlanner`): bulmaca->oda, referans->oda, cevaplar, eşya ödülleri, anahtar/kolonya/kazı noktası. Prosedürel harita üreticisi **yok**. |
| Bulmacalar | Tek genel model: **N parça x K değer** (`PuzzleState`) + toplu sabotaj + referans dönüş/tuzak verisi. Her bulmaca tipi = veri (`PuzzleDefinition` SO) + görsel (`PuzzleView`). |
| Veri | Tüm denge değerleri **ScriptableObject** (`GameBalanceConfig`, `PossessableDefinition`, `ItemDefinition`, `PuzzleDefinition`). Sihirli sayı yok. |
| Test | Mantık düz C# -> **EditMode**. Akışlar -> PlayMode + Unity MCP duman testi. |
| Solo test | **Hotseat:** Tek editörde 4 piyon, F1-F4 ile rol değiştirme. |

> **PUN 2 notu:** Bakım modunda. `INetBridge` sayesinde gerekirse sadece `Net/Pun` değiştirilerek Fusion/başka çözüme geçilebilir. Oyun kodu `Photon.*` tiplerini asla doğrudan kullanmaz.

---

## 1. Proje Yapısı

```
Assets/_Project/
├── Art/KayKit/ (Models, Textures, License)   Art/Materials/   Art/VFX/   Art/Characters/
├── Audio/
├── Prefabs/
│   ├── Player/        Human, SpiritBase, EvilJinn, GoodJinn
│   ├── Possessables/  P_Poss_* (küçük/büyük/raf/lamba/AnneTerligi/Radyo/Kolonya)
│   ├── Environment/   Env_* (duvar, zemin, kapı, merdiven…)
│   ├── Props/         Prop_* (possessable olmayan dekor)
│   ├── Puzzles/       P_Puzzle_Paintings, P_Puzzle_Statue, P_Puzzle_Wires, P_Puzzle_Sigil + referans prefabları
│   ├── World/         P_House (elle kurulmuş ev), P_Gate_*, P_Lamp_*, P_DigSpot, P_KeyHideSpot, P_CologneSpot
│   ├── Items/         P_Item_* (görev eşyaları ve Kutsanmış Yağ, yerdeki hali)
│   └── UI/
├── ScriptableObjects/ Config/  Possessables/  Items/  Puzzles/  World/
├── Scenes/            Boot, MainMenu, Game, Sandbox (serbest test)
├── Scripts/
│   ├── Core/          CM.Core — kontratlar, servisler, EventBus, Net soyutlaması
│   ├── Match/         CM.Match — maç durum makinesi, dinamik süre, rol rotasyonu, kademeli puan
│   ├── Player/        CM.Player — insan, sabit fener (zayıf/güçlü), piyon tabanı, kamera, input, envanter
│   ├── Jinn/          CM.Jinn — ruh formu, Ruh Perdesi, Ruh Çarpışması, Ruh Siperi, iyi cin yetenekleri, enerji
│   ├── Possession/    CM.Possession — possession, eşya aksiyonları (terlik, radyo, kolonya dahil)
│   ├── Visibility/    CM.Visibility — rol bazlı görünürlük ve odaklanma parıltısı
│   ├── World/         CM.World — ev kontratı (marker'lar), oda/ışık/ses bastırma servisi, RoundSetupPlanner
│   ├── Objectives/    CM.Objectives — bulmacalar, toplu sabotaj, aşamalar, geçitler, eşya ödülleri, 3 katmanlı kazı
│   ├── Audio/  UI/  DebugTools/  Net/Pun/ (M5)  Editor/
└── Tests/ EditMode/  PlayMode/
```

### 1.1 Assembly'ler ve Bağımlılıklar
```
CM.Core          -> —                      [Unity.InputSystem]
CM.Player        -> CM.Core
CM.Possession    -> CM.Core
CM.Jinn          -> CM.Core, CM.Possession
CM.Visibility    -> CM.Core
CM.World         -> CM.Core                [Unity.AI.Navigation]
CM.Objectives    -> CM.Core, CM.World
CM.Audio / CM.UI -> CM.Core
CM.Match         -> CM.Core, CM.World
CM.DebugTools    -> CM.Core, CM.Match, CM.Player, CM.Jinn, CM.World
CM.Net.Pun       -> CM.Core                [Photon]
CM.Editor        -> hepsi (Editor-only)
```
Döngüsel bağımlılık yasak. Modüller birbirini **Core arayüzleri + EventBus + `GameServices.Get<T>()`** ile tanır.

### 1.2 Kodlama Standartları
- Namespace = `CinliMahzen.<Modül>`. Bir dosya = bir public tip. Adlar İngilizce.
- `[SerializeField] private`; public alan yok.
- `Update` içinde `Find*`, `GetComponent`, LINQ, allocation yok.
- Zaman: `GameServices.Net.Time` (`Time.time` oyun mantığında yok; `Time.deltaTime` serbest).
- Rastgelelik: oyun durumunu etkileyen her şey `GameRandom` (seed'li). `UnityEngine.Random` sadece kozmetik.
- Log: `CMLog.Info("Kategori", "mesaj")`. Kullanıcıya görünen metin: `Loc.T("anahtar")`.

---

## 2. Tags, Layers, Fizik Matrisi

| # | Layer | Kullanım |
|---|---|---|
| 6 | `Environment` | Duvar, zemin, statik geometri |
| 7 | `Possessable` | Cin girebilen eşyalar |
| 8 | `HumanPawn` | İnsanın CharacterController'ı |
| 9 | `HumanHitbox` | İnsanın hasar alan trigger'ı |
| 10 | `SpiritPawn` | Cin piyonları (Mahzen aşamasında İyi Cin'in `SpiritBlock` çarpışma kontrolü dahil) |
| 11 | `EvilJinnVisual` | Kötü cin görselleri |
| 12 | `GoodJinnVisual` | İyi cin görselleri (insan da soluk görür; referans okurken parlar) |
| 13 | `SpiritOnly` | **Bulmaca referansları**, anahtar parıltısı, gerçek kazı noktası (iki cin de görür) |
| 14 | `JinnOnlyFX` | Nişan okları vb. (sadece kötü cinler) |
| 15 | `Interactable` | Etkileşim raycast hedefleri (bulmaca parçaları, kol, düğme, kapı, radyo) |
| 16 | `Projectile` | Fırlatılan eşyalar (şişe, tabak, Anne Terliği) |
| 17 | `CosmeticPhysics` | Kırık parçalar |
| 18 | `XRayOutline` | Duvar arkası outline |

Fizik matrisi: `HumanPawn <-> Environment, Possessable` · `Projectile <-> Environment, HumanHitbox, Possessable, SpiritPawn (sadece otorite SphereCast'te İyi Cin Ruh Siperi için)` · `CosmeticPhysics <-> Environment, CosmeticPhysics` · `SpiritPawn <-> hiçbiri (kinematik uçuş)`.

**Culling (lokal role göre `CameraRig`):**
| Rol | Ek görür | Görmez |
|---|---|---|
| İnsan | GoodJinnVisual (soluk materyal) | EvilJinnVisual, SpiritOnly, JinnOnlyFX, XRayOutline |
| İyi Cin | GoodJinnVisual, EvilJinnVisual*, SpiritOnly**, XRayOutline | JinnOnlyFX |
| Kötü Cin | EvilJinnVisual, GoodJinnVisual (soluk veya odaklanırken parlak), SpiritOnly (yeri görünür, referans içeriği silik), JinnOnlyFX, XRayOutline | — |

\* Mesafe kuralları `VisibilityService` ile (§6.6).
\*\* Bulmaca referanslarının içeriği İyi Cin Sağ Tık (`FocusReference`) basılı tuttuğunda netleşir.

---

## 3. Sahne & Uygulama Akışı

```
Boot ──► GameServices (DontDestroyOnLoad), Config, NetBridge = Offline
MainMenu ──► "Test Oyunu" (hotseat) / "Online" (M5)
Game ──► MatchController
           ├─ P_House (sahnede sabit, elle kurulmuş)
           ├─ HouseService.Index()          -> oda/marker/NetId kaydı (bir kez)
           ├─ RoundSetupPlanner.Plan(seed)  -> RoundPlan (her raund)
           ├─ RoundSetupApplier.Apply(plan) -> bulmaca/referans/eşya/kolonya yerleşimi + reset
           ├─ PawnSpawner                   -> 4 piyon
           └─ MatchStateMachine
```
- Ev **silinmez**; her raund `RoundSetupApplier` bulmacaları, kapıları, lambaları, radyoları, eşyaları **sıfırlar** ve yeni plana göre kurar.
- Editörde `Game.unity` doğrudan Play -> otomatik bootstrap + hotseat (MCP testi için kritik).

---

## 4. Core Kontratları (CM.Core)

### 4.1 Kimlikler ve Roller
`Role {None, Human, GoodJinn, EvilJinn}`, `Team {None, Seekers, Jinns}`, `PlayerId(byte)`, `NetId(int)` — aralıklar: 1..999 piyon/sistem, 1000..59999 ev nesneleri (deterministik), 60000+ runtime spawn.

### 4.2 Oyuncu Kaydı
`IPlayerRegistry { Players, LocalPlayer, GetRole, Get, LocalPlayerChanged, RolesChanged }`, `PlayerInfo { Id, Nickname, Role, Score }`.

### 4.3 Network Soyutlaması — EN ÖNEMLİ KURAL
```
[İstemci] Request ──► [OTORİTE] doğrula + durumu değiştir ──► Broadcast ──► [Herkes] uygula
```
Offline'da `OfflineNetBridge` aynı frame içinde döngüye sokar. `...Authority` ile biten metotlar sadece `Net.IsAuthority` iken çağrılır (başında `Debug.Assert`).

`INetBridge { IsOnline, IsAuthority, LocalPlayer, Time, SendToAuthority, Broadcast, Register, Unregister }` · `NetMsg { Code, Sender, SentTime, object[] Payload }` — payload sadece PUN-serileştirilebilir tipler.

**MsgCode aralıkları (v2.1):**
```
1-29    Maç:        MatchStateChanged, RoundSetup{seed, roles}, RoundTimeAdded{bonusSec, newEndTime},
                    RoundEnded, RolesAssigned, ScoreChanged
30-59   İnsan:      ReqInteract, InteractResult, HumanDamaged, HumanFainted, HumanRevived{byCologne},
                    ReqKick, KickResult, StatusApplied, ReqLanternPulse, LanternPulsed,
                    BlessedOilCollected, InventoryChanged, ReqDropItem, ItemDropped, ReqPush, PushResult
60-99   Cinler:     ReqPossess, PossessBegan, PossessCompleted, PossessEnded, PossessDenied,
                    ReqAction, ActionTelegraph, ActionResolved, ActionDenied, EnergyChanged,
                    JinnStunned, ProjectileSpawned, ProjectileImpact, SpiritBlockedProjectile,
                    ReqExorcise, ExorciseProgress, ExorciseResult, ReqPing, PingPlaced,
                    RageStarted, PossessedMove, ObjectStateChanged, ReqLampLight, ReqLampRepair,
                    ReqFocusReference, ReferenceFocusChanged, ReqSpiritBump, SpiritBumped,
                    CologneReviveTriggered
100-139 Dünya/Bulmaca: HouseReady, ReqPuzzleOp, PuzzleStateChanged, ReqPuzzleScramble, PuzzleScrambled,
                    ReqPuzzleConfirm, PuzzleConfirmResult, PuzzleCompleted, StageCompleted,
                    GateOpened, ItemSpawned, ItemPickedUp, LightsChanged, RadioStateChanged,
                    KeyFound, DigProgress, DigCheckpointReached, TreasureDug, HouseHash
200-254 Debug
```
Her yeni mesaj için encode/decode yardımcısı + **EditMode round-trip testi** zorunlu.

### 4.4 Varlık Kaydı
`INetEntity`, `IEntityRegistry { Register, Unregister, TryGet<T>, AllocateRuntimeId }`. Ev nesnelerine NetId'yi `HouseService.Index()` **deterministik sırayla** atar (hiyerarşi yolu sıralı).

### 4.5 Hasar, Durum, Etkileşim
- `DamageInfo { Amount, Attacker, SourceObject, CauseId, Flags, KnockdownTime, Point, Direction }` — v2.1'de ölüm yok; can 0 -> **bayılma**.
- `IDamageable.ApplyDamageAuthority`, `IStatusReceiver.ApplyStatusAuthority`.
- `StatusType`: `Knockdown, Fainted, Stunned, Slowed, CameraSpin180 (Anne Terliği), VisionBlurred (Ruh Çarpışması)`.
- `IInteractable { NetId, PromptKey, HoldTime, CanInteract(who, role), ValidateAuthority(who), ExecuteAuthority(who), NoiseOnInteract }`.
- **v2.1 kuralı:** Bulmaca parçası (`PuzzlePart`) sadece `Role.Human` için `CanInteract` döner; Kötü Cinler parçaları tek tek çevirmez, bulmacanın ana gövdesindeki `PuzzleScrambleTarget` bileşenine `1.5 sn` basılı tutarak toplu sabotaj yapar.

### 4.6 EventBus (lokal)
Ana event'ler (v2.1):
| Event | Yayınlayan | Dinleyen |
|---|---|---|
| `RoundStartedEvt{roundIndex, seed}` | Match | herkes |
| `RoundTimeAddedEvt{bonusSeconds, newEndTime}` | Match | UI, Audio |
| `HumanDamagedEvt{info, hpAfter}` | Player | UI, Audio, Stats |
| `HumanFaintedEvt{info, faintCount}` / `HumanRevivedEvt{byCologne}` | Player | Match, UI, Stats |
| `BlessedOilCollectedEvt` | Player | UI, Audio, Objectives |
| `PossessionChangedEvt{player, obj, state}` | Possession | UI, Visibility, Input, Audio |
| `ActionTelegraphEvt` / `ActionResolvedEvt` | Possession | Audio, VFX, Stats |
| `PuzzleStateChangedEvt{puzzle, byPlayer}` | Objectives | Audio, VFX |
| `PuzzleScrambledEvt{puzzle, byJinn, cooldownUntil}` | Objectives | Audio, VFX, UI, Stats |
| `PuzzleCompletedEvt{puzzle, stage}` / `StageCompletedEvt{stage}` | Objectives | UI, Match (puan + süre), Audio |
| `RoomLightChangedEvt{room, state}` | World | Possession (eject), Visibility, UI |
| `RadioStateChangedEvt{room, isPlaying}` | World | Audio (telgraf seslerini bastırma) |
| `ReferenceFocusChangedEvt{jinn, isFocusing}` | Jinn | Visibility, UI |
| `SpiritBumpedEvt{evilJinn, goodJinn}` | Jinn | CameraRig (bulanıklık), UI, Audio |
| `SpiritBlockedEvt{goodJinn, projectileId}` | Possession | Jinn, UI, Audio, Stats |
| `DigProgressEvt{progress, checkpoint}` / `TreasureDugEvt` | Objectives | Match, Jinn (Öfke), UI |
| `LocalRoleChangedEvt{role}` | Core | Kamera, UI, Visibility, Input |
| `RoundEndedEvt{result, reason}` | Match | UI, Stats |

### 4.7 Servisler
`GameServices { Net, Players, Entities, Config, Match, Level, Register<T>, Get<T>, TryGet<T> }`.

### 4.8 Köprü Arayüzleri (v2.1)
```csharp
// Possession uygular
IKickable { NetId; OnKickedAuthority(PlayerId by) }
IPossessionQuery { IsPossessed, PossessorOf, GetPossessedInCone(...) }

// Player uygular (Fener koruması ve baygınlık sorgusu için)
IHumanStatusQuery
{
    Vector3 Position { get; }
    bool IsFainted { get; }
    bool IsBlessedLanternActive { get; }
    bool IsInLanternSafeZone(Vector3 worldPos); // Mesafe <= LanternSafeZoneRadius ve fener güçlü/açık
}

// World uygular
IRoomService
{
    int RoomOf(Vector3 p);
    RoomLightState LightOf(int room);
    bool IsNoiseSuppressed(int room); // Eski Radyo çalıyorsa true (12 sn)
}
ILightService  { void SetLampAuthority(NetId lamp, LampState s); } // Off / On / Broken
IHouseInfo     { Bounds Bounds; Vector3 HumanSpawn; IReadOnlyList<Vector3> JinnSpawns; }

// Objectives uygular
IObjectiveInfo
{
    int StageIndex { get; }
    int StageCount { get; }
    bool IsPuzzleDone(int slot);
    bool IsPuzzleScrambleOnCooldown(int slot, out float remaining);
    float DigProgress { get; }
    int DigCheckpoint { get; } // 0..3
    ObjectivePhase Phase { get; }
}
enum ObjectivePhase : byte { Garden = 0, House = 1, Cellar = 2, Done = 3 }

// Match uygular
IMatchInfo { MatchState State; int RoundIndex; double StateEndTime; bool JinnsAwake; int Faints; }
```

### 4.9 Konfigürasyon
`GameBalanceConfig` — `01_GDD_Oyun.md §14` tablosundaki her satır birebir aynı isimle yer alır (`BaseRoundDuration`, `StageTimeBonus`, `LanternSafeZoneRadius`, `ScrambleHoldTime`, `ScramblePuzzleCooldown`, `FocusReferenceCost`, `SpiritBumpBlurTime`, `SpiritBlockEnergyCost`, `DigCheckpoints`, `ScorePerPuzzle`, `ScoreBasementOpen`, `ScoreTreasureWin`, `ScorePerFaint`, `ScorePerUnfinishedPuzzle` dahil).

---

## 5. İnsan Sistemleri (CM.Player)

### 5.1 Piyon Mimarisi
`PawnBase (NetEntity) { Owner, Role, IPawnInput Input, IsLocallyControlled }`. Hotseat: 4 piyon, lokal olmayanlar `NullInput`. `IHumanInput { Move, Look, Sprint, InteractHeld/Pressed, KickPressed, LanternPulsePressed, SelectSlot(-1/0/1/2), DropPressed }`.

### 5.2 HumanController
CharacterController, zıplama yok, **merdiven = rampa collider**. Durumlar (`HumanMotorState`, düz C#): `Normal`, `Interacting` (kazı/itme — hareket yok), `KnockedDown`, `Fainted`.
- **Anne Terliği Etkisi (`StatusType.CameraSpin180`):** İsabet halinde insanın look yaw açısı `0.15 sn` içinde `180 derece` döndürülür ve "ŞRAK!" ses efekti oynatılır.

### 5.3 HumanHealth, Bayılma ve Kolonya ile Ayıltma (otoriter)
```
ApplyDamageAuthority: dokunulmazlık? -> HP -= dmg -> HumanDamaged
  HP == 0 -> faints++ -> HumanFainted{faints} -> ScoreService.OnHumanFainted()
           -> seçili görev eşyasını düşür (ItemDropped)
           -> faints >= FaintsToLose ? Match: JinnsWin(Faint) : FaintDuration sonra HumanRevived{byCologne=false} (HP = Max)
```
- **Limon Kolonyası ile Anında Ayıltma:** İnsan `Fainted` durumundayken İyi Cin `PD_Cologne` eşyasının içindeyken insanın `3 m` yakınında aksiyonu tetiklerse (`CologneReviveTriggered`), otorite bayılma sayacını beklemeden anında `HumanRevived{byCologne=true}` yayınlar ve insanı tam canla ayağa kaldırır.

### 5.4 Sabit Fener, Kutsanmış Yağ, Envanter, Tekme
- **Sabit Fener (`HumanLantern`):** Envanter slotu kaplamaz. Raund başında **Zayıf Mod** (kısa menzil, Parlat kapalı, bulmaca koruma alanı kapalı) olarak başlar.
- **Kutsanmış Yağ (`ID_BlessedOil`):** Bahçedeki alet kulübesinden (`ToolShedItemPoint`) alındığında `BlessedOilCollected` yayınlanır ve fener **Güçlü Mod**a geçer:
  1. İnsanın `LanternSafeZoneRadius` (`2.5 m`) çevresindeki bulmacalar ruh formundaki kötü cinler tarafından sabote edilemez (`IHumanStatusQuery.IsInLanternSafeZone`).
  2. **Sağ Tık = Parlat (`ReqLanternPulse`):** `8 m` konide cin girmiş eşyalar `3 sn` parlar (`12 sn` bekleme).
- **Inventory (otoriter):** `ItemSlotCount` = 3 slot (sadece görev eşyaları: Ev Anahtarı, Şalter Anahtarı, Boya, Kürek, Bodrum Anahtarı).

---

## 6. Cin & Possession Sistemleri (CM.Jinn, CM.Possession, CM.Visibility)

### 6.1 SpiritController ve Ruh Çarpışması (Spirit Bump)
FPS uçuş, noclip, kinematik. Sınır: `IHouseInfo.Bounds` içinde clamp.
- **Ruh Çarpışması:** Kötü Cin `Shift` (Hızlı Uçuş) basılıyken, referans okuyan (`IsFocusingReference == true`) İyi Cin'in `1.5 m` yakınından geçerse `ReqSpiritBump` gönderilir. Otorite doğrularsa `SpiritBumped` yayınlanır: İyi Cin'in referans odaklanması kesilir ve ekranına `SpiritBumpBlurTime` (`2 sn`) boyunca bulanıklık/sarsıntı uygulanır (bekleme süresi `6 sn`).

### 6.2 Possession Durum Makinesi
```
Free -> Entering(PossessTime) -> Possessed(Lurking <-> Charging -> Recovering) -> Free
Özel: Spent (tek kullanımlık bitti: Raf, Limon Kolonyası)
```
**`PossessionArbiter.TryPossess(player, obj)` kuralları (v2.1):**
1. Rol `EvilJinn` **veya** `GoodJinn` (iyi cin sadece `AllowGoodJinn` işaretli eşyalara), sersem/kilitli değil.
2. `JinnsAwake`.
3. Nesne `Free`, `Spent` değil.
4. **Işık kuralı:** Kötü cin ise ve nesnenin odası `On` -> red (`PossessDenyReason.Lit`) — **istisna:** nesne `IsLamp`.
5. Mesafe <= `PossessRange + NetRangeTolerance`.
6. `ReenterCooldown` dolmuş, oyuncu başka nesnede değil.

### 6.3 PossessableDefinition ve Özel Eşyalar (v2.1)
Mevcut alanlar + `bool AllowGoodJinn`, `bool IsLamp`, `PossessableSize Size {Small, Slipper, Large, Shelf, Lamp, Radio, Cologne, QuestItem}`.
Aksiyon `Id`'leri: `throw`, `homing_slipper` (Anne Terliği), `hop`, `lunge`, `slide`, `topple`, `burst`, `play_tune` (Radyo), `fling` (görev eşyası / kolonya fırlatma), `revive_splash` (İyi Cin kolonya ile ayıltma).

**`PD_*` asset'leri (v2.1):**
- `PD_Bottle`, `PD_Candle`, `PD_Plate` (küçük fırlatılanlar)
- `PD_Slipper` (**Anne Terliği**: kat başına 1 adet, yerçekimsiz düz çizgi atış `arc = 0`, yüksek hız, 1 hasar + `CameraSpin180`, kırılmaz, `25` enerji, `15 sn` bekleme)
- `PD_Stool`, `PD_Chair` (`hop` / `lunge`)
- `PD_Barrel`, `PD_Chest`, `PD_Table` (`slide`)
- `PD_Shelf` (`topple`, tek kullanım)
- `PD_Lamp` (`burst`)
- `PD_Radio` (**Eski Radyo / Gramofon**: `play_tune`, `15` enerji, `20 sn` bekleme; `12 sn` boyunca `IRoomService.IsNoiseSuppressed(room) = true` yapar ve odadaki sabotaj/titreme seslerini bastırır)
- `PD_Cologne` (**Limon Kolonyası**: Kötü Cin `fling` ile uzağa fırlatabilir; İyi Cin `revive_splash` ile baygın insanın üstünde kırıp anında ayıltır, tek kullanımlık `Spent`)
- `PD_QuestItem` (`fling`)

### 6.4 Aksiyon Akışı ve Ruh Siperi (Spirit Block)
- Fırlatma = otorite `ProjectileSpawned{runtimeId, origin, dir, speed, arc, t0}`, otorite `SphereCast` ile ilerletir.
- **Ruh Siperi (Mahzen Finali):** `IObjectiveInfo.Phase == ObjectivePhase.Cellar` iken, otoritenin `Projectile` çarpışma kontrolü **İyi Cin'in ruh piyonu (`SpiritPawn`, yarıçap `1.0 m`)** ile kesişirse ve İyi Cin'in en az `SpiritBlockEnergyCost` (`30`) enerjisi varsa:
  1. Eşya insana ulaşmadan havada durdurulur ve yere düşer.
  2. İyi Cin'den `30` enerji düşülür ve hafif geri tepme uygulanır.
  3. `SpiritBlockedProjectile` yayınlanır (İyi Cin'e "Kolonyacı / Koruyucu" istatistiği yazar).

### 6.5 İyi Cin Yetenekleri ve Ruh Perdesi
- **Ruh Görüşü Odakla (`FocusReference`):** İyi Cin Sağ Tık basılı tutarken (`ISpiritInput.FocusHeld`), `FocusReferenceCost` (`2 enerji/sn`) harcar. `PuzzleReference` bileşenleri yalnızca lokal İyi Cin `IsFocusing` durumundayken sembol/açı detaylarını gösterir. Odaklanırken İyi Cin'in görsel parlaklığı artar (Kötü Cinler uzaktan fark edebilir).
- **Lamba Yak / Onar (`E`):** `ReqLampLight` (anında) / `ReqLampRepair` (`2 sn` basılı).
- **İşaret (`Q`), Kov (`R` basılı `1.5 sn`):** `ReqPing`, `ReqExorcise`.
- **Eşyaya Gir (`Shift+E`):** Kaydırma, görev eşyası taşıma ve Limon Kolonyası ile ayıltma.

---

## 7. Bulmacalar & İlerleme (CM.Objectives) ⭐

### 7.1 Genel model — `PuzzleState` (düz C#)
```csharp
public sealed class PuzzleState
{
    public int PartCount { get; }
    public int ValueCount(int part);          // her parçanın K değeri
    public int Get(int part);
    public IReadOnlyList<int> Solution { get; }
    public int ReferenceRotation { get; }     // Mühür Taşları için 0, 90, 180, 270 derece
    public int[] TrapValues { get; }          // Tılsımlı Şalter için referanstaki 2 tuzak sembol (X)
    public bool IsSolved { get; }
    public bool IsLocked { get; }             // onaylandıktan sonra true
    public double ScrambleCooldownUntil { get; set; }

    // İnsan op tipleri: Cycle(part, +1/-1), Set(part, value), Swap(a, b)
    public bool TryApply(in PuzzleOp op);     // locked ise false
    // Kötü cin toplu sabotajı: çözülmemiş rastgele bir dizilime sokar
    public bool TryScrambleAll(GameRandom rng);
    public void ResetToSolution();            // raund başı
}
```
Her bulmaca bu modele indirgenir (v2.1):
| Tip | Parça x değer | İnsan Op | Özel Referans Verisi |
|---|---|---|---|
| **Paintings (Portreler)** | 3 portre x 4 açı (90 derece) | Cycle | Bıyıklı Dede, Pörtlek Nine, Ters Eşek portreleri |
| **Statue (Heykelin Nazarlığı)** | 2 parça x {6 çevre objesi, 3 asa eğimi} | Cycle | Gölge 6 objeden (Mum, Kafatası, Kedi, İbrik, Çarık, Ayna) birini gösterir |
| **Wires (Tılsımlı Şalter)** | 4 soket x 6 köy motifi | Cycle | Referansta 4 doğru motif + üzerinde **Çarpı (X)** olan 2 tuzak motif (`TrapValues`) |
| **Sigil (Mühür Taşları)** | 12 çizgi x 2 (boyalı/boş) | Set (Boya gerekli) | Köşelerde 4 sembol (Ay, Güneş, Yıldız, Kemik); referans `ReferenceRotation` kadar dönük çizilir |
| **Books** *(sonra)* | 5 permütasyon + 1 x 4 | Swap + Cycle | — |
| **Jigsaw** *(sonra)* | 6 permütasyon | Swap | — |

### 7.2 PuzzleDefinition (SO)
```csharp
public class PuzzleDefinition : ScriptableObject
{
    public PuzzleType Type;               // Paintings, Statue, Wires, Sigil, Books, Jigsaw
    public string NameKey;
    public int[] ValueCounts;             // parça başına K
    public PuzzleOpKind OpKind;
    public string RequiredItemId;         // "" = yok ("paint", "fusekey")
    public GameObject InteractivePrefab;  // P_Puzzle_*
    public GameObject ReferencePrefab;    // P_PuzzleRef_* (SpiritOnly, Ruh Perdesi destekli)
    public PuzzleSlotSize Size;           // Wall / Floor / Table — slot uyumu
}
```

### 7.3 Bulmaca ve Toplu Sabotaj Akışı (network)
```
İnsan Parça Değişimi:
  ReqPuzzleOp{puzzleSlot, part, op}
  -> Otorite: rol == Human, menzil + gerekli eşya kontrolü
  -> state.TryApply -> PuzzleStateChanged{slot, values[], by}

Kötü Cin Toplu Sabotajı (Anti-Spam):
  Kötü cin bulmacaya bakıp ScrambleHoldTime (1.5 sn) E basılı tutar -> ReqPuzzleScramble{slot}
  -> Otorite doğrulamaları:
       1. Rol == EvilJinn ve JinnsAwake
       2. Oda karanlık (IRoomService.LightOf(room) != On)
       3. İnsan fener koruma alanında DEĞİL (!IHumanStatusQuery.IsInLanternSafeZone(puzzlePos))
       4. Net.Time >= state.ScrambleCooldownUntil
       5. Enerji >= ScrambleEnergyCost (25)
  -> Başarılıysa: enerji düş, state.TryScrambleAll(rng),
     state.ScrambleCooldownUntil = Net.Time + ScramblePuzzleCooldown (15 sn)
  -> Broadcast: PuzzleScrambled{slot, values[], by, cooldownUntil}

İnsan Onay Kolu:
  ReqPuzzleConfirm{slot} -> Otorite: IsSolved?
    ✔ -> Lock -> PuzzleConfirmResult{ok} + PuzzleCompleted{slot} -> ScoreService (+50)
      -> Ödül (ItemSpawned) -> Aşama kontrolü (StageCompleted -> GateOpened + StageTimeBonus +90 sn)
    ✘ -> ConfirmFailDamage (1 hasar) + ConfirmFailCooldown (5 sn) -> PuzzleConfirmResult{fail}
```

### 7.4 Bileşenler
| Bileşen | Tip | Görev |
|---|---|---|
| `PuzzleSystem` | servis (otoriter) | Slot -> `PuzzleState`; op ve toplu sabotaj handler'ları; `IObjectiveInfo` |
| `PuzzleView` | MonoBehaviour | Değerleri görsele çevirir. Sabotajda yüksek sesli gıcırtı (odada Radyo çalmıyorsa) + titreme |
| `PuzzlePart` | IInteractable | Parça başına; sadece İnsan etkileşime girer |
| `PuzzleScrambleTarget` | IInteractable | Bulmaca gövdesi; sadece Kötü Cin `1.5 sn` basılı tutarak toplu sabotaj yapar |
| `ConfirmLever` | IInteractable | Onay kolu (İnsan) |
| `PuzzleReference` | SpiritOnly görsel | İyi Cin `FocusReference` (Sağ Tık) basılı tutarken cevabı net gösterir |
| `StageGate` | — | `StageCompleted` -> açılır + sayaca `StageTimeBonus` (`+90 sn`) eklenir |
| `KeyHideSpot` | IInteractable | Bahçede anahtar arama |
| `DigSpot` | IInteractable | Kürek gerekli. `DigTotalTime = 18 sn`, `DigCheckpoints = 3` (her `6 sn`'de bir katman tamamlanır ve ilerleme o katmana kilitlenir) |

### 7.5 RoundSetupPlanner (düz C#, seed'li, EditMode test) ⭐
Girdi: `HouseIndex` (odalar, slotlar), `PuzzleDefinition[]` havuzu, `GameBalanceConfig`, seed. Çıktı: `RoundPlan`.
1. Her aşama için (`StagesActive` kadar): o aşamanın `PuzzleSlot`'larından `PuzzlesPerStage` tanesini seç.
2. Her seçili slota uyumlu (`Size`, tip daha önce kullanılmamış) bir `PuzzleDefinition` ata.
3. Her bulmaca için `ReferenceSlot` seç: farklı oda (mümkünse farklı kat), tip uyumlu, tekrar yok.
4. Cevaplar: her parça için `GameRandom.Range(0, K)` (+ Mühür Taşları için `ReferenceRotation`, Şalter için 2 adet `TrapValues`).
5. Eşya ödülleri — bağımlılık çözümü: gerekli eşyalar (`RequiredItemId`) + Kürek -> ihtiyaç duyulduğu bulmacadan ÖNCEKİ aşamadaki veya AYNI aşamadaki diğer bulmacanın ödülüne atanır.
6. Ev anahtarının saklandığı yer (`KeyHideSpot`), Limon Kolonyasının yeri (`CologneSpawnSpot`), gerçek kazı noktası (`DigSpot`).

---

## 8. Ev Kontratı (Marker'lar) ⭐

| Marker | Alanlar | Kural |
|---|---|---|
| `RoomMarker` | roomId, floor (-1 mahzen, 0 bahçe/zemin, 1, 2), `BoxCollider` | Her oda 1 tane; `RoomOf(p)` bununla |
| `StageZone` | stageIndex, roomIds[] | Aşama 0 = bahçe, 1.. = katlar |
| `PuzzleSlot` | stageIndex, roomId, `PuzzleSlotSize`, allowedTypes, `RewardPoint` | Aşama başına en az 3 |
| `ReferenceSlot` | roomId, `PuzzleSlotSize`, allowedTypes | Toplam en az 8 |
| `StageGate` | stageIndex, door transform | Açıldığında `+90 sn` (`StageTimeBonus`) verir |
| `GateLock` | requiredItemId | Ön kapı (ev anahtarı), mahzen kapağı (bodrum anahtarı) |
| `LampMarker` + `LightSwitch` | roomId | Her oda en az 1 |
| `KeyHideSpot` | — | Bahçede en az 5 |
| `CologneSpawnSpot` | roomId | Ev içinde en az 3 aday nokta (seed 1 tanesine `PD_Cologne` koyar) |
| `DigSpot` | — | Mahzende en az 3 |
| `HumanSpawnMarker` / `JinnSpawnMarker` | — | 1 / 3 |
| `ToolShedItemPoint` | itemId (`blessed_oil`) | Bahçede alet kulübesinde (Kutsanmış Yağ) |

---

## 9. Maç Yönetimi, Dinamik Süre ve Kademeli Puanlama (CM.Match)

```
Lobby -> RoundSetup -> RoleReveal(3s) -> Intro(5s) -> Playing -> RoundEnd(8s) ─┬─► RoundSetup (raund < 4)
                                                                            └─► MatchEnd
```
- **Dinamik Gün Doğumu Sayacı:** `Playing` başladığında `StateEndTime = Net.Time + Config.BaseRoundDuration` (`360 sn`). Ön kapı veya bir `StageGate` açıldığında otorite `StateEndTime += Config.StageTimeBonus` (`+90 sn`) uygular ve `RoundTimeAdded` yayınlar.
- **Kademeli Puanlama (`ScoreService`):**
  - Bulmaca tamamlandığında (`PuzzleCompletedEvt`): İnsan ve İyi Cin'e `+Config.ScorePerPuzzle` (`50`).
  - Mahzen kapağı açıldığında: İnsan ve İyi Cin'e `+Config.ScoreBasementOpen` (`50`).
  - Hazine kazıldığında (`SeekersWin`): İnsan ve İyi Cin'e `+Config.ScoreTreasureWin` (`150`) + ` kalanSaniye`.
  - İnsan her bayıldığında (`HumanFaintedEvt`): Kötü Cinlere `+Config.ScorePerFaint` (`50`).
  - Kötü Cinler raundu kazandığında (`Timeout` veya `Faint`): Kötü Cinlere `+Config.JinnWinPoints` (`200`) + `(çözülmemişBulmacaSayısı * Config.ScorePerUnfinishedPuzzle)` (`40`).
- **Maç Sonu Unvanları (`TitleCalculator`):** "Elektrikçi", "Bozguncu" (en çok sabotaj), "Ampul Katili", "Navigasyon", "Tekmeci Dayı", "Terlik Nişancısı" (Anne Terliği isabeti), "Kolonyacı" (kolonya ile ayıltma / Ruh Siperi).

---

## 10. UI (CM.UI)
uGUI + TextMeshPro. `UIRoot` Canvas 1920x1080 referans.
- **İnsan HUD:** Sabit fener durumu (Zayıf / Kutsanmış Yağlı), 3 görev eşyası slotu, 3 kademeli kazı barı, `+90 sn` süre eklenme animasyonu.
- **İyi Cin HUD:** Sağ Tık Ruh Perdesi odaklanma göstergesi, Ruh Çarpışması bulanıklık efekti, Mahzen Ruh Siperi bildirimi.
- **Kötü Cin HUD:** Bulmaca sabotaj dolum barı (`1.5 sn`) ve bulmacanın kalan sabotaj bekleme süresi (`15 sn`), Fener Koruması uyarı ikonu.

---

## 11. Debug & Test Altyapısı

### 11.1 Hotseat
**F1-F4** oyuncu değiştir · **F5** insan bayılmaz · **F6** sınırsız enerji · **F7** her şeyi göster (SpiritOnly ve Ruh Perdesi açık) · **F8** yeni seed ile raund · **F9** overlay · **F10** zaman x2 · **F11** mevcut aşamayı tamamla (+90 sn) · **F12** Dummy İnsan botu.

### 11.2 Menü komutları (MCP `execute_menu_item`)
```
CinliMahzen/Debug/Switch To Player 1..4
CinliMahzen/Debug/Restart Round (New Seed)
CinliMahzen/Debug/Faint Human
CinliMahzen/Debug/Revive Human With Cologne
CinliMahzen/Debug/Complete Current Stage
CinliMahzen/Debug/Solve Puzzle Under Cursor
CinliMahzen/Debug/Scramble All Puzzles
CinliMahzen/Debug/Give All Quest Items + Blessed Oil
CinliMahzen/Debug/Toggle All Lights
CinliMahzen/Debug/Dump State To Console
CinliMahzen/House/Validate
CinliMahzen/House/Print Round Plan (Seed=12345)
CinliMahzen/House/Plan Batch Report (500 seeds)
```

### 11.3 Testler
- **EditMode (zorunlu):** `PuzzleState` (op'lar, `TryScrambleAll`, `ScrambleCooldownUntil`, kilit), `RoundSetupPlanner` (determinizm + 500 seed geçerlilik + eşya bağımlılığı + kolonya/referans dönüşü), `PossessionArbiter` (ışık kuralı + özel eşyalar), `ScoreService` (kademeli puanlama), `HumanHealth` (bayılma + kolonya ile anında ayılma), `MatchStateMachine` (dinamik `+90 sn` süre), tüm NetMsg round-trip.

---

## 12. Online (M5 — EN SON) — Photon PUN 2
- PUN 2 import -> `Assets/Photon/`. AppId repo'ya commit edilmez. Region `eu`.
- `PunNetBridge : INetBridge` (`MsgCode` 1:1 event code; `SendToAuthority` = MasterClient, `Broadcast` = All).
- **Ev nesneleri PhotonView kullanmaz** — NetId + RaiseEvent. Raund planı tüm istemcilerde aynı seed'den üretilir (`HouseHash` + plan hash kontrolü).

---

## 13. Performans & Build
- En az 90 FPS @1080p orta sistem. Tek gölgeli ışık (insan feneri); lambalar gölgesiz point light.
- Update'lerde GC allocation 0. Raund kurulumu < 1 sn.
- Windows x64, `CinliMahzen/Build/Windows` menüsü -> `Builds/`.

---

## 14. v2.0 -> v2.1 Kod Geçişi Özeti
| Alan | Değişiklik |
|---|---|
| `GameBalanceConfig` | `RoundDuration=900` yerine `BaseRoundDuration=360` + `StageTimeBonus=90`. Ekle: `LanternSafeZoneRadius`, `ScrambleHoldTime`, `ScramblePuzzleCooldown`, `FocusReferenceCost`, `SpiritBumpBlurTime`, `SpiritBlockEnergyCost`, `DigCheckpoints`, kademeli puan alanları |
| Sabotaj & Fener | Parça başı karıştırma kaldırıldı -> `PuzzleScrambleTarget` ile 1.5 sn basılı toplu sabotaj + 15 sn cooldown + 2.5 m Fener Koruması |
| Fener & Kulübe | Fener sabit ekipman oldu; kulübedeki eşya `ID_BlessedOil` (Kutsanmış Yağ) olarak güncellendi |
| Ruh Etkileşimleri | İyi Cin `FocusReference` (Ruh Perdesi), Kötü Cin `SpiritBump` (Ruh Çarpışması), Mahzende `SpiritBlock` (Ruh Siperi) eklendi |
| Özel Eşyalar | `PD_Slipper` (Anne Terliği), `PD_Radio` (Eski Radyo - ses bastırma), `PD_Cologne` (Limon Kolonyası - anında ayıltma) eklendi |
| Puanlama | `ScoreService` kademeli puanlama modeline geçirildi |