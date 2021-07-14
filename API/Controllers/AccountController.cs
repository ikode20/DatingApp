using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext db;
        private readonly ITokenService tokenService;

        public AccountController(DataContext db, ITokenService tokenService)
        {
            this.db = db;
            this.tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto dto)
        {
            if (await UserExists(dto.Username))
            {
                return BadRequest("Username already exists");
            }

            using var hmac = new HMACSHA512();

            var user = new AppUser
            {
                UserName = dto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)),
                PasswordSalt = hmac.Key
            };

            this.db.Users.Add(user);

            await this.db.SaveChangesAsync();

            return new UserDto
            {
                Username = user.UserName,
                Token = this.tokenService.CreateToken(user)
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto dto)
        {
            var user = await this.db.Users.SingleOrDefaultAsync(u => u.UserName == dto.Username);

            if (user == null)
            {
                return Unauthorized("Invalid username");
            }

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));

            for (int i = 0; i < computeHash.Length; i++)
            {
                if (computeHash[i] != user.PasswordHash[i])
                {
                    return Unauthorized("Invalid password");
                }
            }

            return new UserDto
            {
                Username = user.UserName,
                Token = this.tokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await this.db.Users.AnyAsync(u => u.UserName == username.ToLower());
        }

    }
}