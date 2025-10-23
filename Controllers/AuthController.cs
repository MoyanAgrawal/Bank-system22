using BankSystem.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace BankSystem.Controllers
{
    public class AuthController : Controller
    {
        BSEntities3 db = new BSEntities3();

        public ViewResult Index()
        {
            return View();
        }




        [HttpGet]
        public ActionResult CustRegister(string returnTo = null)
        {
            // preserve returnTo so the form/post can include it (optional)
            ViewBag.ReturnTo = returnTo;
            return View();
        }

        [HttpPost]
        public ActionResult CustRegister(Customer c)
        {
            // read returnTo from form or querystring (supports GET link or hidden form field)
            var returnTo = (Request.Form["returnTo"] ?? Request.QueryString["returnTo"])?.ToString();

            // DOB validation
            if (c.DOB > DateTime.Today)
            {
                ModelState.AddModelError(nameof(c.DOB), "Date of birth cannot be in the future.");
            }

            if (!ModelState.IsValid)
            {
                // keep returnTo for redisplay
                ViewBag.ReturnTo = returnTo;
                return View(c);
            }

            db.Customers.Add(c);
            int i = db.SaveChanges();
            if (i > 0)
            {
                ViewData["msg"] = "Registered Successfully!";
                ViewData["cls"] = "btn btn-success";

                // If caller asked to return to manager, redirect there
                if (string.Equals(returnTo, "manager", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToRoute(new { controller = "Manager", action = "Index" });
                }

                // Otherwise redirect to customer dashboard if CustID present, else to Customer Index
                if (!string.IsNullOrWhiteSpace(c.CustID))
                    return RedirectToAction("Dashboard", "Customer", new { custId = c.CustID });

                return RedirectToAction("Index", "Customer");
            }
            else
            {
                ViewData["msg"] = "Registration Unsuccessful.";
                ViewData["cls"] = "btn btn-danger";
                ViewBag.ReturnTo = returnTo;
                return View(c);
            }
        }











        [HttpGet]
        public ActionResult CustLogin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CustLogin(Customer c)
        {

            var res = (from t in db.Customers
                       where t.CustName == c.CustName && t.Cpassword == c.Cpassword
                       select t).Count();
            var customer = db.Customers.FirstOrDefault(t => t.CustName == c.CustName && t.Cpassword == c.Cpassword);
            if (res > 0)
            {
                Session["name"] = c.CustName;
                return RedirectToAction("Dashboard", "Customer", new { custId = customer.CustID });
                //ViewData["msg"] = "Login Success!!";
            }
            else
            {
                ViewData["msg"] = "Login Unsuccesfull!!";
            }
            return View(c);
        }













        [HttpGet]
        public ViewResult EmpLogin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult EmpLogin(employee e)
        {
            var emp = db.employees.FirstOrDefault(t => t.Empname == e.Empname && t.Epassword == e.Epassword);
            if (emp != null)
            {
                // set session values required by other pages/controllers
                Session["name"] = emp.Empname;   // display name
                Session["uname"] = emp.Empid;    // employee identifier used by controller lookups
                Session["deptid"] = emp.Deptid;  // department id
                Session["emptype"] = emp.Emptype; // store employee type

                // optional: store the department name for convenience
                var dept = db.Departments.FirstOrDefault(d => d.Deptid == emp.Deptid);
                if (dept != null)
                {
                    Session["deptname"] = dept.Deptname;
                }
                else
                {
                    Session["manager"] = emp.Empname;
                }

                // route based on employee type: Manager (M) -> Manager controller, Employee (E) -> Employee controller
                if (string.Equals(emp.Emptype, "M", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToRoute(new { controller = "Manager", action = "Index" });
                }
                else // treat anything else (including "E") as regular employee
                {
                    return RedirectToRoute(new { controller = "Employee", action = "Index" });
                }
            }
            else
            {
                ViewData["msg"] = "Login Unsuccesfull!!";
            }
            return View();
        }


        public ActionResult Logout()
        {
            Session.Abandon();
            return RedirectToRoute(new { controller = "Auth", action = "Index" });
        }   
        //public RedirectToRouteResult Redirect()
        //{
        //    return RedirectToRoute(new { controller = "Employee", action = "Index" });
        //}
    }
}