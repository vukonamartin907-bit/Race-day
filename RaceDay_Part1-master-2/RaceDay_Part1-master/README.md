# RaceDay - Part 1: System Planning and Database

## System overview

RaceDay is a full-stack event management platform designed for the South African road-running, walking and cycling community. It replaces paper registration and disconnected spreadsheets with a central database and API-driven system.

Part 1 establishes the data model, API contract and SQL Server database that will be used consistently in Parts 2 and 3.

## User roles

### Organiser
Organisers can:
- Create, edit and delete events.
- Create and manage event categories.
- View event enrolments.
- Capture, edit and delete participant results.
- View event-level enrolment and result summaries.

### Participant
Participants can:
- Create an account and log in.
- Browse events and categories.
- Enrol in an event by selecting a category.
- View their own enrolments.
- View and track their personal results/performance history.

Role-based access will be enforced at the API layer in Part 2 and reflected consistently in the MVC interface in Part 3.

## Part 1 contents

The `/docs` folder contains:

- `RaceDay_ERD.png` – complete ERD with 7 entities, attributes, PKs, FKs and relationship cardinalities.
- `RaceDay_ERD.dot` – editable Graphviz source for the ERD.
- `API_Endpoint_Plan.md` – complete endpoint specification including method, route, description, role, request body and expected response.
- `RaceDayDB.sql` – SQL Server schema and realistic seed data.

## Database model

The seven entities are:

1. `UserAccount`
2. `OrganiserProfile`
3. `ParticipantProfile`
4. `Event`
5. `Category`
6. `Enrollment`
7. `Result`

The database uses primary keys, foreign keys, unique constraints, `NOT NULL` constraints, defaults and validation `CHECK` constraints. The `Enrollment` table uses a composite foreign key to ensure that a selected category belongs to the specified event.

## Running the SQL script

1. Open SQL Server Management Studio (SSMS).
2. Connect to a SQL Server instance.
3. Open `docs/RaceDayDB.sql`.
4. Execute the complete script on a development SQL Server instance.
5. Confirm that the verification queries return users, organisers, participants, events, categories, enrolments and results.
6. Capture screenshots of the successful execution/results for the Part 1 demonstration and keep them available for the video.

The password values in the seed data are deliberately labelled demo hashes. Part 2 must replace these with properly hashed passwords and secure authentication.
## ERD DESIGN 
<img width="1366" height="768" alt="image" src="https://github.com/user-attachments/assets/5c2ae018-5abc-4b9b-9471-d451b8e16e66" />


## GitHub and CI/CD
The repository includes `.github/workflows/validate.yml`. The workflow checks that the required `/docs` files exist and performs basic SQL-script validation.
<img width="1244" height="1456" alt="image" src="https://github.com/user-attachments/assets/be484846-7ecc-48e7-a3c2-171abbbf8e97" />

### Required before submission

- [ ] Push the complete project to the lecturer-provided GitHub repository.
- [ ] Use the student's own GitHub account and make at least 20 meaningful commits for Part 1.
- [ ] Ensure the commit history shows genuine incremental work rather than repeated cosmetic changes.
- [ ] Confirm the GitHub Actions workflow completes successfully with a green check.
- [ ] Add a screenshot of the successful green workflow to this README.
- [ ] Replace the video placeholder below with the student's own unlisted YouTube link.
- [ ] The Part 1 video must use the student's own voice. AI-generated voices are not permitted.
- [ ] The video must show the running SQL script in SSMS and explain the ERD, endpoint decisions and design choices.

### CI/CD screenshot
<img width="1244" height="788" alt="image" src="https://github.com/user-attachments/assets/fb8be126-d506-4cc3-8503-58ce8a614e19" />



### Part 1 video Youtube link
https://youtu.be/RLegr60rUAk?si=FVrN-7UNeWsNASu2 

## Github link 
https://github.com/Chantell25/RaceDay_Part1 

## Consistency rule for Parts 2 and 3

The Part 2 REST API should implement this endpoint plan and database model without unexplained deviations. Part 3 should consume the same API and enforce the same two-role permissions in the MVC interface.

If the implementation intentionally changes the plan, the change and reason should be documented in this README as required by the brief.
