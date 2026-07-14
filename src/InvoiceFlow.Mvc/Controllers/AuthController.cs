using InvoiceFlow.Domain;
using InvoiceFlow.Infrastructure.Persistence;
using InvoiceFlow.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Mvc.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly InvoiceFlowDbContext _db;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        InvoiceFlowDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request)
    {
        var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) || !Uri.IsWellFormedUriString(request.ReturnUrl, UriKind.Relative)
            ? "/"
            : request.ReturnUrl;

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Redirect($"/register?error={Uri.EscapeDataString("Email and password are required.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        if (request.Password != request.ConfirmPassword)
        {
            return Redirect($"/register?error={Uri.EscapeDataString("Passwords do not match.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        if (request.Password.Length < 6)
        {
            return Redirect($"/register?error={Uri.EscapeDataString("Password must be at least 6 characters.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Redirect($"/register?error={Uri.EscapeDataString("An account with this email already exists.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var workspace = new Workspace($"{request.Email}'s Workspace");
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

            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return Redirect($"/register?error={Uri.EscapeDataString(errors)}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        await _signInManager.SignInAsync(user, true);
        return Redirect(returnUrl);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] LoginRequest request)
    {
        var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) || !Uri.IsWellFormedUriString(request.ReturnUrl, UriKind.Relative)
            ? "/"
            : request.ReturnUrl;

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Redirect($"/login?error={Uri.EscapeDataString("Email and password are required.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, true, false);

        if (result.Succeeded)
        {
            return Redirect(returnUrl);
        }

        return Redirect($"/login?error={Uri.EscapeDataString("Invalid email or password.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/login");
    }
}
