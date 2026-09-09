using System.Security.Claims;
using System.Text.Json;
using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;

namespace backend.Functions;

public class AuthFunctions(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
{
    private static readonly JsonSerializerOptions RequestJsonOptions = JsonSerializerOptions.Web;

    [Function("Register")]
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequest req)
    {
        var request = await req.ReadFromJsonAsync<RegisterRequest>(RequestJsonOptions);
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new BadRequestObjectResult(new { message = "Email and password are required." });
        }

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new BadRequestObjectResult(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }

        return new OkObjectResult(new UserResponse(user.Id, user.Email ?? user.UserName!, 0));
    }

    [Function("Login")]
    public async Task<IActionResult> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequest req)
    {
        var request = await req.ReadFromJsonAsync<LoginRequest>(RequestJsonOptions);
        if (request is null || string.IsNullOrWhiteSpace(request.Email))
        {
            return new UnauthorizedResult();
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return new UnauthorizedResult();
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password, false, false);
        if (!result.Succeeded)
        {
            return new UnauthorizedResult();
        }

        return new OkObjectResult(await ToUserResponseAsync(user));
    }

    [Function("Logout")]
    public async Task<IActionResult> Logout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/logout")] HttpRequest req)
    {
        await signInManager.SignOutAsync();
        return new NoContentResult();
    }

    [Function("Me")]
    public async Task<IActionResult> Me(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/me")] HttpRequest req)
    {
        var user = await GetAuthenticatedUserAsync(req.HttpContext);
        if (user is null)
        {
            return new UnauthorizedResult();
        }

        return new OkObjectResult(await ToUserResponseAsync(user));
    }

    [Function("PasskeyCreateOptions")]
    public async Task<IActionResult> PasskeyCreateOptions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/passkey/create-options")] HttpRequest req)
    {
        var user = await GetAuthenticatedUserAsync(req.HttpContext);
        if (user is null)
        {
            return new UnauthorizedResult();
        }

        var userId = await userManager.GetUserIdAsync(user);
        var userName = await userManager.GetUserNameAsync(user) ?? "User";

        var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = userId,
            Name = userName,
            DisplayName = userName
        });

        return new ContentResult
        {
            StatusCode = StatusCodes.Status200OK,
            Content = optionsJson,
            ContentType = "application/json"
        };
    }

    [Function("PasskeyRegister")]
    public async Task<IActionResult> PasskeyRegister(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/passkey/register")] HttpRequest req)
    {
        var user = await GetAuthenticatedUserAsync(req.HttpContext);
        if (user is null)
        {
            return new UnauthorizedResult();
        }

        var request = await req.ReadFromJsonAsync<PasskeyRegistrationRequest>(RequestJsonOptions);
        if (request is null || string.IsNullOrWhiteSpace(request.CredentialJson))
        {
            return new BadRequestObjectResult(new { message = "credentialJson is required." });
        }

        var attestationResult = await signInManager.PerformPasskeyAttestationAsync(request.CredentialJson);
        if (!attestationResult.Succeeded)
        {
            return new BadRequestObjectResult(new { message = attestationResult.Failure?.Message ?? "Passkey registration failed." });
        }

        if (attestationResult.Passkey is not { } passkey)
        {
            return new BadRequestObjectResult(new { message = "No passkey data was returned." });
        }

        if (!string.Equals(attestationResult.UserEntity.Id, user.Id, StringComparison.Ordinal))
        {
            return new BadRequestObjectResult(new { message = "Passkey was created for a different user." });
        }

        passkey.Name = string.IsNullOrWhiteSpace(request.Name) ? "Passkey" : request.Name.Trim();

        var addResult = await userManager.AddOrUpdatePasskeyAsync(user, passkey);
        if (!addResult.Succeeded)
        {
            return new BadRequestObjectResult(new { message = string.Join(" ", addResult.Errors.Select(e => e.Description)) });
        }

        return new OkObjectResult(ToPasskeyInfoResponse(passkey));
    }

    [Function("PasskeyLoginOptions")]
    public async Task<IActionResult> PasskeyLoginOptions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/passkey/login-options")] HttpRequest req)
    {
        var request = await req.ReadFromJsonAsync<PasskeyLoginOptionsRequest>(RequestJsonOptions);
        ApplicationUser? user = null;
        if (!string.IsNullOrWhiteSpace(request?.Username))
        {
            user = await userManager.FindByEmailAsync(request.Username);
            user ??= await userManager.FindByNameAsync(request.Username);
        }

        var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
        return new ContentResult
        {
            StatusCode = StatusCodes.Status200OK,
            Content = optionsJson,
            ContentType = "application/json"
        };
    }

    [Function("PasskeyLogin")]
    public async Task<IActionResult> PasskeyLogin(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/passkey/login")] HttpRequest req)
    {
        var request = await req.ReadFromJsonAsync<PasskeyLoginRequest>(RequestJsonOptions);
        if (request is null || string.IsNullOrWhiteSpace(request.CredentialJson))
        {
            return new BadRequestObjectResult(new { message = "credentialJson is required." });
        }

        var result = await signInManager.PasskeySignInAsync(request.CredentialJson);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return new ObjectResult(new { message = "Account is locked out." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            return new UnauthorizedResult();
        }

        var user = await userManager.GetUserAsync(req.HttpContext.User);
        if (user is null)
        {
            return new UnauthorizedResult();
        }

        return new OkObjectResult(await ToUserResponseAsync(user));
    }

    [Function("PasskeyList")]
    public async Task<IActionResult> PasskeyList(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/passkeys")] HttpRequest req)
    {
        var user = await GetAuthenticatedUserAsync(req.HttpContext);
        if (user is null)
        {
            return new UnauthorizedResult();
        }

        var passkeys = await userManager.GetPasskeysAsync(user);
        return new OkObjectResult(passkeys.Select(ToPasskeyInfoResponse));
    }

    [Function("PasskeyDelete")]
    public async Task<IActionResult> PasskeyDelete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "auth/passkeys/{credentialId}")] HttpRequest req,
        string credentialId)
    {
        var user = await GetAuthenticatedUserAsync(req.HttpContext);
        if (user is null)
        {
            return new UnauthorizedResult();
        }

        if (!TryDecodeBase64Url(credentialId, out var credentialIdBytes))
        {
            return new BadRequestObjectResult(new { message = "Credential id is not valid." });
        }

        var result = await userManager.RemovePasskeyAsync(user, credentialIdBytes);
        if (!result.Succeeded)
        {
            return new BadRequestObjectResult(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }

        return new NoContentResult();
    }

    private async Task<ApplicationUser?> GetAuthenticatedUserAsync(HttpContext context)
    {
        var principal = await GetAuthenticatedPrincipalAsync(context);
        return principal is null ? null : await userManager.GetUserAsync(principal);
    }

    private async Task<ClaimsPrincipal?> GetAuthenticatedPrincipalAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        return result.Succeeded ? result.Principal : null;
    }

    private async Task<UserResponse> ToUserResponseAsync(ApplicationUser user)
    {
        var passkeyCount = (await userManager.GetPasskeysAsync(user)).Count;
        return new UserResponse(user.Id, user.Email ?? user.UserName ?? string.Empty, passkeyCount);
    }

    private static PasskeyInfoResponse ToPasskeyInfoResponse(UserPasskeyInfo passkey)
        => new(
            Id: Base64UrlEncode(passkey.CredentialId),
            Name: string.IsNullOrWhiteSpace(passkey.Name) ? "Passkey" : passkey.Name,
            CreatedAt: passkey.CreatedAt,
            IsBackedUp: passkey.IsBackedUp,
            IsUserVerified: passkey.IsUserVerified);

    private static string Base64UrlEncode(byte[] bytes) => WebEncoders.Base64UrlEncode(bytes);

    private static bool TryDecodeBase64Url(string value, out byte[] bytes)
    {
        try
        {
            bytes = WebEncoders.Base64UrlDecode(value);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }
}