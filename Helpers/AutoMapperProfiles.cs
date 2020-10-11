using System;
using AutoMapper;
using signup_verification.Entities;
using signup_verification.Models.Accounts;

namespace signup_verification.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<Account, AccountResponse>();
            CreateMap<Account, AuthenticateResponse>();
            CreateMap<RegisterRequest, Account>();
            CreateMap<CreateRequest, Account>();
            CreateMap<UpdateRequest, Account>()
                .ForAllMembers(x => x.Condition(
                    (src, dest, prop) =>
                    {
                        // null & empty 는 무시
                        if (prop == null) return false;
                        if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;

                        // role 이 null 인 경우
                        if (x.DestinationMember.Name == "Role" && src.Role == null) return false;

                        return true;
                    }));
        }
    }
}
