using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BankSystem.Models.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public Customer Customer { get; set; }
        public IEnumerable<SavingsAccount> SavingsAccounts { get; set; }
        public IEnumerable<SavingsTransaction> SavingsTransactions { get; set; }
        public IEnumerable<LoanAccount> LoanAccounts { get; set; }
        public IEnumerable<LoanTransaction> LoanTransactions { get; set; }
        public IEnumerable<FixedDepositAccount> FDAccounts { get; set; }
        public IEnumerable<FDTransaction> FDTransactions { get; set; }
    }
}