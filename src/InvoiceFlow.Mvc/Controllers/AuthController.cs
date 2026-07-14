using System.Security.Claims;
using InvoiceFlow.Infrastructure.Persistence;
using InvoiceFlow.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Mvc.Controllers;

[Authorize]
public class AuthController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly InvoiceFlowDbContext _db;

    public AuthController(
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
        Microsoft.AspNetCore.Identity.SignInManager<ApplicationUser> signInManager,
        Infrastructure.Persistence.InvoiceFlowDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl ?? "/";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) || !Uri.IsWellFormedUriString(request.ReturnUrl, UriKind.Relative)
            ? "/"
            : request.ReturnUrl;

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            ViewBag.Error = "Email and password are required.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, true, false);

        if (result.Succeeded)
        {
            return Redirect(returnUrl);
        }

        ViewBag.Error = "Invalid email or password.";
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl ?? "/";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) || !Uri.IsWellFormedUriString(request.ReturnUrl, UriKind.Relative)
            ? "/"
            : request.ReturnUrl;

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            ViewBag.Error = "Email and password are required.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        if (request.Password != request.ConfirmPassword)
        {
            ViewBag.Error = "Passwords do not match.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        if (request.Password.Length < 6)
        {
            ViewBag.Error = "Password must be at least 6 characters.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            ViewBag.Error = "An account with this email already exists.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var workspace = new Domain.Workspace($"{request.Email}'s Workspace");
        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            WorkspaceId = workspace.Id,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            _db.Workspaces.Remove(workspace);
            await _db.SaveChangesAsync();

            ViewBag.Error = string.Join(" ", result.Errors.Select(e => e.Description));
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        await _signInManager.SignInAsync(user, true);
        return Redirect(returnUrl);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }
}
