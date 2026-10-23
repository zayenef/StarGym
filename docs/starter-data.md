# Starter Data for StarGym

## 1. Membership plans

### Basic Gym
- Id: 1
- Name: Basic Gym
- DurationDays: 30
- Price: 250
- Description: Standard gym access
- IsActive: true

### Premium Gym
- Id: 2
- Name: Premium Gym
- DurationDays: 60
- Price: 450
- Description: Full gym access with locker access
- IsActive: true

### Personal Training
- Id: 3
- Name: Personal Training
- DurationDays: 30
- Price: 700
- Description: Includes personal training sessions
- IsActive: true

### Family Plan
- Id: 4
- Name: Family Plan
- DurationDays: 90
- Price: 800
- Description: Family membership package
- IsActive: true

## 2. Trainers

### Ali Hassan
- Id: 1
- Name: Ali Hassan
- Phone: 0501112233
- Specialization: Strength
- IsActive: true

### Sara Ahmed
- Id: 2
- Name: Sara Ahmed
- Phone: 0502223344
- Specialization: Cardio
- IsActive: true

### Nora Sami
- Id: 3
- Name: Nora Sami
- Phone: 0503334455
- Specialization: CrossFit
- IsActive: true

## 3. Members

### Omar Khalid
- Id: 1
- FullName: Omar Khalid
- Phone: 0501000001
- Email: omar@example.com
- Gender: Male
- DateOfBirth: 1992-05-10
- JoinDate: 2025-08-01
- MembershipPlanId: 1
- Status: Active
- TrainerId: 1
- Notes: Regular member

### Layla Salem
- Id: 2
- FullName: Layla Salem
- Phone: 0501000002
- Email: layla@example.com
- Gender: Female
- DateOfBirth: 1995-02-12
- JoinDate: 2025-07-20
- MembershipPlanId: 2
- Status: Active
- TrainerId: 2
- Notes: Prefers cardio training

### Yousef Nasser
- Id: 3
- FullName: Yousef Nasser
- Phone: 0501000003
- Email: yousef@example.com
- Gender: Male
- DateOfBirth: 1988-11-08
- JoinDate: 2025-03-01
- MembershipPlanId: 3
- Status: Expired
- TrainerId: 3
- Notes: Needs renewal

### Dana Saleh
- Id: 4
- FullName: Dana Saleh
- Phone: 0501000004
- Email: dana@example.com
- Gender: Female
- DateOfBirth: 1997-09-21
- JoinDate: 2025-08-15
- MembershipPlanId: 4
- Status: Active
- TrainerId: 2
- Notes: New family plan member

### Hamad Ali
- Id: 5
- FullName: Hamad Ali
- Phone: 0501000005
- Email: hamad@example.com
- Gender: Male
- DateOfBirth: 1990-01-17
- JoinDate: 2025-06-05
- MembershipPlanId: 2
- Status: Paused
- TrainerId: 1
- Notes: On temporary pause

### Sara Omar
- Id: 6
- FullName: Sara Omar
- Phone: 0501000006
- Email: sara@example.com
- Gender: Female
- DateOfBirth: 2000-04-08
- JoinDate: 2025-09-01
- MembershipPlanId: 1
- Status: Active
- TrainerId: 3
- Notes: Beginner program

### Khaled Faris
- Id: 7
- FullName: Khaled Faris
- Phone: 0501000007
- Email: khaled@example.com
- Gender: Male
- DateOfBirth: 1986-12-15
- JoinDate: 2025-05-10
- MembershipPlanId: 1
- Status: Inactive
- TrainerId: 1
- Notes: Did not renew

### Reem Rami
- Id: 8
- FullName: Reem Rami
- Phone: 0501000008
- Email: reem@example.com
- Gender: Female
- DateOfBirth: 1994-07-03
- JoinDate: 2025-08-28
- MembershipPlanId: 3
- Status: Active
- TrainerId: 2
- Notes: High consistency

### Zaid Majed
- Id: 9
- FullName: Zaid Majed
- Phone: 0501000009
- Email: zaid@example.com
- Gender: Male
- DateOfBirth: 1993-06-29
- JoinDate: 2025-08-22
- MembershipPlanId: 2
- Status: Active
- TrainerId: 1
- Notes: Uses gym after work

