using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Run(async (HttpContext context) =>
{
    if (context.Request.Path.StartsWithSegments("/"))
    {
        context.Response.Headers["Content-Type"] = "text/html";

        await context.Response.WriteAsync($"The method is: {context.Request.Method}<br/>");
        await context.Response.WriteAsync($"The Url is: {context.Request.Path}<br/>");

        await context.Response.WriteAsync($"<b>Headers:</b><br/>");
        await context.Response.WriteAsync("<ul>");
        foreach (var key in context.Request.Headers.Keys)
        {
            await context.Response.WriteAsync($"<li><b>{key}</b>: {context.Request.Headers[key]}</li>");
        }
        await context.Response.WriteAsync("</ul>");

    }
    else if (context.Request.Path.StartsWithSegments("/employees"))
    {
        if (context.Request.Method == "GET")
        {
            if (context.Request.Query.ContainsKey("name")) // Get current select employee
            {
                var name = context.Request.Query["name"];
                var employee = EmployeesRepository.GetEmployeeByName(name);

                if (employee is not null)
                {
                    await context.Response.WriteAsync($"{employee.Name}: {employee.Position} {employee.Salary}");
                }
                else
                {
                    await context.Response.WriteAsync("This employee is not found!");
                }
            }
            else if (context.Request.Query.ContainsKey("id"))
            {
                var id = context.Request.Query["id"];

                if (int.TryParse(id, out int employeeId))
                { 
                    var employee = EmployeesRepository.GetEmployeeById(employeeId);
                    if (employee is not null)
                    {
                        await context.Response.WriteAsync($"{employee.Name}: {employee.Position} {employee.Salary}");
                    }
                    else
                    {
                        await context.Response.WriteAsync("Employee is not found!");
                    }
                }
            }
            else // Get all of the employees information
            {
                context.Response.StatusCode = 200;

                foreach (var emp in EmployeesRepository.GetEmployees())
                {
                    await context.Response.WriteAsync($"{emp.Name}: {emp.Position} {emp.Salary}\n");
                }
            }
        }
        else if (context.Request.Method == "POST")
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            try
            {
                var emp = JsonSerializer.Deserialize<Employee>(body);

                if (emp is null || emp.Id <= 0)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                EmployeesRepository.AddEmployee(emp);

                context.Response.StatusCode = 201;
                await context.Response.WriteAsync("Employee added successfully.");
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(ex.ToString());
                return;
            }

        }
        else if (context.Request.Method == "PUT")
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            var emp = JsonSerializer.Deserialize<Employee>(body);

            var result = EmployeesRepository.UpdateEmployee(emp);
            if (result)
            {
                context.Response.StatusCode = 204;
                await context.Response.WriteAsync("Employee is update!");
                return;
            }
            else
            {
                await context.Response.WriteAsync("Employee is not found!");
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
                        var result = EmployeesRepository.DeleteEmployee(employeeId);

                        if (result)
                        {
                            await context.Response.WriteAsync("Employee is delete!");
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsync("Employee is not found!");
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("You are not access!");
                    }

                }
            }
        }
    }
    else if (context.Request.Path.StartsWithSegments("/redirection"))
    {
        context.Response.Redirect("/employees");
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

app.Run();

static class EmployeesRepository
{
    private static List<Employee> employees = new List<Employee>
    {
        new Employee(1, "John Doe", "Engineer", 60000),
        new Employee(2, "Jane Smith", "Manager", 75000),
        new Employee(3, "Sam Brown", "Technician", 50000)
    };

    public static List<Employee> GetEmployees() => employees;

    public static Employee? GetEmployeeById(int employeeId)
    {
        return employees.FirstOrDefault(x => x.Id == employeeId);
    }

    public static Employee? GetEmployeeByName(string? name)
    {
        return employees.FirstOrDefault(x => x.Name.Contains(name??string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    public static void AddEmployee(Employee? emp)
    {
        if (emp is not null)
            employees.Add(emp);
    }

    public static bool UpdateEmployee(Employee? employee)
    {
        if (employee is not null)
        {
            var e = employees.FirstOrDefault(x => x.Id == employee.Id);

            if (e is not null)
            {
                e.Name = employee.Name;
                e.Salary = employee.Salary;
                e.Position = employee.Position;

                return true;
            }
        }

        return false;
    }

    public static bool DeleteEmployee(int employeeId)
    {
        var emp = employees.FirstOrDefault(x => x.Id == employeeId);

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