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
CM.Player        -> CM.Core                [Unity.InputSystem, URP Runtime (CameraRig blur)]
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

**CameraRig (1.2):** Arayüz `ICameraRig` (`Scripts/Core/Camera/`: `SetFps(head)`, `SetOrbit(target, dist)`, `SetFaint(body)`, `TriggerBlur(sn)`, `Mode`, `Camera`) Core'da; uygulaması `CameraRig` (CM.Player, sahnenin Main Camera'sında) `GameServices.Register<ICameraRig>` ile kaydolur ve pozu LateUpdate'te dünya uzayında yazar. Mantık düz C# (`Scripts/Player/Camera/Logic/`, EditMode testli): `CameraBlendState` (mod geçişi 0.3 sn, ilk bağlanış anlık), `OrbitSolver` (orbit; `Physics.SphereCast` ile `Environment` layer'ına çarpan mesafe kısaltır), `BlurPulse` (bulanıklık zarfı), `CullingMasks` (yukarıdaki tablo; `LocalRoleChangedEvt` → `camera.cullingMask`, rol yokken İnsan maskesi). Orbit yaw/pitch'i `IPossessedInput.Look`'tan (`LocalInputSource`) alır.
**Bulanıklık (`VisionBlurred`):** global URP `Volume` + Gaussian `DepthOfField` override'ı; `Volume.weight` = `BlurPulse` yoğunluğu (0..1; fade-in/out ve sarsıntı genliği `CameraRig` alanları) + küçük konum sarsıntısı. Tetikleyiciler: `SpiritBumpedEvt` (`GoodJinn` yerel oyuncuysa) ve hedefi yerel piyon olan `StatusApplied(VisionBlurred)`; süre `SpiritBumpBlurTime`; üst üste tetikleme süreyi uzatır, toplamaz (aynı çarpışma iki yoldan gelse de 2 sn).
**Kurulum:** `CinliMahzen/Setup/Add Camera Rig To Open Scene` (idempotent, sahneyi kaydetmez): Main Camera'ya `CameraRig` + post-processing, çocuk `CM_BlurVolume` (weight 0) ve `Assets/_Project/Settings/CM_BlurProfile.asset`. Debug: `CinliMahzen/Debug/Trigger Vision Blur`, `Cycle Camera Mode (Fps-Orbit-Faint)`; Dump State bölümü: `camera`.

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
`OfflinePlayerRegistry` (`Scripts/Core/Players/`, düz C#, 4 slot `P0..P3`, takma ad `P1..P4`): `GameBootstrap` kaydeder ve `OfflineNetBridge`'in yerel oyuncusunu ondan okutur (`localPlayer: () => registry.LocalPlayer`). Somut sınıfta `SetLocalPlayer(PlayerId)` (F1-F4; `LocalPlayerChanged`, yerel rol değiştiyse `LocalRoleChangedEvt`) ve `SetRoles(Role[])` (`RolesChanged`, yerel rol değiştiyse `LocalRoleChangedEvt`) bulunur; ikisi `IPlayerRegistry`'de değildir (1.10/M5'te karar verilecek). İlk roller (raund 0) `HotseatBootstrap` (DebugTools hook'u, `RoleRotation`) ile atanır; 1.10 entegrasyonunda `MatchController` atar.
`PawnSpawner` (`Scripts/Match/Spawning/`, sahnede; prefab slotları `P_Human` / `P_Spirit`): `Start`'ta roller varsa ve her `RolesChanged`'de eski piyonları yok edip (önce `SetActive(false)`: NetId hemen boşalır) rolü olan her oyuncu için piyon kurar — insan → `P_Human` (`IHouseInfo.HumanSpawn`), iki cin → `P_Spirit` (`JinnSpawns` sırayla). **NetId = 1 + oyuncu slotu** (P0→N1 … P3→N4; piyon rolle değil oyuncuyla sabit). Saf kısım `PawnSpawnPlanner` (EditMode testli). Sahnedeki elle konmuş piyon, spawner'ın planladığı bir NetId'yi tutuyorsa çalışma anında uyarıyla silinir; `Add Pawn Spawner To Open Scene` elle konmuş P_Human/P_Spirit'leri sahneden kaldırır.

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
- `StatusType`: `Knockdown, Stunned, Slowed, CameraSpin180 (Anne Terliği), VisionBlurred (Ruh Çarpışması)`. v1'den kalan `Grabbed, Drunk, Darkness` int kararlılığı için enum'da durur, kullanılmaz. Bayılma bir durum efekti değildir: `HumanMotorState.Fainted` + `HumanFaintedEvt`.
- `IInteractable { NetId, PromptKey, HoldTime, CanInteract(who, role), ValidateAuthority(who), ExecuteAuthority(who), NoiseOnInteract }`.
- **v2.1 kuralı:** Bulmaca parçası (`PuzzlePart`) sadece `Role.Human` için `CanInteract` döner; Kötü Cinler parçaları tek tek çevirmez, bulmacanın ana gövdesindeki `PuzzleScrambleTarget` bileşenine `1.5 sn` basılı tutarak toplu sabotaj yapar.
- **Etkileşim (TODO 1.4, `Scripts/Player/Interaction/`):** Yerel `Interactor` (insanın `Head`'inden `InteractRange` raycast, `Interactable` layer; `InteractHoldTracker` basılı tutma; tamamlanınca `ReqInteract` → otorite `InteractionAuthority`: menzil (`InteractValidation`, `+NetRangeTolerance`) → `ValidateAuthority` → `ExecuteAuthority` → `InteractResult{ok}`). İpucu `InteractPromptEvt{PromptKey, Progress, Visible}` yalnız değer değişince yayınlanır. Kodlayıcılar `ReqInteractMsg` / `InteractResultMsg` (`Scripts/Core/Net/`).
- **Test küpü:** `DebugInteractableCube` (NetId 1001, 1 sn), menü `CinliMahzen/Setup/Place Test Interactable In Open Scene` → (0, 0.5, -3).

### 4.6 EventBus (lokal)
Ana event'ler (v2.1):
| Event | Yayınlayan | Dinleyen |
|---|---|---|
| `RoundStartedEvt{roundIndex, seed}` | Match | herkes |
| `RoundTimeAddedEvt{bonusSeconds, newEndTime}` | Match | UI, Audio |
| `HumanDamagedEvt{info, hpAfter}` | Player | UI, Audio, Stats |
| `HumanFaintedEvt{info, faintCount}` / `HumanRevivedEvt{byCologne}` | Player | Match, UI, Stats |
| `BlessedOilCollectedEvt{collector}` | Player | UI, Audio, Objectives |
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
| `DigProgressEvt{progress, checkpoint}` / `TreasureDugEvt{digger}` | Objectives | Match, Jinn (Öfke), UI |
| `LocalRoleChangedEvt{role}` | Core | Kamera, UI, Visibility, Input |
| `RoundEndedEvt{result, reason}` | Match | UI, Stats |

### 4.7 Servisler
`GameServices { Net, Players, Entities, Config, Match, House, Register<T>, Get<T>, TryGet<T> }`. Modül kendi uygulamasını kaydedene kadar köprü arayüzleri stub'a çözülür: `StubMatchInfo`, `StubHouseInfo`, `StubRoomService` (her yer oda 0, karanlık), `StubObjectiveInfo`, `StubLightService`, `StubHumanStatusQuery`, `StubPossessionQuery`.

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
    int RoomOf(Vector3 p);   // -1 = hiçbir odada değil; LightOf(-1) == Off
    RoomLightState LightOf(int room);       // enum RoomLightState : byte { Off = 0, On = 1 }
    bool IsNoiseSuppressed(int room); // Eski Radyo çalıyorsa true (12 sn)
}
ILightService  { void SetLampAuthority(NetId lamp, LampState s); } // enum LampState : byte { Off = 0, On = 1, Broken = 2 }
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
enum RoundEndReason : byte { None = 0, Faint = 1, TreasureDug = 2, Timeout = 3 }
```

### 4.9 Konfigürasyon
`GameBalanceConfig` — `01_GDD_Oyun.md §14` tablosundaki her satır birebir aynı isimle yer alır (`BaseRoundDuration`, `StageTimeBonus`, `LanternSafeZoneRadius`, `ScrambleHoldTime`, `ScramblePuzzleCooldown`, `FocusReferenceCost`, `SpiritBumpBlurTime`, `SpiritBumpRange`, `SpiritBlockEnergyCost`, `SpiritBlockRadius`, `CologneReviveRange`, `DigCheckpoints`, `ScorePerPuzzle`, `ScoreBasementOpen`, `ScoreTreasureWin`, `ScorePerFaint`, `ScorePerUnfinishedPuzzle` dahil).

`ItemDefinition` (`ID_*`) — `{ Id, NameKey, Icon, Kind, Prefab }`; `ItemKind { QuestItem = 0, LanternUpgrade = 1 }`. Asset'ler: `ID_HouseKey` (`house_key`), `ID_BlessedOil` (`blessed_oil`, LanternUpgrade — slot kaplamaz), `ID_FuseKey` (`fuse_key`), `ID_Paint` (`paint`), `ID_Shovel` (`shovel`), `ID_CellarKey` (`cellar_key`). `NameKey = "item." + Id`.

---

## 5. İnsan Sistemleri (CM.Player)

### 5.1 Piyon Mimarisi
`PawnBase (NetEntity) { Owner, Role, IPawnInput Input, IsLocallyControlled }` — **CM.Core**'da (`Scripts/Core/Entities/PawnBase.cs`), çünkü CM.Jinn, CM.Player'ı göremez ve `SpiritController` de ondan türer. Serileştirilen `netId`/`ownerId`/`role` elle yerleştirilen piyonun (Sandbox) başlangıç durumudur; PawnSpawner (1.3) çalışma anında `SetNetId`/`SetOwner`/`SetInput` ile üzerine yazar. `OnEnable/OnDisable`'da `IEntityRegistry`'ye kaydolur/çıkar. Core'da `NullInput` yok: `Input` boş olabilir, alt sınıf boşu "hareketsiz" sayar. Hotseat: 4 piyon, lokal olmayanlar `NullInput`. `IHumanInput { Move, Look, Sprint, InteractHeld/Pressed, KickPressed, LanternPulsePressed, SelectSlot(-1/0/1/2), DropPressed }`.
`ISpiritInput { Move, Look, Vertical, Boost, PrimaryPressed/Held, FocusHeld, PossessAsGoodPressed, PingPressed, ExorciseHeld }` — `PrimaryPressed/Held` = `E` (kötü: eşyaya gir / bulmacayı sabote et; iyi: lamba yak/onar), `FocusHeld` = Sağ Tık, `PossessAsGoodPressed` = `Shift+E`, `PingPressed` = `Q`, `ExorciseHeld` = `R`. `Shift` basılıyken `E` `Primary` sayılmaz (`LocalInputSource`, `Boost` kontrolü). Tuş tablosu: `01_GDD_Oyun.md §11`.

### 5.2 HumanController
CharacterController, zıplama yok, **merdiven = rampa collider**. Durumlar (`HumanMotorState`, düz C#): `Normal`, `Interacting` (kazı/itme — hareket yok), `KnockedDown`, `Fainted`.
- **Düz C# mantık** (`Scripts/Player/Human/Logic/`, EditMode testli): `HumanMotor` (durum + `CanMove/CanLook/CanSprint`; öncelik `Fainted` > `KnockedDown` > `Interacting` > `Normal`; `KnockDown(until)` + `Tick(now)`; `Fainted`'te bakış da kapalı, diğerlerinde açık), `StaminaModel` (`Max/Drain/Regen/RegenDelay` `GameBalanceConfig`'ten, `Tick(dt, wantsSprint, isMoving, now)` sprint olup olmadığını döndürür) ve `YawSpin` (`Begin(degrees, duration)` / `Step(dt)`; kareler toplamı tam `degrees`).
- **`HumanController`** (`Scripts/Player/Human/HumanController.cs`, `PawnBase` + `IStatusReceiver`): yerel kontrolde `LocalInputSource`'u `InputMode.Human` ile kullanır; gövde yaw + `head` pitch; hız/stamina config'ten; zıplama yok, yerçekimi + `slopeLimit` 50° / `stepOffset` 0.4 m (his parametreleri, serileştirilmiş). Dump State bölümü: `human`.
- **Merdiven:** `Env_Stairs`/`Env_StairsWide` prefablarına basamak mesh collider'ının yanında basamak burunlarından geçen 45° eğik bir `RampCollider` (BoxCollider) eklenir (`CinliMahzen/Setup/Add Ramp Colliders To Stairs`); karakter basamaklara değil rampaya basar.
- **Anne Terliği Etkisi (`StatusType.CameraSpin180`):** İsabet halinde insanın look yaw açısı `0.15 sn` içinde `180 derece` döndürülür ve "ŞRAK!" ses efekti oynatılır. Akış: otorite `HumanController.ApplyStatusAuthority` → `StatusApplied` yayını → her istemcide hedef piyon yerel kontrol ediliyorsa `YawSpin.Begin(180, duration)`; süre `PD_Slipper` status süresinden gelir. Mesaj kodlayıcı `StatusAppliedMsg` (`Scripts/Core/Net/`, Jinn'in `VisionBlurred` için de kullanacağı için Core'da): payload `{ int netId, byte status, float duration, byte source }`.
- Kamera: yerel insan `ICameraRig.SetFps(head)` çağırır (1.2 `CameraRig`; eski `TempFpsCameraAttach` kaldırıldı).

### 5.3 HumanHealth, Bayılma ve Kolonya ile Ayıltma (otoriter)
```
ApplyDamageAuthority: dokunulmazlık? -> HP -= dmg -> HumanDamaged
  HP == 0 -> faints++ -> HumanFainted{faints} -> ScoreService.OnHumanFainted()
           -> seçili görev eşyasını düşür (ItemDropped)
           -> faints >= FaintsToLose ? Match: JinnsWin(Faint) : FaintDuration sonra HumanRevived{byCologne=false} (HP = Max)
```
- **Limon Kolonyası ile Anında Ayıltma:** İnsan `Fainted` durumundayken İyi Cin `PD_Cologne` eşyasının içindeyken insanın `3 m` yakınında aksiyonu tetiklerse (`CologneReviveTriggered`), otorite bayılma sayacını beklemeden anında `HumanRevived{byCologne=true}` yayınlar ve insanı tam canla ayağa kaldırır.
- **Çekirdek (düz C#, `CM.Player`):** `HumanHealthCore` (ayarlar: `HumanHealthSettings.FromConfig`) — `ApplyDamage(in DamageInfo, double now) → DamageResult{Outcome: Ignored|Damaged|Fainted|Lost, HpAfter, FaintCount, Knockdown, KnockdownTime}`, `Tick(now)` (süre dolunca ayılır), `TryReviveWithCologne()`, `ResetForRound()`. Zamanı çağıran verir (`Net.Time`); `Lost` = `FaintsToLose`'a ulaşan bayılma (`HumanFainted` yine yayınlanır, ardından `JinnsWin(Faint)`; bir daha ayılmaz). Knockdown sadece `DamageResult`'a aktarılır, çekirdek yönetmez.
- **Mesajlar (`Scripts/Player/Net`):** `HumanDamagedMsg{info, hpAfter}`, `HumanFaintedMsg{info, faintCount}` (tam `DamageInfo` taşır: otopsi `CauseId`, skor `Attacker`), `HumanRevivedMsg{byCologne, hp}`; ortak yardımcı `DamageInfoCodec`.
- **`HumanHealth`** (`Scripts/Player/Human/HumanHealth.cs`, P_Human üzerinde, `IDamageable` + `IHumanStatusQuery`): otoritede `HumanHealthCore` sahibi — `ApplyDamageAuthority` → `HumanDamaged` (+ `HumanFainted`; `Lost`'ta raundu `MatchController` `HumanFaintedEvt` ile bitirir), `Update`'te `Tick` → `HumanRevived{false}`, `ReviveWithCologneAuthority()` → `HumanRevived{true}`. Mesaj handler'ları **her istemcide** (otorite dahil) `HumanHealthMirror`'ı (düz C#, testli: `Hp/Faints/IsFainted`) günceller, `HumanController.SetFainted` ile motoru `Fainted`'e alır (yerel piyonda kamera `SetFaint` ↔ `SetFps`), `DamageFlags.Knockdown` varsa `HumanMotor.KnockDown` başlatır ve `HumanDamagedEvt/HumanFaintedEvt/HumanRevivedEvt` yayınlar. Raund sıfırlaması her istemcide `RoundStartedEvt` ile (`ResetForRound`). Eşya düşürme 2.7'de gelecek (kodda TODO). `OnEnable`'da `GameServices.Register<IHumanStatusQuery>(this)` (`IsBlessedLanternActive` fener 3.8'e kadar `false`); P_Human'a `HumanHealth` ve `HumanHitbox` (layer 9) trigger kapsülü `Build Human Prefab` ile eklenir — hasar verenler `GetComponentInParent<IDamageable>()` ile kökü bulur.
- **Debug:** F5 = `HumanHealth.Immortal` (otorite hasarı yok sayar); `CinliMahzen/Debug/Faint Human` (`HumanMaxHp`, `IgnoreInvuln`, `debug.faint`), `Revive Human With Cologne`, `Damage Human 1`, `Toggle Immortal Human (F5)`; Dump State bölümü `health` (`hp, maxHp, faints, fainted, immortal`). Bayılmışken hasar yok sayıldığı için ×3 bayıltmada arada ayıltmak gerekir.

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
- **Sınıflar (TODO 1.6, `Scripts/Jinn/Spirit/`):** `SpiritController`, `P_Spirit`'te `SpiritPawn`'ın kardeş bileşenidir (türeyen değil: sahiplik/yerellik/kamera/görsel `SpiritPawn`'da kalır; `SpiritPawn.IsLocalView` iken `Input as ISpiritInput` okur). Yerelde mouse bakışı (gövde yaw + `Eyes` pitch), kinematik uçuş (collider/Rigidbody yok → duvardan geçer; WASD bakış yönünde, Space/Ctrl dünya yukarı/aşağı, hız `EvilJinnSpeed`/`GoodJinnSpeed`, Shift'te `× SpiritBoostMult`), her karede `IHouseInfo.Bounds`'a clamp. Düz C# (`Logic/`, EditMode testli): `SpiritFlightModel`, `SpiritBumpRules` (`CanRequest` istemci ön kontrolü; `Validate` otorite kontrolü, red nedeni `SpiritBumpDenyReason`), `ReferenceFocusState` (otoriter odak bayrakları), `SpiritBumpSettings`; otorite tarafı `SpiritAuthority` (düz C#, `SpiritBootstrap` GameBootstrap kancasıyla bridge'e bağlanır).
- **Mesaj akışı** (kodlayıcılar `Scripts/Jinn/Net/`): iyi cin yerelde `FocusHeld` değişince `ReqFocusReference{focusing}` → otorite rolü (`GoodJinn`) doğrular, bayrağı değiştirir → `ReferenceFocusChanged{player, focusing}` → her makinede o oyuncunun `SpiritController.IsFocusingReference` aynası güncellenir + `ReferenceFocusChangedEvt`. Kötü cin yerelde Shift + odaklanan iyi cin `SpiritBumpRange` içinde + yerel bekleme geçtiyse `ReqSpiritBump{goodJinn}` → otorite rol/odak/istek sahibinin bekleme süresi/mesafe (`SpiritBumpRange + NetRangeTolerance`) doğrular → `ReferenceFocusChanged{good, false}` sonra `SpiritBumped{evil, good}` → herkes `SpiritBumpedEvt` yayınlar (CameraRig yerel iyi cinde bulanıklığı başlatır). Shift'i otorite göremez, yalnız istemci denetler. Yeni raund / roller değişince otorite tüm odakları kapatıp yayınlar, beklemeleri sıfırlar. Enerji (`FocusReferenceCost`) 3.5'te eklenir.
- **Debug:** `CinliMahzen/Debug/Toggle Focus (local good jinn)` (basılı tutmadan odak isteği; odak, başka oyuncuya geçince de korunur), `Teleport Local Spirit To Good Jinn` (yerel cini iyi cinin `debugTeleportDistance` = 1 m yanına ışınlar), `Request Spirit Bump` (yerel kötü cin adına istek; Shift/menzil/bekleme ön kontrolünü atlar, otorite kuralları geçerli — karar `[Spirit]` logunda); Dump State bölümü `spirits` (`id, net, role, local, pos, focusing, boostOn, lastBump`).

### 6.2 Possession Durum Makinesi
```
Free -> Entering(PossessTime) -> Possessed(Lurking <-> Charging -> Recovering) -> Free
Özel: Spent (tek kullanımlık bitti: Raf, Limon Kolonyası)
```
**`PossessionArbiterCore.TryPossess(player, obj, now)` kuralları (v2.1):** düz C# sınıfı; dış dünyayı `IPossessionWorld` ile okur (`GetRole`, `IsPossessionLocked`, `IsPlaying`, `JinnsAwake`, `LightOfObject(NetId) -> RoomLightState`, `TryGetDistance`). MonoBehaviour tarafı `LightOfObject`'i `IRoomService.LightOf(RoomOf(pos))` ile uygular. Nesneler `Register(NetId, PossessableTraits{IsLamp, AllowGoodJinn})` ile kaydedilir (`PossessableTraits.From(definition)`).
1. Rol `EvilJinn` **veya** `GoodJinn` (aksi halde `RoleNotAllowed`); iyi cin sadece `AllowGoodJinn` işaretli eşyalara (`GoodJinnNotAllowed`); sersem/kilitli değil (`Stunned`).
2. Maç `Playing` (`NotPlaying`) ve `JinnsAwake` (`JinnsAsleep`).
3. Nesne kayıtlı (`InvalidTarget`), `Free` (`Occupied`), `Spent` değil (`Spent`).
4. **Işık kuralı:** Kötü cin ise ve nesnenin odası `RoomLightState.On` -> red (`PossessDenyReason.Lit`) — **istisna:** `IsLamp` nesne. İyi cin ışıktan etkilenmez.
5. Mesafe <= `PossessRange + NetRangeTolerance` (`OutOfRange`).
6. `ReenterCooldown` dolmuş (`Cooldown`, iyi cin için de geçerli), oyuncu başka nesnede değil (`AlreadyPossessing`).

- `PossessDenyReason` değerleri byte olarak ağa gider: `NotEvilJinn` -> `RoleNotAllowed` (1, aynı değer), `8` ve `9` (v1 kutsama/tuz) emekli — yeniden kullanılmaz; yeni değerler sonda: `Lit = 13`, `GoodJinnNotAllowed = 14`.
- Kutsama (bless) ve tuz v2.1'de yoktur (`IPossessionBlocker*` kaldırıldı). `PossessEndReason.Salted` -> `RoomLit = 2`: kötü cin içerideyken oda aydınlanırsa `LitRoomEjectDelay` (2 sn) sonra `Eject` ile dışarı atılır (lamba hariç).

### 6.3 PossessableDefinition ve Özel Eşyalar (v2.1)
Mevcut alanlar + `bool allowGoodJinn`, `bool isLamp` (`size == Lamp` ile tutarlı olmalı), `PossessableSize size {Small=0, Slipper=1, Large=2, Shelf=3, Lamp=4, Radio=5, Cologne=6, QuestItem=7}`. v1'e özgü `isSearchableContainer` kaldırıldı.
`PossessableActionDef` yeni alan: `ActionUser usableBy {EvilOnly=0, GoodOnly=1, Both=2}` (`IsUsableBy(Role)`). Yay `projectileArc` (0 = yerçekimsiz düz çizgi), oda etkisi süresi `effectDuration` (Radyo 12 sn; 0 = yok / başkası geri alana kadar), tek kullanım `singleUse` (Raf, Kolonya sıçraması, kırılan küçük eşyalar) zaten vardı. İyi cinin kullanabildiği aksiyon `damage = 0`, `flags = None`, `applyStatus = None` olmak zorundadır; `allowGoodJinn` ile "en az bir iyi cin aksiyonu" birbirini gerektirir (`CollectProblems` denetler).
Aksiyon `Id`'leri: `throw`, `homing_slipper` (Anne Terliği), `lunge`, `slide`, `topple`, `burst`, `play_tune` (Radyo), `fling` (görev eşyası / kolonya fırlatma), `revive_splash` (İyi Cin kolonya ile ayıltma). `hop` bir tık aksiyonu değil hareket modudur (`canMove` + `Hop*` alanları). `nameKey`'ler `poss.<id>` / `action.<id>`. Aksiyonların çalışma zamanı davranışı görev 1.8'dedir; asset'ler yalnızca veridir.

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

Asset'lerde GDD'de olmayan, seçilmiş teknik değerler: küçük fırlatılanlar mermi `10 m/s`, menzil `10 m`, yay `1.5 m`, isabet yarıçapı `0.3 m`, kırılır (`singleUse`); `PD_Slipper` mermi `16 m/s`, menzil `15 m`, yarıçap `0.25 m`, `statusDuration = 0.15` (§5.1); `slide` hızı `3 m/s` (1.5 m), A/D `±45°`; `fling` `10 m/s`, yay `1.0 m`, menzil `8 m`; `revive_splash` `10` enerji (menzil `CologneReviveRange`); `burst`/`play_tune`/`revive_splash` hit-check şekli `Custom`.

### 6.4 Aksiyon Akışı ve Ruh Siperi (Spirit Block)
- Fırlatma = otorite `ProjectileSpawned{runtimeId, origin, dir, speed, arc, t0}`, otorite `SphereCast` ile ilerletir.
- **Ruh Siperi (Mahzen Finali):** `IObjectiveInfo.Phase == ObjectivePhase.Cellar` iken, otoritenin `Projectile` çarpışma kontrolü **İyi Cin'in ruh piyonu (`SpiritPawn`, yarıçap `1.0 m`)** ile kesişirse ve İyi Cin'in en az `SpiritBlockEnergyCost` (`30`) enerjisi varsa:
  1. Eşya insana ulaşmadan havada durdurulur ve yere düşer.
  2. İyi Cin'den `30` enerji düşülür ve hafif geri tepme uygulanır.
  3. `SpiritBlockedProjectile` yayınlanır (İyi Cin'e "Kolonyacı / Koruyucu" istatistiği yazar).

### 6.5 İyi Cin Yetenekleri ve Ruh Perdesi
- **Ruh Görüşü Odakla (`FocusReference`):** İyi Cin Sağ Tık basılı tutarken (`ISpiritInput.FocusHeld`), `FocusReferenceCost` (`2 enerji/sn`) harcar. `PuzzleReference` bileşenleri yalnızca lokal İyi Cin `IsFocusing` durumundayken sembol/açı detaylarını gösterir. Odaklanırken İyi Cin'in görsel parlaklığı artar (Kötü Cinler uzaktan fark edebilir).
- **Lamba Yak / Onar (`E`, `ISpiritInput.PrimaryPressed` / `PrimaryHeld`):** `ReqLampLight` (anında) / `ReqLampRepair` (`2 sn` basılı).
- **İşaret (`Q`, `ISpiritInput.PingPressed`), Kov (`R` basılı `1.5 sn`, `ISpiritInput.ExorciseHeld`):** `ReqPing`, `ReqExorcise`.
- **Eşyaya Gir (`Shift+E`, `ISpiritInput.PossessAsGoodPressed`):** Kaydırma, görev eşyası taşıma ve Limon Kolonyası ile ayıltma.

---

## 7. Bulmacalar & İlerleme (CM.Objectives) ⭐

### 7.1 Genel model — `PuzzleState` (düz C#)
**Dosya konumu:** tanım tarafı (`PuzzleDefinition`, `PuzzleType`, `PuzzleOpKind`, `PuzzleSlotSize`) `Scripts/Core/Config/` altında (CM.Core); durum tarafı (`PuzzleState`, `PuzzleOp`) `Scripts/Objectives/Logic/` altında (CM.Objectives). Sebep: `RoundSetupPlanner` (CM.World) `PuzzleDefinition` havuzunu okur, CM.World ise CM.Objectives'i referans edemez (Objectives → World bağımlı).
```csharp
public readonly struct PuzzleOp               // tek bayraklı op; fabrikalar doğrulamaz, TryApply doğrular
{
    public PuzzleOpKind Kind { get; }         // Cycle | Set | Swap (default(PuzzleOp) = None, hep reddedilir)
    public int Part { get; }                  // Swap'ta ilk parça
    public int Value { get; }                 // Cycle: yön (+1/-1), Set: yeni değer
    public int PartB { get; }                 // Swap: ikinci parça
    public static PuzzleOp Cycle(int part, int dir);
    public static PuzzleOp Set(int part, int value);
    public static PuzzleOp Swap(int a, int b);
}

public sealed class PuzzleState
{
    // Çözülmüş ve kilitsiz başlar (Oyun §4.2). Diziler kopyalanır. Geçersiz girdide ArgumentException:
    // uzunluk uyuşmazlığı, K < 2, çözüm değeri [0,K) dışı, allowedOps None/bilinmeyen bayrak,
    // referenceRotation ∉ {0,90,180,270}, tuzak değeri [0,maxK) dışı.
    // allowedOps == Swap (saf permütasyon, Jigsaw) ise ek olarak her K == PartCount ve çözüm 0..N-1 permütasyonu olmalı.
    public PuzzleState(int[] valueCounts, int[] solution, PuzzleOpKind allowedOps,
                       int referenceRotation = 0, int[] trapValues = null);

    public int PartCount { get; }
    public int ValueCount(int part);          // her parçanın K değeri (geçersiz parça: ArgumentOutOfRangeException)
    public int Get(int part);
    public IReadOnlyList<int> Solution { get; }   // salt okunur görünüm; dışarıya değiştirilebilir dizi verilmez
    public int ReferenceRotation { get; }     // Mühür Taşları için 0, 90, 180, 270 derece
    public IReadOnlyList<int> TrapValues { get; } // Tılsımlı Şalter için referanstaki 2 tuzak sembol (X); yoksa boş
    public PuzzleOpKind AllowedOps { get; }
    public bool IsSolved { get; }
    public bool IsLocked { get; }             // onaylandıktan sonra true (IsLocked ⇒ IsSolved)
    public double ScrambleCooldownUntil { get; set; }   // salt veri; PuzzleSystem yönetir
    public bool IsScrambleOnCooldown(double now);        // now < ScrambleCooldownUntil

    // İnsan op tipleri: Cycle(part, +1/-1) K içinde sarar, Set(part, value), Swap(a, b)
    // false: kilitli | op türü AllowedOps'ta yok | parça/değer aralık dışı | Cycle yönü ±1 değil | Swap aynı parça
    public bool TryApply(in PuzzleOp op);

    // Kötü cin toplu sabotajı: HER ZAMAN çözülmemiş bir dizilim üretir. Kilitliyse false.
    //  - AllowedOps == Swap (Jigsaw): çözümün değerleri karıştırılır (permütasyon korunur)
    //  - diğerleri (Books dahil): her parçaya rng.Range(0, K); Books'ta ilk 5 parça permütasyon kalmayabilir
    //  - sonuç çözümle aynı çıkarsa deterministik düzeltme (Swap: ilk iki parça takas; diğer: parça 0 +1 sar), döngü yok
    // Cooldown'ı KONTROL ETMEZ ve DEĞİŞTİRMEZ; bunu (enerji, ışık, fener kurallarıyla birlikte) PuzzleSystem yapar.
    public bool TryScrambleAll(GameRandom rng);

    public bool TryLock();                    // sadece IsSolved && !IsLocked iken kilitler; aksi false (çifte tamamlama olmaz)
    public void ResetToSolution();            // raund başı: değerler = çözüm, kilit açık, ScrambleCooldownUntil = 0

    // Ağ senkronu (allocation yok): otorite yayınlar, istemciler uygular
    public void CopyValuesTo(int[] buffer);   // ilk PartCount elemanı yazar; buffer kısa/null ise ArgumentException
    public bool ApplySnapshot(int[] values, bool locked);  // önce doğrular, geçersizse hiçbir şeyi değiştirmeden false
                                                           // (uzunluk, aralık, permütasyonda tekrar, locked ama çözüm değil)
}
```
`ApplySnapshot` otoritenin gerçeğidir: kilit durumunu iki yönde de ezer; `ScrambleCooldownUntil` taşımaz (mesajdan ayrıca atanır).
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
`Scripts/Core/Config/PuzzleDefinition.cs` (CM.Core, `CinliMahzen.Core`; sebep §7.1'de). Asset'ler `ScriptableObjects/Puzzles/PZ_*.asset`. Alanlar `[SerializeField] private`, dışarıya property olarak açılır. Enum'lar byte'tır ve asset'te int saklanır: `PuzzleType {Paintings=0, Statue=1, Wires=2, Sigil=3, Books=4, Jigsaw=5}`, `[Flags] PuzzleOpKind {None=0, Cycle=1, Set=2, Swap=4}` (Books = Swap|Cycle), `PuzzleSlotSize {Wall=0, Floor=1, Table=2}`.
```csharp
public sealed class PuzzleDefinition : ScriptableObject
{
    public const int MinValueCount = 2;
    public PuzzleType Type { get; }               // Paintings, Statue, Wires, Sigil, Books, Jigsaw
    public string NameKey { get; }
    public IReadOnlyList<int> ValueCounts { get; }// parça başına K (salt okunur görünüm)
    public PuzzleOpKind OpKind { get; }
    public string RequiredItemId { get; }         // "" = yok; ItemDefinition.Id ile aynı ("paint", "fuse_key")
    public GameObject InteractivePrefab { get; }  // P_Puzzle_*
    public GameObject ReferencePrefab { get; }    // P_PuzzleRef_* (SpiritOnly, Ruh Perdesi destekli)
    public PuzzleSlotSize Size { get; }           // Wall / Floor / Table — slot uyumu

    public int PartCount { get; }                 // ValueCounts.Count
    public bool HasRequiredItem { get; }
    public bool IsPermutation { get; }            // OpKind == Swap (yalnız Jigsaw). Books (Swap|Cycle) şimdilik permütasyon sayılmaz
    public int[] CopyValueCounts();               // PuzzleState ctor'una vermek için bağımsız kopya
    public static bool IsValidOpKinds(PuzzleOpKind kinds);   // None değil ve yalnız bilinen bayraklar
    public void CollectProblems(List<string> problems);     // boş nameKey, boş/K<2 valueCounts, OpKind None/bilinmeyen,
                                                             // permütasyonda K != PartCount
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

- **Dosyalar (CM.World):** marker bileşenleri `Scripts/World/Markers/`; `HouseService` (P_House kökünde, `Awake`'te `Index()`), `HouseIndex`/`HouseIndexBuilder` (düz veri), `RoomService`, `HouseInfo`, `HouseValidator` (kural sayıları `HouseContract`) `Scripts/World/House/`; graybox marker verisi `Scripts/World/Graybox/GrayboxMarkers.cs` (kurucu `Build Graybox` marker'ları + HouseService'i prefab'a ekler). `RoomMarker.roomId` int'tir (sabit tablo `GrayboxMarkers`, sona eklenir, sıra değişmez), `roomKey` Loc anahtarını verir (`room.<roomKey>`); L şekilli odaların birden fazla `BoxCollider`'ı olur, `RoomOf` yarı açık kutularla bakar, iç içe kutuda en küçük hacim kazanır (eşitse en düşük roomId). `RoomMarker.isOutdoor` (bahçe) lamba/düğme şartından muaftır.
- **NetId:** `NetId` taşıyan marker'lar (`PuzzleSlot`, `ReferenceSlot`, `StageGate`, `LampMarker`, `LightSwitch`, `KeyHideSpot`, `CologneSpawnSpot`, `DigSpot`, `ToolShedItemPoint`) anahtarı `P_House köküne göreli hiyerarşi yolu + "#" + marker tipi` olan ordinal sıralamayla 1000'den ardışık id alır (>59999 hata); sıra, kök adı ve konumdan bağımsızdır, serileştirilmez, `Index()` atar ve `IEntityRegistry`'ye kaydeder.
- **Doğrulama:** `CinliMahzen/House/Validate` açık sahnedeki (yoksa prefab'daki) `HouseService`'i indeksler, `HouseValidator` sonuçlarını loglar ve `House/Validate: N errors, M warnings` ile biter. `DigSpot` < 3 ve son aşamanın `StageGate`'i (mahzen kapağı) eksikliği **Warning**'dir (mahzen TODO 3.7'de gelir); diğer ihlaller Error.

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
- **Kod (`Scripts/Match/`, düz C#):** `Logic/MatchStateMachine` (akış, sayaç, raund sonu; yalnız otoritede), `Logic/MatchSettings` (`GameBalanceConfig`'ten), `Logic/RoleRotation` (raund r: İnsan = oyuncu r, İyi Cin = r+1). `Net/`: `MatchStateChangedMsg`, `RoundSetupMsg`, `RoundEndedMsg`, `RoundTimeAddedMsg` (encode/decode, EditMode testleri `Tests/EditMode/Match/`).
- **RoundSetup → RoleReveal:** `Tick` RoundSetup'ı kendiliğinden bitirmez; otorite `CompleteRoundSetup(now)` çağırır (offline: aynı frame; online: tüm istemciler `HouseReady` bildirince). Çağrı `StateEndTime = now + RoleRevealDuration` kurar.
- **`MatchController`** (`Scripts/Match/MatchController.cs`, Game sahnesinde tek; `CinliMahzen/Setup/Add Match Controller To Open Scene`; `IMatchInfo` olarak kayıtlı): otoritede her frame `Net.Time` ile `Tick` döner ve her `MatchTransition`'ı `MatchStateChanged` olarak yayınlar; `RoundSetup`'a girince `RoundSetup{seed, roles}` (`RoleRotation`) yayınlar ve offline'da aynı frame `CompleteRoundSetup` çağırır (online: `HouseReady` bekler, M5); `RoundEnd`'e girince `RoundEnded` yayınlar. Girdiler: `HumanFaintedEvt` (3 bayılma), `TreasureDugEvt`, `AddStageTimeBonusAuthority()` (kapı/geçit → `RoundTimeAdded`, 2.7). **Her makine** (otorite dahil) yayınları uygular: `MatchMirror` (düz C#, testli) `IMatchInfo`'yu besler; `RoundSetup` → registry `SetRoles` (`RolesChanged` → `PawnSpawner` piyonları yeniden kurar) → `RoundStartedEvt`; `RoundTimeAdded` / `RoundEnded` → `RoundTimeAddedEvt` / `RoundEndedEvt`. Maç ilk `Update`'te başlar; Game sahnesinde `HotseatBootstrap` rol atamaz.
- **Seed:** otorite maç başında bir maç seed'i seçer (`fixedSeed` ≠ 0 ise o, değilse `Environment.TickCount`'tan `RoundSeeds.CreateMatchSeed`); raund seed'i `RoundSeeds.ForRound(maçSeed, raund)` (`GameRandom`'ın (raund+1). çekilişi) `RoundSetup` ile herkese gider.
- **Debug:** F8 = `RestartWithNewSeed()` (Lobby'ye döner, yeni maç seed'iyle raund 0'dan başlar); F11 = `AddStageTimeBonusAuthority()` (şimdilik yalnız +90 sn; kapı/bulmaca 2.7); `Skip Match State` = `MatchStateMachine.TrySkipTimer` (RoleReveal/Intro/RoundEnd beklemesini kısaltır, Playing'de Timeout bitişi; yeni bitiş zamanı aynı durumla `MatchStateChanged` olarak yeniden yayınlanır). Dump `core.match`: `state, round, jinnsAwake, faints, stateEndTime, left, seed, lastResult, lastReason`.

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
**Oyuncu değiştirme akışı:** F1-F4 / `Switch To Player N` → `DebugCommands.SwitchToPlayer(i)` → `OfflinePlayerRegistry.SetLocalPlayer` → `LocalPlayerChanged` (+ `LocalRoleChangedEvt`: `CameraRig` culling, `LocalInputSource` haritası) → her `PawnBase` `OnLocalityChanged(bool)` alır (ilk `Start`'ta, her geçişte, `SetOwner` sonrası): yerel piyon `ILocalInputProvider.AcquireLocal(rol)` ile gerçek input + `ICameraRig.SetFps` + imleç kilidi (`AcquireCursorLock`) alır, diğerleri `Idle` (`NullInput`). `ILocalInputProvider` (Core; `LocalInputSource` uygular) CM.Jinn'in CM.Player'ı görmeden input alabilmesi içindir. Kurulum: `Build Spirit Prefab` → `Add Camera Rig To Open Scene` → `Add Pawn Spawner To Open Scene` (Game ve Sandbox'ta); Dump State bölümü `players` (`id, role, local, pawn, pos`).

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
CinliMahzen/Debug/Spin Human 180
CinliMahzen/Debug/Trigger Vision Blur
CinliMahzen/Debug/Cycle Camera Mode (Fps-Orbit-Faint)
CinliMahzen/House/Build Graybox
CinliMahzen/House/Place House In Open Scene
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