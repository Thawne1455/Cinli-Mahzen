# CİNLİ MAHZEN — Asset Eşleştirme

> Sürüm: **v2.1 — 2026-10-15** (`01_GDD_Oyun.md` v2.1, `02_GDD_Teknik.md` v2.1 ve `03_TODO.md` v2.1 ile senkronize)
> Kaynak: repo kökündeki `KayKit_Dungeon_Pack_1.1_FREE/` (ham paket) -> içindeki `Assets/fbx(unity)/` -> `Assets/_Project/Art/KayKit/Models/`; texture `dungeon_texture.png` -> `Art/KayKit/Textures/`.
> Tüm modeller tek texture atlas -> tek materyal `M_KayKit_Dungeon`. Lisans CC0 (`Art/KayKit/License.txt`).
> Import + prefablar `agent-c/m0-assets-scene` dalında hazır -> TODO **P.2** ile `main`'e alınır.
> Karar: köy evi de **KayKit Dungeon** parçalarıyla kurulur (taş duvar + ahşap zemin). Eksikler §5.

---

## 1. Ölçüler (C0.1'de ölçüldü)

| Model | Bounds (x x y x z) | Pivot | Not |
|---|---|---|---|
| `wall` | 4.00 x 4.00 x 1.00 | segment merkezi, taban y=0; uzunluk yerel X, kalınlık Z (+/-0.5) | Grid hücresi |
| `wall_doorway` | 4.00 x 4.00 x 1.00 | wall ile aynı; kapı mesh'i ayrı çocuk (`wall_doorway_door`) | **Kapı var** -> bulmaca/aşama kapıları (+90 sn bonus) |
| `wall_gated` | 4.00 x 4.00 x 1.00 | wall ile aynı (parmaklık) | Aşama geçidi alternatifi |
| `floor_tile_large` | 4.00 x 0.15 x 4.00 | hücre merkezi; üst yüzey +0.05 | |
| `floor_tile_small` | 2.00 x 0.15 x 2.00 | karo merkezi | |
| `stairs` | 5.00 x 5.10 x 4.00 | pivot yerelde z=0 ucu (üst uç); **yerel −Z yönünde (pivota doğru) yükselir** (2026-10-16 raycast ölçümü): 8 basamak x 0.5 m = 4 m yükseklik / 4 m koşu (45°), 5.1 m üst korkuluk | `Env_Stairs`/`Env_StairsWide` prefablarında 45° `RampCollider` var (`CinliMahzen/Setup/Add Ramp Colliders To Stairs`); insan `slopeLimit` 50° ile çıkar |
| `shelf_large` | 2.00 x 0.45 x 0.50 | arka yüz z=0 | |
| `barrel_large` | 1.80 x 2.00 x 1.80 | taban merkezi | |
| `chest` | 1.70 x 1.30 x 1.45 | taban merkezi | |
| `chair` | 0.75 x 1.23 x 0.75 | taban merkezi | |

**Grid:** 4 m. **Duvar yüksekliği / kat yüksekliği:** 4 m. **Duvar kalınlığı:** 1 m, grid çizgisine ortalı (köşelerde `pillar`).

---

## 2. Ev & Bahçe

| Kullanım | Model(ler) |
|---|---|
| Zemin (ev içi) | `floor_wood_large`, `floor_wood_large_dark`, `floor_wood_small(_dark)` |
| Zemin (mahzen, kiler) | `floor_tile_large`, `floor_tile_small_*`, `floor_dirt_*` |
| Zemin (bahçe) | `floor_dirt_*`, `floor_tile_small_weeds_A/B` |
| Duvar | `wall`, `wall_cracked`, `wall_half` (bahçe çiti yerine), `wall_window_closed` (pencere) |
| Köşe / bağlantı | `wall_corner`, `wall_Tsplit`, `wall_crossing`, `wall_endcap`, `pillar` |
| **Kapı** | `wall_doorway` + `wall_doorway_door` (açılır kapı) |
| **Aşama geçidi** | `wall_doorway_door` (kilitli) veya `wall_gated` |
| Merdiven | `stairs`, `stairs_walled`, `stairs_wide` |
| Tavan | `ceiling_tile` |
| Dekor | `crates_stacked`, `box_stacked`, `barrel_small_stack`, `rubble_*`, `wall_shelves`, `keyring_hanging` |

