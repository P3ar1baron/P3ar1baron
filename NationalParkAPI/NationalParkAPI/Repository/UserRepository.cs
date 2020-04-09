﻿using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NationalParkAPI.DataAccess;
using NationalParkAPI.Models;
using NationalParkAPI.Repository.IRepository;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Security.Claims;

namespace NationalParkAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly AppSettings _appSettings;

        public UserRepository(ApplicationDbContext db, IOptions<AppSettings> appSettings)
        {
            _db = db;
            _appSettings = appSettings.Value;
        }
        public User Authenticate(string username, string password)
        {
            var user = _db.Users.SingleOrDefault(_ => _.Username == username && _.Password == password);

            //user not found
            if (user == null)
            {
                return null;
            }

            //if user was found generate JWT Token 
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name,user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials
                                (new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);
            user.Password = "";
            return user;
        }

        public bool IsUniqueUser(string username)
        {
            var user = _db.Users.SingleOrDefault(_ => _.Username == username);

            //return null if user not found
            if (user == null)
            {
                return true;
            }

            return false;
        }

        public User Register(string username, string password)
        {
            User userObj = new User()
            {
                Username = username,
                Password = password
            };

            _db.Users.Add(userObj);
            _db.SaveChanges();
            userObj.Password = "";
            return userObj;
        }
    }
}
