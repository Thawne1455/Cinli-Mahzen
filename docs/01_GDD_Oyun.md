# CİNLİ MAHZEN — Oyun Tasarım Dokümanı

> Sürüm: **GDD v2.1 — 2026-10-15** (v2.0 üzerine mekanik darboğaz çözümleri, iletişim odaklı bulmaca revizyonları, dinamik süre ve kademeli puanlama eklenmiştir)
> Bu doküman "oyun ne, nasıl oynanır, nasıl hissettirir" sorularını cevaplar.
> Teknik karşılıklar: `02_GDD_Teknik.md`. Görev listesi: `03_TODO.md`. Asset eşleştirme: `04_Asset_Eslestirme.md`.
> **Buradaki tüm sayısal değerler `GameBalanceConfig` içinde birebir aynı isimle bulunur.** Sayı değişirse önce config, sonra bu doküman.

---

## 1. Tek Cümlede Oyun

Ormandaki terk edilmiş köy evinin **mahzeninde bir hazine** var; define avcısı gün doğmadan evin bulmaca zincirini çözüp hazineyi kazmaya çalışırken, **iyi cin** ona sadece kendisinin görebildiği cevapları fısıldar, **iki kötü cin** ise bulmacaları sabote edip eşyaları üstüne fırlatarak onu durdurmaya çalışır.

- **Tür:** Asimetrik 2v2 online parti oyunu (iletişim + bulmaca + kaos)
- **Oyuncu:** Tam 4 kişi — **1 İnsan + 1 İyi Cin** (Arayıcılar) **vs** **2 Kötü Cin** (Cinler)
- **Maç:** 4 raund, her raund ortalama 6–9 dk (dinamik gün doğumu süresi). İnsan rolü her raund döner → herkes 1 kez insan olur.
- **Ton:** Korku-komedi. Gergin ama salak.
- **Kamera:** Herkes **FPS**. Cin bir eşyanın içine girince **TPS** (eşyanın etrafında dönen kamera).
- **İletişim:** Discord. **Takımlar ayrı ses kanalında** (İnsan + İyi Cin / 2 Kötü Cin). Lobi ekranı hatırlatır. Oyun içi "İşaret" sesi olmadan da oynanabilir kılar.
- **Görsel:** KayKit Dungeon paketi (low-poly, sevimli). Karanlık ama okunaklı.

### İlham
*Keep Talking and Nobody Explodes* (cevabı gören ≠ çözen) + *poltergeist kaosu* (eşyaya girip fırlatma) + *escape room* (kapı → eşya → sonraki kapı).

