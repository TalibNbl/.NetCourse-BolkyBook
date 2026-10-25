using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BulkyBook.Models
{
    public class OrderHeader
    {
        public int Id { get; set; }

        public string ApplicationUserID { get; set; }
        [ForeignKey("ApplicationUserID")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime ShippingDate { get; set; }

        public double OrderTotal { get; set; }
        public string? OrderStatus { get; set; }

        public string? SessionID {  get; set; }
        public string? PaymentIntentID { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Carrier {  get; set; }

        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string StreetAdrress { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        [Required]
        public string PostalCode { get; set; }
        [Required]
        public string Name { get; set; }

        [ValidateNever]
        public IEnumerable<OrderDetails> OrderDetails { get; set; }
    }
}
