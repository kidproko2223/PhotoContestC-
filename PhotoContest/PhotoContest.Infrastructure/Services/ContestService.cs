namespace PhotoContest.Infrastructure.Services
{
    using PhotoContest.Infrastructure.Models.ViewModels.Strategy;

    using PhotoContest.Infrastructure.Interfaces;
    using System.Linq;
    using System.Collections.Generic;
    using AutoMapper.QueryableExtensions;

    using PhotoContest.Data.Interfaces;
    using PhotoContest.Infrastructure.Models.ViewModels.Contest;
    using PhotoContest.Models.Enums;
    using System;
    using System.Web;

    using PhotoContest.Common.Exceptions;
    using PhotoContest.Data.Strategies;
    using PhotoContest.Infrastructure.Models.BindingModels.Contest;
    using PhotoContest.Infrastructure.Models.BindingModels.Invitation;
    using PhotoContest.Models;
    using AutoMapper;
    using PhotoContest.Infrastructure.Models.BindingModels.Reward;

    public class ContestService : BaseService, IContestsService
    {
        private const string GoogleDrivePicturesBaseLink = "http://docs.google.com/uc?export=open&id=";

        private PictureService _pictureService;

        public ContestService(IPhotoContestData data)
            :base(data)
        {
            this._pictureService = new PictureService(data);
        }

        public IQueryable<ContestViewModel> GetTopNewestContests(int takeCount)
        {
            var topContests =
                this.Data.Contests.All()
                    .Where(c => c.Status == ContestStatus.Active)
                    .OrderByDescending(c => c.StartDate)
                    .Take(takeCount)
                    .Project()
                    .To<ContestViewModel>();
            return topContests;
        }

        public IEnumerable<ContestViewModel> GetActiveContests(string userId)
        {
            var activeContests = this.Data.Contests.All()
                .Where(c => c.Status == ContestStatus.Active)
                .OrderByDescending(c => c.StartDate)
                .ProjectTo<ContestViewModel>()
                .ToList();

            this.ApplyRights(activeContests, userId);

            return activeContests;
        }

        public IEnumerable<ContestViewModel> GetInactiveContests()
        {
            var inactiveContests = this.Data.Contests.All()
                .Where(c => c.Status != ContestStatus.Active)
                .OrderByDescending(c => c.StartDate)
                .ProjectTo<ContestViewModel>()
                .ToList();

            return inactiveContests;
        }

        public IEnumerable<ContestViewModel> GetMyContests(string userId = null)
        {
            var myContests = this.Data.Contests.All()
               .Where(c => c.OrganizatorId == userId)
               .OrderByDescending(c => c.StartDate)
               .Project()
               .To<ContestViewModel>()
               .ToList();

            return myContests;
        }

        public IEnumerable<ContestViewModel> GetAllContests(string userId = null)
        {
            var allContests = this.Data.Contests.All()
                    .OrderByDescending(c => c.StartDate)
                    .ProjectTo<ContestViewModel>()
                    .ToList();

            this.ApplyRights(allContests, userId);

            return allContests;
        }

        public UpdateContestBindingModel GetManageContest(int id, string userId)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                throw new NotFoundException("The selected contest does not exist");
            }

            var loggedUserId = userId;

            if (contest.OrganizatorId != loggedUserId)
            {
                throw new UnauthorizedException("Logged user is not the contest organizator");
            }

            return Mapper.Map<UpdateContestBindingModel>(contest);
        }

        public BaseContestViewModel GetPreviewContest(int id, string userId = null)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                throw new NotFoundException("The selected contest no longer exists");
            }

            if (contest.Status != ContestStatus.Active)
            {
                var viewModel = new PreviewInactiveContestViewModel
                {
                    Winners = this.Data.ContestWinners.All()
                        .Where(c => c.ContestId == contest.Id)
                        .ProjectTo<ContestWinnerViewModel>()
                        .ToList(),
                    Status = contest.Status
                };

                return viewModel;
            }

            var contestViewModel = Mapper.Map<PreviewContestViewModel>(contest);

            if (userId != null)
            {
                var user = this.Data.Users.Find(userId);

                if (contest.OrganizatorId == user.Id)
                {
                    contestViewModel.CanManage = true;
                }
                else
                {
                    if ((contest.ParticipationStrategy.ParticipationStrategyType != ParticipationStrategyType.Closed || (contest.ParticipationStrategy.ParticipationStrategyType == ParticipationStrategyType.Closed && user.PendingInvitations.Any(i => i.ContestId == contest.Id && i.Status == InvitationStatus.Neutral)))
                        && (!user.InContests.Any(c => c.Id == contest.Id)
                        && !user.CommitteeInContests.Any(c => c.Id == contest.Id)))
                    {
                        contestViewModel.CanParticipate = true;
                    }
                }
            }

            return contestViewModel;
        }

        public CreateContestViewModel GetCreateContest()
        {
            var viewModel = new CreateContestViewModel
            {
                RewardStrategies = this.Data.RewardStrategies.All()
                    .Select(r => new StrategyViewModel()
                    {
                        Id = r.Id,
                        Description = r.Description,
                        Name = r.Name,
                    })
                    .ToList(),
                ParticipationStrategies = this.Data.ParticipationStrategies.All()
                    .Select(p => new StrategyViewModel()
                    {
                        Id = p.Id,
                        Description = p.Description,
                        Name = p.Name,
                    })
                    .ToList(),
                VotingStrategies = this.Data.VotingStrategies.All()
                    .Select(v => new StrategyViewModel()
                    {
                        Id = v.Id,
                        Description = v.Description,
                        Name = v.Name,
                    })
                    .ToList(),
                DeadlineStrategies = this.Data.DeadlineStrategies.All()
                    .Select(dl => new StrategyViewModel()
                    {
                        Id = dl.Id,
                        Description = dl.Description,
                        Name = dl.Name,
                    })
                    .ToList()
            };

            return viewModel;
        }

        public bool DismissContest(int id, string userId)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                throw new NotFoundException(string.Format("Contest with id {0} not found", id));
            }

            if (contest.Status != ContestStatus.Active)
            {
                throw new BadRequestException(string.Format("Contest with id {0} is not active", id));
            }

            var loggedUser = this.Data.Users.Find(userId);

            if (contest.OrganizatorId != loggedUser.Id)
            {
                throw new UnauthorizedException("Only the contest organizator can dismiss it.");
            }

            contest.IsOpenForSubmissions = false;
            contest.Status = ContestStatus.Dismissed;
            contest.EndDate = DateTime.Now;

            this.Data.SaveChanges();

            return true;
        }

        public bool FinalizeContest(int id, string userId)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                throw new NotFoundException(string.Format("Contest with id {0} not found", id));
            }

            if (contest.Status != ContestStatus.Active)
            {
                throw new BadRequestException(string.Format("Contest with id {0} is not active", id));
            }

            var loggedUser = this.Data.Users.Find(userId);

            if (contest.OrganizatorId != loggedUser.Id)
            {
                throw new UnauthorizedException("Only the contest organizator can finalize it.");
            }

            contest.IsOpenForSubmissions = false;
            contest.Status = ContestStatus.Finalized;
            contest.EndDate = DateTime.Now;

            this.Data.SaveChanges();

            var rewardStrategy =
                    StrategyFactory.GetRewardStrategy(contest.RewardStrategy.RewardStrategyType);

            rewardStrategy.ApplyReward(this.Data, contest);

            return true;
        }

        public int InviteUser(CreateInvitationBindingModel model, string loggerUserId)
        {
            var contest = this.Data.Contests.Find(model.ContestId);

            if (contest == null)
            {
                throw new NotFoundException(string.Format("Contest with id {0} not found", model.ContestId));
            }

            var loggedUser = this.Data.Users.Find(loggerUserId);

            if (contest.OrganizatorId != loggedUser.Id)
            {
                throw new BadRequestException("Only the contest organizator can invite users.");
            }

            if (model.Type == InvitationType.Committee
                && contest.VotingStrategy.VotingStrategyType != VotingStrategyType.Closed)
            {
                throw new BadRequestException("The contest voting strategy type is not 'CLOSED'.");
            }

            if (model.Type == InvitationType.ClosedContest
                && contest.ParticipationStrategy.ParticipationStrategyType != ParticipationStrategyType.Closed)
            {
                throw new BadRequestException("The contest participation strategy type is not 'CLOSED'.");
            }

            if (!contest.IsOpenForSubmissions)
            {
                throw new BadRequestException("The contest is closed for submissions/registrations.");
            }

            var userToInvite = this.Data.Users.All().FirstOrDefault(u => u.UserName == model.Username);

            if (userToInvite == null)
            {
                throw new NotFoundException(string.Format("User with username {0} not found", model.Username));
            }

            if (contest.Participants.Contains(userToInvite))
            {
                throw new BadRequestException("You cannot invite user who already participates in the contest");
            }

            if (contest.Committee.Contains(userToInvite))
            {
                throw new BadRequestException("You cannot invite user who already is in the contest committee");
            }

            if (userToInvite.UserName == loggedUser.UserName)
            {
                throw new BadRequestException("Users cannot invite themselves.");
            }

            if (userToInvite.PendingInvitations.Any(i => i.ContestId == model.ContestId && i.Type == model.Type))
            {
                throw new BadRequestException(string.Format("User is already invited to contest with id {0}", contest.Id));
            }

            var invitation = new Invitation
            {
                ContestId = model.ContestId,
                InviterId = loggedUser.Id,
                InvitedId = userToInvite.Id,
                DateOfInvitation = DateTime.Now,
                Type = model.Type,
                Status = InvitationStatus.Neutral
            };

            if (model.Type == InvitationType.ClosedContest)
            {
                contest.InvitedUsers.Add(userToInvite);
            }

            userToInvite.PendingInvitations.Add(invitation);
            loggedUser.SendedInvitations.Add(invitation);

            this.Data.SaveChanges();

            return invitation.Id;
        }

        public int CreateContest(CreateContestBindingModel model, string userId)
        {
            if (this.Data.RewardStrategies.Find(model.RewardStrategyId) == null)
            {
                throw new NotFoundException("Not existing reward strategy");
            }

            if (this.Data.ParticipationStrategies.Find(model.ParticipationStrategyId) == null)
            {
                throw new NotFoundException("Not existing participation strategy");
            }

            if (this.Data.VotingStrategies.Find(model.VotingStrategyId) == null)
            {
                throw new NotFoundException("Not existing voting strategy");
            }

            if (this.Data.DeadlineStrategies.Find(model.DeadlineStrategyId) == null)
            {
                throw new NotFoundException("Not existing deadline strategy");
            }

            var loggedUserId = userId;

            var contest = new Contest
            {
                Title = model.Title,
                Description = model.Description,
                Status = ContestStatus.Active,
                RewardStrategyId = model.RewardStrategyId,
                VotingStrategyId = model.VotingStrategyId,
                ParticipationStrategyId = model.ParticipationStrategyId,
                DeadlineStrategyId = model.DeadlineStrategyId,
                ParticipantsLimit = model.ParticipantsLimit,
                TopNPlaces = model.TopNPlaces,
                SubmissionDeadline = model.SubmissionDeadline,
                IsOpenForSubmissions = true,
                StartDate = DateTime.Now,
                OrganizatorId = loggedUserId,
            };

            this.Data.Contests.Add(contest);
            this.Data.SaveChanges();

            return contest.Id;
        }

        public bool JoinContest(int id, IEnumerable<HttpPostedFileBase> files, string userId)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                throw new NotFoundException("Contest with such id does not exists");
            }

            var user = this.Data.Users.Find(userId);

            if (contest.Committee.Contains(user) || contest.Participants.Contains(user)
                || contest.OrganizatorId == user.Id)
            {
                throw new BadRequestException("You are either organizator of this contest or in the committee or you already participate in it");
            }

            var deadlineStrategy =
                StrategyFactory.GetDeadlineStrategy(contest.DeadlineStrategy.DeadlineStrategyType);

            deadlineStrategy.CheckDeadline(this.Data, contest, user);

            var participationStrategy =
                StrategyFactory.GetParticipationStrategy(contest.ParticipationStrategy.ParticipationStrategyType);

            participationStrategy.CheckPermission(this.Data, user, contest);

            if (contest.Participants.Contains(user))
            {
                throw new BadRequestException("You already participate in this contest");
            }

            if (contest.Committee.Contains(user))
            {
                throw new UnauthorizedException("You cannot participate in this contest, you are in the committee");
            }

            foreach (var file in files)
            {
                var result = this._pictureService.UploadImageToGoogleDrive(file, file.FileName, file.ContentType);

                if (result[0] != "success")
                {
                    throw new BadRequestException(result[1]);
                }

                Picture picture = new Picture
                {
                    UserId = user.Id,
                    Url = GoogleDrivePicturesBaseLink + result[1],
                    GoogleFileId = result[1],
                    ContestId = contest.Id
                };

                this.Data.Pictures.Add(picture);
            }

            contest.Participants.Add(user);
            this.Data.Contests.Update(contest);
            this.Data.SaveChanges();

            return true;
        }

        public bool UpdateContest(UpdateContestBindingModel model, string userId)
        {
            var contest = this.Data.Contests.Find(model.Id);

            if (contest == null)
            {
                throw new NotFoundException("The selected contest does not exist");
            }

            var loggedUserId = userId;

            if (contest.OrganizatorId != loggedUserId)
            {
                throw new UnauthorizedException("Logged user is not the contest organizator");
            }

            if (!string.IsNullOrWhiteSpace(model.Title))
            {
                contest.Title = model.Title;
            }

            if (!string.IsNullOrWhiteSpace(model.Description))
            {
                contest.Description = model.Description;
            }

            if (model.EndDate != default(DateTime))
            {
                contest.EndDate = model.EndDate;
            }

            this.Data.Contests.Update(contest);
            this.Data.SaveChanges();

            return true;
        }

        public bool AddRewards(int id, CreateRewardsBindingModel model, string userId)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                throw new NotFoundException("Contest not found");
            }

            if (contest.Status != ContestStatus.Active)
            {
                throw new BadRequestException("Cannot add reward to inactive contest.");
            }

            for (int i = 0; i < model.Name.Length; i++)
            {
                if (model.Place[i] < 1 || (contest.TopNPlaces != null && model.Place[i] > contest.TopNPlaces))
                {
                    throw new BadRequestException("Reward for unknown place");
                }

                contest.Rewards.Add(new Reward()
                {
                    ContestId = contest.Id,
                    Name = model.Name[i],
                    Description = model.Description[i],
                    Place = model.Place[i],
                    ImageUrl = model.ImageUrl[i]
                });
            }

            this.Data.SaveChanges();

            return true;
        }

        public bool JoinContestCommittee(int id, string userId)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest.Status != ContestStatus.Active)
            {
                throw new BadRequestException("The contest is not active");
            }

            var user = this.Data.Users.Find(userId);

            var invitation = contest.Organizator.SendedInvitations.FirstOrDefault(i => i.ContestId == contest.Id && i.InvitedId == user.Id && i.Type == InvitationType.Committee);

            if (invitation == null)
            {
                throw new BadRequestException("You don't have an invitation");
            }

            if (invitation.Status != InvitationStatus.Neutral)
            {
                throw new BadRequestException("You already have responded to the invitation");
            }

            invitation.Status = InvitationStatus.Accepted;
            contest.Committee.Add(user);

            this.Data.SaveChanges();
            return true;
        }

        private void ApplyRights(IList<ContestViewModel> contests, string userId = null)
        {
            if (userId != null)
            {
                var user = this.Data.Users.Find(userId);

                for (int i = 0; i < contests.Count(); i++)
                {
                    if (user.Id == contests[i].OrganizatorId)
                    {
                        contests[i].CanManage = true;
                        continue;
                    }

                    if (contests[i].ParticipationStrategyType != ParticipationStrategyType.Closed
                        && !user.InContests.Any(c => c.Id == contests[i].Id)
                        && !user.CommitteeInContests.Any(c => c.Id == contests[i].Id))
                    {
                        contests[i].CanParticipate = true;
                    }
                }
            }
        }
    }
}