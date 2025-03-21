# 📒 ToDoListApp

🌟 **Project Overview**

ToDoListApp is a RESTful API built with ASP.NET Core that helps users effectively manage their daily tasks. The application supports basic CRUD operations for task management and uses Redis Cache to optimize data retrieval.

🛠️ **Technologies Used**

* ASP.NET Core 8.0
* Entity Framework Core for database querying
* Redis Cache for data caching
* SQL Server as the database
* Dependency Injection and Unit of Work Pattern

🔧 **Installation and Configuration**

1.  Clone the repository:
    ```bash
    git clone https://github.com/HaiquangPham14/ToDoListApp.git
    ```
2. Open MSSQL and execute the command:
    ```bash
    CREATE DATABASE your_database;
    ```
3.  Update the connection string in `appsettings.json`:
    ```json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Server=your_server;Database=your_database;User Id=your_user;Password=your_password;"
      },
      "Redis": {
        "Configuration": "your_server",
        "Instancename": "your_cache_name"
      }
    }
    ```
4.  Install necessary packages:
    ```bash
    dotnet add package Microsoft.EntityFrameworkCore -v 8.0.10
    dotnet add package Microsoft.EntityFrameworkCore.Design -v 8.0.10
    dotnet add package Microsoft.EntityFrameworkCore.SqlServer -v 8.0.10
    dotnet add package Microsoft.EntityFrameworkCore.Tools -v 8.0.10
    dotnet add package Microsoft.VisualStudio.Azure.Containers.Tools.Targets -v 1.21.0
    dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design -v 8.0.7
    dotnet add package Swashbuckle.AspNetCore -v 6.6.2
    dotnet add package StackExchange.Redis
    dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
    ```
5.  Apply migrations and update the database:
    ```bash
    Add-Migration Initial
    Update-Database
    ```
6.  Run the application:
    ```bash
    dotnet run
    ```

🔥 **Features**

* CRUD operations (Create, Read, Update, Delete) for Task Items.
* Data caching with Redis to enhance performance.
* Supports pagination for large data queries.
* Exception handling and data validation.

📄 **API Endpoints**

| HTTP Method | Endpoint                     | Description                                  |
| :---------- | :--------------------------- | :------------------------------------------- |
| GET         | `/api/v1/TaskItems`          | Get a paginated list of tasks                |
| GET         | `/api/v1/TaskItems/{id}`     | Get task details by ID                       |
| POST        | `/api/v1/TaskItems`          | Create a new task                            |
| PUT         | `/api/v1/TaskItems/{id}`     | Update task details                          |
| DELETE      | `/api/v1/TaskItems/{id}`     | Delete a task by ID                          |
| POST        | `/api/v1/TaskDependencies`     | Create a new task dependency                 |
| GET         | `/api/v1/TaskDependencies/{id}` | Get task dependency details by ID            |
| GET         | `/api/v1/TaskDependencies/task/{taskId}` | Get dependencies for a specific task        |
| PUT         | `/api/v1/TaskDependencies/{id}` | Update task dependency details               |
| DELETE      | `/api/v1/TaskDependencies/{id}` | Delete a task dependency by ID               |

🔎 **Caching Strategy**

* Uses Redis Cache to store frequently accessed data.
* Cache is updated on Create, Update, and Delete operations.

🚀 **Future Development**

* Integrate user authentication and advanced authorization.
* Develop a frontend interface for a better user experience.
* Create a notification system for upcoming/overdue tasks
* Implement a background job scheduler for periodic tasks
  
🤝 **Contribution**

1.  Fork the repository.
2.  Create a new branch for your feature.
3.  Commit and push your code to the new branch.
4.  Create a Pull Request for review.

📧 **Contact**

For questions or contributions, please reach out via phamwang02@gmail.com.
