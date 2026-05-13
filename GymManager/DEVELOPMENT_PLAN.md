# Local Gym Manager Development Plan

This app is for one local Windows PC in a gym reception/admin desk. Build it in small working slices so each step can be tested before adding the next one.

## Step 1 - Project Foundation

- Create a .NET MAUI Windows app.
- Replace the default sample screen with a gym dashboard.
- Add an in-memory member registration form and searchable member list.
- Add Shell sidebar navigation with the main page. Done.

## Step 2 - Local Database

- Keep the architecture split clean: `GymManager.MauiView -> GymManager.Business -> GymManager.Core`.
- Add SQLite storage on the PC. Done for members.
- Save members permanently. Done for added members.
- Generate member IDs automatically and search by exact member ID. Done.
- Add member details page. Done.
- Add one-day membership plan. Done.
- Cancel memberships without deleting member records. Done.
- Add backup/export support so the gym owner can copy data safely.

## Step 3 - Memberships And Payments

- Track subscription plans, start dates, end dates, paid amounts, discounts, and remaining balances.
- Show members who need renewal.
- Print or export payment receipts.

## Step 4 - Attendance

- Add daily check-in.
- Search by name or phone at reception.
- Show whether a member can enter based on subscription status.

## Step 5 - Reports

- Daily income.
- Monthly renewals.
- Active and expired members.
- Attendance history.

## Step 6 - Polish And Deployment

- Add settings for gym name, plans, prices, and backup folder.
- Package the Windows app for the gym PC.
- Add basic admin protection if needed.
