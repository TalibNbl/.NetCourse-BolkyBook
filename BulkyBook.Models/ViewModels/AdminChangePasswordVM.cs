using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BulkyBook.Models.ViewModels
{
    public class AdminChangePasswordVM
    {
        public string UserId { get; set; }

        [Display(Name ="User Email")]
        public string UserEmail { get; set; }

        [Display(Name = "User Name")]
        [ValidateNever]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name ="Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The passowrd and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
