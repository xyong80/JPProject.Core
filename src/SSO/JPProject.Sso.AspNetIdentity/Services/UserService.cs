using AspNetCore.IQueryable.Extensions;
using AspNetCore.IQueryable.Extensions.Filter;
using AspNetCore.IQueryable.Extensions.Pagination;
using AspNetCore.IQueryable.Extensions.Sort;
using IdentityModel;
using JPProject.Domain.Core.Bus;
using JPProject.Domain.Core.Interfaces;
using JPProject.Domain.Core.Notifications;
using JPProject.Domain.Core.Util;
using JPProject.EntityFrameworkCore.Interfaces;
using JPProject.Sso.Domain.Commands.User;
using JPProject.Sso.Domain.Commands.UserManagement;
using JPProject.Sso.Domain.Interfaces;
using JPProject.Sso.Domain.Models;
using JPProject.Sso.Domain.ViewModels.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JPProject.Sso.AspNetIdentity.Services
{
    public class UserService<TUser, TRole, TKey> : IUserService
        where TUser : IdentityUser<TKey>, IDomainUser
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly UserManager<TUser> _userManager;
        private readonly IMediatorHandler _bus;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly IIdentityFactory<TUser> _userFactory;
        private readonly IJpEntityFrameworkStore _store;

        public UserService(
            UserManager<TUser> userManager,
            IMediatorHandler bus,
            ILoggerFactory loggerFactory,
            IConfiguration config,
            IIdentityFactory<TUser> userFactory,
            IJpEntityFrameworkStore store)
        {
            _userManager = userManager;
            _bus = bus;
            _config = config;
            _userFactory = userFactory;
            _store = store;

            _logger = loggerFactory.CreateLogger<UserService<TUser, TRole, TKey>>();
        }

        public Task<AccountResult?> CreateUserWithPass(RegisterNewUserCommand command, string password)
        {
            var user = _userFactory.Create(command);
            return CreateUser(user, command, password, null, null);
        }

        public Task<AccountResult?> CreateUserWithouthPassword(RegisterNewUserWithoutPassCommand command)
        {
            var user = _userFactory.Create(command);
            return CreateUser(user, command, null, command.Provider, command.ProviderId);
        }

        public Task<AccountResult?> CreateUserWithProviderAndPass(RegisterNewUserWithProviderCommand command)
        {
            var user = _userFactory.Create(command);
            return CreateUser(user, command, command.Password, command.Provider, command.ProviderId);
        }

        private async Task<AccountResult?> CreateUser(TUser user, UserCommand command, string password, string provider, string providerId)
        {
            IdentityResult result;

            if (provider.IsPresent())
            {
                var userByProvider = await _userManager.FindByLoginAsync(provider, providerId);
                if (userByProvider != null)
                    await _bus.RaiseEvent(new DomainNotification("New User", $"User already taken with {provider}"));
            }

            if (password.IsMissing())
                result = await _userManager.CreateAsync(user);
            else
                result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // User claim for write customers data
                //await _userManager.AddClaimAsync(newUser, new Claim("User", "Write"));

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = $"{_config.GetValue<string>("ApplicationSettings:UserManagementURL")}/confirm-email?userId={ user.Email.UrlEncode()}&code={code.UrlEncode()}";

                await AddClaims(user, command);

                if (!string.IsNullOrWhiteSpace(provider))
                    await AddLoginAsync(user, provider, providerId);


                if (password.IsPresent())
                    _logger.LogInformation("User created a new account with password.");

                if (provider.IsPresent())
                    _logger.LogInformation($"Provider {provider} associated.");
                return new AccountResult(user.UserName, code, callbackUrl);
            }

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return null;
        }

        /// <summary>
        /// Add custom claims here
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task AddClaims(TUser user, UserCommand command)
        {
            _logger.LogInformation("Begin include claims");
            var claims = new List<Claim>();
            _logger.LogInformation(command.ToJson());
            if (command.Picture.IsPresent())
                claims.Add(new Claim(JwtClaimTypes.Picture, command.Picture));

            if (command.Name.IsPresent())
                claims.Add(new Claim(JwtClaimTypes.GivenName, command.Name));

            if (command.Birthdate.HasValue)
                claims.Add(new Claim(JwtClaimTypes.BirthDate, command.Birthdate.Value.ToString(CultureInfo.CurrentCulture)));

            if (command.SocialNumber.IsPresent())
                claims.Add(new Claim("social_number", command.SocialNumber));

            if (claims.Any())
            {
                var result = await _userManager.AddClaimsAsync(user, claims);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Claim created successfull.");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
                    }
                }
            }



        }

        public async Task<bool> UsernameExist(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            return user != null;
        }

        public async Task<bool> EmailExist(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null;
        }


        public async Task<AccountResult?> GenerateResetPasswordLink(string emailOrUsername)
        {
            var user = await GetUserByEmailOrUsername(emailOrUsername);
            if (user == null)
                return null;


            // For more information on how to enable account confirmation and password reset please
            // visit https://go.microsoft.com/fwlink/?LinkID=532713
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = $"{_config.GetValue<string>("ApplicationSettings:UserManagementURL")}/reset-password?email={user.Email.UrlEncode()}&code={code.UrlEncode()}";

            //await _emailService.SendEmailAsync(user.Email, "Reset Password", $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
            //_logger.LogInformation("Reset link sended to userId.");

            return new AccountResult(user.UserName, code, callbackUrl);
        }

        public async Task<string> ConfirmEmailAsync(string email, string code)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                await _bus.RaiseEvent(new DomainNotification("Email", $"Unable to load userId with ID '{email}'."));
                return null;
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
                return user.UserName;

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return null;
        }



        public async Task<bool> UpdateProfileAsync(UpdateProfileCommand command)
        {
            var user = await _userManager.FindByNameAsync(command.Username);
            _userFactory.UpdateProfile(command, user);

            var claims = await _userManager.GetClaimsAsync(user);

            await AddOrUpdateClaim(claims, user, JwtClaimTypes.GivenName, command.Name);
            await AddOrUpdateClaim(claims, user, JwtClaimTypes.WebSite, command.Url);

            if (command.Birthdate.HasValue)
                await AddOrUpdateClaim(claims, user, JwtClaimTypes.BirthDate, command.Birthdate.Value.ToString(CultureInfo.CurrentCulture));

            await AddOrUpdateClaim(claims, user, "company", command.Company);
            await AddOrUpdateClaim(claims, user, "job_title", command.JobTitle);
            await AddOrUpdateClaim(claims, user, "bio", command.Bio);
            await AddOrUpdateClaim(claims, user, "social_number", command.SocialNumber);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return true;

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return false;
        }

        private async Task<IdentityResult> AddOrUpdateClaim(IEnumerable<Claim> claims, TUser user, string type, string value)
        {
            var pictureClaim = claims.Of(type);
            var newPictureClaim = new Claim(type, value);
            if (pictureClaim != null)
            {
                return await _userManager.ReplaceClaimAsync(user, pictureClaim, newPictureClaim);
            }
            else
            {
                return await _userManager.AddClaimAsync(user, newPictureClaim);
            }
        }

        public async Task<bool> UpdateProfilePictureAsync(UpdateProfilePictureCommand command)
        {
            var user = await _userManager.FindByNameAsync(command.Username);
            var claims = await _userManager.GetClaimsAsync(user);

            IdentityResult result;
            var pictureClaim = claims.Of(JwtClaimTypes.Picture);
            var newPictureClaim = new Claim(JwtClaimTypes.Picture, command.Picture);
            if (pictureClaim != null)
            {
                result = await _userManager.ReplaceClaimAsync(user, pictureClaim, newPictureClaim);
            }
            else
            {
                result = await _userManager.AddClaimAsync(user, newPictureClaim);
            }

            if (result.Succeeded)
                return true;

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return false;
        }


        public async Task<bool> UpdateUserAsync(AdminUpdateUserCommand command)
        {
            var user = await _userManager.FindByNameAsync(command.Username);
            _userFactory.UpdateInfo(command, user);

            var claims = await _userManager.GetClaimsAsync(user);

            if (command.Name.IsPresent())
                await AddOrUpdateClaim(claims, user, JwtClaimTypes.GivenName, command.Name);

            if (command.Birthdate.HasValue)
                await AddOrUpdateClaim(claims, user, JwtClaimTypes.BirthDate, command.Birthdate.Value.ToString(CultureInfo.CurrentCulture));

            if (command.SocialNumber.IsPresent())
                await AddOrUpdateClaim(claims, user, "social_number", command.SocialNumber);

            var resut = await _userManager.UpdateAsync(user);
            if (!resut.Succeeded)
            {
                foreach (var error in resut.Errors)
                {
                    await _bus.RaiseEvent(new DomainNotification("User", error.Description));
                }

                return false;
            }

            return true;
        }

        public async Task<bool> CreatePasswordAsync(SetPasswordCommand request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword)
            {
                /*
                 * DO NOT display the reason.
                 * if this happen is because userId are trying to hack.
                 */
                throw new Exception("Unknown error");
            }

            var result = await _userManager.AddPasswordAsync(user, request.Password);
            if (result.Succeeded)
                return true;

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return false;
        }

        public async Task<bool> RemoveAccountAsync(RemoveAccountCommand request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return true;

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return false;
        }

        public async Task<bool> HasPassword(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            return await _userManager.HasPasswordAsync(user);
        }


        private async Task AddLoginAsync(TUser user, string provider, string providerUserId)
        {
            var result = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerUserId, provider));

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }
        }

        public async Task<IDomainUser> FindByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user;
        }

        public async Task<IDomainUser> FindByNameAsync(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            return user;
        }

        public async Task<IDomainUser> FindByProviderAsync(string provider, string providerUserId)
        {
            var user = await _userManager.FindByLoginAsync(provider, providerUserId);
            return user;
        }

        public async Task<IEnumerable<Claim>> GetClaimByName(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            var claims = await _userManager.GetClaimsAsync(user);

            return claims;
        }

        public async Task<bool> SaveClaim(string username, Claim claim)
        {
            var user = await _userManager.FindByNameAsync(username);
            var result = await _userManager.AddClaimAsync(user, claim);

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return result.Succeeded;
        }

        public async Task<bool> RemoveClaim(string username, string claimType, string value)
        {
            var user = await _userManager.FindByNameAsync(username);
            var claims = await _userManager.GetClaimsAsync(user);

            var claimToRemove = value.IsMissing() ?
                                    claims.First(c => c.Type.Equals(claimType)) :
                                    claims.First(c => c.Type.Equals(claimType) && c.Value.Equals(value));

            var result = await _userManager.RemoveClaimAsync(user, claimToRemove);

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return result.Succeeded;
        }

        public async Task<IEnumerable<string>> GetRoles(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> RemoveRole(string username, string requestRole)
        {
            var user = await _userManager.FindByNameAsync(username);
            var result = await _userManager.RemoveFromRoleAsync(user, requestRole);

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return result.Succeeded;
        }


        public async Task<bool> SaveRole(string username, string role)
        {
            var user = await _userManager.FindByNameAsync(username);
            var result = await _userManager.AddToRoleAsync(user, role);

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return result.Succeeded;
        }

        public async Task<IEnumerable<UserLogin>> GetUserLogins(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            var logins = await _userManager.GetLoginsAsync(user);
            return logins.Select(a => new UserLogin(a.LoginProvider, a.ProviderDisplayName, a.ProviderKey));
        }

        public async Task<bool> RemoveLogin(string username, string loginProvider, string providerKey)
        {
            var user = await _userManager.FindByNameAsync(username);
            var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return result.Succeeded;
        }

        public async Task<IEnumerable<IDomainUser>> GetUserFromRole(string role)
        {
            return (await _userManager.GetUsersInRoleAsync(role));//.Select(GetUser);
        }

        public async Task<bool> RemoveUserFromRole(string name, string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            var result = await _userManager.RemoveFromRoleAsync(user, name);
            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return result.Succeeded;
        }

        public async Task<bool> ResetPasswordAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, password);
            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return result.Succeeded;
        }

        public async Task<string> ResetPassword(string email, string code, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the userId does not exist
                return null;
            }

            var result = await _userManager.ResetPasswordAsync(user, code, password);

            if (result.Succeeded)
            {
                var emailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
                if (!emailConfirmed)
                {
                    user.ConfirmEmail();
                    await _userManager.UpdateAsync(user);

                }
                _logger.LogInformation("Password reseted successfull.");
                return user.UserName;
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
                }
            }

            return null;
        }


        public async Task<bool> ChangePasswordAsync(ChangePasswordCommand request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.Password);
            if (result.Succeeded)
                return true;

            foreach (var error in result.Errors)
            {
                await _bus.RaiseEvent(new DomainNotification(result.ToString(), error.Description));
            }

            return false;
        }
        /// <summary>
        /// Add login for user, if success return his username
        /// </summary>
        /// <param name="email">email</param>
        /// <param name="provider">provider: eg, Google, Facebook</param>
        /// <param name="providerId">Unique identifier from provider</param>
        /// <returns></returns>
        public async Task<string> AddLoginAsync(string email, string provider, string providerId)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return null;

            await AddLoginAsync(user, provider, providerId);

            return user.UserName;
        }

        public async Task<IDomainUser> FindByUsernameOrEmail(string emailOrUsername)
        {
            var user = await GetUserByEmailOrUsername(emailOrUsername);
            return user;
        }

        private async Task<TUser> GetUserByEmailOrUsername(string emailOrUsername)
        {
            TUser user;
            if (emailOrUsername.IsEmail())
                user = await _userManager.FindByEmailAsync(emailOrUsername);
            else
                user = await _userManager.FindByNameAsync(emailOrUsername);
            return user;
        }
        public async Task<int> CountByProperties(string query)
        {
            return await (from user in _store.Set<TUser>().AsQueryable()
                          join claim in _store.Set<IdentityUserClaim<TKey>>().AsQueryable() on user.Id equals claim.UserId into userClaims
                          from c in userClaims.DefaultIfEmpty()
                          where
                              (user.UserName.Contains(query) ||
                               user.Email.Contains(query) ||
                               c.ClaimValue.Contains(query)) ||
                              query == null
                          select
                              user.UserName)
                          .Distinct()
                          .CountAsync();
        }

        public async Task<IEnumerable<IDomainUser>> SearchByProperties(string query, IQuerySort sort, IQueryPaging paging)
        {
            var usersId = await (
                    from user in _store.Set<TUser>().AsQueryable()
                    join claim in _store.Set<IdentityUserClaim<TKey>>().AsQueryable() on user.Id equals claim.UserId into userClaims
                    from c in userClaims.DefaultIfEmpty()
                    where
                        (user.UserName.Contains(query) ||
                         user.Email.Contains(query) ||
                         c.ClaimValue.Contains(query)) ||
                         query == null
                    group user by user.Id)
                .Sort(sort)
                .Paginate(paging)
                .Select(s => s.Key)
                .ToListAsync();

            return _userManager.Users.Where(w => usersId.Contains(w.Id));
        }
        public Task<int> Count(ICustomQueryable search)
        {
            return _userManager.Users.Filter(search).CountAsync();
        }

        public async Task<IEnumerable<IDomainUser>> Search(ICustomQueryable search)
        {
            var users = await _userManager.Users.Apply(search).ToListAsync();
            return users;
        }

        public async Task<Dictionary<Username, IEnumerable<Claim>>> GetClaimsFromUsers(IEnumerable<string> username, params string[] claimType)
        {
            var claims = await (from claim in _store.Set<IdentityUserClaim<TKey>>().AsQueryable()
                                join user in _store.Set<TUser>().AsQueryable() on claim.UserId equals user.Id
                                where username.Contains(user.UserName) && ((claimType != null && claimType.Contains(claim.ClaimType)) || claimType == null)
                                select new { user.UserName, claim }).ToListAsync();

            var dictionary = new Dictionary<Username, IEnumerable<Claim>>();

            foreach (var user in username)
            {
                dictionary.Add(user, claims.Where(w => w.UserName == user).Select(s => s.claim.ToClaim()));
            }

            return dictionary;
        }

    }
}
