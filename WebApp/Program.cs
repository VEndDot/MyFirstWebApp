using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Run(async (HttpContext context) =>
{
    if (context.Request.Path.StartsWithSegments("/"))
    {
        await context.Response.WriteAsync($"The method is: {context.Request.Method}\r\n");
        await context.Response.WriteAsync($"The Url is: {context.Request.Path}\r\n");

        await context.Response.WriteAsync($"\r\nHeaders:\r\n");
        foreach (var key in context.Request.Headers.Keys)
        {
            await context.Response.WriteAsync($"{key}: {context.Request.Headers[key]}\r\n");
        }
    }
    else if (context.Request.Path.StartsWithSegments("/employees"))
    {
        if (context.Request.Method == "GET")
        {
            var employees = EmployeeRepository.GetEmployees();

            foreach (var employee in employees)
            {
                await context.Response.WriteAsync($"{employee.Name} {employee.Position} {employee.Salary}\n");
            }
        }
        else if (context.Request.Method == "POST")
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            var employee = JsonSerializer.Deserialize<Employee>(body);

            EmployeeRepository.AddEmployee(employee);
        }
        else if (context.Request.Method == "PUT")
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            var employee = JsonSerializer.Deserialize<Employee>(body);

            var reuslt = EmployeeRepository.UpdateEmployee(employee);

            if (reuslt)
            {
                await context.Response.WriteAsync("Employee update successully.");
            }
            else
            {
                await context.Response.WriteAsync("Employee not found.");
            }
        }
        else if (context.Request.Method == "DELETE")
        {
            if (context.Request.Query.ContainsKey("id"))
            {
                var id = context.Request.Query["id"];
                if (int.TryParse(id, out int employeeId))
                {
                    if (context.Request.Headers["Authorization"] == "frank")
                    {
                        var result = EmployeeRepository.DeleteEmployee(employeeId);
                        if (result)
                        {
                            await context.Response.WriteAsync("Employee is delete successully.");
                        }
                        else
                        {
                            await context.Response.WriteAsync("Employee not found.");
                        }
                    }
                    else
                    {
                        await context.Response.WriteAsync("You are not authorized to delete.");
                    }
                }
            }
        }
    }
});

app.Run();


static class EmployeeRepository
{ 
    private static List<Employee> employees = new List<Employee>
    { 
        new Employee(1, "John Doe", "Engineer", 60000),
        new Employee(2, "Jane Smith", "Manager", 75000),
        new Employee(3, "Sam Brown", "Technician", 50000),
    };

    public static List<Employee> GetEmployees()=> employees;
    public static void AddEmployee(Employee? employee)
    {
        if (employee is not null)
        { 
            employees.Add(employee);
        }
    }

    public static bool UpdateEmployee(Employee? employee)
    {
        if (employee is not null)
        {
            var emp = employees.FirstOrDefault(x => x.Id == employee.Id);
            if (emp != null)
            {
                emp.Name = employee.Name;
                emp.Position = employee.Position;
                emp.Salary = employee.Salary;

                return true;
            }
        }

        return false;
    }

    public static bool DeleteEmployee(int id)
    {
        var emp = employees.FirstOrDefault(x => x.Id == id);
        if (emp is not null)
        {
            employees.Remove(emp);
            return true;
        }

        return false;
    }
}

public class Employee
{ 
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public double Salary { get; set; }

    public Employee(int id, string name, string position, double salary)
    {
        Id = id;
        Name = name;
        Position = position;
        Salary = salary;
    }
}