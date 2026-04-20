using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public virtual ICollection<RoleModuleFunction> RoleModuleFunctions { get; set; } = new List<RoleModuleFunction>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
