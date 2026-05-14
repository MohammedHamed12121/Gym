# Local Gym Manager - Project Notes

This document records what has been done so far so the project can be continued later without guessing.

## Project Goal

Build a Windows desktop app for a local gym using .NET MAUI. The app is intended to run on one PC at the gym and manage members, subscriptions, attendance, payments, and reports.

## Current Solution Structure

- Solution: `GymSystem.sln`
- Core project: `GymManager.Core/GymManager.Core.csproj`
- Business project: `GymManager.Business/GymManager.Business.csproj`
- MAUI view project: `GymManager/GymManager.MauiView.csproj`
- Target SDK: `.NET 8`, pinned by `global.json` to `8.0.300`
- MAUI target framework: `net8.0-windows10.0.19041.0`
- App title: `إدارة الجيم`
- Local database: SQLite file named `gym-manager.db`

The MAUI project file is named `GymManager.MauiView.csproj`. The physical folder is still `GymManager` because the folder rename was blocked while the project was open/locked by another process. The project name and architecture are already correct.

## Architecture Rules

The solution is split into three layers:

- `GymManager.Core`
  - Holds database-shaped models and enums only.
  - Does not reference Business or MAUI.
  - Current contents:
    - `Models/Member.cs`
    - `Models/PlanType.cs` — Id, Name, Status (Active/NotActive), CreatedAt
    - `Models/Plan.cs` — Id, Name, Description, Price, Period, PlanTypeId, Status (Active/NotActive), CreatedAt
    - `Enums/MembershipPlan.cs`
    - `Enums/MembershipStatus.cs`
    - `Enums/PlanTypeStatus.cs` — Active=1, NotActive=2
    - `Enums/PlanStatus.cs` — Active=1, NotActive=2
    - `Enums/Period.cs` — Daily=1, Monthly=2, Quarter=3, Year=4

- `GymManager.Business`
  - Holds application logic.
  - Holds the current SQLite storage service for app data.
  - References `GymManager.Core`.
  - Does not reference MAUI.
  - Current contents:
    - `Services/MemberDirectoryService.cs`
    - `Services/AttendanceService.cs`
    - `Services/PlanTypeService.cs` — CRUD for plan types, seed data (شهري, ربع سنوي, سنوي, يومي), status toggle
    - `Services/PlanService.cs` — CRUD for plans, status toggle, delete
    - `Models/AddMemberRequest.cs`
    - `Models/DashboardSummary.cs`
    - `Models/MemberDetails.cs`
    - `Models/MemberListItem.cs`
    - `Models/AddPlanTypeRequest.cs`
    - `Models/PlanTypeListItem.cs`
    - `Models/AddPlanRequest.cs`
    - `Models/UpdatePlanRequest.cs`
    - `Models/PlanListItem.cs`

- `GymManager.MauiView`
  - Holds only the app UI and page event handling.
  - References `GymManager.Business`.
  - Does not contain member subscription rules directly.
  - Current main UI files:
    - `App.xaml.cs`
    - `AppShell.xaml`
    - `MemberDetailsPage.xaml`
    - `MemberDetailsPage.xaml.cs`
    - `MainPage.xaml`
    - `MainPage.xaml.cs`
    - `PlanTypePage.xaml` — Add plan type form + list with status toggle buttons
    - `PlanTypePage.xaml.cs`
    - `PlanPage.xaml` — Add/edit plan form + list with update/disable/enable/delete buttons
    - `PlanPage.xaml.cs`

Dependency direction:

```text
GymManager.MauiView -> GymManager.Business -> GymManager.Core
```

## Completed Work

### Initial MAUI App

- Created a .NET MAUI app for Windows.
- Limited the app to one local Windows PC.
- Added the project to `GymSystem.sln`.
- Added `WindowsSdkPackageVersion` because the Windows App SDK required it.
- Retargeted the app to .NET 8 for Visual Studio compatibility.

### Main Screen

- Replaced the default MAUI counter page with a gym dashboard.
- Added dashboard counters for:
  - Active members.
  - Members ending soon.
  - Today's check-ins.
  - Expired subscriptions.
- Added a member form.
- Added an in-memory member list.
- Added search by member name, phone, or subscription plan.
- Added sample members for testing.
- Selecting a member from the list opens the member details page.

### Navigation Shell

