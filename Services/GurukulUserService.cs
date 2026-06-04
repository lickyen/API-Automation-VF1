using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class GurukulUserService
    {
        private readonly GurukulUserRepository _repository;

        public GurukulUserService(GurukulUserRepository repository)
        {
            _repository = repository;
        }

        public List<GurukulUserModel> GetAllUsers(string? status)
        {
            return _repository.GetAllUsers(status);
        }

        public GurukulUserModel? GetUser(string userId)
        {
            return _repository.GetUser(userId);
        }

        public int AddUser(AddGurukulUserDTO user)
        {
            return _repository.AddUser(user);
        }

        public int UpdateUser(UpdateGurukulUserDTO user)
        {
            return _repository.UpdateUser(user);
        }

        public int DisableUser(DisableUserDTO user)
        {
            return _repository.DisableUser(user);
        }

        public int ActivateUser(ActivateUserDTO user)
        {
            return _repository.ActivateUser(user);
        }
    }
}