### Noor Mansoor
- Id: 10
- FullName: Noor Mansoor
- Phone: 0501000010
- Email: noor@example.com
- Gender: Female
- DateOfBirth: 1998-10-11
- JoinDate: 2025-04-20
- MembershipPlanId: 2
- Status: Expired
- TrainerId: 2
- Notes: Renewal reminder sent

## 4. Payments

### Payment 1
- Id: 1
- MemberId: 1
- Amount: 250
- PaymentDate: 2025-08-01
- PaymentMethod: Card
- InvoiceNumber: INV-001
- Status: Paid
- Notes: Monthly payment

### Payment 2
- Id: 2
- MemberId: 2
- Amount: 450
- PaymentDate: 2025-07-20
- PaymentMethod: Cash
- InvoiceNumber: INV-002
- Status: Paid
- Notes: Premium payment

### Payment 3
- Id: 3
- MemberId: 3
- Amount: 700
- PaymentDate: 2025-03-01
- PaymentMethod: Transfer
- InvoiceNumber: INV-003
- Status: Pending
- Notes: Renewal pending

### Payment 4
- Id: 4
- MemberId: 4
- Amount: 800
- PaymentDate: 2025-08-15
- PaymentMethod: Card
- InvoiceNumber: INV-004
- Status: Paid
- Notes: Family plan

### Payment 5
- Id: 5
- MemberId: 5
- Amount: 450
- PaymentDate: 2025-06-05
- PaymentMethod: Cash
- InvoiceNumber: INV-005
- Status: Pending
- Notes: Membership on pause

### Payment 6
- Id: 6
- MemberId: 6
- Amount: 250
- PaymentDate: 2025-09-01
- PaymentMethod: Card
- InvoiceNumber: INV-006
- Status: Paid
- Notes: Basic plan

### Payment 7
- Id: 7
- MemberId: 7
- Amount: 250
- PaymentDate: 2025-05-10
- PaymentMethod: Transfer
- InvoiceNumber: INV-007
- Status: Overdue
- Notes: Failed renewal

### Payment 8
- Id: 8
- MemberId: 8
- Amount: 700
- PaymentDate: 2025-08-28
- PaymentMethod: Cash
- InvoiceNumber: INV-008
- Status: Paid
- Notes: Personal training

## 5. Attendance

### Attendance 1
- Id: 1
- MemberId: 1
- CheckInDate: 2025-09-10
- CheckInTime: 08:30:00
- CheckOutTime: 09:45:00
- Status: Present

### Attendance 2
- Id: 2
- MemberId: 2
- CheckInDate: 2025-09-10
- CheckInTime: 18:00:00
- CheckOutTime: 19:00:00
- Status: Present

### Attendance 3
- Id: 3
- MemberId: 3
- CheckInDate: 2025-09-09
- CheckInTime: 07:00:00
- CheckOutTime: 08:00:00
- Status: Late

### Attendance 4
- Id: 4
- MemberId: 4
- CheckInDate: 2025-09-11
- CheckInTime: 17:15:00
- CheckOutTime: 18:30:00
- Status: Present

### Attendance 5
- Id: 5
- MemberId: 5
- CheckInDate: 2025-09-11
- CheckInTime: 09:00:00
- CheckOutTime: 10:20:00
- Status: Present

### Attendance 6
- Id: 6
- MemberId: 6
- CheckInDate: 2025-09-10
- CheckInTime: 06:45:00
- CheckOutTime: 07:45:00
- Status: Present

### Attendance 7
- Id: 7
- MemberId: 7
- CheckInDate: 2025-09-08
- CheckInTime: 08:00:00
- CheckOutTime: 09:00:00
- Status: Absent

### Attendance 8
- Id: 8
- MemberId: 8
- CheckInDate: 2025-09-11
- CheckInTime: 19:15:00
- CheckOutTime: 20:00:00
- Status: Late

## 6. Dashboard KPIs from this data
- Total members: 10
- Active members: 6
- Expired members: 2
- Paused members: 1
- Inactive members: 1
- Pending or overdue payments: 3
- Monthly revenue estimate: based on paid records in the last 30 days

## 7. Data rules
- A member can have many payments.
- A member can have many attendance records.
- Each member belongs to one membership plan.
- Some members may have no trainer.
- If a member payment is overdue or pending, they should appear in payment alerts.
- If a member plan expired, they should be marked as Expired.
