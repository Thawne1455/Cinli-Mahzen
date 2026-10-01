# CİNLİ MAHZEN — Teknik Tasarım Dokümanı

> Sürüm: **v2.0 — 2026-10-01** (tek geliştirici + Claude Code; ev + bulmaca tasarımı)
> Oyun kuralları: `01_GDD_Oyun.md`. Görevler: `03_TODO.md`. Asset eşleştirme: `04_Asset_Eslestirme.md`.
> Kontratlar (§4, §8) değişirse bu doküman **aynı commit'te** güncellenir.

---

## 0. Özet Kararlar

| Konu | Karar |
|---|---|
| Motor | **Unity 6 — 6000.3.18f1**, URP, Input System |
| Geliştirme | **Tek geliştirici + Claude Code (Unity MCP ile).** Ajan/sahiplik sistemi yok. |
| Network | **Photon PUN 2 — en son fazda (M5).** O zamana kadar offline, ama kod ilk günden network'e hazır (§4.3 `INetBridge`). |
| Otorite | **Master Client otoriter**: can/bayılma, possession, enerji, bulmaca durumları, ışıklar, envanter, maç. Oyuncu hareketi sahip-otoriter. |
| Fizik | Oyun sonucunu etkileyen hiçbir şey Rigidbody'ye bağlı değil. Fırlatma/kaydırma kinematik + otorite hit-check. Rigidbody sadece kozmetik. |
| Dünya | **Elle kurulmuş ev** (`P_House` prefabı). Rastgelelik = **seed'li yerleşim** (`RoundSetupPlanner`): bulmaca→oda, referans→oda, cevaplar, eşya ödülleri, anahtar/kazı noktası. Prosedürel harita üreticisi **yok**. |
| Bulmacalar | Tek genel model: **N parça × K değer** (`PuzzleState`). Her bulmaca tipi = veri (`PuzzleDefinition` SO) + görsel (`PuzzleView`). |
| Veri | Tüm denge değerleri **ScriptableObject** (`GameBalanceConfig`, `PossessableDefinition`, `ItemDefinition`, `PuzzleDefinition`). Sihirli sayı yok. |
| Test | Mantık düz C# → **EditMode**. Akışlar → PlayMode + Unity MCP duman testi. |
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
│   ├── Possessables/  P_Poss_* (küçük/büyük/raf/lamba)
│   ├── Environment/   Env_* (duvar, zemin, kapı, merdiven…)
│   ├── Props/         Prop_* (possessable olmayan dekor)
│   ├── Puzzles/       P_Puzzle_Paintings, P_Puzzle_Statue, P_Puzzle_Wires, P_Puzzle_Sigil + referans prefabları
│   ├── World/         P_House (elle kurulmuş ev), P_Gate_*, P_Lamp_*, P_DigSpot, P_KeyHideSpot
│   ├── Items/         P_Item_* (görev eşyaları, yerdeki hali)
│   └── UI/
├── ScriptableObjects/ Config/  Possessables/  Items/  Puzzles/  World/
├── Scenes/            Boot, MainMenu, Game, Sandbox (serbest test)
├── Scripts/
│   ├── Core/          CM.Core — kontratlar, servisler, EventBus, Net soyutlaması
│   ├── Match/         CM.Match — maç durum makinesi, rol rotasyonu, puan
│   ├── Player/        CM.Player — insan, piyon tabanı, kamera, input, envanter
│   ├── Jinn/          CM.Jinn — ruh formu, iyi cin yetenekleri, enerji
│   ├── Possession/    CM.Possession — possession, eşya aksiyonları
│   ├── Visibility/    CM.Visibility — rol bazlı görünürlük
│   ├── World/         CM.World — ev kontratı (marker'lar), oda/ışık servisi, RoundSetupPlanner
│   ├── Objectives/    CM.Objectives — bulmacalar, aşamalar, geçitler, eşya ödülleri, kazı
│   ├── Audio/  UI/  DebugTools/  Net/Pun/ (M5)  Editor/
└── Tests/ EditMode/  PlayMode/
```

### 1.1 Assembly'ler ve Bağımlılıklar (değişmedi)
```
CM.Core          → —                      [Unity.InputSystem]
CM.Player        → CM.Core
CM.Possession    → CM.Core
CM.Jinn          → CM.Core, CM.Possession
CM.Visibility    → CM.Core
CM.World         → CM.Core                [Unity.AI.Navigation]
CM.Objectives    → CM.Core, CM.World
CM.Audio / CM.UI → CM.Core
CM.Match         → CM.Core, CM.World
CM.DebugTools    → CM.Core, CM.Match, CM.Player, CM.Jinn, CM.World
CM.Net.Pun       → CM.Core                [Photon]
CM.Editor        → hepsi (Editor-only)
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
| 10 | `SpiritPawn` | Cin piyonları |
| 11 | `EvilJinnVisual` | Kötü cin görselleri |
| 12 | `GoodJinnVisual` | İyi cin görselleri (insan da soluk görür) |
| 13 | `SpiritOnly` | **Bulmaca referansları**, anahtar parıltısı, gerçek kazı noktası (iki cin de görür) |
| 14 | `JinnOnlyFX` | Nişan okları vb. (sadece kötü cinler) |
| 15 | `Interactable` | Etkileşim raycast hedefleri (bulmaca parçaları, kol, düğme, kapı) |
| 16 | `Projectile` | Fırlatılan eşyalar |
| 17 | `CosmeticPhysics` | Kırık parçalar |
| 18 | `XRayOutline` | Duvar arkası outline |

Fizik matrisi değişmedi: `HumanPawn ↔ Environment, Possessable` · `Projectile ↔ Environment, HumanHitbox, Possessable` · `CosmeticPhysics ↔ Environment, CosmeticPhysics` · `SpiritPawn ↔ hiçbiri`.

**Culling (lokal role göre `CameraRig`):**
| Rol | Ek görür | Görmez |
|---|---|---|
| İnsan | GoodJinnVisual (soluk materyal) | EvilJinnVisual, SpiritOnly, JinnOnlyFX, XRayOutline |
| İyi Cin | GoodJinnVisual, EvilJinnVisual*, SpiritOnly, XRayOutline | JinnOnlyFX |
| Kötü Cin | EvilJinnVisual, GoodJinnVisual (soluk), SpiritOnly, JinnOnlyFX, XRayOutline | — |

\* Mesafe kuralları `VisibilityService` ile (§6.6).

---

## 3. Sahne & Uygulama Akışı

```
Boot ──► GameServices (DontDestroyOnLoad), Config, NetBridge = Offline
MainMenu ──► "Test Oyunu" (hotseat) / "Online" (M5)
Game ──► MatchController
           ├─ P_House (sahnede sabit, elle kurulmuş)
           ├─ HouseService.Index()          → oda/marker/NetId kaydı (bir kez)
           ├─ RoundSetupPlanner.Plan(seed)  → RoundPlan (her raund)
           ├─ RoundSetupApplier.Apply(plan) → bulmaca/referans/eşya yerleşimi + reset
           ├─ PawnSpawner                   → 4 piyon
           └─ MatchStateMachine
```
- Ev **silinmez**; her raund `RoundSetupApplier` bulmacaları, kapıları, lambaları, eşyaları **sıfırlar** ve yeni plana göre kurar.
- Editörde `Game.unity` doğrudan Play → otomatik bootstrap + hotseat (MCP testi için kritik).

---

## 4. Core Kontratları (CM.Core)

### 4.1 Kimlikler ve Roller (mevcut, değişmedi)
`Role {None, Human, GoodJinn, EvilJinn}`, `Team {None, Seekers, Jinns}`, `PlayerId(byte)`, `NetId(int)` — aralıklar: 1..999 piyon/sistem, 1000..59999 ev nesneleri (deterministik), 60000+ runtime spawn.

### 4.2 Oyuncu Kaydı (mevcut)
`IPlayerRegistry { Players, LocalPlayer, GetRole, Get, LocalPlayerChanged, RolesChanged }`, `PlayerInfo { Id, Nickname, Role, Score }`.

### 4.3 Network Soyutlaması — EN ÖNEMLİ KURAL
```
[İstemci] Request ──► [OTORİTE] doğrula + durumu değiştir ──► Broadcast ──► [Herkes] uygula
```
Offline'da `OfflineNetBridge` aynı frame içinde döngüye sokar. `...Authority` ile biten metotlar sadece `Net.IsAuthority` iken çağrılır (başında `Debug.Assert`).

`INetBridge { IsOnline, IsAuthority, LocalPlayer, Time, SendToAuthority, Broadcast, Register, Unregister }` · `NetMsg { Code, Sender, SentTime, object[] Payload }` — payload sadece PUN-serileştirilebilir tipler.

**MsgCode aralıkları (v2):**
```
1-29    Maç:        MatchStateChanged, RoundSetup{seed, roles}, RoundEnded, RolesAssigned, ScoreChanged
30-59   İnsan:      ReqInteract, InteractResult, HumanDamaged, HumanFainted, HumanRevived,
                    ReqKick, KickResult, StatusApplied, ReqLanternPulse, LanternPulsed,
                    InventoryChanged, ReqDropItem, ItemDropped, ReqPush, PushResult
60-99   Cinler:     ReqPossess, PossessBegan, PossessCompleted, PossessEnded, PossessDenied,
                    ReqAction, ActionTelegraph, ActionResolved, ActionDenied, EnergyChanged,
                    JinnStunned, ProjectileSpawned, ProjectileImpact, ReqExorcise, ExorciseProgress,
                    ExorciseResult, ReqPing, PingPlaced, RageStarted, PossessedMove, ObjectStateChanged,
                    ReqLampLight, ReqLampRepair
100-139 Dünya/Bulmaca: HouseReady, ReqPuzzleOp, PuzzleStateChanged, ReqPuzzleConfirm, PuzzleConfirmResult,
                    PuzzleCompleted, StageCompleted, GateOpened, ItemSpawned, ItemPickedUp,
                    LightsChanged, KeyFound, DigProgress, TreasureDug, HouseHash
200-254 Debug
```
Her yeni mesaj için encode/decode yardımcısı + **EditMode round-trip testi** zorunlu.

### 4.4 Varlık Kaydı (mevcut)
`INetEntity`, `IEntityRegistry { Register, Unregister, TryGet<T>, AllocateRuntimeId }`. Ev nesnelerine NetId'yi `HouseService.Index()` **deterministik sırayla** atar (hiyerarşi yolu sıralı).

### 4.5 Hasar, Durum, Etkileşim
- `DamageInfo { Amount, Attacker, SourceObject, CauseId, Flags, KnockdownTime, Point, Direction }` — v2'de `Lethal` bayrağı kullanılmaz (ölüm yok); can 0 → **bayılma**.
- `IDamageable.ApplyDamageAuthority`, `IStatusReceiver.ApplyStatusAuthority`.
- `StatusType`: `Knockdown, Fainted, Stunned, Slowed` (+ eski değerler sıra bozulmasın diye korunur; `PD_*` asset'leri int saklar).
- `IInteractable { NetId, PromptKey, HoldTime, CanInteract(who, role), ValidateAuthority(who), ExecuteAuthority(who), NoiseOnInteract }`.
- **v2 eki:** `IInteractable.CanInteract` artık rol alır → bulmaca parçası insan için "çevir", kötü cin için "karıştır" olur (aynı bileşen).

### 4.6 EventBus (lokal, mevcut)
Ana event'ler (v2):
| Event | Yayınlayan | Dinleyen |
|---|---|---|
| `RoundStartedEvt{roundIndex, seed}` | Match | herkes |
| `HumanDamagedEvt{info, hpAfter}` | Player | UI, Audio, Stats |
| `HumanFaintedEvt{info, faintCount}` / `HumanRevivedEvt` | Player | Match, UI, Stats |
| `PossessionChangedEvt{player, obj, state}` | Possession | UI, Visibility, Input, Audio |
| `ActionTelegraphEvt` / `ActionResolvedEvt` | Possession | Audio, VFX, Stats |
| `PuzzleStateChangedEvt{puzzle, byPlayer, scrambled}` | Objectives | Audio, VFX, Stats |
| `PuzzleCompletedEvt{puzzle, stage}` / `StageCompletedEvt{stage}` | Objectives | UI, Match, Audio |
| `RoomLightChangedEvt{room, state}` | World | Possession (eject), Visibility, UI |
| `DigProgressEvt{progress}` / `TreasureDugEvt` | Objectives | Match, Jinn (Öfke), UI |
| `LocalRoleChangedEvt{role}` | Core | Kamera, UI, Visibility, Input |
| `RoundEndedEvt{result, reason}` | Match | UI, Stats |

### 4.7 Servisler (mevcut)
`GameServices { Net, Players, Entities, Config, Match, Level, Register<T>, Get<T>, TryGet<T> }`.

### 4.8 Köprü Arayüzleri (v2)
```csharp
// Possession uygular
IKickable { NetId; OnKickedAuthority(PlayerId by) }
IPossessionQuery { IsPossessed, PossessorOf, GetPossessedInCone(...) }

// World uygular
IRoomService   { int RoomOf(Vector3 p); RoomLightState LightOf(int room); }      // eski ILevelInfo yerine
ILightService  { void SetLampAuthority(NetId lamp, LampState s); }              // Off / On / Broken
IHouseInfo     { Bounds Bounds; Vector3 HumanSpawn; IReadOnlyList<Vector3> JinnSpawns; }

// Objectives uygular
IObjectiveInfo { int StageIndex; int StageCount; bool IsPuzzleDone(int slot); float DigProgress; ObjectivePhase Phase; }
enum ObjectivePhase : byte { Garden = 0, House = 1, Cellar = 2, Done = 3 }

// Match uygular
IMatchInfo { MatchState State; int RoundIndex; double StateEndTime; bool JinnsAwake; int Faints; }
```
Kaldırılanlar: `IPossessionBlocker(Registry)` (tuz yok), `IInteractInterceptor` (mimik yok), `GoldStateEvt`, `KeyFragmentCollectedEvt`. Yerine **ışık kuralı** possession arbiter'a `IRoomService.LightOf` üzerinden girer.

### 4.9 Konfigürasyon
`GameBalanceConfig` — `01_GDD_Oyun.md §14` her satır bir alan, aynı isim. `ItemDefinition { Id, NameKey, Icon, Prefab, IsQuestItem }`. `PossessableDefinition` (§6.3). `PuzzleDefinition` (§7.2).

---

## 5. İnsan Sistemleri (CM.Player)

### 5.1 Piyon Mimarisi (mevcut tasarım)
`PawnBase (NetEntity) { Owner, Role, IPawnInput Input, IsLocallyControlled }`. Hotseat: 4 piyon, lokal olmayanlar `NullInput`. `IHumanInput { Move, Look, Sprint, InteractHeld/Pressed, KickPressed, LanternPressed, SelectSlot(-1/0/1/2), DropPressed }`.

### 5.2 HumanController
CharacterController, zıplama yok, **merdiven = rampa collider** (çok katlı ev). Durumlar (`HumanMotorState`, düz C#): `Normal`, `Interacting` (kazı/itme — hareket yok), `KnockedDown`, `Fainted`.

### 5.3 HumanHealth & Bayılma (otoriter)
```
ApplyDamageAuthority: dokunulmazlık? → HP -= dmg → HumanDamaged
  HP == 0 → faints++ → HumanFainted{faints} → seçili eşyayı düşür (ItemDropped)
           → faints >= FaintsToLose ? Match: JinnsWin(Faint) : FaintDuration sonra HumanRevived (HP = Max)
```
Bayılmışken hasar almaz. Kozmetik: kamera yere düşer, ekran kararır, kalp atışı sesi.

### 5.4 Etkileşim, Envanter, Tekme, Fener
- **Interactor:** kamera merkezinden `Interactable` raycast (`InteractRange`), basılı tutma lokal, tamamlanınca `ReqInteract(netId)`. Otorite menzil (`InteractRange + NetRangeTolerance`) + `ValidateAuthority` → `ExecuteAuthority`.
- **Inventory (otoriter):** `ItemSlotCount` slot. Görev eşyaları `ItemDefinition.IsQuestItem`. Eşya gerektiren etkileşimler (`GateLock`, `PuzzleRequirement`) otoritede envanteri sorar. `ReqDropItem` → yerdeki `P_Item_*` runtime spawn (NetId 60000+) — **possessable küçük eşya**.
- **Push:** büyük mobilya `PushHoldTime` → `ReqPush(obj, dir)` → otorite hücre kaydırma (Possession'ın kaydırma mantığını paylaşır).
- **Kick / Lantern:** v1 ile aynı (`IKickable`, `IPossessionQuery.GetPossessedInCone`). Fener bir eşya; elde değilse Parlat yok.

---

## 6. Cin & Possession Sistemleri (CM.Jinn, CM.Possession, CM.Visibility)

### 6.1 SpiritController
FPS uçuş, noclip, kinematik. Sınır: `IHouseInfo.Bounds` (bahçe dahil) içinde clamp; zemin altına (mahzen hariç) inemez.

### 6.2 Possession Durum Makinesi (mevcut B0.1 kodu, uyarlanacak)
```
Free → Entering(PossessTime) → Possessed(Lurking ⇄ Charging → Recovering) → Free
Özel: Spent (tek kullanımlık bitti)
```
**`PossessionArbiter.TryPossess(player, obj)` kuralları (v2):**
1. Rol `EvilJinn` **veya** `GoodJinn` (iyi cin sadece `AllowGoodJinn` işaretli eşyalara), sersem/kilitli değil
2. `JinnsAwake`
3. Nesne `Free`, `Spent` değil
4. **Işık kuralı:** kötü cin ise ve nesnenin odası `On` → red (`PossessDenyReason.Lit`) — **istisna:** nesne `IsLamp`
5. Mesafe ≤ `PossessRange + NetRangeTolerance`
6. `ReenterCooldown` dolmuş, oyuncu başka nesnede değil

**Aydınlanınca atılma:** `RoomLightChangedEvt(On)` → otorite o odadaki kötü cinli nesneler için `LitRoomEjectDelay` sonra `PossessEnded{reason=Lit}`.

### 6.3 PossessableDefinition (mevcut SO, alan ekleri)
Mevcut alanlar + `bool AllowGoodJinn`, `bool IsLamp`, `PossessableSize Size {Small, Large, Shelf, Lamp, QuestItem}`. Aksiyon `Id`'leri (v2): `throw`, `hop`, `lunge`, `slide`, `topple`, `burst`, `fling` (görev eşyası). İyi cin için hasarsız varyant: aksiyon `Damage` iyi cinde 0'a zorlanır.

`PD_*` asset'leri (v2): `PD_Bottle`, `PD_Candle`, `PD_Plate`, `PD_Stool`, `PD_Chair`, `PD_Barrel`, `PD_Chest`, `PD_Table`, `PD_Shelf`, `PD_Lamp`, `PD_QuestItem`. (Eski `PD_Keg`, `PD_SwordShield`, `PD_Torch` kaldırılır.)

### 6.4 Aksiyon Akışı (mevcut tasarım)
`ReqAction → otorite doğrula, enerji düş → ActionTelegraph{resolveAt} → hit-check → ActionResolved`. Fırlatma = otorite `ProjectileSpawned{runtimeId, origin, dir, speed, arc, t0}`, herkes görsel simüle eder, otorite `SphereCast` ile ilerletir. **Kaydırma (`slide`)** = hücre bazlı (1.5 m), hedefte `Environment`/başka mobilya varsa red; kapı geçidini tıkayabilir (tasarım).
**Patlatma (`burst`)** → `ILightService.SetLampAuthority(lamp, Broken)`.

### 6.5 Enerji, Sersemlik, Öfke
`JinnEnergyCore` (mevcut) — kötü ve iyi cin ayrı ayarlarla. Öfke: `DigProgressEvt` ilk > 0 → `RageStarted`.

### 6.6 Görünürlük (lokal, network'süz)
`01_GDD_Oyun.md §12` tablosu. `SpiritOnly` artık **iki cin rolüne** de açık. İyi cin insana soluk görünür (ayrı materyal, `GoodJinnVisual`).

### 6.7 İyi Cin Yetenekleri
- **Lamba Yak:** `ReqLampLight(lamp)` (menzil `LampLightRange`) → `SetLampAuthority(On)`. **Onar:** `ReqLampRepair` basılı tut `RepairHoldTime`.
- **İşaret, Kov:** v1 ile aynı akış (`ReqPing`, `ReqExorcise`).
- **Eşyaya Gir:** aynı arbiter (kural 1), aynı aksiyon akışı, hasarsız.

### 6.8 Kameralar
`CameraRig`: `FpsMode`, `OrbitMode` (eşya içi), `FaintMode` (yerde, kararan ekran). Geçiş 0.3 sn.

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
    public bool IsSolved { get; }
    public bool IsLocked { get; }             // onaylandıktan sonra true

    // Op tipleri: Cycle(part, +1/-1), Set(part, value), Swap(a, b)
    public bool TryApply(in PuzzleOp op);     // locked ise false
    public void ResetToSolution();            // raund başı
}
```
Her bulmaca bu modele indirgenir:
| Tip | Parça × değer | Op |
|---|---|---|
| Paintings | 3 × 8 | Cycle |
| Statue | 2 × {12, 4} | Cycle |
| Wires | 4 × 4 | Cycle (renk) |
| Sigil | 12 × 2 | Set (boya/sil) — Boya gerekli |
| Books (sonra) | 5 permütasyon + 1 × 4 | Swap + Cycle |
| Jigsaw (sonra) | 6 permütasyon | Swap |

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
    public GameObject ReferencePrefab;    // P_PuzzleRef_* (SpiritOnly)
    public PuzzleSlotSize Size;           // Wall / Floor / Table — slot uyumu
}
```

### 7.3 Akış (network)
```
İnsan / Kötü cin: ReqPuzzleOp{puzzleSlot, part, op}
Otorite: rol kontrolü (insan: menzil + gerekli eşya; kötü cin: menzil ScrambleRange, oda karanlık,
         enerji ScrambleEnergyCost, ScrambleInterval) → state.TryApply → PuzzleStateChanged{slot, values[], by}
İnsan:   ReqPuzzleConfirm{slot} → otorite: IsSolved?
           ✔ → Lock → PuzzleConfirmResult{ok} + PuzzleCompleted{slot} → ödül (ItemSpawned) → aşama kontrolü
           ✘ → ConfirmFailDamage (IDamageable) + ConfirmFailCooldown → PuzzleConfirmResult{fail}
Aşamadaki tüm slotlar tamam → StageCompleted{stage} → GateOpened{gate}
```
Bulmaca durumu **tam değer dizisi** olarak yayınlanır (küçük; geç gelen istemci için de doğru).

### 7.4 Bileşenler
| Bileşen | Tip | Görev |
|---|---|---|
| `PuzzleSystem` | servis (otoriter) | Slot → `PuzzleState`; mesaj handler'ları; `IObjectiveInfo` |
| `PuzzleView` | MonoBehaviour | Değerleri görsele çevirir (tablo açısı, kablo rengi, çizgi). Telgraf: karıştırmada gıcırtı + titreme |
| `PuzzlePart` | IInteractable | Parça başına; insan → çevir, kötü cin → karıştır |
| `ConfirmLever` | IInteractable | Onay (insan) |
| `PuzzleReference` | SpiritOnly görsel | Cevabı `PuzzleView` ile aynı kodla çizer (`values = Solution`) |
| `StageGate` | — | `StageCompleted` → açılır (kapı animasyonu) |
| `ItemReward` | — | Bulmaca tamamlanınca ödül eşyasını slotun `RewardPoint`'inde spawn eder |
| `KeyHideSpot` | IInteractable | Arama; gerçeği plan seçer |
| `DigSpot` | IInteractable | Kürek gerekli, ilerleme otoritede birikir, `DigTotalTime` → `TreasureDug` |

### 7.5 RoundSetupPlanner (düz C#, seed'li, EditMode test) ⭐
Girdi: `HouseIndex` (odalar, slotlar), `PuzzleDefinition[]` havuzu, `GameBalanceConfig`, seed. Çıktı: `RoundPlan`.
```
1. Her aşama için (StagesActive kadar): o aşamanın PuzzleSlot'larından PuzzlesPerStage tanesini seç
2. Her seçili slota uyumlu (Size, tip daha önce kullanılmamış) bir PuzzleDefinition ata
3. Her bulmaca için ReferenceSlot seç: farklı oda (mümkünse farklı kat), tip uyumlu, tekrar yok
4. Cevaplar: her parça için GameRandom.Range(0, K)
5. Eşya ödülleri — bağımlılık çözümü:
     gerekli eşyalar (RequiredItemId) + Kürek → her biri, ihtiyaç duyulduğu bulmacadan ÖNCEKİ aşamadaki
     veya AYNI aşamadaki diğer bulmacanın ödülüne atanır; çözülemezse aşamadaki tip seçimini yeniden dene (max 20)
6. Anahtar saklanma yeri, gerçek kazı noktası
```
**Doğrulama (`RoundPlanValidator`):** her gerekli eşya erişilebilir, referans ≠ bulmaca odası, tip tekrarı yok. **Test:** aynı seed → aynı plan (hash); 500 seed → hepsi geçerli.

### 7.6 RoundSetupApplier (MonoBehaviour)
Plan'ı uygular: slotlara bulmaca prefabını yerleştirir (raund başında havuzdan / önceden instantiate edilmiş, aktif/pasif), referansları yerleştirir, `PuzzleState.ResetToSolution`, kapıları kapatır, lambaları `Off`, eşyaları kaldırır, possessable'ları başlangıç konumlarına döndürür. NetId: slot sırası + tip ile deterministik (`1000 + slotIndex*16 + part`).

---

## 8. Ev Kontratı (Marker'lar) ⭐

Ev elle kurulur; oyun sistemleri evi **sadece marker bileşenleri** üzerinden tanır.

| Marker | Alanlar | Kural |
|---|---|---|
| `RoomMarker` | roomId, floor (-1 mahzen, 0 bahçe/zemin, 1, 2), `BoxCollider` (trigger, oda hacmi) | Her oda 1 tane; `RoomOf(p)` bununla |
| `StageZone` | stageIndex, roomIds[] | Aşama 0 = bahçe, 1.. = katlar |
| `PuzzleSlot` | stageIndex, roomId, `PuzzleSlotSize`, allowedTypes (Flags), `RewardPoint` | Aşama başına ≥ 3 (çeşitlilik için) |
| `ReferenceSlot` | roomId, `PuzzleSlotSize`, allowedTypes | Toplam ≥ 8 |
| `StageGate` | stageIndex, door transform | Aşama sonunda açılır |
| `GateLock` | requiredItemId | Ön kapı (ev anahtarı), mahzen kapağı (bodrum anahtarı) |
| `LampMarker` + `LightSwitch` | roomId | Her oda ≥ 1 |
| `KeyHideSpot` | — | Bahçede ≥ 5 |
| `DigSpot` | — | Mahzende ≥ 3 |
| `HumanSpawnMarker` / `JinnSpawnMarker` | — | 1 / 3 |
| `ToolShedItemPoint` | itemId (fener) | Bahçede |

**`HouseValidator`** (editör menüsü `CinliMahzen/House/Validate`): zorunlu marker sayıları, her aşamada yeterli slot, her bulmaca tipi için en az 2 uygun slot ve 2 uygun referans slotu, her oda lambalı, NavMesh bake edilmiş.

**Geometri:** KayKit grid **4 m** hücre, duvar yüksekliği **4 m**, kalınlık 1 m (`04_Asset_Eslestirme.md`). Kat yüksekliği 4 m → kat k zemini y = 4k. Merdivenler `Env_Stairs` (5 m yükselme → kat arası için ölçeklenir/ara sahanlık). NavMesh: `NavMeshSurface` ev köküne.

---

## 9. Maç Yönetimi (CM.Match)

```
Lobby → RoundSetup → RoleReveal(3s) → Intro(5s) → Playing → RoundEnd(8s) ─┬─► RoundSetup (raund < 4)
                                                                            └─► MatchEnd
```
- Bitiş (otorite): `TreasureDug` → SeekersWin · süre 0 → JinnsWin(Timeout) · `faints >= FaintsToLose` → JinnsWin(Faint).
- `RoundEndReason { None, Treasure, Timeout, Faint }` (eski `Kill/Exit` değerleri yerine).
- Rol rotasyonu: İnsan = `players[r]`, İyi = `players[(r+1)%4]`.
- `ScoreService`: `01_GDD_Oyun.md §10`. `StatsService` + `TitleCalculator` (düz C#).

---

## 10. UI (CM.UI)
uGUI + TextMeshPro. `UIRoot` Canvas 1920×1080 referans. Her HUD bir `RoleHud`, `LocalRoleChangedEvt` ile değişir. Türkçe karakterli font. Ekranlar `01_GDD_Oyun.md §13`.

---

## 11. Debug & Test Altyapısı

### 11.1 Hotseat
**F1-F4** oyuncu değiştir · **F5** insan bayılmaz · **F6** sınırsız enerji · **F7** her şeyi göster (SpiritOnly herkese) · **F8** yeni seed ile raund · **F9** overlay · **F10** zaman ×2 · **F11** mevcut aşamayı tamamla · **F12** Dummy İnsan botu.

### 11.2 Menü komutları (MCP `execute_menu_item`)
```
CinliMahzen/Debug/Switch To Player 1..4
CinliMahzen/Debug/Restart Round (New Seed)
CinliMahzen/Debug/Faint Human
CinliMahzen/Debug/Complete Current Stage
CinliMahzen/Debug/Solve Puzzle Under Cursor
CinliMahzen/Debug/Scramble All Puzzles
CinliMahzen/Debug/Give All Quest Items
CinliMahzen/Debug/Toggle All Lights
CinliMahzen/Debug/Dump State To Console      ← MCP doğrulaması buradan okunur
CinliMahzen/House/Validate
CinliMahzen/House/Print Round Plan (Seed=12345)
CinliMahzen/House/Plan Batch Report (500 seeds)
```

### 11.3 Testler
- **EditMode (zorunlu):** `PuzzleState` (op'lar, kilit, çözülme), `RoundSetupPlanner` (determinizm + 500 seed geçerlilik + eşya bağımlılığı), `PossessionArbiter` (ışık kuralı dahil), `JinnEnergy`, `HumanHealth` (bayılma, kaybetme), `MatchStateMachine`, rol rotasyonu, tüm NetMsg round-trip, `TitleCalculator`.
- **PlayMode:** raund başlar → insan bahçede; F11 ile aşamalar → kazı → SeekersWin; Faint ×3 → JinnsWin.

---

## 12. Online (M5 — EN SON) — Photon PUN 2
- PUN 2 import → `Assets/Photon/`. AppId repo'ya commit edilmez. Region `eu`.
- `PunNetBridge : INetBridge` (`MsgCode` 1:1 event code; `SendToAuthority` = MasterClient, `Broadcast` = All; `EnergyChanged`, `PossessedMove` unreliable).
- `PunPlayerRegistry`, `PunPawnSync` (20 Hz, 100 ms interpolasyon), `PunPawnSpawner`, `PunLobbyUI`.
- **Ev nesneleri PhotonView kullanmaz** — NetId + RaiseEvent. Raund planı tüm istemcilerde aynı seed'den üretilir (`HouseHash` + plan hash kontrolü).
- Master ayrılırsa / oyuncu ayrılırsa: raund iptal → lobi.
- Test: Unity Multiplayer Play Mode (paket kurulu).

---

## 13. Performans & Build
- ≥ 90 FPS @1080p orta sistem. Tek gölgeli ışık (insan feneri); lambalar gölgesiz point light.
- Update'lerde GC allocation 0. Raund kurulumu < 1 sn.
- Windows x64, `CinliMahzen/Build/Windows` menüsü → `Builds/`.

---

## 14. v1 → v2 Kod Geçişi (mevcut kodda değişecekler)
Mevcut kod v1 tasarımına göre yazıldı. `03_TODO.md` **M0.5 Pivot** görevleri şunları yapar:
| Alan | Değişiklik |
|---|---|
| `GameBalanceConfig` | Kaldır: tuz, nazar, altın, mühür, ısı izi, gürültü, kutsa alanları. Ekle: §14 GDD v2 alanları |
| Core/Events | Kaldır: `GoldStateEvt`, `KeyFragmentCollectedEvt`, `HumanDiedEvt`, `NoiseEvt`. Ekle: §4.6 v2 event'leri |
| Core/Bridges | Kaldır: `IPossessionBlocker(Registry)`, `IInteractInterceptor`, `ILevelInfo`. Ekle: `IRoomService`, `IHouseInfo`; `ILightService`, `IObjectiveInfo`, `ObjectivePhase`, `RoundEndReason`, `IMatchInfo` güncelle; stub'ları güncelle |
| `MsgCode` | §4.3 v2 listesi |
| `ItemDefinition` | `ID_Salt`, `ID_Nazar` sil; görev eşyaları ekle |
| Possession | `PossessionArbiterCore`: tuz kuralı → ışık kuralı, iyi cin desteği. `PD_*` asset'leri §6.3 v2 listesi |
| Input | `IHumanInput`: 3 slot, Drop; `ISpiritInput`: Bless → Exorcise/LampLight/PossessAsGood |
| DebugTools | F11 / menüler §11 |
| World (C dalı) | `agent-c/m0-assets-scene` dalından **sadece** KayKit import + `Env_*` / `Prop_*` prefabları + materyal alınır. `ProceduralLevelGenerator`, `LevelPlanner`, Level kontratı **alınmaz** |
