using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class Module
{
    public int ModuleId { get; set; }

    public string ModuleName { get; set; } = null!;

    public virtual ICollection<RoleModuleFunction> RoleModuleFunctions { get; set; } = new List<RoleModuleFunction>();
}
