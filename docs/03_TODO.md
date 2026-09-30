# CİNLİ MAHZEN — Görev Listesi (TODO)

> **Nasıl kullanılır (Claude Code için):**
> 1. Sadece **kendi ajan harfindeki** (A/B/C) görevleri al.
> 2. Görevi başlatmadan önce **Bağımlılık** sütunundaki görevlerin `[x]` olduğunu kontrol et. Değilse → Stub'la ilerle (bkz. `02_GDD_Teknik.md §4.8`) veya kullanıcıya bildir.
> 3. Görevi alınca kutuyu `[~]` yap (devam ediyor), bitince `[x]` yap ve commit mesajına görev ID'sini yaz: `B1.2: possession flow + orbit camera`.
> 4. **Kabul kriterlerinin hepsi** sağlanmadan `[x]` yapma. Kriterlerde "MCP" yazıyorsa Unity MCP ile play mode'da doğrula; `CinliMahzen/Debug/Dump State To Console` çıktısını kanıt olarak kullan.
> 5. Bu dosyada sadece **kendi satırlarının durum kutusunu** değiştir (merge çakışması olmasın).
>
> Boyut: **S** ≈ tek oturum, **M** ≈ 2-3 oturum, **L** ≈ 4+ oturum (bölünebilir).
>
> **Ajanlar:**
> - **A — Çekirdek & İnsan & Maç** (+ M4'te PUN altyapısı)
> - **B — Cinler & Possession & Görünürlük**
> - **C — Dünya & Hedefler & Bulmacalar & Ses & Otopsi**

---

## Kilometre Taşları

| MS | Ad | Çıkış kriteri |
|---|---|---|
| **M0** | Kurulum | Proje 3 makinede derleniyor, Core kontratları + stub'lar var, KayKit import edildi, ProceduralLevelGenerator v1 harita üretiyor |
| **M1** | Çekirdek Döngü (offline) | Hotseat'te: insan dolaşır, kötü cin rafa girip devirir ve insanı öldürür; insan altını bulup çıkabilir; raund biter ve yeni seed ile başlar |
| **M2** | Tam Oynanış (offline) | Tüm MVP eşyaları, iyi cin yetenekleri, görünürlük, 3 mühür bulmacası, hazine/kaçış, 4 raund rotasyon + puan |
| **M3** | Cila | Menüler, otopsi, maç sonu, ses, VFX, denge araçları, performans |
| **M4** | Online (PUN 2) | 4 oyuncu, 4 farklı istemci, tam maç sorunsuz oynanıyor |
| **M5** | Playtest & Denge | 3 oturum gerçek playtest, denge değişiklikleri |

---

## M0 — Kurulum

> ⚠️ **Sıra önemli:** Önce **A0.1–A0.4** biter ve push edilir. B ve C projeyi **ondan sonra** klonlar. Sonra A0.5+, B0.x, C0.x paralel.

| ✔ | ID | Sahip | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|---|
| [x] | A0.1 | A | Unity projesi + git kurulumu | — | S |
| [x] | A0.2 | A | Paketler & proje ayarları | A0.1 | S |
| [x] | A0.3 | A | Klasör yapısı + asmdef'ler | A0.2 | S |
| [x] | A0.4 | A | Tags/Layers/Fizik matrisi/Render feature | A0.3 | S |
| [x] | A0.5 | A | Core kontratları + stub'lar | A0.3 | M |
| [x] | A0.6 | A | OfflineNetBridge, EventBus, EntityRegistry, GameServices + testler | A0.5 | M |
| [x] | A0.7 | A | GameBalanceConfig + Loc + CMLog + GameRandom | A0.5 | S |
| [x] | A0.8 | A | Input Actions asset | A0.2 | S |
| [ ] | A0.9 | A | Boot/Game sahneleri, auto-bootstrap, debug overlay iskeleti | A0.6 | M |
| [~] | B0.1 | B | Possession veri modeli (SO) + düz C# durum makinesi + testler | A0.5 | M |
| [~] | B0.2 | B | Sandbox_B sahnesi + spirit placeholder görseller | A0.4 | S |
| [ ] | C0.1 | C | KayKit import + materyal + ölçü raporu | A0.3 | S |
| [ ] | C0.2 | C | Environment & prop prefabları | C0.1 | M |
| [ ] | C0.3 | C | Level kontratı kodu (ILevelGenerator, marker'lar, LevelLayout, Validator) | A0.5 | M |
| [ ] | C0.4 | C | ProceduralLevelGenerator v1 (kalıcı level generator) | C0.2, C0.3 | L |

### A0.1 — Unity projesi + git
- Unity Hub → **6000.3.18f1** → "Universal 3D" şablonu → proje adı `CinliMahzen`.
- Editor Settings: Asset Serialization = **Force Text**, Version Control = **Visible Meta Files**.
- `.gitignore` (Unity standart + `Builds/`, `UserSettings/`, `*.csproj`, `*.sln`, `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset*`).
- `.gitattributes`: `*.unity *.prefab *.asset *.mat *.anim *.controller merge=unityyamlmerge eol=lf`, `*.fbx *.png *.wav *.ogg binary` (LFS gerekmez; toplam asset < 50 MB).
- Repo kökü = Unity proje kökü. `CLAUDE.md` ve `docs/` bu dokümanlardan kopyalanır.
- **Kabul:** Repo GitHub'da, `main` dalı, proje açılıyor, konsolda hata yok.

### A0.2 — Paketler & ayarlar
- Paketler: `com.unity.inputsystem`, `com.unity.ai.navigation`, `com.unity.test-framework`, `com.unity.multiplayer.playmode` (M4 için şimdiden).
- Player Settings: Active Input Handling = Input System Package (New). Company `CinliMahzen`, Product `Cinli Mahzen`.
- Quality: tek seviye "Default" + "Low". URP Asset: Shadow distance 25, tek ek ışık gölgesi, max additional lights 16.
- **Kabul:** Paketler manifest'te, derleme temiz.

### A0.3 — Klasörler + asmdef
- `02_GDD_Teknik.md §1` klasör ağacını ve `§1.1` asmdef'leri oluştur (boş klasörler için `.gitkeep`).
- Her asmdef'te `rootNamespace` = `CinliMahzen.<Modül>`.
- **Kabul:** Her asmdef'te birer boş placeholder script derleniyor; bağımlılık grafiği §1.1 ile birebir.

### A0.4 — Tags/Layers/Fizik/Render
- `§2` tablosu birebir. Physics Layer Collision Matrix §2'deki gibi.
- URP Renderer'a "Render Objects" feature: `XRayOutline` layer, Depth Test = Always, override materyal `M_XRay` (unlit, şeffaf, renk property'li).
- **Kabul:** Ayarlar commit'lendi; `docs/CONTRACT_CHANGES.md` dosyası oluşturuldu (boş şablon).

### A0.5 — Core kontratları
- `§4.1 – §4.8` arasındaki **tüm** tip ve arayüzler `CM.Core` içinde.
- Stub'lar: `StubObjectiveInfo`, `StubLightService`, `StubLevelInfo`, `StubPossessionQuery`, `StubMatchInfo` — gerçek uygulama gelene kadar `GameServices` bunları döndürür.
- `CM.Core/Events/` altında §4.6 tablosundaki event struct'ları.
- **Kabul:** Derleniyor. B ve C'ye "kontratlar hazır" diye commit mesajı: `A0.5: core contracts ready`.

### A0.6 — Net/EventBus/Registry
- `OfflineNetBridge`: `SendToAuthority` → handler'ı **aynı frame, senkron** çağır; `Broadcast` → tüm kayıtlı handler'lar. `Sender` = `Players.LocalPlayer`. Hotseat'te lokal oyuncu değişse de otorite hep bu makine.
- `EventBus` (struct event'ler, allocation'sız dispatch), `EntityRegistry`, `GameServices`.
- **Testler (EditMode):** mesaj round-trip, otorite olmayan Broadcast hata logu, EventBus subscribe/unsubscribe, registry runtime ID tahsisi.
- **Kabul:** Tüm testler yeşil.

### A0.7 — Config & yardımcılar
- `GameBalanceConfig` SO — `01_GDD_Oyun.md §12` her satır; `Config/GameBalanceConfig.asset` değerlerle dolu.
- `ItemDefinition` SO + `ID_Salt`, `ID_Nazar` asset'leri.
- `Loc` + `LocTable.asset` (tr/en sütunları; ilk anahtarlar: rol adları, etkileşim ipuçları).
- `CMLog`, `GameRandom` (System.Random sarmalayıcı: `Range(int,int)`, `Range(float,float)`, `Pick<T>(IList<T>)`, `Shuffle`, `WeightedPick`).
- **Kabul:** GameRandom determinizm testi (aynı seed → aynı 1000 sayı).

### A0.8 — Input Actions
- `CMInput.inputactions` + generated C# class. Map'ler: `Human`, `Spirit`, `Possessed`, `UI`, `Debug` — `01_GDD_Oyun.md §7` tuşları.
- `LocalInputSource` — aktif role göre map açıp kapatır, `IHumanInput`/`ISpiritInput`/`IPossessedInput` sunar (arayüzler Core'da).
- **Kabul:** Hotseat'te map geçişi log'la doğrulandı.

### A0.9 — Sahneler & bootstrap & debug
- `Boot`, `MainMenu` (placeholder butonlar), `Game`, `Sandbox_A`. B ve C kendi sandbox'larını kendileri açar.
- Auto-bootstrap (`§3`), `DebugOverlay` (F9), `DebugHotkeys` (F1-F12 iskelet — işlevsiz olanlar "TODO" log'lar), `CinliMahzen/Debug/Dump State To Console` menüsü.
- **Kabul (MCP):** `Game.unity` Play → konsolda "Bootstrap OK (offline hotseat)", F9 overlay açılıyor.

### B0.1 — Possession veri modeli
- `PossessableDefinition`, `PossessableActionDef` (`§6.3`), enum'lar.
- Düz C#: `PossessableStateMachine`, `PossessionArbiterCore` (tüm §6.2 kuralları, bağımlılıklar arayüzle enjekte), `JinnEnergyCore`, `CooldownTracker`.
- 10 adet `PD_*.asset` — `01_GDD_Oyun.md §4` değerleriyle.
- **Testler:** Arbiter'ın 6 kuralının her biri için pozitif + negatif test; yarış durumu; enerji regen + Öfke; cooldown × Öfke.
- **Kabul:** Tüm testler yeşil.

### B0.2 — Sandbox_B + spirit görselleri
- `Sandbox_B.unity`: düz zemin, birkaç KayKit prop'u (C0.2 bitmeden ham fbx sürükle-bırak olabilir).
- Placeholder cin görselleri: küre + göz (iki küçük siyah küre) + parçacık iz. `EvilJinnVisual` / `GoodJinnVisual` layer'ları.
- **Kabul:** Screenshot (MCP) ile görseller doğrulandı.

### C0.1 — KayKit import
- Kaynak: repo kökündeki `KayKit_Dungeon_Pack_1.1_FREE/` klasörü (ham paket, Unity import etmez) → içindeki **`Assets/fbx(unity)/`** klasörü → `Assets/_Project/Art/KayKit/Models/`. Texture: `Assets/textures/dungeon_texture.png` → `Art/KayKit/Textures/`.
- Tek materyal `M_KayKit_Dungeon` (URP Lit, base map = dungeon_texture, smoothness 0.1). Model import ayarı: Materials → "Use External Materials (Legacy)" veya Remap ile hepsini bu materyale bağla.
- **Ölçü raporu:** `wall`, `floor_tile_large`, `floor_tile_small`, `wall_doorway`, `wall_gated`, `stairs`, `shelf_large`, `barrel_large`, `chest`, `chair` bounds'larını ölç → `docs/04_Asset_Eslestirme.md` "Ölçüler" tablosunu doldur. Grid hücre boyutunu (beklenti 4 m) kesinleştir.
- **Kabul:** Tüm modeller pembe değil (materyal bağlı), ölçü tablosu dolu, commit.

### C0.2 — Prefablar
- `Prefabs/Environment/`: duvar çeşitleri, zemin, kapı açıklığı, gated, sütun, merdiven — collider'lı (`Environment` layer), pivot hücre köşesine/ortasına **tutarlı** (raporda belirt).
- `Prefabs/Props/`: `04_Asset_Eslestirme.md`'deki props — collider'lı, henüz possessable bileşeni YOK (B ekleyecek varyant olarak).
- **Kabul:** Sandbox_C'de 3×3 odalık elle kurulmuş örnek, boşluk/z-fighting yok (screenshot).

### C0.3 — Level kontratı
- `§8.1–§8.5` tüm tipler, marker MonoBehaviour'ları (Gizmo çizimli: renkli küp + etiket), `LevelValidator`, `LevelHash` (tüm marker + Environment objelerinin pozisyon/tip hash'i, sıralı).
- **Testler:** Validator eksik marker senaryoları.
- **Kabul:** Testler yeşil.

### C0.4 — ProceduralLevelGenerator v1
- `02_GDD_Teknik.md §7.2` (v1). Kalıcı kod: mantık düz C#, sonradan v2'ye genişletilecek. Marker kurallarını sağlar. Editör menüleri.
- **Testler:** Aynı seed 2 kez → aynı `LevelHash`; 50 farklı seed → hepsi Validator'dan geçer.
- **Kabul (MCP):** Menüden üret → screenshot (üstten ortografik) → odalar, kapılar, marker gizmoları görünüyor.

---

## M1 — Çekirdek Döngü (offline)

| ✔ | ID | Sahip | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|---|
| [ ] | A1.1 | A | PawnBase + Human FPS controller + stamina | A0.8, A0.9 | M |
| [ ] | A1.2 | A | CameraRig (FPS/Orbit/DeathCam) + culling mask | A1.1 | M |
| [ ] | A1.3 | A | Interactor (raycast, basılı tutma, ipucu UI event) | A1.1 | S |
| [ ] | A1.4 | A | HumanHealth (otoriter) + ölüm + ragdoll placeholder | A1.1 | M |
| [ ] | A1.5 | A | PlayerRegistry + Hotseat (F1-F4) + PawnSpawner | A1.1, C0.3 | M |
| [ ] | A1.6 | A | MatchStateMachine (minimal) + sayaç + bitiş koşulları | A1.5 | M |
| [ ] | A1.7 | A | Human HUD (minimal: can, stamina, ipucu, sayaç) | A1.3, A1.4 | S |
| [ ] | B1.1 | B | SpiritController (FPS uçuş, noclip, sınır) | A1.1, A1.2 | S |
| [ ] | B1.2 | B | Possession akışı (MonoBehaviour tarafı) + mesajlar + Orbit kamera bağlama | B0.1, B1.1 | L |
| [ ] | B1.3 | B | Possessable prefab tabanı + Shelf/Barrel/Chair/Stool varyantları | B1.2, C0.2 | M |
| [ ] | B1.4 | B | Aksiyonlar: Topple, Roll, Hop, Lunge | B1.3, A1.4 | L |
| [ ] | B1.5 | B | Evil HUD (minimal: enerji, aksiyon kartları, deny mesajı) | B1.2 | S |
| [ ] | C1.1 | C | LevelService (raund başı üret/temizle) + LevelInfo | C0.4, A0.9 | S |
| [ ] | C1.2 | C | LevelPopulator (possessable + kap + altın + çıkış, NetId) | C1.1, B1.3 | M |
| [ ] | C1.3 | C | SearchableContainer + LootTable | C1.2, A1.3 | M |
| [ ] | C1.4 | C | M1 geçici hedef: altın sandığı + çıkış (mühürsüz) | C1.3, A1.6 | S |
| [ ] | C1.5 | C | LightService + oda ışıkları + temel atmosfer | C1.1 | S |

### A1.1 — Human controller
- `§5.1–§5.2`. State machine düz C# (`HumanMotorState`) + test (Carrying'de koşamaz, KnockedDown'da hareket yok).
- **Kabul (MCP):** Sandbox_A'da WASD/koşu/stamina çalışıyor; Dump State'te stamina değişiyor.

### A1.2 — CameraRig
- `§6.9`. B'nin kullanacağı public API: `CameraRig.SetFps(Transform head)`, `SetOrbit(Transform target, float dist)`, `SetDeathCam(Transform ragdollRoot)`. Culling mask'i `LocalRoleChangedEvt` ile ayarla.
- **Kabul:** Mod geçişleri 0.3 sn yumuşak; orbit duvar içine girmiyor.

### A1.3 — Interactor
- `§4.5` akışı. `InteractPromptEvt{key, progress}` yayınlar (UI dinler).
- **Kabul:** Test küp `IInteractable` ile 1 sn basılı tutma → otoritede `ExecuteAuthority` log.

### A1.4 — HumanHealth
- `§5.3`. `DamageInfo` bayrakları, dokunulmazlık, nazar (A2.1'de envanter gelince bağlanacak — şimdilik bool).
- Ragdoll: placeholder capsule insan için basit "devrilme" (Rigidbody ekle + darbe kuvveti).
- **Testler:** Dokunulmazlık, lethal, nazar.
- **Kabul (MCP):** `CinliMahzen/Debug/Kill Human` → ragdoll + `HumanDiedEvt` log.

### A1.5 — Hotseat
- 4 PlayerInfo, rol rotasyonu (raund 0: P1 insan, P2 iyi, P3-P4 kötü), `PawnSpawner` level marker'larından doğurur (C stub'ı varsa stub'dan).
- F1-F4 → LocalPlayer değişimi → input, kamera, HUD, culling.
- **Kabul (MCP):** F1..F4 ile 4 piyon arasında geçiş; her birinde doğru HUD/kamera.

### A1.6 — Match minimal
- `§9.1` (RoundSetup → Intro → Playing → RoundEnd → RoundSetup döngüsü; MatchEnd M2'de).
- **Testler:** Geçişler, 3 bitiş koşulu.
- **Kabul (MCP):** Kill Human → RoundEnd → 8 sn → yeni seed ile yeni harita.

### A1.7 — Human HUD minimal
- 3 kalp, stamina barı, ortada etkileşim ipucu + dairesel ilerleme, sayaç.

### B1.1 — SpiritController
- `§6.1`. İyi/kötü aynı controller, farklı config hız.
- **Kabul (MCP):** F3 → kötü cin, duvardan geçerek uçuyor, sınır dışına çıkamıyor.

### B1.2 — Possession akışı
- `PossessionSystem` (MonoBehaviour, NetBridge handler'ları), `Possessable` bileşeni (`INetEntity`, `IKickable`), `PossessionCameraBinder`.
- `IPossessionQuery` gerçek uygulamasını `GameServices`'e kaydet (stub'ı değiştirir).
- Girme telgrafı: 1.2 sn titreme (pozisyon gürültüsü ±2 cm, rotasyon ±2°) + gıcırtı SFX hook'u (`ActionTelegraphEvt`).
- Çıkış (Space/E), tekme sersemliği (`OnKickedAuthority`), JinnWakeDelay kontrolü (`IMatchInfo.JinnsAwake`).
- **Kabul (MCP):** F3 → rafa bak → E → 1.2 sn titreme → TPS kamera → Space → FPS'e dönüş. Dump State'te possession görünüyor. Aynı rafa 10 sn içinde tekrar girmeye çalışınca `PossessDenied(Cooldown)`.

### B1.3 — Possessable prefablar
- `P_Poss_Base` (Possessable + NetEntity + collider `Possessable` layer + outline child `XRayOutline` layer kapalı) → varyantlar: `P_Poss_Shelf`, `P_Poss_Barrel`, `P_Poss_Chair`, `P_Poss_Stool`.
- **Kabul:** 4 prefab Sandbox_B'de, her biri doğru `PD_*` referanslı.

### B1.4 — İlk aksiyonlar
- `PossessableActionRunner` (§6.4 akışı) + aksiyon davranışları `IActionBehaviour`: `ToppleAction`, `RollAction`, `HopMove`, `LungeAction`.
- Telgraf görselleri (eğilme animasyonu kod ile — `AnimationCurve`), sonuç animasyonu, kozmetik devrilme.
- Hit-check otoritede `Physics.OverlapBox` + `HumanHitbox` mask → `IDamageable`.
- **Kabul (MCP):** Hotseat'te P1 insanı rafın önüne koy → F3 → rafa gir → sol tık → 1 sn sonra raf devrilir → insan ölür → RoundEnd. Fıçı yuvarla → 1 hasar + knockdown. Sandalye WASD zıplıyor, çarp → 1 hasar.

### B1.5 — Evil HUD minimal
- Enerji barı, eşyadayken aksiyon kartları (tuş, isim, maliyet, cooldown dolumu), deny sebebi toast'u.

### C1.1 — LevelService
- `§7.1` adım 1-5, 7. `ILevelInfo` gerçek uygulaması.
- **Kabul:** RoundSetup'ta harita yeniden üretiliyor, eski harita tamamen siliniyor (Hierarchy'de kalıntı yok).

### C1.2 — Populator
- `§7.3` (M1 kapsamı: possessable'lar, kaplar, altın, çıkış; bulmacalar M2). `§8.3` ağırlık tablosu.
- **Testler:** Aynı seed → aynı NetId → aynı prefab eşlemesi.

### C1.3 — Container + Loot
- `SearchableContainer` (`IInteractable`, runtime `AddComponent`), `LootTable` SO, `IInteractInterceptor` önce sorulur.
- Aranan kap görsel olarak açık/boş hale gelir (sandık kapağı açılır; fıçı/raf için "arandı" toz efekti).
- **Kabul (MCP):** İnsan kabı arar → `ContainerSearched` log, loot (şimdilik log).

### C1.4 — Geçici hedef
- Altın sandığı rastgele bir odada (vault kapısı yok), `GoldChest` taşıma + `ExitZone` → `SeekersWin`.
- **Kabul (MCP):** Hotseat: insan altını alır → yavaşlar → çıkışa gider → RoundEnd(SeekersWin).

### C1.5 — Işık
- `§7.5` temel. `ILightService` gerçek uygulaması.
- **Kabul:** Screenshot: karanlık, meşaleler turuncu ışık, fener spot ışığı gölgeli.

### ✅ M1 Çıkış Testi (her ajan kendi makinesinde yapar)
1. `Game.unity` Play → harita üretilir, 4 piyon doğar.
2. F3 → kötü cin → bir rafa gir → F1 → insanı rafın önüne yürüt → F3 → devir → insan ölür.
3. RoundEnd → yeni harita.
4. F1 → altını bul → çıkışa taşı → SeekersWin.
5. Konsolda Error yok.

---

## M2 — Tam Oynanış (offline)

| ✔ | ID | Sahip | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|---|
| [ ] | A2.1 | A | Envanter (2 slot) + Tuz + Nazar + pickup | A1.4, C1.3 | M |
| [ ] | A2.2 | A | Tekme + Fener Parlat | A1.3, B1.2 | M |
| [ ] | A2.3 | A | StatusController (Knockdown, Grabbed, Drunk, Slowed) + görselleri | A1.4 | M |
| [ ] | A2.4 | A | NoiseEmitter | A1.1 | S |
| [ ] | A2.5 | A | 4 raund rotasyonu + MatchEnd + ScoreService + StatsService | A1.6 | M |
| [ ] | A2.6 | A | Human HUD tam | A2.1, A2.2 | S |
| [ ] | B2.1 | B | Aksiyonlar: Mimic, SwordLaunch, Extinguish, Flame, BottleThrow, KegExplode + projectile sistemi | B1.4, C1.5, A2.3 | L |
| [ ] | B2.2 | B | VisibilityService + XRay outline + soğuk nefes FX | B1.2, A1.2 | M |
| [ ] | B2.3 | B | İyi cin: Kov, İşaret, Kutsa | B1.2, B2.2 | M |
| [ ] | B2.4 | B | Kötü cin algısı: ısı izi, gürültü halkası, takım outline | B2.2, A2.4 | S |
| [ ] | B2.5 | B | Öfke modu | B1.4, C2.3 | S |
| [ ] | B2.6 | B | Evil HUD + Good HUD tam | B2.3 | M |
| [ ] | B2.7 | B | Dummy İnsan botu (BotInput) | A1.1, C1.1 | M |
| [ ] | C2.1 | C | ObjectiveState + faz geçişleri + mühür parçası (kap) | C1.3 | M |
| [ ] | C2.2 | C | Rün bulmacası (taşlar + ipucu duvarı) | C2.1 | M |
| [ ] | C2.3 | C | Hayalet izleri + DigSpot | C2.1 | M |
| [ ] | C2.4 | C | VaultDoor + hazine odası + altın akışı (M1 geçicisini değiştir) | C2.1 | M |
| [ ] | C2.5 | C | Hedef bildirim UI (Mühür 2/3, ALTIN ALINDI, faz değişimi) | C2.1 | S |
| [ ] | C2.6 | C | Timeout "Bekçi geldi" + raund sonu sebep metinleri | A2.5 | S |

### A2.1 — Envanter & eşyalar
- `§5.4`. Pickup: C'nin loot'u `ItemDefinition` id verir → `InventoryChanged`. Dolu envanter → loot yerde kalır (pickup prefabı `Prefabs/Items/`).
- Tuz: `SaltZone` (runtime NetId, görsel beyaz halka + parçacık), `IPossessionBlockerRegistry`'ye kayıt, içindeki possessed nesnelerin cinlerini çıkartma (Possession'a event ile: `SaltPlacedEvt`).
- **Testler:** Envanter slot mantığı, nazar tüketimi.
- **Kabul (MCP):** Tuz at → alan içindeki rafa P3 giremiyor (`PossessDenied(Blocked)`).

### A2.2 — Tekme & Fener
- `§5.4`. Fener modeli (`torch_lit` elde, FPS view model), Spot Light, Parlat efekti (sadece insan istemcisinde parıltı shader/emission pulse).
- **Kabul (MCP):** P3 rafta beklerken P1 tekme → `JinnStunned` 2.5 sn, aksiyon denied. Parlat → rafta parıltı görünüyor.

### A2.3 — Status
- `§5.5`. Drunk: kamera roll sinüs + fare Y ters. Grabbed/Knockdown kamera efektleri.

### A2.4 — Gürültü
- `§5.4` gürültü değerleri, `NoiseEvt` + `HumanNoise` msg (throttle 4 Hz).

### A2.5 — Maç tam
- 4 raund, `§9.2` rotasyon, `§9.3` puan, `StatsService` istatistik anahtarları: `kicks_wasted, falls, hits_taken, shots_missed, exorcisms, salt_used, damage_by_object[*]`.
- `TitleCalculator` + testleri (`01_GDD_Oyun.md §6` unvanları).
- **Kabul:** F8 ile 4 raund hızlıca geç → MatchEnd state, puanlar Dump State'te doğru.

### A2.6 — Human HUD tam
- `01_GDD_Oyun.md §10` madde 4.

### B2.1 — Kalan aksiyonlar
- `01_GDD_Oyun.md §4` satır 4-8. Projectile sistemi (`§6.4`). Mimik: `IInteractInterceptor` uygulaması, armed bilgisinin gizliliği (offline: sadece görsel filtre).
- Söndür → `ILightService.SetRoomLightsAuthority`. Şişe → `StatusType.Drunk`. Bira fıçısı → iki halkalı hasar + yakındaki `Spent` olmayan kırılabilirleri kır (kozmetik).
- Prefablar: `P_Poss_Chest`, `P_Poss_Trunk`, `P_Poss_SwordShield`, `P_Poss_Torch`, `P_Poss_Candle`, `P_Poss_Bottle`, `P_Poss_Keg`.
- **Kabul (MCP):** Her aksiyon için Sandbox_B'de dummy insana karşı test; Dump State'te hasar/status doğru.

### B2.2 — Görünürlük
- `§6.6` tablosu birebir. Soğuk nefes: insan kamerasının önünde hafif buğu partikülü + vinyet.
- **Kabul (MCP):** F2 (iyi cin) → 20 m içindeki kötü cin duvar arkasından outline. Rafta bekleyen kötü cin 5 m'de görünmüyor, 3 m'de görünüyor. Şarj ederken 20 m'de kırmızı.

### B2.3 — İyi cin yetenekleri
- `§6.7`. Kov ilerleme çubuğu + içerideki cine uyarı.
- **Kabul (MCP):** F2 → rafta P3 → E basılı 1.5 sn → P3 fırlar, 4 sn sersem, raf 15 sn kutsanmış.

### B2.4 — Algı
- `§6.8`. **Kabul:** F3'te insanın izi ve koşu gürültü halkaları görünüyor, F1/F2'de görünmüyor.

### B2.5 — Öfke
- `GoldStateEvt.carried` → `RageStarted`. HUD'da kırmızı vinyet + "ÖFKE!" yazısı.

### B2.6 — HUD'lar
- `01_GDD_Oyun.md §10` madde 5-6.

### B2.7 — Dummy İnsan
- `§12.3`, F12 aç/kapa. **Kabul:** F12 → insan kendi kendine dolaşıyor, kapları arıyor.

### C2.1 — ObjectiveState
- `IObjectiveInfo` gerçek uygulaması, `PhaseChanged`, kaptan mühür parçası (`§7.4`), iyi cin için doğru kapta **ruh parıltısı** (SpiritOnly, 6 m).

### C2.2 — Rün bulmacası
- 4 `RuneStone` (duvara yapışık, sembollü — sembol texture'ları basit: 4 farklı geometrik şekil, `TextMeshPro` ile de olabilir), `RuneHintWall` (SpiritOnly layer, doğru sıra). Yanlış basış → sıfırlama + `HumanNoise(1.0)`.
- **Testler:** Sıra kontrol mantığı.
- **Kabul (MCP):** F2'de ipucu duvarı görünür, F1'de görünmez; doğru sıra → fragment.

### C2.3 — Hayalet izleri
- `FootprintTrail` (NavMesh path noktaları, 0.6 m aralık, sol/sağ ayak decal/quad, SpiritOnly), `DigSpot` (3 sn, gürültü, `floor_tile_small_broken_A` görseli).

### C2.4 — Vault
- `VaultDoor` (`wall_gated`, 3 fragment → açılma animasyonu), `GoldChest` (`chest_gold`), taşıma kuralları (`01_GDD_Oyun.md §5.3`), hasar alınca düşürme.

### C2.5 — Bildirimler
- Ekran üstü banner: "Mühür Parçası 2/3", "Hazine Odası Açıldı!", "ALTIN ALINDI — CİNLER ÖFKELİ!". Rol bazlı farklı metin (kötü cinlere: "İnsan altını aldı! DURDURUN!").

### C2.6 — Timeout
- Sayaç 0 → `JinnsWin(Timeout)` → "Bekçi geldi" metni. Son 60 sn sayaç kırmızı + tik-tak sesi hook'u.

### ✅ M2 Çıkış Testi
Hotseat'te tek kişi tam bir raundu oynayabilmeli: 3 mühür (kap, rün, kazı) → vault → altın → çıkış. Başka raundda kötü cinler 8 eşya tipinin hepsini kullanarak öldürme. İyi cin kovma/işaret/kutsa çalışıyor. 4 raund → MatchEnd.

---

## M3 — Cila

| ✔ | ID | Sahip | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|---|
| [ ] | A3.1 | A | MainMenu, Pause, Ayarlar (hassasiyet, FOV, ses, kalite) — PlayerPrefs | A0.9 | M |
| [ ] | A3.2 | A | RoleReveal ekranı + Intro geri sayım | A2.5 | S |
| [ ] | A3.3 | A | İnsan karakter modeli entegrasyonu (asset gelince) + FPS el/fener modeli | A1.1 | M |
| [ ] | A3.4 | A | Performans turu (Profiler, GC 0, ≥90 FPS) | M2 | M |
| [ ] | A3.5 | A | Windows build menüsü | — | S |
| [ ] | B3.1 | B | Juice: possession/aksiyon VFX, ekran sarsıntısı, squash&stretch | M2 | M |
| [ ] | B3.2 | B | Runtime denge paneli (debug overlay'de config/PD değerleri kaydırıcı) | M2 | M |
| [ ] | B3.3 | B | Cin görselleri v2 (yüz ifadeleri: bekleme/şarj/sersem) | B2.2 | S |
| [ ] | C3.1 | C | Otopsi Raporu ekranı | A2.5 | M |
| [ ] | C3.2 | C | MatchEnd ekranı + unvanlar | A2.5 | S |
| [ ] | C3.3 | C | AudioService + SfxLibrary + tüm hook'lara placeholder ses | M2 | M |
| [ ] | C3.4 | C | **ProceduralLevelGenerator v2** — kalite & çeşitlilik (şekilli odalar/şablonlar, rol atama, sezgiler, batch rapor) | C0.4, C2.4 | L |
| [ ] | C3.5 | C | Post-MVP eşyalar: Masa Kay, Diken, Tabak Gürültü (B ile koordineli; prefab B, soket C) | M2 | M |

### C3.4 — ProceduralLevelGenerator v2
- `02_GDD_Teknik.md §7.2` (v2) + `§8.6` kuralları, `01_GDD_Oyun.md §11` tasarım gereksinimleri.
- **Kabul:** `Batch Report (100 seeds)` → 100/100 Validator'dan geçiyor, determinizm testi yeşil, 3 farklı seed'de tam raund oynanıyor, üretim+populate < 1.5 sn, üstten screenshot'larda belirgin çeşitlilik.

*(Diğer M3 görevlerinin kabul kriteri: ilgili GDD bölümü + MCP screenshot + konsolda Error yok.)*

---

## M4 — Online (Photon PUN 2) — EN SON

> M4'e başlamadan önce M2 çıkış testi **3 makinede** geçmiş olmalı.

| ✔ | ID | Sahip | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|---|
| [ ] | A4.1 | A | PUN 2 import + ServerSettings (AppId yerel) + PunConnection | M2 | S |
| [ ] | A4.2 | A | Lobi UI (oda kodu, 4 slot, hazır, başlat, Discord hatırlatması) | A4.1 | M |
| [ ] | A4.3 | A | PunNetBridge + PunPlayerRegistry + oda özellikleri | A4.1 | L |
| [ ] | A4.4 | A | PunPawnSync + PunPawnSpawner (tüm roller) | A4.3 | M |
| [ ] | A4.5 | A | Bağlantı kopması / master ayrılması → lobiye dönüş | A4.3 | S |
| [ ] | B4.1 | B | Possession & aksiyonların online doğrulaması, `PossessedMove` unreliable, hedefli mimik bilgisi | A4.3 | M |
| [ ] | B4.2 | B | Görünürlük & iyi cin yetenekleri online | A4.4 | S |
| [ ] | C4.1 | C | Seed senkronu + `LevelHash` karşılaştırma (uyuşmazlık → hata ekranı) | A4.3 | S |
| [ ] | C4.2 | C | Hedefler & ışıklar & otopsi online | A4.3 | M |
| [ ] | ALL4.6 | Hepsi | Online test checklist (aşağıda) | hepsi | M |

### ALL4.6 — Online Test Checklist
- [ ] Multiplayer Play Mode ile 4 sanal oyuncu: oda kur/katıl, başlat.
- [ ] Her istemcide aynı harita (LevelHash eşit).
- [ ] Possession yarışı: 2 kötü cin aynı rafa aynı anda → biri kazanır, diğeri denied.
- [ ] Mimik armed bilgisi insan istemcisinde **yok** (Dump State ile doğrula).
- [ ] 150 ms yapay gecikme (Photon `NetworkSimulationSet`) ile tam raund oynanabilir.
- [ ] Master ayrılınca herkes lobiye düşüyor, crash yok.
- [ ] 3 gerçek makine + 1 sanal ile tam 4 raundluk maç.

---

## M5 — Playtest & Denge

| ✔ | ID | Sahip | Görev |
|---|---|---|---|
| [ ] | P5.1 | Hepsi | 3 playtest oturumu; her oturum sonrası `docs/PLAYTEST_LOG.md`'ye: kazanma oranları (Arayıcı/Cin), ortalama raund süresi, en çok öldüren eşya, "sinir bozucu" anlar |
| [ ] | P5.2 | B | Denge değişiklikleri (config) — hedef: Arayıcı kazanma oranı %45-55 |
| [ ] | P5.3 | Hepsi | Kritik bug fix turu |
