using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using signup_verification.Entities;
using signup_verification.Helpers;
using signup_verification.Models.Accounts;
using signup_verification.Services;

namespace signup_verification.Controllers
{
  [Route("[controller]")]
  [ApiController]
  public class AccountsController : BaseController
  {
    private readonly IAccountService _accountService;
    private readonly IMapper _mapper;

    public AccountsController(IAccountService accountService, IMapper mapper)
    {
      _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
      _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpPost("authenticate")]
    public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
    {
      var response = _accountService.Authenticate(model, ipAddress());
      setTokenCookie(response.RefreshToken);

      return Ok(response);
    }

    [HttpPost("refresh-token")]
    public ActionResult<AuthenticateResponse> RefreshToken()
    {
      var refreshToken = Request.Cookies["refreshToken"];
      var response = _accountService.RefreshToken(refreshToken, ipAddress());
      setTokenCookie(response.RefreshToken);

      return Ok(refreshToken);
    }

    [Authorize]
    [HttpPost("revoke-token")]
    public IActionResult RevokeToken(RevokeTokenRequest model)
    {
      // accept toke from request body or cookie
      var token = model.Token ?? Request.Cookies["refreshToken"];

      if (string.IsNullOrEmpty(token))
        return BadRequest(new { message = "Token is required." });

      // users can revoke their own tokens and admins can revoke any tokens
      if (!Account.OwnsToken(token) && Account.Role != Role.Admin)
        return Unauthorized(new { message = "Unauthorized." });

      _accountService.RevokeToken(token, ipAddress());

      return Ok(new { message = "Token revoked." });
    }

    [HttpPost("register")]
    public IActionResult Register(RegisterRequest model)
    {
      _accountService.Register(model, Request.Headers["origin"]);

      return Ok(new { message = "Registration successful, check your email verification." });
    }

    [HttpPost("verify-email")]
    public IActionResult VerifyEmail(VerifyEmailRequest model)
    {
      _accountService.VerifyEmail(model.Token);

      return Ok(new { message = "Verification successful, you can now login." });
    }

    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword(ForgotPasswordRequest model)
    {
      _accountService.ForgotPassword(model, Request.Headers["origin"]);

      return Ok(new { message = "Please check your email for password reset instructions." });
    }

    [HttpPost("validate-reset-token")]
    public IActionResult ValidateResetToken(ValidateResetTokenRequest model)
    {
      _accountService.ValidateResetToken(model);

      return Ok(new { message = "Token is valid." });
    }

    [HttpPost("reset-password")]
    public IActionResult ResetPassword(ResetPasswordRequest model)
    {
      _accountService.ResetPassword(model);

      return Ok(new { message = "Password reset successful, you can now login." });
    }

    [Authorize(Role.Admin)]
    [HttpGet]
    public ActionResult<IEnumerable<AccountResponse>> GetAll()
    {
      var accounts = _accountService.GetAll();

      return Ok(accounts);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public ActionResult<AccountResponse> GetBy(int id)
    {
      if (id != Account.Id && Account.Role != Role.Admin)
        return Unauthorized(new { message = "Unauthorized" });

      var account = _accountService.GetBy(id);

      return Ok(account);
    }

    [Authorize(Role.Admin)]
    [HttpPost]
    public ActionResult<AccountResponse> Create(CreateRequest model)
    {
      var account = _accountService.Create(model);

      return Ok(account);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public ActionResult<AccountResponse> Update(int id, UpdateRequest model)
    {
      if (id != Account.Id && Account.Role != Role.Admin)
        return Unauthorized(new { message = "Unauthorized" });

      if (Account.Role != Role.Admin)
        model.Role = null;

      var account = _accountService.Update(id, model);

      return Ok(account);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
      if (id != Account.Id && Account.Role != Role.Admin)
        return Unauthorized(new { message = "Unauthorized" });

      _accountService.Delete(id);

      return Ok(new { message = "Account is deleted." });
    }

    #region helper methods

    private void setTokenCookie(string token)
    {
      var cookieOptions = new CookieOptions
      {
        HttpOnly = true,
        Expires = DateTime.UtcNow.AddDays(7)
      };
      Response.Cookies.Append("refreshToken", token, cookieOptions);
    }

    private string ipAddress()
    {
      if (Request.Headers.ContainsKey("X-Forwarded-For"))
        return Request.Headers["X-Forwarded-For"];
      else
        return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
    }

    #endregion
  }
}
