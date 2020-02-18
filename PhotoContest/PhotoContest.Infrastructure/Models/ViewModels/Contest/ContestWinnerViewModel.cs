﻿namespace PhotoContest.Infrastructure.Models.ViewModels.Contest
{
    using PhotoContest.Models;
    using AutoMapper;
    using PhotoContest.Infrastructure.Mapping;

    public class ContestWinnerViewModel : IMapFrom<ContestWinners>, IHaveCustomMappings
    {
        public int Id { get; set; }

        public string ContestTitle { get; set; }

        public string ContestDescription { get; set; }

        public string Winner { get; set; }

        public int Place { get; set; }

        public void CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<ContestWinners, ContestWinnerViewModel>()
               .ForMember(c => c.Id, opt => opt.MapFrom(c => c.ContestId))
               .ForMember(c => c.ContestTitle, opt => opt.MapFrom(c => c.Contest.Title))
               .ForMember(c => c.ContestDescription, opt => opt.MapFrom(c => c.Contest.Description))
               .ForMember(c => c.Place, opt => opt.MapFrom(c => c.Place))
               .ForMember(c => c.Winner, opt => opt.MapFrom(c => c.Winner.UserName))
               .ReverseMap();
        }
    }
}