### Tasarım Sütunları
1. **Konuşmak zorunlu.** İnsan cevabı göremez, iyi cin bulmacaya dokunamaz. Takım ancak konuşarak ilerler.
2. **Ezber yok.** Ev sabit, ama hangi odada hangi bulmacanın olduğu, cevaplar ve eşyaların yeri her raund **rastgele (seed'li)**.
3. **Herkes her an meşgul.** Kötü cinler ya bulmaca sabote ediyor ya iyi cinin kopya çekmesini engelliyor ya da pusu kuruyor. İyi cin ya cevap okuyor ya ışık yakıyor ya da insanı koruyor.
4. **Adil telgraf ve E-spam koruması.** Her saldırının önceden işareti var (titreme, gıcırtı). Kötü cinler insanın burnunun dibinde görünmezken bulmaca parçalarını spamlamaz; önce insanı uzaklaştırmaları gerekir.
5. **İlerleme kaybolmaz.** Onaylanmış bulmaca ve tamamlanmış kazı katmanları kilitlenir; kötü cinler geri alamaz. Kaybettirdikleri şey **zaman**.

---

## 2. Hikaye & Tema

Orman içinde, köyden uzak, **3 katlı eski bir köy evi**. Rivayete göre evin sahibi büyücü dede, altınlarını **mahzene** gömmüş ve evi cinlere emanet etmiş. Define avcısı gece yarısı bahçe kapısından girer; **gün doğunca köylüler gelecek**.

- Yanında dedesinden kalma, sevimli bir **iyi cin** vardır (ruh dünyasını görür, fiziksel dünyaya zor dokunur).
- Evin **iki kötü cini** misafir sevmez; bulmacaları bozar, eşyaları fırlatır.
- Türk folkloru ve köy evi tatları: büyü sembolü, eski portreler, anne terliği, limon kolonyası, cızırtılı radyo, şömine, mahzen, define kazısı.
- Süre dolarsa: *"Horoz öttü. Köylüler kazmalarla geldi. Define avcısı kaçak kazıdan gözaltında."*
- Çok bayılırsa: *"Define avcısı üçüncü kez bayıldı ve bu sefer kalkmamaya karar verdi."*

---

## 3. Raund Akışı (Oyun Döngüsü) ⭐

```
[Rol Açıklama 3 sn] → [Giriş 5 sn]
   → AŞAMA 0: BAHÇE        — ev anahtarını ve kutsanmış yağı bul → ön kapıyı aç (+90 sn)
   → AŞAMA 1: ZEMİN KAT    — 2 paralel bulmaca (ikisi de gerekli, sıra serbest) → merdiven kapısı (+90 sn)
   → AŞAMA 2: 1. KAT       — 2 paralel bulmaca → çatı katı / mahzen kapısı (+90 sn)
   → AŞAMA 3: ÇATI KATI    — (Tam sürümde) 2 paralel bulmaca → bodrum anahtarı (+90 sn)
   → FİNAL:   MAHZEN       — kürekle hazineyi 3 katmanda kaz (en gergin an)
   → [Raund Sonu 8 sn: rapor] → sonraki raund
```

- **Dinamik Sayaç (Gün Doğumu):** Raund `BaseRoundDuration` = 360 sn (6 dk) ile başlar. Arayıcılar her aşama geçidini açtığında sayaca `StageTimeBonus` = +90 sn eklenir. Herkesin ekranında güneş/ay göstergesi vardır.
- **Cinler Uyanıyor:** İlk `JinnWakeDelay` = 10 sn kötü cinler hiçbir şeye dokunamaz (konumlanma süresi).
- **İnsan bahçede zayıf fenerle başlar.** Bahçe aşaması kötü cinlere doğal bir hazırlık süresi verir: bu sırada evin içinde bulmacaları sabote eder, mobilyaları kapı önlerine iterler.
- **Kazanma:**
  - **Arayıcılar:** Hazine tamamen kazıldı (3 katman tamamlandı).
  - **Cinler:** Gün doğdu (süre bitti) **veya** insan `FaintsToLose` = 3 kez bayıldı.

### 3.1 Aşama yapısı
- Her aşama bir **kat/bölge** ile eşleşir ve **kilitli bir geçitle** biter (kapı / merdiven kapısı / bodrum kapağı).
- Her aşamada **2 bulmaca** aynı anda açıktır. **İkisi de çözülmeli**, sıra serbest. İnsan hangisine gideceğini seçer → kötü cinler bölünmek zorunda kalır.
- Bir bulmaca çözülünce **ödül** verir: bir **görev eşyası** (bkz. §6) ve/veya geçidin kilit parçası. Aşamadaki iki bulmaca da çözülünce geçit açılır ve +90 sn süre eklenir.
- Bazı bulmacalar başlamak için **eşya ister** (ör. Sembol Boyama → Boya). Rastgele dağıtım, gerekli eşyanın **daha önceki bir aşamada** veya **aynı aşamadaki diğer bulmacanın ödülü** olarak verilmesini garanti eder. Bulmaca çözülünce gerektirdiği eşya envanterden **düşer** (raundda onu isteyen çözülmemiş başka bulmaca yoksa); anahtarlar da kilidinde kalır. Böylece 3 slot hep yeter.

> MVP (ilk oynanabilir sürüm): Bahçe + **2 aşama** (zemin kat + 1. kat, toplam 4 bulmaca) + Mahzen. Çatı katı aşaması bulmaca havuzu büyüyünce açılır.

---

## 4. Bulmaca Sistemi ⭐

### 4.1 Her bulmacanın iki hali
| Hal | Ne | Nerede | Kim görür |
|---|---|---|---|
| **Referans** | Doğru çözülmüş hal (cevap) | Evin **başka bir odasında** (aynı odada asla; mümkünse başka katta) | **Sadece cinler** (İyi cin odaklanarak açar; kötü cinler yerini görür) |
| **Etkileşimli** | Oynanabilen bulmaca (parçalar döner, yer değiştirir, boyanır) | Aşamanın odasında | Herkes |

### 4.2 Bulmacanın yaşam döngüsü ve Anti-Spam Kuralları
```
Raund başı: Etkileşimli bulmaca ÇÖZÜLMÜŞ halde başlar (= referansla aynı)
   │
   ├─ Kötü cinler bulmacayı TOPLU SABOTE eder (ruh formunda, 1.5 sn E basılı tutarak)
   ├─ İyi cin referans odasında Sağ Tık basılı tutarak Ruh Perdesini aralar ve cevabı okur
   ├─ İnsan parçaları çevirir/yerleştirir (Feneri açıkken bulmaca yakını sabotaja korunaklıdır)
   ▼
İnsan ONAY KOLUNU çeker
   ├─ Doğruysa → TAMAMLANDI: bulmaca kilitlenir, kimse dokunamaz, ödül çıkar
   └─ Yanlışsa → CEZA: elektrik çarpar (1 hasar) + yüksek ses + `ConfirmFailCooldown` 5 sn kol kilitli
```
- **Neden çözülmüş başlıyor?** Kötü cinlerin işi bozmak. Hiç sabote etmedikleri bulmacayı insan doğrudan onaylayıp geçer → kötü cinler enerjilerini ve zamanlarını nereye harcayacaklarına karar vermek zorunda.
- **Toplu Sabotaj (E-Spam Önleme):** Kötü cin parçaları tek tek çevirmek yerine bulmacaya bakıp `ScrambleHoldTime` = 1.5 sn `E` basılı tutarak tüm bulmacayı tek seferde rastgele bir dizilime sokar (`ScrambleEnergyCost` = 25 enerji). Sabote edilen bulmacaya `ScramblePuzzleCooldown` = 15 sn boyunca tekrar sabotaj yapılamaz. Sabotaj sırasında tüm parçalar sesli şekilde gıcırdar ve titrer.
- **Fener Koruması (Yakın Alan Kilidi):** İnsan bulmacaya `LanternSafeZoneRadius` = 2.5 m mesafedeyken ve feneri Kutsanmış Yağ ile güçlendirilmişse (Güçlü Mod), kötü cinler o bulmacayı **ruh formunda doğrudan sabote edemez**. Kötü cinin önce insanı oradan uzaklaştırması (eşya fırlatma, raf devirme, sandalye ile çarpma) veya odayı karartıp dikkatini dağıtması gerekir. Aydınlık odada da sabotaj **yapılamaz** (bkz. §7).
- **Ruh Perdesi (Referans Okuma ve Kovalamaca):** Referanslar duvarda sürekli açık durmaz. İyi cin referansın olduğu odada **Sağ Tık (Ruh Görüşü Odakla)** basılı tutarak (`FocusReferenceCost` = 2 enerji/sn) referansı görünür kılar. Bu sırada iyi cin parlar. Kötü cin ruh formundayken referans okuyan iyi cinin içinden hızla (`Shift` ile) geçerse **Ruh Çarpışması** yaşanır: iyi cinin görüşü `SpiritBumpBlurTime` = 2 sn bulanıklaşır ve referans kapanır.

### 4.3 Bulmaca havuzu (Korku-Komedi İletişim Odaklı)
Hepsi aynı teknik modele oturur (**N parça × her parçanın K olası değeri**), ancak diyalogların matematiksel değil komik ve panik dolu olması için görsel semboller kullanılır:

| # | Bulmaca | Parçalar / değerler | İnsan ne yapar | Referans neye benzer | Gereken eşya | MVP |
|---|---|---|---|---|---|---|
| P1 | **Portreler (Resimler)** | 3 komik/ürkütücü portre (Bıyıklı Dede, Pörtlek Nine, Ters Eşek) × 4 açı (90°: Düz, Sağ, Sol, Ters) | Tabloya bakıp E → 90° döndürür | Başka odada aynı 3 portrenin ruhani kopyası, doğru yönlerde (*"Dedeyi baş aşağı çevir, nine sola baksın!"*) | — | ✅ |
| P2 | **Heykelin Nazarlığı** | 1 heykel × 6 çevre objesi yönü (Mum, Kafatası, Kedi, İbrik, Çarık, Ayna) + 3 asa eğimi (Yukarı, Orta, Aşağı) | E ile baktığı objeyi, Shift+E ile asa eğimini değiştirir | Heykelin ruhani gölgesi doğru objeyi işaret eder ve asasının duruşunu gösterir (*"Heykeli çarığa döndür, asayı indir!"*) | — | ✅ |
| P3 | **Tılsımlı Şalter (Kablolar)** | 4 soket × 6 köy motifi fişi (Yılan, Göz, Anahtar, Orak, Çekiç, Horoz) | Sokete bakıp E → takılı motifli fişi değiştirir | Ruhani şemada 6 sembol görünür; doğru 4'ü sıralıdır, 2 tanesinin üzerinde tuzak **Çarpı (X)** vardır | **Şalter Anahtarı** (kutuyu açar) | ✅ |
| P4 | **Mühür Taşları (Büyü Sembolü)** | 3×3 nokta ızgarası (köşelerinde Ay, Güneş, Yıldız, Kemik sembolleri), 12 çizgi × (boyalı/boş) | Boyayla çizgi çeker/siler | Ruhani tamamlanmış sembol; ancak duvara 90°/180° dönük yansıtılmış olabilir (köşe sembollerinden yön hizalanır) | **Boya** | ✅ |
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
- **Kutsanmış Yağ (Fener Güçlendirmesi):** İnsan oyuna elinde cılız yanan bir fenerle başlar (envanter slotu kaplamaz). Bahçedeki alet kulübesinde (başlangıçtan ~15 m) garanti olarak bulunan **Kutsanmış Yağ** alındığında fener tam menziline kavuşur, bulmaca başında koruma sağlar ve **Sağ Tık (Parlat)** yeteneği açılır.

### 4.5 Final: Mahzen kazısı ve Ruh Siperi
- Mahzen kapağı bodrum anahtarıyla açılır. Mahzende `DigSpot` adaylarından biri gerçek (iyi cin görür).
- **Kürek** gerekli. Kazı toplam `DigTotalTime` = 18 sn sürer ve `DigCheckpoints` = **3 katmandan** (her biri 6 sn) oluşur:
  1. *Toprak Kazısı (6 sn)* → Tamamlanınca checkpoint alınır.
  2. *Tahta Mühür Kırma (6 sn)* → Tamamlanınca checkpoint alınır.
  3. *Hazine Sandığını Çıkarma (6 sn)* → Oyun kazanılır.
- İnsan kazıyı bıraktığında ilerleme **son tamamlanan 6 saniyelik katmana** kadar korunur (veya mevcut katman içinde kaldığı yerden devam eder).
- Kazarken insan savunmasızdır (bakış serbest, hareket yok) ve çok gürültülüdür.
- Kazı başladığında kötü cinlere **ÖFKE**: enerji dolumu ×2, bekleme süreleri ×0.6.
- **Ruh Siperi (İyi Cin Final Görevi):** Mahzen kazısı sırasında iyi cin, insanın etrafında uçarak kötü cinlerin fırlattığı küçük eşyalara (şişe, tabak, terlik) kendi ruh bedeniyle **göğüs gerebilir (Body-block)**. Havada durdurulan eşya yere düşer, iyi cinin `SpiritBlockEnergyCost` = 30 enerjisini siler ve onu hafifçe geri savurur ama insanın kazısı bölünmez.

---

## 5. Roller

### 5.1 🧔 İNSAN — Define Avcısı
**Amaç:** Bulmaca zincirini çözüp hazineyi kazmak. **3 kez bayılma.**

| Özellik | Değer (`config`) |
|---|---|
| Can | 3 (`HumanMaxHp`) |
| Bayılma | Can 0 → `FaintDuration` = 12 sn yerde; elindeki görev eşyasını düşürür; kalkınca can dolu. Sayaç +1 |
| Kaybetme | `FaintsToLose` = 3 bayılma |
| Hasar sonrası dokunulmazlık | 1.5 sn (`HumanInvulnAfterHit`) |
| Yürüme / Koşma | 3.5 / 5.5 m/s, stamina 100 (koşu −20/sn, dolum +15/sn, 1 sn gecikme) |
| Etkileşim menzili | 2.2 m (`InteractRange`) |
| Envanter | Sabit Fener + 3 slot görev eşyası (`ItemSlotCount`) — 1/2/3 ile seç |

**Yetenekleri**
- **Etkileşim (E):** Bulmaca parçası çevir, onay kolu çek, kapı aç, ara, kaz, ışık düğmesi, radyo aç/kapat, mobilya it.
- **Mobilya itme (E basılı, `PushHoldTime` = 1.5 sn):** Kapı önüne çekilmiş mobilyayı 1 hücre iter.
- **Tekme (F):** 2 m. İçinde kötü cin olan eşyaya vurursa cin `KickStun` = 2.5 sn sersemler. Bekleme 6 sn.
- **Fener (Sabit ekipman):** Karanlıkta görmeyi sağlar. Kulübeden *Kutsanmış Yağ* alındıktan sonra:
  - İnsanın `LanternSafeZoneRadius` = 2.5 m çevresindeki bulmacalar ruh formunda sabote edilemez.
  - **Sağ tık = Parlat:** 8 m konide cin girmiş eşyalar 3 sn parlar. Bekleme 12 sn.
- **Işık düğmeleri:** Odadaki lambayı açar/kapatır (patlak lambayı açamaz).

**His:** "Tamam söyle… bıyıklı dede ne tarafa bakıyor? DUR, içeriden radyo sesi geliyor, kesin pusu kurdular!"

### 5.2 😇 İYİ CİN — Rehber ve Koruyucu
**Amaç:** Cevapları okuyup insana anlatmak, yol göstermek, ışık tutmak, finalde siper olmak.

| Özellik | Değer |
|---|---|
| Hareket | FPS uçuş, 7 m/s, duvarlardan geçer (`GoodJinnSpeed`) |
| Enerji | Max 100 (`GoodEnergyMax`), dolum 8/sn (`GoodEnergyRegen`) |
| İnsan görür mü? | **Soluk bir parıltı** olarak (konum belli, ifade yok) |

**Görüşü:**
- Bulmaca **referanslarının yerini**, anahtar saklanma yerini, gerçek kazı noktasını görür (`SpiritOnly`).
- Ruh formundaki kötü cinleri 20 m içinde duvar arkasından görür; eşyada sinsi bekleyeni 4 m, aksiyon şarj edeni 25 m.

**Yetenekleri:**
- **Ruh Görüşü Odakla (Sağ Tık basılı):** Referans odasında referans perdesini aralar (`FocusReferenceCost` = 2 enerji/sn). Okurken etrafına parıltı saçar.
- **Işık Yak / Onar (E, lambaya bakarak, 8 m):** Kapalı lambayı uzaktan yakar (anında). Patlak lambayı onarır (basılı tut `RepairHoldTime` = 2 sn).
- **İşaret (Q):** Bir noktaya/eşyaya 8 sn insanın da gördüğü parıltı. Max 2 aktif.
- **Kov (R basılı 1.5 sn, 3 m):** Eşyadaki kötü cini dışarı atar → 4 sn sersem + 4 sn eşyaya giremez. Bekleme 18 sn.
- **Eşyaya Gir (Shift+E):** Kötü cinlerle aynı possession sistemi, ama aksiyonları **hasar vermez**: eşyayı it/kaydır (kapı önünü açmak), küçük eşyayı insana doğru fırlat/taşı (yere düşmüş görev eşyasını getirmek), **Limon Kolonyasını** baygın insanın üstünde kırıp anında ayıltmak.
- **Ruh Siperi (Pasif - Mahzende):** Havada insana doğru uçan küçük eşyaları bedeniyle durdurur (-30 enerji).

**His:** Uçan bir navigasyon ve koruma meleği. "Referans üst katta, odaklanıyorum… Eyvah kötü cin üstüme uçuyor, kopyayı kapattı!"

### 5.3 😈 KÖTÜ CİN ×2 — Bozguncular
**Amaç:** Gün doğana kadar oyalamak veya insanı 3 kez bayıltmak.

| Özellik | Değer |
|---|---|
| Ruh formu | FPS uçuş, 6.5 m/s, duvarlardan geçer; insana görünmez |
| Enerji | Max 100, başlangıç 50, dolum 5/sn |
| Eşyaya girme | 3 m, 1.2 sn — bu sırada eşya titrer ve gıcırdar |
| Aydınlık oda | Eşyaya **giremez**, bulmaca **sabote edemez**. İçerideyken oda aydınlanırsa 2 sn sonra dışarı fırlar |

**Yaptıkları:**
1. **Bulmaca sabotajı** (ruh formu, bulmacaya bakıp 1.5 sn E basılı, 25 enerji) — §4.2.
2. **Ruh Çarpışması** (Shift ile uçarken referans okuyan iyi cinin içinden geçip görüşünü 2 sn bozma).
3. **Eşya fırlatma** — küçük eşyalara (veya *Anne Terliğine*) girip insana fırlat (hasar).
4. **Mobilya itme** — büyük eşyaya girip kapı önüne/koridora kaydır (yol tıkar).
5. **Devirme** — rafı insanın üstüne devir (hasar + yere düşürme).
6. **Işık patlatma** — lambaya girip patlat → oda karanlık, sadece iyi cin onarabilir.
7. **Ses yemi ve eşya saklama** — radyoyu açıp gürültü yapma; yerdeki görev eşyasına veya kolonyaya girip uzağa fırlatma.

**His:** Beceriksiz poltergeist ekibi. "Sen radyoyu açıp anne terliğine gir, ben lambayı patlatıp bulmacayı bozuyorum!"

---

## 6. Görev Eşyaları ve Özel Eşyalar

| Eşya | Nereden çıkar | Ne işe yarar |
|---|---|---|
| **Ev Anahtarı** | Bahçe (saklanma yeri) | Ön kapıyı açar |
| **Kutsanmış Yağ** | Alet kulübesi (garanti) | İnsanın başlangıç fenerini güçlendirir: Parlat (Sağ Tık) ve bulmaca koruma alanı açılır |
| **Şalter Anahtarı** | Bir bulmaca ödülü | Tılsımlı Şalter bulmacasının (P3) kutusunu açar |
| **Boya** | Bir bulmaca ödülü | Mühür Taşları sembol bulmacası (P4) |
| **Kürek** | Bir bulmaca ödülü (son aşama) | Mahzende 3 katmanlı kazı |
| **Bodrum Anahtarı** | Son aşamanın geçidi | Mahzen kapağını açar |
| **Limon Kolonyası** *(Özel)* | Evde rastgele 1 odada (tek kullanımlık) | İyi cin içine girip baygın insanın üstünde kırarsa `FaintDuration` (12 sn) anında biter, insan hemen ayağa kalkar |

- Görev eşyaları yere düşebilir (bayılma, bilerek bırakma G). Yerdeki eşya **küçük possessable**'dır: kötü cin içine girip fırlatabilir.
- Gerekli olmayan görev eşyası raunda konmaz (ör. P3 yoksa şalter anahtarı yok).

---

## 7. Işık Mekaniği ⭐

Ev karanlık başlar (sadece ay ışığı). Her odada 1+ lamba ve duvarda düğme var.

| Durum | Ne olur |
|---|---|
| **Aydınlık oda** | Kötü cinler bu odada eşyaya giremez, bulmaca sabote edemez. İnsan rahat görür. |
| **Karanlık oda** | Her şey serbest. İnsan fenersiz neredeyse kör; bulmaca detayları zor seçilir. |
| **Patlak lamba** | Düğmeyle açılmaz. Sadece iyi cin onarır (2 sn). |

- **İnsan:** düğmeyle açar/kapar (düğmeye gitmesi lazım).
- **İyi cin:** uzaktan yakar, patlak lambayı onarır.
- **Kötü cin:** lambaya girip **patlatır** (30 enerji, `PD_Lamp`, oda karanlık + lamba patlak). Aydınlık odadaki lambanın kendisine girebilir (tek istisna) ama girerken 1.2 sn titreme telgrafı herkese görünür.
- **Sonuç:** Işık bir **bölge kontrolü** çekişmesi: iyi cin insanın çalıştığı odayı aydınlık tutmaya, kötü cinler karartmaya çalışır.

---

## 8. Eşyalar (Possessable)

Kontroller eşyanın içindeyken: **Sol tık = Aksiyon 1**, **Sağ tık = Aksiyon 2**, **A/D = sınırlı yön**, **WASD = hareket** (sadece hareketli eşyalarda), **Space/E = çık**.

| # | Eşya (KayKit + Özel) | Kötü cin aksiyonu | Telgraf | Etki | Enerji | Bekleme |
|---|---|---|---|---|---|---|
| 1 | **Şişe / Mum / Tabak** (küçük) | **Fırlat** | 0.5 sn sallanma | A/D ±45°, yay atış. İsabet: 1 hasar | 15 | — (kırılır / yere düşer) |
| 2 | **Anne Terliği** (katta 1 adet, efsanevi küçük) | **Güdümlü Fırlat** | 0.6 sn havada dönme | Yay çizmez, baktığı yöne dümdüz hızlı gider. İsabet: 1 hasar + "ŞRAK!" sesiyle insanın kamerasını 180° döndürür | 25 | 15 sn (kırılmaz) |
| 3 | **Tabure / Sandalye** | **Zıpla** (WASD) / **Çarp** | Çarp: 0.4 sn geri çekilme | Zıpla 1 m/0.5 sn, 2 enerji. Çarp 2 m atılma, 1 hasar | 2 / 15 | — / 5 sn |
| 4 | **Fıçı / Sandık / Masa** (büyük) | **Kaydır** | 0.6 sn sürtünme sesi | 1 hücre (yaklaşık 1.5 m) kayar; kapı önünü tıkar | 10 | 3 sn |
| 5 | **Raf** | **Devril** | 1.0 sn öne eğilme + gıcırtı | Önündeki alan: 1 hasar + `KnockdownTime` 2 sn yere düşme | 35 | tek kullanım |
| 6 | **Lamba** (mum / avize / duvar lambası) | **Patlat** | 1.2 sn titreme + vızıltı | Oda karanlık, lamba patlak | 30 | 20 sn |
| 7 | **Eski Radyo / Gramofon** | **Türkü Çal (Ses Yemi)** | 0.3 sn cızırtı | Yüksek sesli oyun havası çalar; odadaki sabotaj gıcırtısını ve eşya titreme seslerini 12 sn bastırır | 15 | 20 sn |
| 8 | **Görev eşyası / Kolonya** (yerdeyken) | **Fırlat** | 0.5 sn | Eşyayı 8 m uzağa fırlatır, hasar yok (Kolonyayı iyi cin kırarsa baygın insanı ayıltır) | 10 | 8 sn |

- Aynı eşyada aynı anda tek cin. Aynı eşyaya tekrar girme bekleme 10 sn.
- İyi cin aynı eşyalara girebilir ama sadece **Kaydır**, hasarsız **Taşı/Fırlat** ve **Kolonya ile Ayıltma** yapabilir.
- Kombo örneği: Radyoyu aç (sesleri bastır) → Lamba patlat → İnsana Anne Terliği fırlatıp yönünü şaşırt → Bulmacayı sabote et.

---

## 9. Dünya & Harita Kuralları

**Mekân:** Orman içinde **3 katlı köy evi** + bahçe + **mahzen**.

| Bölge | İçerik |
|---|---|
| **Bahçe** | Ön kapı, alet kulübesi (Kutsanmış Yağ), kuyu, odun yığını, saksılar (anahtar saklanma adayları), çit, ağaçlar (sınır) |
| **Zemin kat** | Giriş holü, mutfak, oturma odası (şömine, eski radyo), kiler, merdiven |
| **1. kat** | Yatak odaları, banyo (kolonya aday noktası), koridor, çalışma odası |
| **Çatı katı** | Tavan arası, büyü odası |
| **Mahzen** | Hazine kazı alanı (kilerdeki bodrum kapağıyla girilir; kapak son aktif aşamadan düşen Bodrum Anahtarı ile açılır) |

- **Ev elle tasarlanır** (oda konumları, kapılar, merdivenler sabit). Claude Code mümkün olduğunca kurar, ince işler elle.
- **Her raund rastgele (seed) olanlar:**
  1. Hangi bulmacanın hangi odada olduğu (odadaki `PuzzleSlot`'lar hangi tipleri kabul ettiğini söyler)
  2. Her bulmacanın referansının hangi odada olduğu (`ReferenceSlot`'lar; bulmacayla aynı oda olamaz)
  3. Bulmaca cevapları (ve Mühür Taşları referansının dönüş açısı)
  4. Ev anahtarının saklandığı yer, Limon Kolonyasının yeri, gerçek kazı noktası
  5. Hangi bulmacanın hangi eşyayı ödül verdiği (bağımlılık kurallarına uyarak)
- Her odada ≥ 1 lamba + düğme. Her katta en az 1 döngü (kaçış yolu), çıkmaz koridor sınırlı.
- Possessable eşyalar elle yerleştirilir (kat başına ~10-15 + 1 Anne Terliği).

---

## 10. Puanlama & Maç Sonu ⭐

Motivasyonun son saniyeye kadar kopmaması ve her iki takımın da kısmi başarılarının ödüllendirilmesi için **Kademeli Puanlama Modeli** uygulanır:

| Eylem / Sonuç | Arayıcılar: İnsan + İyi Cin (Kişi Başı) | Kötü Cinler (Kişi Başı) |
|---|---|---|
| **Çözülen Her Bulmaca** | `+50` Puan (`ScorePerPuzzle`) | — |
| **Mahzen Kapağını Açma** | `+50` Puan (`ScoreBasementOpen`) | — |
| **Hazineyi Kazıp Raundu Kazanma** | `+150` Puan (`ScoreTreasureWin`) + `(Kalan Saniye × 1)` | — |
| **İnsanı Her Bayıltma** | — | `+50` Puan (`ScorePerFaint`, max 150) |
| **Raundu Kazanma (Süre veya 3 Bayılma)** | — | `+200` Puan (`JinnWinPoints`) + `(Çözülmemiş Bulmaca × 40)` |

- **Neden çalışır?** Arayıcılar mahzende kaybetse bile çözdükleri bulmacalardan puan alır (0 çekmezler). Kötü cinler ise insanı ne kadar erken aşamada durdurursa (`ScorePerUnfinishedPuzzle` = 40) o kadar yüksek bonus kazanır; "mahzende bekleyelim" tembelliği önlenir.
- 4 raund sonunda en yüksek toplam puanlı oyuncu maçı kazanır (roller döndüğü için puan bireyseldir).
- **Rol rotasyonu:** Raund r: İnsan = oyuncu r, İyi Cin = oyuncu r+1, diğer ikisi kötü cin.

**Raund raporu (8 sn):** Süre, çözülen bulmacalar, bayılma sayısı ve sebepleri ("2. bayılma: Güdümlü Anne Terliği"), yanlış onay sayısı, sabotaj sayısı, patlatılan lamba.
**Maç sonu unvanları:** "Elektrikçi" (en çok yanlış onay), "Bozguncu" (en çok bulmaca sabotajı), "Ampul Katili", "Navigasyon" (en çok işaret), "Tekmeci Dayı" (boşa tekme), "Terlik Nişancısı" (terlikle isabet), "Kolonyacı" (baygın insanı ayıltan / siper olan iyi cin).

---

## 11. Kontroller

### İnsan (FPS)
| Tuş | Eylem |
|---|---|
| WASD / Fare | Hareket / Bakış |
| Shift | Koş |
| E (bas / basılı) | Etkileşim / döndür / ara / kaz / it / radyo |
| F | Tekme |
| Sağ Tık | Fener Parlat (Kutsanmış Yağ alındıktan sonra) |
| 1 / 2 / 3 | Görev eşyası slotu seç |
| G | Seçili eşyayı bırak |
| Tab | Hedefler / skor |

### Cin — Ruh Formu (FPS uçuş)
| Tuş | Eylem |
|---|---|
| WASD / Fare | Uç / Bak |
| Space / Ctrl | Yüksel / Alçal |
| Shift | Hızlı uçuş (Kötü cin iyi cinin içinden geçerse **Ruh Çarpışması** yapar) |
| E (bas / basılı) | **Kötü:** Eşyaya gir / Bulmacayı sabote et (1.5 sn basılı) · **İyi:** Lamba yak/onar |
| Sağ Tık (basılı) | **İyi:** Ruh Görüşü Odakla (Referans perdesini açar) |
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
| İyi cin | **Soluk parıltı** | Kendi | Soluk (Referans okurken parlak) |
| Bulmaca referansı | ❌ | Sağ Tık odaklanınca ✅ | Yeri görünür (içeriği silik) |
| Anahtar yeri / gerçek kazı noktası | ❌ | ✅ | ✅ |
| İşaret (ping) | ✅ | ✅ | ❌ |
| Bulmaca sabote ediliyor | Parçalar döner + yüksek gıcırtı | ✅ | ✅ |

FOV: İnsan 75, ruh formu 85 (renkli vinyet: iyi turkuaz, kötü mor), eşya içi TPS 70.

---

## 13. UI Ekranları

1. **Ana Menü:** Oyna (Online — sonra), Test Oyunu (offline hotseat), Ayarlar, Çıkış
2. **Lobi:** Oda kodu, 4 slot, hazır, Discord takım kanalı hatırlatması
3. **Rol Açıklama (3 sn)**
4. **HUD — İnsan:** Can (3 kalp), bayılma sayacı (☠ 1/3), stamina, Fener durumu + 3 eşya slotu, aşama ilerlemesi (Zemin Kat: ✔ Portreler · ◻ Şalter), gün doğumu göstergesi (+90 sn animasyonu), 3 kademeli kazı barı, etkileşim ipucu
5. **HUD — İyi Cin:** Yetenek bekleme süreleri, enerji, insanın canı/bayılma, aşama ilerlemesi, insanın yönü, referans odaklanma göstergesi
6. **HUD — Kötü Cin:** Enerji, aksiyon kartları, bulmaca sabotaj bekleme süresi, takım arkadaşı, aşama ilerlemesi, ÖFKE
7. **Raund Raporu**, **Maç Sonu**, **Duraklat/Ayarlar**

Dil: Türkçe öncelikli, tüm metinler `Loc` anahtarlı.

---

## 14. Denge — Tek Tablo (`GameBalanceConfig`)

| Config adı | Değer | Not |
|---|---|---|
| `BaseRoundDuration` | 360 | sn (Başlangıç süresi: 6 dk) |
| `StageTimeBonus` | 90 | sn (Her aşama kapısı açıldığında eklenen süre) |
| `JinnWakeDelay` / `IntroCountdown` | 10 / 5 | sn |
| `RoleRevealDuration` / `RoundEndDuration` | 3 / 8 | sn (Rol Açıklama / Raund Sonu raporu) |
| `RoundsPerMatch` | 4 | |
| `StagesActive` | 2 | MVP: 2 aşama (4 bulmaca); tam oyun 3 |
| `PuzzlesPerStage` | 2 | |
| `HumanMaxHp` / `HumanInvulnAfterHit` | 3 / 1.5 | |
| `FaintDuration` / `FaintsToLose` | 12 / 3 | |
| `KnockdownTime` | 2 | sn (raf devrilmesi) |
| `HumanWalkSpeed` / `HumanSprintSpeed` | 3.5 / 5.5 | m/s |
| `StaminaMax` / `StaminaDrain` / `StaminaRegen` / `StaminaRegenDelay` | 100 / 20 / 15 / 1 | |
| `InteractRange` | 2.2 | m |
| `SearchHoldTime` / `PushHoldTime` | 1 / 1.5 | sn |
| `DigTotalTime` / `DigCheckpoints` | 18 / 3 | sn / katman (her 6 sn'de bir güvenli kayıt) |
| `ConfirmFailDamage` / `ConfirmFailCooldown` | 1 / 5 | |
| `KickRange` / `KickCooldown` / `KickStun` | 2 / 6 / 2.5 | |
| `LanternSafeZoneRadius` | 2.5 | m (Kutsanmış Yağ sonrası güçlü fenerin, ruh formunda sabotajı engellediği yarıçap) |
| `LanternPulseRange` / `LanternPulseDuration` / `LanternPulseCooldown` | 8 / 3 / 12 | |
| `LanternPulseAngle` | 30 | ° (Parlat konisinin yarım açısı) |
| `LanternWeakRange` / `LanternStrongRange` | 4 / 12 | m (fener ışığının menzili: yağdan önce / sonra) |
| `ItemSlotCount` | 3 | (Sabit fener hariç görev eşyası slotu) |
| `EvilJinnSpeed` / `GoodJinnSpeed` / `SpiritBoostMult` | 6.5 / 7 / 1.5 | |
| `EnergyMax` / `EnergyStart` / `EnergyRegen` | 100 / 50 / 5 | kötü cin |
| `GoodEnergyMax` / `GoodEnergyRegen` | 100 / 8 | iyi cin |
| `FocusReferenceCost` / `SpiritBumpBlurTime` | 2 / 2 | enerji/sn (referans okuma) / sn (ruh çarpışması bulanıklığı) |
| `SpiritBumpRange` / `SpiritBumpCooldown` | 1.5 / 6 | m (hızlı uçan kötü cinin odaklanan iyi cine çarpma mesafesi) / sn |
| `SpiritBlockEnergyCost` / `SpiritBlockRadius` | 30 / 1 | İyi cinin mahzende uçan eşyayı göğüsleme bedeli / Ruh Siperi çarpışma yarıçapı (m) |
| `CologneReviveRange` | 3 | m (iyi cinin kolonyayla baygın insanı ayıltma mesafesi) |
| `PossessRange` / `PossessTime` / `ReenterCooldown` | 3 / 1.2 / 10 | |
| `LitRoomEjectDelay` | 2 | sn |
| `ScrambleEnergyCost` / `ScrambleHoldTime` / `ScrambleRange` | 25 / 1.5 / 3 | Toplu bulmaca sabotajı değerleri |
| `ScramblePuzzleCooldown` | 15 | sn (Sabote edilen bulmacanın tekrar bozulamama süresi) |
| `LampLightRange` / `RepairHoldTime` | 8 / 2 | Lamba patlatma enerjisi `PD_Lamp` asset'inde (§8: 30) |
| `ExorciseRange` / `ExorciseHoldTime` / `ExorciseCooldown` / `ExorciseStun` / `ExorcisePossessLock` | 3 / 1.5 / 18 / 4 / 4 | |
| `PingDuration` / `PingCooldown` / `PingMaxActive` | 8 / 3 / 2 | |
| `GoodSightSpiritRange` / `GoodSightLurkRange` / `GoodSightChargeRange` | 20 / 4 / 25 | m |
| `KeySenseRange` | 10 | m |
| `ColdBreathRange` | 3 | m |
| `RageRegenMult` / `RageCooldownMult` | 2 / 0.6 | kazı başlayınca |
| `ScorePerPuzzle` / `ScoreBasementOpen` / `ScoreTreasureWin` | 50 / 50 / 150 | Arayıcı kademeli puanları (+ kalan sn) |
| `ScorePerFaint` / `JinnWinPoints` / `ScorePerUnfinishedPuzzle` | 50 / 200 / 40 | Kötü cin kademeli puanları |
| `NetRangeTolerance` | 0.5 | m (otoritenin menzil kontrollerinde tanıdığı tolerans) |

Possessable değerleri §8 tablosundadır ve her birinin `PossessableDefinition` asset'inde tutulur.

---

## 15. Kapsam Dışı (şimdilik)
- Büyü craftı, Kitaplar & Şömine (P5), Yapboz (P6) — MVP sonrası
- Oyun içi sesli sohbet (Discord)
- Kozmetik / ilerleme sistemi, matchmaking (sadece oda kodu), late join
- Bot oyuncular (sadece geliştirme amaçlı "Dummy İnsan")

## 16. Playtest Odak Noktaları (Doğrulanacaklar)
- **Dinamik Süre Dengesi:** 360 sn başlangıç + geçit başına 90 sn bonus, 4 bulmacalı MVP'de ideal 7–8 dakikalık gerilimi sağlıyor mu?
- **Fener Koruması & Sabotaj Döngüsü:** `LanternSafeZoneRadius` (2.5 m) ve 15 saniyelik `ScramblePuzzleCooldown`, kötü cinleri doğrudan eşya fırlatmaya ve ışık patlatmaya yeterince teşvik ediyor mu?
- **Mahzen Finali:** 3 katmanlı (3×6 sn) kazı ve İyi Cin'in `SpiritBlock` (Ruh Siperi) mekaniği, kötü cinlerin ÖFKE modu karşısında adil bir çekişme yaratıyor mu?