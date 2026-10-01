# CİNLİ MAHZEN — Asset Eşleştirme

> Kaynak: repo kökündeki `KayKit_Dungeon_Pack_1.1_FREE/` (ham paket) → içindeki `Assets/fbx(unity)/` → `Assets/_Project/Art/KayKit/Models/`; texture `dungeon_texture.png` → `Art/KayKit/Textures/`.
> Tüm modeller tek texture atlas → tek materyal `M_KayKit_Dungeon`. Lisans CC0 (`Art/KayKit/License.txt`).
> Import + prefablar `agent-c/m0-assets-scene` dalında hazır → TODO **P.2** ile `main`'e alınır.
> Karar: köy evi de **KayKit Dungeon** parçalarıyla kurulur (taş duvar + ahşap zemin). Eksikler §5.

---

## 1. Ölçüler (C0.1'de ölçüldü)

| Model | Bounds (x × y × z) | Pivot | Not |
|---|---|---|---|
| `wall` | 4.00 × 4.00 × 1.00 | segment merkezi, taban y=0; uzunluk yerel X, kalınlık Z (±0.5) | Grid hücresi |
| `wall_doorway` | 4.00 × 4.00 × 1.00 | wall ile aynı; kapı mesh'i ayrı çocuk (`wall_doorway_door`) | **Kapı var** → bulmaca/aşama kapıları |
| `wall_gated` | 4.00 × 4.00 × 1.00 | wall ile aynı (parmaklık) | Aşama geçidi alternatifi |
| `floor_tile_large` | 4.00 × 0.15 × 4.00 | hücre merkezi; üst yüzey +0.05 | |
| `floor_tile_small` | 2.00 × 0.15 × 2.00 | karo merkezi | |
| `stairs` | 5.00 × 5.10 × 4.00 | alt kenar ortası; +Z yönünde yükselir | Kat yüksekliği 4 m ile uyum için ölçek/sahanlık gerekir |
| `shelf_large` | 2.00 × 0.45 × 0.50 | arka yüz z=0 | |
| `barrel_large` | 1.80 × 2.00 × 1.80 | taban merkezi | |
| `chest` | 1.70 × 1.30 × 1.45 | taban merkezi | |
| `chair` | 0.75 × 1.23 × 0.75 | taban merkezi | |

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

---

## 3. Possessable Eşyalar

| Oyun eşyası (`PD_*`) | Model(ler) | Boyut |
|---|---|---|
| Şişe (`PD_Bottle`) | `bottle_A_*`, `bottle_B_*`, `bottle_C_*` | Small |
| Mum (`PD_Candle`) | `candle_triple`, `candle_lit`, `candle_thin_lit` | Small (aynı zamanda lamba olabilir) |
| Tabak (`PD_Plate`) | `plate_stack` | Small |
| Tabure / Sandalye | `stool`, `chair` | Small (hareketli) |
| Fıçı / Sandık / Masa | `barrel_large`, `chest`, `trunk_*`, `table_medium`, `table_long`, `table_small` | Large (kaydır) |
| Raf (`PD_Shelf`) | `shelf_large`, `shelves`, `shelf_small` | Shelf (devril) |
| Lamba (`PD_Lamp`) | `candle_triple` (masa), `torch_mounted` (duvar lambası yerine) | Lamp |

---

## 4. Bulmacalar, Görev Eşyaları, Hedefler

| Nesne | Model | Durum |
|---|---|---|
| Tablolar (P1) | — | ⚠️ Model yok → ince küp/quad çerçeve + desen texture |
| Heykel (P2) | `pillar_decorated` / `sword_shield` üstte | ⚠️ Geçici; heykel modeli eksik |
| Şalter kutusu + kablolar (P3) | — | ⚠️ Model yok → küp kutu + renkli silindir kablolar |
| Büyü sembolü (P4) | `floor_tile_small_decorated` duvarda + LineRenderer çizgiler | Geçici |
| Onay kolu | — | ⚠️ Küp + silindir kol |
| Referanslar | Bulmacanın aynı görsel kodu, SpiritOnly yarı saydam materyal | Kodla |
| Ev / Bodrum Anahtarı, Şalter Anahtarı | `key`, `keyring_hanging` | ✅ |
| Fener | `torch_lit` (elde) | ✅ geçici |
| Boya | `bottle_C_*` / `box_small` renk tint | Geçici |
| Kürek | — | ⚠️ Model yok → silindir sap + düz küp |
| Hazine | `chest_gold`, `coin_stack_*` | ✅ |
| Kazı noktası | `floor_tile_small_broken_A`, `rubble_half` | ✅ |

---

## 5. ⚠️ Eksik Asset Listesi

| Öncelik | Asset | Neden | Öneri (ücretsiz) | Geçici |
|---|---|---|---|---|
| 🔴 | **İnsan karakteri + animasyon** | Cinler insanı görüyor | KayKit Adventurers + KayKit Character Animations (CC0) | Kapsül |
| 🔴 | **Bahçe/orman**: ağaç, çit, kuyu, alet kulübesi, saksı | Bahçe aşaması | KayKit Forest Nature Pack / Quaternius (CC0) | Primitive |
| 🟠 | **Tablo, heykel, şalter kutusu, kürek, kova** | Bulmacalar | Quaternius / Kenney Furniture Kit (CC0) | Primitive |
| 🟠 | **Ev mobilyası** (yatak, dolap, şömine) | Ev hissi | Kenney Furniture Kit (CC0) | KayKit masa/sandık |
| 🟠 | **Ses efektleri** | Telgraf + bulmaca geri bildirimi | Kenney Audio, freesound (CC0) | Sessiz + log |
| 🟠 | **Türkçe karakterli font** | UI | Nunito / Baloo 2 | TMP varsayılan |
| 🟡 | Müzik, cin VFX texture'ları | Atmosfer | Incompetech, Kenney Particle Pack | — |

Yeni asset: `Assets/_Project/Art/<Kategori>/` altına, lisans dosyasıyla; bu tabloya "✅ eklendi".
