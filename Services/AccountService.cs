using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using signup_verification.Entities;
using signup_verification.Helpers;
using signup_verification.Models.Accounts;
using BC = BCrypt.Net.BCrypt;

namespace signup_verification.Services
{
  public interface IAccountService
  {
    AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);

    AuthenticateResponse RefreshToken(string token, string ipAddress);

    void RevokeToken(string token, string ipAddress);

    void Register(RegisterRequest model, string origin);

    void VerifyEmail(string token);

    void ForgotPassword(ForgotPasswordRequest model, string origin);

    void ValidateResetToken(ValidateResetTokenRequest model);

    void ResetPassword(ResetPasswordRequest model);

    IEnumerable<AccountResponse> GetAll();

    AccountResponse GetBy(int id);

    AccountResponse Create(CreateRequest model);

    AccountResponse Update(int id, UpdateRequest model);

    void Delete(int id);
  }

  public class AccountService : IAccountService
  {
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly AppSettings _appSettings;
    private readonly IEmailService _emailService;

    public AccountService(DataContext context, IMapper mapper, IOptions<AppSettings> appSettings, IEmailService emailService)
    {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
      _appSettings = appSettings.Value;
      _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
    {
      var account = _context.Accounts.SingleOrDefault(x => x.Email == model.Email);

      if (account == null || !account.IsVerified || !BC.Verify(model.Password, account.PasswordHash))
        throw new AppException("Email or Password is incorrect.");

      // generate  authenticate
      var jwtToken = generateJwtToken(account);
      var refreshToken = generateRefreshToken(ipAddress);

      // save
      account.RefreshTokens.Add(refreshToken);
      _context.Update(account);
      _context.SaveChanges();

      var response = _mapper.Map<AuthenticateResponse>(account);
      response.JwtToken = jwtToken;
      response.RefreshToken = refreshToken.Token;

      return response;
    }

    public AccountResponse Create(CreateRequest model)
    {
      // validate
      if (_context.Accounts.Any(c => c.Email == model.Email))
        throw new AppException($"Email '{model.Email}' is already registered.");

      // map
      var account = _mapper.Map<Account>(model);
      account.CreatedAt = DateTime.UtcNow;
      account.VerifiedAt = DateTime.UtcNow;

      // hash
      account.PasswordHash = BC.HashPassword(model.Password);

      // save
      _context.Accounts.Add(account);
      _context.SaveChanges();

      return _mapper.Map<AccountResponse>(account);
    }

    public void Delete(int id)
    {
      var account = getAccount(id);
      _context.Accounts.Remove(account);
      _context.SaveChanges();
    }

    public void ForgotPassword(ForgotPasswordRequest model, string origin)
    {
      var account = _context.Accounts.SingleOrDefault(x => x.Email == model.Email);

      // always return ok response to prevent email enmeration
      if (account == null) return;

      // create
      account.ResetToken = randomTokenString();
      account.ResetTokenExpires = DateTime.UtcNow.AddDays(24);

      _context.Accounts.Update(account);
      _context.SaveChanges();

      // send email
      sendPasswordResetEmail(account, origin);
    }

    public IEnumerable<AccountResponse> GetAll()
    {
      var accounts = _context.Accounts;
      return _mapper.Map<IList<AccountResponse>>(accounts);
    }

    public AccountResponse GetBy(int id)
    {
      var account = getAccount(id);
      return _mapper.Map<AccountResponse>(account);
    }

    public AuthenticateResponse RefreshToken(string token, string ipAddress)
    {
      var (refreshToken, account) = getRefreshToken(token);

      // replace
      var newRefreshToken = generateRefreshToken(ipAddress);
      refreshToken.Revoked = DateTime.UtcNow;
      refreshToken.RevokedByIp = ipAddress;
      refreshToken.ReplacedByToken = newRefreshToken.Token;
      account.RefreshTokens.Add(newRefreshToken);
      _context.Update(account);
      _context.SaveChanges();

      // generate
      var jwtToken = generateJwtToken(account);

      var response = _mapper.Map<AuthenticateResponse>(account);
      response.JwtToken = jwtToken;
      response.RefreshToken = newRefreshToken.Token;

      return response;
    }

    public void Register(RegisterRequest model, string origin)
    {
      // validate
      if (_context.Accounts.Any(x => x.Email == model.Email))
      {
        // send already registered error in email to prevent account enumeration
        sendAlreadyRegisteredEmail(model.Email, origin);
        return;
      }

      // map
      var account = _mapper.Map<Account>(model);

      // first registered account is an admin
      var isFirstAccount = _context.Accounts.Count() == 0;
      account.Role = isFirstAccount ? Role.Admin : Role.User;
      account.CreatedAt = DateTime.UtcNow;
      account.VerificationToken = randomTokenString();

      // hash
      account.PasswordHash = BC.HashPassword(model.Password);

      // save
      _context.Accounts.Add(account);
      _context.SaveChanges();

      // send email
      sendVerificationEmail(account, origin);
    }

    public void ResetPassword(ResetPasswordRequest model)
    {
      var account = _context.Accounts.SingleOrDefault(a => a.ResetToken == model.Token && a.ResetTokenExpires > DateTime.UtcNow);

      if (account == null) throw new AppException("Invalid token.");

      // update
      account.PasswordHash = BC.HashPassword(model.Password);
      account.PasswordReset = DateTime.UtcNow;
      account.ResetToken = null;
      account.ResetTokenExpires = null;

      _context.Accounts.Update(account);
      _context.SaveChanges();
    }

    public void RevokeToken(string token, string ipAddress)
    {
      var (refreshToken, account) = getRefreshToken(token);

      // revoke
      refreshToken.Revoked = DateTime.UtcNow;
      refreshToken.RevokedByIp = ipAddress;
      _context.Update(account);
      _context.SaveChanges();
    }

    public AccountResponse Update(int id, UpdateRequest model)
    {
      var account = getAccount(id);

      // validate
      if (account.Email != model.Email && _context.Accounts.Any(a => a.Email == model.Email))
        throw new AppException($"Email '{model.Email}' is already taken.");

      // hash
      if (!string.IsNullOrEmpty(model.Password))
        account.PasswordHash = BC.HashPassword(model.Password);

      // copy
      _mapper.Map(model, account);
      account.UpdatedAt = DateTime.UtcNow;
      _context.Accounts.Update(account);
      _context.SaveChanges();

      return _mapper.Map<AccountResponse>(account);
    }

    public void ValidateResetToken(ValidateResetTokenRequest model)
    {
      var account = _context.Accounts.SingleOrDefault(s =>
      s.ResetToken == model.Token && s.ResetTokenExpires > DateTime.UtcNow);

      if (account == null)
        throw new AppException("Invalid token.");
    }

    public void VerifyEmail(string token)
    {
      var account = _context.Accounts.SingleOrDefault(x => x.VerificationToken == token);

      if (account == null) throw new AppException("Verfication failed.");

      account.VerifiedAt = DateTime.UtcNow;
      account.VerificationToken = null;

      _context.Accounts.Update(account);
      _context.SaveChanges();
    }

    #region helper methods

    private Account getAccount(int id)
    {
      var account = _context.Accounts.Find(id);
      if (account == null) throw new KeyNotFoundException("Account not found.");
      return account;
    }

    private (RefreshToken, Account) getRefreshToken(string token)
    {
      var account = _context.Accounts.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
      if (account == null) throw new AppException("Invalid token.");
      var refreshToken = account.RefreshTokens.Single(x => x.Token == token);
      if (!refreshToken.IsActive) throw new AppException("Invalid token.");

      return (refreshToken, account);
    }

    private string generateJwtToken(Account account)
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
        Expires = DateTime.UtcNow.AddMinutes(15),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
      };
      var token = tokenHandler.CreateToken(tokenDescriptor);

      return tokenHandler.WriteToken(token);
    }

    private RefreshToken generateRefreshToken(string ipAddress)
    {
      return new RefreshToken
      {
        Token = randomTokenString(),
        Expires = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow,
        CreatedByIp = ipAddress
      };
    }

    private string randomTokenString()
    {
      using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
      {
        var randomBytes = new byte[40];
        rngCryptoServiceProvider.GetBytes(randomBytes);

        // convert random bytes to hex string
        return BitConverter.ToString(randomBytes).Replace("-", "");
      }
    }

    private void sendVerificationEmail(Account account, string origin)
    {
      string message;
      if (!string.IsNullOrEmpty(origin))
      {
        var verifyUrl = $"{origin}/account/verify-email?token={account.VerificationToken}";
        message = $@"<p>아래의 링크를 클릭하여 이메일을 인증하세요.</p>
                    <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";
      }
      else
      {
        message = $@"<p><code>/accounts/verify-email</code> 에 대한 이메일 인증은 아래의 코드를 이용하세요.</p>
                     <p><code>{account.VerificationToken}</code></p>";
      }

      _emailService.Send(to: account.Email, subject: "Sign-up Verification API - 이메일 인증",
        html: $@"<h4>이메일 인증</h4>
                 <p>인증해주세요.</p>
                 {message}");
    }

    private void sendAlreadyRegisteredEmail(string email, string origin)
    {
      string message;
      if (!string.IsNullOrEmpty(origin))
        message = $@"<p>비밀번호를 분실하였다면 <a href=""{origin}/account/forgot-password"">forgot-password</a>를 클릭하세요.</p>";
      else
        message = "<p><code>/accounts/forgot-password</code> 로 방문하세요.</p>";

      _emailService.Send(to: email, subject: "Sign-up Verification API - 가입되어 있는 이메일 입니다.",
        html: $@"<h4>이미 가입되어 있는 이메일 입니다.</h4>
                 <p>이메일: <strong>{email}</strong> 은 이미 가입되어 있습니다.</p>
                 {message}");
    }

    private void sendPasswordResetEmail(Account account, string origin)
    {
      string message;
      if (!string.IsNullOrEmpty(origin))
      {
        var resetUrl = $"{origin}/account/reset-password?token={account.ResetToken}";
        message = $@"<p>패스워드 재설정을 위해 아래의 링크를 클릭하세요. (이 링크는 1일간만 유효합니다.)</p>
                     <p><a href=""{resetUrl}"">{resetUrl}</a></p>";
      }
      else
      {
        message = $@"<p><code>/account/reset-password</code> 에 대한 패스워드 재설정을 위해 아래의 토큰을 이용하세요.</p>
                     <p><code>{account.ResetToken}</code></p>";
      }
      _emailService.Send(to: account.Email, subject: "Sign-up Verification API - 비밀번호 재설정",
        html: $@"<h4>비밀번호 재설정</h4>
                 {message}");
    }

    #endregion helper methods
  }
}