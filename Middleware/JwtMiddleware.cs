﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Ocsp;
using signup_verification.Helpers;

namespace signup_verification.Middleware
{
  public class JwtMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly AppSettings _appSettings;

    public JwtMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
    {
      _next = next;
      _appSettings = appSettings.Value;
    }

    public async Task Invoke(HttpContext context, DataContext dataContext)
    {
      // httpcontext 상의 Authorization 헤더 검증
      var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

      if (token != null)
        await attachAccountToContext(context, dataContext, token);

      await _next(context);
    }

    private async Task attachAccountToContext(HttpContext context, DataContext dataContext, string token)
    {
      try
      {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(key),
          ValidateIssuer = false,
          ValidateAudience = false,
          // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
          ClockSkew = TimeSpan.Zero
        }, out SecurityToken validatedToken);

        var jwtToken = (JwtSecurityToken)validatedToken;
        var accountId = int.Parse(jwtToken.Claims.First(c => c.Type == "id").Value);

        // attach
        context.Items["Account"] = await dataContext.Accounts.FindAsync(accountId);
      }
      catch
      {
        // do nothing
      }
    }
  }
}
