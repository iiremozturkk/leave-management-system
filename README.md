# Personel Izin ve Onay Yonetim Sistemi

Personel Izin ve Onay Yonetim Sistemi; calisanlarin izin taleplerini yonetmek, izin bakiyelerini takip etmek, yonetici onay sureclerini kontrol etmek ve insan kaynaklari raporlamasini desteklemek icin gelistirilen bir backend API projesidir.

Proje; ASP.NET Core Web API, Entity Framework Core, PostgreSQL, Clean Architecture, CQRS/MediatR, FluentValidation ve JWT Bearer Authentication kullanilarak katmanli bir yapi ile gelistirilmektedir.

Bu repository, staj projesi kapsaminda fazlara ayrilmis sekilde gelistirilmektedir. Mevcut durumda Faz 1, Faz 2 ve Faz 3 kapsamindaki temel mimari, veritabani altyapisi, CRUD ve izin yonetimi, CQRS gecisi, JWT authentication, veritabani destekli authorization, takvim sorgusu ve departman izin istatistikleri tamamlanmistir.

---

## Proje Durumu

| Faz | Kapsam | Durum |
|---|---|---|
| Faz 1 | Temel mimari, entity yapisi, EF Core, migration, seed data ve temel CRUD endpointleri | Tamamlandi |
| Faz 2 | Is kurallari, validasyon, izin bakiyesi, onay akisi ve yonetici yetki kurallari | Tamamlandi |
| Faz 3 | CQRS/MediatR, JWT authentication, claim ve role authorization, calendar query ve departman raporlama | Tamamlandi |
| Faz 4 | Final dokumantasyon, teslim hazirligi ve gerekirse secili hardening calismalari | Planlandi |

---

## Faz 1 Kapsaminda Tamamlananlar

- Domain, Application, Infrastructure ve WebAPI katmanlari olusturuldu.
- `Employee`, `Department`, `LeaveType` ve `LeaveRequest` entity'leri tanimlandi.
- `Employee` entity'sinde `ManagerId` ile self-referencing yonetici iliskisi kuruldu.
- Entity Framework Core `AppDbContext` yapisi olusturuldu.
- PostgreSQL baglantisi yapilandirildi.
- Ilk migration olusturuldu ve basariyla uygulandi.
- Varsayilan `LeaveType` kayitlari migration ile seed data olarak eklendi.
- Local gelistirme ortami icin Docker Compose ile PostgreSQL kurulumu eklendi.
- Employee icin temel CRUD endpointleri yazildi.
- LeaveRequest icin temel CRUD endpointleri yazildi.
- Swagger UI ile API endpointleri test edilebilir hale getirildi.
- Temel integration test altyapisi olusturuldu.
- Employee ve LeaveRequest CRUD akislari gercek PostgreSQL uzerinde test edildi.
- Faz 1 endpointleri Swagger UI uzerinden manuel olarak dogrulandi.

---

## Faz 2 Kapsaminda Tamamlananlar

Faz 2 kapsaminda projeye izin yonetimi icin gercek is kurallari, bakiye hesabi ve onay akisi eklenmistir.

Tamamlanan basliklar:

- Izin bakiyesi hesaplama mantigi eklendi.
- Yillik hak edis `EntitledDays`, `UsedDays` ve `RemainingDays` uzerinden temsil edildi.
- Izin bakiyesi yil bazinda hesaplanir hale getirildi.
- Cross-year izin talepleri ilgili yillara bolunerek hesaplanir hale getirildi.
- Yillik hakki sifirdan buyuk izin turlerinde create, update ve approve akislari icin kalan izin bakiyesinin asilmasi engellendi.
- Ayni calisan icin cakisan tarih araligina sahip gecerli izin talepleri engellendi.
- Rejected izin taleplerinin yeni talepleri engellememesi saglandi.
- Izin talebi yasam dongusu uygulandi: `Pending -> Approved / Rejected`.
- Yalniz talebi acan calisanin dogrudan yoneticisinin approve/reject islemi yapabilmesi saglandi.
- Employee, Manager ve HR rolleri icin is kurallari tanimlandi.
- Employee, HR veya direct manager olmayan bir manager'in review yapmasi engellendi.
- Yetki hatalari icin `403 Forbidden`, is kurali hatalari icin `400 Bad Request` donulmesi saglandi.
- Review edilmis izin taleplerinin tekrar approve/reject edilmesi engellendi.
- Yalniz `Pending` taleplerin update ve delete edilebilmesi saglandi.
- Var olmayan izin talebi icin review isteginde `404 Not Found` davranisi korundu.
- Zero allowance leave type davranisi test edildi.
- Faz 2 integration test kapsami genisletildi; ayni davranislar Faz 3 CQRS gecisinde handler unit testleriyle de sabitlendi.

---

## Faz 3 Kapsaminda Tamamlananlar

