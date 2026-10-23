# StarGym Architecture

## Overview
The application is built for the gym owner, not the clients. It focuses on operations such as member registration, payments, attendance, and reports.

## Proposed architecture

### 1. Presentation Layer
This is the WPF desktop UI.
- Views for dashboard, members, payments, attendance
- User interactions
- Binding to ViewModels

### 2. ViewModel Layer
Handles UI logic and data binding.
- DashboardViewModel
- MembersViewModel
- PaymentsViewModel
- AttendanceViewModel
- MainViewModel

### 3. Application / Service Layer
Business logic and orchestration.
- MemberService
- PaymentService
- AttendanceService
- MembershipService
- DashboardService

### 4. Domain Layer
Core business entities.
- Member
- MembershipPlan
- Payment
- Attendance
- Trainer

### 5. Infrastructure Layer
Data access and persistence.
- SQLite database
- Repository pattern
- Data context / database connection logic

## Core entities

### Member
- Id
- FullName
- Phone
- Email
- Gender
- DateOfBirth
- JoinDate
- MembershipPlanId
- Status
- TrainerId
- Notes

### MembershipPlan
- Id
- Name
- DurationDays
- Price
- Description
- IsActive

### Payment
- Id
- MemberId
- Amount
- PaymentDate
- PaymentMethod
- InvoiceNumber
- Status
- Notes

### Attendance
- Id
- MemberId
- CheckInDate
- CheckInTime
- CheckOutTime
- Status

### Trainer
- Id
- Name
- Phone
- Specialization
- IsActive

## First implementation milestone
Before UI polish, build the following modules:
1. Dashboard
2. Members
3. Payments
4. Attendance

## Recommended database approach
- SQLite database file stored locally
- Repository pattern for CRUD operations
- Easy backup and portability

## Design principle
Keep the owner-focused workflow simple, fast, and operationally useful.
