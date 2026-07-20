# Personel Izin ve Onay Yonetim Sistemi

Personel Izin ve Onay Yonetim Sistemi, calisanlarin izin taleplerini yonetmek, izin bakiyelerini takip etmek ve yonetici onay sureclerini kontrol etmek icin gelistirilen bir backend API projesidir.

Proje; ASP.NET Core Web API, Entity Framework Core, PostgreSQL ve Clean Architecture prensiplerine uygun katmanli bir yapi ile gelistirilmektedir.

Bu repository, staj projesi kapsaminda fazlara ayrilmis sekilde gelistirilmektedir. Mevcut durumda Faz 1 ve Faz 2 kapsamindaki temel mimari yapi, veritabani altyapisi, CRUD endpointleri, izin bakiyesi hesaplama mantigi, onay akisi ve temel is kurallari tamamlanmistir.

---

## Proje Durumu

| Faz | Kapsam | Durum |
|---|---|---|
| Faz 1 | Temel mimari, entity yapisi, EF Core, migration, seed data ve temel CRUD endpointleri | Tamamlandi |
| Faz 2 | Is kurallari, validasyon, izin bakiyesi, onay akisi ve yonetici yetki kurallari | Tamamlandi |
| Faz 3 | CQRS, MediatR, JWT authentication, authorization ve raporlama | Planlandi |
| Faz 4 | Gelismis testler, final dokumantasyon ve teslim hazirligi | Planlandi |

---

## Faz 1 Kapsaminda Tamamlananlar

- Domain, Application, Infrastructure ve WebAPI katmanlari olusturuldu.
- `Employee`, `Department`, `LeaveType` ve `LeaveRequest` entity'leri tanimlandi.
- `Employee` entity'sinde `ManagerId` ile self-referencing yonetici iliskisi kuruldu.
- Entity Framework Core `DbContext` yapisi olusturuldu.
- PostgreSQL baglantisi yapilandirildi.
- Ilk migration olusturuldu ve basariyla uygulandi.
- Varsayilan `LeaveType` kayitlari migration ile seed data olarak eklendi.
- Local gelistirme ortami icin Docker Compose ile PostgreSQL kurulumu eklendi.
- Employee icin temel CRUD endpointleri yazildi.
- LeaveRequest icin temel CRUD endpointleri yazildi.
- Swagger UI ile API endpointleri test edilebilir hale getirildi.
- Temel integration test projesi olusturuldu.
- Employee ve LeaveRequest icin CRUD integration testleri eklendi.
- Faz 1 endpointleri Swagger UI uzerinden manuel olarak test edildi.

---

## Faz 2 Kapsaminda Tamamlananlar

Faz 2 kapsaminda projeye gercek is kurallari ve onay mantigi eklenmistir.

Tamamlanan basliklar:

- Izin bakiyesi hesaplama mantigi eklendi.
- Yillik hak edis mantigi `EntitledDays`, `UsedDays` ve `RemainingDays` uzerinden temsil edildi.
- Izin bakiyesi yil bazinda hesaplanir hale getirildi.
- Cross-year izin talepleri ilgili yillara bolunerek hesaplanir hale getirildi.
- Kalan izin bakiyesinden fazla talep olusturulmasi engellendi.
- Ayni calisan icin cakisan tarih araligina sahip izin talepleri engellendi.
- Reddedilmis izin taleplerinin yeni talepleri engellememesi saglandi.
- Izin talebi onay akisi uygulandi: `Pending -> Approved / Rejected`.
- Sadece talebi acan calisanin dogrudan yoneticisinin approve/reject islemi yapabilmesi saglandi.
- Employee, Manager ve HR rolleri uzerinden is kurallari uygulandi.
- Employee, HR veya baska bir manager'in baskasinin talebini onaylamasi engellendi.
- Yetki hatalari icin `403 Forbidden`, is kurali hatalari icin `400 Bad Request` donulmesi saglandi.
- Approved durumdaki izin taleplerinin tekrar review/update/delete edilmesi engellendi.
- Var olmayan izin talebi icin approve istegi geldiginde `404 Not Found` donulmesi saglandi.
- Zero allowance leave type icin create ve approve davranisi test edildi.
- Faz 2 icin integration test kapsami genisletildi.

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
  LeaveManagementSystem.IntegrationTests
```

### Katmanlarin Sorumluluklari

| Katman | Sorumluluk |
|---|---|
| Domain | Temel entity'ler, enum'lar ve cekirdek is modeli |
| Application | DTO'lar, servis interface'leri ve application-level exception'lar |
| Infrastructure | EF Core, PostgreSQL, DbContext ve servis implementasyonlari |
| WebAPI | Controller'lar, HTTP endpointleri, Swagger konfigurasyonu ve HTTP response mapping |
| IntegrationTests | API seviyesinde integration testler |

Mevcut request akisi:

```text
Controller
  -> Application service interface
    -> Infrastructure service implementation
      -> AppDbContext
        -> PostgreSQL
