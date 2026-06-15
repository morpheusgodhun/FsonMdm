# FSON MDM

Kurumsal Android cihazları merkezi olarak yöneten, politika uygulayan, kiosk
moduyla kilitleyen ve uzaktan komut çalıştıran tam çalışır bir **Mobile Device
Management (MDM)** sistemi.

Sistem iki parçadan oluşur:

| Parça | Teknoloji | Klasör |
|-------|-----------|--------|
| Backend API | ASP.NET Core 8 · EF Core · SQLite · JWT · Clean Architecture · Multi-Tenant | `backend/` |
| Android Agent (DPC) | Kotlin · DevicePolicyManager · Foreground Service · WorkManager | `android/FsonMdmAgent/` |

> **Kapsam notu:** Bu sistem cihazın **zaten Device Owner olarak hazırlandığını**
> varsayar. Enrollment, QR ve Device Owner kurulumu kapsam dışıdır.

---

## 1. Backend

### Mimari

```
backend/
├─ FsonMdm.sln
└─ src/
   ├─ FsonMdm.Domain          # Entity'ler, enum'lar (bağımsız çekirdek)
   ├─ FsonMdm.Application      # DTO, servis arayüzleri, iş kuralları, JWT sözleşmesi
   ├─ FsonMdm.Infrastructure   # EF Core DbContext, repository'ler, JWT üretimi, migration
   └─ FsonMdm.Api              # Controller'lar, middleware, Program.cs, Swagger
```

Katmanlar tek yönlü bağımlıdır: `Api → Infrastructure → Application → Domain`.
İş mantığı servis katmanındadır; controller'lar incedir.

### Çalıştırma

**Visual Studio ile:** `backend/FsonMdm.sln` dosyasını açın, `FsonMdm.Api`
projesini başlangıç projesi yapıp çalıştırın.

**Komut satırı ile:**

```bash
cd backend/src/FsonMdm.Api
dotnet restore
dotnet run
```

İlk açılışta veritabanı otomatik migrate edilir ve demo verisi tohumlanır
(`Migrate` + `DbSeeder`). SQLite dosyası `fsonmdm.db` olarak oluşur.

Adresler (`launchSettings.json`):

- HTTP: `http://localhost:5080`
- HTTPS: `https://localhost:7080`
- Swagger UI: kök adres (`/`) → `/swagger`

### Demo kimlik bilgileri (DbSeeder)

| Alan | Değer |
|------|-------|
| Tenant | `FSON Demo Tenant` |
| Enrollment Key | `FSON-DEMO-ENROLLMENT-KEY` |
| Admin kullanıcı | `admin` |
| Admin şifre | `Admin123!` |

> **Üretim notu:** `appsettings.json` içindeki `Jwt:Key` değeri yer tutucudur.
> Üretimde en az 32 karakterlik rastgele bir gizli anahtarla değiştirin ve
> `web.config` ortam değişkeni olarak saklayın.

### Kimlik doğrulama modeli

- **Admin** → `POST /api/auth/login` ile kullanıcı adı/şifre vererek admin JWT alır.
- **Cihaz** → `POST /api/device/register` çağrısında `X-Enrollment-Token`
  başlığında tenant enrollment anahtarını gönderir, karşılığında uzun ömürlü
  cihaz JWT'si alır. Sonraki tüm cihaz çağrıları `Authorization: Bearer <token>`
  ile yapılır.
- Her tablo `TenantId` taşır; sorgular JWT claim'inden çözülen tenant ile mantıksal
  olarak izole edilir.

### API uçları

| Metot | Uç | Yetki | Açıklama |
|-------|-----|-------|----------|
| POST | `/api/auth/login` | Anonim | Admin girişi |
| POST | `/api/device/register` | Enrollment başlığı | Cihaz kaydı → cihaz JWT |
| POST | `/api/device/heartbeat` | Cihaz | Canlılık sinyali, LastSeen güncelle |
| GET  | `/api/device/list` | Admin | Tenant cihaz listesi |
| GET  | `/api/policy/{deviceId}` | Cihaz/Admin | Aktif politikayı getir |
| POST | `/api/policy/update` | Admin | Politika oluştur/güncelle (versiyon otomatik artar) |
| POST | `/api/command/create` | Admin | Komut kuyruğa ekle (LOCK·MESSAGE·RESTART) |
| GET  | `/api/command/pending/{deviceId}` | Cihaz/Admin | Bekleyen komutlar |
| POST | `/api/command/ack` | Cihaz | Komut durum güncelle (SENT·DONE) |

`backend/FsonMdm.http` dosyası tüm akışı uçtan uca test eden hazır REST
istekleri içerir (login → register → heartbeat → policy update → policy fetch →
device list → LOCK komutu → poll → ack).

### Politika sözleşmesi (JSON)

```json
{
  "kioskMode": true,
  "blockCamera": true,
  "blockSettings": true,
  "blockPlayStore": true,
  "allowedApps": ["com.android.chrome"]
}
```