Faz 3 kapsaminda servis tabanli use-case'ler CQRS/MediatR yapisina tasinmis, JWT tabanli kimlik dogrulama eklenmis ve production endpointleri rol ile kaynak kapsamina gore korunmustur.

Tamamlanan ana basliklar:

- MediatR ve FluentValidation Application katmanina eklendi.
- Ortak `ValidationBehavior` pipeline'i olusturuldu.
- Employee ve LeaveRequest use-case'leri Command/Query handler yapisina tasindi.
- Controller'lar business logic calistirmak yerine `ISender` kullanir hale getirildi.
- Legacy Employee ve LeaveRequest service yollari kontrollu gecis sonrasinda temizlendi.
- Application, Infrastructure ve WebAPI unit test projeleri eklendi.
- `UserAccount` domain modeli ve Employee ile one-to-one iliskisi eklendi.
- Password hashing icin ASP.NET Core Identity `PasswordHasher` altyapisi kullanildi.
- `POST /api/auth/login` CQRS login akisi eklendi.
- JWT issuer, audience, signing key, expiration ve required-claim dogrulamalari eklendi.
- Swagger UI icin Bearer authentication tanimi eklendi.
- Token claim'lerinin guncel veritabani state'i ile tekrar dogrulanmasi saglandi.
- Pasif UserAccount, pasif Employee ve token/DB role uyumsuzlugu engellendi.
- Employee, Manager ve HR icin named authorization policy'leri eklendi.
- Default ve fallback authorization kullanilarak production endpointleri varsayilan olarak korundu.
- Login ve health endpointleri acikca `[AllowAnonymous]` olarak tanimlandi.
- Employee administration endpointleri yalniz HR erisimine acildi.
- Employee ve Manager icin kendi izinlerine yonelik self-service kapsamı, Manager icin active direct report okuma/review kapsami ve HR icin genis okuma/raporlama kapsami uygulandi.
- Scope disi tekil kaynaklarda veri varligini sizdirmamak icin `404 Not Found` yaklasimi uygulandi.
- Approve/reject reviewer identity'si request body yerine authenticated current user'dan turetilir hale getirildi.
- HR'in approve/reject yapmasi engellendi.
- Calendar/date-range sorgusu eklendi.
- Calendar sorgusunda inclusive overlap ve rol bazli gorunurluk uygulandi.
- HR-only departman izin istatistikleri raporu eklendi.
- Departman raporunda PostgreSQL tarafinda `GroupBy`, `Count`, `Sum`, `Average` ve deterministik siralama uygulandi.
- Authentication, authorization, Swagger metadata, calendar ve reporting davranislari unit ve integration testlerle dogrulandi.
- Son feature-branch full-suite sonucu `524/524` basarili olarak dogrulandi.

---

## Mimari Yapi

Proje Clean Architecture prensiplerine uygun katmanli bir yapi kullanmaktadir.

```text
src/
  LeaveManagementSystem.Domain
  LeaveManagementSystem.Application
  LeaveManagementSystem.Infrastructure
  LeaveManagementSystem.WebAPI

tests/
  LeaveManagementSystem.Application.UnitTests
  LeaveManagementSystem.Infrastructure.UnitTests
  LeaveManagementSystem.IntegrationTests
  LeaveManagementSystem.WebAPI.UnitTests
```

### Katmanlarin Sorumluluklari

| Katman | Sorumluluk |
|---|---|
| Domain | Entity'ler, enum'lar ve cekirdek domain davranislari |
| Application | Command/Query, handler, validator, DTO, repository abstraction'lari ve application-level exception'lar |
| Infrastructure | EF Core, PostgreSQL, DbContext, repository implementasyonlari, password hashing ve JWT token generation |
| WebAPI | Controller'lar, authentication/authorization pipeline'i, HTTP endpointleri, exception mapping ve Swagger konfigurasyonu |
| Application.UnitTests | Handler, validator, rule sirasi ve fake repository tabanli hizli unit testler |
| Infrastructure.UnitTests | Password hashing, JWT options validation ve JWT token generation unit testleri |
| WebAPI.UnitTests | Authorization handler ve HTTP result/exception handler unit testleri |
| IntegrationTests | Gercek HTTP pipeline'i, OpenAPI metadata'si ve gercek PostgreSQL sorgulari uzerinden integration testleri |

Guncel CQRS request akisi:

```text
Controller
  -> ISender
    -> Command / Query
      -> Handler
        -> Application repository abstraction
          -> Infrastructure implementation
            -> AppDbContext
              -> PostgreSQL
```

Authentication ve authorization akisi:

```text
Bearer token
  -> Signature / issuer / audience / lifetime validation
    -> Required claim validation
      -> UserAccount ve Employee DB kontrolu
        -> Guncel role ve active-state kontrolu
          -> Named policy / resource scope
            -> Application use-case authorization kontrolu
```

