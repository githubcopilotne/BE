using System;
using System.Collections.Generic;

namespace BE.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? GoogleId { get; set; }

    public string Email { get; set; } = null!;

    public string? Password { get; set; }

    public string Role { get; set; } = null!;

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public int? Gender { get; set; }

    public DateOnly? Birthday { get; set; }

    public string? Address { get; set; }

    public int? ProvinceId { get; set; }

    public string? ProvinceName { get; set; }

    public int? DistrictId { get; set; }

    public string? DistrictName { get; set; }

    public string? WardCode { get; set; }

    public string? WardName { get; set; }

    public string? EmployeeCode { get; set; }

    public string? IdCard { get; set; }

    public DateOnly? HireDate { get; set; }

    public DateOnly? LeaveDate { get; set; }

    public string? PersonalEmail { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