- Added a MAUI Shell sidebar/flyout in `AppShell.xaml`.
- The sidebar currently has four items: `الرئيسية`, `سجل الحضور`, `أنواع الخطط`, `الخطط`.
- The sidebar has an Arabic header for the local gym system.
- The Shell itself is not globally right-to-left so the Windows minimize, maximize, and close buttons stay on the right.
- Future pages should be added as new `FlyoutItem` or `ShellContent` entries in `AppShell.xaml`.

### Member Details Page

- Added a dedicated Arabic page for member information.
- Shows member ID, name, phone, plan, start date, end date, registration date, subscription status, and remaining/expired days.
- The page loads data from SQLite through `MemberDirectoryService`.
- The route is registered in `AppShell.xaml.cs`.
- The page includes an `إلغاء العضوية` button.
- Canceling a membership keeps the member record, changes the status to `ملغي`, stores the cancellation date, and hides the cancel button.

### Arabic Support

- Arabic is the default user-facing language.
- App culture is set to `ar-EG`.
- Main content uses right-to-left layout.
- The Windows title bar remains normal so minimize, maximize, and close buttons stay on the right.
- Visible UI text, placeholders, plan names, status names, validation messages, and dates are Arabic.

### Add Member Form

- `رقم العضوية` is generated automatically by the system when adding a member.
- Input text color was fixed so typed text is visible.
- Separate labels were removed where placeholders are enough.
- Form inputs now use clearer background colors and consistent height.
- Plans currently supported:
  - `شهري`
  - `ربع سنوي`
  - `سنوي`

### Architecture Split

- Added `GymManager.Core` for models/enums.
- Added `GymManager.Business` for application logic.
- Renamed the MAUI project file to `GymManager.MauiView.csproj`.
- Wired project references:
  - Business references Core.
  - MauiView references Business.
- Moved member add/search/dashboard logic out of `MainPage.xaml.cs` into `MemberDirectoryService`.
- `MainPage.xaml.cs` now only handles UI events, form clearing, and binding refresh.

### SQLite Storage

- Added `Microsoft.Data.Sqlite` to `GymManager.Business`.
- `MemberDirectoryService` now stores members in a SQLite database instead of an in-memory list.
- The MAUI app passes the database path to the business service:
  - `Path.Combine(FileSystem.AppDataDirectory, "gym-manager.db")`
- The service automatically creates the database folder if needed.
- The service automatically creates the `Members` table on first run.
- Existing databases are migrated automatically with cancellation columns when needed.
- Sample members are inserted only when the table is empty.
- New members are saved permanently and should remain available after closing and reopening the app.
- Member IDs are generated by SQLite and stored in the `Members.Id` primary key, so duplicates are not allowed.
- Searching with a normal ID number, for example `4`, returns only the member whose ID is exactly `4`.
- Phone number search still works for numbers that start with `0`.
- Search also supports member name and subscription plan.
- The local database file is ignored by Git through `.gitignore` using `*.db`, `*.sqlite`, and `*.sqlite3`.

### Members Grid Limit

- **File:** `GymManager.Business/Services/MemberDirectoryService.cs`
- Changed `.Take(100)` to `.Take(30)` in both return paths of `SearchMembers()` to limit the members grid to 30 members.

### Attendance Model

- **File:** `GymManager.Core/Models/Attendance.cs`
- Created with fields: `Id`, `MemberId`, `MemberName`, `AttendDateTime`, `AttendDay`, `AttendMonth`, `AttendYear`, `PlayType`, `Status`.

### Attendance Service

- **File:** `GymManager.Business/Services/AttendanceService.cs`
- Created with operations:
  - `CheckIn(memberId, memberName, playType)` — records attendance with current timestamp
  - `GetTodayAttendance()` — returns all check-ins for today
  - `GetAttendanceByDate(date)` — filter attendance by specific date
  - `GetAttendanceByMember(memberId)` — filter attendance by member
  - `SearchAttendance(searchText)` — search attendance by member name or play type
  - `GetTodayCheckInCount()` — returns today's check-in count for the dashboard
- Uses SQLite with auto-created `Attendance` table, following the same patterns as `MemberDirectoryService`.

### Attendance Summary Model

- **File:** `GymManager.Business/Models/AttendanceSummary.cs`
- Created record with `TodayCount` and `MonthCount`.

### AttendanceService Extended

- Added `GetAttendanceSummary()` — returns today and month counts.
- Added `GetMonthCheckInCount(year, month)` — monthly check-in count.

### Attendance Page

