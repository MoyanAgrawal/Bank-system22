using BankSystem.Models;
using BankSystem.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BankSystem.Controllers
{
    public class ManagerController : Controller
    {
        BSEntities3 db = new BSEntities3();
        // GET: Manager
        public ActionResult Index()
        {
            // load all records (no pagination)
            ViewBag.Employees = db.employees.OrderBy(e => e.Empnum).ToList();
            ViewBag.Customers = db.Customers.OrderBy(c => c.CustNum).ToList();
            ViewBag.LoanAccounts = db.LoanAccounts.OrderBy(l => l.LNNum).ToList();
            ViewBag.FDAccounts = db.FixedDepositAccounts.OrderBy(f => f.FDNum).ToList();

            return View();
        }
        [HttpGet]
        public ActionResult AddStaff()
        {
            var vm = new EmployeeRegisterViewModel();

            // populate departments dropdown
            vm.DeptList = new SelectList(db.Departments.OrderBy(d => d.Deptname).ToList(), "Deptid", "Deptname");

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Manager")]
        public ActionResult AddStaff(EmployeeRegisterViewModel vm)
        {
            vm.DeptList = new SelectList(db.Departments.OrderBy(d => d.Deptname).ToList(), "Deptid", "Deptname");

            // check PAN uniqueness across employees and customers
            var panNormalized = (vm.PAN ?? string.Empty).Trim();
            var panExistsInEmployees = db.employees.Any(e => e.PAN == panNormalized);
            var panExistsInCustomers = db.Customers.Any(c => c.PAN == panNormalized);

            if (panExistsInEmployees || panExistsInCustomers)
            {
                ViewBag.err = "Employee Already Exist";
                return View(vm);
            }

            if (ModelState.IsValid)
            {
                // create and persist employee
                var emp = new employee
                {
                    //Empid = vm.Empid.Trim(),
                    Empname = vm.Empname.Trim(),
                    Deptid = vm.Deptid.Trim(),
                    Emptype = "E", // "M" or "E"
                    Epassword = vm.Epassword,
                    PAN = panNormalized
                };

                db.employees.Add(emp);
                db.SaveChanges();

                ViewBag.Success = "Staff registered successfully.";
            }
            else
            {
                ViewBag.Success = "Staff not registered.";
            }
                ModelState.Clear();
            return View(new EmployeeRegisterViewModel());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}