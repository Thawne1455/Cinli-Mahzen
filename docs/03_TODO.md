# CİNLİ MAHZEN — Görev Listesi (TODO)

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
| **M0.5** | Pivot (v1 → v2) | Kod v2 kontratlarına uyarlandı, KayKit import edildi, derleme + testler yeşil |
| **M1** | Hareket & Possession | Hotseat'te insan yürür, cinler uçar, kötü cin eşyaya girip fırlatır, insan hasar alıp bayılır; graybox ev |
| **M2** | Bulmaca Çekirdeği | 1 bulmaca (Resimler) uçtan uca: referans, karıştırma, onay, ödül; RoundSetupPlanner; aşama geçidi |
| **M3** | Tam Oynanış (offline) | 4 bulmaca, bahçe → 2 aşama → mahzen; ışık mekaniği; iyi cin yetenekleri; 4 raund + puan; HUD'lar |
| **M4** | Cila | KayKit ile ev, menüler, raund raporu, ses, VFX, denge |
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

## M0.5 — Pivot (v1 → v2)

| ✔ | ID | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|
| [x] | P.1 | Dokümanları v2'ye yaz (GDD, Teknik, TODO, CLAUDE.md, README), ajan dosyalarını kaldır | — | S |
| [ ] | P.2 | KayKit import + prefablar (C dalından al) | — | S |
| [ ] | P.3 | Core kontratları v2 (config, event, bridge, MsgCode, stub) | P.1 | M |
| [ ] | P.4 | Possession v2 (ışık kuralı, iyi cin, PD_* listesi) + Input v2 | P.3 | M |
| [ ] | P.5 | Eski dalları temizle + Sandbox sahnesi | P.2 | S |

### P.2 — KayKit import
- `origin/agent-c/m0-assets-scene` dalından **sadece** şunları `main`'e al (`git checkout <dal> -- <yol>`): `Assets/_Project/Art/KayKit/**`, `Art/Materials/M_KayKit_Dungeon.mat`, `Prefabs/Environment/Env_*`, `Prefabs/Props/Prop_*` (+ .meta'lar), `docs/04_Asset_Eslestirme.md` ölçü tablosu.
- **Almayacakların:** `Scripts/World/**` (generator, planner, level kontratı), `Tests/EditMode/C/**`, `Editor/C/**`, `LevelPrefabSet`. Prefablar bu script'lere referans veriyorsa referansı kaldır.
- **Kabul (MCP):** Derleme temiz, konsolda missing script yok, birkaç prefab sahneye konup screenshot'ta pembe değil.

### P.3 — Core kontratları v2
- `02_GDD_Teknik.md §14` tablosundaki Core satırları: `GameBalanceConfig` (+ asset değerleri GDD §14), event'ler, bridge arayüzleri, `MsgCode`, `RoundEndReason`, `ObjectivePhase`, stub'lar, `ItemDefinition` + görev eşyası asset'leri (`ID_HouseKey`, `ID_Lantern`, `ID_FuseKey`, `ID_Paint`, `ID_Shovel`, `ID_CellarKey`).
- `StatusType` / `DamageFlags` int değerleri **değişmez** (yeni değerler sona eklenir).
- **Kabul:** Derleme temiz, tüm EditMode testleri yeşil (silinen tiplere ait testler kaldırıldı/uyarlandı).

### P.4 — Possession & Input v2
- `PossessionArbiterCore`: tuz kuralı → ışık kuralı (`IRoomService.LightOf`, lamba istisnası), iyi cin (`AllowGoodJinn`), `PossessDenyReason.Lit`.
- `PossessableDefinition` alan ekleri (§6.3), `PD_*` v2 listesi (eskileri sil).
- Input: `IHumanInput` 3 slot + Drop, `ISpiritInput` (Exorcise, LampLight, PossessAsGood), `CMInput` map'leri GDD §11.
- **Testler:** ışık kuralı (karanlık ✔, aydınlık ✘, lamba aydınlıkta ✔), iyi cin izinli/izinsiz eşya.
- **Kabul:** Testler yeşil.

