using Core.Models;

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