Bu yapi sayesinde WebAPI katmani dogrudan `AppDbContext` ile calismaz. Application katmani EF Core veya Npgsql bilmez; veritabani implementasyonlari Infrastructure katmaninda tutulur.

---

## Kullanilan Teknolojiler

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL
- Npgsql
- Docker Compose
- MediatR
- FluentValidation
- ASP.NET Core Identity PasswordHasher
- JWT Bearer Authentication
- Swagger / Swashbuckle
- xUnit
- WebApplicationFactory
- Git / GitHub
- GitHub Desktop

---

## Domain Model Ozeti

### Employee

Calisan bilgisini temsil eder.

One cikan alanlar:

- `FirstName`
- `LastName`
- `Email`
- `DepartmentId`
- `ManagerId`
- `Role`
- `IsActive`

`ManagerId` alani ile ayni tablo uzerinde self-referencing iliski kurulmustur. Boylece bir calisan baska bir calisanin yoneticisi olabilir.

```text
Employee
  -> Manager
  -> DirectReports
```

Employee delete islemi soft delete olarak uygulanir. Calisan veritabanindan fiziksel olarak silinmez; `IsActive` degeri `false` yapilir.

### Department

Calisanin bagli oldugu departmani temsil eder.

Departman izin istatistikleri, izin talebinin olusturuldugu tarihteki tarihsel snapshot'a gore degil, sorgu aninda calisanin bagli oldugu guncel departmana gore gruplanir.

### LeaveType

Izin turunu temsil eder. Varsayilan izin turleri migration ile veritabanina eklenir:

```text
Annual Leave
Sick Leave
Unpaid Leave
```

One cikan alanlar:

- `Name`
- `DefaultAnnualAllowanceDays`
- `IsPaid`

`DefaultAnnualAllowanceDays`, ilgili izin turu icin varsayilan yillik hak edis gun sayisini temsil eder.

Zero allowance icin guncel davranis:

```text
DefaultAnnualAllowanceDays = 0
-> approved usage sorgusu yapilir
-> insufficient-balance restriction uygulanmaz
-> diger review kurallari uygunsa approval devam edebilir
```

### LeaveRequest

Bir calisanin izin talebini temsil eder.

One cikan alanlar:

- `EmployeeId`
- `LeaveTypeId`
- `StartDate`
- `EndDate`
- `RequestedDays`
- `Status`
- `Reason`
- `ManagerComment`
- `ReviewedAtUtc`
- `ReviewedByEmployeeId`

Yeni olusturulan izin talepleri varsayilan olarak `Pending` durumunda baslar. `RequestedDays` degeri inclusive tarih araligina gore otomatik hesaplanir.

Izin talebi status akisi:

```text
Pending -> Approved
Pending -> Rejected
```

Approved veya Rejected durumuna gecmis talepler tekrar review edilemez. Yalniz `Pending` talepler update veya delete edilebilir.

### UserAccount

JWT login yapabilen kullanici hesabini temsil eder.

One cikan alanlar:

- `EmployeeId`
- `PasswordHash`
- `IsActive`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Kurallar:

- Her UserAccount bir Employee'ye baglidir.
- Employee basina en fazla bir UserAccount bulunur.
- Plain-text password veritabaninda saklanmaz.
- UserAccount ve bagli Employee aktif degilse login veya protected endpoint erisimi verilmez.
- Token role claim'i, guncel Employee role degeri ile uyusmalidir.

---

## Is Kurallari Ozeti

### Izin Bakiyesi

Izin bakiyesi yil bazinda hesaplanir.

```text
EntitledDays - UsedDays = RemainingDays
```

- `EntitledDays`: Ilgili izin turu icin yillik hak edilen gun sayisi
- `UsedDays`: Ilgili yil icinde approved izin gunleri
- `RemainingDays`: Kalan izin gunu

Yalniz `Approved` durumdaki izin talepleri kullanilmis izin olarak sayilir.

Approve sirasinda incelenen mevcut talep, approved usage hesabinda tekrar sayilmaz.

### Cross-Year Izinler

Bir izin talebi birden fazla yila yayiliyorsa gunler ilgili yillara bolunur.

Ornek:

```text
2026-12-30 -> 2027-01-02
```

Bu talep toplam 4 gun surer:

```text
2026: 2 gun
2027: 2 gun
```

Balance kontrolu her yil icin ayri yapilir.

### Overlap Kontrolu

Ayni calisan icin cakisan tarih araligina sahip gecerli izin talepleri engellenir.

Rejected durumdaki izin talepleri yeni izin taleplerini engellemez. Boylece reddedilmis bir talep sonrasinda ayni tarih araligi icin yeni talep olusturulabilir.

Calendar sorgusunda overlap inclusive olarak hesaplanir:

```text
LeaveRequest.StartDate <= queryEnd
&& queryStart <= LeaveRequest.EndDate
```

### Onay Kurali

Bir izin talebi yalniz authenticated current user olan ve talebi acan calisanin guncel dogrudan yoneticisi konumundaki Manager tarafindan approve veya reject edilebilir.

Reviewer identity istemciden alinmaz:

```text
JWT current user
-> current EmployeeId
-> Approve / Reject command
-> current direct-manager kontrolu
```

Asagidaki kullanicilar approve/reject yapamaz:

- Talebi acan Employee
- HR kullanicisi
- Baska bir Manager
- Talebi acan calisanin guncel direct manager'i olmayan Manager
- Pasif UserAccount veya pasif Employee
- Token role claim'i guncel DB role ile uyusmayan kullanici

### Authorization Kapsami

| Rol | Temel erisim kapsami |
|---|---|
| Employee | Kendi izin kayitlari ve self-service islemleri |
| Manager | Kendi self-service islemleri; guncel ve aktif direct report kayitlarini okuma ve review |
| HR | Kendi self-service islemleri; Employee administration, genis leave read kapsami ve raporlama |

Ek kurallar:

- Employee administration endpointleri HR-only'dir.
- Manager'in genel collection ve calendar scope'una kendi izin kayitlari otomatik olarak dahil edilmez; bu akislarda yalniz guncel ve aktif direct report kayitlari doner.
- Manager kendi izin kayitlarina `/api/leave-requests/mine`, kendi kaydi icin by-id ve self-service endpointleri uzerinden erisebilir.
- Pasif direct report, Manager collection ve calendar scope'una dahil edilmez.
- HR genis okuma ve raporlama yapabilir ancak approve/reject yapamaz.
- Scope disi tekil kaynaklar, kaydin varligini sizdirmamak icin `404 Not Found` donebilir.
- Token claim'leri tek basina authoritative degildir; guncel DB state'i kontrol edilir.

### Employee Administration Kurallari

- Employee administration yalniz guncel ve aktif HR kullanicilari tarafindan yapilabilir.
- Manager olarak atanacak Employee mevcut, aktif ve `Manager` rolunde olmalidir.
- Bir Employee kendisinin manager'i olarak atanamaz.
- Manager hiyerarsisinde cycle olusturulamaz.
- Aktif direct report'u bulunan bir Manager pasif hale getirilemez veya baska bir role gecirilemez.
- Aktif direct report'u bulunan bir Employee soft delete ile pasif hale getirilemez.
- Aktif UserAccount'a bagli son aktif HR administrator pasif hale getirilemez veya HR disinda bir role gecirilemez.

### Lifecycle Kurallari

- Yeni izin talebi `Pending` olarak olusturulur.
- Yalniz `Pending` talepler update edilebilir.
- Yalniz `Pending` talepler delete edilebilir.
- Yalniz `Pending` talepler approve/reject edilebilir.
- Approved veya Rejected talepler tekrar review edilemez.
- LeaveRequest delete mevcut kontratta `Pending` kayit icin fiziksel silmedir.
- Employee delete soft delete olarak uygulanir.

### Departman Izin Istatistikleri

Departman raporu yalniz `Approved` izin taleplerini hesaplar.

Her departman icin:

```text
ApprovedRequestCount
TotalApprovedLeaveDays
AverageApprovedLeaveDaysPerRequest
```

uretilir.

Kurallar:

- Pending ve Rejected talepler hesaba katilmaz.
- Gruplama, calisanin sorgu anindaki guncel departmanina gore yapilir.
- Onayli talebi olmayan departmanlar response'ta listelenmez.
- Sonuclar `DepartmentName`, ardindan `DepartmentId` ile deterministik siralanir.
- Filtreleme, grouping, `Count`, `Sum`, `Average` ve siralama PostgreSQL tarafinda calisir.
- PostgreSQL'den yalniz departman bazli aggregate sonuc satirlari alinir; ham `LeaveRequest` kayitlari bellekte gruplanmaz.
- Veritabani tarafinda hesaplanan average sonucu, materialization sonrasinda DTO'nun `decimal` alanina donusturulur.
- Provider-specific sorgu davranisi gercek PostgreSQL integration testiyle dogrulanmistir.

---

## Gereksinimler

Projeyi calistirmak icin asagidaki araclarin kurulu olmasi gerekir:

- .NET 10 SDK
- Docker Desktop
- Git
- DBeaver, pgAdmin veya benzeri bir veritabani araci

Entity Framework CLI yuklu degilse asagidaki komut ile kurulabilir:

```bash
dotnet tool install --global dotnet-ef
```

JWT icin asagidaki configuration alanlari gereklidir:

```text
Jwt:Issuer
Jwt:Audience
Jwt:SigningKey
Jwt:AccessTokenExpirationMinutes
```