### P.5 — Temizlik
- Kullanıcıya sor, onay verirse: `agent-c/*` ve `claude/gracious-euler-dewg8h` uzak dallarını sil.
- `Sandbox.unity` (serbest test sahnesi, düz zemin + birkaç prop). `Sandbox_A` → `Sandbox` olarak yeniden adlandırılabilir.

---

## M1 — Hareket & Possession

| ✔ | ID | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|
| [ ] | 1.1 | PawnBase + Human FPS controller + stamina + merdiven/rampa | P.3 | M |
| [ ] | 1.2 | CameraRig (FPS / Orbit / Faint) + culling mask | 1.1 | M |
| [ ] | 1.3 | PlayerRegistry + Hotseat (F1-F4) + PawnSpawner | 1.1 | M |
| [ ] | 1.4 | Interactor (raycast, basılı tutma, ipucu event'i) | 1.1 | S |
| [ ] | 1.5 | HumanHealth + bayılma + kaybetme (otoriter) | 1.1 | M |
| [ ] | 1.6 | SpiritController (uçuş, noclip, sınır) | 1.2 | S |
| [ ] | 1.7 | Possession akışı (MonoBehaviour) + mesajlar + orbit kamera | P.4, 1.6 | L |
| [ ] | 1.8 | Aksiyonlar: Throw, Hop, Lunge, Slide, Topple | 1.7, 1.5 | L |
| [ ] | 1.9 | Graybox ev v0 (zemin kat + 1. kat + bahçe, primitive/KayKit, marker'sız) | P.2 | M |
| [ ] | 1.10 | MatchStateMachine minimal (sayaç, Faint/Timeout bitişi, raund döngüsü) | 1.3, 1.5 | M |

### 1.1 — Human controller
- `02_GDD_Teknik.md §5.1–5.2`. `HumanMotorState` düz C# + test (Fainted/Interacting'de hareket yok).
- **Kabul (MCP):** Sandbox'ta yürü/koş/stamina, rampa çıkılıyor; Dump State'te stamina değişiyor.

### 1.2 — CameraRig
- API: `SetFps(head)`, `SetOrbit(target, dist)`, `SetFaint()`. Culling `LocalRoleChangedEvt` ile (§2).
- **Kabul:** Geçişler 0.3 sn; orbit duvara girmiyor.

### 1.3 — Hotseat
- 4 PlayerInfo, rotasyon (raund 0: P1 insan, P2 iyi, P3-P4 kötü). F1-F4 → input/kamera/HUD/culling.
- **Kabul (MCP):** `Switch To Player 1..4` ile 4 piyon arası geçiş.

### 1.4 — Interactor
- §5.4. `InteractPromptEvt{key, progress}`.
- **Kabul:** Test küp 1 sn basılı → otoritede `ExecuteAuthority` log.

### 1.5 — HumanHealth & bayılma
- §5.3. Bayılma, eşya düşürme (envanter yoksa log), `FaintsToLose`.
- **Testler:** dokunulmazlık, bayılma sayacı, bayılmışken hasar yok, 3. bayılma → kaybetme event'i.
- **Kabul (MCP):** `Faint Human` ×3 → `HumanFaintedEvt` ×3 + JinnsWin(Faint) log.

### 1.6 — SpiritController
- §6.1. İyi/kötü aynı controller, farklı hız.
- **Kabul (MCP):** F3 → duvardan geçerek uçuyor, sınır dışına çıkamıyor.

### 1.7 — Possession akışı
- `PossessionSystem`, `Possessable` (`INetEntity`, `IKickable`), `PossessionCameraBinder`, gerçek `IPossessionQuery`. Girme telgrafı (titreme + ses hook), çıkış, tekme sersemliği.
- **Kabul (MCP):** F3 → sandalyeye gir → TPS → çık → FPS. Aydınlık odada (stub `IRoomService` ile) girme reddi. Dump State'te possession görünüyor.

### 1.8 — Aksiyonlar
- `PossessableActionRunner` + `IActionBehaviour`: `ThrowAction` (projectile), `HopMove`, `LungeAction`, `SlideAction` (hücre kaydırma), `ToppleAction`. Hit-check otoritede.
- **Kabul (MCP):** Şişe fırlat → insana 1 hasar; raf devril → hasar + knockdown; fıçı kaydır → kapı önünü tıkıyor; 3 isabet → bayılma.

### 1.9 — Graybox ev v0
- Claude Code: KayKit `Env_*` prefablarıyla (4 m grid) **editör script'i** ile zemin kat + 1. kat + bahçe iskeleti üretir (`CinliMahzen/House/Build Graybox`) → `P_House` prefabı. Oda listesi GDD §9.
- 🖐 elle: kullanıcı oda oranlarını, kapıları, merdiveni elle düzeltir.
- **Kabul (MCP):** Üstten + içeriden screenshot; insan iki kat arasında yürüyebiliyor.

### 1.10 — Match minimal
- §9. RoundSetup → Intro → Playing → RoundEnd → RoundSetup.
- **Testler:** geçişler, 3 bitiş koşulu (Treasure stub event ile).
- **Kabul (MCP):** Faint ×3 → RoundEnd → 8 sn → yeni raund.

### ✅ M1 Çıkış Testi
1. `Game.unity` Play → evde 4 piyon doğar.
2. F3 → kötü cin → şişeye gir → F1'deki insana fırlat ×3 → bayılma.
3. Faint ×3 → raund biter, yeni raund başlar. Konsolda Error yok.

---

## M2 — Bulmaca Çekirdeği

| ✔ | ID | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|
| [ ] | 2.1 | `PuzzleState` + `PuzzleOp` + `PuzzleDefinition` SO + testler | P.3 | S |
| [ ] | 2.2 | Ev marker'ları (§8) + `HouseService.Index` + `HouseValidator` + `IRoomService` | 1.9 | M |
| [ ] | 2.3 | `RoundSetupPlanner` + `RoundPlanValidator` + testler | 2.1, 2.2 | L |
| [ ] | 2.4 | `PuzzleSystem` + mesajlar (Op / Confirm / Completed) + round-trip testleri | 2.1, 1.4 | M |
| [ ] | 2.5 | Bulmaca P1 **Resimler**: `PuzzleView`, `PuzzlePart`, `ConfirmLever`, `PuzzleReference` | 2.4 | M |
| [ ] | 2.6 | Kötü cin karıştırma (ruh formundan, enerji, aralık, telgraf) | 2.5, 1.7 | S |
| [ ] | 2.7 | `RoundSetupApplier` + `StageGate` + `ItemReward` + Inventory | 2.3, 2.5 | M |

### 2.1 — PuzzleState
- §7.1. **Testler:** Cycle/Set/Swap, kilitliyken op reddi, `ResetToSolution`, `IsSolved`.

### 2.2 — Ev marker'ları
- §8 marker bileşenleri (Gizmo çizimli: renkli küp + etiket), `HouseService` (NetId deterministik), `HouseValidator` + menü.
- 🖐 elle / MCP: graybox eve marker'ları yerleştir (Claude Code MCP ile yerleştirir, kullanıcı kontrol eder).
- **Kabul:** `House/Validate` temiz.

### 2.3 — RoundSetupPlanner
- §7.5. **Testler:** aynı seed → aynı plan hash; 500 seed → validator temiz; Boya gerektiren bulmaca hep Boya'dan sonra; referans ≠ bulmaca odası.
- **Kabul:** `House/Plan Batch Report (500 seeds)` → %100 geçerli + tip dağılımı raporu.

### 2.4 — PuzzleSystem
- §7.3 akışı, `IObjectiveInfo` gerçek uygulaması.
- **Testler:** NetMsg round-trip; yanlış onay → hasar + cooldown; kilitli bulmacaya op reddi.

### 2.5 — Resimler bulmacası
- `P_Puzzle_Paintings` (3 tablo duvarda, KayKit'te tablo yok → quad + basit desen texture), `P_PuzzleRef_Paintings` (SpiritOnly, yarı saydam).
- **Kabul (MCP):** Insan tabloyu çevirir → `PuzzleStateChanged`; çözülmüşken kol → Completed; F2'de referans görünüyor, F1'de görünmüyor (screenshot ×2).

### 2.6 — Karıştırma
- **Kabul (MCP):** F3 → tabloya bak → E → tablo döner + titreşir, enerji düşer; aydınlık odada (stub) reddedilir.

### 2.7 — Applier, geçit, ödül, envanter
- §7.6. Inventory (3 slot, otoriter), `ReqDropItem`, yerdeki eşya possessable.
- **Kabul (MCP):** Raund başı plan uygulanıyor; aşamadaki 2 bulmaca (şimdilik 2 × Resimler kopyası olabilir) tamam → geçit açılıyor → ödül eşyası alınıyor; F8 → yeni seed ile farklı yerleşim.

### ✅ M2 Çıkış Testi
F8 ile 3 farklı seed: her birinde bulmacalar farklı odalarda, referanslar başka odalarda; F3 karıştırır, F2 referansa bakar, F1 çözer + onaylar → geçit açılır.

---

## M3 — Tam Oynanış (offline)

| ✔ | ID | Görev | Bağımlılık | Boyut |
|---|---|---|---|---|
| [ ] | 3.1 | P2 Heykel Açısı | 2.7 | M |
| [ ] | 3.2 | P3 Kablolar (+ şalter kutusu, Şalter Anahtarı gereksinimi) | 2.7 | M |
| [ ] | 3.3 | P4 Büyü Sembolü (+ Boya gereksinimi) | 2.7 | M |
| [ ] | 3.4 | Işık sistemi: lambalar, düğmeler, `LightService`, aydınlanınca atılma, Patlat | 2.2, 1.8 | M |
| [ ] | 3.5 | İyi cin yetenekleri: Lamba Yak/Onar, İşaret, Kov, Eşyaya Gir (hasarsız) | 3.4, 1.7 | L |
| [ ] | 3.6 | Bahçe aşaması: KeyHideSpot, ön kapı GateLock, alet kulübesi feneri | 2.7 | S |
| [ ] | 3.7 | Mahzen: kapak, DigSpot, kürek, kazı ilerlemesi, Öfke, TreasureDug | 2.7 | M |
| [ ] | 3.8 | Fener (eşya) + Parlat + Tekme + mobilya itme | 1.8, 2.7 | M |
| [ ] | 3.9 | Görünürlük servisi (GDD §12 tablosu) + soğuk nefes | 1.7 | M |
| [ ] | 3.10 | Maç tam: 4 raund rotasyon, puan, MatchEnd | 1.10, 3.7 | M |
| [ ] | 3.11 | HUD'lar: İnsan, İyi Cin, Kötü Cin (GDD §13) | 3.5, 3.10 | L |
| [ ] | 3.12 | Dummy İnsan botu (NavMesh gezinme) | 2.2 | S |

Her bulmaca görevi için kabul: tip planner'a eklendi, 500 seed raporu geçerli, MCP ile çözülme + karıştırma + referans screenshot'ı.

### ✅ M3 Çıkış Testi (tam raund, hotseat)
Bahçe anahtarı → zemin kat 2 bulmaca → 1. kat 2 bulmaca → mahzen kazısı → SeekersWin; ayrı raundda 3 bayılma → JinnsWin; süre bitimi → JinnsWin. 4 raund → MatchEnd puanları doğru.

---

## M4 — Cila

| ✔ | ID | Görev | Boyut |
|---|---|---|---|
| [ ] | 4.1 | Ev v1: KayKit ile 3 kat + çatı katı + mahzen + bahçe (Claude Code iskelet, 🖐 elle detay) | L |
| [ ] | 4.2 | Ana menü, ayarlar, duraklat, rol açıklama | M |
| [ ] | 4.3 | Raund raporu + maç sonu unvanları (`StatsService`, `TitleCalculator`) | M |
| [ ] | 4.4 | Ses: telgraf, karıştırma, bulmaca, ortam, müzik | M |
| [ ] | 4.5 | VFX: cin görselleri, referans parıltısı, lamba patlaması | M |
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
| [ ] | 6.1 | 3 playtest oturumu, notlar `docs/PLAYTEST.md` |
| [ ] | 6.2 | Denge değişiklikleri (config + GDD §14 aynı commit) |
| [ ] | 6.3 | P5 Kitaplar & Şömine, P6 Yapboz (isteğe bağlı) |
