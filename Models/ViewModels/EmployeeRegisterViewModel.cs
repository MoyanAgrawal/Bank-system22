using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BankSystem.Models.ViewModels
{
    public class EmployeeRegisterViewModel
    {

        [Required]
        [Display(Name = "Employee Name")]
        public string Empname { get; set; }

        [Required]
        [Display(Name = "Department Name")]
        public string Deptid { get; set; }

       
        public string Emptype { get; set; } // "M" or "E"
        public SelectList DeptList { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        [Display(Name = "Password")]
        public string Epassword { get; set; }

        [Required]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "PAN must be 8 characters.")]
        [RegularExpression(@"^[A-Za-z]{4}\d{4}$", ErrorMessage = "PAN must start with 4 letters followed by 4 digits.")]
        [Display(Name = "PAN")]
        public string PAN { get; set; }
    }
}