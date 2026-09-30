# CİNLİ MAHZEN — Teknik Tasarım Dokümanı

> Hedef okuyucu: **Claude Code ajanları** (A, B, C) ve geliştiriciler.
> Oyun kuralları için: `01_GDD_Oyun.md`. Görevler: `03_TODO.md`. Asset eşleştirme: `04_Asset_Eslestirme.md`.
> Bu dokümandaki **kontratlar (§4, §8)** değiştirilmeden önce `docs/CONTRACT_CHANGES.md`'ye kayıt düşülür (bkz. `CLAUDE.md`).

---

## 0. Özet Kararlar

| Konu | Karar |
|---|---|
| Motor | **Unity 6 — 6000.3.x LTS** (makinede `6000.3.18f1` kurulu). **3 kişi de birebir aynı sürümü kullanır.** |
| Render | **URP** (3D URP şablonu) |
| Input | **Input System** paketi (Active Input Handling = Input System Package (New)) |
| Network | **Photon PUN 2** — **en son fazda (M4)** entegre edilir. O zamana kadar oyun **offline** çalışır, ama kod **ilk günden network'e hazır** yazılır (§4 `INetBridge`). |
| Otorite modeli | **Master Client otoriter**: hasar, can, possession, enerji, hedefler, maç durumu. Oyuncu hareketi **sahip-otoriter** (owner). |
| Fizik | **Oyun sonucunu etkileyen hiçbir şey Rigidbody fiziğine bağlı değildir.** Saldırılar scripted (kinematik/animasyon) + otorite hit-check. Rigidbody sadece kozmetik (ragdoll, kırık parçalar) ve lokaldir. |
| Level | `ILevelGenerator` arayüzü. Uygulaması `ProceduralLevelGenerator` (C yazar, kalıcı; v1 → v2 geliştirilir). **Seed ile deterministik.** |
| Veri | Tüm denge değerleri **ScriptableObject** (`GameBalanceConfig`, `PossessableDefinition`, `ItemDefinition`). Kodda sihirli sayı yok. |
| Test | Mantık düz C# sınıflarında (MonoBehaviour'suz) → **EditMode testleri**. Akışlar → PlayMode testleri + Unity MCP ile duman testi. |
| Solo test | **Hotseat Debug Modu:** Tek editörde 4 piyon, F1-F4 ile rol değiştirme. Online olmadan tüm oyun test edilebilir. |

> **PUN 2 notu:** PUN 2, Photon tarafından bakım modunda (yeni projelere Fusion öneriliyor). Asset Store'dan hâlâ indirilebiliyorsa kullanılır. `INetBridge` soyutlaması sayesinde gerekirse sadece `Net/Pun` klasörü değiştirilerek Fusion/başka çözüme geçilebilir. Oyun kodu PUN tiplerini **asla** doğrudan kullanmaz.

---

## 1. Proje Yapısı

```
Assets/
└── _Project/
    ├── Art/
    │   ├── KayKit/            # (C) fbx(unity) klasöründen import, dokunulmaz
    │   ├── Materials/         # (C) M_KayKit_Dungeon vb.
    │   ├── VFX/               # (B) cin görselleri, telgraf efektleri
    │   └── Characters/        # (A) insan modeli (placeholder → gerçek asset)
    ├── Audio/                 # (C) placeholder SFX
    ├── Prefabs/
    │   ├── Player/            # (A) Human, SpiritBase (B'nin component'leri eklenir → bkz. §11)
    │   ├── Jinn/              # (B) EvilJinn, GoodJinn, VFX prefabları
    │   ├── Possessables/      # (B) P_Poss_Shelf, P_Poss_Barrel ...
    │   ├── Environment/       # (C) duvar, zemin, modüller
    │   ├── Objectives/        # (C) RuneStone, DigSpot, VaultDoor, GoldChest, Exit
    │   ├── Items/             # (A) Salt, Nazar pickups
    │   └── UI/                # her sahip kendi HUD'u
    ├── ScriptableObjects/
    │   ├── Config/            # (A) GameBalanceConfig.asset
    │   ├── Possessables/      # (B) PD_Shelf.asset ...
    │   ├── Items/             # (A)
    │   └── Level/             # (C) LevelGenSettings, LootTable
    ├── Scenes/
    │   ├── Boot.unity         # (A)
    │   ├── MainMenu.unity     # (A)
    │   ├── Game.unity         # (A — entegrasyon sahnesi, sadece A düzenler)
    │   ├── Sandbox_A.unity    # (A) kişisel test
    │   ├── Sandbox_B.unity    # (B) kişisel test
    │   └── Sandbox_C.unity    # (C) kişisel test
    ├── Scripts/
    │   ├── Core/              # (A) CM.Core — kontratlar, servisler, EventBus, Net soyutlaması
    │   ├── Match/             # (A) CM.Match
    │   ├── Player/            # (A) CM.Player — insan, ortak piyon, kamera, input
    │   ├── Jinn/              # (B) CM.Jinn
    │   ├── Possession/        # (B) CM.Possession
    │   ├── Visibility/        # (B) CM.Visibility
    │   ├── World/             # (C) CM.World — level kontratı, ProceduralLevelGenerator, populator
    │   ├── Objectives/        # (C) CM.Objectives
    │   ├── Audio/             # (C) CM.Audio
    │   ├── UI/                # CM.UI — Common (A), Human (A), Jinn (B), Round (C)
    │   ├── DebugTools/        # (A) CM.DebugTools
    │   ├── Net/Pun/           # (A, M4) CM.Net.Pun
    │   └── Editor/            # CM.Editor — menü komutları (her sahip kendi alt klasörü)
    └── Tests/
        ├── EditMode/          # CM.Tests.EditMode (her sahip kendi alt klasörü)
        └── PlayMode/          # CM.Tests.PlayMode
```

### 1.1 Assembly Definition'lar ve Bağımlılıklar

Döngüsel bağımlılık **yasaktır**. Modüller birbirini **Core'daki arayüzler + EventBus** ile tanır.

```
CM.Core          → (hiçbiri)                       [Unity.InputSystem]
CM.Player        → CM.Core
CM.Possession    → CM.Core
CM.Jinn          → CM.Core, CM.Possession
CM.Visibility    → CM.Core
CM.World         → CM.Core                        [Unity.AI.Navigation]
CM.Objectives    → CM.Core, CM.World
CM.Audio         → CM.Core
CM.Match         → CM.Core, CM.World
CM.UI            → CM.Core  (sadece Core arayüzleri + event'lerden okur)
CM.DebugTools    → CM.Core, CM.Match, CM.Player, CM.Jinn, CM.World
CM.Net.Pun       → CM.Core                        [PhotonUnityNetworking, PhotonRealtime]
CM.Editor        → hepsi (Editor-only)
CM.Tests.*       → ilgili modüller + test framework
```

**Kural:** `CM.Player` insan hasarını bilir ama raf kodunu bilmez. Raf (`CM.Possession`) insanı `IDamageable` üzerinden, `HumanHitbox` layer'ı ile bulur.

