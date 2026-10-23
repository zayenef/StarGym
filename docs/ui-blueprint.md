# Gym App UI Blueprint

## Main screens

### 1. Dashboard
Purpose: quick owner overview.

Widgets:
- Total Members
- Active Members
- Expired Members
- Pending Payments
- Monthly Revenue
- Members currently in the gym
- Today's total check-ins
- Monthly attendance
- Recent activity list

### 2. Members
Purpose: manage all members.

Features:
- Search by name or phone
- Filter by status
- Show all members by default with a live membership badge: green for current subscriptions, red for expired subscriptions, and the expiry date
- Show each member's saved photo beside their directory entry, with initials as a fallback
- Add a new client with an active membership plan
- Capture a required client photo from a webcam when creating a new client; store it locally for future badge check-in
- Change a member's photo while editing their profile
- Renew a selected client's membership using an active plan
- Optionally record collected membership payments
- Edit client details
- Permanently remove a client and their payment/attendance history after confirmation
- View member details
- Membership expiry indicator

### 3. Reports
Purpose: business reporting.

Reports:
- Monthly revenue report
- Membership expiry report
- Attendance report
- Payment status summary

### 4. Badge Check-in
Purpose: register member attendance at the entry desk.

Features:
- Identify members by badge
- Show the saved member photo, falling back to initials when no photo is available
- Record check-ins

### 5. Settings
Purpose: admin configuration.

Features:
- Gym profile settings
- Default plans
- Data backup option
- System preferences

The Payments and Attendance management screens are temporarily removed. Existing payment and attendance records continue to power the dashboard and reports, and attendance can still be recorded through Badge Check-in.

## Navigation layout
- Left sidebar menu
- Top header with title and search
- Main content area with cards and tables
- Right side detail panel for selected member when needed

## Design style
- Clean blue-and-white theme
- Primary blue (#1769E0) with deep navy (#0B2E68)
- Light blue surfaces (#EAF2FF) and white cards
- Gym identity reinforced with a StarGym star mark, the supplied training banner, and subtle activity charts
- Strong contrast for readability
- Clean tables and compact cards
- Owner-focused UI with operational clarity

## Color palette (blue and white)
- **Primary Blue**: #1769E0 (main buttons, highlights, progress bars)
- **Deep Navy**: #0B2E68 (headers and navigation)
- **Light Blue**: #B9D7FF (accents and borders)
- **Ink**: #10233F (primary text)
- **Muted**: #6B7E99 (secondary text and labels)
- **Light Background**: #F4F7FB (window backgrounds and panels)
- **White**: #FFFFFF (cards and input fields)
- **Soft Blue**: #EAF2FF (soft backgrounds and selected states)
- **Borders**: #DDE6F2 (subtle dividers)

The dashboard hero uses the user-supplied StarGym training banner.
