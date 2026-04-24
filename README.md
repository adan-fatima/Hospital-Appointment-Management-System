# Hospital-Appointment-Management-System
# Hospital Appointment Management System

## Overview

The Hospital Appointment Management System is a web-based application designed to simplify and manage medical appointments digitally. It allows patients, doctors, and administrators to interact on a single platform, improving efficiency and reducing manual work.

## Features

### Patient

* Register and log in
* Search and filter doctors
* View doctor profiles
* Book appointments based on availability
* Cancel appointments (only before 24 hours)
* View unavailable or blocked time slots
* Provide feedback

### Doctor

* Create and manage profile
* Set availability by blocking specific dates or time slots
* Cancel appointments when needed

### Admin

* Add, update, and delete doctor data
* Manage system records

## Tech Stack

Frontend: HTML, CSS, JavaScript
Backend: C# (ASP.NET MVC)
Database: SQL Server
ORM: Entity Framework

## Architecture

The system follows MVC architecture:

* Model handles data and database logic
* View manages the user interface
* Controller processes user requests

## Database

Stores:

* Users (Admin, Doctor, Patient)
* Appointments
* Doctor availability
* Feedback

## Scheduling Logic

* Patients can only book available slots
* Doctors can block time slots, which are visible to patients
* Both doctors and patients can cancel appointments
* Patients must cancel at least 24 hours before the appointment

## Benefits

* Simplifies appointment booking
* Improves schedule management
* Reduces manual errors
* Provides a user-friendly experience

## Future Improvements

* Email notifications for appointments
* Online payment integration
* Chat system between doctor and patient
* Cloud deployment (e.g., Azure)

