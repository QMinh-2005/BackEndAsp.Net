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

    public byte[]? Salt { get; set; }

    public virtual ICollection<AdminAuditLog> AdminAuditLogs { get; set; } = new List<AdminAuditLog>();

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();

    public virtual ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
