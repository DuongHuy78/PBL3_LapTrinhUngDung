using AutoMapper;
using Volunteer_website.Models;
using Volunteer_website.ViewModels;

namespace Volunteer_website.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() {
            CreateMap<RegisterVM, Volunteer>()
                .ForMember(dest => dest.VolunteerId, opt => opt.Ignore()) // Bỏ qua VolunteerId (sinh tự động)
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.HasValue ? src.DateOfBirth.Value.Date : (DateTime?)null))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.ImagePath))
                .ForMember(dest => dest.Donations, opt => opt.Ignore()) // Không map danh sách Donations
                .ForMember(dest => dest.Registrations, opt => opt.Ignore()); // Không map danh sách Registrations
            CreateMap<RegisterVM, User>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName)) // Đặt UserName là Email
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password)) // Map password
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => 1)) // Mặc định role là 1 (ví dụ: User)
                  .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true)) // Mặc định role là 1 (ví dụ: User)
                .ForMember(dest => dest.UserId, opt => opt.Ignore()); // UserId thường do hệ thống sinh ra

        }

    }
}
