using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class UserProfile
{
    public int ProfileId { get; set; }

    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? DetailedAddress { get; set; }

    public virtual User User { get; set; } = null!;
}
