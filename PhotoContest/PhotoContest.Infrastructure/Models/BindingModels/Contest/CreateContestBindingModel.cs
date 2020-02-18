﻿namespace PhotoContest.Infrastructure.Models.BindingModels.Contest
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class CreateContestBindingModel
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        [UIHint("TextField")]
        public string Title { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        [UIHint("TextField")]
        public string Description { get; set; }

        [Required]
        public int RewardStrategyId { get; set; }

        [Required]
        public int VotingStrategyId { get; set; }

        [Required]
        public int ParticipationStrategyId { get; set; }

        [Required]
        public int DeadlineStrategyId { get; set; }

        public int? ParticipantsLimit { get; set; }

        public int? TopNPlaces { get; set; }

        public DateTime? SubmissionDeadline { get; set; }
    }
}