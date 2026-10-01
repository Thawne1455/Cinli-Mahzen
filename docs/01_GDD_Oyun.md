# CİNLİ MAHZEN — Oyun Tasarım Dokümanı

> Sürüm: **GDD v2.0 — 2026-10-01** (v1 "zindan + öldürme" tasarımının yerini alır)
> Bu doküman "oyun ne, nasıl oynanır, nasıl hissettirir" sorularını cevaplar.
> Teknik karşılıklar: `02_GDD_Teknik.md`. Görev listesi: `03_TODO.md`. Asset eşleştirme: `04_Asset_Eslestirme.md`.
> **Buradaki tüm sayısal değerler `GameBalanceConfig` içinde birebir aynı isimle bulunur.** Sayı değişirse önce config, sonra bu doküman.

---

## 1. Tek Cümlede Oyun

Ormandaki terk edilmiş köy evinin **mahzeninde bir hazine** var; define avcısı gün doğmadan evin bulmaca zincirini çözüp hazineyi kazmaya çalışırken, **iyi cin** ona sadece kendisinin görebildiği cevapları fısıldar, **iki kötü cin** ise bulmacaları karıştırıp eşyaları üstüne fırlatarak onu durdurmaya çalışır.

- **Tür:** Asimetrik 2v2 online parti oyunu (iletişim + bulmaca + kaos)
- **Oyuncu:** Tam 4 kişi — **1 İnsan + 1 İyi Cin** (Arayıcılar) **vs** **2 Kötü Cin** (Cinler)
- **Maç:** 4 raund, her raund en fazla 15 dk. İnsan rolü her raund döner → herkes 1 kez insan olur.
- **Ton:** Korku-komedi. Gergin ama salak.
- **Kamera:** Herkes **FPS**. Cin bir eşyanın içine girince **TPS** (eşyanın etrafında dönen kamera).
- **İletişim:** Discord. **Takımlar ayrı ses kanalında** (İnsan + İyi Cin / 2 Kötü Cin). Lobi ekranı hatırlatır. Oyun içi "İşaret" sesi olmadan da oynanabilir kılar.
- **Görsel:** KayKit Dungeon paketi (low-poly, sevimli). Karanlık ama okunaklı.

### İlham
*Keep Talking and Nobody Explodes* (cevabı gören ≠ çözen) + *poltergeist kaosu* (eşyaya girip fırlatma) + *escape room* (kapı → eşya → sonraki kapı).