`Jwt:SigningKey` en az 32 UTF-8 byte olmalidir. `Jwt:AccessTokenExpirationMinutes` degeri 1 ile 1440 arasinda olmalidir.

Gercek production signing key repository'ye veya README'ye yazilmamalidir. Environment variable, user secrets veya guvenli configuration kullanilmalidir.

Environment variable adlari ASP.NET Core configuration formatinda su sekilde verilebilir:

```text
Jwt__Issuer
Jwt__Audience
Jwt__SigningKey
Jwt__AccessTokenExpirationMinutes
```

---

## Projeyi Calistirma

Asagidaki komutlar repository ana dizininde calistirilmalidir.

### 1. Repository'yi klonlama

```bash
git clone <repository-url>
cd leave-management-system
```

### 2. PostgreSQL'i Docker Compose ile baslatma

```bash
docker compose up -d
```

Local PostgreSQL baglanti bilgileri:

```text
Host: localhost
Port: 5432
Database: leave_management_db
Username: postgres
Password: postgres
```

Bu bilgiler yalniz local development ornegidir. Production ortaminda guvenli secret/configuration kullanilmalidir.

### 3. Migration'lari uygulama

```bash
dotnet ef database update --project src/LeaveManagementSystem.Infrastructure --startup-project src/LeaveManagementSystem.WebAPI
```

Migration'lar temel tablolarla birlikte authentication icin gerekli `UserAccounts` tablosunu da olusturur.

### 4. JWT configuration'i ayarlama

Development ortaminda gerekli JWT alanlari environment variable veya user secrets ile tanimlanmalidir.

Ornek configuration yapisi:

```json
{
  "Jwt": {
    "Issuer": "LeaveManagementSystem",
    "Audience": "LeaveManagementSystem.Client",
    "SigningKey": "<en-az-32-byte-guclu-ve-gizli-signing-key>",
    "AccessTokenExpirationMinutes": 60
  }
}
```

Ornekteki signing key gercek bir production credential degildir.

### 5. Web API'yi calistirma

```bash
dotnet run --project src/LeaveManagementSystem.WebAPI --launch-profile http
```

Uygulama calistiktan sonra Swagger UI asagidaki adresten acilabilir:

```text
http://localhost:5252/swagger
```

Uygulama farkli bir portta baslarsa terminalde gorunen URL kullanilmali ve sonuna `/swagger` eklenmelidir.

### 6. Ilk UserAccount notu

Login yapabilmek icin veritabaninda:

- Aktif bir `Employee`
- Bu Employee'ye bagli aktif bir `UserAccount`
- Gecerli `PasswordHash`

bulunmalidir.

Plain-text password veya production JWT secret seed edilmez. Ilk kullanici/bootstrap islemi development ya da deployment ortaminda kontrollu olarak yapilmalidir.

---

## Authentication ve Swagger Kullanimi

### Login

```http
POST /api/auth/login
```

Request body:

```json
{
  "email": "hr@example.com",
  "password": "<password>"
}
```

Basarili response alanlari:

```json
{
  "accessToken": "<jwt-access-token>",
  "expiresAtUtc": "2026-08-06T18:00:00Z",
  "userAccountId": "11111111-1111-1111-1111-111111111111",
  "employeeId": "22222222-2222-2222-2222-222222222222",
  "email": "hr@example.com",
  "role": 3
}
```

Role degerleri:

```text
1 = Employee
2 = Manager
3 = HR
```

Email bulunamamasi, password'un yanlis olmasi, UserAccount'in pasif olmasi veya Employee'nin pasif olmasi durumlarinda public response ayni genel `401 Unauthorized` kontratini kullanir.

### Swagger Authorize

1. `/api/auth/login` endpoint'i ile token alin.
2. Swagger UI icindeki `Authorize` butonunu acin.
3. Access token'i Bearer authentication alanina girin.
4. Protected endpointleri kullanici rolune ve resource scope'una gore test edin.

HTTP header ornegi:

```text
Authorization: Bearer <jwt-access-token>
```

---

## API Endpointleri

### Health ve Authentication

| Method | Endpoint | Erisim | Aciklama |
|---|---|---|---|
| GET | `/api/health` | Anonymous | API saglik kontrolu |
| POST | `/api/auth/login` | Anonymous | Email ve password ile JWT access token uretir |

### Employees

| Method | Endpoint | Erisim | Aciklama |
|---|---|---|---|
| GET | `/api/employees` | HR | Calisanlari listeler |
| GET | `/api/employees/{id}` | HR | Id'ye gore calisan getirir |
| POST | `/api/employees` | HR | Yeni calisan olusturur |
| PUT | `/api/employees/{id}` | HR | Calisan bilgilerini gunceller |
| DELETE | `/api/employees/{id}` | HR | Calisani soft delete ile pasif hale getirir |

