﻿using Microsoft.AspNetCore.Mvc;
using BWC.DataConnection;
using Microsoft.EntityFrameworkCore;
using BWC.Models;
using System.Threading.Tasks;
using BWC.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace BWC.Controllers
{
    public class LoginController : Controller
    {
        private readonly SqlServerDbContext _context;
        private readonly IUserService _userService;

        public LoginController(SqlServerDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear user information from the user service
            _userService.UserId = null;
            _userService.Username = null;

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        var user = await _context.Users
                            .FirstOrDefaultAsync(u => (u.Email == model.Username || u.Username == model.Username) && u.PasswordHash == model.Password);

                        if (user != null)
                        {
                            // Save user information in the user service
                            _userService.UserId = user.Id.ToString();
                            _userService.Username = user.Username;

                            // Create claims
                            var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim("Role", user.Role.ToString())
                    };

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = true
                            };

                            // Sign in the user
                            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                            // Redirect based on user role
                            switch (user.Role)
                            {
                                case 0:
                                    return RedirectToAction("Index", "StudentAppointment");
                                case 1:
                                    return RedirectToAction("Index", "CounselorAppointment");
                                case 2:
                                    return RedirectToAction("Index", "AdminAppointment");
                                default:
                                    // Handle other roles or default case
                                    ModelState.AddModelError(string.Empty, "Invalid role.");
                                    break;
                            }
                        }
                        else
                        {
                            // Login failed, show error message
                            ModelState.AddModelError(string.Empty, "Invalid Credentials.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception (optional)
                        // _logger.LogError(ex, "An error occurred during login.");

                        // Show a generic error message
                        ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
            }

            // If we got this far, something failed, redisplay form
            return View("Index", model);
        }


    }
}
