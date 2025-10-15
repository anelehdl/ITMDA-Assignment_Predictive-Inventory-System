using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IRoleService
    {
        //added roles respective to the services
        Task<List<Role>> GetAllRolesAsync();
        Task<Role?> GetRoleByIdAsync(string roleId);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task<bool> HasPermissionAsync(string roleId, string permission);
    }
}