**Graybox ev v0 (TODO 1.9):** yerleşim verisi `Assets/_Project/Scripts/World/Graybox/GrayboxLayout.cs` (`Default`; sabit ve elle düzenlenir: oda dikdörtgenleri, kapılar, merdiven, korkuluk, bahçe prop'ları; üretici yok). Yeniden kurma: `CinliMahzen/House/Build Graybox` → `Assets/_Project/Prefabs/World/P_House.prefab` (üzerine yazar); `Place House In Open Scene` açık sahneye (0,0,0) koyar; `Print Graybox ASCII Map` kat planlarını konsola yazar.
- Ölçek: 4 m grid, duvar grid çizgisine ortalı ve kenar başına bir kez (`GrayboxWallPlanner`); çit ve korkuluk = `Env_Wall` yükseklik/kalınlık ölçekli (`Env_WallHalf` 2 m uzunluğunda ama 4 m yüksekliğinde, çit işi görmez). Aşama geçidi ve ön kapı kanadı çizili, collider'ı kapalı (kilit 2.7'de).
- Merdiven: `Env_Stairs` **ölçeksiz**, 1 hücre (4 m koşu = 4 m kat, 45°; `RampCollider` ile `slopeLimit` 50° çıkar). Prefab döndürülmeden yerel −Z'ye yükselir, pivot üst uçtadır: pivot varış kenarının ortasına konur, yaw = `GrayboxStairs.ModelYawDegrees` (Kuzey için 180°, giriş +Z ucu = güney).

---

## 3. Possessable Eşyalar (`PD_*` v2.1)

| Oyun eşyası (`PD_*`) | Model(ler) | Boyut (`PossessableSize`) | Özel Not / Görsel |
|---|---|---|---|
| Şişe (`PD_Bottle`) | `bottle_A_*`, `bottle_B_*`, `bottle_C_*` | `Small` | Yay atış, kırılır |
| Mum (`PD_Candle`) | `candle_triple`, `candle_lit`, `candle_thin_lit` | `Small` | Aynı zamanda lamba olabilir |
| Tabak (`PD_Plate`) | `plate_stack` | `Small` | Yay atış, kırılır |
| **Anne Terliği (`PD_Slipper`)** | ⚠️ Geçici: yassı kapsül + bant mesh (veya CC0 terlik modeli) | `Slipper` | Kat başına 1 adet, kırılmaz, düz çizgi hızlı atış (`CameraSpin180`) |
| Tabure / Sandalye (`PD_Stool`, `PD_Chair`) | `stool`, `chair` | `Small` (hareketli) | `hop` ve `lunge` aksiyonları |
| Fıçı / Sandık / Masa (`PD_Barrel`, `PD_Chest`, `PD_Table`) | `barrel_large`, `chest`, `trunk_*`, `table_medium`, `table_long`, `table_small` | `Large` | 1.5 m hücre kaydırma (`slide`), kapı önü tıkama |
| Raf (`PD_Shelf`) | `shelf_large`, `shelves`, `shelf_small` | `Shelf` | Öne devrilme (`topple`), tek kullanım |
| Lamba (`PD_Lamp`) | `candle_triple` (masa), `torch_mounted` (duvar lambası yerine) | `Lamp` | Kötü cin patlatır (`burst`), İyi Cin uzaktan yakar/onarır |
| **Eski Radyo / Gramofon (`PD_Radio`)** | ⚠️ Geçici: `box_small` + silindir boru/anten | `Radio` | Çalışırken hafif sallanır + nota/ses dalgası VFX (`IsNoiseSuppressed` = 12 sn) |
| **Limon Kolonyası (`PD_Cologne`)** | `bottle_B_green` / sarı tint'li `bottle_A_labeled` | `Cologne` | Kötü cin uzağa fırlatır (`fling`); İyi Cin baygın insanda kırıp anında ayıltır (`revive_splash`) |
| Görev Eşyası (`PD_QuestItem`) | İlgili `P_Item_*` modeli (yerdeyken) | `QuestItem` | Yere düşmüş görev eşyası (kötü cin saklamak için, iyi cin getirmek için fırlatır) |

---

## 4. Bulmacalar, Görev Eşyaları, Hedefler (v2.1)

