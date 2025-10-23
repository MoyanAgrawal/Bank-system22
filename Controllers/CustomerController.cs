using BankSystem.Models;
using BankSystem.Models.ViewModels;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BankSystem.Controllers
{
    public class CustomerController : Controller
    {
        BSEntities3 db = new BSEntities3();

        // GET: Customer
        public ActionResult Index()
        {
            return View();
        }

        // GET: Customer/Dashboard?custId=CU0001
        public ActionResult Dashboard(string custId)
        {
            if (string.IsNullOrWhiteSpace(custId))
                return RedirectToAction("Index", "Auth");

            var customer = db.Customers
                .FirstOrDefault(c => c.CustID == custId);

            if (customer == null)
                return HttpNotFound();

            // load related accounts and recent transactions
            var savings = db.SavingsAccounts.Where(s => s.CustomerID == customer.CustID).ToList();
            var savingsAccountIds = savings.Select(s => s.SBAccountID).ToList();
            var savingsTx = db.SavingsTransactions
                .Where(t => savingsAccountIds.Contains(t.SBAccountID))
                .OrderByDescending(t => t.TransactionDate)
                .Take(10)
                .ToList();

            var loans = db.LoanAccounts.Where(l => l.CustomerID == customer.CustID).ToList();
            var loanIds = loans.Select(l => l.LNAccountID).ToList();
            var loanTx = db.LoanTransactions
                .Where(t => loanIds.Contains(t.LNAccountID))
                .OrderByDescending(t => t.EMIDate_Actual)
                .Take(10)
                .ToList();

            var fds = db.FixedDepositAccounts.Where(f => f.CustomerID == customer.CustID).ToList();
            var fdIds = fds.Select(f => f.FDAccountID).ToList();
            var fdTx = db.FDTransactions
                .Where(t => fdIds.Contains(t.FDAccountID))
                .OrderByDescending(t => t.TransactionDate)
                .Take(10)
                .ToList();

            var vm = new CustomerDashboardViewModel
            {
                Customer = customer,
                SavingsAccounts = savings,
                SavingsTransactions = savingsTx,
                LoanAccounts = loans,
                LoanTransactions = loanTx,
                FDAccounts = fds,
                FDTransactions = fdTx
            };

            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}