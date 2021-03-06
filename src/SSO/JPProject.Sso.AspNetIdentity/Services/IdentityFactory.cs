using JPProject.Sso.AspNetIdentity.Models.Identity;
using JPProject.Sso.Domain.Commands.User;
using JPProject.Sso.Domain.Commands.UserManagement;
using JPProject.Sso.Domain.Interfaces;

namespace JPProject.Sso.AspNetIdentity.Services
{
    public class IdentityFactory : IIdentityFactory<UserIdentity>, IRoleFactory<RoleIdentity>
    {
        public UserIdentity Create(UserCommand user)
        {
            return new UserIdentity
            {
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                UserName = user.Username,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnd = null,
            };
        }

        public RoleIdentity CreateRole(string name)
        {
            return new RoleIdentity(name);
        }


        public void UpdateInfo(AdminUpdateUserCommand command, UserIdentity userDb)
        {
            userDb.Email = command.Email;
            userDb.EmailConfirmed = command.EmailConfirmed;
            userDb.AccessFailedCount = command.AccessFailedCount;
            userDb.LockoutEnabled = command.LockoutEnabled;
            userDb.LockoutEnd = command.LockoutEnd;
            userDb.TwoFactorEnabled = command.TwoFactorEnabled;
            userDb.PhoneNumber = command.PhoneNumber;
            userDb.PhoneNumberConfirmed = command.PhoneNumberConfirmed;
        }

        public void UpdateProfile(UpdateProfileCommand command, UserIdentity user)
        {
            user.PhoneNumber = command.PhoneNumber;
        }

    }
}