### 1.2 Namespace & Kodlama Standartları
- Namespace = asmdef adı: `CinliMahzen.Core`, `CinliMahzen.Possession`, ...
- Bir dosya = bir public tip; dosya adı = tip adı.
- `[SerializeField] private` alanlar; public alan yok (ScriptableObject tanım verileri hariç, onlar da property ile okunur).
- `Update` içinde `Find*`, `GetComponent`, LINQ, `new` allocation **yok**.
- Tüm zaman hesapları `Net.Time` (bkz. §4.3) üzerinden — `Time.time` doğrudan kullanılmaz (online'da senkron saat gerekiyor).
- Rastgelelik: Oyun durumunu etkileyen her rastgelelik **seed'li `System.Random`** (`GameRandom` sarmalayıcı). `UnityEngine.Random` sadece kozmetik için.
- Loglar: `CMLog.Info/Warn/Error("Possession", "mesaj")` — kategori etiketli.
- Yorumlar İngilizce veya Türkçe olabilir; **tip/metot/değişken adları İngilizce**.

---

## 2. Tags, Layers, Fizik Matrisi (M0'da A tarafından **bir kez** kurulur)

ProjectSettings çakışmasını önlemek için tüm layer'lar baştan tanımlanır. Sonradan layer eklemek **kontrat değişikliğidir**.

| # | Layer | Kullanım |
|---|---|---|
| 6 | `Environment` | Duvar, zemin, statik geometri |
| 7 | `Possessable` | Cin girebilen eşyalar (collider) |
| 8 | `HumanPawn` | İnsanın CharacterController'ı |
| 9 | `HumanHitbox` | İnsanın hasar alan trigger'ı (saldırılar bunu arar) |
| 10 | `SpiritPawn` | Cin piyonları (collider'ları sadece bakış/etkileşim için) |
| 11 | `EvilJinnVisual` | Kötü cin görselleri |
| 12 | `GoodJinnVisual` | İyi cin görselleri |
| 13 | `SpiritOnly` | Ruh ipuçları (sadece iyi cin görür) |
| 14 | `JinnOnlyFX` | Nişan okları, ısı izi, gürültü halkası (sadece kötü cinler) |
| 15 | `Interactable` | Etkileşim raycast hedefleri |
| 16 | `Projectile` | Kılıç, şişe mermileri |
| 17 | `CosmeticPhysics` | Ragdoll, kırık parçalar |
| 18 | `XRayOutline` | Duvar arkası outline render (URP Render Objects) |

**Fizik Matrisi (sadece işaretli olanlar çarpışır):**
- `HumanPawn` ↔ `Environment`, `Possessable`
- `Projectile` ↔ `Environment`, `HumanHitbox`, `Possessable`
- `CosmeticPhysics` ↔ `Environment`, `CosmeticPhysics`
- `SpiritPawn` ↔ **hiçbiri** (noclip; sınırlar kodla kısıtlanır)
- Diğer tüm kombinasyonlar kapalı.

**Tags:** `HumanSpawn`, `JinnSpawn`, `Exit`, `Vault` (marker bulma için yedek; asıl yol marker component'leri).

**Kamera Culling Mask'leri (lokal role göre `CameraRig` ayarlar):**

| Lokal Rol | Görür | Görmez |
|---|---|---|
| İnsan | Default, Environment, Possessable, Interactable, Projectile, CosmeticPhysics | EvilJinnVisual, GoodJinnVisual, SpiritOnly, JinnOnlyFX, XRayOutline |
| İyi Cin | Yukarıdakiler + GoodJinnVisual, EvilJinnVisual*, SpiritOnly, XRayOutline | JinnOnlyFX |
| Kötü Cin | Yukarıdakiler + EvilJinnVisual, GoodJinnVisual (soluk), JinnOnlyFX, XRayOutline | SpiritOnly |

\* Kötü cin görselleri iyi cin için ayrıca mesafe kurallarına göre `VisibilityService` tarafından açılıp kapanır (§6.6).

---

## 3. Sahne & Uygulama Akışı

```
Boot.unity ──► GameServices kurulur (DontDestroyOnLoad)
   │            - Config yüklenir, NetBridge = OfflineNetBridge (M4'te seçilebilir)
   ▼
MainMenu.unity ──► "Test Oyunu" (offline hotseat)  /  "Online" (M4: Lobi → Oda)
   ▼
Game.unity ──► MatchController
                 ├─ LevelService.Build(seed)   → ILevelGenerator + LevelPopulator
                 ├─ PawnSpawner                → 4 piyon
                 └─ MatchStateMachine          → Raund döngüsü
```

- `Game.unity` içinde sabit olan: `MatchController`, `LevelRoot` (boş), `UIRoot`, `AudioRoot`, `DebugRoot`, global ışık/post-process volume.
- Harita her raund **silinir ve yeniden üretilir** (yeni seed). Piyonlar yeniden doğar.
- Editörde doğrudan `Game.unity` açılıp Play'e basılırsa: `GameServices` yoksa **otomatik bootstrap** yapılır (`[RuntimeInitializeOnLoadMethod]`) ve **hotseat modunda** başlar. → Claude Code'un MCP ile hızlı test etmesi için kritik.

---

## 4. Core Kontratları (CM.Core) — M0'da A yazar, herkes bunlara göre kodlar

> Aşağıdaki kod "iskelet"tir; imzalar bağlayıcıdır, gövdeler A'nın uygulamasıdır.

### 4.1 Kimlikler ve Roller
```csharp
namespace CinliMahzen.Core
{
    public enum Role : byte { None = 0, Human = 1, GoodJinn = 2, EvilJinn = 3 }
    public enum Team : byte { None = 0, Seekers = 1, Jinns = 2 }

    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public readonly byte Value;          // 0..3 (offline hotseat) / Photon ActorNumber eşlemesi (M4)
        public static readonly PlayerId None = new PlayerId(255);
        // ctor, Equals, GetHashCode, ToString
    }

    public readonly struct NetId : IEquatable<NetId>
    {
        public readonly int Value;           // 0 = geçersiz
        // Aralıklar: 1..999 piyonlar/sistem, 1000..59999 level nesneleri (deterministik),
        //            60000+ runtime spawn (otorite atar: mermiler, tuz alanları)
    }

    public static class RoleUtil { public static Team TeamOf(Role r); }
}
```

### 4.2 Oyuncu & Rol Kaydı
```csharp
public interface IPlayerRegistry
{
    IReadOnlyList<PlayerInfo> Players { get; }        // 4 oyuncu
    PlayerId LocalPlayer { get; }                      // hotseat'te F1-F4 ile değişir
    Role GetRole(PlayerId id);
    PlayerInfo Get(PlayerId id);
    event Action<PlayerId> LocalPlayerChanged;         // UI & kamera & visibility dinler
    event Action RolesChanged;
}
public sealed class PlayerInfo { public PlayerId Id; public string Nickname; public Role Role; public int Score; }
```

### 4.3 Network Soyutlaması — **EN ÖNEMLİ KONTRAT**

**Altın kural:** Oyun durumunu değiştiren **her** işlem şu akışı izler:

```
[Herhangi bir istemci]  Request (NetMsg)  ──►  [OTORİTE] doğrula + durumu değiştir
                                                   │
                         Result Event (NetMsg) ◄───┘ Broadcast (herkese, otoritenin kendisi dahil)
                                   │
                  [Tüm istemciler] görseli/lokal durumu uygular
```

Offline'da `OfflineNetBridge` bu akışı **aynı frame içinde** döngüye sokar; kod aynı kalır. M4'te `PunNetBridge` aynı mesajları `PhotonNetwork.RaiseEvent` ile taşır.

```csharp
namespace CinliMahzen.Core.Net
{
    public interface INetBridge
    {
        bool IsOnline { get; }
        bool IsAuthority { get; }                 // offline: true. online: PhotonNetwork.IsMasterClient
        PlayerId LocalPlayer { get; }
        double Time { get; }                      // offline: Time.timeAsDouble. online: PhotonNetwork.Time

        void SendToAuthority(NetMsg msg);         // istek
        void Broadcast(NetMsg msg);               // SADECE otorite çağırabilir (değilse hata logla)
        void Register(MsgCode code, Action<NetMsg> handler);
        void Unregister(MsgCode code, Action<NetMsg> handler);
    }

    /// Photon RaiseEvent ile birebir uyumlu olsun diye payload sadece PUN'un serileştirebildiği tipler:
    /// byte, bool, short, int, long, float, double, string, Vector2/3, Quaternion, byte[], int[], float[], object[]
    public struct NetMsg
    {
        public MsgCode Code;
        public PlayerId Sender;                   // köprü doldurur
        public double SentTime;                   // köprü doldurur
        public object[] Payload;
    }

    public enum MsgCode : byte
    {
        // --- 1-29 Maç (A) ---
        MatchStateChanged = 1, RoundSetup = 2, RoundEnded = 3, RolesAssigned = 4, ScoreChanged = 5,
        // --- 30-59 İnsan (A) ---
        ReqInteract = 30, InteractResult = 31, HumanDamaged = 32, HumanDied = 33,
        ReqUseItem = 34, ItemUsed = 35, ReqKick = 36, KickResult = 37, StatusApplied = 38,
        ReqLanternPulse = 39, LanternPulsed = 40, InventoryChanged = 41, HumanNoise = 42,
        // --- 60-99 Possession & Cinler (B) ---
        ReqPossess = 60, PossessBegan = 61, PossessCompleted = 62, PossessEnded = 63, PossessDenied = 64,
        ReqAction = 65, ActionTelegraph = 66, ActionResolved = 67, ActionDenied = 68,
        EnergyChanged = 69, JinnStunned = 70, ProjectileSpawned = 71, ProjectileImpact = 72,
        ReqExorcise = 73, ExorciseProgress = 74, ExorciseResult = 75,
        ReqPing = 76, PingPlaced = 77, ReqBless = 78, BlessApplied = 79, RageStarted = 80,
        PossessedMove = 81, ObjectStateChanged = 82,
        // --- 100-139 Dünya & Hedefler (C) ---
        LevelBuilt = 100, ContainerSearched = 101, KeyFragmentCollected = 102,
        RunePressed = 103, RuneResult = 104, DigProgress = 105, VaultOpened = 106,
        GoldPickedUp = 107, GoldDropped = 108, PhaseChanged = 109, LightsChanged = 110, LevelHash = 111,
        // --- 200-254 Debug ---
        DebugCommand = 200,
    }
}
```

**Mesaj sahipliği:** Her MsgCode aralığının sahibi o aralığın ajanıdır. Yeni kod eklemek = kendi aralığında serbest; başkasının aralığına dokunmak = kontrat değişikliği.

**Payload yardımcıları:** Her modül kendi mesajları için tip güvenli encode/decode sınıfı yazar:
```csharp
// Örnek (B yazar): CinliMahzen.Possession.Net.PossessionMsgs
public static class PossessionMsgs
{
    public static NetMsg ReqPossess(NetId obj) => new NetMsg { Code = MsgCode.ReqPossess, Payload = new object[] { obj.Value } };
    public static NetId ReadReqPossess(in NetMsg m) => new NetId((int)m.Payload[0]);
    // ...
}
```
→ Her encode/decode çifti için **EditMode round-trip testi** zorunlu.

### 4.4 Varlık Kaydı
```csharp
public interface INetEntity { NetId NetId { get; } }
public interface IEntityRegistry
{
    void Register(INetEntity e);   void Unregister(INetEntity e);
    bool TryGet<T>(NetId id, out T entity) where T : class;
    NetId AllocateRuntimeId();     // sadece otorite, 60000+
}
```
- `NetEntity` MonoBehaviour'u: `[SerializeField] int netId` — level nesnelerine **populator deterministik atar**, piyonlara `PawnSpawner` atar.

### 4.5 Hasar, Durum Etkileri, Etkileşim
```csharp
[Flags] public enum DamageFlags : byte { None = 0, Knockdown = 1, Grab = 2, Lethal = 4, IgnoreInvuln = 8 }

public struct DamageInfo
{
    public int Amount;              // 3 = anında öldürür (HumanMaxHp)
    public PlayerId Attacker;       // kötü cin
    public NetId SourceObject;      // raf, fıçı...
    public string CauseId;          // "shelf.topple" → Otopsi metni Loc anahtarı
    public DamageFlags Flags;
    public float KnockdownTime;     // Flags.Knockdown ise
    public float GrabTime;          // Flags.Grab ise
    public Vector3 Point, Direction;
}

public interface IDamageable { NetId NetId { get; } void ApplyDamageAuthority(in DamageInfo info); }   // SADECE otorite çağırır

public enum StatusType : byte { None, Knockdown, Grabbed, Drunk, Darkness, Slowed, Stunned }
public interface IStatusReceiver { void ApplyStatusAuthority(StatusType t, float duration, PlayerId source); }

public interface IInteractable
{
    NetId NetId { get; }
    string PromptKey { get; }                      // Loc anahtarı, ör. "interact.search"
    float HoldTime { get; }                        // 0 = anında
    bool CanInteract(PlayerId who, Role role);     // lokal tahmin (UI için)
    bool ValidateAuthority(PlayerId who);          // otoritede tekrar kontrol
    void ExecuteAuthority(PlayerId who);           // otoritede sonuç → Broadcast
    float NoiseOnInteract { get; }                 // 0..1, gürültü sistemi için
}
```

**Etkileşim akışı:** `Interactor` (A) → basılı tutma tamamlandı → `ReqInteract(netId)` → otorite `IInteractable.ValidateAuthority` + menzil kontrolü (`InteractRange + 0.5` tolerans) → `ExecuteAuthority` → ilgili modül kendi result mesajını yayınlar.

### 4.6 EventBus (lokal, network'süz)
```csharp
public static class EventBus
{
    public static void Subscribe<T>(Action<T> h) where T : struct;
    public static void Unsubscribe<T>(Action<T> h) where T : struct;
    public static void Publish<T>(in T evt) where T : struct;
    public static void ClearAll();   // sahne değişiminde
}
```
- **Network mesajları** otorite kararlarını taşır. Mesaj alındığında modül **lokal EventBus event'i** yayınlar → UI, ses, VFX, istatistik bunları dinler.
- Event'ler `CM.Core/Events/` altında (sahibi kim yazdıysa, dosya başında `// Owner: B` yorumu).
- Ana event listesi:

| Event | Yayınlayan | Dinleyen |
|---|---|---|
| `RoundStartedEvt{roundIndex, seed}` | Match | herkes |
| `PhaseChangedEvt{phase}` | Objectives | Match, UI, Audio, Jinn(Öfke) |
| `HumanDamagedEvt{info, hpAfter}` | Player | UI, Audio, Stats |
| `HumanDiedEvt{info}` | Player | Match, UI(Otopsi), Stats |
| `PossessionChangedEvt{player, obj, state}` | Possession | UI, Visibility, Audio |
| `ActionTelegraphEvt{obj, actionIdx}` | Possession | Audio, VFX, Visibility |
| `ActionResolvedEvt{obj, actionIdx, hitHuman}` | Possession | Stats |
| `KeyFragmentCollectedEvt{count}` | Objectives | UI, Match |
| `GoldStateEvt{carried, carrier}` | Objectives | Player(hız), Jinn(Öfke), UI |
| `NoiseEvt{pos, loudness}` | Player/Objectives | Visibility (kötü cin halkası) |
| `LocalRoleChangedEvt{role}` | Core | Kamera, UI, Visibility, Input |
| `RoundEndedEvt{result, reason}` | Match | UI, Stats |

### 4.7 Servis Erişimi
```csharp
public static class GameServices
{
    public static INetBridge Net { get; }
    public static IPlayerRegistry Players { get; }
    public static IEntityRegistry Entities { get; }
    public static GameBalanceConfig Config { get; }
    public static IMatchInfo Match { get; }         // faz, kalan süre, raund
    public static ILevelInfo Level { get; }         // aktif level verisi (C)
    public static void Register<T>(T service);      // modüller kendi servislerini kaydeder
    public static T Get<T>();
}
```

### 4.8 Modüller Arası Köprü Arayüzleri (Core'da tanımlı, sahibi uygular)
Modüller birbirinin somut sınıfını bilmez; bu arayüzleri `GameServices.Get<T>()` ile bulur.

```csharp
// Uygulayan: B (Possession)
public interface IKickable            { NetId NetId { get; } void OnKickedAuthority(PlayerId by); }
public interface IPossessionQuery     { bool IsPossessed(NetId obj); PlayerId PossessorOf(NetId obj);
                                        void GetPossessedInCone(Vector3 origin, Vector3 dir, float range, float angleDeg, List<NetId> results); }
public interface IInteractInterceptor { bool TryInterceptAuthority(NetId target, PlayerId who); } // true → etkileşim tüketildi (mimik)

// Uygulayan: A (Player) — tuz alanları; tüketen: B (PossessionArbiter)
public interface IPossessionBlocker   { bool Blocks(Vector3 worldPos); }
public interface IPossessionBlockerRegistry { void Add(IPossessionBlocker b); void Remove(IPossessionBlocker b); bool IsBlocked(Vector3 p); }

// Uygulayan: C (World)
public interface ILightService        { int RoomOf(Vector3 worldPos); void SetRoomLightsAuthority(int roomId, bool on, float duration); }
public interface ILevelInfo           { Bounds Bounds { get; } int RoomCount { get; } int RoomOf(Vector3 p); Vector3 HumanSpawn { get; } IReadOnlyList<Vector3> JinnSpawns { get; } }

// Uygulayan: C (Objectives) — tüketen: A (HUD, Match), B (Öfke)
public interface IObjectiveInfo       { int Fragments { get; } bool VaultOpen { get; } PlayerId GoldCarrier { get; } ObjectivePhase Phase { get; } }
public enum ObjectivePhase : byte     { Explore = 0, Vault = 1, Escape = 2 }

// Uygulayan: A (Match)
public interface IMatchInfo           { MatchState State { get; } int RoundIndex { get; } double StateEndTime { get; } bool JinnsAwake { get; } }
```
M0'da A bu arayüzleri **boş (stub) uygulamalarıyla** birlikte yazar ki B ve C, diğerlerinin kodunu beklemeden derleyip test edebilsin (ör. `StubObjectiveInfo`, `StubLightService`).

### 4.9 Konfigürasyon
- `GameBalanceConfig : ScriptableObject` — `01_GDD_Oyun.md §12` tablosundaki **her satır** bir alan, aynı isim.
- `ItemDefinition : ScriptableObject` — `id, nameKey, icon, useType, prefab`.
- `PossessableDefinition` — §6.3.
- Debug overlay'de runtime değer ayarlama (M3, B3.2).

---

## 5. İnsan Sistemleri (CM.Player — Ajan A)

### 5.1 Piyon Mimarisi (tüm roller için ortak)
```
PawnBase (MonoBehaviour, NetEntity)
 ├─ PlayerId Owner, Role Role
 ├─ IPawnInput Input          ← LocalInputSource | NullInput | BotInput (Dummy İnsan)
 ├─ bool IsLocallyControlled   ← Owner == Players.LocalPlayer
 └─ PawnSync (M4)              ← owner pozisyon/rotasyon yayınlar (20 Hz), diğerleri interpolasyon
```
- **Hotseat:** 4 piyon aynı anda sahnede. Lokal olmayanlar `NullInput` alır (durur). F1-F4 → `Players.LocalPlayer` değişir → input/kamera/UI/visibility yeniden bağlanır.
- `IPawnInput` arayüzü **Core'da** tanımlı ki B'nin Dummy botu insan piyonunu sürebilsin:
```csharp
public interface IHumanInput : IPawnInput
{
    Vector2 Move { get; } Vector2 Look { get; } bool Sprint { get; }
    bool InteractHeld { get; } bool KickPressed { get; } bool LanternPressed { get; }
    bool UseItemPressed { get; } int SelectSlot { get; } bool DropGoldPressed { get; }
}
```

### 5.2 HumanController
- `CharacterController` tabanlı. Yerçekimi, zıplama yok (MVP).
- Durumlar (state machine, düz C# `HumanMotorState`): `Normal`, `Carrying`, `KnockedDown`, `Grabbed`, `Dead`.
- Hız çarpanları status'lardan toplanır (`Slowed` vb.).
- Baş sallanması, adım sesi event'i (`FootstepEvt`) → Audio.
- **Hitbox:** Ayrı bir child, `HumanHitbox` layer'ı, CapsuleCollider trigger (yükseklik 1.8, yarıçap 0.35). Saldırılar `Physics.OverlapBox/Sphere` + `LayerMask(HumanHitbox)` ile bunu arar.

### 5.3 HumanHealth (otoriter)
- `ApplyDamageAuthority`: dokunulmazlık kontrolü → Nazar kontrolü → HP düş → `HumanDamaged` broadcast → HP 0 ise `HumanDied` broadcast.
- Knockdown/Grab bayrakları → `StatusApplied`.
- Ölüm: tüm istemcilerde lokal **ragdoll** (CosmeticPhysics, darbe yönü ve noktası mesajda) + kill-cam 2 sn.
- Taşınan altın varsa: hasarda `GoldDropped` isteği Objectives'e (event üzerinden, doğrudan referans değil).

### 5.4 Etkileşim, Tekme, Fener, Eşyalar
- **Interactor:** Kamera merkezinden `Interactable` layer'ına raycast (`InteractRange`). Basılı tutma ilerlemesi lokal, tamamlanınca istek. Tutarken hareket ederse iptal (arama/kazma sırasında savunmasızlık = tasarım).
- **Kick:** `ReqKick(lookDir)` → otorite 2 m'lik kutu ile `Possessable` layer'ı arar → `IKickable.OnKickedAuthority(by)` (Core'da arayüz; Possession uygular) → `KickResult{hitObj, hadJinn}`.
- **Lantern:** `ReqLanternPulse(dir)` → otorite koni içindeki possessed nesneleri bulur (`IPossessionQuery` Core arayüzü, B uygular) → `LanternPulsed{netIds[]}` → **sadece insan istemcisinde** parıltı çizilir.
- **Items:** `Inventory` (2 slot, otoriter). `ReqUseItem(slot, pos)`:
  - Tuz → otorite `SaltZone` spawn (runtime NetId) → `IPossessionBlocker` olarak kaydolur (B'nin arbiter'ı sorar), içerideki cinleri çıkarır.
  - Nazar → pasif, `HumanHealth` kontrol eder.
- **Gürültü:** `NoiseEmitter` — koşu (0.4/sn), arama (0.6), kazma (1.0), tekme (0.8), yanlış rün (1.0, C yayınlar). `HumanNoise` mesajı ~4 Hz ile throttle.

### 5.5 Status Etkileri (A)
`StatusController` — süreli etkiler, otorite uygular, broadcast edilir, her istemci görselini gösterir.
| Status | Etki |
|---|---|
| Knockdown | Hareket yok, kamera yere düşer, süre sonunda kalkar |
| Grabbed | Hareket yok, bakış serbest |
| Drunk | Kamera sallanır (sinüs), fare Y ters, 4 sn |
| Darkness | Oda ışıkları kapalı — Objectives/World'ün `LightsChanged` mesajıyla (status değil, ortam) |
| Slowed | Hız ×0.6 |

---

## 6. Cin & Possession Sistemleri (CM.Jinn, CM.Possession, CM.Visibility — Ajan B)

### 6.1 SpiritController (hem iyi hem kötü cin)
- FPS uçuş: WASD + Space/Ctrl, Shift boost. Kinematik (`transform` hareketi), collider yok, **noclip**.
- Level sınırları: `ILevelInfo.Bounds` içinde clamp + zemin altına inemez (y ≥ 0.3), tavan üstüne çıkamaz (y ≤ 3.5; asset ölçümüne göre güncellenir).
- Görsel: yarı saydam küre + yüz + parçacık iz. Kötü: mor/kırmızı. İyi: turkuaz. (Placeholder: URP Unlit şeffaf materyal + primitive.)

### 6.2 Possession Durum Makinesi (düz C#, EditMode test edilebilir)

**Nesne tarafı (`PossessableState`):**
```
Free ──ReqPossess(ok)──► Entering(1.2s) ──► Possessed(Lurking) ◄──► Possessed(Charging) ──► Possessed(Recovering)
  ▲                          │ iptal                  │ çıkış / kovulma / tuz / tekme-sersem sonrası devam
  └──────────────────────────┴────────────────────────┘
Özel: Spent (tek kullanımlık bitti) — bir daha girilemez. Blessed(t) — süre bitene kadar girilemez.
```

**Otorite doğrulaması — `PossessionArbiter.TryPossess(player, obj)`:**
1. Oyuncu rolü `EvilJinn` ve sersem/kilitli değil
2. Faz `Playing` ve `JinnWakeDelay` geçti
3. Nesne `Free`, `Spent` değil, `Blessed` değil, hiçbir `IPossessionBlocker` (tuz) engellemiyor
4. Mesafe ≤ `PossessRange + 0.5`
5. Bu oyuncu için bu nesnenin `ReenterCooldown`'ı dolmuş
6. Oyuncu başka nesnede değil
→ Geçerse `PossessBegan{player,obj,endTime}` broadcast; süre dolunca otorite `PossessCompleted`. Aksi halde `PossessDenied{reason}` (UI gösterir).

**Yarış durumu:** İki kötü cin aynı nesneye aynı anda → otoriteye ilk ulaşan kazanır (offline'da deterministik sıra).

### 6.3 PossessableDefinition (ScriptableObject)
```csharp
[CreateAssetMenu(menuName = "CinliMahzen/Possessable Definition")]
public class PossessableDefinition : ScriptableObject
{
    public string Id;                         // "shelf", "barrel" ...
    public string NameKey;                    // Loc
    public PossessableActionDef Primary;      // sol tık
    public PossessableActionDef Secondary;    // sağ tık (opsiyonel, null olabilir)
    public bool CanMove;                      // sandalye/tabure
    public float HopDistance = 1f, HopInterval = 0.5f, HopEnergy = 2f;
    public float YawLimitDeg;                 // 0 = döndürme yok, 35 = ±35°
    public float YawSpeedDeg = 60f;
    public bool IsSearchableContainer;        // sandık/fıçı/raf aynı zamanda aranabilir mi
}

[Serializable]
public class PossessableActionDef
{
    public string Id;                         // "topple", "roll", "mimic", "launch", "extinguish", "flame", "throw", "explode", "lunge"
    public string CauseId;                    // Otopsi anahtarı "shelf.topple"
    public ActionTrigger Trigger;             // Press | HoldToCharge | ToggleArm
    public float TelegraphTime;               // saniye
    public float EnergyCost;
    public float Cooldown;
    public bool SingleUse;
    public int Damage;
    public DamageFlags Flags;
    public float KnockdownTime, GrabTime;
    public ActionShape Shape;                 // Box | Sphere | Cone | Projectile | Custom
    public Vector3 ShapeSize;                 // Box: boyut; Sphere: x=yarıçap; Cone: x=menzil y=açı
    public Vector3 ShapeOffset;               // nesne local
    public float ProjectileSpeed, ProjectileRange, ProjectileArc;
    public StatusType ApplyStatus; public float StatusDuration;
}
```
`01_GDD_Oyun.md §4` MVP tablosundaki **her satır** bir `PD_*.asset` dosyasıdır (`PD_Shelf`, `PD_Barrel`, `PD_Chair`, `PD_Stool`, `PD_Chest`, `PD_SwordShield`, `PD_Torch`, `PD_Candle`, `PD_Bottle`, `PD_Keg`).

### 6.4 Aksiyon Akışı (network-hazır)
```
Kötü cin istemcisi:  ReqAction{obj, actionIdx, yaw, chargeT}
Otorite:             doğrula (sahip mi, enerji, cooldown, spent, stun) → enerji düş
                     → Broadcast ActionTelegraph{obj, idx, yaw, resolveAt = now + TelegraphTime}
Tüm istemciler:      telgraf animasyonu + ses + (Visibility: Charging parlaması)
Otorite (resolveAt): Shape'e göre hit-check (HumanHitbox) → IDamageable.ApplyDamageAuthority
                     → Broadcast ActionResolved{obj, idx, hit, newObjState}
Tüm istemciler:      sonuç animasyonu (raf yerde, fıçı yuvarlanıyor...), kozmetik fizik
```
- **Projectile (kılıç, şişe):** Otorite `ProjectileSpawned{runtimeId, origin, dir, speed, arc, t0}` yayınlar. Tüm istemciler aynı parametrelerle **görsel** simülasyon yapar. Otorite her FixedUpdate `SphereCast` ile ilerletir; çarpışınca `ProjectileImpact{runtimeId, point, hitHuman}`.
- **Hareketli aksiyonlar (fıçı yuvarlanma, sandalye atılma):** Otorite kinematik yol hesaplar (yön × hız, duvara `SphereCast` ile durur); başlangıç + bitiş noktası + süre yayınlanır; istemciler aynı eğriyi oynatır. Yol boyunca insan hit-check'i otoritede her tick.
- **Sandalye zıplama (WASD):** Sahip cin istemcisi `PossessedMove{obj, targetPos}` isteği (hop başına) → otorite doğrular (mesafe ≤ HopDistance, zemin var, duvar yok, enerji) → broadcast → herkes zıplama animasyonu (0.25 sn parabol).
- **Mimik sandık:** `ToggleArm` → otorite "armed" durumunu tutar (yayınlanmaz! insan bilmemeli — sadece kötü cinlere ve 4 m'deki iyi cine görsel). İnsan `ReqInteract` ile sandığı açınca Objectives'in container mantığından **önce** `IInteractInterceptor` (Core arayüzü) sorulur → mimik tetiklenir.
  > Online'da "yayınlanmaz" bilgisi: M4'te otorite armed bilgisini sadece Jinns takımına + menzildeki iyi cine hedefli gönderir (`RaiseEventOptions.TargetActors`). Offline'da hepsi aynı makinede olduğundan sadece görsel filtre uygulanır.
- **Söndür:** `ILightService` (Core arayüzü, C uygular) → `SetRoomLights(roomId, off, duration)`.

### 6.5 Enerji, Sersemlik, Öfke
- `JinnEnergy` (otoriter, düz C# çekirdek + MonoBehaviour sarmalayıcı): regen × (Öfke ? RageRegenMult : 1). `EnergyChanged` yayını değişim ≥ 1 olduğunda veya 4 Hz.
- Cooldown'lar `CooldownTracker` (düz C#) — Öfke çarpanı uygulanır.
- `JinnStunned{player, until, lockUntil}`: sersem cin hareket edebilir ama eşyaya giremez/aksiyon yapamaz.
- Öfke: `GoldStateEvt.carried == true` ilk kez → `RageStarted` (otorite).

### 6.6 Görünürlük (CM.Visibility)
Her istemci **lokal rolüne göre** her frame (veya 10 Hz) hesaplar:

| Hedef | İnsan | İyi Cin | Kötü Cin |
|---|---|---|---|
| Kötü cin (ruh formu) | ❌ (3 m içinde soğuk nefes FX) | ≤ 20 m: görsel + XRay outline | Takım arkadaşı: her zaman + outline |
| Kötü cin (eşyada, Lurking) | ❌ (fener parlatınca parıltı) | ≤ 4 m: nesne mor outline | Her zaman |
| Kötü cin (eşyada, Charging) | Telgraf (herkes) | ≤ 25 m: kırmızı outline | Her zaman |
| İyi cin | ❌ | Kendi | Soluk görsel (%30 alfa) |
| Ruh ipuçları (SpiritOnly) | ❌ | ✅ | ❌ |
| İşaret (ping) | ✅ | ✅ | ❌ |
| Isı izi, gürültü halkası, nişan oku | ❌ | ❌ | ✅ |

- **XRay outline:** URP Renderer Feature "Render Objects" — `XRayOutline` layer'ı, depth test `Always`, tek renk şeffaf materyal. Görünür yapılacak nesnenin outline child'ının layer'ı dinamik açılır/kapanır.
- `VisibilityService` sadece **lokal** çalışır, network mesajı üretmez.

### 6.7 İyi Cin Yetenekleri
- **Kov:** `ReqExorcise(obj)` başlat → otorite süreyi sayar, `ExorciseProgress` (içerideki cine uyarı), iyi cin menzilden çıkar/bakışı kaçırırsa iptal (istemci `ReqExorcise` cancel gönderir). Tamamlanınca: `PossessEnded{reason=Exorcised}` + `JinnStunned` + `BlessApplied(obj, BlessAfterExorcise)`.
- **İşaret:** `ReqPing(pos, objId?)` → `PingPlaced{id, pos, until}` → insan ve iyi cin istemcisinde görsel.
- **Kutsa:** `ReqBless(obj)` → `BlessApplied{obj, until}`; `PossessionArbiter` kontrol eder.

### 6.8 Kötü Cin Algısı
- **Isı izi:** Lokal. İnsan piyonunun (sync edilen) pozisyonundan her 0.3 sn bir iz noktası, `HeatTrailDuration` sonra söner. `JinnOnlyFX` layer.
- **Gürültü halkası:** `HumanNoise` mesajı → kötü cin istemcisinde mesafe ≤ `NoisePingRange` ise o noktada genişleyen halka.

### 6.9 Kameralar
- `CameraRig` (A yazar, Core'a yakın, `CM.Player`): tek ana kamera, moda göre davranış:
  - `FpsMode(targetHead)` — İnsan ve ruh formu
  - `OrbitMode(target, distance, pitchClamp)` — Kötü cin eşyada (B `PossessionCameraBinder` ile modu değiştirir)
  - `DeathCamMode(ragdoll)` — ölüm
- Geçiş: 0.3 sn pozisyon/rotasyon lerp. Orbit çarpışma: `SphereCast` Environment'a karşı.
- Culling mask lokal role göre (§2).

---

## 7. Dünya & Hedefler (CM.World, CM.Objectives — Ajan C)

### 7.1 LevelService Akışı
```
Match (otorite): seed = GameRandom.NewSeed() → RoundSetup{seed, roles} broadcast
Tüm istemciler:
  1. LevelRoot temizlenir
  2. ILevelGenerator.Generate(seed, settings, LevelRoot) → LevelLayout
  3. LevelValidator.Validate(layout)  (eksik marker → hata + fallback)
  4. NavMeshSurface.BuildNavMesh()   (Dummy bot için)
  5. LevelPopulator.Populate(seed, layout) → eşyalar, kaplar, bulmacalar, altın, NetId atama
  6. LevelHash hesapla → online'da otoriteye LevelHash gönder (M4: uyuşmazlık tespiti)
  7. EventBus: LevelBuiltEvt
```
**Tüm istemciler aynı seed'den aynı haritayı üretir** → yüzlerce prop için network nesnesi gerekmez. Her prop'un NetId'si populator sırasıyla deterministik atanır.

### 7.2 ProceduralLevelGenerator (C yazar — kalıcı)
Tek başına harita üreticisi; C'nin diğer görevleriyle birlikte sürekli geliştirilir. Mantık düz C# (grid, oda grafı, rol atama) → Unity yerleştirmesi ince katman (EditMode testlenebilir).

**v1 (C0.4 — M0, A/B'yi bloklamamak için hızlı):**
- Grid tabanlı: hücre = KayKit duvar/zemin modül boyutu (**M0'da ölçülüp `04_Asset_Eslestirme.md`'ye yazılacak**, beklenti 4 m).
- 7×7 grid'e 8-12 dikdörtgen oda (2×2 – 3×4 hücre) yerleştir, MST + %20 ekstra kenar ile koridorlar (döngü garantisi).
- Duvar yerleştirme: kenar bazlı; kapı açıklığı `wall_doorway`, hazine girişi `wall_gated`.
- Marker'ları (§8) kurallara göre yerleştirir.
- **Deterministik:** Sadece `System.Random(seed)`. `Dictionary`/`HashSet` iterasyon sırasına güvenme — listeler sıralı.
- Editör menüsü: `CinliMahzen/Level/Generate Map (Random Seed)` ve `(Seed=12345)`.

**v2 (C3.4 — kalite & çeşitlilik):**
- Dikdörtgen olmayan odalar (L/T) ve/veya elle hazırlanmış oda şablonları (room prefab + soket noktaları) + prosedürel yerleşim. Yaklaşımı C v2 başında seçer.
- Oda rolleri graf/yol mesafesiyle: Start, Vault, Exit (vault→çıkış ≥ 35 m), bulmaca odaları (rün ↔ ipucu ≥ 2 oda) — `01_GDD_Oyun.md §11`.
- Oynanış sezgileri: vault çevresinde döngü, çıkmaz sokak sınırı, koridor/oda oranı, eşya yoğunluğu hedefleri.
- Tüm ayarlar `LevelGenSettings` (C'nin SO'su) içinde.
- **Kalite aracı:** `CinliMahzen/Level/Batch Report (100 seeds)` → seed başına oda sayısı, en uzun yol, döngü, çıkmaz, validator sonucu.

### 7.3 LevelPopulator (C yazar — kalıcı; her `ILevelGenerator` uygulamasıyla çalışır)
Generator sadece **geometri + marker (soket)** üretir. Oynanış nesnelerini populator yerleştirir:
1. Marker'ları deterministik sırala (oda indeksi → marker tipi → local pozisyon x,z).
2. `GameRandom(seed ^ 0x5EED)` ile:
   - Hazine odası → `VaultDoor` + `GoldChest`
   - Çıkış → `ExitZone`
   - Bulmaca soketleri → `RunePuzzle` (4 taş) + ayrı odada `RuneHintWall`; `FootprintPuzzle` (başlangıç + `DigSpot`)
   - `PossessableSocket`'ler → kategoriye uygun possessable prefabları (§8.3)
   - Aranabilir kaplardan biri → Mühür parçası; diğerlerine `LootTable`
3. NetId: `1000 + sıra`.
4. Oda başına ışıklar `LightService`'e kaydedilir.

### 7.4 Hedef Nesneleri
| Bileşen | Tip | Davranış |
|---|---|---|
| `SearchableContainer` | IInteractable | Arama (1 sn) → otorite loot verir → `ContainerSearched{obj, lootId}`. Bir kez aranır. Possessable olabilir (sandık/fıçı/raf): `IInteractInterceptor` önce sorulur (mimik) |
| `KeyFragmentPickup` | — | Container loot'u olarak verilir, doğrudan envantere değil `ObjectiveState.fragments++` |
| `RuneStone` ×4 | IInteractable | Anında bas. Otorite sırayı kontrol eder. Yanlış → sıfırla + `HumanNoise(1.0)` |
| `RuneHintWall` | SpiritOnly görsel | 4 sembolün sırası (rastgele permütasyon, seed'li) |
| `FootprintTrail` | SpiritOnly görsel | Başlangıçtan `DigSpot`'a nokta dizisi (NavMesh path veya düz çizgi + oda kapıları) |
| `DigSpot` | IInteractable | 3 sn basılı, gürültü 1.0 → fragment |
| `VaultDoor` | — | fragments == 3 → `VaultOpened` → kapı animasyonu (gate aşağı iner) |
| `GoldChest` | IInteractable | 1 sn → `GoldPickedUp{carrier}`. Taşıyıcı hasar alırsa `GoldDropped{pos}` (otorite, insanın önüne) |
| `ExitZone` | Trigger | Taşıyan insan girerse (otorite kontrol) → Match'e `SeekersWin` |
| `ObjectiveState` | Otoriter servis | fragments, vaultOpen, goldCarrier, phase → `PhaseChanged` |

### 7.5 Işık & Atmosfer
- URP, karanlık ambient (neredeyse siyah, hafif mavi), `torch_mounted`/`candle` başına Point Light (gölgesiz, performans), insan feneri Spot Light (gölgeli, tek gölgeli ışık).
- `LightService : ILightService` — oda ID → ışık listesi; `SetRoomLights(roomId, on/off, duration)` → `LightsChanged` broadcast.
- Hafif volumetric his: URP fog (Exponential Squared, koyu).
- Performans hedefi: 1080p, orta sistem, **≥ 90 FPS** (tek gölgeli ışık kuralı bunun için).

### 7.6 Ses (CM.Audio)
- `AudioService` — `Play(SfxId, pos)`, `PlayUI(SfxId)`; `SfxLibrary` ScriptableObject (id → clip listesi, rastgele pitch).
- **Telgraf sesleri 3D ve yüksek önceliklidir** (oyunun adaleti). Mixer grupları: Master / SFX / Telegraph / UI / Music.
- Placeholder: Clip yoksa `CMLog.Warn` + sessiz devam (crash yok).

---

## 8. Level Kontratı (Generator ↔ Oyun Sınırı) ⭐

> `ProceduralLevelGenerator` (ve ileride denenebilecek başka her generator) **sadece bu arayüzü uygular**. Oynanış yerleşimini `LevelPopulator` yapar. A ve B yalnızca bu kontrata bağımlıdır.

### 8.1 Arayüz
```csharp
namespace CinliMahzen.World
{
    public interface ILevelGenerator
    {
        /// Deterministik olmak ZORUNDA: aynı seed + settings → birebir aynı hiyerarşi ve pozisyonlar.
        /// UnityEngine.Random KULLANMA. Sadece verilen seed ile System.Random.
        /// Tüm nesneleri 'root' altına oluştur. Senkron çalışmalı (tek frame) veya IEnumerator versiyonu kullan.
        LevelLayout Generate(int seed, LevelGenSettings settings, Transform root);
    }

    [Serializable] public class LevelGenSettings { public int MinRooms = 8, MaxRooms = 14; public float CellSize = 4f; /* genişletilebilir */ }

    public class LevelLayout
    {
        public Bounds Bounds;                          // cinlerin uçuş sınırı
        public List<RoomInfo> Rooms;                   // index = RoomId
        public List<LevelMarker> Markers;              // root altındaki tüm marker'lar
        public int[,] RoomAdjacency;                   // opsiyonel: oda komşulukları (kapı sayısı)
    }

    public class RoomInfo { public int RoomId; public Bounds Bounds; public RoomTag Tags; public List<int> Neighbors; }

    [Flags] public enum RoomTag { None = 0, Start = 1, Vault = 2, Exit = 4, Corridor = 8, PuzzleCandidate = 16, Large = 32 }
}
```

### 8.2 Marker Bileşenleri (generator bunları prefablara/boş objelere ekler)
| Marker | Adet | Zorunlu alanlar | Kural |
|---|---|---|---|
| `HumanSpawnMarker` | 1 | roomId | Start odasında |
| `JinnSpawnMarker` | 3 | roomId | İnsana en az 25 m |
| `ExitMarker` | 1 | roomId | Vault'tan ≥ 35 m yol |
| `VaultMarker` | 1 | roomId, doorTransform | Vault odası tek girişli; `doorTransform` = mühürlü kapı yeri |
| `GoldSpawnMarker` | 1 | roomId | Vault odası içinde |
| `PossessableSocket` | 30-50 | roomId, `SocketCategory` (Flags), `wallFacing` (bool) | Populator doldurur (§8.3). Doldurulmayan soket boş kalır |
| `ContainerSocket` | 10-16 | roomId | Aranabilir kap yeri (populator sandık/fıçı/raf seçer) |
| `PuzzleSocket` | ≥ 3 | roomId, `PuzzleType` (RuneStones, RuneHint, FootprintStart, DigSpot) | RuneStones ve RuneHint farklı odalarda, aralarında ≥ 2 oda |
| `LightSocket` | oda başına ≥ 1 | roomId, `wallMounted` | Meşale/mum yeri |
| `DecorSocket` | serbest | — | Opsiyonel süs (populator dokunmaz) |

```csharp
[Flags] public enum SocketCategory
{
    None = 0, WallLarge = 1 /*raf*/, Floor = 2 /*fıçı, sandalye*/, Table = 4 /*şişe, mum*/,
    WallMount = 8 /*kılıç-kalkan, meşale*/, Corner = 16 /*bira fıçısı*/, FloorTile = 32 /*diken - post MVP*/
}
```

### 8.3 Populator Eşleme
| SocketCategory | Olası possessable'lar (ağırlık) |
|---|---|
| WallLarge | Raf (1.0) |
| Floor | Fıçı (0.5), Sandalye (0.3), Tabure (0.2) |
| Table | Şişe (0.6), Mum (0.4) |
| WallMount | Kılıç-Kalkan (0.5), Meşale (0.5) |
| Corner | Bira fıçısı (0.4), Fıçı (0.6) |

### 8.4 Geometri Gereksinimleri
- Zemin collider'ları `Environment` layer'ında, y = 0 düzleminde (tek kat).
- Duvar collider'ları kapalı (insan dışarı çıkamaz).
- Tüm yürünebilir alan NavMesh bake'e uygun (`NavMeshSurface` root'a eklenecek, `Environment` layer'ını kullanır).
- Tavan: opsiyonel (`ceiling_tile`). Varsa `Environment` değil `Default` layer (insan fenerini engellemesin, cinler üstüne çıkamaz zaten).

### 8.5 Doğrulama (`LevelValidator`)
Zorunlu marker eksikse / kurallar tutmuyorsa → `CMLog.Error` + **aynı seed+1 ile yeniden dene** (max 5), sonra `LevelGenSettings.FallbackSeed` (bilinen iyi seed) ile üret. Editör menüsü: `CinliMahzen/Level/Validate Current Level` → rapor.

### 8.6 Generator Değişikliği Kuralları (C0.4, C3.4 ve sonrası)
1. Her generator değişikliğinden sonra determinizm testi: aynı seed ile 2 kez üret → `LevelHash` eşit (EditMode).
2. Çoklu seed testi: v1'de 50, v2'de 100 seed → hepsi `LevelValidator`'dan geçer.
3. Kontrat (§8.1–8.4) değişirse önce `CONTRACT_CHANGES.md` (A ve B etkilenir).
4. Üretim + populate < 1.5 sn.

---

## 9. Maç Yönetimi (CM.Match — Ajan A)

### 9.1 MatchStateMachine (düz C#, otoriter)
```
Lobby → RoundSetup → RoleReveal(3s) → Intro(5s) → Playing → RoundEnd(8s) ─┬─► RoundSetup (raund < 4)
                                                                            └─► MatchEnd
```
- `Playing` içinde alt faz `ObjectiveState.phase`: Explore → Vault → Escape (C yönetir, A dinler).
- Bitiş koşulları (otorite): `HumanDiedEvt` → JinnsWin(Kill); `ExitReached` → SeekersWin; süre 0 → JinnsWin(Timeout).
- `MatchStateChanged{state, endTime}` broadcast; herkes `endTime - Net.Time` ile sayaç gösterir.

### 9.2 Rol Rotasyonu
- Raund `r` (0..3): İnsan = `players[r]`, İyi Cin = `players[(r+1)%4]`, Kötü = diğer iki.
- `RolesAssigned{byte[4] roles}`.

### 9.3 Puan & İstatistik
- `ScoreService` (otoriter) — §6 tablosu.
- `StatsService` (her istemci lokal toplar, otorite nihai değerleri RoundEnd'de yayınlar): boşa tekme, düşme, ıskalama, kovma, tuz, hasar kaynakları.
- Unvan hesaplama `TitleCalculator` (düz C#, test edilebilir) — C'nin Otopsi/MatchEnd UI'ı kullanır.

---

## 10. UI (CM.UI)

- **uGUI + TextMeshPro** (Unity 6 dahili). UI Toolkit kullanılmaz (tutarlılık).
- `UIRoot` prefabı: Canvas (Screen Space Overlay, 1920×1080 referans, Scale With Screen Size 0.5).
- Her HUD bir `RoleHud` alt sınıfı; `LocalRoleChangedEvt` ile doğru olan aktif olur.
- **Loc:** `Loc.T("key")` — `LocTable` ScriptableObject (key, tr, en). Eksik anahtar → `#key#` gösterir + uyarı.
- Font: Türkçe karakterli TMP font asset (Noto Sans veya benzeri, `ğüşıöçİĞÜŞÖÇ` atlas'a dahil).

| Ekran | Sahip |
|---|---|
| UI framework, Loc, MainMenu, Pause/Ayarlar, RoleReveal, Human HUD | A |
| Evil HUD, Good HUD, possession ipuçları, possess/deny mesajları | B |
| Otopsi, MatchEnd, hedef bildirimleri (Mühür 2/3, ALTIN ALINDI) | C |
| Lobi (M4) | A |

---

## 11. Prefab Sahipliği & Bileşim

Birden fazla ajanın aynı prefabı düzenlemesini önlemek için:
- **Human prefabı (A):** Tüm bileşenler A'nın.
- **Spirit prefabları (B):** `P_EvilJinn`, `P_GoodJinn` — B'nin. `PawnBase` (A'nın script'i) içerir; A'nın script'lerini kullanır ama prefab dosyası B'nin.
- **Possessable prefabları (B):** Model referansları C'nin import ettiği modellerden. `P_Poss_Base` → varyantlar.
- **Container (sandık/fıçı/raf aranabilir):** Possessable prefabına C'nin `SearchableContainer` bileşeni **eklenmez**; bunun yerine populator runtime'da `AddComponent` yapar. → prefab çakışması yok.
- **Environment & Objectives prefabları (C).**
- `Game.unity` sadece A; B ve C ihtiyaçlarını A'ya bildirir (ya da kendi sandbox sahnelerinde test eder).

---

## 12. Debug & Test Altyapısı (Claude Code için hayati)

### 12.1 Hotseat Modu (offline varsayılan)
- 4 `PlayerInfo`: "P1".."P4", roller rotasyona göre.
- **F1-F4:** lokal oyuncuyu değiştir. **F5:** İnsan ölümsüz. **F6:** Sınırsız enerji. **F7:** Tüm cinleri/ipuçlarını göster. **F8:** Yeni seed ile raundu yeniden başlat. **F9:** Debug overlay. **F10:** Zaman ×2. **F11:** Tüm mühürleri ver. **F12:** Dummy İnsan botunu aç/kapa.
- Debug overlay: FPS, lokal rol, faz, kalan süre, enerji, possession durumları listesi, son 10 NetMsg.

### 12.2 Editör Menü Komutları (MCP `execute_menu_item` ile tetiklenebilir)
```
CinliMahzen/Debug/Switch To Player 1..4
CinliMahzen/Debug/Restart Round (New Seed)
CinliMahzen/Debug/Kill Human
CinliMahzen/Debug/Give All Fragments
CinliMahzen/Debug/Toggle Dummy Human
CinliMahzen/Debug/Possess Nearest (Evil P3)
CinliMahzen/Debug/Trigger Action Primary (Evil P3)
CinliMahzen/Debug/Dump State To Console
CinliMahzen/Level/Generate Map (Random Seed)
CinliMahzen/Level/Batch Report (100 seeds)
CinliMahzen/Level/Validate Current Level
```
- `Dump State To Console`: Maç durumu, oyuncular, roller, HP, enerji, possession'lar, hedef durumu → tek JSON log. **Claude Code play mode doğrulamasını buradan okur.**

### 12.3 Dummy İnsan Botu (B yazar, `BotInput : IHumanInput`)
- NavMesh üzerinde rastgele oda gez, kapları ara, 20% ihtimalle koş. Kötü cin sistemlerini tek başına test etmek için.

### 12.4 Testler
- **EditMode (zorunlu):** `PossessionArbiter` kuralları, `JinnEnergy`, `CooldownTracker`, `HumanHealth` (nazar, dokunulmazlık), `MatchStateMachine` geçişleri, rol rotasyonu, `ObjectiveState`, rün sırası, `TitleCalculator`, tüm NetMsg encode/decode round-trip, `ProceduralLevelGenerator` determinizm (aynı seed → aynı hash), `LevelValidator`.
- **PlayMode (önemli akışlar):** Raund başlar → insan doğar; cin rafa girer → devirir → insan ölür → RoundEnd; F11 + altın + çıkış → SeekersWin.
- Her görevin kabul kriterinde hangi testin geçmesi gerektiği yazılıdır.

---

## 13. Online Entegrasyon (M4 — EN SON) — Photon PUN 2

> M0-M3 boyunca bu bölüm **uygulanmaz**. Ama M0-M3'te yazılan her kod §4.3 kuralına uyduğu için M4 = "köprüyü değiştir + piyon senkronu ekle".

### 13.1 Kurulum
- PUN 2 (Asset Store, ücretsiz) import → `Assets/Photon/` (repo'ya dahil).
- `PhotonServerSettings`: AppId (PUN) — **AppId repo'ya commit edilmez**: `PhotonServerSettings.asset` `.gitignore`'a; her geliştirici kendi yerel kopyasına aynı AppId'yi girer (README'de yazılı). Region: `eu` (Türkiye için), Fixed Region.
- `PhotonNetwork.SendRate = 30`, `SerializationRate = 20`.

### 13.2 Bileşenler (CM.Net.Pun)
| Sınıf | Görev |
|---|---|
| `PunConnection` | Bağlan, nickname, oda oluştur/katıl (6 haneli kod = oda adı), MaxPlayers=4, `IsVisible=false`, late join kapalı (oyun başlayınca `IsOpen=false`) |
| `PunNetBridge : INetBridge` | `MsgCode` → Photon event code (1:1, byte). `SendToAuthority` = `RaiseEvent(ReceiverGroup.MasterClient)`; `Broadcast` = `RaiseEvent(ReceiverGroup.All)`; hedefli gönderim için `SendTo(actors[])` ek metodu (mimik gizliliği). Reliable varsayılan; `EnergyChanged`, `HumanNoise`, `PossessedMove` unreliable |
| `PunPlayerRegistry : IPlayerRegistry` | ActorNumber ↔ PlayerId (katılım sırasına göre 0-3, oda özelliği olarak saklanır) |
| `PunPawnSync : IPunObservable` | Owner pozisyon/rotasyon/bakış yayını, diğerleri 100 ms gecikmeli interpolasyon |
| `PunPawnSpawner` | Raund başında her istemci kendi piyonunu `PhotonNetwork.Instantiate("Pawns/Human" / "Pawns/EvilJinn"...)` |
| `PunLobbyUI` | Oda kodu, oyuncu listesi, hazır, başlat (sadece master) |

- **Oda Özellikleri:** `seed`, `round`, `roles` (byte[]), `playerOrder` (int[] ActorNumber).
- **Level nesneleri PhotonView KULLANMAZ.** Hepsi NetId + `RaiseEvent` ile (§7.1 determinizm).
- **Master ayrılırsa:** Raund iptal → herkes lobiye döner (host migration yok, MVP).
- **Oyuncu ayrılırsa:** Raund iptal, lobiye dönüş, "X ayrıldı" mesajı.

### 13.3 Online Test Yöntemi
- **Unity 6 Multiplayer Play Mode** (paket: `com.unity.multiplayer.playmode`) ile tek makinede 4 sanal oyuncu, **veya** ParrelSync klonları.
- Checklist M4 görevlerinde.

---

## 14. Performans & Kalite Hedefleri
- ≥ 90 FPS @1080p orta sistem, ≤ 1.5 GB RAM.
- GC allocation Update'lerde 0 (Profiler ile kontrol M3'te).
- Maç başına network trafiği: < 10 KB/s/oyuncu.
- Harita üretimi + populate: < 1.5 sn.

---

## 15. Build & Dağıtım
- Hedef: Windows x64 (Mono veya IL2CPP — M3'te karar; varsayılan Mono, hızlı build).
- Build: `CinliMahzen/Build/Windows` menü komutu → `Builds/CinliMahzen_<tarih>/`.
- Steam: kapsam dışı (ileride).