- **Files:** `GymManager/AttendancePage.xaml` and `AttendancePage.xaml.cs`
- New page accessible from the sidebar ("سجل الحضور").
- Shows summary cards: today's attendance count and this month's attendance count.
- Shows a grid of today's attendance records with member name, time, and play type.
- Includes a search bar to filter attendance by member name.

### Check-In Button in Members Grid

- **File:** `GymManager/MainPage.xaml`
- Added a "حضور" button in each member card in the `CollectionView` item template.
- **File:** `GymManager/MainPage.xaml.cs`
- Added `AttendanceService` instance.
- Added `OnCheckInClicked` handler that calls `attendanceService.CheckIn()` with the member's ID, name, and plan.
- Updated dashboard check-in count to use `attendanceService.GetTodayCheckInCount()` instead of hardcoded 0.

### Membership Plans And Statuses

- Plans currently supported:
  - `شهري`
  - `ربع سنوي`
  - `سنوي`
  - `يومي`
- Statuses currently supported:
  - `نشط`
  - `تجديد قريب`
  - `منتهي`
  - `ملغي`

### PlanType Management

- **Files:** `GymManager.Core/Models/PlanType.cs`, `GymManager.Core/Enums/PlanTypeStatus.cs`
- Added `PlanType` entity with fields: Id, Name, Status (Active/NotActive), CreatedAt.
- **Files:** `GymManager.Business/Services/PlanTypeService.cs`, `GymManager.Business/Models/AddPlanTypeRequest.cs`, `GymManager.Business/Models/PlanTypeListItem.cs`
- `PlanTypeService` provides:
  - `AddPlanType(request)` — inserts a new plan type with Active status
  - `GetAllPlanTypes()` — returns all plan types with status display info
  - `SetPlanTypeStatus(id, status)` — toggle between Active/NotActive
  - Seeds 4 default types on first run: شهري, ربع سنوي, سنوي, يومي
- **Files:** `GymManager/PlanTypePage.xaml` and `PlanTypePage.xaml.cs`
- Accessible from sidebar ("أنواع الخطط").
- Left panel: form to add a new plan type (name input + add button).
- Right panel: list of plan types with status badge and toggle button (تعطيل/تفعيل).

### Plan Management

- **Files:** `GymManager.Core/Models/Plan.cs`, `GymManager.Core/Enums/PlanStatus.cs`
- Added `Plan` entity with fields: Id, Name, Description, Price, Period (Daily/Monthly/Quarter/Year), PlanTypeId (FK), Status (Active/NotActive), CreatedAt.
- **Files:** `GymManager.Business/Services/PlanService.cs`, `GymManager.Business/Models/AddPlanRequest.cs`, `GymManager.Business/Models/UpdatePlanRequest.cs`, `GymManager.Business/Models/PlanListItem.cs`
- `PlanService` provides:
  - `AddPlan(request)` — insert new plan (includes Period)
  - `UpdatePlan(request)` — update name, description, price, period, plan type
  - `DeletePlan(id)` — permanently delete a plan
  - `SetPlanStatus(id, status)` — enable/disable a plan
  - `GetAllPlans()` — join with PlanTypes for type name, includes Period name
  - `GetPlanById(id)` — single plan lookup for editing
  - Automatic migration adds `Period` column to existing `Plans` tables
  - Validation for add and update requests
- **Files:** `GymManager/PlanPage.xaml` and `PlanPage.xaml.cs`
- Accessible from sidebar ("الخطط").
- Left panel: form to add or edit a plan (name, description, price, period picker, plan type picker, save/cancel buttons). The form switches to edit mode when "تعديل" is clicked, with a "حفظ التعديلات" button and "إلغاء" button.
- Right panel: list of plans as cards showing name, description, price, period, plan type, status badge, and action buttons: تعديل (populates form for editing), تعطيل/تفعيل (toggles status), حذف (with confirmation dialog).

## Current Limitations

- Attendance check-in is wired into the UI via a button in the members grid.
- Dedicated attendance page available from the sidebar to view daily and monthly records.
- There is no edit/delete member workflow yet.
- There are no reports yet.

## Recommended Next Step

Add member edit/delete workflows and then expand SQLite tables for payments and attendance. The likely next structure is:

- Add database entities/configuration in `GymManager.Core`.
- Keep repository/storage services in `GymManager.Business`.
- Keep `GymManager.MauiView` calling business services only.
