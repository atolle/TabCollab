using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabRepository.Dtos;
using TabRepository.Models;

namespace TabRepository
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<TabFile, TabFileDto>();
            CreateMap<TabVersion, TabVersionDto>()
                .ForMember(v => v.TabFileDto, option => option.MapFrom(f => f.TabFile));
        }
    }
}