### Leave Requests

| Method | Endpoint | Erisim | Aciklama |
|---|---|---|---|
| GET | `/api/leave-requests` | Authenticated, role scope | Employee/Manager/HR icin yetkili koleksiyon kapsamini dondurur |
| GET | `/api/leave-requests/mine` | Authenticated | Current employee'nin kendi izin taleplerini dondurur |
| GET | `/api/leave-requests/calendar` | Authenticated, role scope | Tarih araligiyla kesisen izin taleplerini takvim response'u olarak dondurur |
| GET | `/api/leave-requests/{id}` | Authenticated, resource scope | Id'ye gore yetkili izin talebini getirir |
| POST | `/api/leave-requests` | Authenticated, self-service scope | Yeni izin talebi olusturur |
| PUT | `/api/leave-requests/{id}` | Authenticated, ownership scope | Pending izin talebini gunceller |
| DELETE | `/api/leave-requests/{id}` | Authenticated, ownership scope | Pending izin talebini siler |
| GET | `/api/leave-requests/balance` | Authenticated, self-service scope | Current employee'nin belirli yil ve izin turu icin izin bakiyesini getirir |
| POST | `/api/leave-requests/{id}/approve` | Manager, direct-report scope | Izin talebini authenticated direct manager olarak onaylar |
| POST | `/api/leave-requests/{id}/reject` | Manager, direct-report scope | Izin talebini authenticated direct manager olarak reddeder |

### Reports

| Method | Endpoint | Erisim | Aciklama |
|---|---|---|---|
| GET | `/api/reports/department-leave-statistics` | HR | Approved izinleri guncel departmana gore gruplayan istatistik raporunu dondurur |

---

## Ornek Request ve Response'lar

