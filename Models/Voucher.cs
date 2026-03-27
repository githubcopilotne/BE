using System;
using System.Collections.Generic;

namespace BE.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string VoucherCode { get; set; } = null!;

    public int DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public decimal? MinOrderValue { get; set; }

    public int UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public DateTime ExpiryDate { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
