using BankSystem.Models;
using BankSystem.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BankSystem.Controllers
{
    public class EmployeeController : Controller
    {
        BSEntities3 db = new BSEntities3();
        // GET: Employee
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult PanCheck()
        {
            return View();
        }

        [HttpPost]
        public ActionResult PanCheck(string Pan)
        {
            if (string.IsNullOrWhiteSpace(Pan))
            {
                ModelState.AddModelError(nameof(Pan), "Please enter a PAN.");
                return View(new Customer { PAN = Pan });
            }

            var customer = db.Customers.FirstOrDefault(t => t.PAN == Pan);
            if (customer != null)
            {
                bool hasSavings = db.SavingsAccounts.Any(sa => sa.CustomerID == customer.CustID);
                bool hasFd = db.FixedDepositAccounts.Any(fd => fd.CustomerID == customer.CustID);
                bool hasLoan = db.LoanAccounts.Any(ln => ln.CustomerID == customer.CustID);

                if (hasSavings || hasFd || hasLoan)
                {
                    ViewBag.err = "An account already exists with this PAN.";
                    return View(new Customer { PAN = Pan });
                }
                // pass PAN as query parameter so OpenAccount can prefill the customer
                return RedirectToAction("OpenAccount", new { pan = Pan });
            }
            else
            {
                TempData["AlertMessage"] = "PAN doesn't exist. Register Customer";
                TempData["AlertType"] = "alert-warning";
                return RedirectToRoute(new { controller = "Auth", action = "CustRegister" });
            }
        }

        [HttpGet]
        public ActionResult OpenAccount(string pan = null)
        {
            var vm = new OpenAccountViewModel();

            // customers (materialize first)
            var customers = db.Customers.OrderBy(c => c.CustName).ToList();
            vm.CustomerList = new SelectList(customers
                .Select(c => new { c.CustNum, Display = c.CustID + " - " + c.CustName })
                .ToList(), "CustNum", "Display");

            // If PAN provided, try to find customer and preselect
            if (!string.IsNullOrWhiteSpace(pan))
            {
                var customer = db.Customers.FirstOrDefault(c => c.PAN == pan);
                if (customer != null)
                {
                    vm.IsCustomerSelected = true;
                    vm.SelectedCustNum = customer.CustNum;
                    vm.CustomerDisplay = customer.CustID + " - " + customer.CustName;
                }
            }

            // Determine permissions from Session["deptid"]
            var deptId = (Session["deptid"] as string) ?? string.Empty;

            vm.AllowSavings = string.Equals(deptId, "DEPT01", StringComparison.OrdinalIgnoreCase);
            vm.AllowFD = string.Equals(deptId, "DEPT01", StringComparison.OrdinalIgnoreCase);
            vm.AllowLoan = string.Equals(deptId, "DEPT02", StringComparison.OrdinalIgnoreCase);
            vm.AllowAll = string.Equals(deptId, "", StringComparison.OrdinalIgnoreCase);

            // Build AccountTypeList based on permissions
            var types = new List<SelectListItem>();

            if (vm.AllowAll)
            {
                // grant all types when AllowAll is true
                types.Add(new SelectListItem { Value = "Savings", Text = "Savings" });
                types.Add(new SelectListItem { Value = "Loan", Text = "Loan" });
                types.Add(new SelectListItem { Value = "FD", Text = "Fixed Deposit" });
            }
            else
            {
                if (vm.AllowSavings) types.Add(new SelectListItem { Value = "Savings", Text = "Savings" });
                if (vm.AllowLoan) types.Add(new SelectListItem { Value = "Loan", Text = "Loan" });
                if (vm.AllowFD) types.Add(new SelectListItem { Value = "FD", Text = "Fixed Deposit" });
            }
            if (types.Count > 0)
            {
                types.Insert(0, new SelectListItem { Value = "", Text = "Select..." });
            }
            else
            {
                types.Add(new SelectListItem { Value = "", Text = "No account types available" });
            }

            vm.AccountTypeList = new SelectList(types, "Value", "Text");

            return View(vm);
        }

        // POST: Employee/OpenAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenAccount(OpenAccountViewModel vm)
        {
            // repopulate customer list
            var customers = db.Customers.OrderBy(c => c.CustName).ToList();
            vm.CustomerList = new SelectList(customers
                .Select(c => new { c.CustNum, Display = c.CustID + " - " + c.CustName })
                .ToList(), "CustNum", "Display", vm.SelectedCustNum);

            // Re-evaluate permissions from Session["deptid"]
            var deptId = (Session["deptid"] as string) ?? string.Empty;

            vm.AllowSavings = string.Equals(deptId, "DEPT01", StringComparison.OrdinalIgnoreCase);
            vm.AllowFD = string.Equals(deptId, "DEPT01", StringComparison.OrdinalIgnoreCase);
            vm.AllowLoan = string.Equals(deptId, "DEPT02", StringComparison.OrdinalIgnoreCase);
            vm.AllowAll = string.Equals(deptId, "", StringComparison.OrdinalIgnoreCase);

            // Rebuild AccountTypeList for redisplay
            var types = new List<SelectListItem>();

            if (vm.AllowAll)
            {
                // grant all types when AllowAll is true
                types.Add(new SelectListItem { Value = "Savings", Text = "Savings" });
                types.Add(new SelectListItem { Value = "Loan", Text = "Loan" });
                types.Add(new SelectListItem { Value = "FD", Text = "Fixed Deposit" });
            }
            else
            {
                if (vm.AllowSavings) types.Add(new SelectListItem { Value = "Savings", Text = "Savings" });
                if (vm.AllowLoan) types.Add(new SelectListItem { Value = "Loan", Text = "Loan" });
                if (vm.AllowFD) types.Add(new SelectListItem { Value = "FD", Text = "Fixed Deposit" });
            }
            if (types.Count > 0)
            {
                types.Insert(0, new SelectListItem { Value = "", Text = "Select..." });
            }
            else
            {
                types.Add(new SelectListItem { Value = "", Text = "No account types available" });
            }

            vm.AccountTypeList = new SelectList(types, "Value", "Text", vm.AccountType);

            // basic validation
            if (vm.SelectedCustNum <= 0)
                ModelState.AddModelError(nameof(vm.SelectedCustNum), "Please select a customer.");

            // ensure the selected type is allowed for this employee
            if (string.IsNullOrWhiteSpace(vm.AccountType) ||
                !(vm.AllowAll ||
      (vm.AccountType == "Savings" && vm.AllowSavings) ||
      (vm.AccountType == "Loan" && vm.AllowLoan) ||
      (vm.AccountType == "FD" && vm.AllowFD)))
            {
                ModelState.AddModelError(nameof(vm.AccountType), "Please select a valid account type.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var customer = db.Customers.FirstOrDefault(c => c.CustNum == vm.SelectedCustNum);
            if (customer == null)
            {
                ModelState.AddModelError("", "Selected customer not found.");
                return View(vm);
            }

            // create account rows
            switch (vm.AccountType.ToLowerInvariant())
            {
                case "savings":
                    // Validate initial deposit
                    if (!vm.SavingsInitialDeposit.HasValue || vm.SavingsInitialDeposit.Value < 1000m)
                    {
                        ViewBag.err = "Minimum initial deposit for a savings account is Rs. 1,000.";
                        return View(vm);
                    }

                    // Prevent employees/managers from being customers (Empid equals CustID)
                    if (db.employees.Any(e => e.Empid == customer.CustID))
                    {
                        ViewBag.err = "Bank employees or managers cannot open customer accounts.";
                        return View(vm);
                    }

                    // Prevent more than one savings account per customer
                    if (db.SavingsAccounts.Any(saCheck => saCheck.CustomerID == customer.CustID))
                    {
                        ViewBag.err = "This customer already has a savings account.";
                        return View(vm);
                    }

                    // Generate a unique SBAccountID (SB + next SBNum) padded to 5 digits
                    var maxSbNum = db.SavingsAccounts.Max(s => (int?)s.SBNum) ?? 0;
                    var nextSbNum = maxSbNum + 1;
                    var generatedAccountId = "SB" + nextSbNum.ToString("D5");

                    var sa = new SavingsAccount
                    {
                        SBAccountID = generatedAccountId,
                        CustomerID = customer.CustID,
                        Balance = vm.SavingsInitialDeposit.Value,
                        CreatedAt = DateTime.Now
                    };

                    // Add the savings account and the initial deposit transaction (no parent navigation used)
                    db.SavingsAccounts.Add(sa);

                    var initTx = new SavingsTransaction
                    {
                        SBAccountID = generatedAccountId,
                        TransactionDate = DateTime.Now,
                        TransactionType = "D",
                        Amount = vm.SavingsInitialDeposit.Value
                    };
                    db.SavingsTransactions.Add(initTx);

                    db.SaveChanges();
                    TempData["Message"] = "Savings account created.";
                    break;

                case "loan":
                    // basic required fields
                    if (!vm.LoanAmount.HasValue || !vm.LoanStartDate.HasValue || !vm.LoanTenureMonths.HasValue || !vm.MonthlyTakeHome.HasValue)
                    {
                        ViewBag.err = "LoanAmount, LoanStartDate, LoanTenureMonths and MonthlyTakeHome are required for a loan.";
                        return View(vm);
                    }

                    // minimum loan amount
                    if (vm.LoanAmount.Value < 10000m)
                    {
                        ViewBag.err = "Minimum loan amount is Rs. 10,000.";
                        return View(vm);
                    }

                    // determine customer age to identify senior citizen (age 60+ considered senior)
                    var dob = customer.DOB;
                    var age = (int)((DateTime.Today - dob).TotalDays / 365.2425);
                    var isSenior = age >= 60;

                    // senior-specific rule: cannot sanction > 1,00,000 and ROI 9.5%
                    if (isSenior && vm.LoanAmount.Value > 100000m)
                    {
                        ViewBag.err = "Senior citizens cannot be sanctioned a loan greater than Rs. 1,00,000.";
                        return View(vm);
                    }

                    // determine ROI according to rules
                    decimal determinedRoi;
                    if (isSenior)
                    {
                        determinedRoi = 9.5m;
                    }
                    else
                    {
                        var amt = vm.LoanAmount.Value;
                        if (amt <= 500000m) determinedRoi = 10.0m;
                        else if (amt <= 1000000m) determinedRoi = 9.5m;
                        else determinedRoi = 9.0m;
                    }

                    // compute EMI and enforce EMI <= 60% of monthly take-home
                    var monthlyRate = (double)determinedRoi / 100.0 / 12.0;
                    var n = vm.LoanTenureMonths.Value;
                    var P = (double)vm.LoanAmount.Value;
                    decimal emi = 0m;
                    if (monthlyRate > 0 && n > 0)
                    {
                        var pow = Math.Pow(1 + monthlyRate, n);
                        var emiDouble = P * monthlyRate * pow / (pow - 1);
                        emi = (decimal)emiDouble;
                    }
                    else if (n > 0)
                    {
                        emi = (decimal)(P / n);
                    }

                    var maxEmiAllowed = vm.MonthlyTakeHome.Value * 0.6m;
                    if (emi > maxEmiAllowed)
                    {
                        ViewBag.err = $"Computed EMI ({emi:C}) exceeds 60% of customer's monthly take-home ({maxEmiAllowed:C}). Adjust tenure or amount.";
                        return View(vm);
                    }

                    // generate LNAccountID (LN + next LNNum padded)
                    var maxLnNum = db.LoanAccounts.Max(l => (int?)l.LNNum) ?? 0;
                    var nextLnNum = maxLnNum + 1;
                    var generatedLnId = "LN" + nextLnNum.ToString("D5");

                    // create loan account and set computed ROI and EMI
                    var ln = new LoanAccount
                    {
                        LNAccountID = generatedLnId,
                        CustomerID = customer.CustID,
                        LoanAmount = vm.LoanAmount.Value,
                        StartDate = vm.LoanStartDate.Value,
                        TenureMonths = vm.LoanTenureMonths.Value,
                        LNROI = determinedRoi,
                        EMIAmount = Math.Round(emi, 2)
                    };

                    db.LoanAccounts.Add(ln);
                    db.SaveChanges();

                    // optional: add an initial LoanTransaction to capture sanctioned amount / outstanding
                    var initialOutstanding = ln.LoanAmount;
                    var initialTx = new LoanTransaction
                    {
                        LNAccountID = ln.LNAccountID,
                        EMIDate_Actual = ln.StartDate,
                        EMIDate_Paid = null,
                        LatePenalty = 0m,
                        Amount = 0m,
                        Outstanding = initialOutstanding
                    };
                    db.LoanTransactions.Add(initialTx);
                    db.SaveChanges();

                    TempData["Message"] = $"Loan account created. Account: {ln.LNAccountID}, EMI: {ln.EMIAmount:C}, ROI: {ln.LNROI}%";
                    break;

                case "fd":
                case "fixeddeposit":
                    if (!vm.FDStartDate.HasValue || !vm.FDEndDate.HasValue || !vm.FDDepositAmount.HasValue)
                    {
                        ViewBag.err = "FDStartDate, FDEndDate and FDDepositAmount are required for fixed deposit.";
                        return View(vm);
                    }
                    var fd = new FixedDepositAccount
                    {
                        CustomerID = customer.CustID,
                        StartDate = vm.FDStartDate.Value,
                        EndDate = vm.FDEndDate.Value,
                        DepositAmount = vm.FDDepositAmount.Value,
                        FDROI = vm.FDROI ?? 0m
                    };
                    db.FixedDepositAccounts.Add(fd);
                    db.SaveChanges();
                    TempData["Message"] = "Fixed deposit created.";
                    break;

                default:
                    ModelState.AddModelError(nameof(vm.AccountType), "Unknown account type.");
                    return View(vm);
            }
            if (vm.AllowAll)
            {
                return RedirectToRoute(new { controller = "Manager", action = "Index" });
            }
            else
            {
                return RedirectToAction("Index");
            }
        }


        [HttpGet]
        public ActionResult CloseAccount()
        {
            // populate account type dropdown based on deptid
            var deptId = (Session["deptid"] as string) ?? string.Empty;
            var types = GetAccountTypesForDept(deptId);
            ViewBag.AccountTypes = new SelectList(types, "Value", "Text");
            return View();
        }


        // Close an account (Savings/Loan/FD)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CloseAccount(string accountType, string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountType) || string.IsNullOrWhiteSpace(accountId))
            {
                TempData["Error"] = "Account type and account id are required.";
                return RedirectToAction("Index");
            }

            accountType = accountType.ToLowerInvariant();

            // Authorize based on employee department stored in session: 
            // - Dept01 can close Savings and FD
            // - Dept02 can close Loan only
            var deptId = (Session["deptid"] as string) ?? string.Empty;

            if (accountType == "savings" || accountType == "fd" || accountType == "fixeddeposit")
            {
                if (!string.Equals(deptId, "DEPT01", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "You are not authorized to close Savings or Fixed Deposit accounts.";
                    return RedirectToAction("Index");
                }
            }
            else if (accountType == "loan")
            {
                if (!string.Equals(deptId, "DEPT02", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "You are not authorized to close Loan accounts.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                TempData["Error"] = "Unknown account type.";
                return RedirectToAction("Index");
            }

            try
            {
                switch (accountType)
                {
                    case "savings":
                        var sAcc = db.SavingsAccounts.FirstOrDefault(s => s.SBAccountID == accountId);
                        if (sAcc == null)
                        {
                            TempData["Error"] = "Savings account not found.";
                            break;
                        }

                        if (sAcc.Balance != 0m)
                        {
                            TempData["Error"] = "Cannot close savings account with non-zero balance. Withdraw balance first.";
                            break;
                        }

                        var sTx = db.SavingsTransactions.Where(t => t.SBAccountID == accountId).ToList();
                        if (sTx.Any())
                        {
                            db.SavingsTransactions.RemoveRange(sTx);
                        }

                        db.SavingsAccounts.Remove(sAcc);
                        db.SaveChanges();
                        TempData["Message"] = "Savings account closed.";
                        break;

                    case "loan":
                        var lAcc = db.LoanAccounts.FirstOrDefault(l => l.LNAccountID == accountId);
                        if (lAcc == null)
                        {
                            TempData["Error"] = "Loan account not found.";
                            break;
                        }

                        decimal outstanding = GetLoanOutstanding(accountId, lAcc);
                        if (outstanding > 0m)
                        {
                            TempData["Error"] = "Cannot close loan account with outstanding balance. Collect payments first.";
                            break;
                        }

                        var lTx = db.LoanTransactions.Where(t => t.LNAccountID == accountId).ToList();
                        if (lTx.Any()) db.LoanTransactions.RemoveRange(lTx);

                        db.LoanAccounts.Remove(lAcc);
                        db.SaveChanges();
                        TempData["Message"] = "Loan account closed.";
                        break;

                    case "fd":
                    case "fixeddeposit":
                        var fdAcc = db.FixedDepositAccounts.FirstOrDefault(f => f.FDAccountID == accountId);
                        if (fdAcc == null)
                        {
                            TempData["Error"] = "Fixed deposit account not found.";
                            break;
                        }

                        if (fdAcc.EndDate > DateTime.Today)
                        {
                            TempData["Error"] = "Cannot close FD before maturity date.";
                            break;
                        }

                        var fdTx = db.FDTransactions.Where(t => t.FDAccountID == accountId).ToList();
                        if (fdTx.Any()) db.FDTransactions.RemoveRange(fdTx);

                        db.FixedDepositAccounts.Remove(fdAcc);
                        db.SaveChanges();
                        TempData["Message"] = "Fixed deposit closed.";
                        break;

                    default:
                        TempData["Error"] = "Unknown account type.";
                        break;
                }
            }
            catch (Exception ex)
            {
                // log exception (omitted) and show generic message
                TempData["Error"] = "An error occurred while closing account: " + ex.Message;
            }

            return RedirectToAction("Index");
        }


        // View transactions for an account (returns View with transactions in ViewBag)
        [HttpGet]
        public ActionResult ViewTransactions(string accountType = null, string accountId = null)
        {
            // populate account type dropdown based on dept
            var deptId = (Session["deptid"] as string) ?? string.Empty;
            var typesList = GetAccountTypesForDept(deptId);
            ViewBag.AccountTypes = new SelectList(typesList, "Value", "Text");

            // If parameters are missing, render a page with a form so user can provide them.
            if (string.IsNullOrWhiteSpace(accountType) || string.IsNullOrWhiteSpace(accountId))
            {
                ViewBag.Transactions = null;
                ViewBag.AccountType = null;
                ViewBag.AccountId = null;
                return View();
            }

            accountType = accountType.ToLowerInvariant();

            switch (accountType)
            {
                case "savings":
                    var sTx = db.SavingsTransactions.Where(t => t.SBAccountID == accountId)
                        .OrderByDescending(t => t.TransactionDate).ToList();
                    ViewBag.Transactions = sTx;
                    break;
                case "loan":
                    var lTx = db.LoanTransactions.Where(t => t.LNAccountID == accountId)
                        .OrderByDescending(t => t.EMIDate_Actual).ToList();
                    ViewBag.Transactions = lTx;
                    break;
                case "fd":
                case "fixeddeposit":
                    var fdTx = db.FDTransactions.Where(t => t.FDAccountID == accountId)
                        .OrderByDescending(t => t.TransactionDate).ToList();
                    ViewBag.Transactions = fdTx;
                    break;
                default:
                    TempData["Error"] = "Unknown account type.";
                    return RedirectToAction("Index");
            }

            ViewBag.AccountType = accountType;
            ViewBag.AccountId = accountId;
            return View();
        }


        [HttpGet]
        public ActionResult Deposit()
        {
            var deptId = (Session["deptid"] as string) ?? string.Empty;
            var types = GetAccountTypesForDept(deptId);
            ViewBag.AccountTypes = new SelectList(types, "Value", "Text");
            return View();
        }


        // Deposit (Savings deposit or Loan payment)
        // Deposit (Savings deposit or Loan payment)
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Deposit(string accountType, string accountId, decimal amount)
        {

            switch (accountType)
            {
                case "Savings":
                    if (string.IsNullOrWhiteSpace(accountId))

                    {

                        ViewData["msg"] = "Please provide an account ID.";

                        ViewData["cls"] = "text-danger";
                        break;
                        return View();

                    }

                    if (amount < 100)

                    {

                        ViewData["msg"] = "Deposit amount must be greater than 100.";

                        ViewData["cls"] = "text-danger";

                        break;
                        return View();

                    }

                    // find existing savings account

                    var account = db.SavingsAccounts.FirstOrDefault(s => s.SBAccountID == accountId);

                    if (account == null)

                    {

                        ViewData["msg"] = "Savings account not found.";

                        ViewData["cls"] = "text-danger";

                        break;
                        return View();

                    }

                    // update balance

                    account.Balance += amount;

                    // create transaction entry

                    var tx = new SavingsTransaction

                    {


                        SBAccountID = accountId,

                        TransactionDate = DateTime.Now,

                        TransactionType = "D",

                        Amount = amount

                    };

                    db.SavingsTransactions.Add(tx);

                    db.SaveChanges();
                    break;
                case "Loan":
                    if (string.IsNullOrWhiteSpace(accountId))

                    {

                        ViewData["msg"] = "Please provide an account ID.";

                        ViewData["cls"] = "text-danger";

                        break;

                    }

                    if (amount < 100)

                    {

                        ViewData["msg"] = "Deposit amount must be greater than 100.";

                        ViewData["cls"] = "text-danger";

                        break;

                    }

                    // find existing savings account

                    var accounts = db.LoanAccounts.FirstOrDefault(s => s.LNAccountID == accountId);
                    if (accounts == null) { TempData["Error"] = "Loan account not found."; break; }

                    if (accounts == null)

                    {

                        ViewData["msg"] = "Savings account not found.";

                        ViewData["cls"] = "text-danger";

                        break;

                    }

                    // update balance

                    accounts.LoanAmount -= amount;

                    // create transaction entry
                    decimal outstanding = GetLoanOutstanding(accountId, accounts);
                    decimal newOutstanding = Math.Max(0m, outstanding - amount);
                    var stx = new LoanTransaction

                    {
                        LNAccountID = accountId,
                        EMIDate_Actual = DateTime.Now,
                        EMIDate_Paid = DateTime.Now,
                        LatePenalty = 0m,
                        Amount = amount,
                        Outstanding = newOutstanding

                    };

                    db.LoanTransactions.Add(stx);

                    db.SaveChanges();
                    break;



            }
            return RedirectToAction("Index");
        }




        [HttpGet]
        public ActionResult Withdraw()
        {
            var deptId = (Session["deptid"] as string) ?? string.Empty;
            var types = GetAccountTypesForDept(deptId);
            ViewBag.AccountTypes = new SelectList(types, "Value", "Text");
            return View();
        }

        // Withdraw (Savings withdrawal, FD withdrawal if matured)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Withdraw(string accountType, string accountId, decimal amount)
        {

            switch (accountType)
            {
                case "Savings":
                    if (string.IsNullOrWhiteSpace(accountId))

                    {

                        ViewData["msg"] = "Please provide an account ID.";

                        ViewData["cls"] = "text-danger";

                        break;

                    }

                    else if (amount < 100)

                    {

                        ViewData["msg"] = "Deposit amount must be greater than 100.";

                        ViewData["cls"] = "text-danger";
                        break;


                    }
                    else
                    {


                        // find existing savings account

                        var account = db.SavingsAccounts.FirstOrDefault(s => s.SBAccountID == accountId);

                        if (account == null)

                        {

                            ViewData["msg"] = "Savings account not found.";

                            ViewData["cls"] = "text-danger";

                            break;

                        }
                        else
                        {

                            // update balance

                            account.Balance -= amount;

                            // create transaction entry

                            var tx = new SavingsTransaction

                            {
                                //TransactionID = "TX-" + Guid.NewGuid().ToString("N"),
                                SBAccountID = accountId,
                                TransactionDate = DateTime.Now,
                                TransactionType = "W",
                                Amount = amount

                            };

                            db.SavingsTransactions.Add(tx);
                            db.SaveChanges();
                            break;
                        }
                    }
            }
            return RedirectToAction("Index");
        }

        // Helper: compute outstanding for loan account (prefer last transaction Outstanding, otherwise LoanAmount - sum(payments))
        private decimal GetLoanOutstanding(string lnAccountId, LoanAccount lnAccount = null)
        {
            if (lnAccount == null)
            {
                lnAccount = db.LoanAccounts.FirstOrDefault(l => l.LNAccountID == lnAccountId);
                if (lnAccount == null) return 0m;
            }

            // if transactions exist, use latest Outstanding stored on last transaction
            var lastTx = db.LoanTransactions
                .Where(t => t.LNAccountID == lnAccountId)
                .OrderByDescending(t => t.EMIDate_Actual)
                .FirstOrDefault();

            if (lastTx != null)
            {
                return lastTx.Outstanding;
            }

            // otherwise compute as LoanAmount - sum of payments recorded (if any)
            var payments = db.LoanTransactions
                .Where(t => t.LNAccountID == lnAccountId)
                .Sum(t => (decimal?)t.Amount) ?? 0m;

            return Math.Max(0m, lnAccount.LoanAmount - payments);
        }

        // Helper to build account-type options based on deptid
        private List<SelectListItem> GetAccountTypesForDept(string deptId)
        {
            var types = new List<SelectListItem>();
            if (string.Equals(deptId, "DEPT01", StringComparison.OrdinalIgnoreCase))
            {
                types.Add(new SelectListItem { Value = "Savings", Text = "Savings" });
                types.Add(new SelectListItem { Value = "FD", Text = "Fixed Deposit" });
            }
            else if (string.Equals(deptId, "DEPT02", StringComparison.OrdinalIgnoreCase))
            {
                types.Add(new SelectListItem { Value = "Loan", Text = "Loan" });
            }
            else
            {
                types.Add(new SelectListItem { Value = "Savings", Text = "Savings" });
                types.Add(new SelectListItem { Value = "FD", Text = "Fixed Deposit" });
                types.Add(new SelectListItem { Value = "Loan", Text = "Loan" });
            }
            // fallback: no types (view will show "No account types available" if needed)
            if (types.Count > 0)
                types.Insert(0, new SelectListItem { Value = "", Text = "Select..." });
            else
                types.Add(new SelectListItem { Value = "", Text = "No account types available" });
            return types;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}