### Employee Olusturma

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "departmentId": "11111111-1111-1111-1111-111111111111",
  "managerId": null,
  "role": 1
}
```

Employee administration endpointleri yalniz HR tarafindan kullanilabilir.

### LeaveRequest Olusturma

```json
{
  "leaveTypeId": "10000000-0000-0000-0000-000000000001",
  "startDate": "2026-07-15",
  "endDate": "2026-07-17",
  "reason": "Annual leave request."
}
```

`employeeId` request body'den alinmaz. Izin talebinin sahibi, dogrulanmis JWT uzerinden bulunan current Employee'dir. UserAccount ve bagli Employee aktif olmalidir. Varsayilan LeaveType kayitlari migration ile otomatik eklenir.

### LeaveRequest Approve

Reviewer identity request body'den alinmaz. Authenticated Manager'in current Employee ID'si kullanilir.

```json
{
  "managerComment": "Approved by direct manager."
}
```

### LeaveRequest Reject

```json
{
  "managerComment": "Rejected by direct manager."
}
```

### Leave Balance Sorgusu

```http
GET /api/leave-requests/balance?leaveTypeId=10000000-0000-0000-0000-000000000001&year=2026
```

`employeeId` query parameter olarak alinmaz. Bakiye, dogrulanmis JWT uzerinden bulunan current Employee icin hesaplanir.

Ornek response:

```json
{
  "employeeId": "22222222-2222-2222-2222-222222222222",
  "leaveTypeId": "10000000-0000-0000-0000-000000000001",
  "leaveTypeName": "Annual Leave",
  "year": 2026,
  "entitledDays": 20,
  "usedDays": 5,
  "remainingDays": 15
}
```

### Calendar Sorgusu

```http
GET /api/leave-requests/calendar?startDate=2026-08-01&endDate=2026-08-31
```

Kurallar:

- Baslangic ve bitis tarihleri inclusive'dir.
- Ters tarih araligi `400 Bad Request` ile reddedilir.
- Employee yalniz kendi kayitlarini gorur.
- Manager yalniz guncel ve aktif direct report kayitlarini gorur.
- HR tarih araligiyla kesisen genis kayit kapsamini gorebilir.
- Scope filtreleri PostgreSQL sorgusunda uygulanir.

### Departman Izin Istatistikleri

```http
GET /api/reports/department-leave-statistics
```

Ornek response:

```json
[
  {
    "departmentId": "33333333-3333-3333-3333-333333333333",
    "departmentName": "Engineering",
    "approvedRequestCount": 2,
    "totalApprovedLeaveDays": 6,
    "averageApprovedLeaveDaysPerRequest": 3
  }
]
```

Ornek GUID yalniz dokumantasyon verisidir.

Rapor kurallari:

- Yalniz HR erisebilir.
- Endpoint parametre almaz.
- Yalniz Approved talepler hesaplanir.
- Pending ve Rejected talepler dahil edilmez.
- Gruplama Employee'nin sorgu anindaki guncel departmanina gore yapilir.
- Onayli talebi bulunmayan departmanlar listelenmez.

---

## Hata Davranislari

| Durum | HTTP Status | Aciklama |
|---|---|---|
| FluentValidation veya is kurali hatasi | `400 Bad Request` | Ornegin gecersiz tarih araligi, bakiye yetersizligi, overlap veya gecersiz lifecycle islemi |
| Token yok veya token gecersiz | `401 Unauthorized` | Authentication basarisiz |
| Gecerli token fakat role, policy veya active-state kontrolu basarisiz | `403 Forbidden` | Ornegin HR review denemesi, inactive account veya token/DB role uyumsuzlugu |
| Kaynak bulunamadi | `404 Not Found` | Istenen kayit gercekten mevcut degil |
| Kaynak kullanicinin resource scope'u disinda | `404 Not Found` | Kaydin varligini yetkisiz kullaniciya sizdirmamak icin |
| Login bilgileri gecersiz veya hesap kullanilamaz durumda | `401 Unauthorized` | Email/password/account durumu ayrintisi public response'ta aciklanmaz |

API, validation ve business-rule hatalarinda standart ProblemDetails tabanli response'lar kullanir.

---

## Testleri Calistirma

Tum solution testlerini calistirmak icin:

```bash
dotnet test
```

Restore islemi daha once tamamlandiysa:

```bash
dotnet test --no-restore
```

Integration testler ayri `leave_management_test_db` PostgreSQL veritabani uzerinden calisir. Testler sirasinda gerekli migration'lar test veritabanina uygulanir.

Tam local kontrol icin:

```bash
dotnet build --no-restore
dotnet test --no-restore
git diff --check
git status
```

Son kesin dogrulanan full-suite sonucu:

```text
524 total
524 succeeded
0 failed
0 skipped
Build succeeded
git diff --check clean
working tree clean
```

Test turleri:

- Application handler ve validator unit testleri
- Domain ve business-order testleri
- Infrastructure password hashing, JWT options validation ve token generation unit testleri
- WebAPI authorization/result/exception handler unit testleri
- Gercek HTTP pipeline integration testleri
- Gercek PostgreSQL repository ve aggregate sorgu testleri
- JWT login ve bearer validation testleri
- Employee/Manager/HR authorization matrix testleri
- Calendar/date-range scope testleri
- Department reporting aggregate testleri
- Swagger/OpenAPI authentication ve security metadata testleri

Test edilen ana senaryolardan bazilari:

- Employee ve LeaveRequest CRUD
- Validation pipeline ve ProblemDetails
- Overlap rejection
- Balance calculation
- Cross-year balance allocation
- Remaining balance kontrolu
- Direct manager approval ve rejection
- Reviewer identity'nin claims/current-user'dan turetilmesi
- Non-direct manager, Employee ve HR review yasaklari
- UserAccount ve Employee active-state kontrolleri
- Token/DB role mismatch
- Missing/invalid token davranisi
- Employee own scope
- Manager active direct-report scope
- HR genis read ve reporting scope'u
- Scope disi tekil resource icin 404
- Default ve fallback authorization
- Inclusive calendar overlap
- Gecersiz calendar tarih araligi
- Approved-only department grouping
- Department count, total ve average hesaplari
- Onayli talebi olmayan departmanin rapordan dislanmasi
- Swagger Bearer security metadata'si
- EF Core/Npgsql sorgularinin gercek PostgreSQL tarafinda calismasi

---

## Gelistirme Workflow'u

Projede branch bazli gelistirme akisi kullanilir.

```text
main      -> stabil branch
develop   -> tamamlanan feature'larin birlestigi gelistirme branch'i
feature/* -> belirli kapsam icin acilan branch'ler
```

Tipik gelistirme akisi:

```text
develop
  -> feature branch
    -> implementation
      -> targeted build/test
        -> full build/test
          -> git diff/status kontrolu
            -> commit ve push
              -> develop icine merge
```

Temel kurallar:

- Basarili build ve test sonucu gorulmeden commit atilmaz.
- `git diff --check` temiz olmadan commit atilmaz.
- Untracked dosyalar dahil degisen dosya kapsami kontrol edilir.
- Her commit tek bir anlamli konuyu kapsar.
- Ilgisiz refactor ayni feature commit'ine eklenmez.
- Migration yalniz entity/configuration/schema degisikliginde olusturulur.
- Provider-specific EF Core sorgulari gercek PostgreSQL integration testiyle dogrulanir.
- `main` her kucuk gelistirme icin kullanilmaz; stabil surum hazirliginda guncellenir.

---

## Faz 1 Dogrulama

Faz 1 kapsaminda asagidaki kontroller tamamlanmistir:

- Employee CRUD endpointleri Swagger UI uzerinden manuel olarak test edildi.
- LeaveRequest CRUD endpointleri Swagger UI uzerinden manuel olarak test edildi.
- Employee ve LeaveRequest CRUD integration testleri basariyla calisti.
- PostgreSQL baglantisi dogrulandi.
- Initial migration basariyla uygulandi.
- Varsayilan LeaveType seed data migration ile eklendi.
- Testler ayri `leave_management_test_db` veritabani uzerinden calistirildi.
- Local test verileri manuel testlerden sonra temizlendi.
- `dotnet build` basarili calisti.
- `dotnet test` basarili calisti.
- `git diff --check` temiz sonuc verdi.
- Git working tree temiz olarak dogrulandi.

---

## Faz 2 Dogrulama

Faz 2 kapsaminda asagidaki kontroller tamamlanmistir:

- Leave balance endpointi test edildi.
- Approve ve reject endpointleri direct manager ile dogrulandi.
- Non-direct manager, Employee ve HR review denemeleri reddedildi.
- Overlap senaryosu `400 Bad Request` ile reddedildi.
- Yetersiz balance senaryosu `400 Bad Request` ile reddedildi.
- Yetki hatalari `403 Forbidden` olarak dogrulandi.
- Cross-year leave balance mantigi integration test ile dogrulandi.
- Approved/Rejected lifecycle guard kurallari test edildi.
- Rejected request status-aware overlap davranisi test edildi.
- Zero allowance davranisi test edildi.
- CQRS'e tasinmadan onceki urun davranislari testlerle sabitlendi.
- Faz 2 davranislari Faz 3 CQRS gecisinden sonra da regresyon testleriyle korundu.

---

## Faz 3 Dogrulama

Faz 3 kapsaminda asagidaki kontroller tamamlanmistir:

- Employee ve LeaveRequest use-case'lerinin CQRS/MediatR uzerinden calistigi dogrulandi.
- Controller'larin `ISender` kullandigi ve legacy service yollarinin temizlendigi dogrulandi.
- FluentValidation pipeline'i unit ve HTTP integration testleriyle dogrulandi.
- Login endpointinin JWT access token ve role bilgisi dondurdugu dogrulandi.
- Infrastructure unit testleriyle password hashing, verification, rehash-needed, JWT options validation ve token generation akislari test edildi.
- JWT signature, issuer, audience, lifetime ve required claim kontrolleri test edildi.
- Tokensiz veya gecersiz token ile protected endpoint erisimi engellendi.
- UserAccount/Employee active-state ve token/DB role uyumu dogrulandi.
- Employee, Manager ve HR authorization policy'leri test edildi.
- Reviewer identity'nin request body yerine claims/current-user'dan turetildigi dogrulandi.
- Default/fallback authorization ve `[AllowAnonymous]` metadata'si test edildi.
- Calendar sorgusunun inclusive overlap ve role-scope davranislari test edildi.
- Department raporunun Approved-only grouping, count, total, average ve siralama davranislari gercek PostgreSQL ile dogrulandi.
- Swagger Bearer security metadata'si test edildi.
- Son full-suite sonucu `524/524` basarili olarak dogrulandi.
- `git diff --check` temiz sonuc verdi.
- Department reporting final commit'i sonrasinda feature branch'in remote ile esit ve working tree'nin temiz oldugu dogrulandi.

---

## Bilinen Kapsam Sinirlari ve Sonraki Adimlar

Faz 3 kapsaminda bilincli olarak eklenmeyenler:

- Department report icin tarih veya yil filtresi
- LeaveType bazli raporlama
- Calisan detay raporu
- Tarihsel departman snapshot sistemi
- Dashboard veya grafik
- CSV, Excel veya PDF export
- Pagination
- Cache
- Audit log
- Notification sistemi

Gerçekleştirilmesi planlanan urun ve teknik kararlar:

- Ilk UserAccount/bootstrap akisi deployment seviyesinde ayrica netlestirilmelidir.
- Production JWT signing key repository disinda guvenli tutulmalidir.
- Token claim'leri tek basina authoritative yapilmamalidir; guncel DB state kontrolu korunmalidir.
- Reviewer EmployeeId tekrar public request body'ye eklenmemelidir.
- Calendar tarihleri `DateOnly` ve inclusive overlap mantigini korumalidir.
- Departman raporu mevcut calisan departmanina gore grouping yapar; tarihsel raporlama ayri feature'dir.
- Ortalama icin belirli ondalik basamak veya rounding kuralina ihtiyac duyulursa acik urun karari ve test eklenmelidir.
- Employee delete soft delete, Pending LeaveRequest delete ise mevcut kontratta fiziksel delete'tir.
- LeaveRequest audit veya soft-delete ihtiyaci ayri hardening ozelligi olarak ele alinmalidir.
- Manager hierarchy ve cycle kurallari yeni degisikliklerde korunmalidir.
- Migration yalniz gercek schema degisikliginde olusturulmalidir.
