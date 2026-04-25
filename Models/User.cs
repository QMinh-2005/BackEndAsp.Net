using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class User
{
    public int UserId { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public byte[]? Salt { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