### Tasarım Sütunları
1. **Konuşmak zorunlu.** İnsan cevabı göremez, iyi cin bulmacaya dokunamaz. Takım ancak konuşarak ilerler.
2. **Ezber yok.** Ev sabit, ama hangi odada hangi bulmacanın olduğu, cevaplar ve eşyaların yeri her raund **rastgele (seed'li)**.
3. **Herkes her an meşgul.** Kötü cinler ya bulmaca karıştırıyor ya pusu kuruyor ya ışık söndürüyor. İyi cin ya cevap okuyor ya ışık yakıyor ya kovuyor.
4. **Adil telgraf.** Her saldırının önceden işareti var (titreme, gıcırtı). Dikkatli insan kaçar.
5. **İlerleme kaybolmaz.** Onaylanmış bulmaca kilitlenir; kötü cinler geri alamaz. Kaybettirdikleri şey **zaman**.

---

## 2. Hikaye & Tema

Orman içinde, köyden uzak, **3 katlı eski bir köy evi**. Rivayete göre evin sahibi büyücü dede, altınlarını **mahzene** gömmüş ve evi cinlere emanet etmiş. Define avcısı gece yarısı bahçe kapısından girer; **gün doğunca köylüler gelecek**.

- Yanında dedesinden kalma, sevimli bir **iyi cin** vardır (ruh dünyasını görür, fiziksel dünyaya zor dokunur).
- Evin **iki kötü cini** misafir sevmez; bulmacaları bozar, eşyaları fırlatır.
- Türk folkloru tatları: büyü sembolü, büyücü kitapları, şömine, mahzen, define kazısı.
- Süre dolarsa: *"Horoz öttü. Köylüler kazmalarla geldi. Define avcısı kaçak kazıdan gözaltında."*
- Çok bayılırsa: *"Define avcısı üçüncü kez bayıldı ve bu sefer kalkmamaya karar verdi."*

---

## 3. Raund Akışı (Oyun Döngüsü) ⭐

```
[Rol Açıklama 3 sn] → [Giriş 5 sn]
   → AŞAMA 0: BAHÇE        — ev anahtarını bul → ön kapıyı aç
   → AŞAMA 1: ZEMİN KAT    — 2 paralel bulmaca (ikisi de gerekli, sıra serbest) → merdiven kapısı
   → AŞAMA 2: 1. KAT       — 2 paralel bulmaca → çatı katı kapısı
   → AŞAMA 3: ÇATI KATI    — 2 paralel bulmaca → bodrum anahtarı
   → FİNAL:   MAHZEN       — kürekle hazineyi kaz (uzun basılı tutma, en gergin an)
   → [Raund Sonu 8 sn: rapor] → sonraki raund
```

- **Sayaç (gün doğumu):** `RoundDuration` = 900 sn. Herkesin ekranında güneş/ay göstergesi.
- **Cinler Uyanıyor:** İlk `JinnWakeDelay` = 10 sn kötü cinler hiçbir şeye dokunamaz (konumlanma süresi).
- **İnsan bahçede başlar.** Bahçe aşaması kötü cinlere doğal bir hazırlık süresi verir: bu sırada evin içinde bulmacaları karıştırır, mobilyaları kapı önlerine iterler.
- **Kazanma:**
  - **Arayıcılar:** Hazine tamamen kazıldı.
  - **Cinler:** Gün doğdu (süre bitti) **veya** insan `FaintsToLose` = 3 kez bayıldı.

### 3.1 Aşama yapısı
- Her aşama bir **kat/bölge** ile eşleşir ve **kilitli bir geçitle** biter (kapı / merdiven kapısı / bodrum kapağı).
- Her aşamada **2 bulmaca** aynı anda açıktır. **İkisi de çözülmeli**, sıra serbest. İnsan hangisine gideceğini seçer → kötü cinler bölünmek zorunda kalır.
- Bir bulmaca çözülünce **ödül** verir: bir **görev eşyası** (bkz. §6) ve/veya geçidin kilit parçası. Aşamadaki iki bulmaca da çözülünce geçit açılır.
- Bazı bulmacalar başlamak için **eşya ister** (ör. Sembol Boyama → Boya). Rastgele dağıtım, gerekli eşyanın **daha önceki bir aşamada** veya **aynı aşamadaki diğer bulmacanın ödülü** olarak verilmesini garanti eder.

> MVP (ilk oynanabilir sürüm): Bahçe + **2 aşama** (zemin kat + 1. kat, toplam 4 bulmaca) + Mahzen. Çatı katı aşaması bulmaca havuzu büyüyünce açılır.

---

## 4. Bulmaca Sistemi ⭐

### 4.1 Her bulmacanın iki hali
| Hal | Ne | Nerede | Kim görür |
|---|---|---|---|
| **Referans** | Doğru çözülmüş hal (cevap) | Evin **başka bir odasında** (aynı odada asla; mümkünse başka katta) | **Sadece cinler** (ruh gözü — iyi ve kötü) |
| **Etkileşimli** | Oynanabilen bulmaca (parçalar döner, yer değiştirir, boyanır) | Aşamanın odasında | Herkes |

### 4.2 Bulmacanın yaşam döngüsü
```
Raund başı: Etkileşimli bulmaca ÇÖZÜLMÜŞ halde başlar (= referansla aynı)
   │
   ├─ Kötü cinler parçaları istedikleri gibi karıştırır (ruh formunda, parçaya bakıp E)
   ├─ İnsan parçaları çevirir/yerleştirir; iyi cin referansa bakıp tarif eder
   │      (kötü cinler bu sırada da karıştırmaya devam edebilir!)
   ▼
İnsan ONAY KOLUNU çeker
   ├─ Doğruysa → TAMAMLANDI: bulmaca kilitlenir, kimse dokunamaz, ödül çıkar
   └─ Yanlışsa → CEZA: elektrik çarpar (1 hasar) + yüksek ses + `ConfirmFailCooldown` 5 sn kol kilitli
```
- **Neden çözülmüş başlıyor?** Kötü cinlerin işi bozmak. Hiç karıştırmadıkları bulmacayı insan doğrudan onaylayıp geçer → kötü cinler enerjilerini ve zamanlarını nereye harcayacaklarına karar vermek zorunda.
- **Karıştırma telgraflıdır:** Parça dönerken gıcırdar, kısa süre titrer. İnsan "biri bunu bozuyor" anlar.
- **Karıştırmanın bedeli:** Her işlem `ScrambleEnergyCost` = 4 enerji, aynı bulmacada cin başına `ScrambleInterval` = 0.4 sn aralık. Aydınlık odada karıştırma **yapılamaz** (bkz. §7).
- İyi cin referansı görmek için **o odaya uçmalıdır**; referans ile bulmaca arasında gidip gelmek iyi cinin ana döngüsüdür.

### 4.3 Bulmaca havuzu
Hepsi aynı modele oturur: **N parça × her parçanın K olası değeri**. Cevap = her parçanın doğru değeri.

| # | Bulmaca | Parçalar / değerler | İnsan ne yapar | Referans neye benzer | Gereken eşya | MVP |
|---|---|---|---|---|---|---|
| P1 | **Resimler** | 3 tablo × 8 açı (45°) | Tabloya bakıp E → 45° döndürür | Başka odada aynı 3 tablonun ruhani kopyası, doğru açılarda | — | ✅ |
| P2 | **Heykel Açısı** | 1 heykel × 12 yön (yatay) + 1 × 4 eğim | E / Shift+E ile yön/eğim değiştirir | Heykelin gölgesi duvarda doğru yönü gösterir | — | ✅ |
| P3 | **Kablolar (Şalter)** | 4 soket × 4 renk kablo | Sokete bakıp E → kablo rengini değiştirir | Ruhani devre şeması (renk sırası) | **Şalter Anahtarı** (kutuyu açar) | ✅ |
| P4 | **Büyü Sembolü** | 3×3 nokta ızgarası, 12 çizgi × (boyalı/boş) | Boyayla çizgi çeker/siler | Ruhani tamamlanmış sembol | **Boya** | ✅ |
| P5 | **Kitaplar & Şömine** | 5 kitap sırası (permütasyon) + 1 okunacak sayfa (4 seçenek) | Kitap yer değiştirir, sayfa seçer | Ruhani raf + parlayan sayfa | — | sonra |
| P6 | **Yapboz** | 6 parça × 6 yuva | Parçaları yerleştirir | Ruhani tamamlanmış resim | — | sonra |

- Bulmaca her raund **farklı bir odaya** düşer (oda o tipe uygunsa — bkz. §9 Harita).
- Cevap her raund **rastgele** (seed); etkileşimli bulmacanın başlangıç hali = cevap.
- **Yapboz (P6) özel:** iyi cin de parça taşıyabilir (eşyaya girme ile) → iki takımın aynı bulmaca üzerinde kapıştığı tek bulmaca.

### 4.4 Bahçe aşaması (özel, bulmaca değil)
- **Ev anahtarı** bahçedeki `KeyHideSpot` adaylarından birinde (saksı, kova, kuyu kenarı, odun yığını…). Seed seçer.
- Aranan yer basılı tut `SearchHoldTime` = 1.0 sn.
- **İyi cin** anahtarın bulunduğu yeri ruh parıltısı olarak `KeySenseRange` = 10 m içinden görür.
- **Kötü cinler** de görür; anahtarın saklandığı kabın içine girip onu sürükleyebilir/fırlatabilir.
- **Fener** alet kulübesinde, garanti (başlangıçtan ~15 m).

### 4.5 Final: Mahzen kazısı
- Mahzen kapağı bodrum anahtarıyla açılır. Mahzende `DigSpot` adaylarından biri gerçek (iyi cin görür).
- **Kürek** gerekli. Kazı toplam `DigTotalTime` = 20 sn; basılı tutarken ilerler, bırakınca **ilerleme kalır**.
- Kazarken insan savunmasız (bakış serbest, hareket yok) ve çok gürültülü.
- Kazı başladığında kötü cinlere **ÖFKE**: enerji dolumu ×2, bekleme süreleri ×0.6.

---

## 5. Roller

### 5.1 🧔 İNSAN — Define Avcısı
**Amaç:** Bulmaca zincirini çözüp hazineyi kazmak. **3 kez bayılma.**

| Özellik | Değer (`config`) |
|---|---|
| Can | 3 (`HumanMaxHp`) |
| Bayılma | Can 0 → `FaintDuration` = 12 sn yerde; elindeki eşyayı düşürür; kalkınca can dolu. Sayaç +1 |
| Kaybetme | `FaintsToLose` = 3 bayılma |
| Hasar sonrası dokunulmazlık | 1.5 sn (`HumanInvulnAfterHit`) |
| Yürüme / Koşma | 3.5 / 5.5 m/s, stamina 100 (koşu −20/sn, dolum +15/sn, 1 sn gecikme) |
| Etkileşim menzili | 2.2 m (`InteractRange`) |
| Envanter | 3 slot görev eşyası (`ItemSlotCount`) — 1/2/3 ile seç |

**Yetenekleri**
- **Etkileşim (E):** Bulmaca parçası çevir, onay kolu çek, kapı aç, ara, kaz, ışık düğmesi, mobilya it.
- **Mobilya itme (E basılı, `PushHoldTime` = 1.5 sn):** Kapı önüne çekilmiş mobilyayı 1 hücre iter.
- **Tekme (F):** 2 m. İçinde kötü cin olan eşyaya vurursa cin `KickStun` = 2.5 sn sersemler. Bekleme 6 sn.
- **Fener (eşya, bulunduktan sonra):** Karanlıkta görmeyi sağlar. **Sağ tık = Parlat:** 8 m konide cin girmiş eşyalar 3 sn parlar. Bekleme 12 sn.
- **Işık düğmeleri:** Odadaki lambayı açar/kapatır (patlak lambayı açamaz).

**His:** "Tamam söyle… ikinci tablo kaç derece? DUR, biri tabloyu çeviriyor!"

### 5.2 😇 İYİ CİN — Rehber
**Amaç:** Cevapları okuyup insana anlatmak, yol göstermek, ışık tutmak.

| Özellik | Değer |
|---|---|
| Hareket | FPS uçuş, 7 m/s, duvarlardan geçer (`GoodJinnSpeed`) |
| İnsan görür mü? | **Soluk bir parıltı** olarak (konum belli, ifade yok) |

**Görüşü:**
- Bulmaca **referanslarını**, anahtar saklanma yerini, gerçek kazı noktasını görür (`SpiritOnly`).
- Ruh formundaki kötü cinleri 20 m içinde duvar arkasından görür; eşyada sinsi bekleyeni 4 m, aksiyon şarj edeni 25 m.

**Yetenekleri:**
- **Işık Yak / Onar (E, lambaya bakarak, 8 m):** Kapalı lambayı uzaktan yakar (anında). Patlak lambayı onarır (basılı tut `RepairHoldTime` = 2 sn).
- **İşaret (Q):** Bir noktaya/eşyaya 8 sn insanın da gördüğü parıltı. Max 2 aktif.
- **Kov (R basılı 1.5 sn, 3 m):** Eşyadaki kötü cini dışarı atar → 4 sn sersem + 4 sn eşyaya giremez. Bekleme 18 sn.
- **Eşyaya Gir (Shift+E):** Kötü cinlerle aynı possession sistemi, ama aksiyonları **hasar vermez**: eşyayı it/kaydır (kapı önünü açmak), küçük eşyayı insana doğru fırlat/taşı (yere düşmüş görev eşyasını getirmek). Enerji kullanır (`GoodEnergy*`).

**His:** Uçan bir navigasyon cihazı. "Referans üst katta, bekle uçuyorum… Tamam: soldaki tablo baş aşağı!"

### 5.3 😈 KÖTÜ CİN ×2 — Bozguncular
**Amaç:** Gün doğana kadar oyalamak veya insanı 3 kez bayıltmak.

| Özellik | Değer |
|---|---|
| Ruh formu | FPS uçuş, 6.5 m/s, duvarlardan geçer; insana görünmez |
| Enerji | Max 100, başlangıç 50, dolum 5/sn |
| Eşyaya girme | 3 m, 1.2 sn — bu sırada eşya titrer ve gıcırdar |
| Aydınlık oda | Eşyaya **giremez**, bulmaca **karıştıramaz**. İçerideyken oda aydınlanırsa 2 sn sonra dışarı fırlar |

**Yaptıkları:**
1. **Bulmaca karıştırma** (ruh formu, parçaya bakıp E) — §4.2.
2. **Eşya fırlatma** — küçük eşyalara girip insana fırlat (hasar).
3. **Mobilya itme** — büyük eşyaya girip kapı önüne/koridora kaydır (yol tıkar).
4. **Devirme** — rafı insanın üstüne devir (hasar + yere düşürme).
5. **Işık patlatma** — lambaya girip patlat → oda karanlık, sadece iyi cin onarabilir.
6. **Görev eşyası saklama** — yerdeki (bayılınca düşmüş) eşyaya girip uzağa fırlat.

**His:** Beceriksiz poltergeist ekibi. "Sen resimleri karıştır, ben lambayı patlatıp sandalyeyle kafasına atlıyorum!"

---

## 6. Görev Eşyaları

| Eşya | Nereden çıkar | Ne işe yarar |
|---|---|---|
| **Ev Anahtarı** | Bahçe (saklanma yeri) | Ön kapıyı açar |
| **Fener** | Alet kulübesi (garanti) | Karanlıkta görme + Parlat |
| **Şalter Anahtarı** | Bir bulmaca ödülü | Kablo bulmacasının (P3) kutusunu açar |
| **Boya** | Bir bulmaca ödülü | Büyü sembolü (P4) |
| **Kürek** | Bir bulmaca ödülü (son aşama) | Mahzende kazı |
| **Bodrum Anahtarı** | Son aşamanın geçidi | Mahzen kapağını açar |

- Eşyalar yere düşebilir (bayılma, bilerek bırakma G). Yerdeki eşya **küçük possessable**'dır: kötü cin içine girip fırlatabilir.
- Gerekli olmayan eşya raunda konmaz (ör. P3 yoksa şalter anahtarı yok).
- *(Sonra)* **Büyü craftı:** etraftan toplanan malzemelerle koruyucu muska (MVP dışı).

---

## 7. Işık Mekaniği ⭐

Ev karanlık başlar (sadece ay ışığı). Her odada 1+ lamba ve duvarda düğme var.

| Durum | Ne olur |
|---|---|
| **Aydınlık oda** | Kötü cinler bu odada eşyaya giremez, bulmaca karıştıramaz. İnsan rahat görür. |
| **Karanlık oda** | Her şey serbest. İnsan fenersiz neredeyse kör; bulmaca detayları zor seçilir. |
| **Patlak lamba** | Düğmeyle açılmaz. Sadece iyi cin onarır (2 sn). |

- **İnsan:** düğmeyle açar/kapar (düğmeye gitmesi lazım).
- **İyi cin:** uzaktan yakar, patlak lambayı onarır.
- **Kötü cin:** lambaya girip **patlatır** (`LampBurstEnergy` = 30 enerji, oda karanlık + lamba patlak). Aydınlık odadaki lambanın kendisine girebilir (tek istisna) ama girerken 1.2 sn titreme telgrafı herkese görünür.
- **Sonuç:** Işık bir **bölge kontrolü** çekişmesi: iyi cin insanın çalıştığı odayı aydınlık tutmaya, kötü cinler karartmaya çalışır.

---

## 8. Eşyalar (Possessable)

Kontroller eşyanın içindeyken: **Sol tık = Aksiyon 1**, **Sağ tık = Aksiyon 2**, **A/D = sınırlı yön**, **WASD = hareket** (sadece hareketli eşyalarda), **Space/E = çık**.

| # | Eşya (KayKit) | Kötü cin aksiyonu | Telgraf | Etki | Enerji | Bekleme |
|---|---|---|---|---|---|---|
| 1 | **Şişe / Mum / Tabak** (küçük) | **Fırlat** | 0.5 sn sallanma | A/D ±45°, yay atış. İsabet: 1 hasar | 15 | — (kırılır / yere düşer) |
| 2 | **Tabure / Sandalye** | **Zıpla** (WASD) / **Çarp** | Çarp: 0.4 sn geri çekilme | Zıpla 1 m/0.5 sn, 2 enerji. Çarp 2 m atılma, 1 hasar | 2 / 15 | — / 5 sn |
| 3 | **Fıçı / Sandık / Masa** (büyük) | **Kaydır** | 0.6 sn sürtünme sesi | 1 hücre (yaklaşık 1.5 m) kayar; kapı önünü tıkar | 10 | 3 sn |
| 4 | **Raf** | **Devril** | 1.0 sn öne eğilme + gıcırtı | Önündeki alan: 1 hasar + `KnockdownTime` 2 sn yere düşme | 35 | tek kullanım |
| 5 | **Lamba** (mum / avize / duvar lambası) | **Patlat** | 1.2 sn titreme + vızıltı | Oda karanlık, lamba patlak | 30 | 20 sn |
| 6 | **Görev eşyası** (yerdeyken) | **Fırlat** | 0.5 sn | Eşyayı 8 m uzağa fırlatır, hasar yok | 10 | 8 sn |

- Aynı eşyada aynı anda tek cin. Aynı eşyaya tekrar girme bekleme 10 sn.
- İyi cin aynı eşyalara girebilir ama sadece **Kaydır** ve hasarsız **Taşı/Fırlat** yapabilir.
- Kombo örneği: Lamba patlat → karanlıkta resimleri karıştır → insan dönerken raf devril.

---

## 9. Dünya & Harita Kuralları

**Mekân:** Orman içinde **3 katlı köy evi** + bahçe + **mahzen**.

| Bölge | İçerik |
|---|---|
| **Bahçe** | Ön kapı, alet kulübesi, kuyu, odun yığını, saksılar (anahtar saklanma adayları), çit, ağaçlar (sınır) |
| **Zemin kat** | Giriş holü, mutfak, oturma odası (şömine), kiler, merdiven |
| **1. kat** | Yatak odaları, banyo, koridor, çalışma odası |
| **Çatı katı** | Tavan arası, büyü odası |
| **Mahzen** | Hazine kazı alanı (bodrum kapağıyla girilir) |

- **Ev elle tasarlanır** (oda konumları, kapılar, merdivenler sabit). Claude Code mümkün olduğunca kurar, ince işler elle.
- **Her raund rastgele (seed) olanlar:**
  1. Hangi bulmacanın hangi odada olduğu (odadaki `PuzzleSlot`'lar hangi tipleri kabul ettiğini söyler)
  2. Her bulmacanın referansının hangi odada olduğu (`ReferenceSlot`'lar; bulmacayla aynı oda olamaz)
  3. Bulmaca cevapları
  4. Ev anahtarının saklandığı yer, gerçek kazı noktası
  5. Hangi bulmacanın hangi eşyayı ödül verdiği (bağımlılık kurallarına uyarak)
- Her odada ≥ 1 lamba + düğme. Her katta en az 1 döngü (kaçış yolu), çıkmaz koridor sınırlı.
- Possessable eşyalar elle yerleştirilir (kat başına ~10-15).

---

## 10. Puanlama & Maç Sonu

- **Arayıcılar kazanırsa:** İnsan ve iyi cin, **kalan saniye** kadar puan alır.
- **Cinler kazanırsa:** Her kötü cin `JinnWinPoints` = 300 puan alır (+ bayıltarak kazandılarsa `FaintWinBonus` = 60).
- 4 raund sonunda en yüksek puanlı oyuncu kazanır (roller döndüğü için puan bireyseldir).
- **Rol rotasyonu:** Raund r: İnsan = oyuncu r, İyi Cin = oyuncu r+1, diğer ikisi kötü cin.

**Raund raporu (8 sn):** Süre, çözülen bulmacalar, bayılma sayısı ve sebepleri ("2. bayılma: Uçan tabure"), yanlış onay sayısı, karıştırma sayısı, patlatılan lamba.
**Maç sonu unvanları:** "Elektrikçi" (en çok yanlış onay), "Ressam" (en çok karıştırma), "Ampul Katili", "Navigasyon" (en çok işaret), "Tekmeci Dayı" (boşa tekme).

---

## 11. Kontroller

### İnsan (FPS)
| Tuş | Eylem |
|---|---|
| WASD / Fare | Hareket / Bakış |
| Shift | Koş |
| E (bas / basılı) | Etkileşim / döndür / ara / kaz / it |
| F | Tekme |
| Sağ Tık | Fener Parlat |
| 1 / 2 / 3 | Eşya slotu seç |
| G | Seçili eşyayı bırak |
| Tab | Hedefler / skor |

### Cin — Ruh Formu (FPS uçuş)
| Tuş | Eylem |
|---|---|
| WASD / Fare | Uç / Bak |
| Space / Ctrl | Yüksel / Alçal |
| Shift | Hızlı uçuş |
| E | **Kötü:** Eşyaya gir / bulmaca parçasını karıştır · **İyi:** Lamba yak/onar |
| Shift+E | **İyi:** Eşyaya gir |
| Q | **İyi:** İşaret |
| R (basılı) | **İyi:** Kov |

### Eşyanın İçinde (TPS)
| Tuş | Eylem |
|---|---|
| Fare | Kamerayı eşyanın etrafında döndür |
| Sol / Sağ Tık | Aksiyon 1 / 2 |
| A / D | Sınırlı yön |
| WASD | Hareket (sadece sandalye/tabure) |
| Space / E | Çık |

---

## 12. Kamera, Telgraf & Görünürlük

| Durum | İnsan | İyi Cin | Kötü Cin |
|---|---|---|---|
| Kötü cin ruh formunda | ❌ (3 m içinde soğuk nefes) | ≤ 20 m görür | Takım arkadaşı her zaman |
| Kötü cin eşyada bekliyor | ❌ (Fener Parlat ile parlar) | ≤ 4 m | ✅ |
| Aksiyon şarjı | Telgraf (herkes) | ≤ 25 m kırmızı | ✅ |
| İyi cin | **Soluk parıltı** | Kendi | Soluk |
| Bulmaca referansı | ❌ | ✅ | ✅ |
| Anahtar yeri / gerçek kazı noktası | ❌ | ✅ | ✅ |
| İşaret (ping) | ✅ | ✅ | ❌ |
| Bulmaca karıştırılıyor | Parça döner + gıcırtı | ✅ | ✅ |

FOV: İnsan 75, ruh formu 85 (renkli vinyet: iyi turkuaz, kötü mor), eşya içi TPS 70.

---

## 13. UI Ekranları

1. **Ana Menü:** Oyna (Online — sonra), Test Oyunu (offline hotseat), Ayarlar, Çıkış
2. **Lobi:** Oda kodu, 4 slot, hazır, Discord takım kanalı hatırlatması
3. **Rol Açıklama (3 sn)**
4. **HUD — İnsan:** Can (3 kalp), bayılma sayacı (☠ 1/3), stamina, 3 eşya slotu, aşama ilerlemesi (Zemin Kat: ✔ Resimler · ◻ Kablolar), gün doğumu göstergesi, etkileşim ipucu
5. **HUD — İyi Cin:** Yetenek bekleme süreleri, enerji, insanın canı/bayılma, aşama ilerlemesi, insanın yönü
6. **HUD — Kötü Cin:** Enerji, aksiyon kartları, takım arkadaşı, aşama ilerlemesi, ÖFKE
7. **Raund Raporu**, **Maç Sonu**, **Duraklat/Ayarlar**

Dil: Türkçe öncelikli, tüm metinler `Loc` anahtarlı.

---

## 14. Denge — Tek Tablo (ilk değerler)

| Config adı | Değer | Not |
|---|---|---|
| `RoundDuration` | 900 | sn (gün doğumu) |
| `JinnWakeDelay` / `IntroCountdown` | 10 / 5 | sn |
| `RoundsPerMatch` | 4 | |
| `StagesActive` | 2 | MVP: 2 aşama (4 bulmaca); tam oyun 3 |
| `PuzzlesPerStage` | 2 | |
| `HumanMaxHp` / `HumanInvulnAfterHit` | 3 / 1.5 | |
| `FaintDuration` / `FaintsToLose` | 12 / 3 | |
| `KnockdownTime` | 2 | sn (raf devrilmesi) |
| `HumanWalkSpeed` / `HumanSprintSpeed` | 3.5 / 5.5 | m/s |
| `StaminaMax` / `StaminaDrain` / `StaminaRegen` / `StaminaRegenDelay` | 100 / 20 / 15 / 1 | |
| `InteractRange` | 2.2 | m |
| `SearchHoldTime` / `PushHoldTime` / `DigTotalTime` | 1 / 1.5 / 20 | sn |
| `ConfirmFailDamage` / `ConfirmFailCooldown` | 1 / 5 | |
| `KickRange` / `KickCooldown` / `KickStun` | 2 / 6 / 2.5 | |
| `LanternPulseRange` / `LanternPulseDuration` / `LanternPulseCooldown` | 8 / 3 / 12 | |
| `ItemSlotCount` | 3 | |
| `EvilJinnSpeed` / `GoodJinnSpeed` / `SpiritBoostMult` | 6.5 / 7 / 1.5 | |
| `EnergyMax` / `EnergyStart` / `EnergyRegen` | 100 / 50 / 5 | kötü cin |
| `GoodEnergyMax` / `GoodEnergyRegen` | 100 / 8 | iyi cin |
| `PossessRange` / `PossessTime` / `ReenterCooldown` | 3 / 1.2 / 10 | |
| `LitRoomEjectDelay` | 2 | sn |
| `ScrambleEnergyCost` / `ScrambleInterval` / `ScrambleRange` | 4 / 0.4 / 3 | |
| `LampBurstEnergy` / `LampLightRange` / `RepairHoldTime` | 30 / 8 / 2 | |
| `ExorciseRange` / `ExorciseHoldTime` / `ExorciseCooldown` / `ExorciseStun` / `ExorcisePossessLock` | 3 / 1.5 / 18 / 4 / 4 | |
| `PingDuration` / `PingCooldown` / `PingMaxActive` | 8 / 3 / 2 | |
| `GoodSightSpiritRange` / `GoodSightLurkRange` / `GoodSightChargeRange` | 20 / 4 / 25 | m |
| `KeySenseRange` | 10 | m |
| `ColdBreathRange` | 3 | m |
| `RageRegenMult` / `RageCooldownMult` | 2 / 0.6 | kazı başlayınca |
| `JinnWinPoints` / `FaintWinBonus` | 300 / 60 | |

Possessable değerleri §8 tablosundadır ve her birinin `PossessableDefinition` asset'inde tutulur.

---

## 15. Kapsam Dışı (şimdilik)
- Büyü craftı, Kitaplar & Şömine (P5), Yapboz (P6) — MVP sonrası
- Oyun içi sesli sohbet (Discord)
- Kozmetik / ilerleme sistemi, matchmaking (sadece oda kodu), late join
- Bot oyuncular (sadece geliştirme amaçlı "Dummy İnsan")

## 16. Açık Sorular (playtest ile netleşecek)
- 15 dk raund çok mu uzun? (4 raund = 1 saat maç)
- Kötü cinler bulmacaları çok hızlı bozuyor mu? (`ScrambleEnergyCost` ile ayarlanır)
- İnsan fenersiz bahçede başlamalı mı, fener başlangıç eşyası mı olmalı?
- Puanlama: kalan saniye ile sabit cin puanı dengeli mi?
