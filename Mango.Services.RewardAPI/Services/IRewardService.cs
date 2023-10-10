using Mango.Services.RewardAPI.Message;

namespace Mango.Services.RewardAPI.Services
{
    public interface IRewardService
    {
        Task UpdateRewards(RewardsService rewardsMessage);
    }
}
