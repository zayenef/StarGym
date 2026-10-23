# Gym App Data Model

## Core tables

### Members
| Field | Type | Description |
| --- | --- | --- |
| Id | int | Primary key |
| FullName | string | Member full name |
| Phone | string | Contact number |
| Email | string | Email address |
| Gender | string | Male / Female / Other |
| DateOfBirth | DateTime | Birth date |
| JoinDate | DateTime | Membership start date |
| MembershipPlanId | int | Foreign key to membership plan |
| Status | string | Active / Inactive / Expired / Paused |
| TrainerId | int? | Optional trainer reference |
| Notes | string | Owner notes |

### MembershipPlans
| Field | Type | Description |
| --- | --- | --- |
| Id | int | Primary key |
| Name | string | Plan name |
| DurationDays | int | Plan length |
| Price | decimal | Monthly or package price |
| Description | string | Short plan description |
| IsActive | bool | Available status |

### Payments
| Field | Type | Description |
| --- | --- | --- |
| Id | int | Primary key |
| MemberId | int | Member reference |
| Amount | decimal | Payment amount |
| PaymentDate | DateTime | Payment date |
| PaymentMethod | string | Cash / Card / Transfer |
| InvoiceNumber | string | Invoice identifier |
| Status | string | Paid / Pending / Partial / Overdue |
| Notes | string | Optional notes |

### Attendance
| Field | Type | Description |
| --- | --- | --- |
| Id | int | Primary key |
| MemberId | int | Member reference |
| CheckInDate | DateTime | Attendance date |
| CheckInTime | TimeSpan | Entry time |
| CheckOutTime | TimeSpan? | Exit time |
| Status | string | Present / Late / Absent |

### Trainers
| Field | Type | Description |
| --- | --- | --- |
| Id | int | Primary key |
| Name | string | Trainer name |
| Phone | string | Contact |
| Specialization | string | Training focus |
| IsActive | bool | Current status |

## Relationships
- One membership plan can have many members.
- One member can have many payments.
- One member can have many attendance records.
- One trainer can have many members.

## Sample data idea
Create 12–20 members with a mix of:
- Active members
- Expired members
- Pending payments
- Different membership plans
- Different trainers

## Example membership plans
- Basic Gym
- Premium Gym
- Personal Training
- Family Plan

## Example status values
- Member status: Active, Inactive, Expired, Paused
- Payment status: Paid, Pending, Partial, Overdue
- Attendance status: Present, Late, Absent
