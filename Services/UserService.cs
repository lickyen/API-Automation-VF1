using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class UserService 
    {
        private readonly UserRepository _userRepository;

        public UserService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public UserModel? GetUser(string userId)
        {
            return _userRepository.GetUser(userId);
        }

        public List<UserModel> GetAllUsers()
        {
            return _userRepository.GetAllUsers();
        }

        public int AddUser(AddUserDTO addUserDTO)
        {
            return _userRepository.AddUser(addUserDTO);
        }

        public int UpdateUser(UpdateUserDTO updateUserDTO)
        {
            return _userRepository.UpdateUser(updateUserDTO);
        }

        public int DisableUser(DisableUserDTO disableUserDTO)
        {

            return _userRepository.DisableUser(disableUserDTO);
        }

        public int ActivateUser(ActivateUserDTO activateUserDTO)
        {
            return _userRepository.ActivateUser(activateUserDTO);
        }

        public List<ModuleDto> GetModulesByUser(string user)
        {
            return _userRepository.GetModulesByUser(user);
        }

        public List<string> GetRoles()
        {
            return _userRepository.GetRoles();
        }
    }
}
