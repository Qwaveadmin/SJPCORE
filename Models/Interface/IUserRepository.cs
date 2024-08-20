using SJPCORE.Models.Attribute;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SJPCORE.Models.Interface
{
    public interface IUserRepository
    {
        Task<List<UserModel>> GetUsersAsync();
        Task<UserModel> GetUserByIdAsync(string id);
        Task UpdateUserAsync(UserModel user);
        Task DeleteUserAsync(UserModel user);
    }
    public class UserRepository : IUserRepository
    {
        private readonly List<UserModel> _users;

        public UserRepository()
        {
            _users = new List<UserModel>();
        }

        public async Task<List<UserModel>> GetUsersAsync()
        {
            return _users;
        }


        public async Task<UserModel> GetUserByIdAsync(string id)
        {
            return _users.FirstOrDefault(u => u.Id == id);
        }

        public async Task UpdateUserAsync(UserModel user)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existingUser != null)
            {
                existingUser.Username = user.Username;
                existingUser.Password = user.Password;
                existingUser.Role = user.Role;
            }
        }

        public async Task DeleteUserAsync(UserModel user)
        {
            _users.Remove(user);
        }
    }
}