| Nesne | Model / Kurulum | Durum |
|---|---|---|
| **P1: Portreler (Resimler)** | ⚠️ İnce küp/quad çerçeve + 3 portre çizimi (Bıyıklı Dede, Pörtlek Nine, Ters Eşek); 90 derece (4 yön) döner | Quad + 2D Sprite/Texture |
| **P2: Heykelin Nazarlığı** | Merkezde `pillar_decorated` + hareketli asa (`torch_unlit`) + etrafında 6 sabit obje: Mum (`candle_lit`), Kafatası (`skull` / kutu), Kedi (figür), İbrik (`bottle_C_brown`), Çarık (primitive), Ayna (parlak quad) | ✅ KayKit + basit primitive kombinasyonu |
| **P3: Tılsımlı Şalter (Kablolar)** | ⚠️ Küp şalter kutusu + 4 soket + 6 köy motifi plakası (Yılan, Göz, Anahtar, Orak, Çekiç, Horoz). Referansta 2 motifin üzerinde kırmızı **Çarpı (X)** quad'ı | Primitive + 2D Sembol İkonları |
| **P4: Mühür Taşları (Büyü Sembolü)** | Duvarda `floor_tile_small_decorated` + köşelerde 4 sembol (Ay, Güneş, Yıldız, Kemik) + 12 adet `LineRenderer` / ince quad çizgi. Referans 0/90/180/270 derece dönük yerleşir | ✅ Geçici hazır |
| **Onay Kolu (`ConfirmLever`)** | ⚠️ Küp kaide + eğilen silindir kol + elektrik kıvılcımı VFX (yanlış onayda) | Primitive |
| **Sabotaj Hedefi (`PuzzleScrambleTarget`)** | Bulmaca ana gövdesinde `Interactable` collider; sabote edildiğinde 15 sn boyunca mor duman/mühür VFX (cooldown göstergesi) | Kod + basit VFX |
| **Referanslar & Ruh Perdesi** | Bulmacanın aynı `PuzzleView` görsel kodu (`SpiritOnly` layer). Normalde buğulu/kapalı perde quad'ı ile örtülüdür; yalnızca İyi Cin Sağ Tık (`FocusReference`) basılı tuttuğunda perde kalkar ve netleşir | Kod + Shader/Materyal |
| **Sabit Fener (`HumanLantern`)** | `torch_lit` (insanın elinde sabit; başlangıçta cılız sarı ışık, güçlenince parlak beyaz/mavimsı ışık + 2.5 m koruma halkası) | ✅ Geçici |
| **Kutsanmış Yağ (`ID_BlessedOil`)** | `bottle_A_labeled` + altın rengi parıltı (Bahçe alet kulübesinde garanti) | ✅ |
| **Ev / Bodrum / Şalter Anahtarı** | `key`, `keyring_hanging` (farklı renk tint'leri ile ayrılır) | ✅ |
| **Boya (`ID_Paint`)** | `bottle_C_*` / `box_small` + kırmızı/mavi boya tint | ✅ Geçici |
| **Kürek (`ID_Shovel`)** | ⚠️ Silindir sap + yassı küp ağız | Primitive |
| **3 Katmanlı Kazı Noktası (`DigSpot`)** | **Katman 0-1 (Toprak):** `floor_dirt_small_A` + `rubble_half`<br>**Katman 1-2 (Tahta Mühür):** `floor_wood_small_dark` (kırık tahta kapak)<br>**Katman 2-3 (Sandık):** Yukarı yükselen `chest_gold` + `coin_stack_*` | ✅ KayKit modelleriyle kademeli görünürlük |

---

## 5. Eksik Asset Listesi (Cila Fazı / M4 Öncesi)

| Öncelik | Asset | Neden | Öneri (ücretsiz) | Geçici Çözüm (M1–M3) |
|---|---|---|---|---|
| 🔴 | **İnsan karakteri + animasyon** | Cinler insanı yürürken, kazarken ve bayılırken görüyor | KayKit Adventurers + KayKit Character Animations (CC0) | Kapsül |
| 🔴 | **Bahçe/orman**: ağaç, çit, kuyu, alet kulübesi, saksı | Bahçe aşaması (`KeyHideSpot` ve `ToolShedItemPoint`) | KayKit Forest Nature Pack / Quaternius Nature (CC0) | Primitive + `wall_half` |
| 🔴 | **2D Sembol & Portre Seti** (15 PNG) | P1 (3 portre: Dede, Nine, Eşek), P3 (6 köy motifi + Çarpı), P4 (Ay, Güneş, Yıldız, Kemik) | Özel çizim / AI-generated low-poly stilize ikonlar | Renkli/harfli geçici quad'lar |
| 🟠 | **Anne Terliği & Eski Radyo & Kürek modelleri** | v2.1 folklorik possessable'lar ve mahzen kazısı | Quaternius / Kenney Furniture Kit (CC0) veya Blender'da 10 dk low-poly modelleme | Birleştirilmiş primitive'ler |
| 🟠 | **Tablo çerçevesi, heykel, şalter kutusu** | Bulmaca gövdeleri | Quaternius / Kenney Furniture Kit (CC0) | Primitive + `pillar_decorated` |
| 🟠 | **Ev mobilyası** (yatak, dolap, şömine) | Köy evi oda kimliği (1. kat yatak odası, zemin kat oturma odası) | Kenney Furniture Kit (CC0) | KayKit masa/sandık |
| 🟠 | **Ses efektleri (v2.1)** | Telgraf, toplu sabotaj gıcırtısı, Anne Terliği **"ŞRAK!"**, Eski Radyo **cızırtılı oyun havası**, Kolonya dökme, Ruh Çarpışması | Kenney Audio, freesound.org (CC0) | Sessiz + konsol log |
| 🟠 | **Türkçe karakterli font** | UI (`ğ, ü, ş, ı, ö, ç, İ`) | Google Fonts: Nunito / Baloo 2 / Rubik (OFL) | TMP varsayılan |
| 🟡 | Müzik, cin VFX texture'ları | Atmosfer, Ruh Perdesi ve Ruh Siperi efektleri | Incompetech, Kenney Particle Pack | — |

Yeni asset eklendiğinde: `Assets/_Project/Art/<Kategori>/` altına lisans dosyasıyla konur ve bu tablodaki durumu "✅ eklendi" olarak güncellenir.