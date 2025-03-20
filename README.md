📌 Project Overview

ToDoListApp is a RESTful API built using ASP.NET Core that helps users manage their daily tasks efficiently. The application supports basic CRUD operations for task management and utilizes Redis Cache to optimize data retrieval.

⚙️ Technologies Used

ASP.NET Core 8.0

Entity Framework Core for database interactions

SQL Server as the database

Dependency Injection and Unit of Work design pattern

🔧 Installation and Setup

Clone the repository:

Navigate to the project directory:

Update the connection string in appsettings.json:

Install required packages:

Apply migrations and update the database:

Run the application:

🔥 Features

Create, Read, Update, Delete (CRUD) for task items

Data caching with Redis to optimize performance

Pagination for large data sets

Exception handling and validation

🗂️ Project Structure

📄 API Endpoints

HTTP Method

Endpoint

Description

GET

/api/v1/TaskItems

Get all tasks (with pagination)

GET

/api/v1/TaskItems/{id}

Get task by ID

POST

/api/v1/TaskItems

Create a new task

PUT

/api/v1/TaskItems/{id}

Update a task by ID

DELETE

/api/v1/TaskItems/{id}

Delete a task by ID

🗄️ Caching Strategy

Uses Redis Cache to store frequently accessed task items.

Cache invalidation on Create, Update, and Delete operations.

🛠️ Future Improvements

Implement user authentication and authorization.

Add role-based access control.

Integrate frontend for better user experience.

🤝 Contributions

Fork the repository.

Create a new branch.

Make necessary changes and commit.

Create a Pull Request.

📧 Contact

For any inquiries or feedback, feel free to reach out .com.
