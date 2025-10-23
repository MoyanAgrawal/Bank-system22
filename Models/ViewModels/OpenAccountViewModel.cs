
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BankSystem.Models.ViewModels
{
    public class OpenAccountViewModel
    {
        public int SelectedCustNum { get; set; }
        public SelectList CustomerList { get; set; }

        // which of "Savings", "Loan", "FD"
        public string AccountType { get; set; }

        // account type select list populated by controller
        public SelectList AccountTypeList { get; set; }

        // flags computed by controller (used in view to render allowed sections)
        public bool AllowSavings { get; set; }
        public bool AllowLoan { get; set; }
        public bool AllowFD { get; set; }
        public bool AllowAll{ get; set; }

        // NEW: when a customer is selected by PAN we show readonly display instead of dropdown
        public bool IsCustomerSelected { get; set; }
        public string CustomerDisplay { get; set; }

        // Savings
        public decimal? SavingsInitialDeposit { get; set; }

        // Loan
        public decimal? LoanAmount { get; set; }
        public DateTime? LoanStartDate { get; set; }
        public int? LoanTenureMonths { get; set; }
        public decimal? MonthlyTakeHome { get; set; }
        public decimal? LoanROI { get; set; }

        // FD
        public DateTime? FDStartDate { get; set; }
        public DateTime? FDEndDate { get; set; }
        public decimal? FDDepositAmount { get; set; }
        public decimal? FDROI { get; set; }
    }
}