```

Bu yapi sayesinde WebAPI katmani dogrudan `AppDbContext` ile calismaz. Controller'lar Application katmanindaki servis interface'leri uzerinden islem yapar.

---

## Kullanilan Teknolojiler

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Docker Compose
- Swagger / Swashbuckle
- xUnit
- Git / GitHub

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

Employee silme islemi su an soft delete olarak uygulanmaktadir. Yani calisan veritabanindan fiziksel olarak silinmez, `IsActive` degeri `false` yapilir.

### Department

Calisanin bagli oldugu departmani temsil eder.

### LeaveType

Izin turunu temsil eder. Varsayilan izin turleri migration ile veritabanina eklenmektedir:

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

Yeni olusturulan izin talepleri varsayilan olarak `Pending` durumunda baslar. `RequestedDays` degeri tarih araligina gore otomatik hesaplanir.

Izin talebi status akisi:

```text
Pending -> Approved
Pending -> Rejected
```

Approved veya Rejected durumuna gecmis talepler tekrar review edilemez. Approved durumdaki talepler update veya delete edilemez.

---

## Is Kurallari Ozeti

### Izin Bakiyesi

Izin bakiyesi yil bazinda hesaplanir.

```text
EntitledDays - UsedDays = RemainingDays
```

- `EntitledDays`: ilgili izin turu icin yillik hak edilen gun sayisi
- `UsedDays`: ilgili yil icinde approved izin gunleri
- `RemainingDays`: kalan izin gunu

Sadece `Approved` durumdaki izin talepleri kullanilmis izin olarak sayilir.

### Cross-Year Izinler

Bir izin talebi birden fazla yila yayiliyorsa, gunler ilgili yillara bolunur.

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

Ayni calisan icin cakisan tarih araligina sahip izin talepleri engellenir.

Rejected durumdaki izin talepleri yeni izin taleplerini engellemez. Boylece reddedilmis bir talep sonrasinda ayni tarih araligi icin yeni talep olusturulabilir.

### Onay Kurali

Bir izin talebi sadece talebi acan calisanin dogrudan yoneticisi tarafindan approve veya reject edilebilir.

Asagidaki kullanicilar approve/reject yapamaz:

- Talebi acan employee
- HR kullanicisi
- Baska bir manager
- Talebi acan calisanin dogrudan yoneticisi olmayan manager

Yetki hatalarinda API `403 Forbidden` doner.

### Lifecycle Kurallari

- Yeni izin talebi `Pending` olarak olusturulur.
- Sadece `Pending` talepler update edilebilir.
- Sadece `Pending` talepler delete edilebilir.
- Sadece `Pending` talepler approve/reject edilebilir.
- Approved talepler tekrar review edilemez.
- Approved talepler update/delete edilemez.

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

### 3. Migration'lari uygulama

```bash
dotnet ef database update --project src/LeaveManagementSystem.Infrastructure --startup-project src/LeaveManagementSystem.WebAPI
```

### 4. Web API'yi calistirma

```bash
dotnet run --project src/LeaveManagementSystem.WebAPI --launch-profile http
```

Uygulama calistiktan sonra Swagger UI asagidaki adresten acilabilir:

```text
http://localhost:5252/swagger
```

Uygulama farkli bir portta baslarsa terminalde gorunen URL kullanilmali ve sonuna `/swagger` eklenmelidir.

---

## API Endpointleri

### Health

| Method | Endpoint | Aciklama |
|---|---|---|
| GET | `/api/health` | API saglik kontrolu |

### Employees

| Method | Endpoint | Aciklama |
|---|---|---|
| GET | `/api/employees` | Tum calisanlari listeler |
| GET | `/api/employees/{id}` | Id'ye gore calisan getirir |
| POST | `/api/employees` | Yeni calisan olusturur |
| PUT | `/api/employees/{id}` | Calisan bilgilerini gunceller |
| DELETE | `/api/employees/{id}` | Calisani pasif hale getirir |

### Leave Requests

| Method | Endpoint | Aciklama |
|---|---|---|
| GET | `/api/leave-requests` | Tum izin taleplerini listeler |
| GET | `/api/leave-requests/{id}` | Id'ye gore izin talebi getirir |
| POST | `/api/leave-requests` | Yeni izin talebi olusturur |
| PUT | `/api/leave-requests/{id}` | Pending durumdaki izin talebini gunceller |
| DELETE | `/api/leave-requests/{id}` | Pending durumdaki izin talebini siler |
| GET | `/api/leave-requests/balance` | Calisanin belirli yil ve izin turu icin izin bakiyesini getirir |
| POST | `/api/leave-requests/{id}/approve` | Izin talebini dogrudan yonetici olarak onaylar |
| POST | `/api/leave-requests/{id}/reject` | Izin talebini dogrudan yonetici olarak reddeder |

Not: Faz 2'de approve/reject endpointleri `reviewerEmployeeId` alani ile test edilmektedir. JWT tabanli kullanici kimligi ve role claim kullanimi Faz 3 kapsaminda eklenecektir.

---

## Ornek Request Body'leri

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

Role degerleri:

```text
1 = Employee
2 = Manager
3 = HR
```

### LeaveRequest Olusturma

```json
{
  "employeeId": "22222222-2222-2222-2222-222222222222",
  "leaveTypeId": "10000000-0000-0000-0000-000000000001",
  "startDate": "2026-07-15",
  "endDate": "2026-07-17",
  "reason": "Annual leave request."
}
```

Not: LeaveRequest olusturmak icin veritabaninda gecerli ve aktif bir `Employee` kaydi bulunmalidir. Varsayilan `LeaveType` kayitlari migration ile otomatik eklenmektedir.

### LeaveRequest Approve

```json
{
  "reviewerEmployeeId": "33333333-3333-3333-3333-333333333333",
  "managerComment": "Approved by direct manager."
}
```

### LeaveRequest Reject

```json
{
  "reviewerEmployeeId": "33333333-3333-3333-3333-333333333333",
  "managerComment": "Rejected by direct manager."
}
```

### Leave Balance Sorgusu

```text
GET /api/leave-requests/balance?employeeId=22222222-2222-2222-2222-222222222222&leaveTypeId=10000000-0000-0000-0000-000000000001&year=2026
```

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

---

## Hata Davranislari

Faz 2 kapsaminda API hata davranislari daha belirgin hale getirilmistir.

| Durum | HTTP Status | Aciklama |
|---|---|---|
| Gecersiz is kurali veya validasyon hatasi | `400 Bad Request` | Ornegin bakiye yetersiz, overlap var veya approved request update edilmeye calisiliyor |
| Yetkisiz review islemi | `403 Forbidden` | Ornegin employee, HR veya direct manager olmayan manager approve/reject yapmaya calisiyor |
| Kaynak bulunamadi | `404 Not Found` | Ornegin var olmayan leave request approve edilmeye calisiliyor |

---

## Testleri Calistirma

Tum testleri calistirmak icin:

```bash
dotnet test
```

Integration testler ayri `leave_management_test_db` veritabani uzerinden calisir. Testler calistirilirken migration'lar bu test veritabanina uygulanir.

Tam local kontrol icin:

```bash
dotnet build
dotnet test
git diff --check
git status
```

Mevcut durumda integration test kapsami:

```text
20 integration tests
0 failed
```

Test edilen ana senaryolar:

- Employee CRUD
- LeaveRequest CRUD
- Overlap rejection
- Balance calculation
- Exceeding remaining balance rejection
- Direct manager approval
- Direct manager rejection
- Non-direct manager forbidden
- Employee approval forbidden
- HR approval forbidden
- Previous-year balance isolation
- Cross-year balance allocation
- Cross-year insufficient balance rejection
- Exact remaining balance approval
- Zero allowance leave type create and approve flow
- Approved request cannot be reviewed again
- Approved request cannot be updated
- Approved request cannot be deleted
- Rejected request does not block new request for same date range
- Approving non-existent leave request returns not found

---

## Gelistirme Workflow'u

Projede branch bazli gelistirme akisi kullanilmaktadir.

```text
main      -> stabil branch
develop   -> aktif gelistirme branch'i
feature/* -> belirli isler icin acilan branch'ler
```

Tipik gelistirme akisi:

```text
develop
  -> feature branch
    -> implementation
    -> build ve test
    -> develop icine merge
```

`main` branch'i her kucuk gelistirme icin kullanilmamaktadir. Stabil bir surum hazir oldugunda veya ozellikle gerekli oldugunda guncellenir.

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

- Leave balance endpointi manuel olarak test edildi.
- Approve endpointi direct manager ile basariyla test edildi.
- Reject endpointi direct manager ile basariyla test edildi.
- Non-direct manager approval denemesi reddedildi.
- Employee approval denemesi reddedildi.
- HR approval denemesi reddedildi.
- Overlap request denemesi `400 Bad Request` ile reddedildi.
- Yetersiz balance senaryosu `400 Bad Request` ile reddedildi.
- Yetki hatalari `403 Forbidden` olarak dogrulandi.
- Cross-year leave balance mantigi integration test ile dogrulandi.
- Approved request lifecycle guard kurallari test edildi.
- Rejected request status-aware overlap davranisi test edildi.
- `dotnet build` basarili calisti.
- `dotnet test` basarili calisti.
- 20 integration test basariyla gecti.
- Git working tree temiz olarak dogrulandi.
