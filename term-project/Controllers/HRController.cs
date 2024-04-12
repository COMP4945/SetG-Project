﻿using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using term_project.Models.CareModels;
using dotenv.net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Supabase;
using term_project.Models.CRMModels;
using Guid = System.Guid;
using System.IO.Compression;

namespace term_project.Controllers
{
    public class HRController : Controller
    {
        private readonly Supabase.Client _supabase;

        public HRController()
        {
            DotEnv.Load();

            var supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url");
            var supabaseKey = Environment.GetEnvironmentVariable("Supabase__Key");

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true
            };

            _supabase = new Supabase.Client(supabaseUrl, supabaseKey, options);
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> HRLanding()
        {
            return View("~/Views/HRView/HRLanding.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEmployees()
        {
            var employeeResponse = await _supabase
                .From<Employee>()
                .Select("*")
                .Get();

            var employeeList = employeeResponse.Models;

            var employeeIDs = new List<Guid>();
            foreach (var employee in employeeList)
            {
                employeeIDs.Add(employee.EmployeeId);
            }

            var employeeFirstNames = new List<string>();
            foreach (var employee in employeeList)
            {
                employeeFirstNames.Add(employee.FirstName);
            }

            var employeeLastNames = new List<string>();
            foreach (var employee in employeeList)
            {
                employeeLastNames.Add(employee.LastName);
            }

            var employeeJobTitles = new List<string>();
            foreach (var employee in employeeList)
            {
                employeeJobTitles.Add(employee.JobTitle);
            }

            var JsonData = new
            {
                employeeIDs = employeeIDs,
                employeeFirstNames = employeeFirstNames,
                employeeLastNames = employeeLastNames,
                employeeJobTitles = employeeJobTitles
            };

            return Json(JsonData);
        }   

        public IActionResult HRManageEmployees()
        {
            return View("~/Views/HRView/HRManageEmployees.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployee(Guid employeeID)
        {
            Console.WriteLine("ID: " + employeeID.ToString());
            var employeeResponse = await _supabase
                .From<Employee>()
                .Select("*")
                .Where(e => e.EmployeeId == employeeID)
                .Single();

            if (employeeResponse == null)
            {
                return Json(new { error = "Employee not found" });
            }

            var employeeData = new
            {
                employeeFirstName = employeeResponse.FirstName,
                employeeLastName = employeeResponse.LastName,
                employeeAddress = employeeResponse.Address,
                employeeEmergencyContact = employeeResponse.EmergencyContact,
                employeeJobTitle = employeeResponse.JobTitle,
                employeeEmploymentType = employeeResponse.EmploymentType,
                employeeSalaryRate = employeeResponse.SalaryRate,
                employeeEmail = employeeResponse.Email
            };

            return Json(employeeData);
        }

        public IActionResult HRViewEmployee(Guid employeeID)
        {
            ViewData["EmployeeID"] = employeeID;
            return View("~/Views/HRView/HRViewEmployee.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> EditEmployee(Guid employeeID)
        {
            string requestBody;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            JObject jsonData = JObject.Parse(requestBody);

            Console.WriteLine(jsonData.ToString());

            string firstName = (string)jsonData["employeefirstName"];
            string lastName = (string)jsonData["employeelastName"];
            string address = (string)jsonData["employeeaddress"];
            string emergencyContact = (string)jsonData["employeeEmergencyContact"];
            string jobTitle = (string)jsonData["employeeJobTitle"];
            string employmentType = (string)jsonData["employeeEmploymentType"];
            string salaryRateString = (string)jsonData["employeeSalaryRate"];
            string email = (string)jsonData["employeeEmail"];

            if (float.TryParse(salaryRateString, out float newSalaryRate))
            {
                // Get the existing employee from the database
                var existingEmployeeResponse = await _supabase
                    .From<Employee>()
                    .Select("*")
                    .Where(e => e.EmployeeId == employeeID)
                    .Single();

                if (existingEmployeeResponse == null)
                {
                    return Json(new { error = "Employee not found" });
                }

                var existingEmployee = existingEmployeeResponse;

                // Get the existing pay history entry for the employee
                var payHistoryResponse = await _supabase
                    .From<PayHistory>()
                    .Select("*")
                    .Where(p => p.EmployeeId == employeeID)
                    .Single();

                var payHistory = payHistoryResponse;

                if (payHistory != null)
                {
                    // Update the pay history entry with the new salary rate and current date
                    var updatePayHistory = await _supabase
                        .From<PayHistory>()
                        .Where(p => p.PayHistoryId == payHistory.PayHistoryId)
                        .Set(p => p.PreviousSalaryRate, payHistory.NewSalaryRate) // Set previous salary rate to the current new salary rate
                        .Set(p => p.NewSalaryRate, newSalaryRate) // Set new salary rate to the updated salary rate
                        .Set(p => p.PayRaiseDate, DateTime.Now) // Set pay raise date to current time
                        .Update();
                }

                // Update the employee details
                var updateEmployee = await _supabase
                    .From<Employee>()
                    .Where(e => e.EmployeeId == employeeID)
                    .Set(e => e.FirstName, firstName)
                    .Set(e => e.LastName, lastName)
                    .Set(e => e.Address, address)
                    .Set(e => e.EmergencyContact, emergencyContact)
                    .Set(e => e.JobTitle, jobTitle)
                    .Set(e => e.EmploymentType, employmentType)
                    .Set(e => e.SalaryRate, newSalaryRate) // Update salary rate to the new rate
                    .Set(e => e.Email, email)
                    .Update();

                return Json(new { redirect = Url.Action("HRManageEmployees", "HR") });
            }
            else
            {
                // Return an error response
                return BadRequest("Failed to parse salary rate.");
            }
        }



        public IActionResult HREditEmployee(Guid employeeID)
        {
            ViewData["EmployeeID"] = employeeID;
            return View("~/Views/HRView/HREditEmployee.cshtml");
        }

		[HttpPost]
		public async Task<IActionResult> DeleteEmployee(Guid employeeID)
		{
			// Delete the employee
			var deleteEmployee = _supabase
				.From<Employee>()
				.Where(e => e.EmployeeId == employeeID)
				.Delete();

			// Delete the associated PayHistory entry
			var deletePayHistory = _supabase
				.From<PayHistory>()
				.Where(p => p.EmployeeId == employeeID)
				.Delete();

			return Json(new { redirect = Url.Action("HRManageEmployees", "HR") });
		}


		public IActionResult HRDeleteEmployee(Guid employeeID)
        {
            ViewData["EmployeeID"] = employeeID;
            return View("~/Views/HRView/HRDeleteEmployee.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee()
        {
            string requestBody;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            JObject jsonData = JObject.Parse(requestBody);

            string firstName = (string)jsonData["employeefirstName"];
            string lastName = (string)jsonData["employeelastName"];
            string address = (string)jsonData["employeeaddress"];
            string emergencyContact = (string)jsonData["employeeEmergencyContact"];
            string jobTitle = (string)jsonData["employeeJobTitle"];
            string employmentType = (string)jsonData["employeeEmploymentType"];
            string salaryRateString = (string)jsonData["employeeSalaryRate"];
            string email = (string)jsonData["employeeEmail"];

            if (float.TryParse(salaryRateString, out float salaryRate))
            {
                var employeeId = Guid.NewGuid();
                var model = new Employee
                {
                    EmployeeId = employeeId,
                    FirstName = firstName,
                    LastName = lastName,
                    Address = address,
                    EmergencyContact = emergencyContact,
                    JobTitle = jobTitle,
                    EmploymentType = employmentType,
                    SalaryRate = salaryRate,
                    Email = email
                };

                await _supabase.From<Employee>().Insert(model);

                await InsertAllNewEntries();


                return Json(new { redirect = Url.Action("HRManageEmployees", "HR") });
            }
            else
            {
                return BadRequest("Failed to parse salary rate.");
            }
        }

        public async Task<IActionResult> InsertAllNewEntries()
        {
            // Retrieve all employees
            var employeeResponse = await _supabase
                .From<Employee>()
                .Select("*")
                .Get();

            var employees = employeeResponse.Models;

            // Retrieve existing employee IDs from Pay_History table
            var payHistoryResponse = await _supabase
                .From<PayHistory>()
                .Select("employee_id")
                .Get();

            var existingEmployeeIds = payHistoryResponse.Models.Select(p => p.EmployeeId).ToList();

            // Filter out new employees who are not in the Pay_History table
            var newEmployees = employees.Where(e => !existingEmployeeIds.Contains(e.EmployeeId)).ToList();

            foreach (var employee in newEmployees)
            {
                // Check if SalaryRate has a value before using it
                float newSalaryRate = employee.SalaryRate.HasValue ? (float)employee.SalaryRate : 0;

                // Create a pay history entry for each new employee
                var payHistoryId = Guid.NewGuid();
                var payHistory = new PayHistory
                {
                    PayHistoryId = payHistoryId,
                    EmployeeId = employee.EmployeeId,
                    PayRaiseDate = DateTime.Now, // Set pay raise date to current time
                    PreviousSalaryRate = 0, // No previous salary rate for new employee
                    NewSalaryRate = newSalaryRate // Set new salary rate to the initial salary rate
                };

                // Insert pay history entry into the database
                await _supabase.From<PayHistory>().Insert(payHistory);
            }

            return Ok("New entries inserted successfully.");
        }



        public IActionResult HRCreateEmployee()
        {
            return View("~/Views/HRView/HRCreateEmployee.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> PayHistory(int page = 1, int pageSize = 10)
        {
            // Calculate the offset based on the page number and page size
            int offset = (page - 1) * pageSize;


            // Retrieve PayHistory data with pagination
            var payHistoryResponse = await _supabase
                .From<PayHistory>()
                .Select("*")
                .Range(offset, offset + pageSize - 1) // Specify the range for pagination
                .Get();

            var payHistoryList = payHistoryResponse.Models;

            // Create a list to store pay history entries with employee details
            var payHistoryWithEmployeeDetails = new List<object>();

            foreach (var payrollEntry in payHistoryList)
            {
                // Extract pay history details
                var payRaiseDate = payrollEntry.PayRaiseDate;
                var previousSalaryRate = payrollEntry.PreviousSalaryRate;
                var newSalaryRate = payrollEntry.NewSalaryRate;

                // Extract employee ID
                var employeeId = payrollEntry.EmployeeId;

                // Retrieve employee details
                var employeeResponse = await _supabase
                    .From<Employee>()
                    .Select("first_name, last_name")
                    .Where(employeeResponse => employeeResponse.EmployeeId == employeeId)
                    .Single();

                if (employeeResponse != null)
                {
                    var employee = employeeResponse;

                    // Extract employee details
                    var employeeFirstName = employee.FirstName;
                    var employeeLastName = employee.LastName;

                    // Create an anonymous object with pay history and employee details
                    var payHistoryEntry = new
                    {
                        EmployeeFirstName = employeeFirstName,
                        EmployeeLastName = employeeLastName,
                        PayRaiseDate = payRaiseDate,
                        PreviousSalaryRate = previousSalaryRate,
                        NewSalaryRate = newSalaryRate
                    };

                    // Add the pay history entry to the list
                    payHistoryWithEmployeeDetails.Add(payHistoryEntry);
                }
            }

            // Serialize the list to JSON and return it
            var jsonData = JsonConvert.SerializeObject(payHistoryWithEmployeeDetails);
            return Content(jsonData, "application/json");
        }





        public IActionResult HRPayHistory()
		{

			return View("~/Views/HRView/HRPayHistory.cshtml");
		}

        [HttpPost]
        public async Task<IActionResult> GetAttendanceRecords(string email, string password)
        {

            string requestBody;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            JObject jsonToBeVerified = JObject.Parse(requestBody);

            string employeeEmail = (string)jsonToBeVerified["email"];
            string employeePassword = (string)jsonToBeVerified["password"];

            // First, check if the email exists in the database.
            var employeeResponse = await _supabase
                .From<Employee>()
                .Select("employee_id, email")
                .Where(e => e.Email == employeeEmail)
                .Single();

            if (employeeResponse == null)
            {
                return Json(new { error = "Invalid email or password" });
            }

            // If the email exists, retrieve the corresponding employee's ID.
            var employeeId = employeeResponse.EmployeeId;

            // Now, verify the password.
            // You need to implement your password verification logic here.
            // For simplicity, let's assume the password is stored in the database and directly compare it.
            var employeePasswordResponse = await _supabase
                .From<Employee>()
                .Select("password")
                .Where(e => e.EmployeeId == employeeId)
                .Single();

            if (employeePasswordResponse == null)
            {
                return Json(new { error = "Invalid email or password" });
            }

            string storedPassword = (string)employeePasswordResponse.Password;

            // Compare the provided password with the stored password.
            if (employeePassword != storedPassword)
            {
                return Json(new { error = "Invalid email or password" });
            }

            // Now, fetch the attendance records for the authenticated employee.
            var attendanceResponse = await _supabase
                .From<Attendance>()
                .Select("*")
                .Where(a => a.EmployeeId == employeeId)
                .Get();

            var attendanceRecords = attendanceResponse.Models;

            // If there are no attendance records for the employee, return an appropriate response.
            if (attendanceRecords == null || !attendanceRecords.Any())
            {
                return Json(new { error = "No attendance records found for the employee" });
            }

            // Serialize the attendance records and return them.
            var jsonData = JsonConvert.SerializeObject(attendanceRecords);
            return Content(jsonData, "application/json");
        }

        public IActionResult HREmployeeAttendance()
        {

            return View("~/Views/HRView/HREmploeeAttendance.cshtml");
        }

    }
}
