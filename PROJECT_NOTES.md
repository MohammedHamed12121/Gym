# Local Gym Manager - Project Notes

This document records what has been done so far so the project can be continued later without guessing.

## Project Goal

Build a Windows desktop app for a local gym using .NET MAUI. The app is intended to run on one PC at the gym and manage members, subscriptions, attendance, payments, and reports.

## Current Structure

- Solution: `GymSystem.sln`
- Main app project: `GymManager/GymManager.csproj`
- Target framework: `.NET 8` Windows, `net8.0-windows10.0.19041.0`
- SDK pinned by `global.json` to `8.0.300`

## Completed Work

### Initial MAUI App

- Created a .NET MAUI app named `GymManager`.
- Limited the app to Windows because it will run on one local PC.
- Added the project to `GymSystem.sln`.
- Added `WindowsSdkPackageVersion` because the Windows App SDK required it.

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

### Arabic Support

- Arabic is now the default user-facing language.
- App culture is set to `ar-EG`.
- Main content uses right-to-left layout.
- The Windows title bar remains normal so minimize, maximize, and close buttons stay on the right.
- Visible UI text, placeholders, plan names, status names, validation messages, and dates were translated to Arabic.

### Add Member Form

- Input text color was fixed so typed text is visible.
- Separate labels were removed where placeholders are enough.
- Form inputs now use clearer background colors and consistent height.
- Plans currently supported:
  - `شهري`
  - `ربع سنوي`
  - `سنوي`

## Important Files

- `GymManager/App.xaml.cs`: sets Arabic culture.
- `GymManager/AppShell.xaml`: app shell and title.
- `GymManager/MainPage.xaml`: main Arabic dashboard UI.
- `GymManager/MainPage.xaml.cs`: temporary in-memory member logic.
- `GymManager/DEVELOPMENT_PLAN.md`: high-level development plan.
- `global.json`: pins the .NET SDK used by the project.

## Current Limitations

- Members are not saved permanently yet. Data is only in memory.
- Attendance and payments are only represented in the dashboard, not implemented yet.
- There is no SQLite database yet.
- There is no edit/delete member workflow yet.
- There are no reports yet.

## Recommended Next Step

Add SQLite local storage so members are saved permanently on the gym PC. After that, move the in-memory member logic from `MainPage.xaml.cs` into models and services.