---

## 2. Android Agent (DPC)

### Mimari

```
android/FsonMdmAgent/app/src/main/kotlin/com/fson/mdm/
├─ core/         Constants, Prefs (token/deviceId/policy önbelleği)
├─ data/         MdmRepository + remote/ (Retrofit ApiService, ApiClient, AuthInterceptor, DTO'lar)
├─ device/       PolicyEnforcer, KioskManager, CommandExecutor, MdmDeviceAdminReceiver
├─ service/      HeartbeatService (foreground), MdmSyncEngine, PolicyWorker, BootReceiver
├─ permission/   PermissionManager (sistem ayar yönlendirmeleri)
└─ ui/           MainActivity (kontrol paneli), KioskActivity (kilitli mod)
```

### Açma ve çalıştırma

1. **Android Studio** ile `android/FsonMdmAgent` klasörünü açın (Gradle senkronu
   bağımlılıkları indirir).
2. Cihazın/emülatörün **Device Owner** olarak ayarlı olduğundan emin olun
   (kapsam gereği bu adım haricî olarak yapılır). Geliştirme için tipik komut:
   ```
   adb shell dpm set-device-owner com.fson.mdm/.device.MdmDeviceAdminReceiver
   ```
   (Cihazda hesap tanımlı olmamalıdır.)
3. Uygulamayı çalıştırın.

### Sunucu adresi

- Emülatör için varsayılan: `http://10.0.2.2:5080/` (host makinenin loopback'i).
- Fiziksel cihaz için, uygulama açılış ekranındaki **Sunucu adresi** alanına
  PC'nizin LAN IP'sini yazın (ör. `http://192.168.1.20:5080/`).
- HTTP trafiği `network_security_config.xml` ile yalnızca yerel adreslere açıktır.

### Çalışma akışı

1. Uygulama açılır → kontrol panelinde Device Owner durumu ve izinler görünür.
2. **İzinler** bölümünden eksik izinler tek tek verilir (bildirim, pil muafiyeti,
   kullanım erişimi, üzerine çizim).
3. **Cihazı Kaydet** → `register` çağrısı, status **ACTIVE** olur, cihaz JWT'si
   saklanır ve politika hemen çekilip uygulanır.
4. **HeartbeatService** (foreground) açılır; her ~45 sn'de bir döngü çalışır:
   heartbeat → politika çek & uygula → bekleyen komutları çalıştır & ACK'le.
   `PolicyWorker` (WorkManager) servis öldürülürse 15 dk'da bir yeniden ayağa
   kaldırır; `BootReceiver` yeniden başlatmada devreye girer.
5. **Kiosk Modu**: politika `kioskMode=true` ise veya panelden başlatılınca
   uygulama HOME launcher olur, lock-task (tek uygulama) devreye girer ve yalnızca
   `allowedApps` listesindeki uygulamalar açılabilir.

### Politika uygulama eşlemesi

| Politika alanı | DevicePolicyManager karşılığı |
|----------------|-------------------------------|
| `blockCamera` | `setCameraDisabled` |
| `blockPlayStore` | `setApplicationHidden(com.android.vending)` |
| `blockSettings` | `setApplicationHidden(com.android.settings)` |
| `allowedApps` | `setLockTaskPackages` (+ agent) |
| `kioskMode` | `startLockTask` + persistent HOME |

### Komut çalıştırma

| Komut | Davranış |
|-------|----------|
| `LOCK` | `lockNow()` ile ekranı anında kilitler |
| `MESSAGE` | Yüksek öncelikli bildirim olarak yönetici mesajını gösterir |
| `RESTART` | `reboot()` ile cihazı yeniden başlatır (Device Owner) |

Her komut önce **SENT**, başarıyla çalıştırılınca **DONE** olarak ACK'lenir.

---

## 3. MVP Test Senaryosu

1. Backend'i çalıştırın (`dotnet run`).
2. Android uygulamasını emülatörde açın, izinleri verin, **Cihazı Kaydet**.
   → Backend'de cihaz **Active** olur (`GET /api/device/list` ile doğrulayın).
3. Swagger veya `FsonMdm.http` ile admin girişi yapın, `policy/update` ile
   `kioskMode=true, blockCamera=true` gönderin.
4. Uygulamada **Politikayı Güncelle** → kamera kapanır, kiosk modu aktifleşir.
5. `command/create` ile `LOCK` komutu gönderin → bir sonraki döngüde cihaz
   ekranı kilitlenir ve komut **DONE** olarak işaretlenir.

---

## 4. Notlar

- Backend yeni NuGet paketi gerektirmez; yalnızca standart ASP.NET Core / EF Core
  paketleri kullanılır. Şifreler `PBKDF2 (SHA256, 100k iter)` ile saklanır.
- Tüm kullanıcı arayüzü metinleri Türkçe; kod ve veritabanı tanımlayıcıları İngilizce.
- Üretim dağıtımında HTTPS kullanın ve cleartext istisnalarını kaldırın.
