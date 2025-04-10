using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Business.Abstract;
using Business.Constants;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.Security.GitHub;
using Core.Utilities.Security.Hashing;
using Core.Utilities.Security.Jwt;
using Entities.Dtos;

namespace Business.Concrete
{
    public class AuthManager:IAuthService
    {
        private IUserService _userService;
        private ITokenHelper _tokenHelper;
        private IGitHubAuthHelper _gitHubAuthHelper;

        public AuthManager(IUserService userService, ITokenHelper tokenHelper, IGitHubAuthHelper gitHubAuthHelper)
        {
            _userService = userService;
            _tokenHelper = tokenHelper;
            _gitHubAuthHelper = gitHubAuthHelper;
        }

        public IDataResult<User> Register(UserForRegisterDto userForRegisterDto, string password)
        {
            byte[] passwordHash, passwordSalt;
            HashingHelper.CreatePasswordHash(password,out passwordHash,out passwordSalt);
            var user = new User
            {
                Email = userForRegisterDto.Email,
                FirstName = userForRegisterDto.FirstName,
                LastName = userForRegisterDto.LastName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Status = true
            };
            _userService.Add(user);
            return  new SuccessDataResult<User>(user,Messages.UserRegistered);
        }

        public IDataResult<User> Login(UserForLoginDto userForLoginDto)
        {
            var userToCheck = _userService.GetByMail(userForLoginDto.Email);
            if (userToCheck==null)
            {
                return new ErrorDataResult<User>(Messages.UserNotFound);
            }

            if (!HashingHelper.VerifyPasswordHash(userForLoginDto.Password,userToCheck.PasswordHash,userToCheck.PasswordSalt))
            {
                return new ErrorDataResult<User>(Messages.PasswordError);
            }

            return new SuccessDataResult<User>(userToCheck,Messages.SuccessfulLogin);
        }

        public IResult UserExists(string email)
        {
            if (_userService.GetByMail(email)!=null)
            {
                return new ErrorResult(Messages.UserAlreadyExists);
            }
            return new SuccessResult();
        }

        public IDataResult<AccessToken> CreateAccessToken(User user)
        {
            var claims = _userService.GetClaims(user);
            var accessToken = _tokenHelper.CreateToken(user, claims);
            return new SuccessDataResult<AccessToken>(accessToken,Messages.AccessTokenCreated);
        }

        public string GetGitHubAuthorizationUrl(string state = null)
        {
            return _gitHubAuthHelper.GetAuthorizationUrl(state);
        }

        public IDataResult<User> GitHubLogin(UserForGitHubLoginDto userForGitHubLoginDto)
        {
            try
            {
                // Get GitHub user info using the provided code
                var gitHubUserInfo = _gitHubAuthHelper.GetUserInfoAsync(userForGitHubLoginDto.Code).GetAwaiter().GetResult();
                
                if (gitHubUserInfo == null || string.IsNullOrEmpty(gitHubUserInfo.Email))
                {
                    return new ErrorDataResult<User>(Messages.UserNotFound);
                }

                // Check if user exists
                var existingUser = _userService.GetByMail(gitHubUserInfo.Email);
                
                if (existingUser != null)
                {
                    // User exists, return success
                    return new SuccessDataResult<User>(existingUser, Messages.SuccessfulLogin);
                }
                else
                {
                    // Create new user with GitHub profile
                    var names = gitHubUserInfo.Name?.Split(' ') ?? new[] { gitHubUserInfo.Login, "" };
                    var firstName = names.Length > 0 ? names[0] : gitHubUserInfo.Login;
                    var lastName = names.Length > 1 ? names[1] : "";
                    
                    // Generate a random password hash/salt since the user won't use password to login
                    byte[] passwordHash, passwordSalt;
                    HashingHelper.CreatePasswordHash(Guid.NewGuid().ToString(), out passwordHash, out passwordSalt);
                    
                    var newUser = new User
                    {
                        Email = gitHubUserInfo.Email,
                        FirstName = firstName,
                        LastName = lastName,
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt,
                        Status = true
                    };
                    
                    _userService.Add(newUser);
                    return new SuccessDataResult<User>(newUser, Messages.GitHubUserRegistered);
                }
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<User>(ex.Message);
            }
        }
    }
}
