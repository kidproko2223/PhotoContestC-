﻿namespace PhotoContest.Infrastructure.Models.BindingModels.Contest
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using PhotoContest.Models;
    using PhotoContest.Models.Enums;
    using PhotoContest.Infrastructure.Mapping;

    public class UpdateContestBindingModel : IMapFrom<Contest>
    {
        [Required]
        public int Id { get; set; }

        [StringLength(200, MinimumLength = 3)]
        [UIHint("TextField")]
        public string Title { get; set; }

        [StringLength(200, MinimumLength = 3)]
        [UIHint("TextField")]
        public string Description { get; set; }

        public VotingStrategy VotingStrategy { get; set; }

        public ParticipationStrategy ParticipationStrategy { get; set; }

        public ContestStatus Status { get; set; }

        [UIHint("DateTimeNullable")]
        public DateTime EndDate { get; set; }
    }
}