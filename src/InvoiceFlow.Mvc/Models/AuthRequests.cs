namespace InvoiceFlow.Mvc.Models;

public record RegisterRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    string? ReturnUrl);

public record LoginRequest(
    string Email,
    string Password,
    string? ReturnUrl);
