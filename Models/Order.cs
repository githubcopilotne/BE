using System;
using System.Collections.Generic;

namespace BE.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int? VoucherId { get; set; }

    public decimal? DiscountAmount { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Address { get; set; } = null!;

    public int? ProvinceId { get; set; }

    public string? ProvinceName { get; set; }

    public int? DistrictId { get; set; }

    public string? DistrictName { get; set; }

    public string? WardCode { get; set; }

    public string? WardName { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal TotalMoney { get; set; }

    public int PaymentMethod { get; set; }

    public int PaymentStatus { get; set; }

    public int Status { get; set; }

    public string? Note { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? PaymentExpireAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User User { get; set; } = null!;

    public virtual Voucher? Voucher { get; set; }
}
